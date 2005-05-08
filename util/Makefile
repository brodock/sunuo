TOPDIR = ..
include $(TOPDIR)/config.mk

all: UOGQuery.exe

clean:
	rm -f UOGQuery.exe

install: all
	install -m 0755 UOGQuery.exe $(RUNUO_BASE)/

UOGQuery.exe: UOGQuery.cs
	$(MCS) $(MCS_FLAGS) -out:$@ $^
