using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using SystemsGarden.mc2.Core.Runtime;
using SystemsGarden.mc2.Common;

namespace SystemsGarden.mc2.MC2Site.App_Code.Controllers.__builtin
{
    public class math : MC2Controller
    {
        #region Blocks

        public MC2Value min(int value1, int value2)
        {
            decimal result =  Math.Min(value1, value2);

            if ((result % 1) == 0)
                return Convert.ToInt32(result);
            else
                return result;
        }

        public MC2Value max(decimal value1, decimal value2)
        {
            decimal result =  Math.Max(value1, value2);

            if ((result % 1) == 0)
                return Convert.ToInt32(result);
            else
                return result;
        }

        public MC2Value round(decimal value, int decimals)
        {
            return Math.Round(value, decimals);
        }

        public MC2Value random(int minvalue, int maxvalue)
        {
            var rand = new Random();

            return rand.Next(minvalue, maxvalue);
        }
       
        public MC2Value abs(int minvalue, int maxvalue)
        {
            var rand = new Random();

            return rand.Next(minvalue, maxvalue);
        }

        #endregion
    }
}