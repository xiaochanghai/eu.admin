using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Globalization;
using System.Security.Cryptography;
using EU.Core.Agent.Application.Mcp;
using EU.Core.Agent.Application.Runtime;
using ModelContextProtocol.Client;

namespace EU.Core.Agent.Infrastructure.Mcp;

public sealed record McpDiscoverySettings(
    IReadOnlyList<string> AllowedHosts,
    IReadOnlyList<int> AllowedPorts,
    IReadOnlyList<McpStdioInvocation> StdioInvocations,
    bool EnableStdio,
    TimeSpan ConnectionTimeout,
    TimeSpan DiscoveryTimeout,
    bool AllowDevelopmentHttp = false);

public sealed record McpStdioInvocation(
    string Command,
    IReadOnlyList<string> Arguments,
    string ExecutableSha256 = "");

public interface IMcpCredentialResolver
{
    ValueTask<string?> ResolveAsync(
        string credentialAlias,
        CancellationToken cancellationToken = default);
}

public sealed class EnvironmentMcpCredentialResolver : IMcpCredentialResolver
{
    public ValueTask<string?> ResolveAsync(
        string credentialAlias,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string suffix = credentialAlias["alias:".Length..]
            .ToUpperInvariant()
            .Replace('-', '_')
            .Replace('.', '_');
        return ValueTask.FromResult(
            Environment.GetEnvironmentVariable($"AGENT_MCP_CREDENTIAL_{suffix}"));
    }
}

public sealed class SdkMcpToolDiscovery(
    McpDiscoverySettings settings,
    IMcpCredentialResolver? credentialResolver = null) : IMcpToolDiscovery
{
    private const long MaximumStdioExecutableBytes = 128L * 1024 * 1024;
    private const string StdioIntegrityFailure =
        "The MCP stdio executable failed integrity validation.";
    private readonly IMcpCredentialResolver _credentialResolver =
        credentialResolver ?? new EnvironmentMcpCredentialResolver();
    private readonly HashSet<string> _allowedHosts = new(
        settings.AllowedHosts.Select(NormalizeHost),
        StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<int> _allowedPorts = new(settings.AllowedPorts);
    private readonly IReadOnlyList<McpStdioInvocation> _stdioInvocations =
        settings.StdioInvocations
            .Select(invocation => new McpStdioInvocation(
                invocation.Command,
                invocation.Arguments.ToArray(),
                invocation.ExecutableSha256))
            .ToArray();

    public async Task<IReadOnlyList<DiscoveredMcpTool>> DiscoverAsync(
        McpServerDefinition server,
        CancellationToken cancellationToken = default)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(settings.ConnectionTimeout + settings.DiscoveryTimeout);
        try
        {
            IClientTransport transport = server.Transport switch
            {
                McpTransportKind.StreamableHttp => await CreateHttpTransportAsync(
                    server,
                    HttpTransportMode.StreamableHttp,
                    timeout.Token),
                McpTransportKind.Sse => await CreateHttpTransportAsync(
                    server,
                    HttpTransportMode.Sse,
                    timeout.Token),
                McpTransportKind.Stdio => await CreateStdioTransportAsync(
                    server,
                    timeout.Token),
                _ => throw new InvalidOperationException("The MCP transport is unsupported.")
            };

            await using McpClient client = await McpClient.CreateAsync(
                transport,
                cancellationToken: timeout.Token);
            IList<McpClientTool> tools = await client.ListToolsAsync(
                cancellationToken: timeout.Token);
            return tools.Select(tool => new DiscoveredMcpTool(
                    tool.ProtocolTool.Name,
                    tool.ProtocolTool.Description ?? string.Empty,
                    tool.ProtocolTool.InputSchema.GetRawText()))
                .ToArray();
        }
        catch (OperationCanceledException exception)
            when (!cancellationToken.IsCancellationRequested && timeout.IsCancellationRequested)
        {
            throw new TimeoutException("MCP connection or tool discovery timed out.", exception);
        }
    }

    internal async Task<McpClient> ConnectAsync(
        McpServerDefinition server,
        CancellationToken cancellationToken)
    {
        IClientTransport transport = server.Transport switch
        {
            McpTransportKind.StreamableHttp => await CreateHttpTransportAsync(
                server,
                HttpTransportMode.StreamableHttp,
                cancellationToken),
            McpTransportKind.Sse => await CreateHttpTransportAsync(
                server,
                HttpTransportMode.Sse,
                cancellationToken),
            McpTransportKind.Stdio => await CreateStdioTransportAsync(
                server,
                cancellationToken),
            _ => throw new InvalidOperationException("The MCP transport is unsupported.")
        };
        return await McpClient.CreateAsync(
            transport,
            cancellationToken: cancellationToken);
    }

    private async Task<IClientTransport> CreateHttpTransportAsync(
        McpServerDefinition server,
        HttpTransportMode mode,
        CancellationToken cancellationToken)
    {
        var endpoint = new Uri(server.Endpoint, UriKind.Absolute);
        await ValidateEndpointAsync(endpoint, cancellationToken);
        var handler = new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            UseProxy = false,
            ConnectCallback = ConnectPinnedAsync
        };
        var httpClient = new HttpClient(handler, disposeHandler: true)
        {
            Timeout = Timeout.InfiniteTimeSpan
        };
        if (!string.IsNullOrWhiteSpace(server.CredentialAlias))
        {
            string? credential = await _credentialResolver.ResolveAsync(
                server.CredentialAlias,
                cancellationToken);
            if (string.IsNullOrWhiteSpace(credential) ||
                credential.Length > 16_384 ||
                credential.Contains('\r') ||
                credential.Contains('\n'))
            {
                httpClient.Dispose();
                throw new InvalidOperationException(
                    "The MCP credential alias could not be resolved safely.");
            }

            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", credential);
        }
        return new HttpClientTransport(
            new HttpClientTransportOptions
            {
                Endpoint = endpoint,
                TransportMode = mode,
                ConnectionTimeout = settings.ConnectionTimeout
            },
            httpClient,
            loggerFactory: null,
            ownsHttpClient: true);
    }

    private async Task<IClientTransport> CreateStdioTransportAsync(
        McpServerDefinition server,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(server.CredentialAlias))
        {
            throw new InvalidOperationException(
                "Credential aliases are not injected into stdio processes.");
        }

        await ValidateStdioInvocationAsync(server, cancellationToken);

        return new StdioClientTransport(new StdioClientTransportOptions
        {
            Name = server.Code,
            Command = server.Command,
            Arguments = server.Arguments.ToList(),
            InheritEnvironmentVariables = false,
            EnvironmentVariables =
                StdioClientTransportOptions.GetDefaultEnvironmentVariables()
        });
    }

    internal async Task ValidateStdioInvocationAsync(
        McpServerDefinition server,
        CancellationToken cancellationToken = default)
    {
        StringComparer commandComparer = OperatingSystem.IsWindows()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;
        McpStdioInvocation? invocation = settings.EnableStdio
            ? _stdioInvocations.FirstOrDefault(invocation =>
            commandComparer.Equals(invocation.Command, server.Command) &&
            invocation.Arguments.SequenceEqual(server.Arguments, StringComparer.Ordinal))
            : null;
        if (invocation is null)
        {
            throw new InvalidOperationException(
                "The MCP stdio invocation is not enabled and allowlisted.");
        }

        if (!string.IsNullOrEmpty(invocation.ExecutableSha256))
        {
            await ValidateExecutableIntegrityAsync(invocation, cancellationToken);
        }
    }

    private static async Task ValidateExecutableIntegrityAsync(
        McpStdioInvocation invocation,
        CancellationToken cancellationToken)
    {
        try
        {
            byte[] expected = Convert.FromHexString(invocation.ExecutableSha256);
            await using var stream = new FileStream(invocation.Command, new FileStreamOptions
            {
                Mode = FileMode.Open,
                Access = FileAccess.Read,
                Share = FileShare.Read,
                Options = FileOptions.Asynchronous | FileOptions.SequentialScan
            });
            if (stream.Length is <= 0 or > MaximumStdioExecutableBytes)
            {
                throw new InvalidDataException();
            }

            byte[] actual = await SHA256.HashDataAsync(stream, cancellationToken);
            if (!CryptographicOperations.FixedTimeEquals(actual, expected))
            {
                throw new InvalidDataException();
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException or
            UnauthorizedAccessException or ArgumentException or FormatException or
            NotSupportedException)
        {
            throw new InvalidOperationException(StdioIntegrityFailure);
        }
    }

    private async ValueTask<Stream> ConnectPinnedAsync(
        SocketsHttpConnectionContext context,
        CancellationToken cancellationToken)
    {
        var endpoint = new UriBuilder(
            context.InitialRequestMessage.RequestUri?.Scheme ?? Uri.UriSchemeHttps,
            context.DnsEndPoint.Host,
            context.DnsEndPoint.Port).Uri;
        IReadOnlyList<IPAddress> addresses =
            await ValidateEndpointAsync(endpoint, cancellationToken);
        var socket = new Socket(
            addresses[0].AddressFamily,
            SocketType.Stream,
            ProtocolType.Tcp);
        try
        {
            await socket.ConnectAsync(
                new IPEndPoint(addresses[0], context.DnsEndPoint.Port),
                cancellationToken);
            return new NetworkStream(socket, ownsSocket: true);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }

    internal async Task<IReadOnlyList<IPAddress>> ValidateEndpointAsync(
        Uri endpoint,
        CancellationToken cancellationToken)
    {
        string host = NormalizeHost(endpoint.IdnHost);
        bool isDevelopmentHttp =
            endpoint.Scheme == Uri.UriSchemeHttp && settings.AllowDevelopmentHttp;
        if ((endpoint.Scheme != Uri.UriSchemeHttps && !isDevelopmentHttp) ||
            !string.IsNullOrEmpty(endpoint.UserInfo) ||
            !_allowedHosts.Contains(host) ||
            !_allowedPorts.Contains(endpoint.Port))
        {
            throw new InvalidOperationException(
                "The MCP endpoint is not an allowlisted HTTP or HTTPS origin.");
        }

        IPAddress[] addresses = IPAddress.TryParse(
            host,
            out IPAddress? literal)
            ? [literal]
            : await Dns.GetHostAddressesAsync(host, cancellationToken);
        if (addresses.Length == 0 || addresses.Any(address => !IsConnectable(address)))
        {
            throw new InvalidOperationException(
                "The MCP endpoint resolves to an invalid network address.");
        }

        if (isDevelopmentHttp && addresses.Any(address => !IPAddress.IsLoopback(address)))
        {
            throw new InvalidOperationException(
                "Development HTTP MCP endpoints must resolve only to loopback addresses.");
        }

        return addresses;
    }

    private static string NormalizeHost(string host)
    {
        string value = host.Trim().TrimEnd('.');
        if (value.Length >= 2 && value[0] == '[' && value[^1] == ']')
            value = value[1..^1];
        return IPAddress.TryParse(value, out IPAddress? address)
            ? address.ToString()
            : new IdnMapping().GetAscii(value).ToLowerInvariant();
    }

    private static bool IsConnectable(IPAddress address)
    {
        if (address.Equals(IPAddress.Any) ||
            address.Equals(IPAddress.IPv6Any) ||
            address.Equals(IPAddress.None) ||
            address.Equals(IPAddress.IPv6None))
        {
            return false;
        }

        if (address.IsIPv4MappedToIPv6)
        {
            return IsConnectable(address.MapToIPv4());
        }

        byte[] bytes = address.GetAddressBytes();
        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            byte first = bytes[0];
            return first < 224;
        }

        if (address.AddressFamily != AddressFamily.InterNetworkV6)
        {
            return false;
        }

        bool multicast = bytes[0] == 0xff;
        return !multicast;
    }
}
