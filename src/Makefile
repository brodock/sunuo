TOPDIR = ..
include $(TOPDIR)/config.mk

SOURCES := $(shell find . -name "*.cs" )

all: Server.exe

clean:
	rm -f Server.exe

install: all
	install -m 0755 Server.exe $(RUNUO_BASE)/

Server.exe: $(SOURCES)
	$(MCS) $(MCS_FLAGS) -o $@ $(SOURCES)
