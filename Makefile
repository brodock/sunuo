include config.mk

VERSION := $(shell perl -ne 'print "$$1\n" if /^sunuo \((.*?)\)/' debian/changelog |head -1)
DISTDIR = build/sunuo-$(VERSION)-bin
DISTDLL = MySql.Data.dll Npgsql.dll log4net.dll

MCS_FLAGS += -define:MONO -debug -lib:lib

SUNUO_SOURCES := $(shell find src -name "*.cs" )
SUNLOGIN_SOURCES := src/AssemblyInfo.cs $(shell find login -name "*.cs" ) $(shell find src/Network/Encryption -name "*.cs" )
SUNLOGIN_SOURCES += src/Network/MessagePump.cs src/Network/ByteQueue.cs src/Network/PacketReader.cs src/Network/Listener.cs src/Network/SendQueue.cs src/Network/BufferPool.cs src/Network/PacketWriter.cs src/ClientVersion.cs src/Config.cs src/Timer.cs src/Insensitive.cs src/Network/PacketProfile.cs src/Attributes.cs src/Network/Compression.cs src/Network/PacketHandler.cs

SCRIPTS = legacy reports remote-admin myrunuo profiler
SCRIPTS_DLL = $(patsubst %,build/scripts/%.dll,$(SCRIPTS))

all: $(addprefix $(DISTDIR)/,SunUO.exe SunUO.exe.config SunLogin.exe SunLogin.exe.config UOGQuery.exe $(DISTDLL)) $(SCRIPTS_DLL)

clean:
	rm -f doc/sunuo.html
	rm -rf build

install: all
	install -m 0755 $(DISTDIR)/SunUO.exe $(RUNUO_BASE)/
	test -f $(DISTDIR)/SunUO.exe.mdb && install -m 0644 $(DISTDIR)/SunUO.exe.mdb $(RUNUO_BASE)/
	test -f $(DISTDIR)/SunUO.exe.config || install -m 0644 SunUO.exe.config $(RUNUO_BASE)/
	test -f $(DISTDIR)/SunLogin.exe.config || install -m 0644 SunLogin.exe.config $(RUNUO_BASE)/
	install -m 0644 $(addprefix $(DISTDIR)/,$(DISTDLL)) $(RUNUO_BASE)/
	install -m 0644 $(SCRIPTS_DLL) $(RUNUO_BASE)/local/lib/

# compile targets

$(DISTDIR)/SunUO.exe: $(SUNUO_SOURCES) lib/MySql.Data.dll lib/Npgsql.dll lib/log4net.dll
	mkdir -p $(DISTDIR)
	rm -f $@.mdb
	$(MCS) $(MCS_FLAGS) -out:$@ -r:System.Data.dll -r:MySql.Data -r:Npgsql.dll -r:log4net.dll $(SUNUO_SOURCES)

$(DISTDIR)/SunLogin.exe: $(SUNLOGIN_SOURCES) lib/MySql.Data.dll lib/log4net.dll
	mkdir -p $(DISTDIR)
	rm -f $@.mdb
	$(MCS) $(MCS_FLAGS) -out:$@ -r:System.Data.dll -r:MySql.Data -r:log4net.dll $(SUNLOGIN_SOURCES)

$(DISTDIR)/UOGQuery.exe: util/UOGQuery.cs
	mkdir -p $(DISTDIR)
	rm -f $@.mdb
	$(MCS) $(MCS_FLAGS) -out:$@ util/UOGQuery.cs

build/scripts/legacy.dll: LIBS = System.Drawing.dll System.Web.dll System.Data.dll log4net.dll
build/scripts/legacy.dll: $(DISTDIR)/SunUO.exe
	mkdir -p $(dir $@)
	$(MCS) $(MCS_FLAGS) -target:library -out:$@ -lib:$(DISTDIR) $(addprefix -r:,$(LIBS)) -r:SunUO.exe -recurse:'scripts/legacy/*.cs'

build/scripts/reports.dll: LIBS = System.Drawing.dll System.Web.dll System.Windows.Forms.dll log4net.dll
build/scripts/reports.dll: $(DISTDIR)/SunUO.exe build/scripts/legacy.dll
	mkdir -p $(dir $@)
	$(MCS) $(MCS_FLAGS) -target:library -out:$@ -lib:$(DISTDIR) $(addprefix -r:,$(LIBS)) -r:SunUO.exe -lib:build/scripts -r:legacy.dll -recurse:'scripts/reports/*.cs'

build/scripts/remote-admin.dll: LIBS = log4net.dll
build/scripts/remote-admin.dll: $(DISTDIR)/SunUO.exe build/scripts/legacy.dll
	mkdir -p $(dir $@)
	$(MCS) $(MCS_FLAGS) -target:library -out:$@ -lib:$(DISTDIR) $(addprefix -r:,$(LIBS)) -r:SunUO.exe -lib:build/scripts -r:legacy.dll -recurse:'scripts/remote-admin/*.cs'

build/scripts/myrunuo.dll: LIBS = System.Data.dll log4net.dll
build/scripts/myrunuo.dll: $(DISTDIR)/SunUO.exe build/scripts/legacy.dll
	mkdir -p $(dir $@)
	$(MCS) $(MCS_FLAGS) -target:library -out:$@ -lib:$(DISTDIR) $(addprefix -r:,$(LIBS)) -r:SunUO.exe -lib:build/scripts -r:legacy.dll -recurse:'scripts/myrunuo/*.cs'

build/scripts/profiler.dll: $(DISTDIR)/SunUO.exe build/scripts/legacy.dll
	mkdir -p $(dir $@)
	$(MCS) $(MCS_FLAGS) -target:library -out:$@ -lib:$(DISTDIR) -r:SunUO.exe -lib:build/scripts -r:legacy.dll -recurse:'scripts/profiler/*.cs'

$(addprefix $(DISTDIR)/,$(DISTDLL)): $(DISTDIR)/%: lib/%
	cp $< $@

# dist targets

.PHONY: dist
dist: build/dist/sunuo-$(VERSION)-bin.zip build/dist/sunuo-$(VERSION).zip

$(addprefix $(DISTDIR)/,COPYING AUTHORS README): $(DISTDIR)/%: %
	cp $< $@

$(DISTDIR)/SunUO.exe.config: conf/SunUO.exe.config
	cp $< $@

$(DISTDIR)/SunLogin.exe.config: conf/SunLogin.exe.config
	cp $< $@

$(DISTDIR)/changelog: debian/changelog
	cp $< $@

.PHONY: export-scripts
export-scripts:
	rm -rf $(DISTDIR)/Scripts $(DISTDIR)/local/src/profiler
	mkdir -p $(DISTDIR)/local/src
	svn export scripts/legacy $(DISTDIR)/Scripts 
	svn export scripts/profiler $(DISTDIR)/local/src/profiler

build/dist/sunuo-$(VERSION)-bin.zip: $(addprefix $(DISTDIR)/,SunUO.exe SunUO.exe.config SunLogin.exe SunLogin.exe.config UOGQuery.exe sunuo.html COPYING AUTHORS README changelog $(DISTDLL)) export-scripts
	mkdir -p $(dir $@)
	cd build && fakeroot zip -q -r $(shell pwd)/$@ sunuo-$(VERSION)-bin

.PHONY: svn-export
svn-export:
	rm -rf build/tmp
	mkdir -p build/tmp
	svn export . build/tmp/sunuo-$(VERSION)

build/dist/sunuo-$(VERSION).zip: svn-export
	mkdir -p build/tmp/sunuo-$(VERSION)/lib
	cp $(addprefix lib/,$(DISTDLL)) build/tmp/sunuo-$(VERSION)/lib/
	mkdir -p $(dir $@)
	cd build/tmp && fakeroot zip -q -r $(shell pwd)/$@ sunuo-$(VERSION)

# auto-download targets

download/mysql-connector-net-1.0.7-noinstall.zip:
	mkdir -p download
	wget http://sunsite.informatik.rwth-aachen.de/mysql/Downloads/Connector-Net/mysql-connector-net-1.0.7-noinstall.zip -O $@.tmp
	mv $@.tmp $@

lib/MySql.Data.dll: download/mysql-connector-net-1.0.7-noinstall.zip
	rm -rf build/tmp && mkdir -p build/tmp
	unzip -q -d build/tmp $<
	mkdir -p lib
	cp build/tmp/bin/mono-1.0/release/MySql.Data.dll lib/
	rm -rf build/tmp

download/Npgsql1.0beta1-bin.tar.bz2:
	mkdir -p $(dir $@)
	wget http://pgfoundry.org/frs/download.php/531/Npgsql1.0beta1-bin.tar.bz2 -O $@.tmp
	mv $@.tmp $@

lib/Npgsql.dll: download/Npgsql1.0beta1-bin.tar.bz2
	rm -rf build/tmp && mkdir -p build/tmp
	tar xjfC $< build/tmp
	mkdir -p lib
	cp build/tmp/Npgsql/bin/mono/Npgsql.dll lib/
	rm -rf build/tmp

download/incubating-log4net-1.2.10.zip:
	mkdir -p $(dir $@)
	wget http://cvs.apache.org/dist/incubator/log4net/1.2.10/incubating-log4net-1.2.10.zip -O $@.tmp
	mv $@.tmp $@

lib/log4net.dll: download/incubating-log4net-1.2.10.zip
	rm -rf build/tmp && mkdir -p build/tmp
	unzip -q -d build/tmp $<
	mkdir -p lib
	cp build/tmp/log4net-1.2.10/bin/mono/1.0/release/log4net.dll lib/
	rm -rf build/tmp

# documentation targets

.PHONY: docs
docs: doc/sunuo.html
doc/sunuo.html: doc/sunuo.xml
	xsltproc -o $@ /usr/share/sgml/docbook/stylesheet/xsl/nwalsh/xhtml/docbook.xsl $<

$(DISTDIR)/sunuo.html: doc/sunuo.html
	cp $< $@

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
