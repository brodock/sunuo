include config.mk

SUNUO_SOURCES := $(shell find src -name "*.cs" )
SUNLOGIN_SOURCES := src/AssemblyInfo.cs $(shell find login -name "*.cs" ) $(shell find src/Network/Encryption -name "*.cs" )
SUNLOGIN_SOURCES += src/Network/MessagePump.cs src/Network/ByteQueue.cs src/Network/PacketReader.cs src/Network/Listener.cs src/Network/SendQueue.cs src/Network/BufferPool.cs src/Network/PacketWriter.cs src/ClientVersion.cs src/Config.cs src/Timer.cs src/Insensitive.cs src/Network/PacketProfile.cs src/Attributes.cs src/Network/Compression.cs src/Network/PacketHandler.cs

all: src/SunUO.exe login/SunLogin.exe util/UOGQuery.exe

clean:
	rm -f src/SunUO.exe login/SunLogin.exe util/UOGQuery.exe doc/sunuo.html

install: all
	install -m 0755 src/SunUO.exe $(RUNUO_BASE)/
	-test -f src/SunUO.exe.mdb && install -m 0644 src/SunUO.exe.mdb $(RUNUO_BASE)/

src/SunUO.exe: $(SUNUO_SOURCES)
	$(MCS) $(MCS_FLAGS) -out:$@ $^

login/SunLogin.exe: $(SUNLOGIN_SOURCES)
	$(MCS) $(MCS_FLAGS) -out:$@ -r:System.Data.dll -r:MySql.Data $^

util/UOGQuery.exe: util/UOGQuery.cs
	$(MCS) $(MCS_FLAGS) -out:$@ $^

.PHONY: docs
docs: doc/sunuo.html
doc/sunuo.html: doc/sunuo.xml
	xsltproc -o $@ /usr/share/sgml/docbook/stylesheet/xsl/nwalsh/xhtml/docbook.xsl $<

export:
	rm -rf /mnt/misc/sunuo /mnt/misc/runuo/SunUO.exe
	svn export . /mnt/misc/sunuo

release: VERSION := $(shell perl -ne 'print "$$1\n" if /^sunuo \((.*?)\)/' debian/changelog |head -1)
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
