# Contrato de lançamento — o GDD inteiro (2026-10-10)

**Pedido do Vinicius (2026-10-10, depois do release `v1.0.0-beta`):** "enquanto o gdd não estiver completo com tudo acabado não finalizamos". Autonomia mantida: sem pausas, sem pedidos de aprovação; commit, push, PR e merge fazem parte do fluxo; créditos de arte autorizados ("seja na arte"), registrados em `PROVENIENCIA.md` por lote; sem teste no POCO (PC primeiro).

Base: `GDD.md` (§2–§17, §20–§21, §27–§28), `CONTRATO_V1.md` (tudo que lá vale continua valendo), `BALANCE.md`. Números são hipóteses: o bot mede e o `BALANCE.md` registra antes e depois.

**Guardrails que não mudam:** 1 moeda (Coins); Reputation não se compra; nenhum item aleatório pago (ECA Digital, Lei 15.211/2025); baú do dia com conteúdo visível antes de abrir; interstitial só entre contratos, nunca nos 3 primeiros do dia, com teto; rewarded com id de transação (`rw-<contratoId>-<tipo>`); sem energia, sem gacha; criativos gravados do jogo real; nada de dado pessoal no diário.

## 1. Matriz GDD × feito (estado em 2026-10-10, `main` 188aa24)

| GDD | Item | Estado | Onde fecha |
|---|---|---|---|
| §2 | carregar ocupa as mãos, dash larga, pousar para lutar, pegar de volta | **feito** | — |
| §2 | portas com ícone de ameaça/recompensa; carga que abre porta | **feito** | — |
| §2 | carga perdida fica na masmorra + resgate | **feito** | — |
| §2 | coração arcano (ímã de moedas **e** de inimigos) | falta | §3 (carga 8) |
| §2/§12 | haptic e som em pousar/pegar/carga atingida/ladrão | **feito** | — |
| §3 | FTUE revisada (0–10 min) | **feito** (v0.1/v1) | — |
| §3 | "o quadro mostra o contrato raro do próximo retorno" | falta | §6.4 |
| §3/§7 | 1ª oferta de rewarded após falha real ("proteja a carga e retome") | falta | §7 |
| §4 | 1º dia: 8 contratos, 12 salas, 5 cargas, 6 inimigos, mini, chefe, 8 perks, 3 equipamentos, contrato do dia | **feito** (região 1 cobre) | — |
| §4 | 1ª compra (Remove Ads / Starter Pack) depois de 3 contratos | falta | §7 |
| §4 | razões para voltar: contrato raro, carga nova, upgrade, **blueprint** | parcial (falta raro e blueprint) | §6.4, §6.6 |
| §5 | D3 região 2 · D7 contrato de chefe · D14 modificadores · D30 quadro de temporada · D60 cargas novas · D90 desafio sem fim · mastery | parcial (só região 1 e chefe) | §2, §6 |
| §6 | fontes: contrato, integridade, loot, daily, rewarded | parcial (falta rewarded) | §7 |
| §6 | ralos: upgrades, **rerolls limitados**, **equipamento lateral**, guilda | parcial | §6.5, §6.6 |
| §6 | Stamps | **decisão do GDD:** só volta "se a coorte justificar" | fica fora até haver coorte (§10) |
| §7 | rewarded: retomar 1×/contrato, dobrar bônus, revelar rota segura, baú do dia | falta | §7 |
| §7 | interstitial entre contratos com teto | falta (desligado) | §7 |
| §7 | IAP: Remove Ads, Starter Pack, roupas do courier, pacote de contrato/evento (conteúdo fixo) | falta | §7 |
| §8 | retenção D1/D3/D7/D14/D30 | parcial | §2, §6 |
| §9 | live ops: Fragile Week, Midnight Delivery, No-Damage, Treasure Detour, Boss Courier Run | falta | §6.1 |
| §10 | **4 regiões** | 1 de 4 | §2 |
| §10 | **30–40 salas modulares** | 21 (16 + 2 secretas + M1/B1/D1) | §2 → ~48 |
| §10 | **15 cargas** | 7 de 15 | §3 |
| §10 | **20 inimigos** | 6 de 20 | §4 |
| §10 | **8 chefes** | 2 de 8 | §4 |
| §10 | **25 perks** | 10 de 25 | §5 |
| §10 | **5 templates de evento** | 0 de 5 | §6.1 |
| §11 | carga visível no personagem; arte Tripo → Blender → sprites | **feito** (região 1) | §8 para o resto |
| §12 | áudio (passos, golpe, impacto, dash, carga, entrega, porta, segredo, recompensa) | **feito** | §9 (músicas por região) |
| §13 | módulos (ContractDefinition, RouteGraph, CargoModifier, EnemyDirector, GuildMeta...) | **feito** no `DC.Core` | §2 (multirregião) |
| §14 | eventos de analytics + `device_fps_bucket`, `route_choice`, `room_fight_choice`, `iap_purchase`, `rewarded_resume` | parcial (diário local) | §6.8; envio remoto = dependência (§11) |
| §17 | 13 criativos de UA gravados do jogo real | falta | §9.3 |
| §20 | 2 regiões, 8 cargas, meta, ads/IAP, remote config, daily, código | parcial | §2, §3, §7; remote config = §6.9 |
| §21 | 4 regiões, 15 cargas, eventos, **iOS**, **localização** | parcial | §2–§6, §9.2; iOS = dependência (§11) |
| §28 | exploits: fechar o app após dano, morrer para resgate, rewarded repetido, reroll até carga fácil, relógio, carga largada na entrada, save futuro | parcial (faltam rewarded e reroll) | §6.5, §7 |

### 1b. Estado em 2026-10-11 (1.1.0-beta na `main`, PR #5)

| Área do GDD | Estado | Falta |
|---|---|---|
| 4 regiões, ~48 salas, 72 contratos | **feito** no núcleo e na View; o bot fecha cada região | — |
| 15 cargas, 20 inimigos, 8 chefes, 25 perks | **feito** no núcleo e na View | sprites: múmia, gárgula, necromante, região 4 inteira e o dragão (raia G, Leva C); hoje desenho procedural legível |
| Eventos (5), modificadores, temporada, raro, rerolls, equipamento + projetos, maestria, marcos, sem fim | **feito** (núcleo + telas) | economia: sobra de moedas (raia E, Leva C) |
| Monetização (rewarded, interstitial, IAP) | lógica no núcleo + telas com **simulador de anúncio e loja falsa** | SDK real (Unity IAP / LevelPlay) e ids das contas do Vinicius (§11) |
| Save v3, telemetria, remote config local | **feito** | envio remoto = dependência (§11) |
| Áudio por região e chefe | **feito** (sintetizado) | escuta humana |
| Localização pt-BR/EN/ES/ID | **feito** | revisão de nativo (opcional) |
| Página da loja e 13 criativos | textos e roteiros **feitos**; gravação `-record` e montagem prontas | gravar e montar os vídeos; fotos da loja; roteiro 13 depende das roupas |
| Roupas do courier (6) | loja e seleção **feitas** (tinta de reserva) | sprites das roupas (raia G) |
| iOS | código sem API só de Android | Mac + conta Apple (§11) |

## 2. Regiões (4) e salas

Cada região tem tema (piso, parede, decoração, paleta, música), ~10 salas normais, 1 sala secreta, 1 mini-chefe (M), 1 chefe (B) e a entrega (D). Total de salas: 16 + 10 + 10 + 10 normais, 2 + 1 + 1 + 1 secretas, 4 M, 4 B (D é cena) = **~48 salas**, acima do teto de 30–40 do GDD por causa da região 1 que já existe (melhor sobrar que faltar).

| # | Região | Tema / verbo do lugar | Destrava | Contratos |
|---|---|---|---|---|
| 1 | **Porões da Guilda** | pedra, tochas (feita) | início | C1–C24 (feita) |
| 2 | **Minas Alagadas** | trilhos e vagonetes, poças de água (−25% vel.; o cristal de gelo recongela), lanternas de mina | 30 estrelas e o Ogro vencido | M2-1…M2-16 |
| 3 | **Cripta Assombrada** | escuro: luz em raio em volta do courier e das tochas; lápides, teias (prendem a carga 1 s) | 60 estrelas e o chefe da região 2 | M3-1…M3-16 |
| 4 | **Forja do Dragão** | lava (dano no contato), respiros de fogo com telegraph, correntes e bigornas | 95 estrelas e o chefe da região 3 | M4-1…M4-16 |

- Contratos das regiões 2–4: 16 por região (C1–C3 de cada região ensinam a novidade; 1 mini-chefe no meio; o chefe no fim), 4–6 camadas, cada carga nova estreia num contrato próprio. Campanha total: **72 contratos**.
- Contrato do dia e por código sorteiam a região entre as destravadas (código carrega a região: o código de 6 caracteres continua; a região sai da semente).
- O resgate continua "na mesma região" (GDD §2).

## 3. Cargas (15)

As 7 da v1 continuam. As 8 novas, cada uma com um verbo:

| # | Carga | Verbo | Falha | Estreia |
|---|---|---|---|---|
| 8 | **Coração arcano** | ímã de moedas **e** de inimigos num raio visível (GDD §2) | — (horda maior) | região 2 |
| 9 | **Cristal de gelo** | derrete com o tempo (barra); perto de lava derrete 3× mais rápido; poça de água recongela | derreteu | região 2 |
| 10 | **Gaiola do grifinho** | o bicho grita a cada ~6 s (onda visível) e acorda/atrai inimigos num raio; dash acalma (zera o tempo) | — | região 2 |
| 11 | **Lanterna** | ilumina a sala escura; no chão ilumina só ali; derrubada por golpe apaga e só reacende numa tocha | — (escuro) | região 3 |
| 12 | **Espelho encantado** | carregado de frente reflete flechas e feitiços; cada reflexo racha (3 = quebra) | quebrou | região 3 |
| 13 | **Balão de gás** | leve (+15% vel.); no chão sobe e escapa em 4 s; ladrões não alcançam | escapou | região 3 |
| 14 | **Planta carnívora** | no chão morde inimigos num raio de 1,2 m (torre), mas morde o courier se ele ficar perto sem carregá-la | — | região 4 |
| 15 | **Mapa do tesouro** | revela baús e paredes rachadas da camada; ladrões o preferem (+30% vel. atrás dele) | — | região 4 |

## 4. Inimigos (20) e chefes (8)

Os 6 da v1 (morcego, ladrão, guarda, bruto, arqueiro, limo) continuam. Os 14 novos:

| Região | Inimigo | Papel |
|---|---|---|
| 2 | **Goblin mineiro** | arremessa picareta em arco (telegraph no chão); acerta courier e carga |
| 2 | **Toupeira** | some debaixo da terra e sai embaixo da carga no chão (derruba/empurra 1,5 m) |
| 2 | **Sapo do lodo** | língua de 3 m puxa a carga do chão para perto dele |
| 2 | **Besouro-bomba** | corre até o courier e explode após 1 s (pode detonar barril, racha ovo) |
| 2 | **Vagonete desgovernado** | perigo móvel nos trilhos: atropela tudo na linha (inimigos também) |
| 3 | **Fantasma** | atravessa paredes; possui a carga no chão e a faz flutuar para longe devagar |
| 3 | **Esqueleto** | volta a montar em 4 s se a carga não estiver a 3 m quando ele cai |
| 3 | **Aranha** | solta teia que prende o courier 1 s (prende a carga se ela estiver no chão) |
| 3 | **Múmia** | lenta, agarra e segura o courier até levar 2 golpes ou dash |
| 3 | **Gárgula** | estátua que acorda quando o courier passa perto com a carga; mergulho em linha |
| 4 | **Salamandra** | deixa rastro de fogo por 3 s (dano; derrete o cristal) |
| 4 | **Diabrete** | teleporta para perto da carga no chão e a troca de lugar com ele |
| 4 | **Kobold incendiário** | joga frascos que viram poças de fogo |
| 4 | **Elemental de magma** | ao morrer se divide em 2 menores |

Chefes (mini e principal por região):

| Região | Mini-chefe (M) | Chefe (B) |
|---|---|---|
| 1 | Capitão Ladrão (feito) | Ogro (feito) |
| 2 | **Rainha Toupeira**: cava em ciclos, sai embaixo da carga, chama toupeiras | **Golem de Pedra**: pancada em área, solta pedras que bloqueiam rotas, ponto fraco nas costas |
| 3 | **Viúva Aranha**: teias pela sala, puxa a carga com o fio | **Necromante**: ergue esqueletos em ondas, raio telegrafado, foge pela sala |
| 4 | **Capataz Kobold**: chicote (alcance 3 m), chama kobolds, joga frascos | **Dragão Ancião**: sopro em cone com telegraph, voo e pouso em área, ovos de salamandra (chefe final) |

## 5. Perks (25)

Os 10 da v1 continuam. 15 novos (o sorteio pela semente continua; nada pago): **Mola** (dash +40% de distância) · **Segundo fôlego** (1×/contrato volta com 2 de vida) · **Faro de baú** (porta mostra o conteúdo do baú) · **Pé firme** (água, lama e teia não freiam) · **Isolamento** (gelo derrete 40% mais devagar e o ovo choca 20% mais tarde) · **Alça firme** (o 1º dash de cada sala leva a carga junto) · **Contragolpe** (dash em quem segura a carga a derruba) · **Caçador** (+2 moedas por inimigo derrotado) · **Couro grosso** (−1 no dano de golpes de 2+) · **Olhos de gato** (luz do courier +60% no escuro) · **Assobio** (ladrão que pega a carga fica parado 1,5 s) · **Primeiros socorros** (+1 de vida ao abrir baú) · **Corredor** (+20% de velocidade sem a carga) · **Escudo da carga** (o 1º golpe em cada sala na carga pousada é ignorado) · **Batedor** (as portas mostram o número exato de inimigos).

## 6. Sistemas de longo prazo (GDD §5, §6, §8, §9)

1. **Eventos (5 templates, offline):** um por semana em rodízio pela data (semana ISO; voltar o relógio não repete recompensa), quadro de eventos com 3 contratos especiais e recompensa fixa visível. Fragile Week (só cargas frágeis, +50% de pagamento) · Midnight Delivery (todas as salas escuras, lanterna obrigatória) · No-Damage Contract (1 de vida, bônus ×3) · Treasure Detour (baús dobrados e salas secretas abertas) · Boss Courier Run (os chefes vencidos em sequência).
2. **Modificadores (D14):** destravam com 40 estrelas; o jogador escolhe 0–2 por contrato e ganha +pagamento: Neblina (visão curta), Pressa (tempo-alvo −30%), Horda (+1 inimigo por sala), Frágil (carga com 2 rachaduras em vez de 3), Pão-duro (baús −50%, pagamento +). O contrato do dia sai com 1 modificador.
3. **Quadro de temporada (D30), offline:** temporada de 28 dias pela data; 20 objetivos ("entregue 5 ovos chocados", "vença um chefe sem dano") com recompensas fixas (moedas, roupas do courier). Sem passe pago.
4. **Contrato raro:** ao voltar depois de ≥8 h, o quadro mostra 1 contrato raro (carga e região sorteadas pela data, pagamento ×2, 1×). O fim da sessão mostra "volte amanhã para o contrato raro".
5. **Rerolls limitados:** o contrato do dia e o raro podem ser trocados 1×/dia por Coins (preço crescente). Fecha o exploit "rerolar até a carga fácil".
6. **Equipamento lateral + blueprints:** 1 espaço de equipamento (além da arma): **Bomba de fumaça** (ladrões perdem a carga de vista 3 s), **Gancho** (puxa a carga de até 5 m), **Armadilha de urso** (prende 1 inimigo 3 s), **Botas de lama** (ignora lama/água). Cada um destrava com 3 blueprints, que saem de baús de conteúdo **visível** (o ícone da porta mostra o blueprint) e do quadro de temporada; depois sobe de nível com Coins.
7. **Mastery e marcos:** por carga, 5 níveis de maestria pelo número de entregas (cosmético: selo no quadro e +2% de pagamento por nível naquela carga). Marcos de reputação (10/30/60/95/150 estrelas) dão moedas e roupas.
8. **Desafio sem fim (D90):** destrava com o chefe da região 4; camadas infinitas misturando as 4 regiões, dificuldade crescente; recorde de camadas e semente semanal (o mesmo desafio para todos, comparável pelo código).
9. **Diário/telemetria:** eventos novos do GDD §14 (`route_choice`, `room_fight_choice` já existem? conferir; `device_fps_bucket` a cada 60 s; `rewarded_*`, `interstitial_*`, `iap_*`, `event_*`, `season_*`, `modifier_*`, `endless_*`). **Remote config:** arquivo `config.json` local com padrões no build e leitura opcional de uma URL (sem backend); sem URL configurada, usa os padrões.
10. **Save v3** com migração explícita de v2/v1/v0 (regiões, eventos, temporada, maestria, blueprints, equipamento lateral, compras, rewarded ids).

## 7. Monetização (GDD §7, nos guardrails)

- **Rewarded:** retomar 1×/contrato da sala atual após falha (não na 1ª sessão), dobrar o bônus do contrato, revelar a rota mais segura da camada, baú do dia (conteúdo visível antes). Cada concessão tem id `rw-<contratoId>-<tipo>` gravado no save; fechar e reabrir não repete.
- **Interstitial:** entre contratos; nunca nos 3 primeiros do dia; no máximo 1 a cada 3 contratos e 1 a cada 4 min; nunca dentro da masmorra; some com Remove Ads.
- **IAP (conteúdo fixo e listado):** Remove Ads · Starter Pack (moedas fixas + 1 roupa + 1 blueprint de cada equipamento lateral, oferecido depois do 3º contrato) · 6 roupas do courier · pacote de evento (contratos especiais extras do evento atual, conteúdo listado).
- **Implementação:** a lógica (ofertas, ids, limites, direitos) fica no `DC.Core` com testes. SDKs: **Unity IAP** com loja falsa no PC (testável sem conta); anúncios atrás de uma interface com o **simulador de anúncio** no PC/Editor. Os ids reais (Play Console, AdMob/LevelPlay) entram quando o Vinicius criar as contas (§11). O build de teste mostra "anúncio de teste".

## 8. Arte (Tripo → Blender → sprites; regra dos itens separados)

- **Lote 4 (região 2):** goblin mineiro (+ picareta separada), toupeira, sapo do lodo, besouro-bomba, vagonete, Rainha Toupeira, Golem de Pedra; cargas coração arcano, cristal de gelo, gaiola do grifinho; decoração (lanterna de mina, trilho, escora de madeira); texturas piso/parede da mina.
- **Lote 5 (região 3):** fantasma, esqueleto, aranha, múmia, gárgula, Viúva Aranha, Necromante (+ cajado separado); cargas lanterna, espelho, balão; decoração (lápide, vela, teia); texturas.
- **Lote 6 (região 4):** salamandra, diabrete, kobold incendiário (+ frasco separado), elemental de magma, Capataz Kobold (+ chicote separado), Dragão Ancião; cargas planta carnívora, mapa; decoração (bigorna, braseiro, corrente); texturas.
- **Roupas do courier (6):** variações de material no Blender sobre o mesmo modelo (sem crédito novo): Clássico (atual), Noturno, Real, Explorador, Bombeiro de forja, Fantasma (só temporada/marco); renderizadas pelo `render_sprites.py` com troca de material.
- Teto por lote: o custo previsto de cada lote vai ao `PROVENIENCIA.md` antes de gerar; o gasto real fecha o lote.

## 9. Áudio, localização, loja e criativos

1. **Áudio:** 3 músicas novas por região (masmorra) + 3 de chefe, SFX dos inimigos e cargas novos, tudo sintetizado pelo `audio_gen.py`.
2. **Localização:** pt-BR, EN, **ES** e **ID** (geos do teste: BR, PH, ID); tabela única de textos; ajuste de layout para o texto mais longo.
3. **Criativos (GDD §17):** os 13 criativos como vídeos 9:16 de 15–30 s gravados do jogo real (`-record`: sequência de quadros + ffmpeg), com o roteiro de cada um.
4. **Página da loja:** ícone 512, feature graphic 1024×500, 8 capturas 1080×1920, descrição curta e longa em 4 idiomas, posicionamento "sem energia, sem gacha" (C7).

## 10. Fica de fora por decisão do próprio GDD

- **Stamps (hard currency):** o GDD §6 diz que só voltam "se a coorte justificar". Sem coorte, não entram.
- **Online, guilda social, procedural infinito, battle pass** (GDD §18 "Sem").
- **IA em runtime** (GDD §24).

## 11. Depende do Vinicius (preparado do nosso lado, sem travar o resto)

- Conta Google Play Console + chave de assinatura de produção (o APK de teste usa a chave debug).
- Contas de anúncio (AdMob ou LevelPlay) e ids das unidades; produtos de IAP cadastrados na Play.
- iOS: Mac + conta Apple Developer (o código evita API só de Android; o build iOS sai num Mac).
- Backend de analytics/remote config, se quiser envio remoto (o diário local e o `config.json` cobrem o teste).
- Teste de mercado (GDD §15–§16): orçamento de UA e as coortes; os criativos ficam prontos (§9.3).

## 12. Levas e raias

Arquivos por região para as raias não colidirem: `Core/Content/Regiao2.cs`…`Regiao4.cs` e comportamentos em `partial class Run` (`Run.Regiao2.cs`...). O núcleo multirregião vem primeiro.

| Leva | Raias (paralelo) | Saída |
|---|---|---|
| A | **N** núcleo multirregião + sistemas §6 + monetização §7 (lógica) + save v3 · **M** desenho de conteúdo (salas/contratos/números das regiões 2–4 em tabela no formato do `RoomDef`/`ContractDef`) · **Coordenador** Lote 4 no Tripo | Core com regiões e sistemas, testes; `docs/CONTEUDO_REGIOES.md`; FBX do Lote 4 |
| B | **R2 / R3 / R4** conteúdo das regiões no núcleo (inimigos, chefes, cargas, salas, contratos, bot) em arquivos próprios · **G** render do Lote 4 e roupas · Coordenador Lotes 5 e 6 | 72 contratos jogáveis pelo bot |
| C | **K2** View do mundo das regiões 2–4 · **I2** telas novas (mapa de regiões, eventos, temporada, modificadores, sem fim, loja/IAP, ofertas de rewarded, equipamento lateral, maestria) · **G** render dos Lotes 5–6 · **J2** áudio | jogo completo no PC |
| D | **L10n** ES/ID · **IAP/Ads** Unity IAP (loja falsa) + simulador de anúncio · **UA** criativos + página da loja | entregáveis de lançamento |
| E | Coordenador: portão completo, balance da campanha de 72 contratos, fotos de tudo, PR/merge, `1.0.0-rc1` (APK + Windows) | release candidate |

**Portões (toda leva):** `dotnet test client/tools/coretests` · `dotnet build client/tools/viewcheck/view` (0/0) · Unity EditMode · build Windows · `-autoplay` da campanha · fotos 540×960 conferidas · BALANCE atualizado · PR com CI verde.

**Fim:** só quando todas as linhas da matriz §1 estiverem "feito" ou em §10/§11 (decisão do GDD ou dependência do Vinicius com a nossa parte pronta).
