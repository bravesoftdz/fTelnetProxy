﻿// TODO Check for code that needs to be rewritten
using RandM.RMLib;
using System;
using System.IO;
using System.Reflection;
using System.Text;

namespace RandM.fTelnetProxy
{
    public class fTelnetProxy : IDisposable
    {
        private FileStream _LogStream = null;
        private object _LogStreamLock = new object();

        WebSocketServerThread _WebSocketServer = null;

        public fTelnetProxy()
        {
            _LogStream = new FileStream(Path.Combine(ProcessUtils.StartupPath, "fTelnetProxy.log"), FileMode.Append, FileAccess.Write, FileShare.Read);
            RMLog.Handler += RMLog_Handler;

            if (Config.Default.Loaded | ParseCommandLineArgs())
            {
                RMLog.Info("fTelnetProxy Starting Up");
                RMLog.Info("Starting WebSocket Proxy Thread");
                try
                {
                    _WebSocketServer = new WebSocketServerThread("0.0.0.0", Config.Default.ListenPort); // TODO wss://
                    _WebSocketServer.Start();
                }
                catch (Exception ex)
                {
                    RMLog.Exception(ex, "Failed to start WebSocket Proxy Thread");
                    _WebSocketServer = null;
                }
            }
            else
            {
                ShowHelp();
            }
        }

        public void Dispose()
        {
            RMLog.Info("fTelnetProxy Shutting Down");

            if (_WebSocketServer != null)
            {
                RMLog.Info("Stopping WebSocket Proxy Thread");
                _WebSocketServer.Stop();
            }

            RMLog.Info("fTelnetProxy Terminated\r\n\r\n");

            if (_LogStream != null)
            {
                _LogStream.Write(Encoding.ASCII.GetBytes(Environment.NewLine), 0, Environment.NewLine.Length);
                _LogStream.Close();
                _LogStream.Dispose();
            }
        }

        private bool ParseCommandLineArgs()
        {
            string[] Args = Environment.GetCommandLineArgs();
            for (int i = 1; i < Args.Length; i++)
            {
                // TODO This is cumbersome
                switch (Args[i])
                {
                    case "/c":
                    case "-c":
                    case "/cert":
                    case "--cert":
                        i += 1;
                        Config.Default.CertFilename = Args[i];
                        break;

                    case "/?":
                    case "-?":
                    case "/h":
                    case "-h":
                    case "/help":
                    case "--help":
                        ShowHelp();
                        return false;

                    case "/l":
                    case "-l":
                    case "/loglevel":
                    case "--loglevel":
                        i += 1;
                        try
                        {
                            RMLog.Level = (LogLevel)Enum.Parse(typeof(LogLevel), Args[i]);
                        }
                        catch (Exception)
                        {
                            Console.WriteLine();
                            Console.WriteLine("Invalid loglevel '" + Args[i] + "'");
                            Console.WriteLine();
                            return false;
                        }
                        break;

                    case "/p":
                    case "-p":
                    case "/port":
                    case "--port":
                        i += 1;
                        try
                        {
                            Config.Default.ListenPort = Convert.ToInt16(Args[i]);
                        }
                        catch (Exception)
                        {
                            Console.WriteLine();
                            Console.WriteLine("Invalid port '" + Args[i] + "'");
                            Console.WriteLine();
                            return false;
                        }
                        break;

                    case "/pw":
                    case "-pw":
                    case "/password":
                    case "--password":
                        i += 1;
                        Config.Default.CertPassword = Args[i];
                        break;

                    case "/r":
                    case "-r":
                    case "/relay":
                    case "--relay":
                        i += 1;
                        Config.Default.RelayFilename = Args[i];
                        break;

                    case "/t":
                    case "-t":
                    case "/target":
                    case "--target":
                        i += 1;
                        if (Args[i].Contains(":"))
                        {
                            Config.Default.TargetHostname = Args[i].Split(':')[0];
                            try
                            {
                                Config.Default.TargetPort = Convert.ToInt16(Args[i].Split(':')[1]);
                            }
                            catch (Exception)
                            {
                                Console.WriteLine();
                                Console.WriteLine("Invalid target port '" + Args[i].Split(':')[1] + "'");
                                Console.WriteLine();
                                return false;
                            }
                        }
                        else
                        {
                            Config.Default.TargetHostname = Args[i];
                        }
                        break;

                    default:
                        RMLog.Error("Unknown parameter: '" + Args[i] + "'");
                        return false;
                }
            }

            return true;
        }

        void RMLog_Handler(object sender, RMLogEventArgs e)
        {
            string Message = string.Format("[{0}] [{1}] {2}\r\n",
                DateTime.Now.ToString(),
                e.Level.ToString(),
                e.Message);

            byte[] MessageBytes = Encoding.ASCII.GetBytes(Message);
            _LogStream.Write(MessageBytes, 0, MessageBytes.Length);
            _LogStream.Flush();

            if (Environment.UserInteractive) Console.Write(Encoding.ASCII.GetString(MessageBytes));
        }

        private void ShowHelp()
        {
            if (Environment.UserInteractive)
            {
                Console.WriteLine();
                Console.WriteLine("Usage: " + Path.GetFileName(ProcessUtils.ExecutablePath) + " [parameters]");
                Console.WriteLine();
                Console.WriteLine("Service-mode parameters:");
                Console.WriteLine();
                Console.WriteLine("  /i, -i, /install, --install       Install the service");
                Console.WriteLine();
                Console.WriteLine("  /u, -u, /uninstall, --uninstall   Uninstall the service"); 
                Console.WriteLine();
                Console.WriteLine("  Edit the " + Path.GetFileNameWithoutExtension(ProcessUtils.ExecutablePath) + ".ini file to configure");
                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine("Console-mode parameters:");
                Console.WriteLine();
                Console.WriteLine("  -p <port>                  Port to listen for connections on");
                Console.WriteLine("  --port <port>              Default is 1123");
                Console.WriteLine();
                Console.WriteLine("  -t <host:port>             Telnet server to redirect to");
                Console.WriteLine("  --target <host:port>       Default is localhost:23");
                Console.WriteLine();
                Console.WriteLine("  -c <filename>              PKCS12 file containing private key and cert chain");
                Console.WriteLine("  --cert <filename>          Needed if your site uses https://");
                Console.WriteLine();
                Console.WriteLine("  -pw <password>             Password to open the PKCS12 file");
                Console.WriteLine("  --password <password>      Needed if your PKCS12 file is password protected");
                Console.WriteLine();
                Console.WriteLine("  -l <level>                 Log level (Trace, Debug, Info, Warning, Error)");
                Console.WriteLine("  --loglevel <level>         Default is Info");
                Console.WriteLine();
                Console.WriteLine("  -?, -h, --help             Display this screen");
                Console.WriteLine();
                //Console.WriteLine("345678901234567890123456789012345678901234567890123456789012345678901234567890");
                Environment.Exit(1);
            }
        }
    }
}
