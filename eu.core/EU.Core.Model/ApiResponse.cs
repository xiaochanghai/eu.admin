﻿namespace EU.Core.Model;

public class ApiResponse
{
    public int Status { get; set; } = 200;
    public string Value { get; set; } = "";
    public ServiceResult<string> MessageModel = new ServiceResult<string>() { };

    public ApiResponse(StatusCode apiCode, string msg = null)
    {
        switch (apiCode)
        {
            case StatusCode.CODE401:
                {
                    Status = 401;
                    Value = msg ?? "很抱歉，您无权访问该接口，用户验证已失效!";
                }
                break;
            case StatusCode.CODE403:
                {
                    Status = 403;
                    Value = msg ?? "很抱歉，您的访问权限等级不够，联系管理员!";
                }
                break;
            case StatusCode.CODE404:
                {
                    Status = 404;
                    Value = "资源不存在!";
                }
                break;
            case StatusCode.CODE500:
                {
                    Status = 500;
                    Value = msg;
                }
                break;
        }

        MessageModel = new ServiceResult<string>()
        {
            Status = Status,
            Message = Value,
            Success = apiCode == StatusCode.CODE200
        };
    }
}

public enum StatusCode
{
    CODE200,
    CODE401,
    CODE403,
    CODE404,
    CODE500
}