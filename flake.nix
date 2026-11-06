{
  description = "GuildSaber development environment";

  inputs.nixpkgs.url = "github:NixOS/nixpkgs/nixos-unstable";

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
