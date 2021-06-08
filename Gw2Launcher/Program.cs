using HtmlAgilityPack;
using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace Gw2Launcher
{
    class Program
    {
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();
        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        const int SW_HIDE = 0;
        const int SW_SHOW = 5;

        static readonly String filePath = @"..\bin64\d3d9.dll";
        static readonly String tmpDir = @"..\tmp";
        static readonly String gameProcessName = "Gw2-64";

        static void Main(string[] args)
        {
            var handle = GetConsoleWindow();
            ShowWindow(handle, SW_HIDE);
            FileInfo fileInfo = new FileInfo(filePath);
            DateTime currentDate = fileInfo.LastWriteTime;
            if (!Directory.Exists(tmpDir))
            {
                Directory.CreateDirectory(tmpDir);
            }
            if (!IsProcessStarted(gameProcessName))
            {
                try
                {
                    string url = ReadSetting("url");
                    DateTime lastUpdate = FetchRemoteModificationDate(url);
                    if (!File.Exists(filePath) || lastUpdate > currentDate)
                    {
                        Console.WriteLine("Arc Dps Start Update.");
                        DownloadFile(url + "d3d9.dll", tmpDir + @"\d3d9.dll");
                        DownloadFile(url + "d3d9.dll.md5sum", tmpDir + @"\d3d9.dll.md5sum");
                        string currentMd5 = CalculateMd5(tmpDir + @"\d3d9.dll");
                        string expectedMd5 = GetMd5FromFile(tmpDir + @"\d3d9.dll.md5sum");
                        if (currentMd5.Equals(expectedMd5.ToUpper()))
                        {
                            File.Copy(tmpDir + @"\d3d9.dll", filePath);
                        }
                        else
                        {
                            CreateToast("Arc Dps", "Une erreur est survenu lors de la mise à jour de ArcDps.");
                        }
                    }
                    CreateToast("Arc Dps", "Arc Dps est à jour. Démarrage du client GuildWars 2.");
                    Process.Start("..\\" + gameProcessName);
                }
                catch (Exception exception)
                {
                    Console.WriteLine("Failed to find Guild Wars 2 Launcher.", exception);
                    CreateToast("Guild Wars 2", "Impossible de lancer le client GuildWars 2.");
                }
            }
            else
            {
                Console.WriteLine("Error the process : " + gameProcessName + " is running. You must close Guild Wars 2 to update Arc Dps.");
                CreateToast("Arc Dps", "Veuillez fermer GuildWars 2 afin de mettre à jour ArcDps.");
            }
            Directory.Delete(tmpDir, true);
        }

        /// <summary>
        /// Gets last available ArcDps release on server. 
        /// </summary>
        /// <returns></returns>
        static DateTime FetchRemoteModificationDate(string url)
        {
            HttpClient client = new HttpClient();
            HttpResponseMessage response = client.GetAsync(url).Result;
            if (HttpStatusCode.OK.Equals(response.StatusCode))
            {
                HttpContent content = response.Content;
                string result = content.ReadAsStringAsync().Result;
                if (result != null)
                {
                    HtmlDocument htmlDocument = new HtmlDocument();
                    htmlDocument.LoadHtml(result);
                    foreach (HtmlNode node in htmlDocument.DocumentNode.SelectNodes("//td[@class='indexcollastmod']"))
                    {
                        string value = node.InnerHtml;
                        if (!value.Contains("&nbsp"))
                        {
                            return DateTime.Parse(value);
                        }
                    }
                }
            }
            return new DateTime();
        }

        /// <summary>
        /// Gets ArcDps files from server.
        /// </summary>
        static void DownloadFile(string url, string filePath)
        {
            WebClient webClient = new WebClient();
            try
            {
                webClient.DownloadFile(url, filePath);
            }
            catch (Exception exception) when (exception is WebException || exception is ArgumentNullException || exception is NotSupportedException)
            {
                throw exception;
            }
        }

        /// <summary>
        /// Gets md5sum from file.
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        static string CalculateMd5(string file)
        {
            HashAlgorithm md5 = new MD5CryptoServiceProvider();
            Byte[] allBytes = File.ReadAllBytes(file);
            return ByteArrayToString(md5.ComputeHash(allBytes));
            
        }

        /// <summary>
        /// Gets expected md5 from file.
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        static string GetMd5FromFile(string file)
        {
            try
            {
                if (File.Exists(file))
                {
                    string text = File.ReadAllText(file);
                    return text.Split(new char[] {' '}, StringSplitOptions.None)[0];
                }
            }
            catch (Exception exception)
            {
                throw exception;
            }
            return "";
        }

        /// <summary>
        /// Checks if process is running.
        /// </summary>
        /// <param name="processName"></param>
        /// <returns></returns>
        static bool IsProcessStarted(String processName)
        {
            return Process.GetProcesses().Where(p => p.ProcessName.Contains(processName)).Any();
        }

        /// <summary>
        /// Create Windows Toast notification.
        /// </summary>
        /// <param name="title"></param>
        /// <param name="message"></param>
        static void CreateToast(String title, String message)
        {
            XmlDocument toastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastImageAndText02);

            XmlNodeList stringElements = toastXml.GetElementsByTagName("text");
            stringElements[0].AppendChild(toastXml.CreateTextNode(title));
            stringElements[1].AppendChild(toastXml.CreateTextNode(message));

            DateTimeOffset delay = DateTimeOffset.Now.AddSeconds(10);
            ToastNotification toast = new ToastNotification(toastXml);
            toast.ExpirationTime = delay;

            ToastNotificationManager.CreateToastNotifier("Gw2Launcher").Show(toast);
        }

        /// <summary>
        /// Reads settings file.
        /// </summary>
        /// <param name="key"></param>
        static string ReadSetting(string key)
        {
            try
            {
                var appSettings = ConfigurationManager.AppSettings;
                return appSettings[key] ?? "Not Found";
            }
            catch (ConfigurationErrorsException configurationErrorsException)
            {
                throw configurationErrorsException;
            }
        }

        static string ByteArrayToString(byte[] bytes)
        {
            return String.Concat(Array.ConvertAll(bytes, x => x.ToString("X2")));
        }
    }
}
