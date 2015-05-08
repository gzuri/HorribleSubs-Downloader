# HorribleSubs-Downloader  ![AppVayour status](https://ci.appveyor.com/api/projects/status/60wftquacksfgng6/branch/master?svg=true)
HorribleSubs-Downloader is a small  Windows console application that automatically downloads torrent files from [http://horriblesubs.info/](http://horriblesubs.info/ "HorribleSubs"). 

##Features
- Tracking of last downloaded torrent to prevent double downloading
- Configurable torrent location
- Configurable video quality

##Instalation
The easiest way is just to download the zip file from URL which should contain the latest build, unzip it in some folder configure the destination torrent folder and you are good to go.

###Configuration
To configure destination folder open the file "HorribleSubsDownload.exe.config" with Notepad. You should see something like shown bellow

    <?xml version="1.0" encoding="utf-8" ?>
	<configuration>
	  <appSettings>
	    <add key="TorrentFileDestPath" value="C:/0/Torrents"/>
	    <add key="ConfigurationFilesPath" value=""/>
	    <add key="VideoQuality" value="720p"/>
	    <add key="LastDownloadedTorrent" value=""/>
	  </appSettings>
      <startup> 
        <supportedRuntime version="v4.0" sku=".NETFramework,Version=v4.5" />
      </startup>
	</configuration>

To configure torrent destination path change the value of the key "TorrentFileDestPath", keep in mind to keep surrounding quotes.
To configure desired video quality change the value of the key "VideoQuality", possible values are "480p", "720p" and "1080p"