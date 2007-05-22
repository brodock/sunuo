@ECHO OFF

set TEMP=%HOME%\tmp
set TMP=%HOME%\tmp

mkdir %TEMP%
mkdir build
mkdir build\32

cd src

set FRAMEWORK=%windir%\Microsoft.NET\Framework\v1.1.4322
%FRAMEWORK%\csc.exe /nologo /debug:full /out:..\build\32\SunUO.exe /lib:..\lib /r:log4net.dll /recurse:*.cs

cd ..

PAUSE
