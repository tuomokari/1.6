using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using SystemsGarden.mc2.Core.Runtime;
using SystemsGarden.mc2.Common;

namespace SystemsGarden.mc2.MC2Site.App_Code.Controllers.__builtin
{
    public class programmedfields : MC2Controller
    {
        #region Blocks

        public MC2Value duration(DataTree item, DataTree itemSchema, DataTree propertySchema)
        {
            bool succeeded = true;

            string startField = propertySchema["start"];
            string endField = propertySchema["end"];

            if (string.IsNullOrEmpty(startField) || string.IsNullOrEmpty(endField))
                succeeded = false;

            DateTime start = (DateTime)item[startField].GetValueOrDefault(DateTime.MinValue);
            DateTime end = (DateTime)item[endField].GetValueOrDefault(DateTime.MinValue);

            if (start == DateTime.MinValue || end == DateTime.MinValue)
                succeeded = false;

            
            TimeSpan durationSpan = ((MC2DateTimeValue)end).DateTimeValue - ((MC2DateTimeValue)start).DateTimeValue;

            MC2Value result = MC2EmptyValue.EmptyValue;

            if (succeeded)
            {
                result = Runtime.RunBlock("text", "formattimespan", (int)durationSpan.TotalMilliseconds);
            }

            return result;
        }
        
        #endregion
    }
}