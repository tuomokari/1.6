using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using SystemsGarden.mc2.Core.Runtime;
using SystemsGarden.mc2.Common;

namespace SystemsGarden.mc2.MC2Site.App_Code.Controllers.__builtin
{
    public class convert : MC2Controller
    {
        #region Blocks

        public MC2Value tostring(MC2Value value)
        {
            return Runtime.ControllerManager.MC2ValueToString(value);
        }

        public MC2Value toint(MC2Value value)
        {
            if (value == null)
                return 0;

            // If the value is a datatree then unwrap it get to it's value.
            if (value is MC2DataTreeValue)
                value = ((MC2DataTreeValue)value).DataTreeValue.Value;

            if (value is MC2IntValue)
                return value;

            if (value is MC2DecimalValue)
                return value;

            if (value is MC2BoolValue)
                return (((MC2BoolValue)value).BoolValue)? 1 : 0;

            if (value is MC2StringValue)
            {
                try
                {
                    return Convert.ToInt32((string)value);
                }
                catch(Exception) // Todo: replace with more accurate exception.
                {
                    // No need to throw. Exception will bo thrown at the end of the method.
                }
            }

            throw new RuntimeException("Failed to convert value to integer: " + value);

        }

        #endregion
    }
}