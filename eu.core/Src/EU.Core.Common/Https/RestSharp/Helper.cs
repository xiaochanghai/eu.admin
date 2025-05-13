using RestSharp;
using System.Security.Cryptography.X509Certificates;

namespace EU.Core.Common.Https.RestSharp;

public class Helper
{
    private RestClient client;
    private RestRequest request;
    private X509Certificate2 certificate;


    public void AddCertificate(string certPath, string certFileName)
    {
        var certFile = Path.Combine(certPath, certFileName);
        certificate = new X509Certificate2(certFile);
    }

    public RestClient SetUrl(string baseUrl, string endpoint)
    {
        var url = Path.Combine(baseUrl, endpoint);
        client = new RestClient(url);
        //client.ClientCertificates = new X509CertificateCollection() { certificate };
        //client.Proxy = new WebProxy();
        return client;
    }

    public RestRequest CreateGetRequest()
    {
        request = new RestRequest()
        {
            Method = Method.Get
        };
        //request.AddHeader("Accept", "application/json");
        request.AddHeaders(new Dictionary<string, string>
        {
            { "Accept", "application/json" },
            { "Content-Type", "application/json" }

        });
        //request.AddFile("Test file", @"C:\Users\yadavm\Downloads\Test.txt", "multipart/form-data");
        return request;
    }

    public RestRequest CreatePostRequest<T>(T payload) where T : class
    {
        request = new RestRequest()
        {
            Method = Method.Post
        };
        request.AddHeader("Accept", "application/json");
        //request.AddParameter("application/json", payload, ParameterType.RequestBody);
        request.AddBody(payload);
        request.RequestFormat = DataFormat.Json;
        return request;
    }

    public RestRequest CreatePutRequest<T>(T payload) where T : class
    {
        request = new RestRequest()
        {
            Method = Method.Put
        };
        request.AddHeader("Accept", "application/json");
        //request.AddParameter("application/json", payload, ParameterType.RequestBody);
        request.AddBody(payload);
        request.RequestFormat = DataFormat.Json;
        return request;
    }

    public RestRequest CreateDeleteRequest()
    {
        request = new RestRequest()
        {
            Method = Method.Delete
        };
        request.AddHeader("Accept", "application/json");
        return request;
    }

    public async Task<RestResponse> GetResponseAsync(RestClient restClient, RestRequest restRequest)
    {
        return await restClient.ExecuteAsync(restRequest);
    }
}