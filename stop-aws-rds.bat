@echo off
setlocal
cd /d "%~dp0"
title CivicHero - Stop AWS RDS profile
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\docker\Stop-AwsRds-Docker.ps1"
pause
endlocal
