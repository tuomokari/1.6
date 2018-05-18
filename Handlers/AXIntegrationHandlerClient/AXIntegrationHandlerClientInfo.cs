using Microsoft.VisualBasic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using SystemsGarden.mc2.Common;
using SystemsGarden.mc2.Common.Constants;
using SystemsGarden.mc2.RemoteConnector.Handlers;

namespace SystemsGarden.mc2.RemoteConnector.Handlers.AXIntegrationHandlerServer
{
    public sealed class AXIntegrationHandlerClientInfo : BaseHandlerInfo
    {
        static internal readonly string HandlerName = "axintegrationhandler";
        public override int HandlerProtocolVersion
        {
            get { return 1; }
        }

        public override string Name
        {
            get { return HandlerName; }
        }

        public override HandlerType HandlerType
        {
            get { return HandlerType.ClientHandler; }
        }
    }
}
