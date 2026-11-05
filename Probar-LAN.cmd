@echo off
setlocal
set "DEMO_EXE=%~dp0Builds\Windows\Tavern and Chaos.exe"
if not exist "%DEMO_EXE%" (
  echo Primero abre Unity y usa Tavern - Build Windows LAN demo.
  pause
  exit /b 1
)
start "Tavern - anfitrion" "%DEMO_EXE%" -screen-width 960 -screen-height 600 -screen-fullscreen 0 -logFile "%~dp0Builds\Windows\anfitrion-manual.log"
start "Tavern - cliente" "%DEMO_EXE%" -screen-width 960 -screen-height 600 -screen-fullscreen 0 -logFile "%~dp0Builds\Windows\cliente-manual.log"
echo Se abrieron dos copias. Crea la sala en una y entra desde la otra.
echo Para este equipo usa IP 127.0.0.1 y puerto 7777 en ambas.
echo El Hito 1 queda pendiente de tu prueba manual.
endlocal
