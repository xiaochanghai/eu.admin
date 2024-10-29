color B

del  .PublishFiles\*.*   /s /q

dotnet restore

dotnet build

cd Tiobon.Core.Api

dotnet publish -o ..\Tiobon.Core.Api\bin\Debug\net8.0\

md ..\.PublishFiles

xcopy ..\Tiobon.Core.Api\bin\Debug\net8.0\*.* ..\.PublishFiles\ /s /e 

echo "Successfully!!!! ^ please see the file .PublishFiles"

cmd