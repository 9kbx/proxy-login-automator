using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lproxy
{
    public class CommandlineOptions
    {
        [Option('p', "local-port", HelpText = "local proxy port", Required = false)]
        public int LocalPort { get; set; }

        [Option("proxy-server", HelpText = "one remote proxy server, format:ip:port:user:pwd", Required = false)]
        public string? ProxyServer { get; set; }

        //[Option("proxy-server-src", HelpText = "remote proxy list from local file or http url, format:local port:ip:port:user:pwd, allow multiple lines", Required = false)]
        //public string? ProxyServerSrc { get; set; }

        [Option("enable-whitelist", Default = false, HelpText = "only allow whitelist client connect", Required = false)]
        public bool EnableWhitelist { get; set; }

        [Option("log", Default = true, HelpText = "show debug log", Required = false)]
        public bool ShowLog { get; set; }
    }
}
