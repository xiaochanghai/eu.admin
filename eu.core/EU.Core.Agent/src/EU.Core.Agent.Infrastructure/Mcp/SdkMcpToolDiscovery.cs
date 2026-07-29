using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using EU.Core.Agent.Application.Mcp;
using EU.Core.Agent.Application.Runtime;
using ModelContextProtocol.Client;

namespace EU.Core.Agent.Infrastructure.Mcp;

public sealed record McpDiscoverySettings(
    IReadOnlyList<string> AllowedHosts,
    IReadOnlyList<int> AllowedPorts,
    IReadOnlyList<string> AllowedStdioCommands,
    bool EnableStdio,
    TimeSpan ConnectionTimeout,
    TimeSpan DiscoveryTimeout);

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
    private readonly IMcpCredentialResolver _credentialResolver =
        credentialResolver ?? new EnvironmentMcpCredentialResolver();
    private readonly HashSet<string> _allowedHosts = new(
        settings.AllowedHosts,
        StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<int> _allowedPorts = new(settings.AllowedPorts);
    private readonly HashSet<string> _allowedCommands = new(
        settings.AllowedStdioCommands,
        StringComparer.OrdinalIgnoreCase);

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
                McpTransportKind.Stdio => CreateStdioTransport(server),
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
            McpTransportKind.Stdio => CreateStdioTransport(server),
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

    private IClientTransport CreateStdioTransport(McpServerDefinition server)
    {
        if (!string.IsNullOrWhiteSpace(server.CredentialAlias))
        {
            throw new InvalidOperationException(
                "Credential aliases are not injected into stdio processes.");
        }

        if (!settings.EnableStdio || !_allowedCommands.Contains(server.Command))
        {
            throw new InvalidOperationException(
                "The MCP stdio command is not enabled and allowlisted.");
        }

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

    private async ValueTask<Stream> ConnectPinnedAsync(
        SocketsHttpConnectionContext context,
        CancellationToken cancellationToken)
    {
        var endpoint = new UriBuilder(
            Uri.UriSchemeHttps,
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

    private async Task<IReadOnlyList<IPAddress>> ValidateEndpointAsync(
        Uri endpoint,
        CancellationToken cancellationToken)
    {
        //if ((endpoint.Scheme != Uri.UriSchemeHttp &&
        //     endpoint.Scheme != Uri.UriSchemeHttps) ||
        //    !string.IsNullOrEmpty(endpoint.UserInfo) ||
        //    !_allowedHosts.Contains(endpoint.DnsSafeHost) 
        //    //||
        //    //!_allowedPorts.Contains(endpoint.Port)
        //    )
        //{
        //    throw new InvalidOperationException(
        //        "The MCP endpoint is not an allowlisted HTTP or HTTPS origin.");
        //}

        IPAddress[] addresses = IPAddress.TryParse(
            endpoint.DnsSafeHost,
            out IPAddress? literal)
            ? [literal]
            : await Dns.GetHostAddressesAsync(endpoint.DnsSafeHost, cancellationToken);
        if (addresses.Length == 0 || addresses.Any(address => !IsConnectable(address)))
        {
            throw new InvalidOperationException(
                "The MCP endpoint resolves to an invalid network address.");
        }

        return addresses;
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
