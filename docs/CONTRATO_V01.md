# Contrato da v0.1 (greybox jogável no PC) — 2026-10-09

Fonte da verdade: `GDD.md` (§2 regra de carregar/pousar, FTUE revisada §3, §6 economia, §18–19 MVP/v0.1, §28 exploits). Este documento fixa **o que a v0.1 entrega e os números iniciais**; os números são hipóteses que o bot mede em `BALANCE.md`. Mudou regra ou número? Atualize aqui e em `BALANCE.md` no mesmo commit.

## 1. Escopo da v0.1

Entra: 1 região, 8 contratos em sequência, 6 salas + sala do chefe + sala de entrega, 3 cargas (Caixa, Relíquia frágil, Ovo), 4 inimigos (Morcego, Ladrão, Guarda, Bruto) + 1 chefe (Ogro), regra de carregar/pousar, dash, portas com ícones de ameaça/recompensa, baú opcional, carga perdida com resgate, guilda com 3 linhas de upgrade, 1 moeda (Coins), save versionado com estado do contrato, bot headless, diário CSV de playtest.

Fica fora (v0.2+): anúncios e IAP, contrato do dia, contrato por código, perks de run, sala secreta, bolo/mensagem/barril, segunda região, analytics remoto. Nada de energia nem item aleatório pago.

## 2. Arquitetura (mesmo padrão do Forge Street)

| Camada | Pasta | Regra |
|---|---|---|
| `DC.Core` | `client/Assets/_DC/Scripts/Core/` | C# puro, `noEngineReferences`, determinístico (RNG próprio com semente), passo fixo de 1/30 s. Regras, conteúdo (defs), save, bot, eventos. |
| `DC` (view) | `client/Assets/_DC/Scripts/View/` | MonoBehaviours, câmera, entrada, HUD por código, arte procedural, SFX, diário CSV, flags de dev. Só lê o Core e manda intenção. |
| `DC.EditorTools` | `client/Assets/_DC/Editor/` | `Setup.Apply`, `Setup.BuildWindows`, `Setup.BuildAndroidDev`. |
| `DC.Tests` | `client/Assets/_DC/Tests/EditMode/` | NUnit; rodam também em `dotnet test client/tools/coretests`. |

Unidades: metros e segundos. Origem no canto inferior esquerdo da sala. Vetor próprio `V2` no Core (sem UnityEngine).

**Entrada (contrato Core ← View):** a cada quadro a View chama `Tick(dt, move, dash)`:
- `move` = direção 0..1 enquanto o dedo arrasta (ou WASD); `V2.Zero` quando o dedo está solto.
- `dash` = verdadeiro no quadro do toque rápido (< 150 ms sem arrastar) ou da barra de espaço.
- O Core cuida de todo o resto (inclusive os 200 ms de dedo solto para pousar), para que o bot e os testes usem as mesmas regras.

## 3. Regras do contrato (GDD §2)

1. **Carregando:** arrastar move o courier com a carga nas mãos; não ataca.
2. **Dash:** toque rápido. Impulso de 3 m em 0,18 s na direção do último movimento, empurra inimigos 1,5 m, **larga a carga** no ponto de partida. Recarga 1,6 s (upgrade reduz). Nenhuma carga da v0.1 proíbe o dash (o bolo, que proíbe, é v0.2).
3. **Pousar e lutar:** dedo solto por ≥ 0,2 s = parar. Se tem a carga, pousa a 0,6 m ao lado. Parado, ataca sozinho o inimigo mais próximo dentro de 5 m (projétil, 0,45 s de recarga, 1 de dano, 12 m/s).
4. **Pegar de volta:** voltar a andar a até 1,5 m da carga no chão pega automaticamente.
5. **Sem carga nas mãos andando:** anda mais rápido (+15%), não ataca.
6. **Porta:** só atravessa com a carga nas mãos (sem ela, a porta não abre e a dica avisa). Guarda vivo mantém a porta dele fechada.
7. **Carga sozinha:** se a carga ficar no chão a mais de 3 m do courier por 8 s, nasce um Ladrão perto dela (§28).
8. **Integridade:** cada golpe na carga = 1 rachadura (visível). Relíquia quebra com 3 (contrato falha). Nas outras, a rachadura só reduz o bônus.

Números iniciais: courier 4,2 m/s carregando (Caixa pesada não entra na v0.1), raio 0,4 m, vida 6 (+1 por nível de "Vida").

## 4. Cargas (v0.1)

| Carga | Verbo | Falha | Pagamento base |
|---|---|---|---|
| Caixa | nenhum (tutorial) | — | 100% |
| Relíquia frágil | 3 rachaduras = quebra | contrato falho | 140% |
| Ovo | choca se o contrato passar do tempo-alvo; o filhote (vida 2) passa a seguir o courier e também tem de chegar | filhote morto = contrato falho | 120% (chocado: 80%) |

Bônus por integridade na entrega: 0 rachaduras ×1,5; 1 ×1,2; 2 ×1,0.

## 5. Inimigos

| Inimigo | Papel | Vida | Vel. | Ataque |
|---|---|---|---|---|
| Morcego | enche a sala, persegue o courier | 2 | 2,4 | toque, 1 de dano, recarga 1 s |
| Ladrão | ignora o courier; corre para a carga **no chão**, pega (0,5 s) e foge para a borda a 1,6 m/s. Atingido, derruba a carga. Se escapar, a carga vira carga perdida e o contrato falha. | 3 | 2,0 | — |
| Guarda | parado na porta; só ela abre quando ele morre (sala de luta obrigatória) | 5 | 0 | golpe 1,2 m, 1 de dano, telegraph 0,5 s |
| Bruto | persegue o courier; golpe com telegraph de 0,6 s, 2 de dano; golpe pelas costas com carga nas mãos acerta também a carga; golpe a até 1,2 m da carga no chão acerta a carga | 6 | 1,6 | — |
| Ogro (chefe) | lento; pancada em área (2 m, telegraph 1 s, 3 de dano, acerta carga no chão); chama 1 Ladrão a cada 10 s | 30 | 1,2 | — |

## 6. Salas, rota e contratos

- Sala 9 m × 14 m (retrato; a câmera mostra a sala inteira). Entrada embaixo no centro; 1–3 portas na parede de cima. Obstáculos são retângulos.
- Cada porta mostra o que há na próxima sala: ameaça 0–3 (caveiras) e recompensa (baú ou nada). O jogador escolhe andando até a porta.
- Conteúdo como dados no Core (`RoomDef`, `ContractDef`): 6 salas normais (R1–R6), sala do chefe (B1) e sala de entrega (D1, cliente no topo). Contrato = camadas de salas + entrega; cada porta leva a uma sala da próxima camada.
- 8 contratos em ordem (Reputation = contratos concluídos; o próximo abre quando o anterior é concluído; os já concluídos podem ser repetidos):

| # | Carga | Camadas | Notas |
|---|---|---|---|
| C1 | Caixa | R1 (Guarda) → R2 (1º Ladrão) → D1 | FTUE §3: dica de pousar; 1ª bifurcação na R1 |
| C2 | Caixa | 2 camadas + D1 | 1º upgrade da guilda liberado antes |
| C3 | Ovo | 2 camadas + D1 | tempo-alvo 30 s (era 90 s: o ovo nunca chocava; BALANCE.md decisão 1) |
| C4 | Caixa | 3 camadas + D1 | Bruto aparece |
| C5 | Relíquia | 3 camadas + D1 | |
| C6 | Ovo | 3 camadas + D1 | tempo-alvo 45 s; Guarda + Ladrão juntos (1ª derrota provável) |
| C7 | Relíquia | 3 camadas + D1 | |
| C8 | Caixa | 3 camadas + B1 + D1 | chefe |

- **Baú** (opcional): moedas fixas, mostradas no ícone da porta; abre parado ao lado dele por 1 s.
- **Carga perdida (C4 do GDD):** ao morrer (ou se o Ladrão escapar), a carga fica pendente (tipo, rachaduras, sala). O próximo contrato oferece o resgate: a carga perdida aparece no chão da 1ª sala. Entregar as duas paga +50% do pagamento da perdida. Só 1 pendente; expira após 1 contrato; o bônus sai uma vez por id de carga; o bônus é menor que o pagamento perdido (exploit §28). Carregar as duas = as duas nas mãos, pousam e se pegam juntas.

## 7. Guilda (meta) e economia

- 1 moeda: Coins. Fontes: pagamento + bônus de integridade, baús, resgate. Ralos: upgrades.
- 3 linhas, 5 níveis cada: **Vida** (+1), **Dash** (−10% de recarga), **Bolsa** (+10% de moedas). Custo = 60 × 1,6^nível (arredondar a 5). Nenhum upgrade deixa a carga indestrutível.
- Sem renda passiva, sem offline, sem energia.

## 8. Save

- Texto `chave=valor` com `v=1`. Grava: moedas, níveis, contratos concluídos, carga perdida pendente e **o estado do contrato em curso** (contrato, camada, sala, vida, rachaduras, tempo) a cada dano e a cada sala.
- Reabrir no meio do contrato retoma na entrada da sala atual com a vida e as rachaduras gravadas (nunca restaura dano, §28).
- Save sem versão = v0 → migra. Versão futura → `Sim.Load` recusa, devolve estado novo marcado `SaveRefused`, e a View **não grava** por cima.

## 9. Eventos e diário

O Core emite eventos por Tick (lista limpa a cada Tick). A View usa para SFX, efeitos e diário CSV em `persistentDataPath/diario.csv` com os eventos do GDD §14: `contract_start`, `contract_end`, `room_enter`, `route_choice`, `damage_taken`, `cargo_integrity`, `optional_loot`, `death_room`, `room_fight_choice` (lutou = matou ao menos 1 inimigo não-Guarda na sala), `cargo_drop`, `cargo_pickup`, `thief_grab`, `cargo_rescue_offer`, `cargo_rescue_result`, `guild_upgrade`, `currency_source`, `currency_sink`.

## 10. Bot e portões

- `Bot`: política heurística determinística (semente) que joga contratos inteiros no Core sem render. Mede: conclusão por contrato, mortes, sala da morte, duração, % de salas com luta (H1), moedas.
- `BALANCE.md`: tabela por contrato (200 sementes) + decisões.
- Portão de cada leva: `dotnet test client/tools/coretests` verde · `dotnet build client/tools/viewcheck/view` com 0 erros · Unity EditMode verde · build Windows · `DungeonCourier.exe -batchmode -nographics -autoplay 8` → `AUTOPLAY OK` · fotos 540×960 conferidas em `client/Builds/validation_v01/shots/`. APK só no fim de um bloco grande; ao gerar, apagar os APKs anteriores (fica só o atual). Celular é o último teste.

## 11. Flags de dev (View)

`-autoplay N` (bot joga N contratos sem render e sai 0/1) · `-bot` (bot dirige a partida com render) · `-speed N` · `-shot foto.png -shotdelay s` · `-contract K` · `-reset` · `-testsession` (não lê nem grava save/diário).
