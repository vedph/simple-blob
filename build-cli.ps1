cd ./blob
dotnet publish -c Release -r osx-x64 --self-contained false
dotnet publish -c Release -r linux-x64 --self-contained false
dotnet publish -c Release -r win-x64 --self-contained false

compress-archive -path .\bin\Release\net6.0\osx-x64\publish\* -destinationpath .\bin\Release\blob-osx-x64.zip
compress-archive -path .\bin\Release\net6.0\linux-x64\publish\* -destinationpath .\bin\Release\blob-linux-x64.zip
compress-archive -path .\bin\Release\net6.0\win-x64\publish\* -destinationpath .\bin\Release\blob-win-x64.zip
