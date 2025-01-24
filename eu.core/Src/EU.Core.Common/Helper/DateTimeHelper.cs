namespace EU.Core.Common.Helper;

/// <summary>
/// 时间帮助类
/// </summary>
public static class DateTimeHelper
{
    #region 获取日期是 今天or 明天 or 后天
    /// <summary>
    /// 获取日期是 今天or 明天 or 后天
    /// </summary>
    /// <param name="date">日期</param>
    /// <returns></returns>
    public static string FriendlyDate(DateTime? date)
    {
        if (!date.HasValue) return string.Empty;

        string strDate = date.Value.ToString("yyyy-MM-dd");
        string vDate = string.Empty;
        if (DateTime.Now.ToString("yyyy-MM-dd") == strDate)
        {
            vDate = "今天";
        }
        else if (DateTime.Now.AddDays(1).ToString("yyyy-MM-dd") == strDate)
        {
            vDate = "明天";
        }
        else if (DateTime.Now.AddDays(2).ToString("yyyy-MM-dd") == strDate)
        {
            vDate = "后天";
        }
        else if (DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd") == strDate)
        {
            vDate = "昨天";
        }
        else if (DateTime.Now.AddDays(2).ToString("yyyy-MM-dd") == strDate)
        {
            vDate = "前天";
        }
        else
        {
            vDate = strDate;
        }

        return vDate;
    }
    #endregion

    #region 格式化字符串类型，精确到年，如：2008
    /// <summary>
    /// 格式化DateTime类型为字符串类型，精确到年，如：2008
    /// </summary>
    /// <param name="dateTime">时间</param>
    /// <returns></returns>
    public static string ConvertToYearString(DateTime dateTime)
    {
        if (string.IsNullOrEmpty(Convert.ToString(dateTime)))
            return "";

        return dateTime.ToString(@"yyyy");
    }

    /// <summary>
    /// 格式化DateTime类型为字符串类型，精确到年，如：2008
    /// </summary>
    /// <param name="dateTime"></param>
    /// <returns></returns>
    public static string ConvertToYearString(object dateTime)
    {
        if (string.IsNullOrEmpty(Convert.ToString(dateTime)))
            return "";
        return ConvertToYearString((DateTime)dateTime);
    }
    #endregion

    #region 格式化为字符串类型，精确到月，如：2008/01
    /// <summary>
    /// 格式化DateTime类型为字符串类型，精确到月，如：2008/01
    /// </summary>
    /// <param name="dateTime"></param>
    /// <returns></returns>
    public static string ConvertToMonthString(DateTime dateTime)
    {
        if (string.IsNullOrEmpty(Convert.ToString(dateTime)))
            return "";
        return dateTime.ToString(@"yyyy\/MM");
    }

    /// <summary>
    /// 格式化object类型为字符串类型，精确到月，如：2008/01
    /// </summary>
    /// <param name="dateTime"></param>
    /// <returns></returns>
    public static string ConvertToMonthString(object dateTime)
    {
        if (string.IsNullOrEmpty(Convert.ToString(dateTime)))
            return "";
        return ConvertToMonthString((DateTime)dateTime);
    }
    #endregion

    #region 格式化为字符串类型，精确到天，如：2008/01/01
    /// <summary>
    /// 格式化DateTime类型为字符串类型，精确到天，如：2008/01/01
    /// </summary>
    /// <param name="dateTime"></param>
    /// <returns></returns>
    public static string ConvertToDayString(DateTime dateTime)
    {
        if (string.IsNullOrEmpty(Convert.ToString(dateTime)))
            return "";
        return dateTime.ToString(@"yyyy\/MM\/dd");
    }

    /// <summary>
    /// 格式化dateTime类型，精确到天，如：2008/01/01
    /// </summary>
    /// <param name="dateTime"></param>
    /// <returns></returns>
    public static DateTime ConvertToDay(DateTime dateTime)
    {
        string result = ConvertToDayString(dateTime);
        if (string.IsNullOrEmpty(result))
            return DateTime.MinValue;
        else
            return Convert.ToDateTime(result);
    }
    /// <summary>
    /// 格式化object类型为字符串类型，精确到天，如：2008/01/01
    /// </summary>
    /// <param name="dateTime"></param>
    /// <returns></returns>
    public static string ConvertToDayString(object dateTime)
    {
        if (string.IsNullOrEmpty(Convert.ToString(dateTime)))
            return "";

        return ConvertToDayString(Convert.ToDateTime(dateTime));
    }
    #endregion

    #region 格式为字符串类型，精确到小时，如：2008/01/01 18
    /// <summary>
    /// 格式化DateTime类型为字符串类型，精确到小时，如：2008/01/01 18
    /// </summary>
    /// <param name="dateTime"></param>
    /// <returns></returns>
    public static string ConvertToHourString(DateTime dateTime)
    {
        if (string.IsNullOrEmpty(Convert.ToString(dateTime)))
            return "";
        return dateTime.ToString(@"yyyy\/MM\/dd HH");
    }
    /// <summary>
    /// 格式化object类型为字符串类型，精确到小时，如：2008/01/01 18
    /// </summary>
    /// <param name="dateTime"></param>
    /// <returns></returns>
    public static string ConvertToHourString(object dateTime)
    {
        if (string.IsNullOrEmpty(Convert.ToString(dateTime)))
            return "";
        return ConvertToHourString((DateTime)dateTime);
    }
    #endregion

    #region 格式化为字符串类型，精确到分钟，如：2008/01/01 18:09
    /// <summary>
    /// 格式化DateTime类型为字符串类型，精确到分钟，如：2008/01/01 18:09
    /// </summary>
    /// <param name="dateTime"></param>
    /// <returns></returns>
    public static string ConvertToMiniuteString(DateTime dateTime)
    {
        if (string.IsNullOrEmpty(Convert.ToString(dateTime)))
            return "";

        return dateTime.ToString(@"yyyy\/MM\/dd HH:mm");
    }

    /// <summary>
    /// 格式化object类型为字符串类型，精确到分钟，如：2008/01/01 18:09
    /// </summary>
    /// <param name="dateTime"></param>
    /// <returns></returns>
    public static string ConvertToMiniuteString(object dateTime)
    {
        if (string.IsNullOrEmpty(Convert.ToString(dateTime)))
            return "";

        return ConvertToMiniuteString(Convert.ToDateTime(dateTime));
    }
    #endregion

    #region 格式化为字符串类型，精确到秒，如：2008/01/01 18:09:20
    /// <summary>
    /// 格式化DateTime类型为字符串类型，精确到秒，如：2008/01/01 18:09:20
    /// </summary>
    /// <param name="dateTime"></param>
    /// <returns></returns>
    public static string ConvertToSecondString(DateTime dateTime)
    {
        if (string.IsNullOrEmpty(Convert.ToString(dateTime)))
            return "";

        return dateTime.ToString(@"yyyy\/MM\/dd HH:mm:ss");
    }

    /// <summary>
    /// 格式化DateTime类型为字符串类型，精确到秒，如：20080101180920
    /// </summary>
    /// <param name="dateTime"></param>
    /// <returns></returns>
    public static string ConvertToSecondString1(DateTime dateTime)
    {
        if (string.IsNullOrEmpty(Convert.ToString(dateTime)))
        {
            return "";
        }
        return dateTime.ToString(@"yyyyMMddHHmmss");
    }
    /// <summary>
    /// 格式化object类型为字符串类型，精确到秒，如：2008/01/01 18:09:20
    /// </summary>
    /// <param name="dateTime"></param>
    /// <returns></returns>
    public static string ConvertToSecondString(object dateTime)
    {
        if (string.IsNullOrEmpty(Convert.ToString(dateTime)))
            return "";

        return ConvertToSecondString(Convert.ToDateTime(dateTime));
    }
    #endregion

    #region 格式化为字符串类型，精确到日天，如：01/01
    /// <summary>
    /// 格式化DateTime类型为字符串类型，如：01/01
    /// </summary>
    /// <param name="dateTime"></param>
    /// <returns></returns>
    public static string ConvertToOnlyMonthDayString(DateTime dateTime)
    {
        if (string.IsNullOrEmpty(Convert.ToString(dateTime)))
            return "";

        return dateTime.ToString(@"MM\/dd");
    }
    /// <summary>
    /// 格式化object类型为字符串类型，如：01/01
    /// </summary>
    /// <param name="dateTime"></param>
    /// <returns></returns>
    public static string ConvertToOnlyMonthDayString(object dateTime)
    {
        if (string.IsNullOrEmpty(Convert.ToString(dateTime)))
            return "";

        return ConvertToOnlyMonthDayString(Convert.ToDateTime(dateTime));
    }
    #endregion

    #region 格式化为字符串类型，精确到时分，如：12:12

    /// <summary>
    /// 格式化DateTime类型为字符串类型，如：12:12
    /// </summary>
    /// <param name="dateTime"></param>
    /// <returns></returns>
    public static string ConvertToOnlyHourMinuteString(DateTime dateTime)
    {
        if (string.IsNullOrEmpty(Convert.ToString(dateTime)))
            return "";

        return dateTime.ToString(@"HH:mm");
    }
    /// <summary>
    /// 格式化object类型为字符串类型，如：12:12
    /// </summary>
    /// <param name="dateTime"></param>
    /// <returns></returns>
    public static string ConvertToOnlyHourMinuteString(object dateTime)
    {
        if (string.IsNullOrEmpty(Convert.ToString(dateTime)))
            return "";

        return ConvertToOnlyHourMinuteString(Convert.ToDateTime(dateTime));
    }
    #endregion

    #region 格式化为字符串类型，精确到时分秒，如：12:12:12
    /// <summary>
    /// 格式化DateTime类型为字符串类型，如：12:12:12
    /// </summary>
    /// <param name="dateTime"></param>
    /// <returns></returns>
    public static string ConvertToOnlySecondString(DateTime dateTime)
    {
        if (string.IsNullOrEmpty(Convert.ToString(dateTime)))
            return "";

        return dateTime.ToString(@"HH:mm:ss");
    }
    /// <summary>
    /// 格式化object类型为字符串类型，如：12:12:12
    /// </summary>
    /// <param name="dateTime"></param>
    /// <returns></returns>
    public static string ConvertToOnlySecondString(object dateTime)
    {
        if (string.IsNullOrEmpty(Convert.ToString(dateTime)))
            return "";

        return ConvertToOnlySecondString(Convert.ToDateTime(dateTime));
    }
    #endregion

    #region 格式化为字符串类型，精确到年月，如：12:12:12
    /// <summary>
    /// 格式化DateTime类型为字符串类型，如：2020/05
    /// </summary>
    /// <param name="dateTime"></param>
    /// <returns></returns>
    public static string ConvertToYearMonthString(DateTime dateTime)
    {
        if (string.IsNullOrEmpty(Convert.ToString(dateTime)))
            return "";

        return dateTime.ToString(@"yyyy\/MM");
    }
    /// <summary>
    /// 格式化object类型为字符串类型，如：2020/05
    /// </summary>
    /// <param name="dateTime"></param>
    /// <returns></returns>
    public static string ConvertToYearMonthString(object dateTime)
    {
        if (string.IsNullOrEmpty(Convert.ToString(dateTime)))
            return "";

        return ConvertToYearMonthString(Convert.ToDateTime(dateTime));
    }

    /// <summary>
    /// 格式化DateTime类型为字符串类型，如：2020-05
    /// </summary>
    /// <param name="dateTime"></param>
    /// <returns></returns>
    public static string ConvertToYearMonthString1(DateTime dateTime)
    {
        if (string.IsNullOrEmpty(Convert.ToString(dateTime)))
            return "";

        return dateTime.ToString(@"yyyy-MM");
    }
    /// <summary>
    /// 格式化object类型为字符串类型，如：2020-05
    /// </summary>
    /// <param name="dateTime"></param>
    /// <returns></returns>
    public static string ConvertToYearMonthString1(object dateTime)
    {
        if (string.IsNullOrEmpty(Convert.ToString(dateTime)))
            return "";

        return ConvertToYearMonthString1(Convert.ToDateTime(dateTime));
    }
    #endregion

    #region 毫秒转天时分秒
    /// <summary>
    /// 毫秒转天时分秒
    /// </summary>
    /// <param name="ms"></param>
    /// <returns></returns>
    public static string FormatTime(long ms)
    {
        int ss = 1000;
        int mi = ss * 60;
        int hh = mi * 60;
        int dd = hh * 24;

        long day = ms / dd;
        long hour = (ms - day * dd) / hh;
        long minute = (ms - day * dd - hour * hh) / mi;
        long second = (ms - day * dd - hour * hh - minute * mi) / ss;
        long milliSecond = ms - day * dd - hour * hh - minute * mi - second * ss;

        string sDay = day < 10 ? "0" + day : "" + day; //天
        string sHour = hour < 10 ? "0" + hour : "" + hour;//小时
        string sMinute = minute < 10 ? "0" + minute : "" + minute;//分钟
        string sSecond = second < 10 ? "0" + second : "" + second;//秒
        string sMilliSecond = milliSecond < 10 ? "0" + milliSecond : "" + milliSecond;//毫秒
        sMilliSecond = milliSecond < 100 ? "0" + sMilliSecond : "" + sMilliSecond;

        return string.Format("{0} 天 {1} 小时 {2} 分 {3} 秒", sDay, sHour, sMinute, sSecond);
    }
    #endregion

    #region 获取系统当前时间（字符串）
    /// <summary>
    /// 获取系统当前时间（字符串）
    /// </summary>
    /// <returns></returns>
    public static string GetSysDateTimeString()
    {
        return ConvertToSecondString(Utility.GetSysDate());
    }
    #endregion

    #region 时间戳转时间
    /// <summary>
    /// 时间戳转时间
    /// </summary>
    /// <param name="time"></param>
    /// <returns></returns>
    public static DateTime StampToDateTime(string time)
    {
        time = time.Substring(0, 10);
        double timestamp = Convert.ToInt64(time);
        var dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);
        dateTime = dateTime.AddSeconds(timestamp).ToLocalTime();
        return dateTime;
    }
    #endregion

    #region 算时间差，格式xx天xx时xx分
    /// <summary>
    /// 算时间差，格式xx天xx时xx分
    /// </summary>
    /// <param name="time1"></param>
    /// <param name="time2"></param>
    /// <returns></returns>
    public static string TimeSubTract(DateTime time1, DateTime time2)
    {
        var subTract = time1.Subtract(time2);
        return $"{subTract.Days} 天 {subTract.Hours} 时 {subTract.Minutes} 分 ";
    }
    #endregion

    #region 时间戳转本地时间-时间戳精确到秒
    /// <summary>
    ///  时间戳转本地时间-时间戳精确到秒
    /// </summary> 
    public static DateTime ToLocalTimeDateBySeconds(long unix)
    {
        var dto = DateTimeOffset.FromUnixTimeSeconds(unix);
        return dto.ToLocalTime().DateTime;
    }

    #endregion

    #region 时间转时间戳Unix-时间戳精确到秒
    /// <summary>
    ///  时间转时间戳Unix-时间戳精确到秒
    /// </summary> 
    public static long ToUnixTimestampBySeconds(DateTime dt)
    {
        var dto = new DateTimeOffset(dt);
        return dto.ToUnixTimeSeconds();
    }

    #endregion

    #region 时间戳转本地时间-时间戳精确到毫秒
    /// <summary>
    ///  时间戳转本地时间-时间戳精确到毫秒
    /// </summary> 
    public static DateTime ToLocalTimeDateByMilliseconds(long unix)
    {
        var dto = DateTimeOffset.FromUnixTimeMilliseconds(unix);
        return dto.ToLocalTime().DateTime;
    }

    #endregion

    #region 时间转时间戳Unix
    /// <summary>
    ///  时间转时间戳Unix-时间戳精确到毫秒
    /// </summary> 
    public static long ToUnixTimestampByMilliseconds(DateTime dt)
    {
        var dto = new DateTimeOffset(dt);
        return dto.ToUnixTimeMilliseconds();
    }
    #endregion

    #region 返回当前日期的星期名称
    /// <summary>返回当前日期的星期名称</summary>
    /// <param name="idt">日期</param>
    /// <returns>星期名称</returns>
    public static string GetWeekNameOfDay(this in DateTime idt)
    {
        return idt.DayOfWeek switch
        {
            DayOfWeek.Monday => "星期一",
            DayOfWeek.Tuesday => "星期二",
            DayOfWeek.Wednesday => "星期三",
            DayOfWeek.Thursday => "星期四",
            DayOfWeek.Friday => "星期五",
            DayOfWeek.Saturday => "星期六",
            DayOfWeek.Sunday => "星期日",
            _ => ""
        };
    }
    #endregion
}
