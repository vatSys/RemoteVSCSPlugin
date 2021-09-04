using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace RemoteVSCSPlugin
{
    public class VSCSWebSocketService : WebSocketBehavior
    {
        public static string Path = VatSysWebSocketServer.ROOT_URL + "/vscs";

        protected override void OnOpen()
        {
            base.OnOpen();
            Send(RemoteVSCSPlugin.CurrentStateJSON);
        }

        protected override void OnMessage(MessageEventArgs e)
        {
            base.OnMessage(e);
        }
    }
}
