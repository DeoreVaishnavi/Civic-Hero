# Environment Configuration

## Non-secret variables

| Variable | Purpose |
|---|---|
| `PUBLIC_ORIGIN` | Exact HTTPS frontend origin used by CORS |
| `APP_HOST` | Nginx TLS hostname |
| `ALLOWED_HOSTS` | ASP.NET Core host filtering |
| `AWS_REGION` | S3 region |
| `AWS_BUCKET` | Private evidence bucket |
| `AI_PROVIDER` | `RuleBased` or `Gemini` |
| `RELEASE_VERSION` | Human-readable release identifier |
| `COMMIT_SHA` | Source revision |
| `BACKEND_IMAGE` | Immutable backend image |
| `FRONTEND_IMAGE` | Immutable frontend image |

## Secret files

The setup script creates these under `deployment/.secrets/`:

- `rds_connection.txt`
- `jwt_secret.txt`
- `aws_access_key.txt`
- `aws_secret_key.txt`
- `ai_api_key.txt`
- `grafana_admin_password.txt`

Empty AWS credential files are allowed when the production host uses an IAM role. The
backend entrypoint exports only non-empty secret values.

## GitHub environments

Create `staging` and `production` environments. Keep SSH credentials in environment
secrets, deployment paths/URLs in environment variables, restrict deployment branches,
and require approval for production.

### Staging secrets

- `STAGING_SSH_PRIVATE_KEY`
- `STAGING_HOST`
- `STAGING_USER`

### Staging variables

- `STAGING_URL`
- `STAGING_DEPLOY_PATH`

### Production secrets

- `PRODUCTION_SSH_PRIVATE_KEY`
- `PRODUCTION_HOST`
- `PRODUCTION_USER`

### Production variables

- `PRODUCTION_URL`
- `PRODUCTION_DEPLOY_PATH`
