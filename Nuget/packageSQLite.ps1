&.\ensureNugetInstalled.ps1

nuget pack SyncClient.SQLite\SyncClient.SQLite.symbols.nuspec -symbols

nuget pack SyncClient.SQLite.Service\SyncClient.SQLite.Service.nuspec
