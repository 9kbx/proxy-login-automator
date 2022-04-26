using System.Collections.Concurrent;
using System.Net;
using Titanium.Web.Proxy;
using Titanium.Web.Proxy.EventArguments;
using Titanium.Web.Proxy.Exceptions;
using Titanium.Web.Proxy.Models;

namespace ProxyLoginAutomator
{
    public class LocalProxyServer
    {
        //int DefaultLocalPort { get; set; } = 10010;
        //string RemoteHost { get; set; }
        //int RemotePort { get; set; }
        //string RemoteUser { get; set; }
        //string RemotePwd { get; set; }

        public bool ShowDebugLog { get; set; } = true;

        /// <summary>
        /// remote proxies[client > local port > remote proxy > website]
        /// </summary>
        ConcurrentDictionary<int, IExternalProxy> UpStreamHttpProxies { get; set; } = new ConcurrentDictionary<int, IExternalProxy>();

        public ProxyServer ProxyServer { get; set; } = new ProxyServer();

        public LocalProxyServer(bool showDebugLog = true)
        {
            ShowDebugLog = showDebugLog;
            ProxyServer.ServerCertificateValidationCallback += OnServerCertificateValidation;
            ProxyServer.BeforeRequest += OnBeforeRequest;
            ProxyServer.BeforeResponse += OnResponse;
            ProxyServer.AfterResponse+= OnResponse;
            ProxyServer.ExceptionFunc = OnExceptionFunc;

            
        }


        void Start()
        {
            if (!ProxyServer.ProxyRunning) ProxyServer.Start();
        }
        public void Start(int localPort, string remoteHost, int remotePort, string user = "", string pwd = "")
        {
            if (!string.IsNullOrWhiteSpace(remoteHost))
            {
                AddEndPoint(localPort, remoteHost, remotePort, user, pwd);
            }
            else
            {
                AddEndPoint(localPort);
            }
            Start();
        }

        public void Start(int localPort, string proxyServer)
        {
            if (!string.IsNullOrWhiteSpace(proxyServer))
            {
                AddEndPoint(localPort, proxyServer);
            }
            else
            {
                AddEndPoint(localPort);
            }
            Start();
        }

        public void AddEndPoint(int localPort, string proxyServer)
        {
            if (!string.IsNullOrWhiteSpace(proxyServer) && proxyServer.IndexOf(":") != -1)
            {
                var args = proxyServer.Split(":".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                var host = args[0];
                var port = Convert.ToInt32(args[1]);
                var user = string.Empty;
                var pwd = string.Empty;
                if (args.Length >= 4)
                {
                    user = args[2];
                    pwd = args[3];
                }

                AddEndPoint(localPort, host, port, user, pwd);
            }
            else
            {
                Console.WriteLine($"proxy server format error: {proxyServer}\r\nfotmat: ip:port or ip:port:user:pwd");
            }
        }
        public void AddEndPoint(int localPort, string remoteHost, int remotePort, string user = "", string pwd = "")
        {
            //if (!string.IsNullOrWhiteSpace(remoteHost))
            if (!string.IsNullOrWhiteSpace(remoteHost) && !UpStreamHttpProxies.ContainsKey(localPort))
            {
                AddEndPoint(localPort);

                var upStreamProxy = new ExternalProxy(remoteHost, remotePort, user, pwd) { ProxyType = ExternalProxyType.Http };
                UpStreamHttpProxies.TryAdd(localPort, upStreamProxy);
            }
        }
        public void AddEndPoint(int localPort)
        {
            // linux: false , because cannot be decrypt ssl under linux
            // win: true or false
            var decryptSsl = false;
            //var tcpProxy = new ExplicitProxyEndPoint(IPAddress.Any, localPort, decryptSsl);
            var tcpProxy = new ExplicitProxyEndPoint(IPAddress.Any, localPort, decryptSsl);
            ProxyServer.AddEndPoint(tcpProxy);

            Console.WriteLine("LocalProxyServer Listening on '{0}' endpoint at Ip {1} and port: {2} ",
                tcpProxy.GetType().Name, tcpProxy.IpAddress, tcpProxy.Port);
        }

        public void Stop()
        {
            ProxyServer.BeforeRequest -= OnBeforeRequest;
            ProxyServer.ServerCertificateValidationCallback -= OnServerCertificateValidation;

            ProxyServer.Stop();
        }

        public Dictionary<int, string> GetUpStreamProxies()
        {
            var dic = new Dictionary<int, string>();
            foreach (var localPort in UpStreamHttpProxies.Keys)
            {
                if (UpStreamHttpProxies.TryGetValue(localPort, out var px))
                {
                    dic.Add(localPort, $"{px.HostName}:{px.Port}");
                }
            }
            return dic;
        }

        async Task OnServerCertificateValidation(object sender, CertificateValidationEventArgs ev)
        {
            // If our destination server has only the domain name in the certificate, we might check it
            // or simply don't care (FOR DEVELOPMENT ONLY).
            ev.IsValid = true;
            await Task.CompletedTask;
        }

        async Task OnBeforeRequest(object sender, SessionEventArgs ev)
        {
            var request = ev.HttpClient.Request;
            var cep = ev.ClientEndPoint;

#if DEBUG

            //ShowLog($"ClientEndPoint.Port\t{ev.ClientEndPoint.Port}");
            //ShowLog($"ClientLocalEndPoint.Port\t{ev.ClientLocalEndPoint.Port}");
            //ShowLog($"ClientRemoteEndPoint.Port\t{ev.ClientRemoteEndPoint.Port}");
            //ShowLog($"LocalEndPoint.Port\t{ev.LocalEndPoint.Port}");
            //ShowLog($"LocalEndPoint.Port\t{ev.ProxyEndPoint.Port}");
#endif

            if (UpStreamHttpProxies.TryGetValue(ev.ProxyEndPoint.Port, out var x))
            {
                ShowLog($"client[{ev.ProxyEndPoint.Port}][{cep.Address.ToString()}:{cep.Port}] > proxy[{x.HostName}:{x.Port}]");
                ev.CustomUpStreamProxy = x;
            }
            else
            {
                ShowLog($"client[{ev.ProxyEndPoint.Port}][{cep.Address.ToString()}:{cep.Port}] > direct");
            }

            ShowLog($"{request.Method} {request.Url}");

            await Task.CompletedTask;
        }
        async Task OnResponse(object sender, SessionEventArgs ev)
        {
            //Console.WriteLine(ev.HttpClient.Request.HeaderText);
            //Console.WriteLine(ev.HttpClient.Response.HeaderText);

            await Task.CompletedTask;
        }
        void OnExceptionFunc(Exception exception)
        {
            if (exception is ProxyHttpException phex)
            {
                // or log it to file/anywhere
                ShowLog("ex: " + exception.Message + ": " + phex.InnerException?.Message);
            }
            else
            {
                ShowLog($"ex: {exception.Message}");
            }
        }

        void ShowLog(string log)
        {
            if (ShowDebugLog) Console.WriteLine($"[{DateTime.Now}]{log}");
        }
    }
}