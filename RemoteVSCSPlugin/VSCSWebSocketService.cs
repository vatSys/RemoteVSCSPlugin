using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharp;
using WebSocketSharp.Server;
using Newtonsoft.Json;

namespace RemoteVSCSPlugin
{
    public class VSCSWebSocketService : WebSocketBehavior
    {
        public static string Path = VatSysWebSocketServer.ROOT_URL + "/vscs";

        public event EventHandler<VSCSCommandReceivedEventArgs> VSCSCommandReceived;
        public event EventHandler OpenedSession;

        public new void Send(string json)
        {
            base.Send(json);
        }

        protected override void OnOpen()
        {
            base.OnOpen();
            OpenedSession?.Invoke(this, new EventArgs());
        }

        protected override void OnMessage(MessageEventArgs e)
        {
            base.OnMessage(e);

            if(e.IsText)
            {
                try
                {
                    VSCSCommand cmd = JsonConvert.DeserializeObject<VSCSCommand>(e.Data);
                    VSCSCommandReceived?.Invoke(this, new VSCSCommandReceivedEventArgs(cmd));

                }
                catch(JsonException ex)
                {
                    Error("Invalid JSON packet received", ex);
                }
            }
        }
    }

    public class VSCSCommandReceivedEventArgs : EventArgs
    {
        public VSCSCommand VSCSCommand { get;}
        public VSCSCommandReceivedEventArgs(VSCSCommand cmd)
        {
            VSCSCommand = cmd;
        }
    }
}
