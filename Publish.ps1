$file = Get-Item "C:\Binaries\Scribe\*.nupkg" | Select -First 1
& "nuget.exe" push $file.FullName