Set-Location PrayerTracker
dotnet publish -c Release -r linux-x64 -p:PublishSingleFile=true --self-contained false
Set-Location bin\Release\net5.0\linux-x64\publish