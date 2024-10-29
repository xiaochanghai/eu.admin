using EU.Core.Model;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Text.Encodings.Web;
using EU.Core.Common.HttpContextUser;

namespace EU.Core.AuthHelper;

public class ApiResponseHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IUser _user;

    public ApiResponseHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, IUser user) : base(options, logger, encoder)
    {
        _user = user;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        throw new NotImplementedException();
    }

    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.ContentType = "application/json";
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        await Response.WriteAsync(JsonConvert.SerializeObject((new ApiResponse(StatusCode.CODE401)).MessageModel));
    }

    protected override async Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        Response.ContentType = "application/json";
        if (_user.MessageModel != null)
        {
            Response.StatusCode = _user.MessageModel.Status;
            await Response.WriteAsync(JsonConvert.SerializeObject(_user.MessageModel));
        }
        else
        {
            Response.StatusCode = StatusCodes.Status403Forbidden;
            await Response.WriteAsync(JsonConvert.SerializeObject((new ApiResponse(StatusCode.CODE403)).MessageModel));
        }
    }
}