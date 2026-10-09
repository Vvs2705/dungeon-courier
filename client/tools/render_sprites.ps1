<#
render_sprites.ps1 - Dungeon Courier: chama o Blender 5.2 headless com render_sprites.py (ASCII puro; PowerShell 5.1).

Uso:
  .\client\tools\render_sprites.ps1 -Nome courier -Model <fbx> -Anims <pasta com 1 FBX por clipe> [-Hold <fbx>]
  .\client\tools\render_sprites.ps1 -Nome caixa -Model <fbx>                      (prop: sem -Anims)
  .\client\tools\render_sprites.ps1 -Nome pilar -Preset none -Model <fbx> -Extra @("--width","1.2","--size","192","--scale","120","--dirs","1")
  .\client\tools\render_sprites.ps1 -Teste [-TestDir <pasta curta>]
      -Nome    = pasta de saida client\Assets\_DC\Resources\Sprites\<Nome>\ e, sem -Preset, o preset do .py
      -Preset  = outro preset do .py ("none" = sem preset; passe tudo em -Extra)
      -Extra   = argumentos crus para o .py (vencem o preset), ex.: @("--fps","10")
  Conferencia: client\Builds\sprites_contact\<Nome>.png (fora de Resources, que vai inteiro para o APK).
  -Teste: gera o humanoide-placeholder do COE e renderiza Idle, Run e CarryRun (braco do Attack1 via -Hold) com o
  preset courier em <TestDir>\out (padrao %TEMP%\dc_sprites: caminho curto, MAX_PATH).
Saida: exit 0 e "SPRITES_OK" no log; exit 1 e "ERRO ..." na falha; exit 124 se passar de -TimeoutS (PC suspenso trava o render).
#>
param(
    [string]$Nome,
    [string]$Model,
    [string]$Anims,
    [string]$Hold,
    [string]$Preset,
    [string[]]$Extra = @(),
    [int]$TimeoutS = 900,
    [switch]$Teste,
    [string]$TestDir = (Join-Path $env:TEMP "dc_sprites"),
    [string]$Blender = "C:\Program Files\Blender Foundation\Blender 5.2\blender.exe",
    [string]$Placeholder = "C:\Users\VINICIUS\Videos\MEUS PROJETOS\chronicles-of-existence\client\tools\placeholder_humanoid.py"
)

$Tools = Split-Path -Parent $MyInvocation.MyCommand.Path
$Client = Split-Path -Parent $Tools
$Script = Join-Path $Tools "render_sprites.py"
if (-not (Test-Path $Blender)) { Write-Host "ERRO Blender nao encontrado: $Blender"; exit 1 }

function Invoke-Blender([string]$Py, [string[]]$ScriptArgs, [string]$Log) {
    $all = @("--background", "--factory-startup", "--python-exit-code", "1", "--python", $Py, "--") + $ScriptArgs
    # Start-Process nao poe aspas: caminho com espaco ("MEUS PROJETOS") quebraria; barra final escaparia a aspa
    $quoted = $all | ForEach-Object { '"' + ([string]$_).TrimEnd('\') + '"' }
    $p = Start-Process -FilePath $Blender -ArgumentList $quoted -NoNewWindow -PassThru `
        -RedirectStandardOutput $Log -RedirectStandardError ($Log + ".err")
    $null = $p.Handle   # sem ler o Handle antes, o ExitCode volta vazio no PowerShell 5.1
    if (-not $p.WaitForExit($TimeoutS * 1000)) {
        $p.Kill()
        Get-Content $Log -Tail 20 | Out-Host
        Write-Host "ERRO timeout de $TimeoutS s (Blender encerrado)"
        return 124
    }
    $p.WaitForExit()
    Get-Content $Log -Tail 40 | Out-Host
    Get-Content ($Log + ".err") -Tail 20 | Out-Host
    return $p.ExitCode
}

if ($Teste) {
    if (-not (Test-Path $Placeholder)) { Write-Host "ERRO placeholder do COE nao encontrado: $Placeholder"; exit 1 }
    $Fbx = Join-Path $TestDir "fbx"
    $Anims = Join-Path $TestDir "anims"
    $Out = Join-Path $TestDir "out"
    New-Item -ItemType Directory -Force -Path $Fbx, $Anims, $Out | Out-Null
    $code = Invoke-Blender $Placeholder @($Fbx) (Join-Path $TestDir "placeholder.log")
    if ($code -ne 0) { Write-Host "ERRO placeholder falhou (exit $code)"; exit $code }
    Copy-Item -Path (Join-Path $Fbx "Idle.fbx"), (Join-Path $Fbx "Run.fbx") -Destination $Anims -Force
    Copy-Item -Path (Join-Path $Fbx "Run.fbx") -Destination (Join-Path $Anims "CarryRun.fbx") -Force
    $Model = Join-Path $Fbx "Model.fbx"
    $Hold = Join-Path $Fbx "Attack1.fbx"
    $Nome = "teste"
    $Preset = "courier"
    $Contact = Join-Path $TestDir "contact.png"
    $Log = Join-Path $TestDir "render.log"
} else {
    if (-not $Nome -or -not $Model) { Write-Host "ERRO informe -Nome e -Model (ou -Teste)"; exit 1 }
    $Out = Join-Path $Client ("Assets\_DC\Resources\Sprites\" + $Nome)
    $Contact = Join-Path $Client ("Builds\sprites_contact\" + $Nome + ".png")
    $Log = Join-Path $env:TEMP ("dc_render_" + $Nome + ".log")
    if (-not $Preset) { $Preset = $Nome }
}

$a = @("--model", $Model, "--out", $Out, "--contact", $Contact)
if ($Preset -ne "none") { $a += @("--preset", $Preset) }
if ($Anims) { $a += @("--anims", $Anims) }
if ($Hold) { $a += @("--hold", $Hold) }
$a += $Extra

$t0 = Get-Date
$code = Invoke-Blender $Script $a $Log
Write-Host ("render_sprites: exit {0} em {1:n0} s -> {2} (log {3})" -f $code, ((Get-Date) - $t0).TotalSeconds, $Out, $Log)
exit $code
