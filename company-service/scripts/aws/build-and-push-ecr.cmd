@echo off
powershell -ExecutionPolicy Bypass -File "%~dp0build-and-push-ecr.ps1" %*
