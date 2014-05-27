SyncWinRT
=========

This project is the WinRT implementation of the Sync Framework Toolkit to enabled synchronization with WinRT or Windows Phone 8 and SQLite.

this toolkit works for :
* Windows Phone 8.0 and 8.1 (Silverlight) 
* Windows Store apps (WinRT)
* iOS (thanks to Xamarin)
* Android (thanks to Xamarin)

The Windows Phone 8.1 (WinRT) is not yet implemented, I just wait for SQLite implementation fro SQLite.org (will be available soon, imo)

Windows Runtime 8.1 / SQLite 3.8.4.3
=========

This new release of the WinRT implementation of the Sync Toolkit will add some new features:
- Upgrade to support SQLite Version 3.8.4.3 
- Nuget package integration : http://www.nuget.org/packages/SyncClient.SQLite 
- Out of the box Platforms support (x86, x64 and ARM) 
- Using of SQLitePCL nuget package from MSOpenTech :  https://sqlitepcl.codeplex.com/ instead of SQLite-WinRT   
- Correction of a server bug occuring in batch mode and the client disconnect during process 
- Adding support for TimeSpan type (mapped to time(7) on sql server) 
- Adding events to track the synchronization process 
- Adding support for iOS and Android

Don’t forget to read the documentation tab tutorial to see how to install the toolkit :)

Introduction
=========

This version of the Sync Toolkit adds support for synchronization with WinRT and Windows Phone 8 :

![Image of Sync](http://download-codeplex.sec.s-msft.com/Download?ProjectName=syncwinrt&DownloadId=694394)


In the sources provided website, you will find 2 samples including a simple HelloWorldSync and a complete sample Fabrikam, with CRUD operations and synchronization.

Setup
=========

To create a “synchonizable application” that will work on Windows Store apps and Windows Phone 8, you will find in the documentation tab a full tutorial (still in progress)

Here the tutorial parts :
- Download and install Sync Framework and Sync Toolkit for WinRT 
- Generate the code for your database, your server and your client application 
- Create a fully functional application 
- Create a filtered application based on a template scope 
- Handle conflicts 

If you want more information, don’t forget to check my blog :  [Msdn Mim’s blog](http://aka.ms/seb)



