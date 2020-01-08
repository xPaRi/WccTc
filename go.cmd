taskkill /IM TOTALCMD64.EXE
CHOICE /C ab /N /M "Continue..." /T 1 /D a
mkdir %COMMANDER_PATH%\plugins\wfx\WccTC
tar -xf "c:\dev\PARI\WccTC\WccTc\bin\Debug\out\WccTC.zip" --directory %COMMANDER_PATH%\plugins\wfx\WccTC
start %COMMANDER_PATH%\TOTALCMD64.EXE