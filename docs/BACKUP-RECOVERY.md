# Backup and Recovery

## AWS RDS

- Enable automated backups and point-in-time recovery.
- Retain backups according to municipal policy.
- Create a manual snapshot before every schema migration and major release.
- Test restoration into a non-production RDS instance on a scheduled basis.

Create a release snapshot:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/deployment/create-rds-snapshot.ps1 `
  -DbInstanceIdentifier civichero-production `
  -AwsRegion ap-south-1
```

## Amazon S3

- Enable bucket versioning.
- Enable default encryption.
- Block public access.
- Configure lifecycle rules for old versions and incomplete multipart uploads.
- Use least-privilege IAM access limited to the CivicHero bucket/prefixes.

## Recovery order

1. Confirm the incident and stop writes when necessary.
2. Choose the RDS point-in-time or snapshot.
3. Restore into a new RDS instance; never overwrite the original immediately.
4. Validate schema, record counts and critical workflows.
5. Update the secret connection string.
6. Restart the backend and verify readiness.
7. Preserve the old instance until formal recovery approval.

## Recovery targets

Academic/MVP targets should be documented explicitly. A reasonable initial objective is an
RPO of 24 hours and an RTO of 4 hours, then improved as production requirements mature.
