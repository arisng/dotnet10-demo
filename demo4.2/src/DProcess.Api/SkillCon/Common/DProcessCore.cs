namespace DProcess.Api.SkillCon.Common;
public static class DProcessCore
{
    /// <summary>
    /// This class contains global definitions used by DProcess software
    /// </summary>
    public class Defines
    {
        public static readonly DateTime MaxDate = DateTime.MaxValue.Date;
        public static readonly DateTime EmptyDate = new DateTime(1753, 1, 1); // SQL Server minimum
        public static readonly string RegistryRoot = @"Software\D-Process";

        // Fixed-values Guids
        public static readonly Guid RootTrainingGuid = new Guid("5a23ee6f-adaf-48f6-b248-07ff21169bc4");
        public static readonly Guid LafiteGUID = new Guid("24F9EB48-FF51-4FC5-8ACF-C1B864B2781B");
        public static readonly string DefaultSysUserName = "sysuser";

        // Common default value for fields that are non-nullable but not set (e.g. ID of Organisation)
        public static readonly string DefaultFieldValueDash = "-";

        /// <summary>
        /// Determines if the date is an empty date.
        /// </summary>
        /// <param name="theDate">The date.</param>
        /// <returns>True if theDate is a empty date.</returns>
        public static bool IsEmptyDate(DateTime theDate)
        {
            return theDate.Date.Equals(EmptyDate.Date);
        }

        /// <summary>
        /// Determines if dateTime is a maximum date.
        /// </summary>
        /// <param name="dateTime">The date.</param>
        /// <returns>True if theDate is a maximum date.</returns>
        public static bool IsMaxDate(DateTime dateTime)
        {
            if (dateTime.Date.Equals(Defines.MaxDate.Date))
                return true;

            return false;
        }

        /// <summary>
        /// Determines if dateTime is Valid. (In the range of EmptyDate and MaxDate).
        /// </summary>
        /// <param name="dateTime">The date.</param>
        /// <returns>True if the date is valid.</returns>
        public static bool IsValidDate(DateTime dateTime)
        {
            if ((dateTime < Defines.EmptyDate) || (dateTime.Date > Defines.MaxDate))
            {
                return false;
            }
            return true;
        }
    }

    public class DateTimeHelper
    {
        /// <summary>
        /// Calculate the difference in months between 2 dates
        /// </summary>
        /// <param name="startDate">The first date</param>
        /// <param name="endDate">The second date</param>
        /// <returns>The number of months between first date and second date</returns>
        public int DiffMonths(DateTime startDate, DateTime endDate)
        {
            int monthsApart = 12 * (startDate.Year - endDate.Year) + startDate.Month - endDate.Month;
            return Math.Abs(monthsApart);
        }

        /// <summary>
        /// Displays datetime in format  dd.MM.yyyy HH:mm:ss
        /// </summary>
        /// <param name="datetime">DateTime to display</param>
        public static string ToExactShortDateTimeString(DateTime? datetime)
        {
            if (!datetime.HasValue)
                return string.Empty;

            return ToExactShortDateTimeString(datetime.Value);
        }

        /// <summary>
        /// Convert nullable DateTime? to DateTime.
        /// </summary>
        /// <param name="datetime">nullable DateTime?</param>
        /// <returns>DateTime value or EmptyDate.</returns>
        public static DateTime ToDateTime(DateTime? datetime)
        {
            if (datetime.HasValue)
                return datetime.Value;
            return Defines.EmptyDate;
        }
    }
}
