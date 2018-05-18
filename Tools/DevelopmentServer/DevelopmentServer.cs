using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Threading;
using System.Text;
using System.IO;
using SystemsGarden.mc2.Core;
using SystemsGarden.mc2.RemoteConnector;
using SystemsGarden.mc2.Common;
using SystemsGarden.mc2.Common.Logging;

namespace SystemsGarden.mc2.Tools.DevelopmentServer
{
    public sealed class DevelopmentServerHost : IDisposable
    {
	    public const  string SOFTWARE_NAME = "Silmu Test Server";

        private const int StartupDelay1 = 1000;
        private const int StartupDelay2 = 1000;
    
	    public object ServiceAddress { get; set; }
	    public object ServiceEndpointName { get; set; }
	    public StringBuilder Output { get; set; }

	    public ILogger logger { get; set; }

        public ManualResetEvent stopping = new ManualResetEvent(false);

        private static string DefaultConfigurationFile = "config.tree";

	    internal DataTree Configuration;
	    internal string ConfigurationText = string.Empty;

	    internal bool Running = false;

	    public IRemoteConnectionContainer remoteConnectionContainer;

        // This remote connection is not used for connecting. Just a helper
	    // to allow us to load all modules and see which modules are available.
	    private RemoteConnection testRemoteConnection;
	    internal HandlerContainer HandlerContainer;

	    private HandlerLoader handlerLoader;

        private DevelopmentServerForm developmentServerForm;

	    public DevelopmentServerHost(DevelopmentServerForm developmentServerForm)
	    {
            this.developmentServerForm = developmentServerForm;

            LoadDefaultConfiguration();

            SetupLogging();

            testRemoteConnection = new RemoteConnection(logger, HandlerType.ServerHandler, SOFTWARE_NAME);

            LoadModules();
        }

        ~DevelopmentServerHost()
        {
            Dispose();
        }

	    public void StartServer()
	    {
		    if (Running)
            {
			    logger.LogWarning("Server already running.");
			    return;
		    }

		    try 
            {
			    logger.LogFineTrace("Creating connection container");

			    remoteConnectionContainer = new RemoteConnectionContainer(HandlerType.ServerHandler, logger);

			    logger.LogTrace("Initializing connection container");

                remoteConnectionContainer.Initialize(Configuration);

			    logger.LogInfo("Opening remote connections.");

			    remoteConnectionContainer.OpenConnections();

			    Running = true;

			    logger.LogDebug("Connections open");
		    }
            catch (Exception ex)
            {
			    logger.LogError("Exception when starting server:", ex);
		    }
	    }


	    public void StopServer()
	    {
		    if (!Running)
            {
			    logger.LogWarning("Server not running when attempting to stop.");
			    return;
		    }

		    logger.LogInfo("Stopping server.");

		    remoteConnectionContainer.CloseConnections();

		    Running = false;

		    logger.LogInfo("Server stopped.");

		    remoteConnectionContainer.Dispose();
		    remoteConnectionContainer = null;
	    }

	    public void SetConfiguration(string configurationText)
	    {
		    try
            {
			    Configuration = DataTree.CreateFromString(configurationText);
			    this.ConfigurationText = configurationText;
                
                developmentServerForm.UpdateConfigurationView();
		    } 
            catch (Exception ex)
            {
                developmentServerForm.ShowErrorMessage("Failed to set configuration: " + ex.Message);
		    }
	    }

        // Test server always logs with named pipe logger.
	    private void SetupLogging()
	    {
            // Wait before starting logger so that trace can become available
            stopping.WaitOne(StartupDelay2);

            logger = LoggingSetup.GetLoggerGroup(Configuration);

            // Wait to give logger time to connect
            stopping.WaitOne(StartupDelay2);

            logger.LogInfo("Starting development server.");
	    }

	    private void LoadModules()
	    {
		    logger.LogDebug("Loading modules for development server.");

		    HandlerContainer = new HandlerContainer(logger, testRemoteConnection);

		    dynamic binPath = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location) + Path.DirectorySeparatorChar;

            handlerLoader = new HandlerLoader(logger, testRemoteConnection, binPath, HandlerContainer);
		    handlerLoader.RegisterAllHandlersToContainer();

		    logger.LogDebug("Modules loaded.");
	    }

        private void LoadDefaultConfiguration()
        {
            LoadConfiguration(DefaultConfigurationFile);
        }

        private void LoadConfiguration(string configurationFile)
        {
            this.ConfigurationText = File.ReadAllText(configurationFile);
            Configuration = DataTree.CreateFromString(ConfigurationText);

            developmentServerForm.UpdateConfigurationView();
        }

	    // To detect redundant calls
	    private bool disposedValue;

	    // IDisposable
	    private void Dispose(bool disposing)
	    {

		    if (!this.disposedValue) {
                stopping.Set();

			    if (logger != null)
				    logger.Dispose();
			    if (remoteConnectionContainer != null)
				    remoteConnectionContainer.Dispose();

		    }
		    this.disposedValue = true;
	    }

	    public void Dispose()
	    {
		    Dispose(true);
		    GC.SuppressFinalize(this);
	    }
    }
}