

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using Fiddler;
using System.Threading.Tasks;
using Ionic;
using Ionic.Zlib;

namespace Demo
{
    public class Program
    {
        public static bool filewtittebtodisk;
        public static Proxy oSecureEndpoint;
        public static string sSecureEndpointHostname = "localhost";
        public static int iSecureEndpointPort = 7777;

        public static List<Fiddler.Session> oAllSessions;
        public static void WriteCommandResponse(string s)
        {
            //ConsoleColor oldColor = Console.ForegroundColor;
            //Console.ForegroundColor = ConsoleColor.Yellow;
            //Console.WriteLine(s);
            //Console.ForegroundColor = oldColor;
        }
        public static string logpath;
        public static string fiddlerurl;




        public static void DoQuit()
        {

            if (null != oSecureEndpoint) oSecureEndpoint.Dispose();

            Fiddler.FiddlerApplication.Shutdown();

        }


        public static void Activitywrite(string s)
        {
            File.AppendAllText(logpath + $"//log//Activity//" + DateTime.Now.ToString("dd-MM-yyyy-HH") + " Activity.txt", Environment.NewLine + Environment.NewLine + DateTime.Now.ToString() + "   " + s + Environment.NewLine + Environment.NewLine);

        }


        public Program()
        {
            logpath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\PerformanceMonitorUtility";
        }



#if SAZ_SUPPORT
        //public static void ReadSessions(List<Fiddler.Session> oAllSessions)
        //{
        //    Session[] oLoaded = Utilities.ReadSessionArchive(Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
        //                                                   + Path.DirectorySeparatorChar + "ToLoad.saz", false);

        //    if ((oLoaded != null) && (oLoaded.Length > 0))
        //    {
        //        oAllSessions.AddRange(oLoaded);
        //        WriteCommandResponse("Loaded: " + oLoaded.Length + " sessions.");
        //    }
        //}

        public static void SaveSessionsToDesktop(List<Fiddler.Session> oAllSessions)
        {
            Activitywrite("writing saz file");
            bool bSuccess = false;

            if (!Directory.Exists(logpath + "\\Log\\NetworkLog"))
            {
                Directory.CreateDirectory(logpath + "\\Log\\NetworkLog");
            }


            string sFilename = logpath + "\\Log\\NetworkLog\\" + DateTime.Now.ToString("dd-MM-yyyy-HH-mm-ss") + ".saz";


            try
            {
                try
                {
                    Monitor.Enter(oAllSessions);

                    string sPassword = "ECWTECH";


                    bSuccess = Utilities.WriteSessionArchive(sFilename, oAllSessions.ToArray(), sPassword, false);
                }
                finally
                {
                    Monitor.Exit(oAllSessions);
                }

                //WriteCommandResponse(bSuccess ? ("Wrote: " + sFilename) : ("Failed to save: " + sFilename));
            }
#pragma warning disable
            catch (Exception eX)
            {
                //Console.WriteLine("Save failed: " + eX.Message);
            }


            Activitywrite("writing saz file done");
        }
#endif

        public static void WriteSessionList(List<Fiddler.Session> oAllSessions)
        {
            //ConsoleColor oldColor = Console.ForegroundColor;
            //Console.ForegroundColor = ConsoleColor.White;
            //Console.WriteLine("Session list contains...");
            try
            {
                Activitywrite("monitor enter with all session");
                Monitor.Enter(oAllSessions);
                //foreach (Session oS in oAllSessions)
                //{
                //    //Console.Write(String.Format("{0} {1} {2}\n{3} {4}\n\n", oS.id, oS.oRequest.headers.HTTPMethod, Ellipsize(oS.fullUrl, 60), oS.responseCode, oS.oResponse.MIMEType));
                //    File.AppendAllText(drv[0] + "//PerformanceUtility//flog.txt", oS.id + " " + oS.oRequest.headers.HTTPMethod + " " + Ellipsize(oS.fullUrl, 60) + " " + oS.responseCode + " " + oS.oResponse.MIMEType);
                //}
            }
            finally
            {
                Activitywrite("exit monitor");
                Monitor.Exit(oAllSessions);
                //todo : max memory usage check and fix ---- added to check if memory can be reclaimed jaymin mod to check 
                //oAllSessions.Clear();

            }
            //Console.WriteLine();
            //Console.ForegroundColor = oldColor;
            return;
        }

        public static void Main(string[] args)
        {


            Activitywrite("inside main");
            oAllSessions = new List<Fiddler.Session>();


            #region AttachEventListeners


            Activitywrite("inside attach event Listeners");

            Fiddler.FiddlerApplication.OnNotification += delegate (object sender, NotificationEventArgs oNEA) { Console.WriteLine("** NotifyUser: " + oNEA.NotifyString); };
            Fiddler.FiddlerApplication.Log.OnLogString += delegate (object sender, LogEventArgs oLEA) { Console.WriteLine("** LogString: " + oLEA.LogString); };



            Activitywrite("before request delegate");

            Fiddler.FiddlerApplication.BeforeRequest += delegate (Fiddler.Session oS)
            {

                // custom code for specific url host

                if (!string.IsNullOrEmpty(fiddlerurl))
                {
                    if (oS.url.Contains(fiddlerurl))
                    {
                        oS.bBufferResponse = false;
                        Monitor.Enter(oAllSessions);
                        oAllSessions.Add(oS);
                        Monitor.Exit(oAllSessions);

                        if ((oS.oRequest.pipeClient.LocalPort == iSecureEndpointPort) && (oS.hostname == sSecureEndpointHostname))
                        {
                            oS.utilCreateResponseAndBypassServer();
                            oS.oResponse.headers.SetStatus(200, "Ok");
                            oS.oResponse["Content-Type"] = "text/html; charset=UTF-8";
                            oS.oResponse["Cache-Control"] = "private, max-age=0";
                            oS.utilSetResponseBody("<html><body>Request for httpS://" + sSecureEndpointHostname + ":" + iSecureEndpointPort.ToString() + " received. Your request was:<br /><plaintext>" + oS.oRequest.headers.ToString());
                        }
                    }

                }



                //capture all traffic remove below else code
                //else
                //{
                //    oS.bBufferResponse = false;
                //    Monitor.Enter(oAllSessions);
                //    oAllSessions.Add(oS);
                //    Monitor.Exit(oAllSessions);

                //    if ((oS.oRequest.pipeClient.LocalPort == iSecureEndpointPort) && (oS.hostname == sSecureEndpointHostname))
                //    {
                //        oS.utilCreateResponseAndBypassServer();
                //        oS.oResponse.headers.SetStatus(200, "Ok");
                //        oS.oResponse["Content-Type"] = "text/html; charset=UTF-8";
                //        oS.oResponse["Cache-Control"] = "private, max-age=0";
                //        oS.utilSetResponseBody("<html><body>Request for httpS://" + sSecureEndpointHostname + ":" + iSecureEndpointPort.ToString() + " received. Your request was:<br /><plaintext>" + oS.oRequest.headers.ToString());
                //    }

                //}



            };


            Activitywrite("session complete");
            Fiddler.FiddlerApplication.AfterSessionComplete += delegate (Fiddler.Session oS)
            {

            };


            Activitywrite("attach cancel press");
            Console.CancelKeyPress += new ConsoleCancelEventHandler(Console_CancelKeyPress);
            #endregion AttachEventListeners

            string sSAZInfo = "NoSAZ";
#if SAZ_SUPPORT
            Activitywrite("loading saz assembly");
            sSAZInfo = Assembly.GetAssembly(typeof(Ionic.Zip.ZipFile)).FullName;



            Activitywrite("fn obtan");
            DNZSAZProvider.fnObtainPwd = () =>
            {
                //Console.ReadLine();
                string sResult = string.Empty;

                return sResult;
            };


            FiddlerApplication.oSAZProvider = new DNZSAZProvider();



#endif


            Activitywrite("fiddler config");
            Fiddler.CONFIG.IgnoreServerCertErrors = false;


            FiddlerApplication.Prefs.SetBoolPref("fiddler.network.streaming.abortifclientaborts", true);


            ushort iPort = 8877;

            FiddlerCoreStartupSettings startupSettings =
                new FiddlerCoreStartupSettingsBuilder()
                    .ListenOnPort(iPort)
                    .RegisterAsSystemProxy()
                    .DecryptSSL()
                    //.AllowRemoteClients()
                    //.ChainToUpstreamGateway()
                    .MonitorAllConnections()
                    //.HookUsingPACFile()
                    //.CaptureLocalhostTraffic()
                    //.CaptureFTP()
                    .OptimizeThreadPool()
                    //.SetUpstreamGatewayTo("http=CorpProxy:80;https=SecureProxy:443;ftp=ftpGW:20")
                    .Build();

            //FiddlerApplication.oSAZProvider = new DNZSAZProvider();

            // *******************************
            // Important HTTPS Decryption Info
            // *******************************
            // When FiddlerCoreStartupSettingsBuilder.DecryptSSL() is called, you must include either
            //
            //     MakeCert.exe
            //
            // *or*
            //
            //     CertMaker.dll
            //     BCMakeCert.dll
            //
            // ... in the folder where your executable and FiddlerCore.dll live. These files
            // are needed to generate the self-signed certificates used to man-in-the-middle
            // secure traffic. MakeCert.exe uses Windows APIs to generate certificates which
            // are stored in the user's \Personal\ Certificates store. These certificates are
            // NOT compatible with iOS devices which require specific fields in the certificate
            // which are not set by MakeCert.exe. 
            //
            // In contrast, CertMaker.dll uses the BouncyCastle C# library (BCMakeCert.dll) to
            // generate new certificates from scratch. These certificates are stored in memory
            // only, and are compatible with iOS devices.
            Activitywrite("fiddler startup config");
            Fiddler.FiddlerApplication.Startup(startupSettings);

            //FiddlerApplication.Log.LogFormat("Created endpoint listening on port {0}", iPort);

            //FiddlerApplication.Log.LogFormat("Gateway: {0}", CONFIG.UpstreamGateway.ToString());

            //Console.WriteLine("Hit CTRL+C to end session.");

            // We'll also create a HTTPS listener, useful for when FiddlerCore is masquerading as a HTTPS server
            // instead of acting as a normal CERN-style proxy server.

            Activitywrite("end point");
            oSecureEndpoint = FiddlerApplication.CreateProxyEndpoint(iSecureEndpointPort, true, sSecureEndpointHostname);
            if (null != oSecureEndpoint)
            {
                //FiddlerApplication.Log.LogFormat("Created secure endpoint listening on port {0}, using a HTTPS certificate for '{1}'", iSecureEndpointPort, sSecureEndpointHostname);
            }

            Activitywrite("main complete");
        }

        public void GetInput(char c)
        {

            Activitywrite("getting input");
            switch (c)
            {


                case 'l':
                    try
                    {
                        Activitywrite("inside l,  setting trust");
                        var gettrustcertval = Fiddler.CertMaker.trustRootCert();
                        if (gettrustcertval == false)
                        {
                            Fiddler.CertMaker.trustRootCert();
                        }
                    }
                    catch (Exception ex)
                    {

                    }

                    Activitywrite("write session from l in switch");
                    WriteSessionList(oAllSessions);
                    return;



                case 'w':
#if SAZ_SUPPORT
                    Activitywrite("terminate flog with w char");
                    if (oAllSessions.Count > 0)
                    {
                        Activitywrite("oall session will be writing saz using task");
                        Task.Factory.StartNew(() => SaveSessionsToDesktop(oAllSessions)).ContinueWith(tsk => filewtittebtodisk = true);

                    }
                    else
                    {
                        //todo  while updating the app check once if you can update the app with 0 status of count
                        //filewtittebtodisk = true
                        filewtittebtodisk = true;
                    }
#else
                       
#endif

                    return;



            }

        }





        /*
        /// <summary>
        /// This callback allows your code to evaluate the certificate for a site and optionally override default validation behavior for that certificate.
        /// You should not implement this method unless you understand why it is a security risk.
        /// </summary>
        static void CheckCert(object sender, ValidateServerCertificateEventArgs e)
        {
            if (null != e.ServerCertificate)
            {
                Console.WriteLine("Certificate for " + e.ExpectedCN + " was for site " + e.ServerCertificate.Subject + " and errors were " + e.CertificatePolicyErrors.ToString());

                if (e.ServerCertificate.Subject.Contains("fiddler2.com"))
                {
                    Console.WriteLine("Got a certificate for fiddler2.com. We'll say this is also good for any other site, like https://fiddlertool.com.");
                    e.ValidityState = CertificateValidity.ForceValid;
                }
            }
        }
        */

        //public static void filetowrite(Stream s)
        //{
        //    byte[] b = null;
        //    s.Write(b, 0, 0);
        //}
        /*
        // This event handler is called on every socket read for the HTTP Response. You almost certainly don't want
        // to add a handler for this event, but the code below shows how you can use it to mess up your HTTP traffic.
        static void FiddlerApplication_OnReadResponseBuffer(object sender, RawReadEventArgs e)
        {
            // NOTE: arrDataBuffer is a fixed-size array. Only bytes 0 to iCountOfBytes should be read/manipulated.
            //
            // Just for kicks, lowercase every byte. Note that this will obviously break any binary content.
            for (int i = 0; i < e.iCountOfBytes; i++)
            {
                if ((e.arrDataBuffer[i] > 0x40) && (e.arrDataBuffer[i] < 0x5b))
                {
                    e.arrDataBuffer[i] = (byte)(e.arrDataBuffer[i] + (byte)0x20);
                }
            }
            Console.WriteLine(String.Format("Read {0} response bytes for session {1}", e.iCountOfBytes, e.sessionOwner.id));
        }
        */

        /// <summary>
        /// When the user hits CTRL+C, this event fires.  We use this to shut down and unregister our FiddlerCore.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            DoQuit();
        }
    }
}

