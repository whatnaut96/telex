{ pkgs ? import <nixpkgs> {} }:

pkgs.mkShell {
  packages = [
    pkgs.mono
    pkgs.dotnet-sdk_8
  ];
}
