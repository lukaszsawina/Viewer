@echo off

set PROJECT_PATH=%cd%\Viewer
set PROJECT_FILE=%PROJECT_PATH%\Viewer.csproj

dotnet build "%PROJECT_FILE%" --configuration Release

for /f "delims=" %%i in ('dir /b /ad "%PROJECT_PATH%\bin\Release"') do set FRAMEWORK_FOLDER=%%i

if not defined FRAMEWORK_FOLDER (
    echo ERROR: %PROJECT_PATH%\bin\Release not found.
    exit /b
)

set EXE_FILE=%PROJECT_PATH%\bin\Release\%FRAMEWORK_FOLDER%\Viewer.exe

set SHORTCUT_PATH=%cd%\Viewer.lnk
set ICON_PATH=%PROJECT_PATH%\bin\Release\%FRAMEWORK_FOLDER%\Images\favicon.ico

echo Set WshShell = CreateObject("WScript.Shell") > CreateShortcut.vbs
echo Set oShortcut = WshShell.CreateShortcut("%SHORTCUT_PATH%") >> CreateShortcut.vbs
echo oShortcut.TargetPath = "%EXE_FILE%" >> CreateShortcut.vbs
echo oShortcut.IconLocation = "%ICON_PATH%" >> CreateShortcut.vbs
echo oShortcut.Save >> CreateShortcut.vbs

cscript //nologo CreateShortcut.vbs
del CreateShortcut.vbs

echo Viewer.exe shortcut was created.

pause
