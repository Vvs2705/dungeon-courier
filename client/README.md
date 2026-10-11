# client/ — projeto Unity de Dungeon Courier

Unity **6000.3.23f1**, Built-in RP, Input System, uGUI por código, retrato. `client/` é a raiz do projeto (`Assets/`, `Packages/`, `ProjectSettings/`).

- `Assets/_DC/Scripts/Core/` — `DC.Core`, C# puro e determinístico (regras, conteúdo, save, bot). Testes em `Assets/_DC/Tests/EditMode/`, que rodam também fora do Unity: `dotnet test client/tools/coretests`.
- `Assets/_DC/Scripts/View/` — `DC`: o único MonoBehaviour de fluxo (`Game`), mundo, HUD, entrada de um dedo, sprites. Compila sem o Unity: `dotnet build client/tools/viewcheck/view`.
- `Assets/_DC/Editor/` — `Setup.Apply` (PlayerSettings, appId `br.com.vstack.dungeoncourier`, retrato, IL2CPP ARM64, API 26+), `Setup.BuildWindows`/`BuildWindowsRelease` e `Setup.BuildAndroidDev`/`BuildAndroidRelease` (apagam os APKs anteriores: fica só o atual); `SpriteImport` (ASTC 6×6 no Android).
  - **APK com caminho longo ou com espaços** (ex.: worktree em `...\MEUS PROJETOS\...\.claude\worktrees\...`): o CMake do Gradle falha em `prefab_command.bat`. Mapeie uma unidade curta, gere e desfaça: `subst R: <pasta client>` → `Unity -batchmode -quit -projectPath R:\ -executeMethod DC.EditorTools.Setup.BuildAndroidRelease` → `subst R: /D`.
- `Assets/_DC/Resources/Sprites/` — sprites gerados por `client/tools/render_sprites.py` (Blender); ver `docs/RENDER_SPRITES.md`.
- Flags de dev do build: `-autoplay N`, `-bot`, `-speed N`, `-shot foto.png -shotdelay s`, `-contract K` (1-based), `-reset`, `-testsession`, `-lang pt|en|es|id` (com `-testsession`: idioma das fotos, docs/UI_V1.md §6), `-record pasta [-recordfps 30] [-recordsecs 20] [-recordfrom 0]` (com `-testsession`: grava quadros PNG 540×960 com passo fixo de 1/fps s e o som do jogo em `audio.wav`, depois de pular `recordfrom` s de jogo, e sai; sem pausa ao perder o foco; docs/CRIATIVOS.md §2).
- Loja e criativos (Python + Pillow + ffmpeg ≥ 6.1; saída em `Builds/`, fora do git): `tools/loja_assets.py` (ícone 512, feature graphic 1024×500, logo e as 8 capturas 1080×1920; docs/LOJA.md) e `tools/criativos.py` (`--comandos` de gravação e montagem dos 13 criativos 9:16; docs/CRIATIVOS.md). Os dois têm `--autoteste`.

**Higienização (disco cheio):** pode apagar sem perder nada `client/Library/Bee/` (cache de build do Unity, ~4 GB depois de um APK; volta no próximo build, que fica mais lento), `client/Builds/win*/` (builds regeneráveis; o release fica no GitHub) e `client/Temp/`. **Não apague** `arte/` (arte crua fora do git, insubstituível sem gastar crédito), `client/Assets/` nem o trabalho não commitado.
