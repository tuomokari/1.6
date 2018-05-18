using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using SystemsGarden.mc2.Core.Runtime;
using SystemsGarden.mc2.MC2SiteEnvironment;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Net;
using System.IO;

[assembly: InternalsVisibleTo("SystemsGarden.mc2.MC2SiteEnvironment")]

namespace SystemsGarden.mc2.MC2Site
{
    public partial class Main : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {

            RuntimeHost runtimeHost =  GetAndSetupRuntimeHost();

            if (runtimeHost.NotStarted)
            {
                Response.Output.WriteLine("Server was set not to start. Please check your configuration files.");
                Response.End();
                return;
            }

            HandleCallActionResult(
                runtimeHost.CallAction(Request, Session, Page.RouteData));
        }

        private void HandleCallActionResult(MC2ActionCallHTTPResult result)
        {

            // Todo: This is sub-optimal. Return type shold be an object with type redirect.
            if (result.HttpStatus == HttpStatusCode.Redirect)
            {
                Response.Redirect(result.ResultBuffer, false);
                HttpContext.Current.ApplicationInstance.CompleteRequest();
            }
            else if (result.FileName != null)
            {
                FileInfo fileInfo = new FileInfo(result.FileName);

                if (fileInfo.Exists)
                {
                    Response.Clear();
                    Response.AddHeader("Content-Disposition", "attachment; filename=" + fileInfo.Name);
                    Response.AddHeader("Content-Length", fileInfo.Length.ToString());
                    Response.ContentType = "application/octet-stream";
                    Response.Flush();
                    Response.TransmitFile(fileInfo.FullName);
                    Response.End();
                }
            }
            else
            {
                const string DefaultErrorBuffer = "error";
                Response.StatusCode = (int)result.HttpStatus;

                // Prevent IIS default error in case of empty buffer.
                if (result.HttpStatus != HttpStatusCode.Accepted && result.HttpStatus != HttpStatusCode.OK &&
                    string.IsNullOrEmpty(result.ResultBuffer))
                    result.ResultBuffer = DefaultErrorBuffer;

                Response.Write(result.ResultBuffer);
                HttpContext.Current.ApplicationInstance.CompleteRequest();
            }
        }

        private RuntimeHost GetAndSetupRuntimeHost()
        {
            List<Type> controllers = GetControllersWithReflection();

            RuntimeHost runtimeHost = RuntimeHost.GetRuntimeHost(this.Server, controllers);

            return runtimeHost;
        }

        /// <summary>
        /// Loads MC2 Controllers into memory.
        /// </summary>
        /// <returns></returns>
        /// <remarks>Done here instead of RuntimeHost to get easier access to assembly.</remarks>
        private List<Type> GetControllersWithReflection()
        {
            var ret = new List<Type>();

            foreach (Type type in Assembly.GetExecutingAssembly().GetTypes())
            {
                if (type.IsClass && type.IsSubclassOf(typeof(MC2Controller)))
                    ret.Add(type);
            }

            return ret;
        }
    }
}