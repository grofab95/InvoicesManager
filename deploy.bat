@echo off
setlocal

:: ==== CONFIG ====
set "SERVICE_NAME=IM"
set "CSPROJ_PATH=D:\IM\IM\IM.csproj"
set "PUBLISH_DIR=D:\IM\IM\bin\Publish"
set "DEPLOY_DIR=C:\fgCode\IM\bin"

echo.
echo ===== 1) Publishing .NET 8 app =====
dotnet publish "%CSPROJ_PATH%" -c Release -o "%PUBLISH_DIR%"
if errorlevel 1 (
    echo Publish failed!
    exit /b 1
)

echo.
echo ===== 2) Stopping service %SERVICE_NAME% =====
sc stop "%SERVICE_NAME%"

echo Waiting for service to stop...
:waitstop
sc query "%SERVICE_NAME%" | find /I "STOPPED" >nul
if errorlevel 1 (
    timeout /t 1 >nul
    goto waitstop
)

echo.
echo ===== 3) Copying files =====
robocopy "%PUBLISH_DIR%" "%DEPLOY_DIR%" /E /R:2 /W:2 /COPY:DAT
if %ERRORLEVEL% GEQ 8 (
    echo Robocopy failed with error %ERRORLEVEL%.
    exit /b %ERRORLEVEL%
)

echo.
echo ===== 4) Starting service %SERVICE_NAME% =====
sc start "%SERVICE_NAME%"

echo Waiting for service to start...
:waitstart
sc query "%SERVICE_NAME%" | find /I "RUNNING" >nul
if errorlevel 1 (
    timeout /t 1 >nul
    goto waitstart
)

echo.
echo Deployment completed successfully!
endlocal
pause
