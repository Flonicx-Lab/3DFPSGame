@echo off
echo ===================================================
echo  3D FPS Game Project Initializer
echo ===================================================
echo.

echo [1/2] Restoring dotnet tools...
call dotnet tool restore
if %ERRORLEVEL% NEQ 0 exit /b %ERRORLEVEL%

echo [2/2] Installing Husky Git hooks...
call dotnet husky install
if %ERRORLEVEL% NEQ 0 exit /b %ERRORLEVEL%

echo Setup completed successfully.
pause
