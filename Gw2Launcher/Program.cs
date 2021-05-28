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

        static readonly String url = "https://www.deltaconnected.com/arcdps/x64/";
        static readonly String filePath = @"..\bin64\d3d9.dll";
        static readonly String gameProcessName = "Gw2-64";

        static void Main(string[] args)
        {
            var handle = GetConsoleWindow();
            ShowWindow(handle, SW_HIDE);
            FileInfo fileInfo = new FileInfo(filePath);
            DateTime currentDate = fileInfo.LastWriteTime;
            if (!IsProcessStarted(gameProcessName))
            {
                DateTime lastUpdate = FetchRemoteModificationDate();
                try
                {
                    if (!File.Exists(filePath) || lastUpdate > currentDate)
                    {
                        Console.WriteLine("Arc Dps Start Update.");
                        DownloadFile();
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
        }

        static DateTime FetchRemoteModificationDate()
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

        static void DownloadFile()
        {
            WebClient webClient = new WebClient();
            try
            {
                webClient.DownloadFile(url + "d3d9.dll", filePath);
                if (File.Exists(filePath))
                {
                    Console.WriteLine("Update success.");
                }
            }
            catch (Exception exception) when (exception is WebException || exception is ArgumentNullException || exception is NotSupportedException)
            {
                throw exception;
            }
        }

        static bool IsProcessStarted(String processName)
        {
            return Process.GetProcesses().Where(p => p.ProcessName.Contains(processName)).Any();
        }

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

        static void ReadSetting(string key)
        {
            try
            {
                var appSettings = ConfigurationManager.AppSettings;
                string result = appSettings[key] ?? "Not Found";
                Console.WriteLine(result);
            }
            catch (ConfigurationErrorsException)
            {
                Console.WriteLine("Error reading app settings");
            }
        }
    }
}
