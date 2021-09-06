using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace RemoteVSCSPlugin
{
    public class VatSysWebSocketServer
    {
        public const string ROOT_URL = "/vatsys";

        private int listenPort;
        private CancellationToken cancellationToken;
        private Task websocketServerTask;
        private WebSocketServer webSocket;
        private string currentStateJson;

        public event EventHandler<VSCSCommandReceivedEventArgs> VSCSCommandReceived;

        public VatSysWebSocketServer(int port, CancellationToken token)
        {
            listenPort = port;
            cancellationToken = token;
            StartWebSocketServer();
        }

        private void StartWebSocketServer()
        {
            webSocket = new WebSocketServer(listenPort);
            cancellationToken.Register(() => webSocket.Stop());
            webSocket.AddWebSocketService<VSCSWebSocketService>(VSCSWebSocketService.Path, a => 
            {
                a.VSCSCommandReceived += OnVSCSCommandReceived;
                a.OpenedSession += OnOpenedSession;
            });

            websocketServerTask = new Task(() => webSocket.Start(), cancellationToken, TaskCreationOptions.LongRunning);
            websocketServerTask.Start();
        }

        private void OnOpenedSession(object sender, EventArgs e)
        {
            BroadcastToVSCS(currentStateJson);
        }

        private void OnVSCSCommandReceived(object sender, VSCSCommandReceivedEventArgs e)
        {
            VSCSCommandReceived?.Invoke(this, e);
        }

        public void BroadcastToVSCS(string message)
        {
            if (!string.IsNullOrEmpty(message))
            {
                currentStateJson = message;
                webSocket.WebSocketServices[VSCSWebSocketService.Path].Sessions.BroadcastAsync(message, null);
            }
        }
    }
}
