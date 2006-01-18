@ECHO OFF

set FRAMEWORK=%windir%\Microsoft.NET\Framework\v1.1.4322

cd src
%FRAMEWORK%\csc.exe /nologo /out:SunUO.exe /unsafe /recurse:*.cs

cd ..\util
%FRAMEWORK%\csc.exe /nologo /out:UOGQuery.exe /unsafe UOGQuery.cs

cd ..

PAUSE
