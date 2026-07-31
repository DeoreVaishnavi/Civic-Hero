# CivicHero multi-service deployment

## Local and container-hosted topology

`docker-compose.yml` runs three isolated containers on `civichero-network`:

- `backend` is the only service published to the host, on port 8080.
- `chatbot-service` is reachable only inside Docker on port 8001.
- `vision-service` is reachable only inside Docker on port 8002.
- AWS RDS MySQL and Amazon S3 remain external managed services.

Copy `.env.example` to `.env`, replace every placeholder locally, and never commit `.env`.

```powershell
Copy-Item .env.example .env
docker compose config
docker compose up --build
```

Check the stack:

```text
http://localhost:8080/api/health
http://localhost:8080/api/v1/health/dependencies
```

Stop it with `docker compose down`. The chatbot vector index is retained in the
`chatbot-index` named volume. RDS and S3 data are not removed by this command.

## Internal authentication

The backend attaches `X-Internal-Service-Key` to chatbot and vision requests.
Both Python services compare it with `INTERNAL_SERVICE_KEY`. Use the same strong,
random value for all three containers. Do not expose either Python port publicly.

The vision API supports:

- `POST /api/v1/vision/verify` for a complaint image and expected category.
- `POST /api/v1/vision/compare-resolution` for before/after verification.

Vision failures remain advisory. Existing background processing and rule-based
fallbacks ensure unavailable model inference does not prevent complaint creation.

## Vercel deployment boundary

Vercel does not start this Compose stack as a persistent private Docker network.
Deploy the React/Vite frontend as its own Vercel project from
`frontend/civichero-web`. If deploying the .NET image with a supported Vercel
container workflow, use `backend/Dockerfile.vercel` and configure public HTTPS
URLs for independently hosted chatbot and vision services.

Required backend production variables include:

```text
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection=...
Jwt__SecretKey=...
AWS__Region=ap-south-1
AWS__AccessKey=...
AWS__SecretKey=...
AWS__BucketName=civichero-images-mitesh
ChatbotService__BaseUrl=https://chatbot.example.com
VisionService__BaseUrl=https://vision.example.com
InternalServices__ApiKey=...
```

The independently hosted Python services must receive the same
`INTERNAL_SERVICE_KEY`. The chatbot additionally receives its Groq/NVIDIA keys;
the vision service receives model/runtime configuration only. Neither Python
service receives RDS, AWS, or JWT credentials.

Set the frontend variable to the deployed backend origin:

```text
VITE_API_BASE_URL=https://your-backend.example.com
```

## Verification order

1. Start Docker Desktop and wait for the Linux engine.
2. Run `docker compose --env-file .env config --quiet`.
3. Run `docker compose up --build`.
4. Confirm all three health checks are healthy with `docker compose ps`.
5. Call the backend health endpoints.
6. Test login and complaint submission.
7. Confirm image objects appear in S3 and metadata appears in RDS.
8. Stop the stack and scan Git for local secrets before committing.
