# CivicHero Phase 2 — AWS RDS MySQL and Amazon S3

## Services

- AWS RDS MySQL 8 is the primary relational database.
- Amazon S3 stores complaint photos and resolution evidence.
- MySQL stores only S3 object keys and file metadata.
- Secrets are not committed to appsettings files.

## RDS connection string

Use .NET user secrets from `backend/CivicHero.Backend`:

```powershell
dotnet user-secrets set "ConnectionStrings:CivicHeroDatabase" "Server=YOUR_RDS_ENDPOINT;Port=3306;Database=civichero;User=YOUR_DB_USER;Password=YOUR_DB_PASSWORD;SslMode=Required;Connection Timeout=15;Default Command Timeout=30;"
```

## AWS configuration

```powershell
dotnet user-secrets set "AWS:Region" "ap-south-1"
dotnet user-secrets set "AWS:S3BucketName" "YOUR_PRIVATE_BUCKET_NAME"
```

Configure AWS credentials using the AWS CLI default credential chain:

```powershell
aws configure --profile civichero-dev
$env:AWS_PROFILE = "civichero-dev"
```

Never commit an access key or secret key.

## EF Core migrations

From the solution root:

```powershell
dotnet tool restore
cd backend\CivicHero.Backend
dotnet ef migrations add Phase2_InitialDatabase --output-dir Migrations
dotnet ef database update
```

Enable initial seed data for one development run:

```powershell
dotnet user-secrets set "Database:SeedDataOnStartup" "true"
dotnet run --launch-profile http
```

After departments and wards are seeded:

```powershell
dotnet user-secrets set "Database:SeedDataOnStartup" "false"
```

## Health endpoints

- `GET /api/v1/health/infrastructure`
- `GET /api/v1/health/database`
- `GET /api/v1/health/storage`
- `GET /health/database`
- `GET /health/storage`

## Required S3 permissions

The development IAM identity needs access only to the CivicHero bucket:

- `s3:GetBucketLocation`
- `s3:GetObject`
- `s3:PutObject`
- `s3:DeleteObject`

Keep Block Public Access enabled. The application uses short-lived pre-signed URLs for reads.
