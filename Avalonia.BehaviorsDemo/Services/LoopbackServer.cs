using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Avalonia.BehaviorsDemo.Services
{
    /// <summary>
    /// A tiny HTTP server bound to 127.0.0.1 only, so HttpRequestAction has something safe to talk to.
    /// POST answers 201 with "pong:" plus the request body; anything else answers 200 with "pong".
    /// </summary>
    public sealed class LoopbackServer : IDisposable
    {
        private readonly HttpListener _listener = new();

        public LoopbackServer()
        {
            // Let the OS pick a free port, release it, then ask HttpListener to use it.
            var probe = new TcpListener(IPAddress.Loopback, 0);
            probe.Start();
            var port = ((IPEndPoint)probe.LocalEndpoint).Port;
            probe.Stop();

            BaseUrl = $"http://127.0.0.1:{port}/";
            _listener.Prefixes.Add(BaseUrl);
            _listener.Start();
            _ = Task.Run(Serve);
        }

        public string BaseUrl { get; }

        private async Task Serve()
        {
            while (_listener.IsListening)
            {
                HttpListenerContext context;
                try
                {
                    context = await _listener.GetContextAsync();
                }
                catch (Exception)
                {
                    return; // listener stopped
                }

                string reply;
                if (context.Request.HttpMethod == "POST")
                {
                    using var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding);
                    reply = "pong:" + await reader.ReadToEndAsync();
                    context.Response.StatusCode = 201;
                }
                else
                {
                    reply = "pong";
                    context.Response.StatusCode = 200;
                }

                var bytes = Encoding.UTF8.GetBytes(reply);
                context.Response.ContentType = "text/plain; charset=utf-8";
                context.Response.ContentLength64 = bytes.Length;
                await context.Response.OutputStream.WriteAsync(bytes);
                context.Response.Close();
            }
        }

        public void Dispose() => _listener.Close();
    }
}
