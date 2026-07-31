@echo off
setlocal
cd /d "%~dp0"
title CivicHero - Configure AWS RDS
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\docker\Configure-AwsRds-Docker.ps1"
if errorlevel 1 pause
endlocal
