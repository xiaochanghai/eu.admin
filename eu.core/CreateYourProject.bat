color 5
echo "if u install template error,pls connect QQ:3143422472"


color 3
dotnet new -i Tiobon.Core.Webapi.Template

set /p OP=Please set your project name(for example:TiobonMicService):

md .1YourProject

cd .1YourProject

dotnet new Tioboncoretpl -n %OP%

cd ../


echo "Create Successfully!!!! ^ please see the folder .1YourProject"

dotnet new -u Tiobon.Core.Webapi.Template


echo "Delete Template Successfully"

pause
