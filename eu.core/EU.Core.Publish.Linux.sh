
find .PublishFiles/ -type f -and ! -path '*/wwwroot/images/*' ! -name 'appsettings.*' |xargs rm -rf
dotnet build;
rm -rf /home/Tiobon.Core/Tiobon.Core.Api/bin/Debug/.PublishFiles;
dotnet publish -o /home/Tiobon.Core/Tiobon.Core.Api/bin/Debug/.PublishFiles;
rm -rf /home/Tiobon.Core/Tiobon.Core.Api/bin/Debug/.PublishFiles/WMTiobon.db;
# cp -r /home/Tiobon.Core/Tiobon.Core.Api/bin/Debug/.PublishFiles ./;
awk 'BEGIN { cmd="cp -ri /home/Tiobon.Core/Tiobon.Core.Api/bin/Debug/.PublishFiles ./"; print "n" |cmd; }'
echo "Successfully!!!! ^ please see the file .PublishFiles";