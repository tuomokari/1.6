using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Optimization;
using System.Diagnostics;
using System.IO;
using SystemsGarden.mc2.Core.Runtime;
using SystemsGarden.mc2.Common;

namespace SystemsGarden.mc2.MC2Site.App_Code.Controllers.__builtin
{
	public class runtime : MC2Controller
	{
		#region Actions

		[GrantAccessToGroup("authenticated")]
		public ActionResult about()
		{
			const string BinFolder = "bin";
			const string CoreDllName = "SystemsGarden.mc2.Core.dll";

			string coreDllPath = Path.Combine(Runtime.RootDirectory, BinFolder, CoreDllName);
			ViewBag["versionmc2"] = FileVersionInfo.GetVersionInfo(coreDllPath).ProductVersion.ToString();

			string applicationDllName = Runtime.Config["application"]["name"] + ".dll";
			string applicationDllPath = Path.Combine(Runtime.RootDirectory, BinFolder, applicationDllName);
			ViewBag["versionapplication"] = FileVersionInfo.GetVersionInfo(applicationDllPath).ProductVersion.ToString();

			return View();
		}

		#endregion

		#region Blocks

		public MC2Value version()
		{
			return typeof(Runtime).Assembly.GetName().Version.ToString();
		}

		public MC2Value scriptbundle()
		{
			return Scripts.Render("~/bundles/commonscripts").ToHtmlString();
		}

		public MC2Value stylebundle()
		{
			return Styles.Render("~/bundles/styles").ToHtmlString();
		}

		public MC2Value amchartsbundle()
		{
			return Scripts.Render("~/bundles/amchartsscripts").ToHtmlString();
		}

		public MC2Value timedtaskinfo()
		{
			return Runtime.GetTimedTaskInfo();
		}

		/// <summary>
		/// Retruns whether the current date has been overridden for testing purproses.
		/// </summary>
		/// <returns></returns>
		public MC2Value isdateoverridden()
		{
			return MC2DateTimeValue.IsNowOverridden;
		}

		/// <summary>
		/// Returns the overridden date set for testing as a string.
		/// </summary>
		/// <returns></returns>
		public MC2Value overriddendate()
		{
			return MC2DateTimeValue.Now();
		}

		#endregion
	}
}