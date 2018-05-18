using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading.Tasks;
using SystemsGarden.mc2.Common;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace SystemsGarden.mc2.Tro.Logic
{
    public class TimesheetUtils
    {
        IRuntime runtime;

        HashSet<string> payTypesThatCountAsRegulsrHours = null;
        object payTypesThatCountAsRegulsrHoursLock = new object();

        public TimesheetUtils(IRuntime runtime)
        {
            this.runtime = runtime;
        }

        /// <summary>
        /// Retrun pay types that cout towards hour totals. Usually these include normal working hours, 
        /// overtime etc. but not any extra compensations logged for these same hours.
        /// </summary>
        /// <returns></returns>
        public async Task<HashSet<string>> GetPayTypesThatCountAsRegularHours()
        {
            lock (payTypesThatCountAsRegulsrHoursLock)
            {

                if (payTypesThatCountAsRegulsrHours != null)
                    return payTypesThatCountAsRegulsrHours;
            }
           
            var query = new DBQuery( "tro", "logic_paytypesthatcountasregularhours");
            DBResponse result = await query.FindAsync().ConfigureAwait(false);

            lock (payTypesThatCountAsRegulsrHoursLock)
            {
                payTypesThatCountAsRegulsrHours = new HashSet<string>();

                foreach (DBDocument payType in result.FirstCollection)
                {
                    payTypesThatCountAsRegulsrHours.Add(payType[DBQuery.Id]);
                }
            }

            return payTypesThatCountAsRegulsrHours;
        }
    }
}