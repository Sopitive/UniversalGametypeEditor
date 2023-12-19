@echo off
echo "Unzipping the UniversalGametypeEditor.zip...
timeout 3
tar -xf UniversalGametypeEditor.zip
echo "Restarting UniversalGametypeEditor..."
timeout 3
cd win-x64
start %~dp0UniversalGametypeEditor.exe
del %~dp0UniversalGametypeEditor.zip
start /b "" cmd /c del "%~f0"&exit /b