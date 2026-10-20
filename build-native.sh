#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "$0")" && pwd)"
VENDOR="$ROOT/vendor/harbour"
NATIVE="$ROOT/native"
COMMON="$VENDOR/src/common"

if [[ "${1:-}" == "win-x64" ]]; then
  CC="${CC:-x86_64-w64-mingw32-gcc}"
  OUT="$ROOT/runtimes/win-x64/native/HarbourPreprocessor.dll"
  PLATFORM_FLAGS=()
  LINK_FLAGS=(-shared -static-libgcc -Wl,--no-undefined -luser32 -ladvapi32 -lshell32 -lws2_32)
elif [[ "${1:-}" == "linux-x64" || -z "${1:-}" ]]; then
  CC="${CC:-gcc}"
  OUT="$ROOT/runtimes/linux-x64/native/libHarbourPreprocessor.so"
  PLATFORM_FLAGS=()
  LINK_FLAGS=(-shared -Wl,--no-undefined -lm -pthread)
else
  echo "Uso: $0 [linux-x64|win-x64]" >&2
  exit 2
fi

mkdir -p "$(dirname "$OUT")"
"$CC" -std=gnu11 -O2 -fPIC -fvisibility=hidden \
  "${PLATFORM_FLAGS[@]}" \
  -I"$VENDOR/include" -I"$NATIVE" \
  "$NATIVE/harbour_preprocessor.c" \
  "$VENDOR/src/pp/ppcore.c" \
  "$VENDOR/src/nortl/nortl.c" \
  $(find "$COMMON" -maxdepth 1 -name '*.c' ! -name hbverdsp.c -print) \
  "${LINK_FLAGS[@]}" \
  -o "$OUT"
echo "Gerada: $OUT"
