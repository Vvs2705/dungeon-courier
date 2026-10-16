# Instruções para o Claude — Dungeon Courier

Este repositório é **só do Dungeon Courier** (GitHub `Vvs2705/dungeon-courier`). Cada chat aberto aqui cuida **apenas deste jogo**.

## Escopo
- Leia e altere apenas arquivos deste repositório. Não mexa nos outros jogos do estúdio, na pasta raiz `JOGOS NOVOS` nem em outros repositórios, a não ser que o Vinicius peça explicitamente neste chat.
- Se o pedido for sobre outro jogo, avise e sugira abrir o chat no grupo daquele jogo no menu lateral.
- Os documentos deste repositório descrevem só o Dungeon Courier (nada de portfólio com os outros jogos).

## Como trabalhar
- Português brasileiro. Código do jogo em `client/Assets/_DC/` (núcleo em C# puro em `Scripts/Core`, testável fora do Unity).
- Git: trabalho em branch (`feat/`, `fix/`, `docs/`, `art/`), commits em português com escopo (ver `CONTRIBUTING.md`), entra por Pull Request na `main`. Merge, push na `main`, tags e Releases só com aval do Vinicius neste chat.
- PC com 7,7 GB de RAM: um processo pesado por vez (Unity **ou** emulador **ou** Blender).
- Guardrails do estúdio: 1 moeda no MVP; offline com teto de 2 h; nada de item aleatório pago (ECA Digital, Lei 15.211/2025); interstitial desligado na v0.1; criativos gravados do jogo real; créditos de IA (Tripo etc.) só com aval explícito.
- Memória do estúdio (leitura; atualize só a parte deste jogo): `C:\Users\VINICIUS\Videos\MEUS PROJETOS\Agentes\_memoria\` — `STATUS_jogos-novos-validacao.md` e, se existir, o arquivo deste projeto.

## Situação
- Estado (2026-10-16): **1.4.0-beta** na `main` — GDD inteiro do nosso lado (`docs/CONTRATO_LANCAMENTO.md` §1b), arte v2 (interface, elenco HD, ícone do app, retratos pintados), tutorial guiado (`View/Tour.cs`; `-tutorial` liga no `-testsession`), Unity IAP 5.4.3, LevelPlay + Unity Ads ligados (anúncio não personalizado, sem `AD_ID`; `docs/MONETIZACAO.md` §4), AAB assinado, política pública. Falta só o que depende dele (§11): Play Console de organização (espera o D-U-N-S, previsão 2026-11-25), iOS e playtest com gente. Portão: `dotnet test client/tools/coretests` · `dotnet build client/tools/viewcheck/view` · Unity EditMode · build Windows · `-autoplay 30` · fotos 540×960. APK do worktree só via `subst R:` (client/README).
- Regras de arte do Vinicius: itens (escudo, arma) gerados separados e presos ao osso no Blender; animação do Mixamo ou feita no Blender; créditos só com aval por lote. Testar no PC primeiro; manter só o APK atual.
- Comece por: `README.md`, `docs/CONTRATO_V01.md`, `docs/GDD.md` (revisão competitiva de 2026-10-09 no topo), `docs/BALANCE.md` e `client/README.md`.
