using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace HorribleSubsDownload
{
    class Program
    {
        const string LOCAL_DB = "HorribleSubsDownloaderLocalDB.csv";
        const string EXCLUDE_DB = "HorribleSubsExclude.csv";
        /// <summary>
        /// Stores last downloaded torrent from the DB
        /// </summary>
        private static string lastDownloadedTorrent;
        static List<string> newTorrents;
        static List<string> excludeTorrents;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        static bool DownloadAllFromFile(string torrentSavePath, string fileName) 
        {
            var isAlreadyFound = false;
            HtmlDocument doc = new HtmlDocument();
            var videoQuality = ConfigurationManager.AppSettings.Get("VideoQuality");
            if (videoQuality != "720p" && videoQuality != "480p" && videoQuality != "1080p")
                throw new  ConfigurationException("Video quality must be set on 480p or 720p or 1080p");

            doc.Load(fileName);
            var torrents = doc.DocumentNode.Descendants("a").Where(d =>
                    d.ParentNode.ParentNode.Attributes.Contains("id") && d.ParentNode.ParentNode.Attributes["id"].Value.Contains(videoQuality)
                    && d.ParentNode.Attributes.Contains("class") && d.ParentNode.Attributes["class"].Value.Contains("ind-link") && d.InnerHtml == "Torrent")
                    .Select(x => x.Attributes["href"].Value);
            

            foreach (var torrent in torrents)
            {
                //Download torrent
                using (var client = new WebClient())
                {
                    var torrentFile = client.DownloadData(torrent);
                    // Try to extract the filename from the Content-Disposition header
                    var torrentFileName = "";
                    if (!String.IsNullOrEmpty(client.ResponseHeaders["Content-Disposition"]))
                        torrentFileName = client.ResponseHeaders["Content-Disposition"].Substring(client.ResponseHeaders["Content-Disposition"].IndexOf("filename=", StringComparison.Ordinal) + 10).Replace("\"", "");
                    else 
                        continue;

                    var torrentPath = Path.Combine(torrentSavePath, torrentFileName);
                    if (!String.IsNullOrEmpty(torrentFileName) && torrentFileName != lastDownloadedTorrent)
                    {
                        if (!excludeTorrents.Any(x => torrentFileName.Contains(x)))
                        {
                            File.WriteAllBytes(torrentPath, torrentFile);
                            newTorrents.Add(torrentFileName);
                            Console.WriteLine(torrentFileName);
                        }
                    }
                    else
                    {
                        isAlreadyFound = true;
                        break;
                    }   
                }
            }
            return isAlreadyFound;
        }

        private static void UpdateSetting(string value)
        {
            Configuration configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            configuration.AppSettings.Settings["LastDownloadedTorrent"].Value = value;
            configuration.Save();

            ConfigurationManager.RefreshSection("appSettings");
        }


        static void Main(string[] args)
        {
            var isFirstRun = false;
            lastDownloadedTorrent = ConfigurationManager.AppSettings.Get("LastDownloadedTorrent");
            
            var appDataFolder = String.Empty;
            if (!String.IsNullOrWhiteSpace(ConfigurationManager.AppSettings.Get("ConfigurationFilesPath")))
                appDataFolder = ConfigurationManager.AppSettings.Get("ConfigurationFilesPath");
            else
                appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            newTorrents = new List<string>();

            //Load exclude torrent rules
            var exludeTorrentSetting = ConfigurationManager.AppSettings.Get("ExcludeTorrents");
            if (!String.IsNullOrWhiteSpace(exludeTorrentSetting))
                excludeTorrents = exludeTorrentSetting.Split(';').Where(x=> !String.IsNullOrWhiteSpace(x)).Select(x=> x.Trim()).ToList();
            else
                excludeTorrents = new List<string>();

            if (String.IsNullOrWhiteSpace(lastDownloadedTorrent))
                isFirstRun = true;

            var torrentFileDestPath = ConfigurationManager.AppSettings.Get("TorrentFileDestPath");
            if (String.IsNullOrWhiteSpace(torrentFileDestPath))
                throw new ConfigurationException("TorrentFileDestPath must be defined");

            if (!Directory.Exists(torrentFileDestPath))
                Directory.CreateDirectory(torrentFileDestPath);
            var pageIndex = 0;
            do
            {
                var tempHtmlFileName = Path.Combine(appDataFolder, Guid.NewGuid() + ".html");

                using (var client = new WebClient())
                {
                    Console.WriteLine("Downloading {0} page from HorribleSubs", pageIndex);
                    client.DownloadFile("http://horriblesubs.info/lib/latest.php?nextid=" + pageIndex, tempHtmlFileName);
                    ++pageIndex;
                }
                var foundExisting = DownloadAllFromFile(torrentFileDestPath, tempHtmlFileName);

                File.Delete(tempHtmlFileName);
                //When system reaches already download torrent it stops
                if (foundExisting)
                    break;

            } while (!isFirstRun || pageIndex < 1);

            if (newTorrents.Count > 0)
                UpdateSetting(newTorrents.First());
        }
    }
}
