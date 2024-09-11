@echo off
setlocal
:: Function to check if a process is running
:CheckProcess
tasklist /FI "IMAGENAME eq UniversalGametypeEditor.exe" 2>NUL | find /I /N "UniversalGametypeEditor.exe">NUL
if "%ERRORLEVEL%"=="0" (
    echo UniversalGametypeEditor is still running...
    timeout /t 1 /nobreak
    goto CheckProcess
)

:: Unzip the file in the current directory
echo Unzipping the UniversalGametypeEditor.zip...
tar -xf UniversalGametypeEditor.zip -C .

:: Start the application
echo Restarting UniversalGametypeEditor...
cd win-x64
start UniversalGametypeEditor.exe

:: Go back to the parent directory
cd ..

:: Delete the zip file
del UniversalGametypeEditor.zip

:: Verify the zip file was deleted and then delete the batch script
if not exist UniversalGametypeEditor.zip (
    start /b "" cmd /c del "%~f0"&exit /b
) else (
    echo Failed to delete UniversalGametypeEditor.zip
    exit /b 1
)

endlocal
exit /b
