REM @echo on

mkdir %SCRIPTS%
robocopy %RECIPE_DIR%\artifacts\StandaloneWindows64 %SCRIPTS%\NanoVer-iMD-VR /e
REM Make NanoverImd available in the Path while keeping it in
REM its directory.
set local_script=%%CONDA_PREFIX%%\Scripts%
echo "%local_script%\NanoVer-iMD-VR\NanoVer iMD.exe" > %SCRIPTS%\NanoVer-iMD-VR.bat
exit 0
