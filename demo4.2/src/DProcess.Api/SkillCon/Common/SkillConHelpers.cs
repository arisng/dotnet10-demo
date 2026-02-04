using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DProcess.Api.SkillCon.Common;

public static class SkillConHelpers
{
    public static DateTime ToCETDateTime(this DateTime utcDateTime)
    {
        var cetTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time");
        return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, cetTimeZone);
    }
}
