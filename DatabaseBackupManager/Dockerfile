# from dotnet 7
FROM mcr.microsoft.com/dotnet/sdk:7.0-alpine AS build
WORKDIR /src
COPY ["DatabaseBackupManager/DatabaseBackupManager.csproj", "DatabaseBackupManager/"]
RUN dotnet restore "DatabaseBackupManager/DatabaseBackupManager.csproj" --use-current-runtime /p:PublishReadyToRun=true

COPY . .

WORKDIR "/src/DatabaseBackupManager"
RUN dotnet build "DatabaseBackupManager.csproj" -c Release -o /app/build
RUN dotnet publish "DatabaseBackupManager.csproj" -c Release -o /app/publish --self-contained --use-current-runtime --no-restore /p:PublishTrimmed=true /p:PublishReadyToRun=true

FROM bash AS final
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 80
EXPOSE 443
ENTRYPOINT ["DatabaseBackupManager"]
