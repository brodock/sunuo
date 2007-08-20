#
#  SunUO
#  $Id$
#
#  (c) 2005-2006 Max Kellermann <max@duempel.org>
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

MCS := mcs

SUNUO_BASE = $(HOME)/dl/runuo

VERSION := $(shell perl -ne 'print "$$1\n" if /^sunuo \((.*?)\)/' debian/changelog |head -1)
DISTDIR = build/sunuo-$(VERSION)
DISTDLL = MySql.Data.dll
DIST_FILES = bin/mono/log4net.dll bin/mono/Npgsql.dll

MCS_FLAGS += -define:MONO -debug -lib:lib
CP_FLAGS = -lf

SUNUO_SOURCES := $(shell find src -name "*.cs" )
SUNLOGIN_SOURCES := src/AssemblyInfo.cs $(shell find login -name "*.cs" ) $(shell find src/Network/Encryption -name "*.cs" )
SUNLOGIN_SOURCES += src/Network/MessagePump.cs src/Network/ByteQueue.cs src/Network/PacketReader.cs src/Network/Listener.cs src/Network/SendQueue.cs src/Network/BufferPool.cs src/Network/PacketWriter.cs src/ClientVersion.cs src/Config.cs src/Timer.cs src/Insensitive.cs src/Network/PacketProfile.cs src/Attributes.cs src/Network/Compression.cs src/Network/PacketHandler.cs

SCRIPTS = legacy reports remote-admin myrunuo profiler
SCRIPTS_DLL = $(patsubst %,build/scripts/%.dll,$(SCRIPTS))

export MONO_PATH := .:lib

ifeq ($(DEBIAN),1)
export MONO_PATH := $(MONO_PATH):/usr/lib/cli/log4net-1.2
else
LOG4NET_DEPS := lib/log4net.dll
endif

ifeq ($(PORTABLE),1)
DIST_FILES += bin/w32/SunUO.exe bin/w32/zlib.dll bin/w32/log4net.dll bin/w32/Npgsql.dll
DIST_FILES += bin/w64/SunUO.exe bin/w64/zlib.dll bin/w64/log4net.dll bin/w64/Npgsql.dll
endif

.PHONY: all core debian-all clean install

all: core build/SunLogin.exe build/UOGQuery.exe $(SCRIPTS_DLL)

core: build/SunUO.exe

debian-all: core $(SCRIPTS_DLL) docs

clean:
	rm -f doc/sunuo.html
	rm -rf build

install: all
	install -d -m 0755 $(SUNUO_BASE) $(SUNUO_BASE)/etc $(SUNUO_BASE)/local $(SUNUO_BASE)/local/lib
	install -m 0755 run.sh build/SunUO.exe $(SUNUO_BASE)/
	test -f build/SunUO.exe.mdb && install -m 0644 build/SunUO.exe.mdb $(SUNUO_BASE)/
	test -f $(SUNUO_BASE)/SunUO.exe.config || install -m 0644 conf/SunUO.exe.config $(SUNUO_BASE)/
	test -f $(SUNUO_BASE)/SunLogin.exe.config || install -m 0644 conf/SunLogin.exe.config $(SUNUO_BASE)/
	test -f $(SUNUO_BASE)/etc/sunuo.xml || install -m 0644 conf/sunuo.xml $(SUNUO_BASE)/etc/
	install -m 0644 $(addprefix lib/,$(DISTDLL)) $(SUNUO_BASE)/
	install -m 0644 $(SCRIPTS_DLL) $(SUNUO_BASE)/local/lib/

# compile targets

build/SunUO.exe: $(SUNUO_SOURCES) $(LOG4NET_DEPS)
	@mkdir -p $(dir $@)
	@rm -f $@.mdb
	$(MCS) $(MCS_FLAGS) -out:$@ -r:log4net.dll $(SUNUO_SOURCES)

build/SunLogin.exe: $(SUNLOGIN_SOURCES) build/MySql.Data.dll $(LOG4NET_DEPS)
	@mkdir -p $(dir $@)
	@rm -f $@.mdb
	$(MCS) $(MCS_FLAGS) -out:$@ -r:System.Data.dll -r:MySql.Data -r:log4net.dll $(SUNLOGIN_SOURCES)

build/UOGQuery.exe: util/UOGQuery.cs
	@mkdir -p $(dir $@)
	@rm -f $@.mdb
	$(MCS) $(MCS_FLAGS) -out:$@ util/UOGQuery.cs

build/scripts/legacy.dll: LIBS = System.Web.dll System.Data.dll log4net.dll
build/scripts/legacy.dll: build/SunUO.exe
	@mkdir -p $(dir $@)
	$(MCS) $(MCS_FLAGS) -target:library -out:$@ -lib:build $(addprefix -r:,$(LIBS)) -r:SunUO.exe -recurse:'scripts/legacy/*.cs'

build/scripts/reports.dll: LIBS = System.Drawing.dll System.Web.dll System.Windows.Forms.dll log4net.dll
build/scripts/reports.dll: build/SunUO.exe build/scripts/legacy.dll
	@mkdir -p $(dir $@)
	$(MCS) $(MCS_FLAGS) -target:library -out:$@ -lib:build $(addprefix -r:,$(LIBS)) -r:SunUO.exe -lib:build/scripts -r:legacy.dll -recurse:'scripts/reports/*.cs'

build/scripts/remote-admin.dll: LIBS = log4net.dll
build/scripts/remote-admin.dll: build/SunUO.exe build/scripts/legacy.dll
	@mkdir -p $(dir $@)
	$(MCS) $(MCS_FLAGS) -target:library -out:$@ -lib:build $(addprefix -r:,$(LIBS)) -r:SunUO.exe -lib:build/scripts -r:legacy.dll -recurse:'scripts/remote-admin/*.cs'

build/scripts/myrunuo.dll: LIBS = System.Data.dll log4net.dll
build/scripts/myrunuo.dll: build/SunUO.exe build/scripts/legacy.dll
	@mkdir -p $(dir $@)
	$(MCS) $(MCS_FLAGS) -target:library -out:$@ -lib:build $(addprefix -r:,$(LIBS)) -r:SunUO.exe -lib:build/scripts -r:legacy.dll -recurse:'scripts/myrunuo/*.cs'

build/scripts/profiler.dll: build/SunUO.exe build/scripts/legacy.dll
	@mkdir -p $(dir $@)
	$(MCS) $(MCS_FLAGS) -target:library -out:$@ -lib:build -r:SunUO.exe -lib:build/scripts -r:legacy.dll -recurse:'scripts/profiler/*.cs'

# dist targets

.PHONY: dist
dist: build/dist/sunuo-$(VERSION).zip build/dist/sunuo-$(VERSION)-src.zip

$(addprefix $(DISTDIR)/,COPYING AUTHORS README): $(DISTDIR)/%: %
	@mkdir -p $(dir $@)
	cp $(CP_FLAGS) $< $@

$(DISTDIR)/bin/mono/SunUO.exe: $(DISTDIR)/bin/mono/%: build/%
	@mkdir -p $(dir $@)
	test -f $<.mdb && cp $(CP_FLAGS) $<.mdb $@.mdb
	cp $(CP_FLAGS) $< $@

$(DISTDIR)/SunUO.exe.config: conf/SunUO.exe.config
	@mkdir -p $(dir $@)
	cp $(CP_FLAGS) $< $@

$(DISTDIR)/run.sh $(DISTDIR)/run.bat $(DISTDIR)/run64.bat: $(DISTDIR)/%: %
	@mkdir -p $(dir $@)
	install -m 0755 $< $@

$(DISTDIR)/etc/sunuo.xml: conf/sunuo.xml
	@mkdir -p $(dir $@)
	cp $(CP_FLAGS) $< $@

$(DISTDIR)/SunLogin.exe.config: conf/SunLogin.exe.config
	@mkdir -p $(dir $@)
	cp $(CP_FLAGS) $< $@

$(DISTDIR)/changelog: debian/changelog
	@mkdir -p $(dir $@)
	cp $(CP_FLAGS) $< $@

$(addprefix $(DISTDIR)/,$(DISTDLL)): $(DISTDIR)/%: lib/%
	@mkdir -p $(dir $@)
	cp $(CP_FLAGS) $< $@

$(DISTDIR)/bin/mono/Npgsql.dll: $(DISTDIR)/bin/mono/%: lib/%
	@mkdir -p $(dir $@)
	cp $(CP_FLAGS) $< $@

$(DISTDIR)/bin/mono/log4net.dll: $(DISTDIR)/bin/mono/%: lib/%
	@mkdir -p $(dir $@)
	cp $(CP_FLAGS) $< $@

ifeq ($(PORTABLE),1)

build/w32/SunUO.exe:
	@mkdir -p $(dir $@)
	scp tiger:svn/sunuo/build/32/SunUO.{exe,pdb} $(dir $@)

build/w64/SunUO.exe:
	@mkdir -p $(dir $@)
	scp tiger:svn/sunuo/build/64/SunUO.{exe,pdb} $(dir $@)

$(DISTDIR)/bin/w32/SunUO.exe: build/w32/SunUO.exe
	@mkdir -p $(dir $@)
	cp $(CP_FLAGS) build/w32/SunUO.exe build/w32/SunUO.pdb $(dir $@)

$(DISTDIR)/bin/w32/zlib.dll: $(DISTDIR)/bin/%: lib/%
	@mkdir -p $(dir $@)
	cp $(CP_FLAGS) $< $@

$(DISTDIR)/bin/w32/Npgsql.dll: $(DISTDIR)/bin/%: lib/%
	@mkdir -p $(dir $@)
	cp $(CP_FLAGS) $< $@

$(DISTDIR)/bin/w32/log4net.dll: $(DISTDIR)/bin/%: lib/%
	@mkdir -p $(dir $@)
	cp $(CP_FLAGS) $< $@

$(DISTDIR)/bin/w64/SunUO.exe: build/w64/SunUO.exe
	@mkdir -p $(dir $@)
	cp $(CP_FLAGS) build/w64/SunUO.exe build/w64/SunUO.pdb $(dir $@)

$(DISTDIR)/bin/w64/zlib.dll: $(DISTDIR)/bin/%: lib/%
	@mkdir -p $(dir $@)
	cp $(CP_FLAGS) $< $@

$(DISTDIR)/bin/w64/Npgsql.dll: $(DISTDIR)/bin/%: lib/%
	@mkdir -p $(dir $@)
	cp $(CP_FLAGS) $< $@

$(DISTDIR)/bin/w64/log4net.dll: $(DISTDIR)/bin/%: lib/%
	@mkdir -p $(dir $@)
	cp $(CP_FLAGS) $< $@

endif

.PHONY: export-scripts export-data export-saves

export-scripts:
	rm -rf $(DISTDIR)/Scripts $(DISTDIR)/local/src/profiler
	@mkdir -p $(DISTDIR)/local/src
	svn export scripts/legacy $(DISTDIR)/Scripts 
	svn export scripts/profiler $(DISTDIR)/local/src/profiler

export-data:
	rm -rf $(DISTDIR)/Data
	svn export data $(DISTDIR)/Data

export-saves:
	rm -rf $(DISTDIR)/Saves
	svn export saves $(DISTDIR)/Saves

build/dist/sunuo-$(VERSION).zip: $(addprefix $(DISTDIR)/,bin/mono/SunUO.exe SunUO.exe.config run.sh run.bat run64.bat sunuo.html COPYING AUTHORS README changelog etc/sunuo.xml $(DISTDLL) $(DIST_FILES)) export-scripts export-data export-saves
	@mkdir -p $(dir $@)
	cd build && fakeroot zip -q -r $(shell pwd)/$@ sunuo-$(VERSION)

.PHONY: svn-export
svn-export:
	@rm -rf build/tmp
	@mkdir -p build/tmp
	svn export . build/tmp/sunuo-$(VERSION)-src

build/dist/sunuo-$(VERSION)-src.zip: svn-export
	@mkdir -p build/tmp/sunuo-$(VERSION)-src/lib/w32
	cp $(addprefix lib/,$(DISTDLL)) build/tmp/sunuo-$(VERSION)-src/lib/
	cp lib/w32/zlib.dll build/tmp/sunuo-$(VERSION)-src/lib/w32/
	@mkdir -p $(dir $@)
	cd build/tmp && fakeroot zip -q -r $(shell pwd)/$@ sunuo-$(VERSION)-src
	@rm -rf build/tmp

# auto-download targets

ifneq ($(DEBIAN),1)

download/mysql-connector-net-1.0.7-noinstall.zip:
	@mkdir -p $(dir $@)
	wget http://sunsite.informatik.rwth-aachen.de/mysql/Downloads/Connector-Net/mysql-connector-net-1.0.7-noinstall.zip -O $@.tmp
	mv $@.tmp $@

lib/MySql.Data.dll: download/mysql-connector-net-1.0.7-noinstall.zip
	rm -rf build/tmp && mkdir -p build/tmp
	unzip -q -d build/tmp $<
	@mkdir -p $(dir $@)
	cp build/tmp/bin/mono-1.0/release/MySql.Data.dll lib/
	rm -rf build/tmp

download/Npgsql1.0-bin-mono1.1.tar.bz2:
	@mkdir -p $(dir $@)
	wget http://pgfoundry.org/frs/download.php/1099/Npgsql1.0-bin-mono1.1.tar.bz2 -O $@.tmp
	mv $@.tmp $@

download/Npgsql1.0-bin-ms1.1.tar.bz2:
	@mkdir -p $(dir $@)
	wget http://pgfoundry.org/frs/download.php/1105/Npgsql1.0-bin-ms1.1.tar.bz2 -O $@.tmp
	mv $@.tmp $@

download/Npgsql1.0-bin-ms2.0.tar.bz2:
	@mkdir -p $(dir $@)
	wget http://pgfoundry.org/frs/download.php/1109/Npgsql1.0-bin-ms2.0.tar.bz2 -O $@.tmp
	mv $@.tmp $@

lib/Npgsql.dll: download/Npgsql1.0-bin-mono1.1.tar.bz2
	rm -rf build/tmp && mkdir -p build/tmp
	tar xjfC $< build/tmp
	@mkdir -p $(dir $@)
	cp build/tmp/Npgsql1.0/Npgsql/bin/mono1.1/Npgsql.dll lib/
	rm -rf build/tmp

lib/w32/Npgsql.dll: download/Npgsql1.0-bin-ms1.1.tar.bz2
	rm -rf build/tmp && mkdir -p build/tmp
	tar xjfC $< build/tmp
	@mkdir -p $(dir $@)
	cp build/tmp/Npgsql1.0/Npgsql/bin/ms1.1/Npgsql.dll lib/w32/
	rm -rf build/tmp

lib/w64/Npgsql.dll: download/Npgsql1.0-bin-ms2.0.tar.bz2
	rm -rf build/tmp && mkdir -p build/tmp
	tar xzfC $< build/tmp
	@mkdir -p $(dir $@)
	cp build/tmp/Npgsql1.0/Npgsql/bin/ms2.0/Npgsql.dll lib/w64/
	rm -rf build/tmp

download/incubating-log4net-1.2.10.zip:
	@mkdir -p $(dir $@)
	wget http://cvs.apache.org/dist/incubator/log4net/1.2.10/incubating-log4net-1.2.10.zip -O $@.tmp
	mv $@.tmp $@

lib/log4net.dll lib/w32/log4net.dll lib/w64/log4net.dll: download/incubating-log4net-1.2.10.zip
	rm -rf build/tmp && mkdir -p build/tmp
	unzip -q -d build/tmp $<
	@mkdir -p lib/w32 lib/w64
	cp build/tmp/log4net-1.2.10/bin/mono/1.0/release/log4net.dll lib/
	cp build/tmp/log4net-1.2.10/bin/net/1.1/release/log4net.dll lib/w32
	cp build/tmp/log4net-1.2.10/bin/net/2.0/release/log4net.dll lib/w64
	rm -rf build/tmp

$(addprefix build/,$(DISTDLL)): build/%: lib/%
	@mkdir -p $(dir $@)
	cp $(CP_FLAGS) $< $@

download/zlib123-dll.zip:
	@mkdir -p $(dir $@)
	wget http://www.gzip.org/zlib/zlib123-dll.zip -O $@.tmp
	mv $@.tmp $@

lib/w32/zlib.dll: download/zlib123-dll.zip
	rm -rf build/tmp && mkdir -p build/tmp
	unzip -q -d build/tmp $<
	@mkdir -p $(dir $@)
	cp build/tmp/zlib1.dll $@
	rm -rf build/tmp

download/zlib123dllx64.zip:
	@mkdir -p $(dir $@)
	wget http://www.winimage.com/zLibDll/zlib123dllx64.zip -O $@.tmp
	mv $@.tmp $@

lib/w64/zlib.dll: download/zlib123dllx64.zip
	rm -rf build/tmp && mkdir -p build/tmp
	unzip -q -d build/tmp $<
	@mkdir -p $(dir $@)
	cp build/tmp/dll_x64/zlibwapi.lib $@
	rm -rf build/tmp

endif

# documentation targets

.PHONY: docs
docs: doc/sunuo.html
doc/sunuo.html: doc/sunuo.xml
	xmlto -v -o $(dir $@) xhtml-nochunks $<

$(DISTDIR)/sunuo.html: doc/sunuo.html
	cp $(CP_FLAGS) $< $@

# release targets

export:
	rm -rf /mnt/misc/sunuo /mnt/misc/runuo/SunUO.exe
	svn export . /mnt/misc/sunuo

release: all docs
	rm -rf /tmp/sunuo
	mkdir -p /tmp/sunuo
	svn export . /tmp/sunuo/sunuo-$(VERSION)
	cd /tmp/sunuo && fakeroot zip -qr sunuo-$(VERSION).zip sunuo-$(VERSION)
	cd /tmp/sunuo && fakeroot tar cjf sunuo-$(VERSION).tar.bz2 sunuo-$(VERSION)
	mkdir -p /tmp/sunuo/sunuo-$(VERSION)-bin
	cp AUTHORS COPYING NEWS README doc/sunuo.html /tmp/sunuo/sunuo-$(VERSION)-bin
	cp debian/changelog /tmp/sunuo/sunuo-$(VERSION)-bin/changelog
	cp src/SunUO.exe util/UOGQuery.exe /tmp/sunuo/sunuo-$(VERSION)-bin
	cd /tmp/sunuo && fakeroot zip -qr sunuo-$(VERSION)-bin.zip sunuo-$(VERSION)-bin

upload: docs
	scp README NEWS debian/changelog doc/sunuo.html max@swift:/var/www/gzipped/download/sunuo/doc/
	ssh max@swift chmod a+rX -R /var/www/gzipped/download/sunuo/doc/
