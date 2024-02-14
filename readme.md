## Environment variable to personalize or config the application

### Database
- DatabaseType (default: sqlite) to choose the database type. Options: Sqlite, Postgres
- DefaultConnection : connection string to data  database. Example: "Data Source=app.db"
- HangfireDb: connection string to hangfire database. Example: "hangfire.db"

### Base data
- DataSettings__DefaultAdminRole default to "Admin"
- DataSettings__DefaultAdminEmail default to "admin@tochange.com"
- DataSettings__DefaultAdminPassword default to "Admin183!!"
- DataSettings__DefaultReaderRole default to "Reader"
- DataSettings__DefaultEditorRole default to "Editor"
- DataSettings__DefaultRestorerRole default to "Restorer"

### Email
- MailSettings__From
- MailSettings__Host
- MailSettings__UserName
- MailSettings__Password
- MailSettings__FromName
- MailSettings__Port default to 587
- MailSettings__UseSsl default to false

### Storage
- StorageSettings__StorageType options: "Local", "S3"

#### S3
- StorageSettings__TempPath default to "/tmp/s3"
- StorageSettings__S3Bucket
- StorageSettings__S3DaysRetention
- StorageSettings__ServerSideEncryption options: "SSE-S3"
- StorageSettings__AccessKey
- StorageSettings__SecretKey
- StorageSettings__S3Endpoint
- StorageSettings__S3UseSSL
- StorageSettings__S3Region
- StorageSettings__S3LinkExpiration

#### Local
Use backup path  in appssettings.json


### Information's
All env var can be replaced with value with  appsettings.json.  
Except for database connection, appsettings.json will override env var.  
All json attributes can be determinate with classes attribute of parameters in [Seeds.cs](./DatabaseBackupManager/Data/Seeds.cs)

## Exemple appssettings.json
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "BackupPath": "/data/backups",
  "DatabaseType": "Sqlite",
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=/data/app.db",
    "Hangfire": "/data/hangfire.db"
  },
  "MailSettings": null,
  "Authentication": {
    "Google": {
      "ClientId": "clientId",
      "ClientSecret": "secret"
    }
  },
  "StorageSettings": {
    "StorageType": "S3",
    "TempPath": "/tmp/s3",
    "S3Bucket": "backup-manager",
    "AccessKey": "accessKey",
    "SecretKey": "secretKey",
    "S3Endpoint": "http://S3Endpoint",
    "S3UseSSL": false,
    "S3Region": "eu-west-1",
    "S3LinkExpiration": 60
  }
}
```
