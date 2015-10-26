# HorribleSubs-Downloader  ![AppVayour status](https://ci.appveyor.com/api/projects/status/60wftquacksfgng6/branch/master?svg=true)
HorribleSubs-Downloader is a small  Windows console application that automatically downloads torrent files from [http://horriblesubs.info/](http://horriblesubs.info/ "HorribleSubs"). 

##Features
- Tracking of last downloaded torrent to prevent double downloading
- Configurable torrent location
- Configurable video quality

##Installation
The easiest way is just to download the zip file from [ur](https://gzuri.blob.core.windows.net/public/HorribleSubsDownload.zip "url")  or download directly from [GitHub Release](https://github.com/gzuri/HorribleSubs-Downloader/releases/latest "GitHub release") which should always contain the latest build, unzip it in some folder configure the destination torrent folder and you are good to go.

###Configuration
To configure destination folder open the file "HorribleSubsDownload.exe.config" with Notepad. You should see something like shown bellow

    <?xml version="1.0" encoding="utf-8" ?>
	<configuration>
	  <appSettings>
	    <add key="TorrentFileDestPath" value="C:/0/Torrents"/>
	    <add key="ConfigurationFilesPath" value=""/>
	    <add key="VideoQuality" value="720p"/>
	    <add key="LastDownloadedTorrent" value=""/>
		<add key="ExcludeTorrents" value=""/>
	  </appSettings>
      <startup> 
        <supportedRuntime version="v4.0" sku=".NETFramework,Version=v4.5" />
      </startup>
	</configuration>

To configure torrent destination path change the value of the key "TorrentFileDestPath", keep in mind to keep surrounding quotes.

To configure desired video quality change the value of the key "VideoQuality", possible values are "480p", "720p" and "1080p"

To configure excludes specify the names (without episode) in the key "ExcludeTorrents", multiple rules are separated by using semicolon (ex: "Anime1;Anime two;Anime three")

**Note: Windows 8, 8.1 can prevent application from running with SmartScreen, just click more options and option Run anyway**

##Automatic torrent download
###Setting up torrrent client
You can set your torrent client to automatically download torrent files from some folder, I'm using uTorrent and this is how my configuration looks like
![](http://tinyurl.com/kum7q32) 

###Setting up script run on startup
Since this app is very light and doesn't waste almost any resources you can set it to run on Windows startup as explained in [http://windows.microsoft.com/en-us/windows/run-program-automatically-windows-starts#1TC=windows-7](http://windows.microsoft.com/en-us/windows/run-program-automatically-windows-starts#1TC=windows-7 "Microsoft documentation")