using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Animation;
using System.Net;
using System.ComponentModel;
using System.Windows.Threading;
using System.IO;

namespace KiepLamp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string ip = "";
        private string deviceId = "";
        private string targetValue = "";
        private string sceneId = "";

        public MainWindow()
        {
            InitializeComponent();

            // Apply rotation animation to status image
            DoubleAnimation da = new DoubleAnimation(0, 360, new Duration(TimeSpan.FromSeconds(3)));
            RotateTransform rt = new RotateTransform();
            imgStatus.RenderTransform = rt;
            imgStatus.RenderTransformOrigin = new Point(0.5, 0.5);
            da.RepeatBehavior = RepeatBehavior.Forever;
            rt.BeginAnimation(RotateTransform.AngleProperty, da);

            ReadCommandline();


            // TODO Better would be to query the IP address via the MIOS API, and cache it
            // TODO Maybe using VeraDotNet library https://veradotnet.codeplex.com/


            if (deviceId != "" && targetValue != "")
            {
                TryReadFromWeb("http://" + ip + ":3480/data_request?id=lu_action&output_format=text&DeviceNum=" + deviceId + "&serviceId=urn:upnp-org:serviceId:SwitchPower1&action=SetTarget&newTargetValue=" + targetValue);
            }

            if (sceneId != "")
            {
                TryReadFromWeb("http://" + ip + ":3480/data_request?id=lu_action&output_format=text&serviceId=urn:micasaverde-com:serviceId:HomeAutomationGateway1&action=RunScene&SceneNum=" + sceneId);
            }
        }

        private void ReadCommandline()
        {
#if !DEBUG
            try
            {
#endif
            string[] commandLineArgs = Environment.GetCommandLineArgs();
            for (int i = 0; i < commandLineArgs.Length; i++)
            {
                switch (commandLineArgs[i])
                {
                    case "-ip":
                        i++;
                        if (commandLineArgs.Length > i)
                        {
                            try
                            {
                                ip = commandLineArgs[i];
                            }
                            catch (Exception) { }
                        }
                        break;

                    case "-device":
                        i++;
                        if (commandLineArgs.Length > i)
                        {
                            try
                            {
                                deviceId = commandLineArgs[i];
                            }
                            catch (Exception) { }
                        }
                        break;

                    case "-value":
                        i++;
                        if (commandLineArgs.Length > i)
                        {
                            try
                            {
                                targetValue = commandLineArgs[i];
                            }
                            catch (Exception) { }
                        }
                        break;

                    case "-scene":
                        i++;
                        if (commandLineArgs.Length > i)
                        {
                            try
                            {
                                sceneId = commandLineArgs[i];
                            }
                            catch (Exception) { }
                        }
                        break;

                    default:
                        // Unknown argument: do nothing
                        break;
                }
            }
#if !DEBUG
            }
            catch (Exception) { }
#endif
        }



        private void TryReadFromWeb(string url)
        {
            try
            {
                BackgroundWorker bw = new BackgroundWorker();
                bw.DoWork += new DoWorkEventHandler(bw_DoWork);
                bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bw_RunWorkerCompleted);
                bw.RunWorkerAsync(url);
            }
            catch (Exception) { }
        }

        void bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            try
            {
            string result = (string)e.Result;
            if (result != "")
            {
                imgStatus.Source = new BitmapImage(new Uri("Ok.png", UriKind.Relative));
            }
            else
            {
                imgStatus.Source = new BitmapImage(new Uri("Error.png", UriKind.Relative));
            }

            imgStatus.RenderTransform = null;
            ExitAfter2Seconds();

            }
            catch (Exception) { }
        }

        void bw_DoWork(object sender, DoWorkEventArgs e)
        {
            e.Result = "";
            try
            {
                string url = (string)e.Argument;
                WebRequest webRequest = WebRequest.Create(url);
                webRequest.Proxy = null;
                webRequest.Timeout = 5000;
                Stream responseStream = webRequest.GetResponse().GetResponseStream();
                StreamReader reader = new StreamReader(responseStream);
                string responseFromServer = reader.ReadToEnd();
                e.Result = responseFromServer;
            }
            catch (Exception) { }
        }

        private void ExitAfter2Seconds()
        {
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = new TimeSpan(0,0,2);
            timer.Tick += new EventHandler(timer_Tick);
            timer.Start();
        }

        void timer_Tick(object sender, EventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
