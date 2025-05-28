FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["DatabaseBackupManager/DatabaseBackupManager.csproj", "DatabaseBackupManager/"]
COPY ["Core/Core.csproj", "Core/"]
COPY ["Agent/Agent.csproj", "Agent/"]

RUN dotnet restore "DatabaseBackupManager/DatabaseBackupManager.csproj" --runtime linux-x64

RUN dotnet dev-certs https --clean
RUN mkdir -p /app/publish/https
RUN dotnet dev-certs https -ep /app/publish/https/DatabaseBackupManager.pfx -p test1345
RUN dotnet dev-certs https --trust

COPY . .

WORKDIR "/src/DatabaseBackupManager"
RUN dotnet publish "DatabaseBackupManager.csproj" -c Release -o /app/publish --no-restore --self-contained true --runtime linux-x64

FROM debian:bookworm-slim AS final

RUN apt-get update
RUN apt-get install -y wget gnupg2
RUN wget -O- https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor --yes --output /etc/apt/trusted.gpg.d/microsoft.gpg

RUN echo "deb http://packages.microsoft.com/debian/12/prod bookworm main" >> /etc/apt/sources.list.d/microsoft.list
RUN echo "deb http://apt.postgresql.org/pub/repos/apt bookworm-pgdg main" >> /etc/apt/sources.list.d/pgdg.list

RUN wget -O- https://www.postgresql.org/media/keys/ACCC4CF8.asc | gpg --dearmor --yes --output /etc/apt/trusted.gpg.d/pgdg.gpg

RUN export ACCEPT_EULA=Y && apt-get update && apt-get install -y \
    libgdiplus \
    openssl \
    postgresql-client \
    postgresql-client-14 \
    default-mysql-client \
    mssql-tools18 \
    sqlite3 \
    && rm -rf /var/lib/apt/lists/* 

# create symlink for mssql tools
RUN ln -s /opt/mssql-tools/bin/sqlcmd /usr/bin/sqlcmd && \
    ln -s /opt/mssql-tools/bin/bcp /usr/bin/bcp

WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 80
EXPOSE 443

VOLUME /data
VOLUME /backups

ENV ASPNETCORE_URLS=http://+:80;https://+:443
ENV ASPNETCORE_Kestrel__Certificates__Default__Password=test1345
ENV ASPNETCORE_Kestrel__Certificates__Default__Path=/app/https/DatabaseBackupManager.pfx

RUN chmod +x DatabaseBackupManager

ENTRYPOINT ["./DatabaseBackupManager"]

