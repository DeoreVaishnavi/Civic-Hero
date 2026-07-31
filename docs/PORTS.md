# CivicHero Port Reference

CivicHero supports two local run modes. Use only one frontend mode at a time because both use host port `5173`.

## 1. Direct development mode

| Component | URL / port |
|---|---|
| React/Vite frontend | `http://localhost:5173` |
| ASP.NET Core backend | `http://localhost:5180` |
| Backend HTTPS profile | `https://localhost:7180` |
| API base URL | `http://localhost:5180/api/v1` |
| Backend liveness | `http://localhost:5180/health` or `/health/live` |
| Backend readiness | `http://localhost:5180/health/ready` |
| Redis, when run on the host | `6379` |
| RabbitMQ AMQP, when run on the host | `5672` |
| RabbitMQ management UI | `15672` |
| MySQL / AWS RDS | `3306` |

## 2. Docker Compose mode

| Component | Host port | Container port |
|---|---:|---:|
| CivicHero website through Nginx | `5173` | `80` |
| Backend direct access with AWS-RDS override | `5180` | `8080` |
| Frontend internal service | not published | `8080` |
| Backend internal service | not published by the base file | `8080` |
| Redis | not published | `6379` |
| RabbitMQ AMQP | not published | `5672` |
| RabbitMQ management UI | `15672` | `15672` |
| AWS RDS MySQL | remote `3306` | n/a |

The Docker public port is controlled by `PUBLIC_HTTP_PORT`. The default is `5173`.

Do not keep `npm run dev` running while starting the Docker website on port `5173`. Stop Vite first, or change `PUBLIC_HTTP_PORT` in the selected deployment environment file.

## Test-only ports

The isolated test Compose file uses `3307`, `6380`, `5673`, `15673`, `9002`, `9003`, `1026`, and `8026`. These do not replace the normal development ports.

## Windows port checks

```powershell
Get-NetTCPConnection -State Listen |
  Where-Object LocalPort -In 5173,5180,7180,3306,6379,5672,15672 |
  Sort-Object LocalPort |
  Format-Table LocalAddress,LocalPort,OwningProcess
```

To identify a process:

```powershell
Get-Process -Id <OwningProcess>
```
