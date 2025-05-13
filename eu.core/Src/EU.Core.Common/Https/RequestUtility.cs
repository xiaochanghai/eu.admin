using Elasticsearch.Net;
using EU.Core.Common.Const;
using EU.Core.Common.Helper;
using EU.Core.Model;
using Magicodes.IE.Core;
using RestSharp;

namespace EU.Core.Common.Https;

public class RequestUtility
{
    private RestSharp.Helper helper;

    public RequestUtility()
    {
        helper = new RestSharp.Helper();
    }
    public async Task<RestResponse> Get(string baseUrl)
    {
        //helper.AddCertificate("", "");
        var client = helper.SetUrl(baseUrl, "");
        var request = helper.CreateGetRequest();
        request.RequestFormat = DataFormat.Json;
        //request.Timeout
        var response = await helper.GetResponseAsync(client, request);
        return response;
    }
    public async Task<ServiceResult<T>> Get<T>(string baseUrl)
    {
        var response = await Get(baseUrl);
        if (response.IsSuccessful)
        {
            var content = response.Content;
            return ServiceResult<T>.OprateSuccess(JsonHelper.JsonToObj<T>(content), ResponseText.QUERY_SUCCESS);
        }
        else
            return ServiceResult<T>.OprateFailed(response.ErrorMessage);
    }
    public async Task<RestResponse> Post(string baseUrl, object postData)
    {
        //helper.AddCertificate("", "");
        var client = helper.SetUrl(baseUrl, "");
        var request = helper.CreateGetRequest();
        request.RequestFormat = DataFormat.Json;
        request.Method = Method.Post;
        request.AddHeader("Content-Type", "application/json");

        if (postData != null)
            request.AddJsonBody(postData);

        var response = await helper.GetResponseAsync(client, request);
        return response;
    }
    public async Task<ServiceResult<T>> Post<T>(string baseUrl, object postData)
    {
        var response = await Post(baseUrl, postData);
        if (response.IsSuccessful)
        {
            var content = response.Content;
            return ServiceResult<T>.OprateSuccess(JsonHelper.JsonToObj<T>(content), ResponseText.QUERY_SUCCESS);
        }
        else
            return ServiceResult<T>.OprateFailed(response.ErrorMessage);
    }
}