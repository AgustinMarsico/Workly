@echo off
powershell -ExecutionPolicy Bypass -File "%~dp0create-ecs-infra-and-task-definitions.ps1" %*
