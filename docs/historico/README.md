# docs/historico — planos e relatórios concluídos

Estes documentos guiaram a construção do jogo e já foram cumpridos. Ficam aqui, fora de `docs/`, como registro de **por que** cada número e cada regra estão como estão: o código cita vários deles pelo nome e pela seção (ex.: `CONTEUDO_REGIOES §5`, `CONTRATO_LANCAMENTO §7`). Não descrevem o estado atual. O estado atual está em [`../GDD.md`](../GDD.md) §0 e no [`../../CHANGELOG.md`](../../CHANGELOG.md).

Arquivados em 2026-10-25 (auditoria da 1.4.3-beta). Não edite: decisão nova vai para o GDD, o `BALANCE.md` ou o doc vivo do assunto.

| Documento | O que foi | Cumprido em |
|---|---|---|
| [`CONTRATO_V01.md`](CONTRATO_V01.md) | escopo e números iniciais da v0.1 (greybox, 1 região, 8 contratos) | v0.1, 2026-10-09 |
| [`CONTRATO_V1.md`](CONTRATO_V1.md) | escopo da v1.0-beta (região 1 completa, 24 contratos, armas, perks, dia e código) | `v1.0.0-beta`, 2026-10-10 |
| [`CONTRATO_LANCAMENTO.md`](CONTRATO_LANCAMENTO.md) | o GDD inteiro: matriz GDD × feito, regiões 2–4, cargas, inimigos, sistemas, monetização, levas e raias | 1.2.0–1.3.0-beta, 2026-10-12/16. A matriz atualizada está no GDD §0 |
| [`CONTEUDO_REGIOES.md`](CONTEUDO_REGIOES.md) | desenho das regiões 2–4 (salas, contratos, inimigos, chefes, cargas, economia), números como hipótese | digitado no `Core/Content/Regiao2-4.cs` e `Run.Regiao2-4.cs` (Leva B) |
| [`BALANCE_REGIAO2.md`](BALANCE_REGIAO2.md), [`BALANCE_REGIAO3.md`](BALANCE_REGIAO3.md), [`BALANCE_REGIAO4.md`](BALANCE_REGIAO4.md) | medição do bot por região, partindo de um save **estimado** da região anterior | substituídos pela campanha inteira do `BALANCE.md` ("Economia da campanha") |
| [`ASSETS.md`](ASSETS.md) | plano dos lotes de arte do Tripo (lista, prompts, custo previsto) | Lotes 1–6 gerados; o gasto real e os prompts usados estão no `PROVENIENCIA.md` |
| [`VIEW_SPRITES.md`](VIEW_SPRITES.md) | relatório da raia F: como a View usa as folhas (ritmo, flash, cena da entrega) | v0.1/v1; o formato das folhas segue no `RENDER_SPRITES.md` §4 |
| [`VIEW_V1_MUNDO.md`](VIEW_V1_MUNDO.md) | relatório da raia K: o mundo da v1.0-beta na View e a tabela de tempos dos chefes | v1.0-beta |

As fotos de validação dessas fases (`client/Builds/validation_v01`, `_v1`, `_v2`, `_v3`, todas anteriores à arte v2) saíram do repositório na mesma auditoria e continuam no histórico do git (ex.: `git show 4d3db49:client/Builds/validation_v3/shots/<foto>.png > foto.png`).
