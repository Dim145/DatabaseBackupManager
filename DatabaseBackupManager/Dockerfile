FROM mcr.microsoft.com/dotnet/sdk:6.0.408 AS build
WORKDIR /src
COPY ["DatabaseBackupManager/DatabaseBackupManager.csproj", "DatabaseBackupManager/"]
RUN dotnet restore "DatabaseBackupManager/DatabaseBackupManager.csproj" --runtime linux-x64

RUN dotnet dev-certs https --clean
RUN dotnet dev-certs https -ep /app/publish/https/DatabaseBackupManager.pfx -p test1345
RUN dotnet dev-certs https --trust

COPY . .

WORKDIR "/src/DatabaseBackupManager"
RUN dotnet publish "DatabaseBackupManager.csproj" -c Release -o /app/publish --no-restore --self-contained true --runtime linux-x64

FROM debian:buster-slim AS final

RUN apt-get update && apt-get install -y curl gnupg2
RUN curl https://packages.microsoft.com/keys/microsoft.asc | apt-key add - && \
        curl https://packages.microsoft.com/config/debian/10/prod.list | tee /etc/apt/sources.list.d/msprod.list

RUN export ACCEPT_EULA=Y && apt-get update && apt-get install -y \
    libgdiplus \
    openssl \
    postgresql-client \
    default-mysql-client \
    mssql-tools \
    sqlite3 \
    && rm -rf /var/lib/apt/lists/* 

# create symlink for mssql tools
RUN ln -s /opt/mssql-tools/bin/sqlcmd /usr/bin/sqlcmd && \
    ln -s /opt/mssql-tools/bin/bcp /usr/bin/bcp

WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 80
EXPOSE 443

ENV ASPNETCORE_URLS=http://+:80;https://+:443
ENV ASPNETCORE_Kestrel__Certificates__Default__Password=test1345
ENV ASPNETCORE_Kestrel__Certificates__Default__Path=/app/https/DatabaseBackupManager.pfx

RUN mkdir /data

RUN chmod +x DatabaseBackupManager

ENTRYPOINT ["./DatabaseBackupManager"]

