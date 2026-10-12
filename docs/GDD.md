# GDD 06 — DUNGEON COURIER
## Action Casual / Objective-runner / Portrait / Android + iOS

> **Revisão 2026-10-09 (comparativo de mercado):** detalhe, fontes e matriz em [`COMPETITIVO.md`](COMPETITIVO.md). **Veredito mantido: NO-GO agora.** Esta revisão prepara o teste de criativos; não autoriza construção.
> - **O nicho continua quase vazio no mobile de ação.** Dungeon Delivery (Com2uS) é um arcade pequeno (20 avaliações no iOS dos EUA). Delivery Survivors (Steam, 4º tri/2026) usa o tema, mas o verbo dele é "matar tudo". A fantasia de entregar vende em escala no hybrid-casual (Pizza Ready: 150M downloads em 2025, segundo a própria Supercent).
> - **Core corrigido contra o dilema da raia B (§2):** carregar ocupa as mãos. Andando, o courier leva a carga e só tem o dash; parado, pousa a carga e ataca sozinho; o dash larga a carga. Ladrões correm para a carga no chão. Lutar vira escolha com custo, e o jogo não vira runner nem Archero.
> - **Cada carga muda um verbo, não só a barra de vida (§2),** e algumas cargas abrem portas.
> - **A carga perdida fica na masmorra** e pode ser resgatada no contrato seguinte (§2): motivo para começar o 2º contrato.
> - **Custo (§11, §22): PROPOSTA de mascote sem rig humano** (mímico ou goblin de mochila) **e greybox 2D top-down**, para cortar o maior custo apontado pela raia C. A decisão de arte é do Vinicius.
> - **Economia e monetização nos guardrails (§6, §7):** 1 moeda no MVP (Coins; Stamps saem do MVP), sem energia, sem item aleatório pago, baú com conteúdo visível, interstitial desligado na v0.1, rewarded com id de transação.
> - **KPIs e kill criteria alinhados ao veredito (§15, §26):** D1 ≥26%, D7 ≥5%, coorte de 1,5–2 mil installs, cancelar só após 2 iterações.
> - **Reuso corrigido (§13, §23)** para o que existe em disco. Seções novas: **§27 Diferenciais competitivos** e **§28 Hipóteses de protótipo e catálogo de exploits**.

> **Hipótese comercial:** um action casual orientado a objetivo pode se diferenciar dos survivor-likes puros ao criar urgência clara (“entregue e saia vivo”), produzindo anúncios legíveis e uma estrutura de conteúdo modular.

# 1. VISÃO DO PRODUTO

**Nome:** Dungeon Courier  
**Gênero:** action casual / objective runner / roguelite-lite  
**Plataforma:** Android primeiro.  
**Orientação:** Portrait.  
**Público:** 13–40, action casual, roguelite leve, dungeon games.  
**Proposta:** atravessar dungeons sob pressão para entregar relíquias, cartas e cargas especiais.  
**Fantasy:** ser o courier que aceita missões que ninguém mais aceita.  
**Diferencial:** o objetivo não é “matar tudo”; é entregar, sobreviver e decidir quanto risco aceitar no caminho.

**Diferencial revisado (2026-10-09):** a carga ocupa as mãos (você escolhe quando pousar para lutar) e cada carga muda um verbo da run. Ver §2 e `COMPETITIVO.md`.

# 2. GAMEPLAY

## Core mechanic
- movimento de um dedo;
- auto-ataque contextual;
- dash simples;
- rota com 2–3 portas por sala;
- pacote/relíquia impõe modificador;
- loot opcional compete com tempo/segurança.

## Exemplos de carga
- relíquia frágil: não sofrer X hits;
- coração arcano: atrai inimigos;
- ovo: não usar explosões próximas;
- mensagem urgente: timer;
- caixa pesada: movimento reduzido, recompensa maior.

## Core loop
`aceitar contrato → entrar → escolher rota → lutar/evitar → entregar/extrair → receber pagamento → melhorar guilda/equipamento`

## Vitória
Entrega realizada e extração concluída.

## Derrota
Personagem cai antes da entrega; parte do loot opcional se perde, contrato pode ser refeito.

## Sessão
4–9 min.

## Revisão 2026-10-09: carregar ocupa as mãos (PROPOSTA)

Motivo: a raia B apontou o dilema estrutural ("se evitar for o melhor, vira runner; se lutar for obrigatório, vira Archero genérico") e viu as cargas como variações de barra de vida. A regra abaixo faz de cada luta uma escolha com custo.

**Controle (um dedo):**
1. **Arrastar = andar carregando.** Com a carga nas mãos, o courier não ataca.
2. **Toque rápido = dash** (toque de menos de 150 ms sem arrastar, ≈ 4–5 quadros a 30 FPS; cooldown curto). O dash empurra inimigos e **larga a carga** no chão.
3. **Soltar o dedo por mais de 200 ms (6 quadros a 30 FPS) = parar, pousar e lutar.** Parado, o courier pousa a carga ao lado e ataca sozinho o inimigo mais próximo (a convenção "parar = atirar" do Archero, com um preço).
4. **Voltar a arrastar perto da carga (até 1,5 m) = pegar de volta**, automaticamente. Longe dela, a carga fica no chão.

**Inimigos com papéis:**
- **Ladrões:** ignoram o courier e correm para a carga no chão; ao serem atingidos, derrubam a carga. Lentos e visíveis.
- **Guardas:** param na porta e só saem se derrotados (salas de luta obrigatória).
- **Brutos:** atacam o courier; golpe pelas costas acerta também a carga.

**Salas:** cada sala se resolve passando sem lutar, lutando em volta da carga ou afastando os inimigos da carga antes de lutar. Meta de desenho: o jogador escolhe lutar em 30–70% das salas (§28, H1).

**Portas:** mostram ícones de ameaça e de recompensa antes da escolha; algumas só abrem para certa carga (a carga também é chave).

**Cargas mudam um verbo** (refinam a lista "Exemplos de carga" acima; os nomes antigos continuam valendo):

| Carga | Verbo que muda | Falha |
|---|---|---|
| Relíquia frágil | cada golpe na carga deixa uma rachadura visível; 3 rachaduras = quebra | entrega com bônus menor ou contrato falho |
| Ovo | choca se o contrato passar do tempo-alvo; o filhote passa a seguir você e também tem de chegar | filhote perdido |
| Coração arcano | ímã de moedas **e** de inimigos num raio visível | horda maior |
| Bolo de casamento | proíbe o dash (balança) | — |
| Mensagem urgente | cronômetro, mas abre portas trancadas sozinha | tempo esgotado |
| Caixa pesada | anda mais devagar, paga mais | — |
| Barril de pólvora (v0.2) | se ficar no chão, explode após 3 s; pode ser arremessado para salvar a vida, mas o contrato falha | explosão |

**Carga perdida fica na masmorra:** ao morrer, a carga fica na sala onde o courier caiu. O próximo contrato na mesma região oferece um resgate opcional: entregar as duas paga bônus. Só 1 carga pendente por vez; expira depois de 1 contrato.

**Interrupção:** se o app for para segundo plano, o contrato pausa. A vida e a integridade da carga são gravadas a cada dano, então reabrir nunca restaura dano já sofrido.

**Feedback:** som e haptic distintos para pousar, pegar, carga atingida e ladrão pegando a carga; contorno da carga piscando quando ela está no chão, longe do courier.
**Teste:** §28 (H1–H3).

# 3. PRIMEIROS 10 MINUTOS

**0:00–0:30** — contrato simples: “Leve esta caixa para a Sala 3”. Joystick e auto-attack entram imediatamente.

**0:30–1:30** — sala 1 ensina movimento e inimigo melee lento.

**1:30–2:30** — primeira bifurcação: rota curta com inimigos ou longa com baú.

**2:30–3:30** — sala 2 ensina dash por telegraph.

**3:30–4:30** — entrega. Cliente paga; animação curta e clara.

**4:30–5:30** — guilda abre primeiro upgrade: bolsa de loot ou dash cooldown lateral.

**5:30–6:30** — segundo contrato com carga frágil; o jogador entende que a carga muda o run.

**6:30–7:30** — primeira oferta de rewarded após falha: “proteja a carga e retome da sala atual”. Pode recusar.

**7:30–8:30** — primeira sala secreta opcional.

**8:30–9:30** — entrega com bônus por integridade.

**9:30–10:00** — quadro mostra contrato raro para o próximo retorno.

### FTUE revisada (2026-10-09): esta é a que vale

A sequência acima é a original. A revisada ensina "pousar para lutar" antes da 1ª bifurcação e tira o rewarded da 1ª sessão (a v0.1 não tem anúncio).

- **0:00–0:30:** contrato: "Leve esta caixa para a Sala 3". Arrastar move o courier com a caixa nas mãos.
- **0:30–1:30:** sala 1: um guarda melee lento bloqueia a porta. O jogo mostra "solte o dedo para pousar a caixa e lutar". Vitória fácil; voltar a arrastar pega a caixa.
- **1:30–2:30:** 1ª bifurcação: porta curta com ícone de ameaça ou porta longa com ícone de baú.
- **2:30–3:30:** sala 2: o 1º ladrão corre para a caixa pousada. O jogador aprende a lutar perto da carga ou a levá-la consigo; o dash entra aqui, com telegraph.
- **3:30–4:30:** entrega: o cliente abre a caixa e reage à integridade (cena de 3–4 s, pulável).
- **4:30–5:30:** a guilda abre o 1º upgrade (bolsa de loot ou cooldown do dash).
- **5:30–6:30:** 2º contrato, com o ovo: o jogador entende que a carga muda o que ele faz.
- **6:30–7:30:** sala com guarda e ladrão juntos; 1ª derrota provável. Se morrer, a carga fica na sala, e o próximo contrato oferece o resgate. Na v0.2, a oferta de rewarded "proteja a carga e retome da sala atual" volta aqui (pode recusar).
- **7:30–8:30:** 1ª sala secreta opcional.
- **8:30–9:30:** entrega com bônus por integridade.
- **9:30–10:00:** o quadro mostra o contrato raro do próximo retorno.

# 4. PRIMEIRO DIA

- 8 contratos;
- 12 salas modulares;
- 5 tipos de carga;
- 6 inimigos;
- 1 miniboss;
- 1 boss;
- 8 perks;
- 3 equipamentos base;
- 1 daily contract.

Primeiro rewarded: continue/recover package depois de uma falha real.
Primeira compra: Remove Ads / Courier Starter Pack depois de 3 contratos.

Razões para voltar:
- daily rare contract;
- novo tipo de carga;
- guild upgrade;
- blueprint.

# 5. PROGRESSÃO

Curta: contrato e loot.  
Média: reputação da guilda, perks, equipamento.  
Longa: regiões/dungeons, contratos especiais e mastery.

- D1: 8 contratos.
- D3: região 2.
- D7: boss contract.
- D14: modifiers.
- D30: season board leve.
- D60: novas cargas.
- D90: endless courier challenge.

Revisão 2026-10-09: o resgate da carga perdida (§2) liga a derrota ao contrato seguinte, sem custo de conteúdo.

# 6. ECONOMIA

**Coins:** soft currency.
Fontes:
- contrato;
- bônus por integridade;
- loot;
- daily;
- rewarded.

Sinks:
- upgrades;
- rerolls limitados;
- equipamento lateral;
- guilda.

**Stamps:** hard currency.
Usos:
- cosmetics;
- bundles;
- event retries.

**Reputation:** não comprável; desbloqueia contratos.

### Ajuste 2026-10-09 (guardrails do portfólio)
- **1 moeda no MVP: Coins.** Stamps (hard currency) saem do MVP; voltam na v1.0 só se a coorte justificar, e nunca compram item aleatório. **Reputation** continua: não é moeda, não é comprável e só desbloqueia contratos.
- **Mapa de fontes e ralos (MVP):**

| Fonte | Controle | Ralo |
|---|---|---|
| Pagamento do contrato + bônus por integridade | limitado pela duração do contrato (4–6 min) | upgrades da guilda (preço crescente, teto de 5 níveis por linha) |
| Loot opcional (baús de rota) | fixo por sala, visível no ícone da porta | equipamento lateral |
| Resgate de carga perdida | 1 pendente por vez; bônus uma vez por id de carga | rerolls limitados de contrato |
| Contrato do dia | 1 por dia, id `daily-AAAAMMDD` | — |
| Rewarded (v0.2) | id de transação por contrato e tipo | — |

- **Retorno decrescente:** nenhum upgrade torna a carga indestrutível ou o courier imune; a meta abre opções (perks, rotas), não poder infinito.
- **Relógio do aparelho:** a data local libera o contrato do dia, o baú do dia, os rerolls, o teto do sem fim, a vaga do evento da semana e a temporada; voltar o relógio não libera de novo (o jogo guarda a última data vista). O contrato raro conta as 8 h pelo instante UTC e também ignora o relógio voltando. Custo aceito: relógio adiantado por engano trava esses sistemas até a data real alcançar a gravada.
- **Offline:** não se aplica (não há renda passiva).

# 7. MONETIZAÇÃO

Rewarded:
- resume 1x/contract;
- double contract bonus;
- reveal safer route;
- daily chest.

Interstitial:
- entre contratos, cap forte;
- nunca dentro de dungeon.

IAP:
- Remove Ads;
- Starter Pack;
- cosmetic courier outfits;
- stamps;
- contract bundle/event.

Não vender “reputation” diretamente.

### Ajuste 2026-10-09 (guardrails e posicionamento)
- **v0.1:** sem anúncio e sem IAP. **Interstitial desligado** na v0.1; se entrar depois, só entre contratos, nunca nos 3 primeiros do dia, com cap forte.
- **Rewarded (v0.2):** resume 1×/contrato, dobrar o bônus, revelar a rota mais segura. Cada concessão tem id de transação (`rw-<contratoId>-<tipo>`) e não se repete ao fechar e reabrir o app.
- **O "daily chest" vira baú do dia com conteúdo visível antes de abrir.** Nenhum item aleatório pago (ECA Digital, Lei 15.211/2025).
- **IAP no MVP:** só Remove Ads e Starter Pack de conteúdo fixo e listado. Roupas do courier, Stamps e pacotes de evento ficam para a v1.0, sempre com conteúdo fixo.
- **Posicionamento:** "sem energia, sem gacha" na página da loja (§27, C7).

# 8. RETENÇÃO

D1: quadro com contrato raro.  
D3: nova região.  
D7: boss contract.  
D14: mastery/reputation milestone.  
D30: event board.

Daily contracts reutilizam salas com:
- carga;
- rota;
- modificadores.

# 9. LIVE OPS

- Fragile Week;
- Midnight Delivery;
- No-Damage Contract;
- Treasure Detour;
- Boss Courier Run.

Baixo custo porque conteúdo nasce de recombinação de salas/cargas.

# 10. CONTEÚDO

MVP:
- 6 salas;
- 3 cargas;
- 4 inimigos;
- 1 boss;
- 4 perks.

Lançamento:
- 4 regiões;
- 30–40 salas modulares;
- 15 tipos de carga;
- 20 inimigos;
- 8 bosses;
- 25 perks;
- 5 event templates.

# 11. ARTE

Stylized fantasy/comic, leitura alta.
Câmera top-down/3D oblíqua.
Cargas precisam ser visíveis no personagem/mochila para reforçar fantasy.

**PROPOSTA 2026-10-09 (decisão do Vinicius):** greybox em 2D top-down com arte procedural, como no Forge Street, e courier como **mascote sem rig humano** (um mímico que anda ou um goblin de mochila enorme, animado por squash and stretch), com a carga visível em cima dele. Corta o rig humanoide (Mixamo) e os inimigos rigados, o maior custo apontado pela raia C. Testar mascote × courier humano em criativo antes de decidir (§27, C5). O 3D oblíquo só entra se a tese validar.

**DECISÃO 2026-10-09 (Vinicius):** o courier é **humano**, e a arte vem do **Tripo com créditos** (lote só com aval do custo), no pipeline do Forge Street: Tripo → Mixamo → Blender (`client/tools/render_sprites.py`) → sprites 2D com câmera a 60°. A proposta de mascote acima e o C5 do §27 ficam como histórico. Detalhe em [`ART_BIBLE.md`](ART_BIBLE.md) e [`ASSETS.md`](ASSETS.md).

# 12. ÁUDIO

- footsteps;
- attack;
- impact;
- dash;
- package damage;
- delivery;
- door;
- secret room;
- reward.

Haptic na carga sofrendo dano e na entrega perfeita.

# 13. TECNOLOGIA

Unity/C#.

Módulos:
- `ContractDefinition`
- `DungeonRoom`
- `RouteGraph`
- `CargoModifier`
- `CombatController`
- `AutoTargeting`
- `EnemyDirector`
- `Extraction/Delivery`
- `GuildMeta`
- MobileCore.

Reuso ARKANA:
- input;
- projectile;
- AI/bots;
- pooling;
- camera/combat feedback.

### Reuso real (verificado pela raia C em 2026-10-06)
O GDD contava "combat framework, camera, AI" do ARKANA; isso não existe em forma reaproveitável, e o reuso estava contado duas vezes no ranking (Reuso 10 e Facilidade 7; a raia D corrigiu para 7 e 6). O que serve:
- `UI/JoystickVirtual.cs` (`JoystickLogica`; depende de `Arkana.Core`: desacoplar) e `Gameplay/Projetil.cs` (depende de `Elemento`/`ArmaSpec`);
- o pool em `Gameplay/VisualDoAbate.cs`, mais `BancadaDeCortes.cs` e `MedidorDeFps.cs`;
- do COE: `Health.cs`, `Damage.cs`, `Hitbox.cs` (padrão de combate) e a arquitetura de save;
- se o courier continuar humanoide: o pipeline do COE (`HumanoidSetup.cs`, `HumanoidMapping.cs`, `PrototipoAnimacoes.cs`) e a lição Tripo→Mixamo.

**Não serve:** `Gameplay/Bot.cs`. **Precisa ser escrito:** `RouteGraph`, salas por kit, `CargoModifier`, a regra de carregar/pousar e a simulação headless do contrato em passo fixo (bot de balance e semente do contrato por código). O gargalo é conteúdo (salas), não código (raia C).

# 14. ANALYTICS

- contract_start/end
- cargo_type
- room_enter
- route_choice
- damage_taken
- cargo_integrity
- optional_loot
- death_room
- rewarded_resume
- delivery_quality
- guild_upgrade
- currency_source/sink
- iap_purchase
- device_fps_bucket

Métricas:
- contract completion;
- route diversity;
- second-contract start;
- D1/D7;
- average session;
- rewarded opt-in;
- CPI.

Eventos acrescentados em 2026-10-09: `cargo_drop`, `cargo_pickup`, `thief_grab`, `room_fight_choice` (lutou ou passou), `cargo_rescue_offer`, `cargo_rescue_result`, `seed_code_entered`.

# 15. KPIs — TARGETS INTERNOS

- tutorial ≥88%;
- first contract completion ≥85%;
- second contract same session ≥55%;
- D1 ≥26% (era 27%; alinhado ao veredito de 2026-10-06);
- D3 ≥12% (acompanhamento, não é gate);
- D7 ≥5% (era 6,5%; o top quartil do GameAnalytics 2026 fica em 6–7%);
- D30 ≥1,5% (acompanhamento);
- rewarded opt-in ≥30%;
- 3+ contracts/day entre retidos;
- crash-free >99.5%.
- **Gate de receita na fase B (IAA, não payer):** opt-in de rewarded, impressões de rewarded por DAU e ARPDAU. Payer conversion só a partir de 5 mil installs.
- **Coorte:** 1,5–2 mil installs pagos em geo barata (BR/PH/ID), sempre comparando pago com pago.

UA low-cost action:
- CPI ≤ ~US$0,95 promissor;
- 0,95–1,50 iterar;
- >1,50 exige retenção/LTV superior.

# 16. TESTE DE MERCADO

Creative-first:
- “fragile package”;
- “safe vs risky door”;
- “1 HP delivery”.

1.500–2.000 installs (revisado em 2026-10-09: com 1 mil installs, a margem do D7 é de ±1,7 pp).
Comparar:
- ação pura;
- decisão/rota;
- humor de courier;
- carregar ocupa as mãos (pousar para lutar, §2);
- mascote × courier humano (§11).

# 17. 10 CRIATIVOS UA

1. pacote frágil em dungeon cheia de espinhos;
2. duas portas: “safe” / “treasure”;
3. entrega com 1% de vida;
4. carga que atrai monstros;
5. jogador ganancioso pega baú e morre;
6. rota curta lotada vs longa segura;
7. “não use explosão perto do ovo”;
8. dash perfeito salvando carga;
9. cliente reage a pacote destruído vs perfeito;
10. contrato simples → dungeon caótica em 15 s.

Acrescentados em 2026-10-09 (§27):
11. goblin ladrão corre para a caixa pousada → o courier larga a luta e mergulha de dash;
12. “não deixe o ovo chocar”: cronômetro + filhote seguindo o courier;
13. o mesmo vídeo com mascote mímico × courier humano.

# 18. MVP

- movement/auto-attack;
- 6 salas;
- route graph;
- 3 cargos;
- regra de carregar/pousar e cargas que mudam verbo (§2);
- carga perdida com resgate (§2);
- 4 enemies;
- 1 boss;
- analytics;
- Android;
- 8 creatives.

Sem:
- online;
- guild social;
- procedural infinito;
- battle pass.

# 19. V0.1

1 região + 8 contratos, já com carregar/pousar (§2). Sem anúncio e sem IAP.

# 20. V0.2

Se CPI/D1:
- 2 regiões;
- 8 cargos;
- meta;
- ads/IAP;
- remote config;
- daily contracts;
- contrato por código (§27, C6).

# 21. V1.0

Se D7/LTV:
- 4 regiões;
- 15 cargos;
- events;
- iOS;
- localization.

# 22. PRODUÇÃO

| Área | Peso |
|---|---|
| Programação | Médio |
| Design | Médio–Alto |
| Arte | Médio |
| UI | Médio |
| Áudio | Médio |
| Backend | Baixo |
| Analytics | Médio |
| Conteúdo | Médio |

Complexidade: **6/10**.

**Nota 2026-10-09:** a estimativa da raia C (6–7 semanas até o APK, com gargalo em salas e rig humanoide) deve cair com o greybox 2D e o mascote sem rig (§11). Recalibrar com a velocidade real da semana 1; não tratar como data.

# 23. REUTILIZAÇÃO

ARKANA:
- combat framework;
- mobile controls;
- camera;
- projectiles;
- AI;
- performance tooling.

COE:
- quest/contract patterns;
- save architecture.

MobileCore.

**Correção 2026-10-09:** ver "Reuso real" em §13. Do ARKANA servem joystick, projétil, pool e a bancada de FPS; "combat framework", câmera e IA precisam ser escritos.

# 24. IA

- modular room ideation;
- contract generation assistida offline;
- balance simulation;
- temporary art;
- QA bots;
- creative variations;
- localization.

Sem IA runtime no MVP.

# 25. RISCOS

**Action parece genérico.**  
Carga precisa mudar gameplay em toda run.

**Rotas não importam.**  
Recompensa/risco precisam ser materialmente diferentes.

**Conteúdo modular fica repetitivo.**  
Cargas + modifiers + room sets.

**Auto-attack reduz habilidade.**  
Posicionamento, dash e rota carregam skill.

**Missão de escolta irrita (2026-10-09).**
Carregar e proteger é famoso por frustrar. Mitigação: a carga nunca anda sozinha, ladrões são lentos e visíveis, e a carga roubada cai quando o ladrão é atingido (§2).

**O tema "entrega" pode ser lido como idle-arcade.**
O Pizza Ready ocupa a fantasia de entregar no hybrid-casual. O anúncio precisa mostrar carga e combate nos primeiros 3 s.

**Tema ocupado no PC.**
O Delivery Survivors (Steam, 4º tri/2026) tem guilda de entregas e rota aprendida com o courier caído. O verbo dele é "matar tudo"; o nosso é "carregar". Se ele chegar ao mobile, a diferença tem de aparecer no vídeo.

# 26. KILL CRITERIA

> Revisado em 2026-10-09 para o padrão do veredito: coorte de 1,5–2 mil installs e cancelamento só após 2 iterações estruturais. Valores anteriores: continuar com D1 ≥27% e D7 ≥6,5%; iterar com D1 21–27% e D7 4–6,5%; cancelar com D1 <21% e D7 <4%.

## CONTINUAR
- first contract ≥85%;
- second contract ≥55%;
- D1 ≥26%;
- D7 ≥5%;
- CPI dentro do target (≤ ~US$0,95);
- “carga/entrega” aparece espontaneamente em feedback como diferencial;
- o jogador escolhe lutar em 30–70% das salas (§28, H1).

## ITERAR
- D1 20–26%;
- D7 3,5–5%;
- CPI até 50% acima;
- combate bom, mas carga irrelevante;
- rota não produz decisões;
- luta escolhida em menos de 30% (virou runner) ou mais de 70% das salas (virou Archero).

## CANCELAR (só após 2 iterações, cada uma com coorte de 1,5–2 mil installs)
- D1 <20% (mediana do GameAnalytics 2026);
- D7 <3,5%;
- CPI >2× o target em 8–10 criativos;
- jogadores descrevem como “Archero genérico”;
- custo de salas/contratos impede cadência.

# 27. DIFERENCIAIS COMPETITIVOS (revisão 2026-10-09)

Resumo. O detalhe (por que importa, como funciona, custo, risco e validação) e as fontes estão em [`COMPETITIVO.md`](COMPETITIVO.md). Tudo é **PROPOSTA**. Custo: P ≤ 1 semana, M 2–4 semanas, G ≥ 1 mês.

| # | Diferencial | O que nenhum similar pesquisado tem | Custo | Quando |
|---|---|---|---|---|
| C1 | **Carregar ocupa as mãos** | pousar para lutar; ladrões vão atrás da carga no chão (§2) | M | **1º**, no MVP |
| C2 | **Cada carga muda um verbo** | ovo que choca, bolo que proíbe o dash, carga que abre porta (§2) | P por carga | **2º**, no MVP (3 cargas) |
| C4 | **A carga perdida fica na masmorra** | resgate no contrato seguinte (§2) | P | **3º**, no MVP |
| C3 | O cliente abre a caixa | entrega como recompensa cômica, reação pela integridade | P–M | já no teste de criativos |
| C5 | Mascote sem rig humano | mímico ou goblin de mochila; corta o maior custo (§11) | P (economiza) | criativo A/B antes do código |
| C6 | Contrato por código | semente de 6 caracteres, sem backend | P–M | v0.2 |
| C7 | Sem energia, sem gacha | justiça como promessa na loja (§7) | P | já no teste de criativos |

**Por que estes três primeiro:** C1 tira o jogo do kill criterion "Archero genérico" sem virar runner; C2 transforma carga em conteúdo barato (dados + 1 comportamento) e dá o gancho de 3 s; C4 custa P e ataca direto o KPI de 2º contrato.

**Posicionamento (rascunho):** o jogo de masmorra em que a carga ocupa as suas mãos: você escolhe quando pousar para lutar, e o cliente abre a caixa no final.

# 28. HIPÓTESES DE PROTÓTIPO E CATÁLOGO DE EXPLOITS (2026-10-09)

Cada hipótese tem métrica, protocolo e critério de refutação. Nenhuma está validada.

| # | Hipótese | Protocolo | Refutada se |
|---|---|---|---|
| H1 | "Pousar para lutar" faz da luta uma escolha | greybox 2D, playtest com 5–8 pessoas, 2 sessões cada; `room_fight_choice` | luta escolhida em menos de 30% ou mais de 70% das salas, ou a maioria descreve o combate como "obrigatório" ou "inútil" |
| H2 | A carga muda a rota | telemetria de porta escolhida por carga | a mesma porta é escolhida em mais de 80% das vezes, qualquer que seja a carga |
| H3 | O resgate puxa o 2º contrato | início do 2º contrato após derrota, com e sem oferta (playtest; remote config na v0.2) | o início do 2º contrato não sobe em relação ao grupo sem oferta |
| H4 | O hook "carga + combate" baixa o CPI frente ao Archero genérico | lote de 8–10 criativos, mesmos geos e orçamento | CPI do hook ≥ CPI do controle |
| H5 | Mascote converte tanto quanto courier humano | 2 criativos idênticos, só muda o personagem | decide a arte; não refuta o jogo |

**Catálogo de exploits (cada um vira teste nomeado):**
- **Fechar o app logo após a carga ser atingida** para restaurar a integridade → vida e integridade gravadas a cada dano; reabrir retoma com os valores atuais.
- **Morrer de propósito para gerar resgate** → o bônus de resgate é menor que o pagamento perdido; 1 pendente por vez; bônus uma vez por id de carga.
- **Rewarded repetido** (fechar e reabrir na tela de recompensa) → id de transação por contrato e tipo.
- **Rerolar o contrato até sair a carga fácil** → rerolls limitados e pagos em Coins.
- **Voltar o relógio** para repetir o contrato do dia → o jogo guarda a última data vista.
- **Deixar a carga na entrada e limpar a sala longe dela** → é estratégia válida, mas ladrões nascem perto da carga se ela ficar sozinha por mais de 8 s.
- **Save antigo ou de versão futura** → migração explícita v0/v1/v2→v3; versão futura é recusada sem apagar o save.
