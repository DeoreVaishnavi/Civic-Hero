# CivicHero Docker with AWS RDS and S3

This profile runs the frontend, backend, Redis, RabbitMQ and Nginx in Docker,
while using the existing AWS RDS MySQL database and Amazon S3 bucket.

It does not start a local MySQL container.

## Why a connection string alone sometimes fails

The connection string must be available inside the backend container. The
CivicHero Docker entrypoint loads it from:

```text
deployment/.secrets/rds_connection.txt
```

Host `.NET user-secrets` are not automatically available inside Docker.

The RDS endpoint must also be reachable:

- Direct mode: RDS is publicly accessible, or the laptop is connected to the VPC by VPN.
- SSM mode: a private RDS endpoint is reached through an SSM-managed EC2 instance.

## Apply package

```powershell
powershell -ExecutionPolicy Bypass `
  -File "C:\CivicHeroDockerAwsRds\CivicHero_Docker_AWS_RDS_Setup\apply-docker-aws-rds-setup.ps1" `
  -ProjectRoot "C:\CivicHeroDev\CivicHeroSolution"
```

## Configure

Double-click:

```text
configure-aws-rds.bat
```

Choose:

```text
Direct
```

when the RDS port is reachable from the laptop.

Choose:

```text
SsmTunnel
```

when RDS is private and an SSM-managed EC2 instance exists in the VPC.

## Start

```text
start-aws-rds.bat
```

Addresses:

```text
Website: http://localhost:8088
Swagger: http://localhost:5180/swagger
Health:  http://localhost:5180/health/ready
```

## Stop

```text
stop-aws-rds.bat
```

## Logs

```text
aws-rds-logs.bat
```

## Database safety

The override explicitly sets:

```text
Database__ApplyMigrationsOnStartup=false
```

Starting Docker therefore does not alter the RDS schema. Normal application
actions can still insert or update business records.

## Recommended team options

1. Public development RDS with each teammate's current IP allowed on port 3306.
2. Private RDS with AWS Client VPN.
3. Private RDS with SSM port forwarding through an EC2 managed node.
4. Deploy Docker once to an EC2 instance in the VPC and let teammates use a shared URL.
