################################
WARNING: Breaking changes:
################################


1) Microsoft has abanboned its SQLitePCL project.
#################################################
As the old SQLitePCL uses the system internal SQLIte.dll, which is - for security resons - prohibited since Android 7.0 we had to switch to another library.
This library (SQLitePCL.pretty) is based on SQLitePCL.raw which now is the status quo out there (SQLite.NET uses it as well as EntityFramework 7.0)

To get it up and running - please refer to the SQLitePCL.Pretty documentation as there are many options to choose from.
For example:
To get the most up-to-date sqlite version on all your systems, reference the nuget package
	Nuget-Install SQLitePCLRaw.bundle_e_sqlite3 -version 1.1.2
and run this code at application startup once:
	SQLitePCL.Batteries_V2.Init();


2) The Sync Service part is now in its own package in "SyncWinrt.SQLite.Service"
################################################################################
The reason for this is that, in a real worls project, we needed to run integration tests. So we added a .NET 4.0 client library to SyncWinrt.SQLite that we could use in our Unit Test projects.
As the current nuget package installed the service dlls when detecting the full .NET 4.0 framework, we had a problem, as now client and service dlls were installed which does not make any sense.
For that reason:

   #1: all client dlls (iOS, Android, UWP and .NET 4.0) are contained in the SyncWinrt.SQLite package
   #2: the service dlls (only for .NET 4.0) are contained in the SyncWinrt.SQLite.Service package