# Dungeon Courier

**Estilo:** ação casual com objetivo (roguelite leve), retrato.

**O que é:** você é o entregador de uma guilda e atravessa masmorras levando uma encomenda.

**Como funciona:** andando com a carga você só desvia; para lutar, precisa pousá-la (e os ladrões correm para ela). Cada carga muda um verbo: o ovo choca se demorar, o bolo proíbe o dash, a carta urgente abre portas. Em cada sala você escolhe 2–3 portas e decide entre lutar, fugir ou parar para o saque. Carga perdida fica na masmorra e pode ser resgatada no próximo contrato.

**Como vai ser jogar:** "entregue e saia vivo"; o pagamento melhora a guilda e o equipamento.

## Situação
**1.4.1-beta (2026-10-16): o GDD inteiro do nosso lado, com a arte v2, os anúncios reais e o tutorial guiado** (4 regiões, 72 contratos, 20 inimigos, 8 chefes, 15 cargas, 25 perks, eventos, temporada, sem fim, 4 idiomas, elenco maior e nítido, interface nova, ícone do app, 8 roupas, Unity IAP, LevelPlay + Unity Ads com anúncio não personalizado, AAB assinado, política de privacidade pública, página da loja e 13 criativos em 4 idiomas). Falta só a conta de organização da Play Console, que espera o D-U-N-S (previsão 2026-11-25). Testado no PC (portão completo) e no POCO F4 (Android 14): todas as telas certas, sem exceção, 308–322 MB de memória na partida. O que falta para o GDD completo está em [`docs/CONTRATO_LANCAMENTO.md`](docs/CONTRATO_LANCAMENTO.md) e no CHANGELOG.

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
