GAME_DIR ?= /home/khill/.local/share/Steam/steamapps/common/Cities_Skylines
MANAGED_DIR := $(GAME_DIR)/Cities_Data/Managed
MONO_CSC ?= mcs
MOD_NAME := Telex
BUILD_DIR := build
SRC := $(shell find src/Telex -name '*.cs' | sort)
OUT := $(BUILD_DIR)/$(MOD_NAME).dll
INSTALL_DIR ?= $(HOME)/.local/share/Colossal Order/Cities_Skylines/Addons/Mods/$(MOD_NAME)

REFS := \
	-r:$(MANAGED_DIR)/mscorlib.dll \
	-r:$(MANAGED_DIR)/System.dll \
	-r:$(MANAGED_DIR)/System.Core.dll \
	-r:$(MANAGED_DIR)/ICities.dll \
	-r:$(MANAGED_DIR)/UnityEngine.dll \
	-r:$(MANAGED_DIR)/Assembly-CSharp.dll \
	-r:$(MANAGED_DIR)/ColossalManaged.dll

.PHONY: all clean install

all: $(OUT)

$(OUT): $(SRC)
	mkdir -p $(BUILD_DIR)
	$(MONO_CSC) -nostdlib -target:library -optimize+ -debug- -out:$(OUT) $(REFS) $(SRC)

install: $(OUT)
	mkdir -p "$(INSTALL_DIR)"
	cp "$(OUT)" "$(INSTALL_DIR)/$(MOD_NAME).dll"

clean:
	rm -rf $(BUILD_DIR)
