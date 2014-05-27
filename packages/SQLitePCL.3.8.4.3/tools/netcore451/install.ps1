param($installPath, $toolsPath, $package, $project)

$sqliteReference = $project.Object.References.AddSDK("SQLite for Windows Runtime (Windows 8.1)", "SQLite.WinRT81, version=3.8.4.3")

Write-Host "Successfully added a reference to the extension SDK SQLite for Windows Runtime (Windows 8.1)."
Write-Host "Please, verify that the extension SDK SQLite for Windows Runtime (Windows 8.1) v3.8.4.3, from the SQLite.org site (http://www.sqlite.org/2014/sqlite-winrt81-3080403.vsix), has been properly installed."