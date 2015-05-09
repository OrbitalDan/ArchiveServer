using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace ArchiveServer
{
    public static class Program
    {
        private static ManualResetEvent EndProgram = new ManualResetEvent(false);

        public static bool   HasGui { get; private set; }
        public static int    Port   { get; private set; }
        public static string Host   { get; private set; }

        private static ArchiveContentServer _server;
        public  static ArchiveContentServer Server
        {
            get         { return _server;  }
            private set { _server = value; }
        }

        [DllImport("kernel32")]
        private static extern bool AllocConsole();

        [DllImport("kernel32", SetLastError = true)]
        private static extern bool FreeConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AttachConsole(uint dwProcessId);

        private const uint ATTACH_PARENT_PROCESS = 0xFFFFFFFF;
        private const int  ERROR_ACCESS_DENIED   = 5;

        [STAThread]
        public static void Main( string[] args )
        {
            // Key: Root, Value: Archive File
            var specified = new List<KeyValuePair<string, string>>();

            // To catch errors until we can allocate a console
            var output = new StringBuilder();

            for (int i = 0; i < args.Length; i++)
            {
                Match match = Regex.Match( args[i],
                    @"^[/\-](?<Option>[\w\d]+)([=:](?<Argument>.*))?$" );
                if (match.Success)
                {
                    switch( match.Groups["Option"].Value.ToUpper() )
                    {
                        case "NOGUI":
                            HasGui = false;
                            break;

                        case "PORT":
                            int port = 0;
                            if (Int32.TryParse(match.Groups["Argument"].Value, out port))
                            {
                                Port = port;
                            }
                            else
                            {
                                output.AppendLine(String.Format(
                                    "Error: Bad port specifier \"{0}\"",
                                    match.Groups["Argument"].Value));
                            }
                            break;

                        case "HOST":
                            Host = match.Groups["Argument"].Value;
                            // TODO: Validate
                            break;

                        case "ARCHIVE":
                            if (i+2 < args.Length)
                            {
                                var root = args[i+1];
                                var rootMatch = Regex.Match(root, @"^(/[\w\d_]*)+/?$"); // Valid hosting path?
                                if (!rootMatch.Success)
                                {
                                    output.AppendLine(String.Format(
                                        "Error: Archive mount root specification is invalid: \"{0}\"", root));
                                    break;
                                }

                                var loc = args[i+2].Trim(new char[] { '\"' });
                                var locMatch = Regex.Match(loc, @"^[A-Z]:\\"); // Absolute path?
                                if (locMatch.Success)
                                {
                                    if ( !System.IO.File.Exists( loc ))
                                    {
                                        output.AppendLine(String.Format(
                                            "Error: Cannot find archive file: \"{0}\"", loc));
                                        break;
                                    }
                                }
                                else
                                {
                                    var absloc = System.IO.Path.Combine(
                                        Environment.CurrentDirectory, loc);
                                    if (!System.IO.File.Exists(absloc))
                                    {
                                        output.AppendLine(String.Format(
                                            "Error: Cannot find archive file: \"{0}\" ({1})", loc, absloc));
                                        break;
                                    }
                                    loc = absloc;
                                }

                                i += 2;
                                specified.Add(new KeyValuePair<string, string>(root, loc));
                            }
                            else
                            {
                                output.AppendLine("Error: Expected archive specification");
                            }
                            break;
                    }
                }
                else
                {
                    output.AppendLine( String.Format(
                        "Error: Unrecognized option \"{0}\"", args[i] ));
                }
            }

            Server = new ArchiveContentServer( new string[] { 
                String.Format( "http://{0}:{1}/", Host, Port ) } );

            foreach (var pair in specified)
            {
                bool success = false;
                ArchiveContent archive = null;
                try
                {
                    archive = new ArchiveContent(pair.Value, pair.Key);
                    success = true;
                }
                catch
                {
                    output.AppendLine(String.Format(
                        "Error: Failed to open archive \"{0}\"", pair.Value));
                }

                if ( success )
                {
                    Server.Archives.Add(archive.Root, archive);
                    Server.Precedence.Add(new KeyValuePair<string, ArchiveContent>(
                        archive.Root, archive));
                }
            }

            if (HasGui)
            {
                Server.Run();
                App.Main();
            }
            else
            {
                if (!AttachConsole(ATTACH_PARENT_PROCESS) && Marshal.GetLastWin32Error() == ERROR_ACCESS_DENIED)
                {
                    if (!AllocConsole())
                    {
                        MessageBox.Show("Error: Console allocation failed!");
                    }
                }
                Console.CancelKeyPress += Console_CancelKeyPress;

                // Flush out any messages we've been saving
                Console.WriteLine(output.ToString());
                Console.WriteLine("Starting server, press Ctrl+C to stop.");

                Server.Run();
                EndProgram.WaitOne();
                Server.Stop();
            }
        }

        static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            EndProgram.Reset();
        }
    }
}
