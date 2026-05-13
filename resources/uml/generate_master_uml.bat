@echo off
setlocal enabledelayedexpansion

echo Generating Master UML Diagram... 
echo.

set OUTPUT_FILE=MasterDiagram.puml

REM Create/overwrite the master file (start empty)
type nul > %OUTPUT_FILE%

REM Find all .puml files excluding obj folder and *.include.puml files
set COUNT=0
for /r %%f in (*.puml) do (
    echo %%f | findstr /i "\obj\\" > nul
    if errorlevel 1 (
        echo %%f | findstr /i "\.include\.puml$" > nul
        if errorlevel 1 (
            type "%%f" >> %OUTPUT_FILE%
            echo. >> %OUTPUT_FILE%
            echo. >> %OUTPUT_FILE%
            
            set /a COUNT+=1
            echo Processed: %%~f
        ) else (
            echo Skipped include file: %%~f
        )
    )
)

echo.
echo ========================================
echo Done! Processed %COUNT% diagram(s)
echo Output file: %OUTPUT_FILE%
echo Please remove all !includes
echo ========================================
echo.

pause