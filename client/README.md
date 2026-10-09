# client/ — projeto Unity de Dungeon Courier

Unity **6000.3.23f1**, Built-in RP, Input System, uGUI por código, retrato. `client/` é a raiz do projeto (`Assets/`, `Packages/`, `ProjectSettings/`).

- `Assets/_DC/Scripts/Core/` — `DC.Core`, C# puro e determinístico (regras, conteúdo, save, bot). Testes em `Assets/_DC/Tests/EditMode/`, que rodam também fora do Unity: `dotnet test client/tools/coretests`.
- `Assets/_DC/Scripts/View/` — `DC`: o único MonoBehaviour de fluxo (`Game`), mundo, HUD, entrada de um dedo, sprites. Compila sem o Unity: `dotnet build client/tools/viewcheck/view`.
- `Assets/_DC/Editor/` — `Setup.Apply` (PlayerSettings, appId `br.com.vstack.dungeoncourier`, retrato, IL2CPP ARM64, API 26+), `Setup.BuildWindows` e `Setup.BuildAndroidDev` (apaga os APKs anteriores); `SpriteImport` (ASTC 6×6 no Android).
- `Assets/_DC/Resources/Sprites/` — sprites gerados por `client/tools/render_sprites.py` (Blender); ver `docs/RENDER_SPRITES.md`.
- Flags de dev do build: `-autoplay N`, `-bot`, `-speed N`, `-shot foto.png -shotdelay s`, `-contract K` (1-based), `-reset`, `-testsession`.
