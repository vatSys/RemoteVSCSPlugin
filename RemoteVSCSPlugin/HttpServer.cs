﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace RemoteVSCSPlugin
{
    public class HttpServer
    {
        private static string[] HTTP_PERMITTED_EXTENSIONS = new string[] { ".html", ".htm", ".txt", ".ico", ".css", ".js" };
        private static string[] HTTP_HEADER_SEPARATOR = new string[1] { "\r\n" };
        private static char[] HTTP_GET_SEPARATOR = new char[1] { ' ' };

        private string vscsHTML;
        private byte[] vscsHttpResponse;
        private byte[] NotFoundHttpResponse;

        private Task httpListenTask;
        private CancellationToken cancellationToken;
        private int listenPort;

        public HttpServer(int port, CancellationToken token)
        {
            listenPort = port;
            cancellationToken = token;

            LoadHttpResponses();
            StartHttpListener();
        }

        private void LoadHttpResponses()
        {
            vscsHTML = File.ReadAllText("vscs.html");

            StringBuilder response = new StringBuilder();
            response.Append("HTTP/1.0 200 OK" + Environment.NewLine);
            response.Append("Connection: close" + Environment.NewLine);
            response.Append("Content-Type: text/html" + Environment.NewLine);
            response.Append("Content-Length: " + vscsHTML.Length + Environment.NewLine);
            response.Append(Environment.NewLine);
            response.Append(vscsHTML);
            response.Append(Environment.NewLine);

            vscsHttpResponse = Encoding.ASCII.GetBytes(response.ToString());

            response = new StringBuilder();
            response.Append("HTTP/1.0 404 Not Found" + Environment.NewLine);
            response.Append("Connection: close" + Environment.NewLine);
            response.Append(Environment.NewLine);

            NotFoundHttpResponse = Encoding.ASCII.GetBytes(response.ToString());
        }

        private void StartHttpListener()
        {
            httpListenTask = new Task(() => HttpListen(), cancellationToken, TaskCreationOptions.LongRunning);
            httpListenTask.Start();
        }

        private async void HttpListen()
        {
            TcpListener listener = new TcpListener(IPAddress.Any, listenPort);
            listener.Start();

            cancellationToken.Register(() => listener.Stop());

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var client = await listener.AcceptTcpClientAsync();
                    ProcessHttpClient(client);
                }
            }
            catch { }
        }

        private async void ProcessHttpClient(TcpClient client)
        {
            client.LingerState = new LingerOption(true, 500);
            var stream = client.GetStream();
            stream.WriteTimeout = 500;

            var receive = new byte[client.ReceiveBufferSize];
            var len = await stream.ReadAsync(receive, 0, receive.Length);

            string req = Encoding.ASCII.GetString(receive, 0, len);

            if (len < 3 || !req.StartsWith("GET "))
            {
                client.Close();
                return;
            }

            string[] reqLines = req.Split(HTTP_HEADER_SEPARATOR, StringSplitOptions.RemoveEmptyEntries);
            var splitGet = reqLines[0].Split(HTTP_GET_SEPARATOR, StringSplitOptions.RemoveEmptyEntries);

            if (splitGet.Length < 2 || splitGet[1] != "/")
            {
                string path = splitGet[1];
                if(path.StartsWith("/"))
                    path = path.Substring(1);

                string ex = Path.GetExtension(path)?.ToLower();

                if (!string.IsNullOrEmpty(ex) && HTTP_PERMITTED_EXTENSIONS.Contains(ex) && File.Exists(path))
                {
                    var buf = BuildResponseFromFile(path, ex);
                    await stream.WriteAsync(buf, 0, buf.Length);
                }
                else
                    await stream.WriteAsync(NotFoundHttpResponse, 0, NotFoundHttpResponse.Length);
            }
            else
            {
                await stream.WriteAsync(vscsHttpResponse, 0, vscsHttpResponse.Length);
            }
            client.Close();
        }

        private byte[] BuildResponseFromFile(string path, string extension)
        {
            string file = File.ReadAllText(path);

            string contentType = "text/plain";
            switch(extension)
            {
                case ".htm":
                case ".html":
                    contentType = "text/html";
                    break;
                case ".css":
                    contentType = "text/css";
                    break;
                case ".js":
                    contentType = "text/javascript";
                    break;
                case ".ico":
                    contentType = "image/x-icon";
                    break;
            }

            StringBuilder response = new StringBuilder();
            response.Append("HTTP/1.0 200 OK" + Environment.NewLine);
            response.Append("Connection: close" + Environment.NewLine);
            response.Append($"Content-Type: {contentType}" + Environment.NewLine);
            response.Append("Content-Length: " + file.Length + Environment.NewLine);
            response.Append(Environment.NewLine);
            response.Append(file);
            response.Append(Environment.NewLine);

            return Encoding.ASCII.GetBytes(response.ToString());
        }
    }
}
