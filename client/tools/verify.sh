#!/bin/bash
# Portão do Dungeon Courier num comando só (mesmo nome e uso do verify.sh do Forge Street). Git Bash no Windows.
# uso: client/tools/verify.sh quick | full
#   quick  núcleo (dotnet test) + viewcheck da View, sem abrir o Unity
#   full   quick + Unity batchmode: EditMode, Setup.BuildWindows e o .exe com -autoplay 30
# Imprime só OK ou o erro; os logs desta rodada ficam em client/Builds/verify/. As fotos 540×960 continuam sendo conferidas à mão.
# Regra do PC (7,7 GB): um processo pesado por vez; aborta se o Unity estiver aberto neste projeto ou se o emulador estiver ligado.
# Variável opcional: UNITY_EXE.
set -uo pipefail
export MSYS_NO_PATHCONV=1 DOTNET_CLI_UI_LANGUAGE=en   # caminhos do Windows passam intactos; saída do dotnet em inglês para o grep

ROOT="$(cd "$(dirname "$0")/../.." && pwd -W)"; CLIENT="$ROOT/client"; OUT="$CLIENT/Builds/verify"
UNITY="${UNITY_EXE:-C:/Program Files/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe}"
T0=$SECONDS

ok()   { echo "OK   $* [$((SECONDS - T0)) s]"; }
fail() { echo "FALHOU: $*" >&2; exit 1; }

quick() {
  local sa="$CLIENT/Library/ScriptAssemblies" d log
  # o viewcheck lê DLLs que só o Unity gera; sem elas seriam dezenas de erros confusos
  for d in UnityEngine.UI Unity.InputSystem; do
    [ -f "$sa/$d.dll" ] || fail "falta $sa/$d.dll. Abra o projeto no Unity 1 vez; num worktree novo, copie as DLLs de client/Library/ScriptAssemblies de onde o projeto já abriu"
  done
  mkdir -p "$OUT"
  log=$(dotnet test "$CLIENT/tools/coretests" -nologo 2>&1); local rc=$?
  echo "$log" > "$OUT/coretests.log"
  [ $rc -eq 0 ] || { echo "$log" | grep -E "Failed |error " | head -20; fail "dotnet test do núcleo (ver $OUT/coretests.log)"; }
  ok "núcleo: $(echo "$log" | grep -m1 -oE 'Failed: +[0-9]+, Passed: +[0-9]+, Skipped: +[0-9]+, Total: +[0-9]+' | tr -s ' ')"
  log=$(dotnet build "$CLIENT/tools/viewcheck/view" -nologo -v q 2>&1); rc=$?
  echo "$log" > "$OUT/viewcheck.log"
  [ $rc -eq 0 ] || { echo "$log" | grep -E " error " | head -20; fail "viewcheck (ver $OUT/viewcheck.log)"; }
  ok "viewcheck: $(echo "$log" | grep -oE '[0-9]+ (Warning|Error)\(s\)' | paste -sd, | tr -s ' ')"
}

# antes de abrir o Unity: nada de Editor neste projeto nem emulador ligado
guard() {
  [ -f "$UNITY" ] || fail "Unity não achado em $UNITY (defina UNITY_EXE)"
  local cl me
  cl=$(powershell.exe -NoProfile -Command "(Get-CimInstance Win32_Process -Filter \"Name='Unity.exe'\").CommandLine" 2>/dev/null | tr 'A-Z\\' 'a-z/')
  me=$(echo "$CLIENT" | tr 'A-Z' 'a-z')
  echo "$cl" | grep -qF "$me" && fail "o Unity está aberto neste projeto ($CLIENT): feche o Editor antes"
  [ -n "$(echo "$cl" | tr -d '[:space:]')" ] && echo "AVISO: há outro Unity.exe aberto (PC de 7,7 GB)"
  tasklist 2>/dev/null | grep -qiE '^(qemu-system|emulator)' && fail "o emulador está ligado: feche-o antes (nunca Unity e emulador juntos)"
  mkdir -p "$OUT"
}

# Unity em batchmode; o log é apagado antes, então o que estiver nele é desta rodada
run_unity() {
  local log="$OUT/$1"; shift
  rm -f "$log"
  "$UNITY" -batchmode -nographics -projectPath "$CLIENT" -logFile "$log" "$@"
}

# o Setup.Apply regrava settings: avisa se mexeu em arquivo versionado (git checkout -- se o gerador não mudou)
dirty() {
  local p=(client/ProjectSettings client/Assets client/Packages) s
  s=$(git -C "$ROOT" diff --name-only -- "${p[@]}" 2>/dev/null; git -C "$ROOT" ls-files --others --exclude-standard -- "${p[@]}")
  [ -z "$s" ] || printf 'AVISO: o Unity mexeu em arquivos versionados:\n%s\n' "$s"
}

full() {
  quick
  guard
  rm -f "$OUT/editmode.xml"
  run_unity editmode.log -runTests -testPlatform EditMode -testResults "$OUT/editmode.xml"   # veredito no XML, não no código de saída
  local run n; run=$(grep -m1 -o '<test-run [^>]*>' "$OUT/editmode.xml" 2>/dev/null) || fail "EditMode sem XML (ver $OUT/editmode.log)"
  n=$(echo "$run" | grep -oE '(total|passed|failed)="[0-9]+"' | paste -sd' ')
  [[ "$run" == *'result="Passed"'* ]] || fail "EditMode: $n (ver $OUT/editmode.xml)"
  ok "EditMode: $n"
  run_unity build_win.log -quit -executeMethod DC.EditorTools.Setup.BuildWindows; local rc=$? sum
  sum=$(grep -m1 -oE "BuildSummary\(StandaloneWindows64[^)]*\): .*" "$OUT/build_win.log")   # o Setup do DC põe "dev 1.4.1-beta" nos parênteses
  [ $rc -eq 0 ] && [[ "$sum" == *result=Succeeded* ]] || fail "BuildWindows (saída $rc, '${sum:-sem BuildSummary}', ver $OUT/build_win.log)"
  ok "BuildWindows: $sum"
  rm -f "$OUT/autoplay.log"
  "$CLIENT/Builds/win/DungeonCourier.exe" -batchmode -nographics -autoplay 30 -logFile "$OUT/autoplay.log" > /dev/null 2>&1 || fail "autoplay saiu $? (ver $OUT/autoplay.log)"
  local a; a=$(grep -m1 -o 'AUTOPLAY OK.*' "$OUT/autoplay.log") || fail "sem 'AUTOPLAY OK' (ver $OUT/autoplay.log)"
  grep -q "Exception" "$OUT/autoplay.log" && fail "exceção no autoplay (ver $OUT/autoplay.log)"
  ok "autoplay: $a"
  dirty
}

case "${1:-}" in
  quick) quick ;;
  full) full ;;
  *) sed -n '3,5p' "$0"; exit 2 ;;
esac
echo "verify $1: OK em $((SECONDS - T0)) s"
