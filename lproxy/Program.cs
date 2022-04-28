// See https://aka.ms/new-console-template for more information

using CommandLine;
using CommandLine.Text;
using lproxy;
using ProxyLoginAutomator;
using System.Net;

#if DEBUG
//args = new string[] { "-p", "6789" };
//args = new string[] { "-p", "6789" "--proxy-server","ip:port:usr:pwd" };
args = File.ReadAllLines("cmd.txt")[0].Split(' ');
#endif

ServicePointManager.SecurityProtocol =
                SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11
                | SecurityProtocolType.Tls;

ServicePointManager.ServerCertificateValidationCallback +=
     (sender, certificate, chain, errors) =>
     {
         return true;
     };

LProxy proxy = new();
proxy.Run(args);
