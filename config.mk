MCS := $(word 1,$(shell which mcs) mcs)
MCS_FLAGS = -unsafe -define:MONO -debug -lib:build/lib

RUNUO_BASE = $(HOME)/dl/runuo

