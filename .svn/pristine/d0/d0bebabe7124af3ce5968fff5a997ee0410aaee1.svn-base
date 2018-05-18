using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Routing;
using System.Web.Security;
using System.Web.SessionState;
using System.Web.Optimization;
using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;
using SystemsGarden.mc2.Common.Constants;
using SystemsGarden.mc2.Common.Logging;
using SystemsGarden.mc2.Common;

namespace WebApplication2
{
	public class Global : HttpApplication
	{
		void Application_Start(object sender, EventArgs e)
		{
			RegisterRoutes(RouteTable.Routes);
			BundleConfig.RegisterBundles(BundleTable.Bundles, this);
		}

		public void Session_OnStart()
		{
			Session["init"] = true;
		}

		protected void Application_BeginRequest(object sender, EventArgs e)
		{
			Context.SetSessionStateBehavior(System.Web.SessionState.SessionStateBehavior.ReadOnly);
		}

		private static void RegisterRoutes(RouteCollection routes)
		{
			// Routes that are similar to calling main.aspx?controller={controller}&action={action}
			routes.MapPageRoute("Controller And Action",
				"app/{controller}/{action}",
				"~/main.aspx");

			routes.MapPageRoute("Rest API document",
				"doc/{collection}/{id}",
				"~/main.aspx",
				false,
				new RouteValueDictionary{
				{ "controller", "restapi" },
				{ "action", "doc" }
				});

			routes.MapPageRoute("Rest API collection",
				"find/{querycontroller}/{query}",
				"~/main.aspx",
				false,
				new RouteValueDictionary{
				{ "controller", "restapi" },
				{ "action", "find" }
				});

			routes.MapPageRoute("Rest API document field",
				"doc/{collection}/{id}/{field}",
				"~/main.aspx",
				false,
				new RouteValueDictionary{
				{ "controller", "restapi" },
				{ "action", "field" }
				});

			routes.MapPageRoute("Rest API document relation",
				"doc/{collection}/{id}/{relation}/{relatedid}",
				"~/main.aspx",
				false,
				new RouteValueDictionary{
				{ "controller", "restapi" },
				{ "action", "relation" }
				});

			routes.MapPageRoute("Rest API schema all",
				"schema",
				"~/main.aspx",
				false,
				new RouteValueDictionary{
				{ "controller", "restapi" },
				{ "action", "schema" }
				});

			routes.MapPageRoute("Rest API schema collection",
				"schema/{collection}",
				"~/main.aspx",
				false,
				new RouteValueDictionary{
				{ "controller", "restapi" },
				{ "action", "schema" }
				});
		}

		public class BundleConfig
		{
			public static void RegisterBundles(BundleCollection bundles, Global global)
			{
				BundleTable.EnableOptimizations = true;

				string rootPath = global.Server.MapPath("~/");

				RunWebCompiler(rootPath);

				RegisterCommonScriptBundle(bundles, rootPath);
				RegisterCSSBundle(bundles, rootPath);
			}

			public static void RegisterCommonScriptBundle(BundleCollection bundles, string rootPath)
			{
				// Use minify for relaese builds.
#if (DEBUG)
                var commonScriptsBundle = new Bundle("~/bundles/commonscripts");
#else
				var commonScriptsBundle = new ScriptBundle("~/bundles/commonscripts");
#endif

				if (new DirectoryInfo(Path.Combine(rootPath, "scripts")).Exists)
					commonScriptsBundle.Include("~/scripts/*.js");

				if (new DirectoryInfo(Path.Combine(rootPath, "scripts/core")).Exists)
					commonScriptsBundle.Include("~/scripts/core/*.js");

				if (new DirectoryInfo(Path.Combine(rootPath, "scripts/core/widgets")).Exists)
					commonScriptsBundle.Include("~/scripts/core/widgets/*.js");

				if (new DirectoryInfo(Path.Combine(rootPath, "scripts/widgets")).Exists)
					commonScriptsBundle.Include("~/scripts/widgets/*.js");

				if (new DirectoryInfo(Path.Combine(rootPath, "scripts/references")).Exists)
					commonScriptsBundle.Include("~/references/*.js");

				bundles.Add(commonScriptsBundle);
			}

			public static void RegisterAmChartsScriptBundle(BundleCollection bundles, string rootPath)
			{
				// Use minify for relaese builds.
#if (DEBUG)
                var amChartsScriptsBundle = new Bundle("~/bundles/amchartsscripts");
#else
				var amChartsScriptsBundle = new ScriptBundle("~/bundles/amchartsscripts");
#endif

				amChartsScriptsBundle.Include("~/references/amcharts/exporting/*.js");
				amChartsScriptsBundle.Include("~/references/amcharts/lang/*.js");
				amChartsScriptsBundle.Include("~/references/amcharts/plugins/responsive/*.js");
				amChartsScriptsBundle.Include("~/references/amcharts/themes/*.js");
				amChartsScriptsBundle.Include("~/references/amcharts/*.js");

				bundles.Add(amChartsScriptsBundle);
			}

			public static void RegisterCSSBundle(BundleCollection bundles, string rootPath)
			{
				// Application css files are bundled by less and added manually
				var cssBundle = new Bundle("~/bundles/styles");

				if (new DirectoryInfo(Path.Combine(rootPath, "styles/widgets")).Exists)
					cssBundle.Include("~/styles/widgets/*.css");

				bundles.Add(cssBundle);
			}

            public static void RunWebCompiler(string rootPath)
            {
                const string CompilerConfigFileName = "compilerconfig.json";
                const string CompilerFileName = "App_Data\\Tools\\Web compiler\\WebCompiler.exe";
                const string CompilerResultsFileName = "App_Data\\Tools\\Web compiler\\results.txt";
                string arguments = "\"" + Path.Combine(rootPath, CompilerConfigFileName);// + "\" " + LessFileFilter;
                string fileName = Path.Combine(rootPath, CompilerFileName);

                Process p = new Process();
                p.StartInfo.FileName = fileName;
                p.StartInfo.Arguments = arguments;
                p.StartInfo.WorkingDirectory = rootPath;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.UseShellExecute = false;

                const int LessFilesBuildMaxTime = 30000;

                try
                {
                    p.Start();
                    p.WaitForExit(LessFilesBuildMaxTime);

                    string output = p.StandardOutput.ReadToEnd();
                    File.WriteAllText(Path.Combine(rootPath, CompilerResultsFileName), output);
                }
                catch (Exception ex)
                {
                    File.WriteAllText(Path.Combine(rootPath, CompilerResultsFileName), "Error: " + ex.ToString());
                    // If less files cannot be compiled in time, continue regardless.
                    return;
                }
            }
		}
	}
}