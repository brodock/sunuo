include config.mk

VERSION := $(shell perl -ne 'print "$$1\n" if /^sunuo \((.*?)\)/' debian/changelog |head -1)
DISTDIR = build/sunuo-$(VERSION)-bin

MCS_FLAGS += -unsafe -define:MONO -debug -lib:build/lib

SUNUO_SOURCES := $(shell find src -name "*.cs" )
SUNLOGIN_SOURCES := src/AssemblyInfo.cs $(shell find login -name "*.cs" ) $(shell find src/Network/Encryption -name "*.cs" )
SUNLOGIN_SOURCES += src/Network/MessagePump.cs src/Network/ByteQueue.cs src/Network/PacketReader.cs src/Network/Listener.cs src/Network/SendQueue.cs src/Network/BufferPool.cs src/Network/PacketWriter.cs src/ClientVersion.cs src/Config.cs src/Timer.cs src/Insensitive.cs src/Network/PacketProfile.cs src/Attributes.cs src/Network/Compression.cs src/Network/PacketHandler.cs

all: $(addprefix $(DISTDIR)/,SunUO.exe SunLogin.exe UOGQuery.exe)

clean:
	rm -f doc/sunuo.html
	rm -rf build

install: all
	install -m 0755 src/SunUO.exe $(RUNUO_BASE)/
	-test -f src/SunUO.exe.mdb && install -m 0644 src/SunUO.exe.mdb $(RUNUO_BASE)/

# compile targets

$(DISTDIR)/SunUO.exe: $(SUNUO_SOURCES)
	mkdir -p $(DISTDIR)
	rm -f $@.mdb
	$(MCS) $(MCS_FLAGS) -out:$@ $(SUNUO_SOURCES)

$(DISTDIR)/SunLogin.exe: $(SUNLOGIN_SOURCES) build/lib/MySql.Data.dll
	mkdir -p $(DISTDIR)
	rm -f $@.mdb
	$(MCS) $(MCS_FLAGS) -out:$@ -r:System.Data.dll -r:MySql.Data $(SUNLOGIN_SOURCES)

$(DISTDIR)/UOGQuery.exe: util/UOGQuery.cs
	mkdir -p $(DISTDIR)
	rm -f $@.mdb
	$(MCS) $(MCS_FLAGS) -out:$@ util/UOGQuery.cs

$(DISTDIR)/MySql.Data.dll: build/lib/MySql.Data.dll
	cp $< $@

# dist targets

.PHONY: dist
dist: build/dist/sunuo-$(VERSION)-bin.zip build/dist/sunuo-$(VERSION).zip

$(addprefix $(DISTDIR)/,COPYING AUTHORS README): $(DISTDIR)/%: %
	cp $< $@

$(DISTDIR)/changelog: debian/changelog
	cp $< $@

build/dist/sunuo-$(VERSION)-bin.zip: $(addprefix $(DISTDIR)/,SunUO.exe SunLogin.exe UOGQuery.exe sunuo.html COPYING AUTHORS README changelog)
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
	unzip -q -d build/tmp download/mysql-connector-net-1.0.7-noinstall.zip
	mkdir -p build/lib
	cp build/tmp/bin/mono-1.0/release/MySql.Data.dll build/lib/
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
