@echo off
SETLOCAL
set thisDir=%~dp0
set thisDir=%thisDir:~0,-1%
set buildDir=%thisDir%\Build
set srcDir=%thisDir%\src
set debug=
rem set debug=--include-symbols --include-source

rem REQUIRE gitversion
if NOT DEFINED GitVersion echo GitVersion not DEFINED & exit /b 1
if not exist "%GitVersion%" echo %GitVersion% not found & exit /b 1

for /f %%a in ('call %GitVersion% /showvariable NugetVersion') do set PackageVersion=%%a
for /f %%a in ('call %GitVersion% /showvariable InformationalVersion') do @set "versioninfo=%%a"
for /f %%a in ('call %GitVersion% /showvariable BranchName') do @set "BranchName=%%a"
for /f %%a in ('call %GitVersion% /showvariable Sha') do @set "CommitHash=%%a"

set pkgDesc=Electroneum API
set pkgReleaseNotes=Electroneum API (beta) Release NOTES
set pkgRepoUrl=https://github.com/stempy/electroneum-dotnet
set pkgAuthors=Rob Stemp
set packopts=/p:RepositoryUrl="%pkgRepoUrl%" /p:Authors="%pkgAuthors%" /p:PackageReleaseNotes="%pkgReleaseNotes%" /p:description="%pkgDesc%"

echo ===================================================================
echo Description     : %pkgDesc%
echo Release         : %pkgReleaseNotes%
echo Repository Url  : %pkgRepoUrl%
echo Options         : %packopts%
echo PackageVersion  : %PackageVersion%
echo Output Dir:     : %buildDir%
echo ===================================================================

if exist "%buildDir%" (
    pushd "%buildDir%"
    del /q/s *.*
    popd
)
if not exist "%buildDir%" md "%buildDir%"

pushd "%srcDir%"
echo Packing projects version ..... %PackageVersion%
call dotnet pack -o "%buildDir%" /p:version=%PackageVersion% %packopts% %debug%
if "%errorlevel%" NEQ "0" echo Error: failed to pack %errorlevel% & exit /b %errorlevel%
popd
ENDLOCAL