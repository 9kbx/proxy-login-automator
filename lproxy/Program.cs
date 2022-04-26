// See https://aka.ms/new-console-template for more information

using CommandLine;
using CommandLine.Text;
using lproxy;
using ProxyLoginAutomator;

#if DEBUG

//args = new string[] { "-p", "6789", "--proxy-server", "192.168.242.38:8800" };
#endif

LProxy proxy = new();
proxy.Run(args);
