TOPDIR = ..
include $(TOPDIR)/config.mk

SOURCES := $(shell find . -name "*.cs" )

all: SunUO.exe

clean:
	rm -f SunUO.exe

install: all
	install -m 0755 SunUO.exe $(RUNUO_BASE)/
	-test -f SunUO.exe.mdb && install -m 0644 SunUO.exe.mdb $(RUNUO_BASE)/

SunUO.exe: $(SOURCES)
	$(MCS) $(MCS_FLAGS) -out:$@ $(SOURCES)
