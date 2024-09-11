@echo off
setlocal

:: Wait for a short time to ensure the primary script has finished
timeout /t 5 /nobreak

:: Delete the primary batch script and the zip file
del "%1\UniversalGametypeEditor.zip"
del "%1\unzip.bat"

:: Delete the temporary directory
rmdir /s /q "%1"

:: Delete this script
del "%~f0"

endlocal
exit /b
