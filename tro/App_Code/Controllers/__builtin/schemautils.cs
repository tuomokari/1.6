using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using SystemsGarden.mc2.Core.Runtime;
using SystemsGarden.mc2.Common;

namespace SystemsGarden.mc2.MC2Site.App_Code.Controllers.Builtin
{
    public class schemautils : MC2Controller
    {
        #region Blocks
       
        public MC2Value getdisplaynameforelation(DataTree element, DataTree itemschema, string relationname)
        {
            if (element.Contains("__displayname"))
                return element["__displayname"];

            bool isFirst = true;
            string ret = string.Empty;

            // Get the relation
            DataTree relationSchema = itemschema.Parent.Parent[relationname];

            DataTree relationElement = element;

            if (relationElement.Count == 0)
                return string.Empty;

            foreach (DataTree schemaItem in relationSchema)
            {
                if ((bool)schemaItem["namefield"])
                {
                    if (isFirst)
                        isFirst = false;
                    else if (!ret.EndsWith(" "))
                        ret += " ";

                    ret += (string)relationElement[schemaItem.Name];
                }
            }

            return ret.Trim();
        }

        public MC2Value getnumberofrelationfields(DataTree element, DataTree itemschema)
        {
            int relationCount = 0;

            // Get the relation
            DataTree relationSchema = itemschema;

            DataTree relationElement = element;

            foreach (DataTree schemaItem in relationSchema)
            {
                if ((string)schemaItem["relationtype"] == "many")
                {
                    relationCount++;
                }
            }

            foreach (DataTree externalRelation in relationSchema["collection"]["relation"])
            {
                relationCount++;
            }

            return relationCount;
        }

        public MC2Value getdefaultvalue(DataTree itemschema)
        {
            return Runtime.SchemaValidation.GetDefaultValue(itemschema, DBCallProperties.NoProperties);
        }

        public MC2Value getreadaccessforitem(DataTree item, DataTree itemschema)
        {
            if (itemschema == null)
                itemschema = Runtime.RunBlock("core", "schemafor", item);

            // Check explicit read access
            bool accessread = (bool)itemschema["collection"]["accessread"];

            // Check access through creator or owner
            if (!accessread)
            {
                string userid = Runtime.SessionManager.CurrentUser[DBQuery.Id];

                if ((bool)itemschema["collection"]["accessread"]["owner"])
                {
                    if ((string)item["owner"] == userid)
                        accessread = true;
                }

                if ((bool)itemschema["collection"]["accessread"]["creator"])
                {
                    if ((string)item["creator"] == userid)
                        accessread = true;
                }
            }

            return accessread;
        }

        public MC2Value getmodifyaccessforitem(DataTree item, DataTree itemschema)
        {
            // Immediate return for efficiency
            if ((bool)item["__readonly"])
                return false;

            if (itemschema == null || itemschema.Empty)
                itemschema = Runtime.RunBlock("core", "schemafor", item);

            // Check explicit read access
            bool accessmodify = (bool)itemschema["collection"]["accessmodify"];

            // Check access through creator or owner
            if (!accessmodify)
            {
                string userid = Runtime.SessionManager.CurrentUser[DBQuery.Id];

                if ((bool)itemschema["collection"]["accessmodify"]["owner"])
                {
                    if ((string)item["owner"] == userid)
                        accessmodify = true;
                }

                if ((bool)itemschema["collection"]["accessmodify"]["creator"])
                {
                    if ((string)item["creator"] == userid)
                        accessmodify = true;
                }
            }

            return accessmodify;
        }

        public MC2Value getremoveaccessforitem(DataTree item, DataTree itemschema)
        {
            // Immediate return for efficiency
            if ((bool)item["__readonly"])
                return false;

            if (itemschema == null)
                itemschema = Runtime.RunBlock("core", "schemafor", item);

            // Check explicit read access
            bool accessremove = (bool)itemschema["collection"]["accessremove"];

            // Check access through creator or owner
            if (!accessremove)
            {
                string userid = Runtime.SessionManager.CurrentUser[DBQuery.Id];

                if ((bool)itemschema["collection"]["accessremove"]["owner"])
                {
                    if ((string)item["owner"] == userid)
                        accessremove = true;
                }

                if ((bool)itemschema["collection"]["accessremove"]["creator"])
                {
                    if ((string)item["creator"] == userid)
                        accessremove = true;
                }
            }

            return accessremove;
        }


        #endregion
    }
}