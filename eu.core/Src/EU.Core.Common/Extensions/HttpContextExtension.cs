using Microsoft.AspNetCore.Http;

namespace EU.Core.Common.Extensions;

public static class HttpContextExtension
{
	public static ISession GetSession(this HttpContext context)
	{
		try
		{
			return context.Session;
		}
		catch (Exception)
		{
			return default;
		}
    }
    public static string GetUserIp(this HttpContext context)
    {
        string realIP = null;
        string forwarded = null;
        if(context != null)
        {
            string remoteIpAddress = context.Connection.RemoteIpAddress.ToString();
            if (context.Request.Headers.ContainsKey("X-Real-IP"))
            {
                realIP = context.Request.Headers["X-Real-IP"].ToString(); 
                if (realIP != remoteIpAddress)
                {
                    remoteIpAddress = realIP;
                }
            }
            if (context.Request.Headers.ContainsKey("X-Forwarded-For"))
            {
                forwarded = context.Request.Headers["X-Forwarded-For"].ToString();
                if (forwarded != remoteIpAddress)
                {
                    remoteIpAddress = forwarded;
                }
            }
            return remoteIpAddress;
        }else return null;
        
    }
}