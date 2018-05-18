﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SystemsGarden.mc2.Common;
using SystemsGarden.mc2.Common.Constants;
using SystemsGarden.mc2.RemoteConnector.Handlers;

namespace SystemsGarden.mc2.RemoteConnector.Handlers.ArchiveHandlerServer
{
    public class ArchiveHandlerServerInfo : BaseHandlerInfo
    {
        static internal readonly string HandlerName = "archivehandler";
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
            get { return HandlerType.ServerHandler; }
        }
    }
}
