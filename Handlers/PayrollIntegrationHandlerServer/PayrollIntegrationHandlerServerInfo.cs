using Microsoft.VisualBasic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using SystemsGarden.mc2.Common;
using SystemsGarden.mc2.Common.Constants;
using SystemsGarden.mc2.RemoteConnector.Handlers;

namespace SystemsGarden.mc2.RemoteConnector.Handlers.PayrollIntegrationHandlerServer
{
    /// <summary>
    /// Information about PayrollIntegrationHandlerServer
    /// </summary>
    public sealed class PayrollIntegrationHandlerServerInfo : BaseHandlerInfo
    {
        static internal readonly string HandlerName = "payrollintegrationhandler";

        /// <summary>
        /// HandlerProtocolVersion
        /// </summary>
        public override int HandlerProtocolVersion
        {
            get { return 1; }
        }

        /// <summary>
        /// HandlerName
        /// </summary>
        public override string Name
        {
            get { return HandlerName; }
        }

        /// <summary>
        /// HandlerType.ServerHandler
        /// </summary>
        public override HandlerType HandlerType
        {
            get { return HandlerType.ServerHandler; }
        }
    }
}
