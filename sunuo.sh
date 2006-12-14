#!/bin/bash
#
#  SunUO
#  $Id$
#
#  (c) 2006 Max Kellermann <max@duempel.org>
#
#   This program is free software; you can redistribute it and/or modify
#   it under the terms of the GNU General Public License as published by
#   the Free Software Foundation; version 2 of the License.
#
#   This program is distributed in the hope that it will be useful,
#   but WITHOUT ANY WARRANTY; without even the implied warranty of
#   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
#   GNU General Public License for more details.
#
#   You should have received a copy of the GNU General Public License
#   along with this program; if not, write to the Free Software
#   Foundation, Inc., 675 Mass Ave, Cambridge, MA 02139, USA.
#

# a few checks

if ! [ -f SunUO.exe ]; then
    echo "ERROR: SunUO.exe does not exist" >&2
    exit 2
fi

if ! [ -f SunUO.exe.config ]; then
    echo "Warning: SunUO.exe.config does not exist" >&2
fi

# prepare environment

export LANG=C
export LC_ALL=C

unset DISPLAY XAUTHORITY

export SUNUO_EXIT=99

# our main loop
while :; do
    # start SunUO.exe
    ${MONO:-mono} ${MONO_OPTS:---server --debug -O=all,-shared} SunUO.exe "$@"
    STATUS=$?
    echo "SunUO.exe exited with status $STATUS" >&2

    # exit status 99 means "really exit, do not restart"
    [ $STATUS == $SUNUO_EXIT ] && break

    # sleep for a reasonable duration and restart
    sleep 10
    echo "Re-starting SunUO.exe"
done

echo "Leaving $0" >&2
