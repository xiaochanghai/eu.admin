namespace EU.Core.Common.Helper;

/// <summary>
/// 日期时间帮助类
/// 提供各种日期时间格式化、转换和计算功能
/// </summary>
public static class DateTimeHelper
{
    #region 获取日期是 今天or 明天 or 后天
    /// <summary>
    /// 获取友好的日期显示字符串（今天、明天、后天、昨天、前天或具体日期）
    /// </summary>
    /// <param name="date">要转换的日期</param>
    /// <returns>返回友好的日期字符串</returns>
    public static string FriendlyDate(this DateTime? date)
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
    /// <param name="dateTime">要格式化的日期时间</param>
    /// <returns>返回年份字符串（格式：yyyy）</returns>
    public static string ConvertToYearString(this DateTime dateTime)
    {
        if (string.IsNullOrEmpty(Convert.ToString(dateTime)))
            return "";

        return dateTime.ToString(@"yyyy");
    }

    /// <summary>
    /// 格式化object类型为字符串类型，精确到年，如：2008
    /// </summary>
    /// <param name="dateTime">要格式化的日期时间对象</param>
    /// <returns>返回年份字符串（格式：yyyy）</returns>
    public static string ConvertToYearString(this object dateTime)
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
    /// <param name="dateTime">要格式化的日期时间</param>
    /// <returns>返回年月字符串（格式：yyyy/MM）</returns>
    public static string ConvertToMonthString(this DateTime dateTime)
    {
        if (string.IsNullOrEmpty(Convert.ToString(dateTime)))
            return "";
        return dateTime.ToString(@"yyyy\/MM");
    }

    /// <summary>
    /// 格式化object类型为字符串类型，精确到月，如：2008/01
    /// </summary>
    /// <param name="dateTime">要格式化的日期时间对象</param>
    /// <returns>返回年月字符串（格式：yyyy/MM）</returns>
    public static string ConvertToMonthString(this object dateTime)
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
    /// <param name="dateTime">要格式化的日期时间</param>
    /// <returns>返回年月日字符串（格式：yyyy/MM/dd）</returns>
    public static string ConvertToDayString(this DateTime dateTime)
    {
        if (string.IsNullOrEmpty(Convert.ToString(dateTime)))
            return "";
        return dateTime.ToString(@"yyyy\/MM\/dd");
    }

    /// <summary>
    /// 格式化DateTime类型，精确到天（去除时分秒），如：2008/01/01
    /// </summary>
    /// <param name="dateTime">要格式化的日期时间</param>
    /// <returns>返回DateTime类型（精确到天）</returns>
    public static DateTime ConvertToDay(this DateTime dateTime)
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
    /// <param name="dateTime">要格式化的日期时间对象</param>
    /// <returns>返回年月日字符串（格式：yyyy/MM/dd）</returns>
    public static string ConvertToDayString(this object dateTime)
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
    /// <param name="dateTime">要格式化的日期时间</param>
    /// <returns>返回年月日小时字符串（格式：yyyy/MM/dd HH）</returns>
    public static string ConvertToHourString(this DateTime dateTime)
    {
        if (string.IsNullOrEmpty(Convert.ToString(dateTime)))
            return "";
        return dateTime.ToString(@"yyyy\/MM\/dd HH");
    }
    /// <summary>
    /// 格式化object类型为字符串类型，精确到小时，如：2008/01/01 18
    /// </summary>
    /// <param name="dateTime">要格式化的日期时间对象</param>
    /// <returns>返回年月日小时字符串（格式：yyyy/MM/dd HH）</returns>
    public static string ConvertToHourString(this object dateTime)
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
    /// <param name="dateTime">要格式化的日期时间</param>
    /// <returns>返回年月日时分字符串（格式：yyyy/MM/dd HH:mm）</returns>
    public static string ConvertToMiniuteString(this DateTime dateTime)
    {
        if (string.IsNullOrEmpty(Convert.ToString(dateTime)))
            return "";

        return dateTime.ToString(@"yyyy\/MM\/dd HH:mm");
    }

    /// <summary>
    /// 格式化object类型为字符串类型，精确到分钟，如：2008/01/01 18:09
    /// </summary>
    /// <param name="dateTime">要格式化的日期时间对象</param>
    /// <returns>返回年月日时分字符串（格式：yyyy/MM/dd HH:mm）</returns>
    public static string ConvertToMiniuteString(this object dateTime)
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
    /// <param name="dateTime">要格式化的日期时间</param>
    /// <returns>返回年月日时分秒字符串（格式：yyyy/MM/dd HH:mm:ss）</returns>
    public static string ConvertToSecondString(this DateTime dateTime)
    {
        if (string.IsNullOrEmpty(Convert.ToString(dateTime)))
            return "";

        return dateTime.ToString(@"yyyy\/MM\/dd HH:mm:ss");
    }

    /// <summary>
    /// 格式化DateTime类型为字符串类型，精确到秒（无分隔符），如：20080101180920
    /// </summary>
    /// <param name="dateTime">要格式化的日期时间</param>
    /// <returns>返回年月日时分秒字符串（格式：yyyyMMddHHmmss）</returns>
    public static string ToSecondString1(this DateTime dateTime)
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
    /// <param name="dateTime">要格式化的日期时间对象</param>
    /// <returns>返回年月日时分秒字符串（格式：yyyy/MM/dd HH:mm:ss）</returns>
    public static string ConvertToSecondString(this object dateTime)
    {
        if (string.IsNullOrEmpty(Convert.ToString(dateTime)))
            return "";

        return ConvertToSecondString(Convert.ToDateTime(dateTime));
    }
    #endregion

    #region 格式化为字符串类型，精确到日天，如：01/01
    /// <summary>
    /// 格式化DateTime类型为字符串类型（仅月日），如：01/01
    /// </summary>
    /// <param name="dateTime">要格式化的日期时间</param>
    /// <returns>返回月日字符串（格式：MM/dd）</returns>
    public static string ConvertToOnlyMonthDayString(this DateTime dateTime)
    {
        if (string.IsNullOrEmpty(Convert.ToString(dateTime)))
            return "";

        return dateTime.ToString(@"MM\/dd");
    }
    /// <summary>
    /// 格式化object类型为字符串类型（仅月日），如：01/01
    /// </summary>
    /// <param name="dateTime">要格式化的日期时间对象</param>
    /// <returns>返回月日字符串（格式：MM/dd）</returns>
    public static string ConvertToOnlyMonthDayString(this object dateTime)
    {
        if (string.IsNullOrEmpty(Convert.ToString(dateTime)))
            return "";

        return ConvertToOnlyMonthDayString(Convert.ToDateTime(dateTime));
    }
    #endregion

    #region 格式化为字符串类型，精确到时分，如：12:12

    /// <summary>
    /// 格式化DateTime类型为字符串类型（仅时分），如：12:12
    /// </summary>
    /// <param name="dateTime">要格式化的日期时间</param>
    /// <returns>返回时分字符串（格式：HH:mm）</returns>
    public static string ConvertToOnlyHourMinuteString(this DateTime dateTime)
    {
        if (string.IsNullOrEmpty(Convert.ToString(dateTime)))
            return "";

        return dateTime.ToString(@"HH:mm");
    }
    /// <summary>
    /// 格式化object类型为字符串类型（仅时分），如：12:12
    /// </summary>
    /// <param name="dateTime">要格式化的日期时间对象</param>
    /// <returns>返回时分字符串（格式：HH:mm）</returns>
    public static string ConvertToOnlyHourMinuteString(this object dateTime)
    {
        if (string.IsNullOrEmpty(Convert.ToString(dateTime)))
            return "";

        return ConvertToOnlyHourMinuteString(Convert.ToDateTime(dateTime));
    }
    #endregion

    #region 格式化为字符串类型，精确到时分秒，如：12:12:12
    /// <summary>
    /// 格式化DateTime类型为字符串类型（仅时分秒），如：12:12:12
    /// </summary>
    /// <param name="dateTime">要格式化的日期时间</param>
    /// <returns>返回时分秒字符串（格式：HH:mm:ss）</returns>
    public static string ConvertToOnlySecondString(this DateTime dateTime)
    {
        if (string.IsNullOrEmpty(Convert.ToString(dateTime)))
            return "";

        return dateTime.ToString(@"HH:mm:ss");
    }
    /// <summary>
    /// 格式化object类型为字符串类型（仅时分秒），如：12:12:12
    /// </summary>
    /// <param name="dateTime">要格式化的日期时间对象</param>
    /// <returns>返回时分秒字符串（格式：HH:mm:ss）</returns>
    public static string ConvertToOnlySecondString(this object dateTime)
    {
        if (string.IsNullOrEmpty(Convert.ToString(dateTime)))
            return "";

        return ConvertToOnlySecondString(Convert.ToDateTime(dateTime));
    }
    #endregion

    #region 格式化为字符串类型，精确到年月，如：12:12:12
    /// <summary>
    /// 格式化DateTime类型为字符串类型，精确到年月，如：2020/05
    /// </summary>
    /// <param name="dateTime">要格式化的日期时间</param>
    /// <returns>返回年月字符串（格式：yyyy/MM）</returns>
    public static string ConvertToYearMonthString(this DateTime dateTime)
    {
        if (string.IsNullOrEmpty(Convert.ToString(dateTime)))
            return "";

        return dateTime.ToString(@"yyyy\/MM");
    }
    /// <summary>
    /// 格式化object类型为字符串类型，精确到年月，如：2020/05
    /// </summary>
    /// <param name="dateTime">要格式化的日期时间对象</param>
    /// <returns>返回年月字符串（格式：yyyy/MM）</returns>
    public static string ConvertToYearMonthString(this object dateTime)
    {
        if (string.IsNullOrEmpty(Convert.ToString(dateTime)))
            return "";

        return ConvertToYearMonthString(Convert.ToDateTime(dateTime));
    }

    /// <summary>
    /// 格式化DateTime类型为字符串类型，精确到年月（使用横线分隔），如：2020-05
    /// </summary>
    /// <param name="dateTime">要格式化的日期时间</param>
    /// <returns>返回年月字符串（格式：yyyy-MM）</returns>
    public static string ConvertToYearMonthString1(this DateTime dateTime)
    {
        if (string.IsNullOrEmpty(Convert.ToString(dateTime)))
            return "";

        return dateTime.ToString(@"yyyy-MM");
    }
    /// <summary>
    /// 格式化object类型为字符串类型，精确到年月（使用横线分隔），如：2020-05
    /// </summary>
    /// <param name="dateTime">要格式化的日期时间对象</param>
    /// <returns>返回年月字符串（格式：yyyy-MM）</returns>
    public static string ConvertToYearMonthString1(this object dateTime)
    {
        if (string.IsNullOrEmpty(Convert.ToString(dateTime)))
            return "";

        return ConvertToYearMonthString1(Convert.ToDateTime(dateTime));
    }
    #endregion

    #region 毫秒转天时分秒
    /// <summary>
    /// 将毫秒数转换为天时分秒格式的字符串
    /// </summary>
    /// <param name="ms">毫秒数</param>
    /// <returns>返回格式化的时间字符串（格式：XX 天 XX 小时 XX 分 XX 秒）</returns>
    public static string FormatTime(this long ms)
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
    /// 获取系统当前时间的字符串表示
    /// </summary>
    /// <returns>返回当前系统时间字符串（格式：yyyy/MM/dd HH:mm:ss）</returns>
    public static string GetSysDateTimeString()
    {
        return ConvertToSecondString(Utility.GetSysDate());
    }
    #endregion

    #region 时间戳转时间
    /// <summary>
    /// 将Unix时间戳字符串转换为DateTime对象
    /// </summary>
    /// <param name="time">Unix时间戳字符串（秒级，至少10位）</param>
    /// <returns>返回对应的本地时间</returns>
    public static DateTime StampToDateTime(this string time)
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
    /// 计算两个时间的差值，并格式化为字符串
    /// </summary>
    /// <param name="time1">时间1（被减数）</param>
    /// <param name="time2">时间2（减数）</param>
    /// <returns>返回时间差字符串（格式：XX 天 XX 时 XX 分）</returns>
    public static string TimeSubTract(DateTime time1, DateTime time2)
    {
        var subTract = time1.Subtract(time2);
        return $"{subTract.Days} 天 {subTract.Hours} 时 {subTract.Minutes} 分 ";
    }
    #endregion

    #region 时间戳转本地时间-时间戳精确到秒
    /// <summary>
    /// 将Unix时间戳（秒）转换为本地时间
    /// </summary>
    /// <param name="unix">Unix时间戳（秒级）</param>
    /// <returns>返回对应的本地DateTime</returns>
    public static DateTime ToLocalTimeDateBySeconds(this long unix)
    {
        var dto = DateTimeOffset.FromUnixTimeSeconds(unix);
        return dto.ToLocalTime().DateTime;
    }

    #endregion

    #region 时间转时间戳Unix-时间戳精确到秒
    /// <summary>
    /// 将DateTime转换为Unix时间戳（秒）
    /// </summary>
    /// <param name="dt">要转换的DateTime对象</param>
    /// <returns>返回Unix时间戳（秒级）</returns>
    public static long ToUnixTimestampBySeconds(this DateTime dt)
    {
        var dto = new DateTimeOffset(dt);
        return dto.ToUnixTimeSeconds();
    }

    #endregion

    #region 时间戳转本地时间-时间戳精确到毫秒
    /// <summary>
    /// 将Unix时间戳（毫秒）转换为本地时间
    /// </summary>
    /// <param name="unix">Unix时间戳（毫秒级）</param>
    /// <returns>返回对应的本地DateTime</returns>
    public static DateTime ToLocalTimeDateByMilliseconds(this long unix)
    {
        var dto = DateTimeOffset.FromUnixTimeMilliseconds(unix);
        return dto.ToLocalTime().DateTime;
    }

    #endregion

    #region 时间转时间戳Unix
    /// <summary>
    /// 将DateTime转换为Unix时间戳（毫秒）
    /// </summary>
    /// <param name="dt">要转换的DateTime对象</param>
    /// <returns>返回Unix时间戳（毫秒级）</returns>
    public static long ToUnixTimestampByMilliseconds(this DateTime dt)
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

    #region UTC时间按时区转换
    /// <summary>
    /// 将UTC时间转换为指定时区时间。
    /// </summary>
    /// <param name="utcTime">UTC时间</param>
    /// <param name="timeZoneId">
    /// 时区ID；为空时默认使用中国标准时区：
    /// Windows 为 China Standard Time，Linux/macOS 为 Asia/Shanghai。
    /// </param>
    /// <returns>转换后的时区时间及其UTC偏移量</returns>
    public static DateTime ConvertUtcToTimeZone(
        this DateTimeOffset utcTime,
        string timeZoneId = null)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            timeZoneId = OperatingSystem.IsWindows()
                ? "China Standard Time"
                : "Asia/Shanghai";
        }

        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        return TimeZoneInfo.ConvertTime(utcTime, timeZone).DateTime;
    }
    #endregion
}
