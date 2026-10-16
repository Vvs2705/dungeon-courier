#!/bin/bash
# loja_fotos.sh [pt en es id] : as fotos das 8 capturas da Play (docs/LOJA.md sec. 5) no build Windows de release, por idioma.
# As 6 de partida saem de uma gravacao -record a 10 quadros/s (o passo do bot e fixo: o quadro N e o mesmo nos 4 idiomas) e as 2
# telas de -shot. Grava em client/Builds/loja_fotos/<foto>_<lang>.png; depois: python -I client/tools/loja_assets.py
set -u
C="$(cd "$(dirname "$0")/.." && pwd)"
EXE="$C/Builds/win-release/DungeonCourier.exe"
F="$C/Builds/loja_fotos"
REC="${TEMP:-/tmp}/dc_loja_rec"
mkdir -p "$F"
w() { cygpath -m "$1" 2>/dev/null || echo "$1"; }
base() { timeout 180 "$EXE" -screen-width 540 -screen-height 960 -screen-fullscreen 0 -testsession -lang "$1" "${@:2}" > /dev/null 2>&1; }

# foto | flags | recordfrom | recordsecs | quadro   (tabela do LOJA.md sec. 5)
CENAS="c12_chamando|-contract 12 -bot|44|6|35
c1_ambiente|-contract 1 -bot|0|3|20
c1_fim_24|-contract 1 -bot|21|5|28
c8_rachaduras|-contract 8 -bot|6|3|10
c27_gelo|-contract 27 -bot|0|3|25
b4_dragao|-contract 72 -room B4 -bot -weapon 1 -guilda|0|9|80"

LANGS=("$@"); [ ${#LANGS[@]} -eq 0 ] && LANGS=(pt en es id)
for L in "${LANGS[@]}"; do
    for t in quadro guilda; do
        base "$L" -dev-desbloqueia -tela "$t" -shot "$(w "$F")/tela_${t}_$L.png" -shotdelay 1.5
    done
    while IFS='|' read -r foto flags de secs q; do
        d="$REC/${foto}_$L"; rm -rf "$d"
        # shellcheck disable=SC2086
        base "$L" $flags -record "$(w "$d")" -recordfps 10 -recordsecs "$secs" -recordfrom "$de"
        src=$(printf "%s/%05d.png" "$d" "$q")
        if [ -f "$src" ]; then cp "$src" "$F/${foto}_$L.png"; echo "OK ${foto}_$L"; else echo "FALHOU ${foto}_$L (sem o quadro $q em $d)"; fi
        rm -rf "$d"
    done <<< "$CENAS"
    for t in quadro guilda; do [ -f "$F/tela_${t}_$L.png" ] && echo "OK tela_${t}_$L" || echo "FALHOU tela_${t}_$L"; done
done
