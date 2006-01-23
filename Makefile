include config.mk

VERSION := $(shell perl -ne 'print "$$1\n" if /^sunuo \((.*?)\)/' debian/changelog |head -1)
DISTDIR = build/sunuo-$(VERSION)-bin
DISTDLL = MySql.Data.dll Npgsql.dll log4net.dll

MCS_FLAGS += -define:MONO -debug -lib:build/lib

SUNUO_SOURCES := $(shell find src -name "*.cs" )
SUNLOGIN_SOURCES := src/AssemblyInfo.cs $(shell find login -name "*.cs" ) $(shell find src/Network/Encryption -name "*.cs" )
SUNLOGIN_SOURCES += src/Network/MessagePump.cs src/Network/ByteQueue.cs src/Network/PacketReader.cs src/Network/Listener.cs src/Network/SendQueue.cs src/Network/BufferPool.cs src/Network/PacketWriter.cs src/ClientVersion.cs src/Config.cs src/Timer.cs src/Insensitive.cs src/Network/PacketProfile.cs src/Attributes.cs src/Network/Compression.cs src/Network/PacketHandler.cs

all: $(addprefix $(DISTDIR)/,SunUO.exe SunUO.exe.config SunLogin.exe SunLogin.exe.config UOGQuery.exe $(DISTDLL))

clean:
	rm -f doc/sunuo.html
	rm -rf build

install: all
	install -m 0755 $(DISTDIR)/SunUO.exe $(RUNUO_BASE)/
	test -f $(DISTDIR)/SunUO.exe.mdb && install -m 0644 $(DISTDIR)/SunUO.exe.mdb $(RUNUO_BASE)/
	install -m 0644 $(addprefix $(DISTDIR)/,$(DISTDLL) SunUO.exe.config SunLogin.exe.config) $(RUNUO_BASE)/

# compile targets

$(DISTDIR)/SunUO.exe: $(SUNUO_SOURCES) build/lib/MySql.Data.dll build/lib/Npgsql.dll build/lib/log4net.dll
	mkdir -p $(DISTDIR)
	rm -f $@.mdb
	$(MCS) $(MCS_FLAGS) -unsafe -out:$@ -r:System.Data.dll -r:MySql.Data -r:Npgsql.dll -r:log4net.dll $(SUNUO_SOURCES)

$(DISTDIR)/SunLogin.exe: $(SUNLOGIN_SOURCES) build/lib/MySql.Data.dll build/lib/log4net.dll
	mkdir -p $(DISTDIR)
	rm -f $@.mdb
	$(MCS) $(MCS_FLAGS) -unsafe -out:$@ -r:System.Data.dll -r:MySql.Data -r:log4net.dll $(SUNLOGIN_SOURCES)

$(DISTDIR)/UOGQuery.exe: util/UOGQuery.cs
	mkdir -p $(DISTDIR)
	rm -f $@.mdb
	$(MCS) $(MCS_FLAGS) -out:$@ util/UOGQuery.cs

$(addprefix $(DISTDIR)/,$(DISTDLL)): $(DISTDIR)/%: build/lib/%
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

build/dist/sunuo-$(VERSION)-bin.zip: $(addprefix $(DISTDIR)/,SunUO.exe SunUO.exe.config SunLogin.exe SunLogin.exe.config UOGQuery.exe sunuo.html COPYING AUTHORS README changelog $(DISTDLL))
	mkdir -p $(dir $@)
	cd build && fakeroot zip -q -r $(shell pwd)/$@ sunuo-$(VERSION)-bin

.PHONY: svn-export
svn-export:
	rm -rf build/tmp
	mkdir -p build/tmp
	svn export . build/tmp/sunuo-$(VERSION)

build/dist/sunuo-$(VERSION).zip: svn-export
	mkdir -p $(dir $@)
	cd build/tmp && fakeroot zip -q -r $(shell pwd)/$@ sunuo-$(VERSION)

# auto-download targets

download/mysql-connector-net-1.0.7-noinstall.zip:
	mkdir -p download
	wget http://sunsite.informatik.rwth-aachen.de/mysql/Downloads/Connector-Net/mysql-connector-net-1.0.7-noinstall.zip -O $@.tmp
	mv $@.tmp $@

build/lib/MySql.Data.dll: download/mysql-connector-net-1.0.7-noinstall.zip
	rm -rf build/tmp && mkdir -p build/tmp
	unzip -q -d build/tmp $<
	mkdir -p build/lib
	cp build/tmp/bin/mono-1.0/release/MySql.Data.dll build/lib/
	rm -rf build/tmp

download/Npgsql1.0beta1-bin.tar.bz2:
	mkdir -p $(dir $@)
	wget http://pgfoundry.org/frs/download.php/531/Npgsql1.0beta1-bin.tar.bz2 -O $@.tmp
	mv $@.tmp $@

build/lib/Npgsql.dll: download/Npgsql1.0beta1-bin.tar.bz2
	rm -rf build/tmp && mkdir -p build/tmp
	tar xjfC $< build/tmp
	cp build/tmp/Npgsql/bin/mono/Npgsql.dll build/lib/
	rm -rf build/tmp

download/incubating-log4net-1.2.9-beta.zip:
	mkdir -p $(dir $@)
	wget http://cvs.apache.org/dist/incubator/log4net/1.2.9/incubating-log4net-1.2.9-beta.zip -O $@.tmp
	mv $@.tmp $@

build/lib/log4net.dll: download/incubating-log4net-1.2.9-beta.zip
	rm -rf build/tmp && mkdir -p build/tmp
	unzip -q -d build/tmp $<
	cp build/tmp/bin/mono/1.0/release/log4net.dll build/lib/
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
