dotnet build -c Release

dotnet publish -c Release -r win-x64
dotnet publish -c Release -r linux-x64
dotnet publish -c Release -r osx-x64

compress-archive -path .\bin\Release\net6.0\win-x64\publish\* -DestinationPath .\bin\Release\net6.0\win-x64\blob-win-x64.zip -Force
compress-archive -path .\bin\Release\net6.0\linux-x64\publish\* -DestinationPath .\bin\Release\net6.0\linux-x64\blob-linux-x64.zip -Force
compress-archive -path .\bin\Release\net6.0\osx-x64\publish\* -DestinationPath .\bin\Release\net6.0\osx-x64\blob-osx-x64.zip -Force
