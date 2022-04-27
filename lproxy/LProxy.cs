using CommandLine;
using CommandLine.Text;
using ProxyLoginAutomator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lproxy
{
    public class LProxy
    {
        LocalProxyServer? Server { get; set; }

        public void Run(string[] args)
        {
            ParserCommandlineOptions(args, RunProxy);
        }

        void RunProxy(CommandlineOptions options)
        {
            Server = new LocalProxyServer(options.ShowLog, options.EnableWhitelist);

            Server.Start(options.LocalPort, options.ProxyServer);

            WaitCommand();

            Server.Stop();
        }

        void WaitCommand()
        {
            var stopCmd = "exit";

            Console.WriteLine("enter 'exit' to close app");

            string command;
            while ((command = Console.ReadLine()) != stopCmd)
            {
                if (!string.IsNullOrWhiteSpace(command))
                {
                    var args = command.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                    if (args.Length > 0)
                    {
                        var cmd = args[0].ToLower();

                        switch (cmd)
                        {
                            case "help": ShowCmdHelp(); break;
                            case "add": ParserCommandlineOptions(args, AddNewEndPoint); break;
                            case "list": ShowProxies(); break;
                            case "hide": HideLog(); break;
                            case "show": HideLog(false); break;
                            case "clear": Console.Clear(); break;
                        }
                    }
                }
            }
        }

        void ParserCommandlineOptions(string[] args, Action<CommandlineOptions> act)
        {
            var parserResult = Parser.Default.ParseArguments<CommandlineOptions>(args);
            parserResult.WithParsed<CommandlineOptions>(options => act(options));

            parserResult.WithNotParsed<CommandlineOptions>(errs =>
            {
                var helpText = HelpText.AutoBuild(parserResult, h =>
                {
                    // Configure HelpText here  or create your own and return it 
                    h.AdditionalNewLineAfterOption = false;
                    return HelpText.DefaultParsingErrorsHandler(parserResult, h);
                }, e =>
                {
                    return e;
                });
                Console.Error.Write(helpText);
            });
        }

        void AddNewEndPoint(CommandlineOptions options)
        {
            Server.AddEndPoint(options.LocalPort, options.ProxyServer);
        }

        void ShowProxies()
        {
            var dic = Server.GetUpStreamProxies();
            foreach (var localPort in dic.Keys)
            {
                Console.WriteLine($"local port:{localPort}\tremote:{dic[localPort]}");
            }
        }

        void ShowCmdHelp()
        {
            var parserResult = Parser.Default.ParseArguments<CommandlineOptions>(new string[] { });
            var helpText = HelpText.AutoBuild(parserResult, h =>
            {
                // Configure HelpText here  or create your own and return it 
                h.AdditionalNewLineAfterOption = false;
                return HelpText.DefaultParsingErrorsHandler(parserResult, h);
            }, e =>
            {
                return e;
            });
            Console.Error.Write(helpText);
        }

        //void HideLog(CommandlineOptions options)
        //{
        //    Server.ShowDebugLog = options.ShowLog;
        //}
        void HideLog(bool hide = true)
        {
            Server.ShowDebugLog = !hide;
        }
    }
}
