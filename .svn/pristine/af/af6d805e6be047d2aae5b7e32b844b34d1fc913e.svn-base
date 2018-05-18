using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using SystemsGarden.mc2.Core.Runtime;
using SystemsGarden.mc2.Common;
using SystemsGarden.mc2.Common.Constants;
using SystemsGarden.mc2.Core;
using System.Net.Mail;
using System.Threading;
using MongoDB.Driver.Builders;
using MongoDB.Driver;
using MongoDB.Bson;
using SystemsGarden.mc2.Common.Compatibility;

namespace SystemsGarden.mc2.MC2Site.App_Code.Controllers.__builtin
{
    public class login : MC2Controller, IMonitorDataSource
	{
        #region Constants

        private const string SilmuErrorHeader = "ERROR: ";

        private const string GateCordovaDevice = "silmu2.phonegap.device";
        private const string GateCordovaVersion = "silmu2.phonegap.device";
        private const string GateCordovaCustomer = "silmu2.phonegap.customer";

		#endregion

		#region Members

		private int usersLoggedIn = 0;
		private int adUsersLoggedIn = 0;
		private int passwordUsersLoggedIn = 0;
		private int gateUsersLoggedIn = 0;
		private int usersLoggedOut = 0;
		private int gateUserRedirects = 0;
		private int failedLogins = 0;
		private int failedGateLogins = 0;
		private int failedLoginsDueToTech = 0;
		private int passwordsRequested = 0;

		Dictionary<string, string> loginTokens = new Dictionary<string, string>();

		public string MonitorDataSourceName
		{
			get
			{
				return "login";
			}
		}

		#endregion

		#region Init

		public override void Init()
		{
			Runtime.Monitoring.RegisterMonitorDataSource(this);
		}

		#endregion

		#region Actions



		[HttpPost]
        [GrantAccessToGroup("anonymous")]
		[Obsolete]
        public ActionResult silmulogin(DataTree formParameters)
        {
            string token = Guid.NewGuid().ToString();

            try
            {
                logger.LogInfo("Silmulogin (post) called.");

                DataTree silmu2 = LegacyRegister.ContentDataToDataTree((string)formParameters["silmu2_contentdata"]);
                DataTree gateParameters = LegacyRegister.ContentDataToDataTree((string)formParameters["gate_parameters"]);

                string loginGuid = (string)formParameters["sov_varmistus_guid"];

                if (string.IsNullOrEmpty(loginGuid) || loginGuid != (string)Runtime.Config["security"]["loginguid"])
                {
                    logger.LogWarning("Login attempt with invalid login GUID");
                    return new AjaxResult(SilmuErrorHeader + ": Login attempt with invalid GUID");
                }

                string userEmail = gateParameters["v001"]["account"]["email"];

                if (string.IsNullOrEmpty(userEmail))    
                    userEmail = silmu2["account"]["email"];

                userEmail = userEmail.ToLower();

                logger.LogInfo("Login info received for user. Passing a token.", userEmail);

                lock (loginTokens)
                {
                    loginTokens.Add(token, userEmail);
                }

                SetupCordovaSessionVariables(gateParameters);
            }
            catch (Exception e)
            {
                return new AjaxResult(SilmuErrorHeader + e.ToString());
            }

            return new AjaxResult((MC2Value)("<url>" + Runtime.CurrentActionCall.RootAddress + "/main.aspx?controller=login&action=silmulogin2&token=" + token + "</url>"));
        }

		public ActionResult gatelogin(DataTree formParameters)
		{
			string token = Guid.NewGuid().ToString();

			try
			{
				logger.LogInfo("Gate login (post) called.");

				DataTree silmu2 = LegacyRegister.ContentDataToDataTree((string)formParameters["silmu2_contentdata"]);
				DataTree gateParameters = LegacyRegister.ContentDataToDataTree((string)formParameters["gate_parameters"]);


				string loginGuid = (string)formParameters["sov_varmistus_guid"];

				if (string.IsNullOrEmpty(loginGuid) || loginGuid != (string)Runtime.Config["security"]["loginguid"])
				{
					logger.LogWarning("Login attempt with invalid login GUID");
					return new AjaxResult(SilmuErrorHeader + ": Login attempt with invalid GUID");
				}

				string userEmail = gateParameters["v001"]["account"]["email"];

				if (string.IsNullOrEmpty(userEmail))
					userEmail = silmu2["account"]["email"];

				userEmail = userEmail.ToLower();

				logger.LogInfo("Login info received for user. Passing a token.", userEmail);

				lock (loginTokens)
				{
					loginTokens.Add(token, userEmail);
				}

				SetupCordovaSessionVariables(gateParameters);
			}
			catch (Exception e)
			{
				Interlocked.Increment(ref failedGateLogins);
				return new AjaxResult(SilmuErrorHeader + e.ToString());
			}

			return new AjaxResult((MC2Value)("<url>" + Runtime.CurrentActionCall.RootAddress + "/main.aspx?controller=login&action=silmulogin2&token=" + token + "</url>"));
		}

		private void SetupCordovaSessionVariables(DataTree gateParameters)
        {
            if ((bool)Runtime.Config["runtime"]["cordova"])
            {
                Runtime.SessionManager.Session["cordova"]["device"] = gateParameters["v002"]["cordova"]["device"];
                Runtime.SessionManager.Session["cordova"]["version"] = gateParameters["v002"]["cordova"]["version"];
                Runtime.SessionManager.Session["cordova"]["customer"] = gateParameters["v002"]["cordova"]["customer"];
            }
        }

        [GrantAccessToGroup("anonymous")]
        public ActionResult silmulogin2(string token)
        {
            string userEmail;

            lock (loginTokens)
            {
				if (!loginTokens.ContainsKey(token))
				{
					Interlocked.Increment(ref failedGateLogins);
					throw new RuntimeException("Invalid login token.");
				}

				userEmail = loginTokens[token];
                loginTokens.Remove(token);
            }

            // Log in user and get default page.
            DataTree loginUser = Runtime.SessionManager.GateLogin(userEmail);

            if (loginUser == null)
            {
                logger.LogWarning("User not found in database.", userEmail);
				Interlocked.Increment(ref failedGateLogins);
				throw new RuntimeException("User not found in database.");
            }

            logger.LogInfo("User logged in.", userEmail);

			Interlocked.Increment(ref usersLoggedIn);
			Interlocked.Increment(ref gateUsersLoggedIn);

			return new RedirectResult(Runtime.CurrentActionCall.RootAddress);
        }

        // Login with the intention of redirecting to another site
        [HttpPost]
        [GrantAccessToGroup("anonymous")]
        public ActionResult gateurllogin(DataTree formParameters)
        {
            logger.LogInfo("Gate url login called.");

            string redirectUrl = string.Empty;

            try
            {
                DataTree common = LegacyRegister.ContentDataToDataTree((string)formParameters["yleiset_contentdata"]);
                DataTree menu = LegacyRegister.ContentDataToDataTree((string)formParameters["menureg_contentdata"]);
                DataTree silmu2 = LegacyRegister.ContentDataToDataTree((string)formParameters["silmu2_contentdata"]);

                string loginGuid = (string)formParameters["sov_varmistus_guid"];

                if (string.IsNullOrEmpty(loginGuid) || loginGuid != (string)Runtime.Config["security"]["loginguid"])
                {
                    logger.LogWarning("Login attempt with invalid login GUID");
                    throw new AccessDeniedException();
                }

                string userEmail = (string)silmu2["account"]["email"].GetValueOrDefault(string.Empty);

                var userQuery = new DBQuery("core", "userbyemail");
                userQuery.AddParameter("email", userEmail);

                // User must be found when redirecting to addresses
                DataTree user = userQuery.FindOneAsync().Result;
                if (user == null)
                    return new AjaxResult(SilmuErrorHeader + "User was not found in database.");

                string target = (string)silmu2["app"]["parameters"]["target"].GetValueOrDefault(String.Empty);
                string url = (string)silmu2["app"]["parameters"]["url"].GetValueOrDefault(String.Empty);

                if (!string.IsNullOrEmpty(target))
                    redirectUrl = Runtime.Config["gateurls"][target];
                else
                    redirectUrl = url;

                if (string.IsNullOrEmpty(redirectUrl))
                    return new AjaxResult(SilmuErrorHeader + "Redirect url was not found.");

				Interlocked.Increment(ref gateUserRedirects);
			}

			catch (Exception ex)
            {
                return new AjaxResult(SilmuErrorHeader + ex.ToString());
            }

            return new AjaxResult("<url>" + redirectUrl + "</url>");
        }


        [GrantAccessToGroup("anonymous")]
        public ActionResult passwordlogin()
        {
            logger.LogInfo("Password login form called.");
            return NamedView("login", "passwordlogin");
        }

        [HttpPost]
        [GrantAccessToGroup("anonymous")]
        public ActionResult passwordlogin(DataTree formParameters)
        {
            logger.LogInfo("Password login (post) called.");

            string userEmail = formParameters["username"];
            string password = formParameters["password"];

            userEmail = userEmail.ToLower();

            bool loginOk = true;
            bool connectionOk = false;
            DataTree loginUser = null;
            // Log in user, get default page and get going.
            try
            {
                loginUser = Runtime.SessionManager.PasswordLogin(userEmail, password);
                connectionOk = true;
            }
            catch (InvalidLoginException)
            {
                loginOk = false;
                connectionOk = true;
            }
            catch (RuntimeException ex)
            {
                loginOk = false;
            }

            // If password login fails use AD login.
            if (loginUser == null || !loginOk)
            {
                try
                {
                    loginUser = Runtime.SessionManager.LoginUserAD(userEmail, password);
                    connectionOk = true;
                }
                catch (InvalidLoginException)
                {
                    loginOk = false;
                    connectionOk = true;
                }
                catch (RuntimeException ex)
                {
                    loginOk = false;
                }
            }

			if (!connectionOk)
			{
				Interlocked.Increment(ref failedLoginsDueToTech);
				return View("error_technicaldifficulties");
			}
			else if (loginUser == null || !loginOk)
			{
				Interlocked.Increment(ref failedLogins);
				return View("error_invalidlogin");
			}

			Interlocked.Increment(ref passwordUsersLoggedIn);
			Interlocked.Increment(ref usersLoggedIn);

			return new RedirectResult(Runtime.CurrentActionCall.RootAddress);
        }

        [GrantAccessToGroup("anonymous")]
        public ActionResult requestpassword()
        {
            return View();
        }

        [HttpPost]
        [GrantAccessToGroup("anonymous")]
        public ActionResult requestpassword(DataTree formParameters)
        {
            logger.LogInfo("Request password (post) called.");

            string userEmail = formParameters["email"];

            userEmail = userEmail.ToLower();

            string password = "";
            try
            {
                DataTree user;
                password = Runtime.SessionManager.GetNewPassword(userEmail, out user);

                string language = (string)user["language"].GetValueOrDefault(Runtime.DefaultLanguage);

                string subject = Runtime.GetTranslation("login_new_password_email_subject", Runtime.Schema.First.Name, language);
                string body = Runtime.GetTranslation("login_new_password_email_body", Runtime.Schema.First.Name, language);
                string companySignature = Runtime.GetTranslation("company_signature", Runtime.Schema.First.Name, language);

                body = body.Replace("{0}", userEmail);
                body = body.Replace("{1}", password);
                body = body.Replace("{2}", companySignature);

                Runtime.EmailSender.SendEmailMessage(
                    userEmail,
                    subject,
                    body);
            }
            catch (RuntimeException ex)
            {
                if (ex.Message == "Invalid email")
                    return View("invalidemail");

                else
                    throw;
            }

			Interlocked.Increment(ref passwordsRequested);

			return View("passwordsent");
        }

        [GrantAccessToGroup("anonymous")]
        public ActionResult adlogin()
        {
            logger.LogInfo("AD login form called.");
            return NamedView("login", "adlogin");
        }

        [HttpPost]
        [GrantAccessToGroup("anonymous")]
        public ActionResult adlogin(DataTree formParameters)
        {
            logger.LogInfo("AD login (post) called.");

            string username = formParameters["username"];
            string password = formParameters["password"];

            bool loginOk = true;
            bool connectionOk = true;

            DataTree loginUser = null;

            // Log in user, get default page and get going.
            try
            {
                loginUser = Runtime.SessionManager.LoginUserAD(username, password);
            }
            catch(InvalidLoginException)
            {
                loginOk = false;
            }
			catch (RemoteConnectionNotOperationalException)
			{
				connectionOk = false;
				loginOk = false;
			}
			catch (RuntimeException)
            {
                loginOk = false;
            }

            // If AD login fails attempt with password login
            if (loginUser == null || !loginOk)
            {
                try
                {
                    loginUser = Runtime.SessionManager.PasswordLogin(username, password);
                    loginOk = true;
					connectionOk = true;
                }
                catch (InvalidLoginException)
                {
					loginOk = false;
                }
				catch (RemoteConnectionNotOperationalException)
				{
					connectionOk = false;
					loginOk = false;
				}
				catch(Exception)
				{
					loginOk = false;
				}
			}

			if (!connectionOk)
			{
				Interlocked.Increment(ref failedLoginsDueToTech);
				return View("error_technicaldifficulties");
			}
			else if (loginUser == null || !loginOk)
			{
				Interlocked.Increment(ref failedLogins);
				return View("error_invalidlogin");
			}

			Interlocked.Increment(ref adUsersLoggedIn);
			Interlocked.Increment(ref usersLoggedIn);

			return new RedirectResult(Runtime.CurrentActionCall.RootAddress);
        }

        [GrantAccessToGroup("sysadmin")]
        public ActionResult setpassword()
        {
            return View();
        }

        [GrantAccessToGroup("sysadmin")]
        [HttpPost]
        public ActionResult setpassword(DataTree formData)
        {
            string password = formData["password"];
            string user = formData["user"];

            var setPasswordMessage = new RCMessage(MongoDBHandlerConstants.mdbsetpassword);
            setPasswordMessage.Handlers[MongoDBHandlerConstants.mongodbhandler][MongoDBHandlerConstants.userid] = user;
            setPasswordMessage.Handlers[MongoDBHandlerConstants.mongodbhandler][MongoDBHandlerConstants.password] = password;

            Runtime.SendRemoteMessage(setPasswordMessage);

            return Redirect(Runtime.HistoryManager.GetPreviousAddress());
        }

        [GrantAccessToGroup("anonymous")]
        public ActionResult logout()
        {
            Runtime.SessionManager.Logout();
			
			Interlocked.Increment(ref usersLoggedOut);			

			return new RedirectResult(Runtime.CurrentActionCall.RootAddress + "?controller=login&action=adlogin");
        }

        [GrantAccessToGroup("administrators")]
        public ActionResult impersonate()
        {
            // Handle impersonation
            return View();
        }

        [HttpPost]
        [GrantAccessToGroup("administrators")]
        public ActionResult impersonate(DataTree formData)
        {
            // Handle impersonation
            string userId = formData["user"];

            bool success = true;

            try
            {
                if (string.IsNullOrEmpty(userId))
                    success = false;

                if (success)
                {
                    DBQuery userQuery = new DBQuery();

                    userQuery["user"][DBQuery.Condition] = Query.EQ("_id", new ObjectId(userId))
                        .ToString();

					DataTree user = userQuery.FindOne();
                    Runtime.SessionManager.UnauthenticatedLogin(user["email"]);

                    return Redirect(Runtime.HistoryManager.GetPreviousAddress());
                }
            }
            catch (Exception)
            {
                success = false;
                logger.LogError("Failed to impersonate", userId);
            }

            return View(success);
        }

		public DataTree GetMonitorData()
		{
			var results = new DataTree();

			results["usersloggedin"] = usersLoggedIn;
			results["adusersloggedin"] = adUsersLoggedIn;
			results["passwordusersloggedin"] = passwordUsersLoggedIn;
			results["gateusersloggedin"] = gateUsersLoggedIn;
			results["usersloggedout"] = usersLoggedOut;
			results["failedlogins"] = failedLogins;
			results["failedloginsduetotech"] = failedLoginsDueToTech;
			results["passwordsRequested"] = passwordsRequested;

			return results;
		}

		#endregion
	}
}