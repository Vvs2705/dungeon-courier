#!/bin/bash
# elenco_hd.sh nome [nome...] : elenco maior e nitido (2026-10-15). Re-render pelo render_sprites.py com --hd K (mais px por metro)
# e --escala 1.25 (a View mostra o ator 25% maior; SpriteSheet.Escala). K = min(1,5; 4096 / (quadros x celula)): a folha nao passa
# de 4096 px (teto seguro no celular). Os chefes ficam como estao (ja grandes). Comandos de origem: docs/PROVENIENCIA.md sec. 5.
# O courier sai do .blend com a aba do chapeu menor: roupas_courier.py -- <courier.fbx> <arte>/roupas classico,mineiro 0.78
# Um render por vez (PC de 7,7 GB). Log de cada folha em $TEMP/dc_hd_<nome>.log; imprime OK ou FALHOU.
set -u
B="${BLENDER:-/c/Program Files/Blender Foundation/Blender 5.2/blender.exe}"
T="$(cd "$(dirname "$0")" && pwd)"
C="$(dirname "$T")"
A="${ARTE:-$C/../../../../arte}"   # worktree -> arte/ do checkout principal (fora do git)
[ -d "$A/tripo" ] || A="$C/../arte"
S="$C/Assets/_DC/Resources/Sprites"
ESC=1.25
HOLD="$A/mixamo/Hold.fbx"
UNZ="${TEMP:-/tmp}/dc_tripo"   # modelo so zipado: extrai aqui (regeneravel, apagado no fim)
fbx() {  # caminho Windows (cygpath -m): o --attach junta caminho:osso:... e o MSYS nao converte o caminho dentro dele
    local f; f=$(ls "$A/tripo/$1/"*.fbx 2>/dev/null | head -1)
    if [ -z "$f" ]; then mkdir -p "$UNZ/$1"; unzip -qo "$A/tripo/$1/$1.zip" -d "$UNZ/$1"; f=$(ls "$UNZ/$1/"*.fbx | head -1); fi
    cygpath -m "$f"
}

render() {  # nome hd modelo [argumentos do render_sprites.py...]
    local n=$1 hd=$2 m=$3
    shift 3
    local log="${TEMP:-/tmp}/dc_hd_$n.log"
    timeout 3600 "$B" -b --factory-startup --python-exit-code 1 --python "$T/render_sprites.py" -- --model "$m" --out "$S/$n" \
        --contact "$C/Builds/sprites_contact/$n.png" --hd "$hd" --escala "$ESC" "$@" > "$log" 2>&1
    if grep -q SPRITES_OK "$log"; then echo "OK $n $(grep -o 'ppu=[0-9.]*' "$log" | tail -1)"; else echo "FALHOU $n ($log)"; tail -3 "$log"; fi
}

job() {
    case $1 in
        courier) render courier 1.5 "$A/roupas/courier_classico.blend" --preset courier --anims "$A/mixamo/courier" --hold "$HOLD" ;;
        courier_mineiro) render courier_mineiro 1.5 "$A/roupas/courier_mineiro.blend" --preset courier --anims "$A/mixamo/courier" --hold "$HOLD" ;;
        courier_fim) render courier_fim 1.06 "$A/roupas/courier_classico.blend" --preset courier_fim --anims "$A/mixamo/courier_fim" ;;
        ladrao) render ladrao 1.5 "$(fbx ladrao)" --preset ladrao --anims "$A/mixamo/ladrao" --hold "$HOLD" ;;
        guarda) render guarda 1.5 "$(fbx guarda)" --preset guarda --anims "$A/mixamo/guarda" --attach "$(fbx escudo):LeftForeArm:0.09,0,0.06:90,180,0:0.75" ;;
        cliente) render cliente 1.5 "$(fbx cliente)" --preset cliente --anims "$A/mixamo/cliente" ;;
        arqueiro) render arqueiro 1.5 "$(fbx arqueiro)" --preset arqueiro --anims "$A/mixamo/arqueiro" --attach "$(fbx arcoflecha):LeftHand:-0.06,0,-0.015:90,180,0:0.8" ;;
        capitao) render capitao 1.5 "$(fbx capitao)" --preset capitao --anims "$A/mixamo/capitao" --hold "$HOLD" ;;
        goblin_mineiro) render goblin_mineiro 1.5 "$(fbx goblin_mineiro)" --preset goblin_mineiro --anims "$A/mixamo/goblin_mineiro" --attach "$(fbx picareta):RightHand:-0.04,-0.229,-0.02:31.6,-67.3,98.4:0.45" ;;
        toupeira) render toupeira 1.5 "$(fbx toupeira)" --preset toupeira --anims "$A/mixamo/toupeira" ;;
        esqueleto) render esqueleto 1.33 "$(fbx esqueleto)" --preset esqueleto --anims "$A/mixamo/esqueleto" --attach "$(fbx clava):RightHand:-0.04,-0.25,-0.02:31.9,44.1,-78.0:0.49" ;;
        mumia) render mumia 1.33 "$(fbx mumia)" --preset mumia --anims "$A/mixamo/mumia" ;;
        diabrete) render diabrete 1.5 "$(fbx diabrete)" --preset diabrete --anims "$A/mixamo/diabrete" ;;
        kobold) render kobold 1.5 "$(fbx kobold)" --preset kobold --anims "$A/mixamo/kobold" --attach "$(fbx frasco):RightHand:-0.05,0,-0.03:90,0,0:0.30" ;;
        brasa) render brasa 1.5 "$(fbx magma)" --preset brasa --anims "$A/mixamo/brasa" ;;
        aranha_empinada) render aranha_empinada 1.5 "$(fbx aranha)" --preset aranha_empinada ;;
        morcego|limo|sapo|besouro|fantasma|aranha|salamandra|filhote) render "$1" 1.5 "$(fbx "$1")" --preset "$1" ;;
        *) echo "FALHOU $1 (folha fora da lista)" ;;
    esac
}

for n in "$@"; do job "$n"; done
rm -rf "$UNZ"
