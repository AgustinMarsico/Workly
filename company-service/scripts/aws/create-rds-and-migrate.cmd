@echo off
powershell -ExecutionPolicy Bypass -File "%~dp0create-rds-and-migrate.ps1" %*
