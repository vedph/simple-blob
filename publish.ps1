dotnet build -c Release

# dotnet publish -c Release -r win-x64 --no-self-contained
dotnet publish -c Release -r win-x64 --self-contained False
dotnet publish -c Release -r linux-x64 --self-contained False
dotnet publish -c Release -r osx-x64 --self-contained False

compress-archive -path .\blob\bin\Release\net8.0\win-x64\publish\* -DestinationPath .\blob\bin\Release\blob-win-x64.zip -Force
compress-archive -path .\blob\bin\Release\net8.0\linux-x64\publish\* -DestinationPath .\blob\bin\Release\blob-linux-x64.zip -Force
compress-archive -path .\blob\bin\Release\net8.0\osx-x64\publish\* -DestinationPath .\blob\bin\Release\blob-osx-x64.zip -Force
