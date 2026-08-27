{
  description = "Telex Cities: Skylines 1 telemetry mod development environment";

  inputs = {
    nixpkgs.url = "github:NixOS/nixpkgs/nixos-unstable";
  };

  outputs = { self, nixpkgs }:
    let
      systems = [
        "x86_64-linux"
        "aarch64-linux"
      ];

      forAllSystems = fn:
        nixpkgs.lib.genAttrs systems (system:
          fn {
            pkgs = import nixpkgs { inherit system; };
          });
    in
    {
      devShells = forAllSystems ({ pkgs }: {
        default = pkgs.mkShell {
          packages = [
            pkgs.dotnet-sdk_8
            pkgs.go
            pkgs.gnumake
            pkgs.jq
            pkgs.mono
            pkgs.ripgrep
          ];

          shellHook = ''
            export GAME_DIR="''${GAME_DIR:-$HOME/.local/share/Steam/steamapps/common/Cities_Skylines}"
            export MONO_CSC="''${MONO_CSC:-mcs}"
            echo "Telex dev shell"
            echo "  make all        # build build/Telex.dll"
            echo "  make install    # install into CS1 Addons/Mods/Telex"
            echo "  go run ./cmd/synco"
            echo "GAME_DIR=$GAME_DIR"
          '';
        };
      });

      apps = forAllSystems ({ pkgs }: {
        build = {
          type = "app";
          program = "${pkgs.writeShellApplication {
            name = "telex-build";
            runtimeInputs = [
              pkgs.gnumake
              pkgs.mono
            ];
            text = ''
              exec make all "$@"
            '';
          }}/bin/telex-build";
        };

        install = {
          type = "app";
          program = "${pkgs.writeShellApplication {
            name = "telex-install";
            runtimeInputs = [
              pkgs.gnumake
              pkgs.mono
            ];
            text = ''
              exec make install "$@"
            '';
          }}/bin/telex-install";
        };

        status = {
          type = "app";
          program = "${pkgs.writeShellApplication {
            name = "telex-status";
            runtimeInputs = [
              pkgs.gnumake
              pkgs.mono
            ];
            text = ''
              exec make status "$@"
            '';
          }}/bin/telex-status";
        };

        synco = {
          type = "app";
          program = "${pkgs.writeShellApplication {
            name = "synco";
            runtimeInputs = [
              pkgs.go
            ];
            text = ''
              exec go run ./cmd/synco "$@"
            '';
          }}/bin/synco";
        };
      });

      formatter = forAllSystems ({ pkgs }: pkgs.nixpkgs-fmt);
    };
}
