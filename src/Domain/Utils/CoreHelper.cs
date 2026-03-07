using sp26se058_3dprintshop_be.Domain.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sp26se058_3dprintshop_be.Domain.Utils
{
    public static class CoreHelper
    {
        private static readonly TimeZoneInfo _hcmTimeZone = GetHcmTimeZone();
        private static TimeZoneInfo GetHcmTimeZone()
        {
            // Thử format của Windows trước, nếu lỗi thì thử format của Linux
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
            }
        }
        public static DateTimeOffset SystemTimeNow => DateTimeOffset.UtcNow;

        // Chuyển từ Utc sang giờ HCM (trả về DateTime)
        public static DateTime UtcToSystemTime(this DateTime dateTimeUtc)
        {
            // Đảm bảo Kind là Utc để tránh lỗi Convert
            var utcKind = DateTime.SpecifyKind(dateTimeUtc, DateTimeKind.Utc);
            return TimeZoneInfo.ConvertTimeFromUtc(utcKind, _hcmTimeZone);
        }
        // Chuyển từ DateTimeOffset sang DateTime HCM
        public static DateTime UtcToSystemTime(this DateTimeOffset dateTimeOffsetUtc)
        {
            return TimeZoneInfo.ConvertTimeFromUtc(dateTimeOffsetUtc.UtcDateTime, _hcmTimeZone);
        }
        // Chuyển sang DateTimeOffset với Offset chính xác của HCM (+7)
        public static DateTimeOffset UtcToOffsetSystemTime(this DateTime dateTimeUtc)
        {
            var localDateTime = UtcToSystemTime(dateTimeUtc);
            return new DateTimeOffset(localDateTime, _hcmTimeZone.GetUtcOffset(localDateTime));
        }

        public static DateTimeOffset UtcToOffsetSystemTime(this DateTimeOffset dateTimeOffsetUtc)
        {
            var localDateTime = UtcToSystemTime(dateTimeOffsetUtc);
            return new DateTimeOffset(localDateTime, _hcmTimeZone.GetUtcOffset(localDateTime));
        }
     


    }
}
