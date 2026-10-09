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
- Estado: só GDD e estrutura do projeto; ainda não há código. Não comece o projeto Unity sem o Vinicius pedir neste grupo.
- Comece por: `README.md`, `docs/GDD.md` (revisão competitiva de 2026-10-09 no topo) e `docs/COMPETITIVO.md`; o layout do futuro projeto está em `client/README.md`.
