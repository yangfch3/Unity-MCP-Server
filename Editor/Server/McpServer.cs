using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using UnityEngine;

namespace UnityMcp.Editor
{
    /// <summary>
    /// MCP 服务核心。在后台线程运行 HttpListener，接收 JSON-RPC 请求并分发处理。
    /// 仅监听 localhost，POST 请求交给 JsonRpcDispatcher，GET 返回 405。
    /// </summary>
    public class McpServer
    {
        private HttpListener _httpListener;
        private Thread _listenerThread;
        private readonly ToolRegistry _toolRegistry;
        private readonly MainThreadQueue _mainThreadQueue;
        private JsonRpcDispatcher _dispatcher;

        private volatile bool _isRunning;
        private volatile int _connectedAgents;
        private int _port;
        private string _lastError;

        public bool IsRunning => _isRunning;
        public int Port => _port;
        public int ConnectedAgents => _connectedAgents;
        public string LastError => _lastError;

        public McpServer(ToolRegistry toolRegistry, MainThreadQueue mainThreadQueue)
        {
            _toolRegistry = toolRegistry ?? throw new ArgumentNullException(nameof(toolRegistry));
            _mainThreadQueue = mainThreadQueue ?? throw new ArgumentNullException(nameof(mainThreadQueue));
        }

        /// <summary>启动 HttpListener，在后台线程监听指定端口。</summary>
        public void Start(int port)
        {
            if (_isRunning)
                return;

            _lastError = null;
            _port = port;
            _dispatcher = new JsonRpcDispatcher(_toolRegistry, _mainThreadQueue);

            try
            {
                _httpListener = new HttpListener();
                _httpListener.Prefixes.Add($"http://localhost:{port}/");
                _httpListener.Start();
            }
            catch (HttpListenerException ex)
            {
                _lastError = $"Port {port} conflict: {ex.Message}";
                Debug.LogError($"[McpServer] {_lastError}");
                _httpListener = null;
                return;
            }

            _isRunning = true;
            _connectedAgents = 0;

            _listenerThread = new Thread(ListenLoop)
            {
                IsBackground = true,
                Name = "McpServer-Listener"
            };
            _listenerThread.Start();

            Debug.Log($"[McpServer] Started on http://localhost:{port}/");
        }

        /// <summary>停止监听，释放线程和端口资源。</summary>
        public void Stop()
        {
            if (!_isRunning)
                return;

            _isRunning = false;

            try
            {
                _httpListener?.Stop();
                _httpListener?.Close();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[McpServer] Error stopping listener: {ex.Message}");
            }

            _httpListener = null;
            _connectedAgents = 0;
            _lastError = null;

            Debug.Log("[McpServer] Stopped.");
        }

        private void ListenLoop()
        {
            while (_isRunning)
            {
                HttpListenerContext ctx;
                try
                {
                    ctx = _httpListener.GetContext();
                }
                catch (HttpListenerException)
                {
                    // Listener was stopped
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }

                try
                {
                    HandleRequest(ctx);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[McpServer] Unhandled error in HandleRequest: {ex.Message}");
                }
            }
        }

        private void HandleRequest(HttpListenerContext ctx)
        {
            var response = ctx.Response;

            // CORS headers
            response.Headers.Set("Access-Control-Allow-Origin", "*");
            response.Headers.Set("Access-Control-Allow-Methods", "POST, GET, OPTIONS");
            response.Headers.Set("Access-Control-Allow-Headers", "Content-Type");

            // Handle CORS preflight
            if (ctx.Request.HttpMethod == "OPTIONS")
            {
                response.StatusCode = 204;
                response.Close();
                return;
            }

            // Only POST is supported for JSON-RPC; GET returns 405
            if (ctx.Request.HttpMethod != "POST")
            {
                response.StatusCode = 405;
                response.ContentType = "text/plain";
                var body = Encoding.UTF8.GetBytes("Method Not Allowed");
                response.ContentLength64 = body.Length;
                response.OutputStream.Write(body, 0, body.Length);
                response.Close();
                return;
            }

            // POST: read body, dispatch, write response
            Interlocked.Increment(ref _connectedAgents);
            try
            {
                string requestBody;
                using (var reader = new StreamReader(ctx.Request.InputStream, ctx.Request.ContentEncoding))
                {
                    requestBody = reader.ReadToEnd();
                }

                var resultTask = _dispatcher.Dispatch(requestBody);
                // Dispatch is async but we're on a background thread, safe to block
                resultTask.Wait();
                var responseJson = resultTask.Result;

                response.StatusCode = 200;
                response.ContentType = "application/json";
                var responseBytes = Encoding.UTF8.GetBytes(responseJson);
                response.ContentLength64 = responseBytes.Length;
                response.OutputStream.Write(responseBytes, 0, responseBytes.Length);
            }
            catch (ThreadAbortException)
            {
                // Mono-specific: Domain Reload aborts background threads via Thread.Abort().
                // On .NET CoreCLR (Unity 6+) this exception is no longer thrown;
                // remove this catch block when migrating away from Mono runtime.
                Debug.Log("[McpServer] Request aborted due to Domain Reload, service will auto-recover.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[McpServer] Error processing request: {ex.Message}");
                response.StatusCode = 500;
                response.ContentType = "application/json";
                var errorJson = "{\"jsonrpc\":\"2.0\",\"id\":null,\"error\":{\"code\":-32603,\"message\":\"Internal error\"}}";
                var errorBytes = Encoding.UTF8.GetBytes(errorJson);
                response.ContentLength64 = errorBytes.Length;
                response.OutputStream.Write(errorBytes, 0, errorBytes.Length);
            }
            finally
            {
                Interlocked.Decrement(ref _connectedAgents);
                response.Close();
            }
        }
    }
}
