# Instruções para o Claude — Dungeon Courier

Este repositório é **só do Dungeon Courier** (GitHub `Vvs2705/dungeon-courier`). Cada chat aberto aqui cuida **apenas deste jogo**.

Regras comuns dos jogos do estúdio (escopo, git, guardrails, processo, arte, testes), editadas num lugar só:
@~/.claude/estudio/REGRAS_JOGOS_ESTUDIO.md

Se o arquivo acima não carregar (outro PC), o mínimo é: só este jogo; mudança entra por PR na `main`; nada de item aleatório pago; anúncio sem `AD_ID`; um processo pesado por vez.

## Deste jogo
- Código do jogo em `client/Assets/_DC/` (núcleo em C# puro em `Scripts/Core`, testável fora do Unity).

## Situação
- Estado (2026-10-16): **1.4.1-beta** na `main` — GDD inteiro do nosso lado (`docs/CONTRATO_LANCAMENTO.md` §1b), arte v2 (interface, elenco HD, ícone do app, retratos pintados), tutorial guiado (`View/Tour.cs`; `-tutorial` liga no `-testsession`), Unity IAP 5.4.3, LevelPlay + Unity Ads ligados (anúncio não personalizado, sem `AD_ID`; `docs/MONETIZACAO.md` §4), AAB assinado, política pública. Falta só o que depende dele (§11): Play Console de organização (espera o D-U-N-S, previsão 2026-11-25), iOS e playtest com gente. Portão: `dotnet test client/tools/coretests` · `dotnet build client/tools/viewcheck/view` · Unity EditMode · build Windows · `-autoplay 30` · fotos 540×960. APK do worktree só via `subst R:` (client/README).
- Comece por: `README.md`, `docs/CONTRATO_V01.md`, `docs/GDD.md` (revisão competitiva de 2026-10-09 no topo), `docs/BALANCE.md` e `client/README.md`.
