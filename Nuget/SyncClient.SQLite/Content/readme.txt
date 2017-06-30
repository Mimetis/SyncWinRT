Breaking change:

Microsoft has abanboned its SQLitePCL project.
As the old SQLitePCL uses the system internal SQLIte.dll, which is - for security resons - prohibited since Android 7.0 we had to switch to another library.
This library (SQLitePCL.pretty) is based on SQLitePCL.raw which now is the status quo out there (SQLite.NET uses it as well as EntityFramework 7.0)

To get it up and running - please refer to the SQLitePCL.Pretty documentation as there are many options to choose from.
For example:
To get the most up-to-date sqlite version on all your systems, reference the nuget package
	Nuget-Install SQLitePCLRaw.bundle_e_sqlite3 -version 1.1.2
and run this code at application startup once:
	SQLitePCL.Batteries_V2.Init();
