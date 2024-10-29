#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

#这种模式是直接在构建镜像的内部编译发布dotnet项目。
#注意下容器内输出端口是9291
#如果你想先手动dotnet build成可执行的二进制文件，然后再构建镜像，请看.Api层下的dockerfile。


#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["EU.Core.Api/EU.Core.Api.csproj", "EU.Core.Api/"]
COPY ["EU.Core.Extensions/EU.Core.Extensions.csproj", "EU.Core.Extensions/"]
COPY ["EU.Core.EventBus/EU.Core.EventBus.csproj", "EU.Core.EventBus/"]
COPY ["EU.Core.Common/EU.Core.Common.csproj", "EU.Core.Common/"]
COPY ["EU.Core.Model/EU.Core.Model.csproj", "EU.Core.Model/"]
COPY ["EU.Core.Serilog.Es/EU.Core.Serilog.Es.csproj", "EU.Core.Serilog.Es/"]
COPY ["Ocelot.Provider.Nacos/Ocelot.Provider.Nacos.csproj", "Ocelot.Provider.Nacos/"]
COPY ["EU.Core.Services/EU.Core.Services.csproj", "EU.Core.Services/"]
COPY ["EU.Core.IServices/EU.Core.IServices.csproj", "EU.Core.IServices/"]
COPY ["EU.Core.Repository/EU.Core.Repository.csproj", "EU.Core.Repository/"]
COPY ["EU.Core.Tasks/EU.Core.Tasks.csproj", "EU.Core.Tasks/"]
COPY ["build", "build/"]
RUN dotnet restore "EU.Core.Api/EU.Core.Api.csproj"
COPY . .
WORKDIR "/src/EU.Core.Api"
RUN dotnet build "EU.Core.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "EU.Core.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 9291 
ENTRYPOINT ["dotnet", "EU.Core.Api.dll"]
