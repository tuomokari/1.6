using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using SystemsGarden.mc2.Core.Runtime;
using SystemsGarden.mc2.Common;
using System.Threading;
using MarkdownSharp;

namespace SystemsGarden.mc2.MC2Site.App_Code.Controllers.Builtin
{
    public class security : MC2Controller
    {
		public MC2Value isusermemberof(DataTree user,  string group)
		{
			return Runtime.Security.IsUserMemberOfAccessGroup(user, group);
		}
	}
}