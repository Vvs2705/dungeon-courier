# Dungeon Courier

**Estilo:** ação casual com objetivo (roguelite leve), retrato.

**O que é:** você é o entregador de uma guilda e atravessa masmorras levando uma encomenda.

**Como funciona:** andando com a carga você só desvia; para lutar, precisa pousá-la (e os ladrões correm para ela). Cada carga muda um verbo: o ovo choca se demorar, o bolo proíbe o dash, a carta urgente abre portas. Em cada sala você escolhe 2–3 portas e decide entre lutar, fugir ou parar para o saque. Carga perdida fica na masmorra e pode ser resgatada no próximo contrato.

**Como vai ser jogar:** "entregue e saia vivo"; o pagamento melhora a guilda e o equipamento.

## Situação
**1.4.3-beta (2026-10-22): o GDD inteiro feito do nosso lado** (4 regiões, 72 contratos, 63 salas, 20 inimigos, 8 chefes, 15 cargas, 25 perks, eventos, temporada, sem fim, 4 idiomas, arte v2 com elenco HD, tutorial guiado, 8 roupas, Unity IAP, LevelPlay + Unity Ads com anúncio não personalizado e sem `AD_ID`, AAB assinado, política de privacidade pública, página da loja e 13 criativos em 4 idiomas). Testado no PC (portão completo) e no POCO F4 (Android 14). Falta só o que depende de fora: a conta de organização da Play Console (espera o D-U-N-S, previsão 2026-11-25), o build iOS e o teste com jogadores. O estado de cada item do GDD, com onde está no código, fica no [`docs/GDD.md`](docs/GDD.md) §0; o que mudou em cada versão, no [`CHANGELOG.md`](CHANGELOG.md).

## Documentos
| Assunto | Onde |
|---|---|
| Desenho e estado real (§0) | [`docs/GDD.md`](docs/GDD.md) · comparativo: [`docs/COMPETITIVO.md`](docs/COMPETITIVO.md) |
| Onde está cada coisa no código | [`docs/MAPA_CODIGO.md`](docs/MAPA_CODIGO.md) |
| Balance medido pelo bot | [`docs/BALANCE.md`](docs/BALANCE.md) |
| Monetização (IAP, anúncios, manifesto) | [`docs/MONETIZACAO.md`](docs/MONETIZACAO.md) |
| Página da loja e criativos | [`docs/LOJA.md`](docs/LOJA.md), [`docs/CRIATIVOS.md`](docs/CRIATIVOS.md) |
| Teste com jogadores | [`docs/TESTE_JOGADORES.md`](docs/TESTE_JOGADORES.md), [`docs/PERGUNTAS_PLAYTEST.md`](docs/PERGUNTAS_PLAYTEST.md); relatório: `python client/tools/diario_report.py <pasta>` |
| Arte, UI e áudio | [`docs/ART_BIBLE.md`](docs/ART_BIBLE.md), [`docs/RENDER_SPRITES.md`](docs/RENDER_SPRITES.md), [`docs/PROVENIENCIA.md`](docs/PROVENIENCIA.md), [`docs/UI_V1.md`](docs/UI_V1.md), [`docs/AUDIO.md`](docs/AUDIO.md) |
| Lições para os outros jogos do estúdio | [`docs/LICOES_PARA_O_ESTUDIO.md`](docs/LICOES_PARA_O_ESTUDIO.md) · prompt para começar um jogo novo: [`docs/PROMPT_MESTRE_NOVOS_JOGOS.md`](docs/PROMPT_MESTRE_NOVOS_JOGOS.md) |
| Planos e relatórios já cumpridos (contratos v0.1, v1 e de lançamento, desenho das regiões, balance por região) | [`docs/historico/`](docs/historico/README.md) |

## Jogar e testar
- **PC:** abra o projeto `client/` no Unity 6000.3.27f1 e rode `DC/Aplicar settings`, ou gere o build com `Unity -batchmode -quit -projectPath client -executeMethod DC.EditorTools.Setup.BuildWindowsRelease` e abra `client/Builds/win-release/DungeonCourier.exe` (arraste com o mouse ou WASD; Espaço = dash).
- **Android:** `Setup.BuildAndroidRelease` (APK; fica só o atual) e `Setup.BuildAndroidBundle` (AAB assinado, chave fora do git). Detalhes em [`client/README.md`](client/README.md).
- **Portão:** `client/tools/verify.sh quick` (núcleo e View sem o Unity) ou `full` (mais EditMode, build Windows e `-autoplay 30`). No CI roda o núcleo (`.github/workflows/nucleo.yml`).

## Estrutura do repositório
| Pasta | Para quê |
|---|---|
| `docs/` | GDD (com o estado real no §0), balance, monetização, loja, arte e playtest; `docs/historico/` guarda os planos já cumpridos |
| `client/` | projeto Unity 6 (6000.3.x), Android primeiro, retrato; código do jogo em `client/Assets/_DC/` |
| `client/Assets/_DC/Scripts/Core/` | núcleo em C# puro (regras, simulação, save), testável fora do Unity |
| `client/Assets/_DC/Scripts/View/` | MonoBehaviours, UI, câmera, entrada |
| `client/Assets/_DC/Editor/` | setup do projeto e builds por linha de comando |
| `client/Assets/_DC/Tests/` | testes NUnit do núcleo (`EditMode/`) e o teste do caminho do dinheiro (`PlayMode/`) |
| `client/Assets/_DC/Resources/` | sprites e dados carregados em tempo de execução |
| `client/tools/` | ferramentas fora do Unity (portão `verify.sh`, testes `dotnet`, scripts de arte e áudio, loja, criativos, relatório de playtest) |
| `arte/` | arte-fonte da interface (`logo/`, `ui/`, `retratos/`); `arte/tripo/`, `arte/mixamo/` e `arte/roupas/` ficam fora do git |

Como contribuir: [`CONTRIBUTING.md`](CONTRIBUTING.md). Histórico: [`CHANGELOG.md`](CHANGELOG.md).

## Repositório e licença
© V-STACK / Vinicius Souza. **Todos os direitos reservados.** Código, documentos e arte visíveis para acompanhamento, sem licença de uso, cópia ou redistribuição.
