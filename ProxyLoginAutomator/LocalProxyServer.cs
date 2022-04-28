using System.Collections.Concurrent;
using System.Net;
using System.Text;
using Titanium.Web.Proxy;
using Titanium.Web.Proxy.EventArguments;
using Titanium.Web.Proxy.Exceptions;
using Titanium.Web.Proxy.Models;

namespace ProxyLoginAutomator
{
    public class LocalProxyServer
    {
        bool decryptSsl = true;
        public bool showDebugLog { get; set; } = true;
        public bool onlyAllowWhitelist { get; set; } = false;
        /// <summary>
        /// remote proxies
        /// <para>int=local port, IExternalProxy=remote proxy</para>
        /// <para>client > local port > remote proxy > website</para>
        /// </summary>
        ConcurrentDictionary<int, IExternalProxy> upStreamHttpProxies { get; set; } = new ConcurrentDictionary<int, IExternalProxy>();

        public ProxyServer proxyServer { get; set; } = new ProxyServer();

        public LocalProxyServer(bool showDebugLog = true, bool onlyAllowWhitelist = false)
        {
            this.showDebugLog = showDebugLog;
            this.onlyAllowWhitelist = onlyAllowWhitelist;

            proxyServer.EnableHttp2 = true;
            proxyServer.TcpTimeWaitSeconds = 10;
            proxyServer.ConnectionTimeOutSeconds = 15;
            proxyServer.ReuseSocket = true;
            proxyServer.EnableConnectionPool = false;
            proxyServer.ForwardToUpstreamGateway = false;
            proxyServer.CertificateManager.SaveFakeCertificates = true;

            //proxyServer.ProxyBasicAuthenticateFunc = async (args, userName, password) =>
            //{
            //    await Task.Delay(0);
            //    return true;
            //};

            proxyServer.ServerCertificateValidationCallback += OnServerCertificateValidation;
            
            proxyServer.BeforeRequest += OnBeforeRequest;
            //proxyServer.BeforeResponse += OnResponse;
            //proxyServer.AfterResponse+= OnResponse;
            proxyServer.ExceptionFunc = OnExceptionFunc;
            //proxyServer.GetCustomUpStreamProxyFunc = onGetCustomUpStreamProxyFunc;
            //proxyServer.CustomUpStreamProxyFailureFunc = onCustomUpStreamProxyFailureFunc;

            //proxyServer.GetCustomUpStreamProxyFunc = async (ev) =>
            //{
            //    UpStreamHttpProxies.TryGetValue(ev.ProxyEndPoint.Port, out var x);
            //    await Task.CompletedTask;
            //    return x;
            //};
            //proxyServer.CustomUpStreamProxyFailureFunc = async (ev) =>
            //{
            //    UpStreamHttpProxies.TryGetValue(ev.ProxyEndPoint.Port, out var x);
            //    await Task.CompletedTask;
            //    return x;
            //};

            //if (RunTime.IsWindows)
            //{
            //    proxyServer.SetAsSystemProxy(explicitEndPoint, ProxyProtocolType.AllHttp);
            //}
        }

        async Task<IExternalProxy> onGetCustomUpStreamProxyFunc(SessionEventArgsBase ev)
        {
            upStreamHttpProxies.TryGetValue(ev.ProxyEndPoint.Port, out var x);
            //await Task.CompletedTask;
            await Task.Delay(0);
            return x;
        }

        private async Task<IExternalProxy> onCustomUpStreamProxyFailureFunc(SessionEventArgsBase ev)
        {
            upStreamHttpProxies.TryGetValue(ev.ProxyEndPoint.Port, out var x);
            //await Task.CompletedTask;
            await Task.Delay(0);
            return x;
        }

        void Start()
        {
            if (!proxyServer.ProxyRunning) proxyServer.Start();
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
            if (!string.IsNullOrWhiteSpace(remoteHost) && !upStreamHttpProxies.ContainsKey(localPort))
            {
                AddEndPoint(localPort);

                //UpStreamHttpProxies.TryAdd(localPort, new ExternalProxy
                //{
                //    HostName = remoteHost,
                //    Port = remotePort,
                //    Password = user,
                //    UserName = pwd,
                //    BypassLocalhost = true,
                //    UseDefaultCredentials = false,

                //});
                var upStreamProxy = new ExternalProxy(remoteHost, remotePort, user, pwd) { ProxyType = ExternalProxyType.Http };
                
                upStreamHttpProxies.TryAdd(localPort, upStreamProxy);
            }
        }
        public void AddEndPoint(int localPort)
        {
            var tcpProxy = new ExplicitProxyEndPoint(IPAddress.Any, localPort, decryptSsl);
            tcpProxy.BeforeTunnelConnectRequest += onBeforeTunnelConnectRequest;
            proxyServer.AddEndPoint(tcpProxy);

            Console.WriteLine("LocalProxyServer Listening on '{0}' endpoint at Ip {1} and port: {2} ",
                tcpProxy.GetType().Name, tcpProxy.IpAddress, tcpProxy.Port);
        }

        public void Stop()
        {
            proxyServer.BeforeRequest -= OnBeforeRequest;
            proxyServer.ServerCertificateValidationCallback -= OnServerCertificateValidation;

            proxyServer.Stop();
        }

        public Dictionary<int, string> GetUpStreamProxies()
        {
            var dic = new Dictionary<int, string>();
            foreach (var localPort in upStreamHttpProxies.Keys)
            {
                if (upStreamHttpProxies.TryGetValue(localPort, out var px))
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

            //if (ev.SslPolicyErrors == System.Net.Security.SslPolicyErrors.None)
            //    ev.IsValid = true;

            await Task.CompletedTask;
        }

        async Task onBeforeTunnelConnectRequest(object sender, TunnelConnectSessionEventArgs e)
        {
            var clientLocalPort = e.ClientLocalEndPoint.Port;
            if (upStreamHttpProxies.TryGetValue(clientLocalPort, out var x))
            {
                e.CustomUpStreamProxy = x;
            }
            await Task.CompletedTask;
        }
        async Task OnBeforeRequest(object sender, SessionEventArgs ev)
        {
            //ev.CustomUpStreamProxy = GetCustomUpStreamProxy(ev);

            var request = ev.HttpClient.Request;

            if (onlyAllowWhitelist)
            {
                // is white proxy > allow
                // else > deny
            }

            // onBeforeTunnelConnectRequest set CustomUpStreamProxy
            Console.WriteLine($"ev.CustomUpStreamProxy is null [ {ev.CustomUpStreamProxy == null} ]");

            var log = new StringBuilder();
            var cep = ev.ClientRemoteEndPoint;

            if (upStreamHttpProxies.TryGetValue(ev.ProxyEndPoint.Port, out var x))
            {
                log.Append($"client[{ev.ProxyEndPoint.Port}][{cep.Address.ToString()}:{cep.Port}] > proxy[{x.HostName}:{x.Port}] ");
                ev.CustomUpStreamProxy = x;
            }
            else
            {
                log.Append($"client[{ev.ProxyEndPoint.Port}][{cep.Address.ToString()}:{cep.Port}] > direct ");
            }

            log.Append($"> {request.Method} {request.Url}");

            ShowLog(log.ToString());

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
                if (exception.InnerException != null)
                {
                    ShowLog($"InnerException: {exception.InnerException.Message}");
                }
            }
        }

        void ShowLog(string log)
        {
            if (showDebugLog) Console.WriteLine($"[{DateTime.Now}]{log}");
        }
    }
}