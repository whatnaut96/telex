GAME_DIR ?= /home/khill/.local/share/Steam/steamapps/common/Cities_Skylines
MANAGED_DIR := $(GAME_DIR)/Cities_Data/Managed
MONO_CSC ?= mcs
MOD_NAME := Telex
BUILD_DIR := build
SRC := $(shell find src/Telex -name '*.cs' | sort)
OUT := $(BUILD_DIR)/$(MOD_NAME).dll
INSTALL_DIR ?= $(HOME)/.local/share/Colossal Order/Cities_Skylines/Addons/Mods/$(MOD_NAME)
INSTALLED_DLL := $(INSTALL_DIR)/$(MOD_NAME).dll
MONODIS ?= monodis

REFS := \
	-r:$(MANAGED_DIR)/mscorlib.dll \
	-r:$(MANAGED_DIR)/System.dll \
	-r:$(MANAGED_DIR)/System.Core.dll \
	-r:$(MANAGED_DIR)/ICities.dll \
	-r:$(MANAGED_DIR)/UnityEngine.dll \
	-r:$(MANAGED_DIR)/Assembly-CSharp.dll \
	-r:$(MANAGED_DIR)/ColossalManaged.dll

.PHONY: all clean install status verify-install

all: $(OUT)

$(OUT): $(SRC)
	mkdir -p $(BUILD_DIR)
	$(MONO_CSC) -nostdlib -target:library -optimize+ -debug- -out:$(OUT) $(REFS) $(SRC)

install: $(OUT)
	mkdir -p "$(INSTALL_DIR)"
	cp "$(OUT)" "$(INSTALLED_DLL)"
	@$(MAKE) --no-print-directory verify-install
	@echo "Installed $(MOD_NAME) to $(INSTALLED_DLL)"
	@echo "Restart Cities: Skylines if it is already running; CS1 keeps loaded mod assemblies in memory."

status: $(OUT)
	@echo "Build:     $$(stat -c '%y %s bytes' "$(OUT)")"
	@if [ -f "$(INSTALLED_DLL)" ]; then \
		echo "Installed: $$(stat -c '%y %s bytes' "$(INSTALLED_DLL)")"; \
		if cmp -s "$(OUT)" "$(INSTALLED_DLL)"; then \
			echo "Status:    installed DLL matches build"; \
		else \
			echo "Status:    installed DLL differs from build; run 'make install'"; \
		fi; \
	else \
		echo "Installed: missing"; \
		echo "Status:    run 'make install'"; \
	fi

verify-install:
	@test -f "$(INSTALLED_DLL)" || (echo "Installed DLL missing: $(INSTALLED_DLL)" && exit 1)
	@cmp -s "$(OUT)" "$(INSTALLED_DLL)" || (echo "Installed DLL differs from build output" && exit 1)
	@if command -v "$(MONODIS)" >/dev/null 2>&1; then \
		$(MONODIS) --userstrings "$(INSTALLED_DLL)" | grep -q '"roads"' || (echo "Installed DLL is missing roads telemetry marker" && exit 1); \
		$(MONODIS) --userstrings "$(INSTALLED_DLL)" | grep -q '"industry_areas"' || (echo "Installed DLL is missing industry_areas telemetry marker" && exit 1); \
	fi

clean:
	rm -rf $(BUILD_DIR)
