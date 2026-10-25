# Como trabalhamos

- **Branch padrão:** `main` sempre compila e passa nos testes. Trabalho novo em `feat/<assunto>`, `fix/<assunto>`, `docs/<assunto>` ou `art/<assunto>` e entra por **Pull Request**.
- **Commits:** em português, no imperativo, com prefixo de escopo: `feat(dc-core): …`, `fix(dc-view): …`, `art(dc): …`, `docs: …`, `test: …`, `chore: …`. Um assunto por commit.
- **Portão para o PR entrar (quando houver código):** `client/tools/verify.sh quick` roda os itens 1 e 2; `full` roda também o 3 (EditMode, build Windows e `-autoplay 30`). O CI (`nucleo.yml`) roda o item 1.
  1. testes do núcleo verdes (`dotnet test client/tools/coretests`);
  2. a View compila sem o Unity (`dotnet build client/tools/viewcheck/view`, 0 erros);
  3. Unity EditMode verde e build Windows/Android quando o PR mexe em jogo;
  4. mudança de economia vem com medição do bot em `docs/BALANCE.md` (antes/depois);
  5. fotos em `client/Builds/validation_*/shots/` quando muda o visual.
- **Saves:** enums e listas salvas por índice só crescem no FIM.
- **Versão:** SemVer no `PlayerSettings.bundleVersion` (definido no `Editor/Setup.cs`); cada versão vira tag `vX.Y.Z` e Release com notas e APK.
- **Docs:** decisão nova vai para o doc vivo do assunto (GDD §0, `BALANCE.md`, `MONETIZACAO.md`...) no mesmo PR; arquivo criado, movido ou dividido atualiza o `docs/MAPA_CODIGO.md`. Plano cumprido vai para `docs/historico/`.
- **Nunca** comitar `.env`, chaves, arte-fonte crua do Tripo/Mixamo, builds, cache do Unity ou dados de playtest de pessoas reais (ver `.gitignore`).
