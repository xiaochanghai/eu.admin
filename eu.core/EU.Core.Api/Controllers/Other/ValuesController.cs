using EU.Core.Common.Https.HttpPolly;
using EU.Core.Common.Option;
using EU.Core.EventBus;
using EU.Core.EventBus.EventHandling;
using EU.Core.Extensions;
using EU.Core.Filter;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;

namespace EU.Core.Controllers;

/// <summary>
/// Values控制器
/// </summary>
[Route("api/[controller]/[action]")]
[ApiController, ApiExplorerSettings(GroupName = Grouping.GroupName_Other)]
//[Authorize]
//[Authorize(Roles = "Admin,Client")]
//[Authorize(Policy = "SystemOrAdmin")]
//[Authorize(PermissionNames.Permission)]
[Authorize]
public class ValuesController : BaseApiController
{ 
    private readonly Love _love; 
    private readonly IUser _user; 
    private readonly IHttpPollyHelper _httpPollyHelper;
    private readonly IRabbitMQPersistentConnection _persistentConnection;
    private readonly SeqOptions _seqOptions;

    /// <summary>
    /// ValuesController
    /// </summary>  
    /// <param name="love"></param> 
    /// <param name="user"></param> 
    /// <param name="httpPollyHelper"></param>
    /// <param name="persistentConnection"></param>
    /// <param name="seqOptions"></param>
    public ValuesController( 
         Love love 
        , IUser user 
        , IHttpPollyHelper httpPollyHelper
        , IRabbitMQPersistentConnection persistentConnection
        , IOptions<SeqOptions> seqOptions)
    {
        // 测试 Authorize 和 mapper 
        _love = love; 
        // 测试 Httpcontext
        _user = user; 
        // httpPolly
        _httpPollyHelper = httpPollyHelper;
        _persistentConnection = persistentConnection;
        _seqOptions = seqOptions.Value;
    }

    /// <summary>
    /// 测试Rabbit消息队列发送
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult TestRabbitMqPublish()
    {
        if (!_persistentConnection.IsConnected)
        {
            _persistentConnection.TryConnect();
        }
        _persistentConnection.PublishMessage("Hello, RabbitMQ!", exchangeName: "Tioboncore", routingKey: "myRoutingKey");
        return Ok();
    }

    /// <summary>
    /// 测试Rabbit消息队列订阅
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult TestRabbitMqSubscribe()
    {
        if (!_persistentConnection.IsConnected)
        {
            _persistentConnection.TryConnect();
        }

        _persistentConnection.StartConsuming("myQueue");
        return Ok();
    }

    private async Task<bool> Dealer(string exchange, string routingKey, byte[] msgBody, IDictionary<string, object> headers)
    {
        await Task.CompletedTask;
        Console.WriteLine("我是消费者，这里消费了一条信息是：" + Encoding.UTF8.GetString(msgBody));
        return true;
    }

    [HttpGet]
    public ServiceResult<List<ClaimDto>> MyClaims()
    {
        return new ServiceResult<List<ClaimDto>>()
        {
            Success = true,
            Data = (_user.GetClaimsIdentity().ToList()).Select(d =>
                new ClaimDto
                {
                    Type = d.Type,
                    Value = d.Value
                }
            ).ToList()
        };
    } 

    /// <summary>
    /// 测试Redis消息队列
    /// </summary>
    /// <param name="_redisBasketRepository"></param>
    /// <returns></returns>
    [HttpGet]
    [AllowAnonymous]
    public async Task RedisMq([FromServices] IRedisBasketRepository _redisBasketRepository)
    {
        var msg = $"这里是一条日志{DateTime.Now}";
        await _redisBasketRepository.ListLeftPushAsync(RedisMqKey.Loging, msg);
    }

    /// <summary>
    /// 测试RabbitMQ事件总线
    /// </summary>
    /// <param name="_eventBus"></param>
    /// <param name="TiobonId"></param>
    /// <returns></returns>
    [HttpGet]
    [AllowAnonymous]
    public void EventBusTry([FromServices] IEventBus _eventBus, string TiobonId = "1")
    {
        var TiobonDeletedEvent = new TiobonQueryIntegrationEvent(TiobonId);

        _eventBus.Publish(TiobonDeletedEvent);
    }

    /// <summary>
    /// Get(int id)方法
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    // GET api/values/5
    [HttpGet("{id}")]
    [AllowAnonymous]
    [TypeFilter(typeof(UseServiceDIAttribute), Arguments = new object[] { "laozhang" })]
    public ActionResult<string> Get(int id)
    {
        var loveu = _love.SayLoveU();

        return "value";
    }

    /// <summary>
    /// 测试参数是必填项
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    [HttpGet]
    [Route("/api/values/RequiredPara")]
    public string RequiredP([Required] string id)
    {
        return id;
    }


    /// <summary>
    /// 通过 HttpContext 获取用户信息
    /// </summary>
    /// <param name="ClaimType">声明类型，默认 jti </param>
    /// <returns></returns>
    [HttpGet]
    [Route("/api/values/UserInfo")]
    public ServiceResult<List<string>> GetUserInfo(string ClaimType = "jti")
    {
        var getUserInfoByToken = _user.GetUserInfoFromToken(ClaimType);
        return new ServiceResult<List<string>>()
        {
            Success = _user.IsAuthenticated(),
            Message = _user.IsAuthenticated() ? _user.Name.ObjToString() : "未登录",
            Data = _user.GetClaimValueByType(ClaimType)
        };
    }

    /// <summary>
    /// to redirect by route template name.
    /// </summary>
    [HttpGet("/api/custom/go-destination")]
    [AllowAnonymous]
    public void Source()
    {
        var url = Url.RouteUrl("Destination_Route");
        Response.Redirect(url);
    }

    /// <summary>
    /// route with template name.
    /// </summary>
    /// <returns></returns>
    [HttpGet("/api/custom/destination", Name = "Destination_Route")]
    [AllowAnonymous]
    public string Destination()
    {
        return "555";
    }

     
    /// <summary>
    /// 测试 post 参数
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    [HttpPost]
    [AllowAnonymous]
    public object TestPostPara(string name)
    {
        return Ok(new { success = true, name = name });
    }



    /// <summary>
    /// 测试Fulent做参数校验
    /// </summary>
    /// <param name="param"></param>
    /// <returns></returns>
    [HttpPost]
    [AllowAnonymous]
    public async Task<string> FluentVaTest([FromBody] UserRegisterVo param)
    {
        await Task.CompletedTask;
        return "Okay";
    }

    /// <summary>
    /// Put方法
    /// </summary>
    /// <param name="id"></param>
    /// <param name="value"></param>
    // PUT api/values/5
    [HttpPut("{id}")]
    public void Put(int id, [FromBody] string value)
    {
    }

    /// <summary>
    /// Delete方法
    /// </summary>
    /// <param name="id"></param>
    // DELETE api/values/5
    [HttpDelete("{id}")]
    public void Delete(int id)
    {
    }

    #region Apollo 配置

    /// <summary>
    /// 测试接入Apollo获取配置信息
    /// </summary>
    [HttpGet("/apollo")]
    [AllowAnonymous]
    public async Task<IEnumerable<KeyValuePair<string, string>>> GetAllConfigByAppllo(
        [FromServices] IConfiguration configuration)
    {
        return await Task.FromResult(configuration.AsEnumerable());
    }

    /// <summary>
    /// 通过此处的key格式为 xx:xx:x
    /// </summary>
    [HttpGet("/apollo/{key}")]
    [AllowAnonymous]
    public async Task<string> GetConfigByAppllo(string key)
    {
        return await Task.FromResult(AppSettings.app(key));
    }

    #endregion

    #region HttpPolly

    [HttpPost]
    [AllowAnonymous]
    public async Task<string> HttpPollyPost()
    {
        var response = await _httpPollyHelper.PostAsync(HttpEnum.LocalHost, "/api/ElasticDemo/EsSearchTest",
            "{\"from\": 0,\"size\": 10,\"word\": \"非那雄安\"}");

        return response;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<string> HttpPollyGet()
    {
        return await _httpPollyHelper.GetAsync(HttpEnum.LocalHost,
            "/api/ElasticDemo/GetDetailInfo?esid=3130&esindex=chinacodex");
    }

    #endregion

    [HttpPost]
    [AllowAnonymous]
    public string TestEnum(EnumDemoDto dto) => dto.Type.ToString();

    [HttpGet]
    [AllowAnonymous]
    public string TestOption()
    {
        return _seqOptions.ToJson();
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult TestDynamic()
    {
        dynamic obj = new ExpandoObject();
        dynamic data = new ExpandoObject();
        string message = "登陆成功！";

        obj.Success = true;
        obj.Message = message;
        obj.Data = data;
        return Ok(obj);
    }
}

public class ClaimDto
{
    public string Type { get; set; }
    public string Value { get; set; }
}