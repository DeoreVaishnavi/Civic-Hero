@echo off
setlocal
cd /d "%~dp0"
title CivicHero - AWS RDS Logs
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\docker\Logs-AwsRds-Docker.ps1"
endlocal
