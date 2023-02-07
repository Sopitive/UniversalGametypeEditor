@echo off
echo "Unzipping the UniversalGametypeEditor.zip...
timeout 3
tar -xf UniversalGametypeEditor.zip
echo "Restarting UniversalGametypeEditor..."
timeout 3
start %~dp0UniversalGametypeEditor.exe