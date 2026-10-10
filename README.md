# Dungeon Courier

**Estilo:** ação casual com objetivo (roguelite leve), retrato.

**O que é:** você é o entregador de uma guilda e atravessa masmorras levando uma encomenda.

**Como funciona:** andando com a carga você só desvia; para lutar, precisa pousá-la (e os ladrões correm para ela). Cada carga muda um verbo: o ovo choca se demorar, o bolo proíbe o dash, a carta urgente abre portas. Em cada sala você escolhe 2–3 portas e decide entre lutar, fugir ou parar para o saque. Carga perdida fica na masmorra e pode ser resgatada no próximo contrato.

**Como vai ser jogar:** "entregue e saia vivo"; o pagamento melhora a guilda e o equipamento.

## Situação
**v1.0-beta (2026-10-10): versão completa para teste com jogadores reais, jogável no PC.** Região 1 "Porões da Guilda" com 24 contratos, 7 cargas, 7 inimigos (com o mini-chefe Capitão Ladrão e o chefe Ogro), 3 armas, 10 perks, salas secretas, estrelas, contrato do dia e por código, guilda 4×8, save v2, áudio, pt-BR/EN e diário local de teste (nada sai do aparelho sem o jogador exportar). Núcleo com 76 testes; o bot fecha a campanha. Arte dos Lotes 1–3 (Tripo → Blender → sprites). Sem anúncios nem compras.

- **Escopo da v1.0-beta:** [`docs/CONTRATO_V1.md`](docs/CONTRATO_V1.md) · **Balance medido pelo bot:** [`docs/BALANCE.md`](docs/BALANCE.md) · v0.1: [`docs/CONTRATO_V01.md`](docs/CONTRATO_V01.md)
- **Teste com jogadores:** [`docs/TESTE_JOGADORES.md`](docs/TESTE_JOGADORES.md), [`docs/PERGUNTAS_PLAYTEST.md`](docs/PERGUNTAS_PLAYTEST.md); relatório dos diários: `python client/tools/diario_report.py <pasta>`
- **Arte e áudio:** [`docs/ART_BIBLE.md`](docs/ART_BIBLE.md), [`docs/ASSETS.md`](docs/ASSETS.md), [`docs/RENDER_SPRITES.md`](docs/RENDER_SPRITES.md), [`docs/PROVENIENCIA.md`](docs/PROVENIENCIA.md), [`docs/AUDIO.md`](docs/AUDIO.md); UI: [`docs/UI_V1.md`](docs/UI_V1.md), mundo: [`docs/VIEW_V1_MUNDO.md`](docs/VIEW_V1_MUNDO.md)
- **Jogar no PC:** abra o projeto `client/` no Unity 6000.3.23f1 e rode `DC/Aplicar settings`, ou gere o build com `Unity -batchmode -quit -projectPath client -executeMethod DC.EditorTools.Setup.BuildWindowsRelease` e abra `client/Builds/win-release/DungeonCourier.exe` (arraste com o mouse ou WASD; Espaço = dash). APK de teste: `Setup.BuildAndroidRelease` (fica só o APK atual).

- **GDD:** [`docs/GDD.md`](docs/GDD.md) (revisão competitiva de 2026-10-09 no topo).
- **Comparativo com jogos similares e diferenciais:** [`docs/COMPETITIVO.md`](docs/COMPETITIVO.md).

## Estrutura do repositório
| Pasta | Para quê |
|---|---|
| `docs/` | GDD, comparativo e, depois, balance, validações e contratos de cada fase |
| `client/` | projeto Unity 6 (6000.3.x), Android primeiro, retrato; código do jogo em `client/Assets/_DC/` |
| `client/Assets/_DC/Scripts/Core/` | núcleo em C# puro (regras, simulação, save), testável fora do Unity |
| `client/Assets/_DC/Scripts/View/` | MonoBehaviours, UI, câmera, entrada |
| `client/Assets/_DC/Editor/` | setup do projeto e builds por linha de comando |
| `client/Assets/_DC/Tests/EditMode/` | testes NUnit do núcleo |
| `client/Assets/_DC/Resources/` | sprites e dados carregados em tempo de execução |
| `client/tools/` | ferramentas fora do Unity (testes `dotnet`, scripts de arte, relatório de playtest) |
| `arte/` | arte-fonte; `arte/tripo/` e `arte/mixamo/` ficam fora do git |

Como contribuir: [`CONTRIBUTING.md`](CONTRIBUTING.md). Histórico: [`CHANGELOG.md`](CHANGELOG.md).

## Repositório e licença
© V-STACK / Vinicius Souza. **Todos os direitos reservados.** Código, documentos e arte visíveis para acompanhamento, sem licença de uso, cópia ou redistribuição.
