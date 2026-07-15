#!/usr/bin/env bash
set -euo pipefail

repo_root="$(git rev-parse --show-toplevel)"
cd "$repo_root"

target="${1:-HEAD}"

if [[ "$target" == *..* ]]; then
  GITLOGUE_RANGE="$target"
else
  GITLOGUE_RANGE="$(git rev-parse "$target")"
fi

export GITLOGUE_RANGE
echo "Rendering gitlogue replay for ${GITLOGUE_RANGE} to replay.mp4"

if command -v vhs >/dev/null 2>&1; then
  vhs replay.tape
else
  nix run nixpkgs#vhs -- replay.tape
fi
