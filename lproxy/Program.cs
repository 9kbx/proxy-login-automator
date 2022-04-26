// See https://aka.ms/new-console-template for more information

using CommandLine;
using CommandLine.Text;
using lproxy;
using ProxyLoginAutomator;

#if DEBUG
//args = new string[] { "-p", "6789" };
args = File.ReadAllLines("cmd.txt")[0].Split(' ');
#endif

LProxy proxy = new();
proxy.Run(args);
