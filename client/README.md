# client/ — projeto Unity de Dungeon Courier

Ainda vazio. Ao começar:
1. Criar o projeto Unity **6000.3.x** **dentro desta pasta** (`client/` é a raiz do projeto: `Assets/`, `Packages/`, `ProjectSettings/`).
2. Código só em `Assets/_DC/` (as pastas já existem com `.gitkeep`; a Unity cria os `.meta` ao abrir).
3. Núcleo em C# puro em `Assets/_DC/Scripts/Core/`, com testes em `Assets/_DC/Tests/EditMode/` que rodam também fora do Unity por `client/tools/coretests` (`dotnet test`), como no Forge Street e no Rune Relay.
4. `Editor/Setup.cs` aplica os PlayerSettings (appId `br.com.vstack.<nome>`, retrato, IL2CPP ARM64, API 26+) e expõe `BuildWindows`/`BuildAndroidDev` por linha de comando.
