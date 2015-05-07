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
        static List<string> localDbData;
        static List<string> newTorrents;
        static List<string> excludeTorrents;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        static bool DownloadAllFromFile(string fileName) 
        {
            var isAlreadyFound = false;
            HtmlDocument doc = new HtmlDocument();
            doc.Load(fileName);

            var torrents = doc.DocumentNode.Descendants("a").Where(d =>
                    d.ParentNode.ParentNode.Attributes.Contains("id") && d.ParentNode.ParentNode.Attributes["id"].Value.Contains("720p")
                    && d.ParentNode.Attributes.Contains("class") && d.ParentNode.Attributes["class"].Value.Contains("ind-link") && d.InnerHtml == "Torrent")
                    .Select(x => x.Attributes["href"].Value);

            var torrentFolder = System.Configuration.ConfigurationManager.AppSettings.Get("TorrentFileDestPath");

            foreach (var torrent in torrents)
            {
                using (var client = new WebClient())
                {
                    var torrentFile = client.DownloadData(torrent);
                    // Try to extract the filename from the Content-Disposition header
                    var torrentFileName = "";
                    if (!String.IsNullOrEmpty(client.ResponseHeaders["Content-Disposition"]))
                    {
                        torrentFileName = client.ResponseHeaders["Content-Disposition"].Substring(client.ResponseHeaders["Content-Disposition"].IndexOf("filename=") + 10).Replace("\"", "");
                    }

                    Console.WriteLine(torrentFolder);
                    Console.WriteLine(torrentFileName);

                    var torrentPath = Path.Combine(torrentFolder, torrentFileName);
                    if (!String.IsNullOrEmpty(torrentFileName) && !localDbData.Contains(torrentFileName) && !isAlreadyFound && !excludeTorrents.Any(x=> torrentFileName.Contains(x)))
                    {
                        File.WriteAllBytes(torrentPath, torrentFile);
                        newTorrents.Add(torrentFileName);
                        Console.WriteLine(torrentFileName);
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


        static void Main(string[] args)
        {
            var isFirstRun = false;
            
            
            var appDataFolder = "";//Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            if (!String.IsNullOrWhiteSpace(ConfigurationManager.AppSettings.Get("ConfigurationFilesPath")))
                appDataFolder = ConfigurationManager.AppSettings.Get("ConfigurationFilesPath");
            var localDbFolder = Path.Combine(appDataFolder, LOCAL_DB);

            excludeTorrents = new List<string>();
            if (File.Exists(Path.Combine(appDataFolder, EXCLUDE_DB))) 
                excludeTorrents = File.ReadAllLines(Path.Combine(appDataFolder, EXCLUDE_DB)).ToList();

            if (!File.Exists(localDbFolder))
            {
                var file = File.Create(localDbFolder);
                file.Close();
                isFirstRun = true;
            }
            localDbData = File.ReadAllLines(localDbFolder).ToList();
            newTorrents = new List<string>();

            if (localDbData.Count == 0)
                isFirstRun = true;

            var pageIndex = 0;
            do
            {
                var fileName = Path.Combine(appDataFolder, Guid.NewGuid() + ".html");

                using (var client = new WebClient())
                {
                    Console.WriteLine("Downloading {0} page from HorribleSubs", pageIndex);
                    client.DownloadFile("http://horriblesubs.info/lib/latest.php?nextid=" + pageIndex, fileName);
                    ++pageIndex;
                }
                var foundExisting = DownloadAllFromFile(fileName);

                File.Delete(fileName);
                if (foundExisting)
                    break;

            } while (isFirstRun ? pageIndex < 1 : true);


            File.AppendAllLines(localDbFolder, newTorrents);
        }
    }
}
