MCS := $(word 1,$(shell which mcs) mcs)
MCS_FLAGS = -unsafe -define:MONO -debug

RUNUO_BASE = $(HOME)/dl/runuo

