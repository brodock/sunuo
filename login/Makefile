TOPDIR = ..
include $(TOPDIR)/config.mk

SOURCES := ../src/AssemblyInfo.cs $(shell find . -name "*.cs" ) $(shell find ../src/Network/Encryption -name "*.cs" )
SOURCES += ../src/Network/MessagePump.cs ../src/Network/ByteQueue.cs ../src/Network/PacketReader.cs ../src/Network/Listener.cs ../src/Network/SendQueue.cs ../src/Network/BufferPool.cs ../src/Network/PacketWriter.cs ../src/ClientVersion.cs ../src/Config.cs ../src/Timer.cs ../src/Insensitive.cs ../src/Network/PacketProfile.cs ../src/Attributes.cs ../src/Network/Compression.cs ../src/Network/PacketHandler.cs

all: SunLogin.exe

clean:
	rm -f SunLogin.exe

SunLogin.exe: $(SOURCES)
	$(MCS) $(MCS_FLAGS) -out:$@ -r:System.Data.dll -r:MySql.Data $(SOURCES)

install: all
	install -m 0755 SunLogin.exe $(RUNUO_BASE)/
	-test -f SunLogin.exe.mdb && install -m 0644 SunLogin.exe.mdb $(RUNUO_BASE)/
