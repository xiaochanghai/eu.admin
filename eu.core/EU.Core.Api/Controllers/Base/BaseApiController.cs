namespace EU.Core.Controllers
{
    public class BaseApiController : Controller
    {
        [NonAction]
        public ServiceResult<T> Success<T>(T data, string msg = "成功")
        {
            return new ServiceResult<T>() { Success = true, Message = msg, Data = data, };
        }

        // [NonAction]
        //public ServiceResult<T> Success<T>(T data, string msg = "成功",bool success = true)
        //{
        //    return new ServiceResult<T>()
        //    {
        //        success = success,
        //        msg = msg,
        //        response = data,
        //    };
        //}
        [NonAction]
        public ServiceResult Success(string msg = "成功")
        {
            return new ServiceResult() { Success = true, Message = msg, Data = null, };
        }

        [NonAction]
        public ServiceResult<string> Failed(string msg = "失败", int status = 500)
        {
            return new ServiceResult<string>() { Success = false, Status = status, Message = msg, Data = null, };
        }

        [NonAction]
        public ServiceResult<T> Failed<T>(string msg = "失败", int status = 500)
        {
            return new ServiceResult<T>() { Success = false, Status = status, Message = msg, Data = default, };
        }

        [NonAction]
        public ServiceResult<PageModel<T>> SuccessPage<T>(int page, int dataCount, int pageSize, List<T> data, int pageCount, string msg = "获取成功")
        {
            return new ServiceResult<PageModel<T>>() { Success = true, Message = msg, Data = new PageModel<T>(page, dataCount, pageSize, data) };
        }

        [NonAction]
        public ServiceResult<PageModel<T>> SuccessPage<T>(PageModel<T> pageModel, string msg = "获取成功")
        {
            return new ServiceResult<PageModel<T>>() { Success = true, Message = msg, Data = pageModel };
        }
    }
}