using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Threading.Tasks;
using ManyWho.Flow.SDK.Utils;
using ManyWho.Flow.SDK.Security;

namespace ManyWho.Service.Salesforce.Utils
{
    public class DateUtils
    {
        /// <summary>
        /// A utility method to make sure the date being assigned is in fact a work day and assign the provided time.
        /// </summary>
        public static DateTime GetDayInWeek(DateTime dateTime, Int32 hour)
        {
            if (dateTime.DayOfWeek == DayOfWeek.Saturday)
            {
                dateTime.AddDays(2);
            }
            else if (dateTime.DayOfWeek == DayOfWeek.Sunday)
            {
                dateTime.AddDays(1);
            }

            // Round the date to the hour
            return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, hour, 0, 0);
        }

        public static DateTime CreateDateFromWhenCommand(INotifier notifier, IAuthenticatedWho authenticatedWho, String when, String adminEmail)
        {
            DateTime whenDate = DateTime.Now;
            String[] whenConfig = null;
            Int32 days = 0;

            // Supported when commands are "n days" or "n day" and "now"
            if (when.IndexOf("day", StringComparison.InvariantCultureIgnoreCase) > 0)
            {
                // Split the when by the space
                whenConfig = when.Split(' ');

                // Check to make sure the split was OK
                if (whenConfig.Length > 0)
                {
                    // Check to make sure the first parameter in the split is the number of days
                    if (Int32.TryParse(whenConfig[0], out days) == false)
                    {
                        // send the author an error message
                    }

                    // Add the days to our task
                    whenDate = whenDate.AddDays(days);
                }
            }
            else if (when.Trim().Equals("now", StringComparison.InvariantCultureIgnoreCase) == true)
            {
                // Use the date as of right now
                whenDate = DateTime.Now;
            }
            else
            {
                // throw and error as this is not a supported command
                String errorMessage = "The provided 'when' command is not valid: " + when;

                ErrorUtils.SendAlert(notifier, authenticatedWho, ErrorUtils.ALERT_TYPE_FAULT, errorMessage);

                throw new ArgumentNullException("SalesforcePlugin", errorMessage);
            }

            return whenDate;
        }
    }
}
