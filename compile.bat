@ECHO OFF

cd src

%windir%\Microsoft.NET\Framework\v1.1.4322\csc.exe /nologo /out:../SunUO.exe /unsafe /recurse:*.cs

cd ..\util

%windir%\Microsoft.NET\Framework\v1.1.4322\csc.exe /nologo /out:UOGQuery.exe /unsafe UOGQuery.cs

PAUSE
