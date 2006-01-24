@ECHO OFF

set FRAMEWORK=%windir%\Microsoft.NET\Framework\v1.1.4322

cd src
%FRAMEWORK%\csc.exe /nologo /out:SunUO.exe /lib:..\build\lib /r:log4net.dll /recurse:*.cs

cd ..\util
%FRAMEWORK%\csc.exe /nologo /out:UOGQuery.exe UOGQuery.cs

cd ..

PAUSE
