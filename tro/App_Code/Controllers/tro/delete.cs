using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using SystemsGarden.mc2.MC2SiteEnvironment;
using SystemsGarden.mc2.Core.Runtime;
using SystemsGarden.mc2.Core;
using SystemsGarden.mc2.Common.Constants;
using SystemsGarden.mc2.Common;
using MongoDB.Driver.Builders;
using MongoDB.Driver;
using MongoDB.Bson;
using mc2.Controllers;
using SystemsGarden.mc2.Tro.Logic;

namespace SysemsGarden.mc2.tro.App_Code.Controllers.tro
{
    public class delete : MC2Controller
    {
        #region Constants

        private const string DefaultAuthenticationToken = "69007566-8071-4871-9351-25D00BBC1A3C";

        #endregion

        #region Members

        private string securityToken = DefaultAuthenticationToken;

        #endregion

        #region Init

        public override void Init()
        {
            securityToken = (string)Runtime.Config["delete"]["securitytoken"].GetValueOrDefault(securityToken);
        }

        #endregion

        #region Actions

        [GrantAccessToGroup("anonymous")]
        [History(false)]
        [HttpPost()]
        public ActionResult addproject(DataTree projectData, string securitytoken)
        {
            if (securityToken == null)
                return new AjaxResult("error:security token missing");

            if (securitytoken != this.securityToken)
                return new AjaxResult("error:invalid security token");

            var message = new RCMessage("addproject");
            message.Handlers["axintegrationhandler"]["projectdata"] = projectData;
            Runtime.SendRemoteMessage(message);

            return new AjaxResult("success");
        }


        #endregion
    }
}