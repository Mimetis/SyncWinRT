SyncWinRT
=========

This project is the WinRT implementation of the Sync Framework Toolkit to enabled synchronization with WinRT or Windows Phone 8 and SQLite.

This toolkit is based on Sync Framework Toolkit. It’s not a new version.

Windows Runtime 8.1 / SQLite 3.8.4.3
=========

This new release of the WinRT implementation of the Sync Toolkit will add some new features:
1.Upgrade to support SQLite Version 3.8.4.3 
2.Nuget package integration : http://www.nuget.org/packages/SyncClient.SQLite 
3.Out of the box Platforms support (x86, x64 and ARM) 
4.Using of SQLitePCL nuget package from MSOpenTech :  https://sqlitepcl.codeplex.com/ instead of SQLite-WinRT   
5.Correction of a server bug occuring in batch mode and the client disconnect during process 
6.Adding support for TimeSpan type (mapped to time(7) on sql server) 
7.Adding events to track the synchronization process 

Don’t forget to read the documentation tab tutorial to see how to install the toolkit :)

Introduction
=========

This version of the Sync Toolkit adds support for synchronization with WinRT and Windows Phone 8 :


In the sources provided on the Codeplex website, you will find 2 samples including a simple HelloWorldSync and a complete sample Fabrikam, with CRUD operations and synchronization.

Setup
=========

To create a “synchonizable application” that will work on Windows Store apps and Windows Phone 8, you will find in the documentation tab a full tutorial (still in progress)

Here the tutorial parts :
1.Download and install Sync Framework and Sync Toolkit for WinRT 
2.Generate the code for your database, your server and your client application 
3.Create a fully functional application 
4.Create a filtered application based on a template scope 
5.Handle conflicts 

If you want more information, don’t forget to check my blog :  Msdn Mim’s blog

