@ECHO OFF
REM
REM  SunUO
REM  $Id$
REM
REM  (c) 2006-2007 Max Kellermann <max@duempel.org>
REM
REM   This program is free software; you can redistribute it and/or modify
REM   it under the terms of the GNU General Public License as published by
REM   the Free Software Foundation; version 2 of the License.
REM
REM   This program is distributed in the hope that it will be useful,
REM   but WITHOUT ANY WARRANTY; without even the implied warranty of
REM   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
REM   GNU General Public License for more details.
REM
REM   You should have received a copy of the GNU General Public License
REM   along with this program; if not, write to the Free Software
REM   Foundation, Inc., 675 Mass Ave, Cambridge, MA 02139, USA.
REM

GOTO COPY

:COPY_FAILED
echo Failed to copy files from bin\w32
GOTO END

:COPY

IF EXIST SunUO.exe GOTO AFTER_COPY_SUNUO
ECHO Copying SunUO.exe and SunUO.pdb from bin\w32

COPY /y bin\w32\SunUO.exe
IF ERRORLEVEL 1 GOTO COPY_FAILED

COPY /y bin\w32\SunUO.pdb
IF ERRORLEVEL 1 GOTO COPY_FAILED

COPY /y bin\w32\*.dll .
IF ERRORLEVEL 1 GOTO COPY_FAILED

:AFTER_COPY_SUNUO

set SUNUO_EXIT=99


:START

SunUO.exe

IF ERRORLEVEL 100 GOTO RESTART
IF ERRORLEVEL 99 GOTO END

:RESTART

GOTO START

:END
