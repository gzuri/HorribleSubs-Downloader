﻿using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.AccessControl;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;


namespace HorribleSubsDownload
{
    class Program
    {
        /// <summary>
        /// Stores last downloaded torrent from the DB
        /// </summary>
        private static List<string> lastDownloadedTorrents;
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
                throw new ConfigurationException("Video quality must be set on 480p or 720p or 1080p");

            doc.Load(fileName);
            var downloadBundles = Convert.ToBoolean(ConfigurationManager.AppSettings.Get("DownloadBundles"));
            var torrentLinks = new List<string>();
            var releases = doc.DocumentNode.Descendants("div").Where(d => d.Attributes.Contains("class") && d.Attributes["class"].Value.StartsWith("release-links") && d.Attributes["class"].Value.Contains(videoQuality));
            foreach (var release in releases)
            {
                var releaseName = release.Descendants("i").First(x => x.ParentNode.Attributes.Contains("class") && x.ParentNode.Attributes["class"].Value.Contains("dl-label")).InnerHtml;
                var releaseTorrentLink = release.Descendants("a").First(d=> d.ParentNode.Attributes.Contains("class") && d.ParentNode.Attributes["class"].Value.Contains("dl-link") && d.InnerHtml == "Torrent").Attributes["href"].Value;
                
                if (Regex.IsMatch(releaseName, @"\([0-9]*-[0-9]*\)"))
                {
                    if (downloadBundles)
                        torrentLinks.Add(releaseTorrentLink);
                }
                else
                    torrentLinks.Add(releaseTorrentLink);
            }

            foreach (var torrent in torrentLinks)
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
                    if (!String.IsNullOrEmpty(torrentFileName) && lastDownloadedTorrents.All(x=> x != torrentFileName))
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

        private static void UpdateSetting(List<string> newTorrents)
        {
            Configuration configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            configuration.AppSettings.Settings["LastDownloadedTorrent"].Value = String.Join(";", newTorrents.Take(10).ToArray());
            configuration.Save();

            ConfigurationManager.RefreshSection("appSettings");
        }


        static void Main(string[] args)
        {
            var isFirstRun = false;
            if (!String.IsNullOrWhiteSpace(ConfigurationManager.AppSettings.Get("LastDownloadedTorrent")))
                lastDownloadedTorrents = ConfigurationManager.AppSettings.Get("LastDownloadedTorrent").Split(';').ToList();
            else 
                lastDownloadedTorrents = new List<string>();
            
            var appDataFolder = String.Empty;
            if (!String.IsNullOrWhiteSpace(ConfigurationManager.AppSettings.Get("ConfigurationFilesPath")))
                appDataFolder = ConfigurationManager.AppSettings.Get("ConfigurationFilesPath");
            else
                appDataFolder = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            newTorrents = new List<string>();

            //Load exclude torrent rules
            var exludeTorrentSetting = ConfigurationManager.AppSettings.Get("ExcludeTorrents");
            if (!String.IsNullOrWhiteSpace(exludeTorrentSetting))
                excludeTorrents = exludeTorrentSetting.Split(';').Where(x=> !String.IsNullOrWhiteSpace(x)).Select(x=> x.Trim()).ToList();
            else
                excludeTorrents = new List<string>();

            if (lastDownloadedTorrents.Count == 0)
                isFirstRun = true;

            var torrentFileDestPath = ConfigurationManager.AppSettings.Get("TorrentFileDestPath");
            if (String.IsNullOrWhiteSpace(torrentFileDestPath))
                throw new ConfigurationException("TorrentFileDestPath must be defined");

            DirectorySecurity fsecurity = Directory.GetAccessControl(torrentFileDestPath);

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
                UpdateSetting(newTorrents);
        }
    }
}
