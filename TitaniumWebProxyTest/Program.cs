// See https://aka.ms/new-console-template for more information
using System.Net;
using Titanium.Web.Proxy;
using Titanium.Web.Proxy.EventArguments;
using Titanium.Web.Proxy.Exceptions;
using Titanium.Web.Proxy.Models;


// 简易流程
// 浏览器 > TitanProxy > 上游代理 > 目标网站

// 严格流程
// 检测上游代理是否可用，可用：启用TitanProxy
// 检测TitanProxy是否可用，可用：启用浏览器（命令行: chrome.exe --proxy-server=TitanProxy）
// 以上每一步不可用时抛出异常


var proxyServer = new ProxyServer();

proxyServer.ServerCertificateValidationCallback += OnServerCertificateValidation;

proxyServer.BeforeRequest += OnBeforeRequest;
proxyServer.ExceptionFunc = async exception =>
{
    if (exception is ProxyHttpException phex)
    {
        // or log it to file/anywhere
        Console.WriteLine("ex: "+exception.Message + ": " + phex.InnerException?.Message);
    }
    else
    {
        Console.WriteLine($"ex: {exception.Message}");
    }
};

var tcpProxy = new ExplicitProxyEndPoint(IPAddress.Any, 6789, true);

// 上游代理
proxyServer.UpStreamHttpProxy = new ExternalProxy("111.1.1.1", 12345, "admin", "123");
proxyServer.UpStreamHttpsProxy = new ExternalProxy("222.2.2.2", 12345, "admin", "123");

proxyServer.AddEndPoint(tcpProxy);
proxyServer.Start();

string command;
while ((command = Console.ReadLine()) != "quit")
{
    if (command == "clear")
    {
        //
    }
}

proxyServer.BeforeRequest -= OnBeforeRequest;

proxyServer.Stop();

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
    
    Console.WriteLine($"{request.Method} {request.Url}");

    await Task.CompletedTask;
}