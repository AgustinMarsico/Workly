@echo off
powershell -ExecutionPolicy Bypass -File "%~dp0create-ecs-services.ps1" %*
