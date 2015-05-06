using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ArchiveServer
{
    public static class Program
    {
        static bool gui = true;

        [STAThread]
        public static void Main( string[] args )
        {
            for (int i = 0; i < args.Length; i++)
            {
                Match match = Regex.Match( args[i],
                    @"^[/\-](?<Option>[\w\d]+)([=:](?<Argument>.*))?$" );
                if (match.Success)
                {
                    switch( match.Groups["Option"].Value.ToUpper() )
                    {
                        case "NOGUI":
                            gui = false;
                            break;
                    }
                }
                else
                {
                    Console.WriteLine("Unrecognized option: " + args[i]);
                }
            }

            if ( gui )
            {
                App.Main();
            }
            else
            {
                Console.CancelKeyPress += Console_CancelKeyPress;
                Console.WriteLine("Starting server, press Ctrl+C to stop.");
                // TODO
            }
        }

        static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            // TODO
        }
    }
}
