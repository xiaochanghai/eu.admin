using EU.Core.Hubs;
using Microsoft.AspNetCore.SignalR;
using System.Diagnostics;
using System.Runtime.InteropServices;
using EU.Core.Extensions.Middlewares;

namespace EU.Core.Controllers
{
    [Route("api/[Controller]/[action]")]
    [ApiController]
    [AllowAnonymous, ApiExplorerSettings(GroupName = Grouping.GroupName_Other)]
    public class MonitorController : BaseApiController
    {
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<MonitorController> _logger;

        public MonitorController(IHubContext<ChatHub> hubContext, IWebHostEnvironment env, ILogger<MonitorController> logger)
        {
            _hubContext = hubContext;
            _env = env;
            _logger = logger;
        }

        /// <summary>
        /// 服务器配置信息
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ServiceResult<ServerViewModel> Server()
        {
            return Success(new ServerViewModel()
            {
                EnvironmentName = _env.EnvironmentName,
                OSArchitecture = RuntimeInformation.OSArchitecture.ObjToString(),
                ContentRootPath = _env.ContentRootPath,
                WebRootPath = _env.WebRootPath,
                FrameworkDescription = RuntimeInformation.FrameworkDescription,
                MemoryFootprint = (Process.GetCurrentProcess().WorkingSet64 / 1048576).ToString("N2") + " MB",
                WorkingTime = DateTimeHelper.TimeSubTract(DateTime.Now, Process.GetCurrentProcess().StartTime)
            }, "获取服务器配置信息成功");
        }


        /// <summary>
        /// SignalR send data
        /// </summary>
        /// <returns></returns>
        // GET: api/Logs
        [HttpGet]
        public ServiceResult<List<LogInfo>> Get()
        {
            if (AppSettings.app(new string[] { "Middleware", "SignalRSendLog", "Enabled" }).ObjToBool())
            {
                _hubContext.Clients.All.SendAsync("ReceiveUpdate", LogLock.GetLogData()).Wait();
            }
            return Success<List<LogInfo>>(null, "执行成功");
        }



        [HttpGet]
        public ServiceResult<RequestApiWeekView> GetRequestApiinfoByWeek()
        {
            return Success(LogLock.RequestApiinfoByWeek(), "成功");
        }

        [HttpGet]
        public ServiceResult<AccessApiDateView> GetAccessApiByDate()
        {
            //return new ServiceResult<AccessApiDateView>()
            //{
            //    msg = "获取成功",
            //    success = true,
            //    response = LogLock.AccessApiByDate()
            //};

            return Success(LogLock.AccessApiByDate(), "获取成功");
        }

        [HttpGet]
        public ServiceResult<AccessApiDateView> GetAccessApiByHour()
        {
            //return new ServiceResult<AccessApiDateView>()
            //{
            //    msg = "获取成功",
            //    success = true,
            //    response = LogLock.AccessApiByHour()
            //};

            return Success(LogLock.AccessApiByHour(), "获取成功");
        }

        private List<UserAccessModel> GetAccessLogsToday(IWebHostEnvironment environment)
        {
            List<UserAccessModel> userAccessModels = new();
            var accessLogs = LogLock.ReadLog(
                Path.Combine(environment.ContentRootPath, "Log"), "RecordAccessLogs_", Encoding.UTF8, ReadType.PrefixLatest
                ).ObjToString();
            try
            {
                return JsonHelper.JsonToObj<List<UserAccessModel>>("[" + accessLogs + "]");
            }
            catch (Exception)
            {
                var accLogArr = accessLogs.Split("\n");
                foreach (var item in accLogArr)
                {
                    if (item.ObjToString() != "")
                    {
                        try
                        {
                            var accItem = JsonHelper.JsonToObj<UserAccessModel>(item.TrimEnd(','));
                            userAccessModels.Add(accItem);
                        }
                        catch (Exception)
                        {
                        }
                    }
                }

            }

            return userAccessModels;
        }
        private List<ActiveUserVM> GetAccessLogsTrend(IWebHostEnvironment environment)
        {
            List<ActiveUserVM> userAccessModels = new();
            var accessLogs = LogLock.ReadLog(
                Path.Combine(environment.ContentRootPath, "Log"), "ACCESSTRENDLOG_", Encoding.UTF8, ReadType.PrefixLatest
                ).ObjToString();
            try
            {
                return JsonHelper.JsonToObj<List<ActiveUserVM>>(accessLogs);
            }
            catch (Exception)
            {
                var accLogArr = accessLogs.Split("\n");
                foreach (var item in accLogArr)
                {
                    if (item.ObjToString() != "")
                    {
                        try
                        {
                            var accItem = JsonHelper.JsonToObj<ActiveUserVM>(item.TrimStart('[').TrimEnd(']'));
                            userAccessModels.Add(accItem);
                        }
                        catch (Exception)
                        {
                        }
                    }
                }

            }

            return userAccessModels;
        }

        [HttpGet]
        public ServiceResult<WelcomeInitData> GetActiveUsers([FromServices] IWebHostEnvironment environment)
        {
            var accessLogsToday = GetAccessLogsToday(environment).Where(d => d.BeginTime.ObjToDate() >= DateTime.Today);

            var Logs = accessLogsToday.OrderByDescending(d => d.BeginTime).Take(50).ToList();

            var errorCountToday = LogLock.GetLogData().Where(d => d.Import == 9).Count();

            accessLogsToday = accessLogsToday.Where(d => d.User != "").ToList();

            var activeUsers = (from n in accessLogsToday
                               group n by new { n.User } into g
                               select new ActiveUserVM
                               {
                                   user = g.Key.User,
                                   count = g.Count(),
                               }).ToList();

            int activeUsersCount = activeUsers.Count;
            activeUsers = activeUsers.OrderByDescending(d => d.count).Take(10).ToList();

            //return new ServiceResult<WelcomeInitData>()
            //{
            //    msg = "获取成功",
            //    success = true,
            //    response = new WelcomeInitData()
            //    {
            //        activeUsers = activeUsers,
            //        activeUserCount = activeUsersCount,
            //        errorCount = errorCountToday,
            //        logs = Logs,
            //        activeCount = GetAccessLogsTrend(environment)
            //    }
            //};

            return Success(new WelcomeInitData()
            {
                activeUsers = activeUsers,
                activeUserCount = activeUsersCount,
                errorCount = errorCountToday,
                logs = Logs,
                activeCount = GetAccessLogsTrend(environment)
            }, "获取成功");
        }

    }

    public class WelcomeInitData
    {
        public List<ActiveUserVM> activeUsers { get; set; }
        public int activeUserCount { get; set; }
        public List<UserAccessModel> logs { get; set; }
        public int errorCount { get; set; }
        public List<ActiveUserVM> activeCount { get; set; }
    }

}
