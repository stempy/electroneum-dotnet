@echo off
SETLOCAL
set thisDir=%~dp0
set thisDir=%thisDir:~0,-1%

call "%thisDir%\pack.cmd"

ENDLOCAL
