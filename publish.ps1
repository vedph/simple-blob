dotnet build -c Release

dotnet publish -c Release -r win-x64
dotnet publish -c Release -r linux-x64
dotnet publish -c Release -r osx-x64

compress-archive -path .\blob\bin\Release\net7.0\win-x64\publish\* -DestinationPath .\blob\bin\Release\blob-win-x64.zip -Force
compress-archive -path .\blob\bin\Release\net7.0\linux-x64\publish\* -DestinationPath .\blob\bin\Release\blob-linux-x64.zip -Force
compress-archive -path .\blob\bin\Release\net7.0\osx-x64\publish\* -DestinationPath .\blob\bin\Release\blob-osx-x64.zip -Force
