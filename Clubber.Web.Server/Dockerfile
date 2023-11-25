FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Clubber.Web.Server/Clubber.Web.Server.csproj", "Clubber.Web.Server/"]
COPY ["Clubber.Domain/Clubber.Domain.csproj", "Clubber.Domain/"]
COPY ["Clubber.Web.Client/Clubber.Web.Client.csproj", "Clubber.Web.Client/"]
RUN dotnet restore "Clubber.Web.Server/Clubber.Web.Server.csproj"
COPY . .
WORKDIR "/src/Clubber.Web.Server"
RUN dotnet build "Clubber.Web.Server.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Clubber.Web.Server.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Clubber.Web.Server.dll"]
