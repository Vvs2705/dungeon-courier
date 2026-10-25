# Instruções para o Claude — Dungeon Courier

Este repositório é **só do Dungeon Courier** (GitHub `Vvs2705/dungeon-courier`). Cada chat aberto aqui cuida **apenas deste jogo**.

Regras comuns dos jogos do estúdio (escopo, git, guardrails, processo, arte, testes), editadas num lugar só:
@~/.claude/estudio/REGRAS_JOGOS_ESTUDIO.md

Se o arquivo acima não carregar (outro PC), o mínimo é: só este jogo; mudança entra por PR na `main`; nada de item aleatório pago; anúncio sem `AD_ID`; um processo pesado por vez.

## Deste jogo
- Código do jogo em `client/Assets/_DC/` (núcleo em C# puro em `Scripts/Core`, testável fora do Unity).

## Situação
- Estado (2026-10-22): **1.4.3-beta** na `main` — GDD inteiro do nosso lado (estado item a item no `docs/GDD.md` §0, auditado em 2026-10-25), arte v2 (interface, elenco HD, ícone do app, retratos pintados), tutorial guiado com cartão da carga nova (`View/Tour.cs`; `-tutorial` liga no `-testsession`), revisão de combate (Guarda sentinela, Nav por tamanho de corpo; `docs/BALANCE.md` no topo), Unity IAP 5.4.3, LevelPlay (ironSource 9.7.0) + Unity Ads ligados (anúncio não personalizado, sem `AD_ID`; `docs/MONETIZACAO.md`), AAB assinado, política pública. Falta só o que depende dele: Play Console de organização (espera o D-U-N-S, previsão 2026-11-25), iOS e playtest com gente. Portão: `client/tools/verify.sh full` (núcleo, viewcheck, EditMode, build Windows e `-autoplay 30`; `quick` sem o Unity) e fotos 540×960 conferidas. APK do worktree só via `subst R:` (client/README).
- Comece por: `docs/MAPA_CODIGO.md` (onde está cada coisa no código; Grep em vez de abrir arquivo inteiro), `README.md`, `docs/GDD.md` (§0 = estado real), `docs/BALANCE.md` e `client/README.md`. Planos já cumpridos (contratos v0.1/v1/lançamento, desenho das regiões) ficam em `docs/historico/`, só como registro: o código cita esses docs pelo nome.
