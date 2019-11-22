REM USAGE: Install.bat <DEBUG/RELEASE> <UUID>
REM Example: Install.bat RELEASE com.barraider.spotify
@echo off
setlocal

REM MAKE SURE THE FOLLOWING ARE CORRECT
SET OUTPUT_DIR=C:\TEMP
SET INPUT_DIR=%USERPROFILE%\source\repos\net.bootscreen.fhem\bin\Debug
SET DISTRIBUTION_TOOL=%USERPROFILE%\source\repos\DistributionTool.exe
SET STREAM_DECK_FILE=%ProgramW6432%\Elgato\StreamDeck\StreamDeck.exe
SET UUID=net.bootscreen.fhem

cd %INPUT_DIR%

taskkill /f /im streamdeck.exe
taskkill /f /im %UUID%.exe
REM timeout /t 2
del %OUTPUT_DIR%\%UUID%.streamDeckPlugin
"%DISTRIBUTION_TOOL%" -b -i "%UUID%.sdPlugin" -o "%OUTPUT_DIR%"
rmdir "%APPDATA%\Elgato\StreamDeck\Plugins\%UUID%.sdPlugin" /s /q
START "" "%STREAM_DECK_FILE%"
REM timeout /t 3
@ping localhost -n 4 > NUL
"%OUTPUT_DIR%\%UUID%.streamDeckPlugin"
