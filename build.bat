@ECHO OFF

set TEMP=%HOME%\tmp
set TMP=%HOME%\tmp

mkdir %TEMP%
mkdir build
mkdir build\32
mkdir build\64

cd src

set FRAMEWORK=%windir%\Microsoft.NET\Framework\v1.1.4322
%FRAMEWORK%\csc.exe /nologo /debug:full /out:..\build\32\SunUO.exe /lib:..\lib /r:log4net.dll /recurse:*.cs

set FRAMEWORK=%windir%\Microsoft.NET\Framework64\v2.0.50727
%FRAMEWORK%\csc.exe /nologo /debug:full /out:..\build\64\SunUO.exe /lib:..\lib /r:log4net.dll /recurse:*.cs

cd ..

PAUSE
