@echo off
powershell -ExecutionPolicy Bypass -File "%~dp0register-ecs-task-definitions.ps1" %*
