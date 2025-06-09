FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Clubber.Web/Clubber.Web.csproj", "Clubber.Web/"]
COPY ["Clubber.Discord/Clubber.Discord.csproj", "Clubber.Discord/"]
COPY ["Clubber.Domain/Clubber.Domain.csproj", "Clubber.Domain/"]
RUN dotnet restore "Clubber.Web/Clubber.Web.csproj"
COPY . .
WORKDIR "/src/Clubber.Web"
RUN dotnet build "./Clubber.Web.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Clubber.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Clubber.Web.dll"]
