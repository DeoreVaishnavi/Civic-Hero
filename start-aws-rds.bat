@echo off
setlocal
cd /d "%~dp0"
title CivicHero - Start with AWS RDS
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\docker\Start-AwsRds-Docker.ps1"
if errorlevel 1 pause
endlocal
