{
  description = "GuildSaber development environment";

  # Temporary SDK update from https://github.com/NixOS/nixpkgs/pull/574338.
  # Switch to github:NixOS/nixpkgs/nixos-unstable once it includes .NET 11 RC2.
  inputs.nixpkgs.url = "github:NixOS/nixpkgs/659c15701f113dea6828bf1953d2b3dc44e34550";

  outputs = { nixpkgs, ... }:
    let
      systems = [ "x86_64-linux" "aarch64-linux" ];
      forEachSystem = nixpkgs.lib.genAttrs systems;
      packagesFor = system:
        let pkgs = import nixpkgs { inherit system; };
        in {
          inherit pkgs;
          dotnet = pkgs.dotnetCorePackages.combinePackages [
            pkgs.dotnetCorePackages.sdk_11_0-bin
            pkgs.dotnetCorePackages.sdk_10_0-bin
          ];
        };
    in {
      packages = forEachSystem (system: {
        inherit (packagesFor system) dotnet;
      });

      devShells = forEachSystem (system:
        let
          inherit (packagesFor system) pkgs dotnet;
        in {
          default = pkgs.mkShell {
            packages = [
              dotnet
              pkgs.nodejs_24
              pkgs.pnpm
              pkgs.git
              pkgs.docker-client
            ];

            DOTNET_ROOT = "${dotnet}/share/dotnet";
            DOTNET_CLI_TELEMETRY_OPTOUT = "1";
            DOTNET_NOLOGO = "1";
            NODE_EXTRA_CA_CERTS = "${pkgs.cacert}/etc/ssl/certs/ca-bundle.crt";

            # Native dependencies used by the Discord player-card renderer.
            LD_LIBRARY_PATH = pkgs.lib.makeLibraryPath [
              pkgs.stdenv.cc.cc.lib
              pkgs.fontconfig
              pkgs.freetype
              pkgs.zlib
            ];
          };
        });
    };
}
