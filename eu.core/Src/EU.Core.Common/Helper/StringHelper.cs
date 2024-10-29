using System.Text;

namespace EU.Core.Common.Helper;

public class StringHelper
{
    public static string Id
    {
        get
        {
            Guid id = Guid.NewGuid();
            return id.ToString();
        }
    }
    public static Guid Id1
    {
        get
        {
            return Guid.NewGuid();
        }
    }

    /// <summary>
    /// 根据分隔符返回前n条数据
    /// </summary>
    /// <param name="content">数据内容</param>
    /// <param name="separator">分隔符</param>
    /// <param name="top">前n条</param>
    /// <param name="isDesc">是否倒序（默认false）</param>
    /// <returns></returns>
    public static List<string> GetTopDataBySeparator(string content, string separator, int top, bool isDesc = false)
    {
        if (string.IsNullOrEmpty(content))
            return new List<string>() { };

        if (string.IsNullOrEmpty(separator))
            throw new ArgumentException("message", nameof(separator));

        var dataArray = content.Split(separator).Where(d => !string.IsNullOrEmpty(d)).ToArray();
        if (isDesc)
            Array.Reverse(dataArray);

        if (top > 0)
            dataArray = dataArray.Take(top).ToArray();

        return dataArray.ToList();
    }
    /// <summary>
    /// 根据字段拼接get参数
    /// </summary>
    /// <param name="dic"></param>
    /// <returns></returns>
    public static string GetPars(Dictionary<string, object> dic)
    {

        StringBuilder sb = new();
        string urlPars = null;
        bool isEnter = false;
        foreach (var item in dic)
        {
            sb.Append($"{(isEnter ? "&" : "")}{item.Key}={item.Value}");
            isEnter = true;
        }
        urlPars = sb.ToString();
        return urlPars;
    }
    /// <summary>
    /// 根据字段拼接get参数
    /// </summary>
    /// <param name="dic"></param>
    /// <returns></returns>
    public static string GetPars(Dictionary<string, string> dic)
    {

        StringBuilder sb = new();
        string urlPars = null;
        bool isEnter = false;
        foreach (var item in dic)
        {
            sb.Append($"{(isEnter ? "&" : "")}{item.Key}={item.Value}");
            isEnter = true;
        }
        urlPars = sb.ToString();
        return urlPars;
    }
    /// <summary>
    /// 获取一个GUID
    /// </summary>
    /// <param name="format">格式-默认为N</param>
    /// <returns></returns>
    public static string GetGUID(string format = "N")
    {
        return Guid.NewGuid().ToString(format);
    }
    /// <summary>  
    /// 根据GUID获取19位的唯一数字序列  
    /// </summary>  
    /// <returns></returns>  
    public static long GetGuidToLongID()
    {
        byte[] buffer = Guid.NewGuid().ToByteArray();
        return BitConverter.ToInt64(buffer, 0);
    }
    /// <summary>
    /// 获取字符串最后X行
    /// </summary>
    /// <param name="resourceStr"></param>
    /// <param name="length"></param>
    /// <returns></returns>
    public static string GetCusLine(string resourceStr, int length)
    {
        string[] arrStr = resourceStr.Split("\r\n");
        return string.Join("", (from q in arrStr select q).Skip(arrStr.Length - length + 1).Take(length).ToArray());
    }

    #region 格式化数字字符
    /// <summary>
    /// 格式化数字字符，如传入1.24500，返回1.245
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static string TrimDecimalString(string value)
    {
        try
        {
            string result = string.Empty;
            if (!string.IsNullOrEmpty(value))
            {
                Decimal tmp = Decimal.Parse(value);
                result = string.Format("{0:#0.##########}", tmp);
            }
            return result;
        }
        catch (Exception)
        {
            throw;
        }
    }

    /// <summary>
    /// 格式化数字字符，并保留指定的小数位
    /// </summary>
    /// <param name="value">需要处理的值</param>
    /// <param name="reservedDigit">保留小数点后位数</param>
    /// <returns></returns>
    public static string TrimDecimalString(string value, int reservedDigit)
    {
        try
        {
            string result = string.Empty;
            if (!string.IsNullOrEmpty(value))
            {
                Decimal tmp = Decimal.Parse(value);
                if (reservedDigit == -1)
                    result = string.Format("{0:#0.##########}", tmp);
                else
                {
                    result = String.Format("{0:N" + reservedDigit.ToString() + "}", tmp);
                    result = result.Replace(",", "");
                }
            }
            return result;
        }
        catch (Exception) { throw; }
    }

    /// <summary>
    /// 格式化数字字符，并保留指定的小数位
    /// </summary>
    /// <param name="value">需要处理的值</param>
    /// <param name="reservedDigit">保留小数点后位数，-1时只会去除小数点后最后几位的0</param>
    /// <returns></returns>
    public static string TrimDecimalString(object value, int reservedDigit)
    {
        try
        {
            string result = string.Empty;
            if (!string.IsNullOrEmpty(Convert.ToString(value)))
            {
                Decimal tmp = Decimal.Parse(Convert.ToString(value));
                if (reservedDigit == -1)
                    result = string.Format("{0:#0.##########}", tmp);
                else
                {
                    result = String.Format("{0:N" + reservedDigit.ToString() + "}", tmp);
                    result = result.Replace(",", "");
                }
            }
            return result;
        }
        catch (Exception) { throw; }
    }

    /// <summary>
    /// 格式化数字字符，并保留指定的小数位
    /// </summary>
    /// <param name="value">需要处理的值</param>
    /// <param name="reservedDigit">保留小数点后位数，-1时只会去除小数点后最后几位的0</param>
    /// <returns></returns>
    public static decimal TrimDecimal(object value, int reservedDigit)
    {
        try
        {
            string result = string.Empty;
            if (!string.IsNullOrEmpty(Convert.ToString(value)))
            {
                Decimal tmp = Decimal.Parse(Convert.ToString(value));
                if (reservedDigit == -1)
                    result = string.Format("{0:#0.##########}", tmp);
                else
                {
                    result = String.Format("{0:N" + reservedDigit.ToString() + "}", tmp);
                    result = result.Replace(",", "");
                }
            }
            return Convert.ToDecimal(result);
        }
        catch (Exception) { throw; }
    }
    #endregion
}
