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

        public VatSysWebSocketServer(int port, CancellationToken token)
        {
            listenPort = port;
            cancellationToken = token;
            StartWebSocketServer();
        }

        private void StartWebSocketServer()
        {
            websocketServerTask = new Task(() => WebSocketServer(), cancellationToken, TaskCreationOptions.LongRunning);
            websocketServerTask.Start();
        }

        private void WebSocketServer()
        {
            webSocket = new WebSocketServer(listenPort);
            cancellationToken.Register(() => webSocket.Stop());
            webSocket.AddWebSocketService<VSCSWebSocketService>(VSCSWebSocketService.Path);
            webSocket.Start();
        }

        public void BroadcastToVSCS(string message)
        {
            webSocket.WebSocketServices[VSCSWebSocketService.Path].Sessions.BroadcastAsync(message, null);
        }
    }
}
