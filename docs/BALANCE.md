# Balance — medição do bot

# Revisão de combate (2026-10-21, feedback do Vinicius no celular)

Queixas: o Bruto preso na quina da sala e o Guarda parado ("extremamente fácil matar ele"). Análise de toda a lógica de ataque e defesa nas 4 regiões; o que mudou:

- **Nav por tamanho de corpo** (`Run.NavFor`, `Nav(room, raio)`): corpo maior que 0,45 m usa uma grade em que a célula só é livre se o corpo inteiro cabe, contando a parede. Antes o Bruto (1,1 m) ia pela fresta de 1 m da R5 e travava na quina; também R11, R2-10, R4-10 e os chefes B1, B2 e B4. Teste: `AndadorGrande_ChegaAoCourierEmTodoCanto` (todas as salas).
- **Guarda sentinela** (`Run.Guard`): levou tiro, fica alerta 2,5 s e anda até 3 m do posto atrás do courier. Fora dos tutoriais, com o courier parado a 1,3–5 m, mira 1,1 s e investe 4,5 m (1 de dano e empurrão). Errou ou bateu, fica atordoado 1,2 s (tiro livre). Recarga de 7 s; depois volta ao posto. Os golpes dele são de escudo: tiram vida, mas não racham a carga na mão. Quem passa andando ou luta com os outros perto dele encara o guarda no posto, como antes; depois que a mensagem abre a porta, ele deixa passar.
- **Toques não empilham** (`Balance.TouchGuard` 0,5 s): 4 morcegos juntos tiravam 4 de vida no mesmo passo.
- **Mira automática**: quem foge com a carga é o 1º alvo (antes o morcego mais perto roubava a mira).
- **Regiões 2-4**: o dash cancela o aviso do Mineiro e do Sapo. Mineiro e Kobold arremessam antes de fugir (colado deixou de ser seguro). Língua do Sapo no courier vai a 4,4 m (era 3: o tiro de 5 m matava de graça). Raios gêmeos do Necromante a ±7° (a ±15° a fase 2 errava quem ficava parado). Gárgula 6 de vida, 3 s pousada, e Gárgula e Toupeira sem escala de vida (só vulneráveis em janelas curtas). Salamandra para e encara 0,35 s antes do corte (era sem aviso) e o corte segue a isca do coração. Aranha avisa em 0,5 s; Diabrete, em 0,7 s. Dragão com 42 de vida (39% de morte do bot com 48). A Bota de ferro não congela mais o vagonete.
- **Testado e descartado:** investida a cada 2,5–5 s, alerta por proximidade (courier parado perto da porta) e seguir quem passa andando. Com a relíquia na mão, cada toque de morcego ou limo durante o desvio racha a carga: ela quebrava em 80% do C5 e do C8 e em 89% do C21. Medido por rachadura, os golpes no courier com a carga na mão somavam 296, contra 20 do ladrão. Com o guarda parado, o C21 voltava a 52%, o que confirmou que a causa era o desvio forçado.

Bot, 6 guildas × 20 sementes (`Bot.MeasureCampaignTable`), % de entregas antes → depois:

- Relíquia: C5 59 → 67, C8 71 → 59, C15 67 → 53, C21 46 → 43; a relíquia quebrada sobe um pouco, e as mortes caem.
- Mensagem: C10 98 → 98, C16 80 → 95, C22 98 → 99.
- Ícones refeitos pelo dano medido:
  - Região 1: R8 3 → 2 (o intervalo entre toques aliviou os morcegos), R12 1 → 2 (guarda sentinela). A R15 com 3 caveiras o bot evitava, e o dano medido nela caía para 1,06; ficou com 2.
  - Região 2: R2-5 1 → 2 (a língua do sapo no courier foi a 4,4 m; o dano por visita passou de 0,52 para 0,98).

# Economia da campanha (Leva C, raia E, 2026-10-11)

As raias R2, R3 e R4 mediram cada região partindo de uma **estimativa** do fim da anterior e todas acharam o mesmo problema: moedas paradas (~3,7 mil no fim da região 2, ~14 mil na 3, ~21,6 mil na 4) contra o alvo do desenho de "no máximo 1 nível de equipamento sobrando" (`CONTEUDO_REGIOES.md` §10.3). Aqui a campanha inteira é medida **numa rodada só**, do zero.

## Como medir

- `EconomiaTests` (`Tests/EditMode/CoreTestsEconomia.cs`): **16 campanhas do bot** (sementes 8000–8015) de **C1 a M4-16 numa rodada só, do zero** (`Bot.PlayCampaign(semente, region: 4)`), comprando como sempre: antes de cada contrato o mais barato que couber (guilda, nível de arma ou de equipamento destravado; `Bot.BuyAll`) e equipa o 1º equipamento destravado. Reproduzir: `dotnet test client/tools/coretests --filter EconomiaTests --logger "console;verbosity=detailed"` (~35 s; roda na suíte normal, não é `[Explicit]`).
- **Paradas no fim** = moedas na entrada da região seguinte, depois das compras (na região 4: o fim da campanha mais uma rodada de compras). **Pico guardado** = maior saldo antes de um contrato da região. **Sem destino** = maior saldo guardado quando **não havia nada para comprar** (guilda, armas e os 4 equipamentos no máximo ou bloqueados): a moeda parada de verdade. Médias das 16 campanhas.
- **Duração da região 2:** cada contrato medido a partir do save que cada campanha completa tinha antes da 1ª tentativa dele (16 × 5 = 80 tentativas por contrato), como a tabela realista da v1.
- Robustez: a mesma medição com 24 campanhas (9100–9123) e com 40 (12000–12039) passa nos mesmos limites; 80 de 80 completam os 4 equipamentos.

## Antes e depois (16 campanhas, C1 → M4-16 do zero)

| Região | ganhas | gastas | paradas no fim (média / máx) | pico guardado | sem destino | o que dá para comprar |
|---|---|---|---|---|---|---|
| 1 (antes = depois) | 10.466 | 9.304 | 576 / 1.020 | 702 | 0 | guilda 7/7/7/7, armas 3/3/2 |
| 2 antes | 15.573 | 13.236 | **3.499** / 5.136 | 3.564 | **3.499** | guilda 8, armas 3, Botas 5: acaba tudo |
| 2 depois | 15.350 | 13.936 | **2.264** / 4.164 | 3.714 | **73** | Botas 4 no fim (o 5 sai na região 3) |
| 3 antes | 18.416 | 6.800 | **15.115** / 16.967 | 15.155 | **15.155** | só a Fumaça |
| 3 depois | 18.348 | 17.188 | **2.362** / 4.764 | 4.489 | **676** | Fumaça 4–5 e Gancho 4 |
| 4 antes | 22.216 | 13.600 | **23.731** / 26.277 | 23.731 | **23.731** | Gancho e Armadilha (13.600) |
| 4 depois | 22.342 | 23.438 | **2.641** / 4.892 | 4.475 | **1.093** | resto do Gancho e a Armadilha; no fim os 4 no nível 5 ou com 1 nível por comprar |

| Campanha inteira | antes | depois |
|---|---|---|
| fecha os 72 | 100% | 100% |
| tentativas / mortes | 86,6 / 6,9 | 86,3 / 6,7 |
| tempo de jogo do bot | 112,8 min | 113,0 min |
| estrelas | 192,2 de 216 | 192,4 de 216 |
| sobra no fim (média) | **23.731** | **2.641** (máx. 4.892, abaixo do nível 5 de equipamento, 5.000) |

O teste `CampanhaCompleta_C1aM4_16_PeloBot_FechaESobraNoMaximo1NivelDeEquipamento` exige, por região: fecha os 72, paradas no fim ≤ 3.000 (média), sem destino ≤ 3.000 e pico ≤ 5.000 (o preço do nível 5). Com o desenho anterior ele falha já na região 2 (3.499).

## Duração da região 2 (alvo do bot: 90–180 s do 4º contrato em diante, sem a mensagem)

| Contrato | antes: salas · % entrega · média (s) | depois: salas · % entrega · média (s) |
|---|---|---|
| M2-4 Relíquia | 6 · 68 · 56 | 6 · 66 · 56 (sem mudança) |
| M2-5 Coração | 6 · 96 · 57 | **7** · 95 · 78 |
| M2-6 Barril | 7 · 76 · 86 | 7 · 80 · 88 |
| M2-7 Cristal | 6 · 89 · 88 | **7** · 91 · 116 |
| M2-8 Rainha | 7 · 99 · 103 | 7 · 100 · 98 |
| M2-10 Gaiola | 6 · 95 · 68 | **7** · 99 · 98 |
| M2-11 Coração | 7 · 93 · 75 | 7 · 93 · 74 |
| M2-12 Bolo | 7 · 94 · 88 | 7 · 93 · 90 |
| M2-13 Caixa pesada | 7 · 98 · 87 | 7 · 95 · 92 |
| M2-14 Gaiola | 7 · 95 · 74 | 7 · 90 · 102 |
| M2-15 Cristal | 7 · 88 · 112 | 7 · 80 · 109 |
| M2-16 Golem | 7 · 78 · 128 | 7 · 80 · 131 |
| **média** | **85 s** | **94 s** |

Teste: `Regiao2_DoQuartoEmDiante_Media90a180s_SemQuebrarAEntrega` (média em 90–180 s e cada contrato entregue em ≥ 40%; falha com o desenho anterior, 85 s). A região 2 continua fechando 100% (17,8 → 17,3 tentativas na campanha completa). Na medição da raia R2 (partida estimada, `BalanceTestsRegiao2`) a média vai de 84 s para 92 s e o H1 fica em 58 / 70 / 83% (agressividade 0,1 / 0,5 / 0,9; antes 55 / 69 / 82%): 70% é a borda de cima do alvo.

## Decisões (com o porquê)

- **E1. Equipamento custa 1.000 / 2.500 / 4.000 / 5.000 por nível (12.500 por peça; era 600 / 1.200 / 2.000 / 3.000 = 6.800).** É o único ralo que cresce com as regiões (cada uma destrava peças) e o GDD §6.6 já o desenha como "depois sobe de nível com Coins". Os 4 equipamentos somam 50.000, perto do que as regiões 2–4 pagam acima da guilda e das armas (~49 mil medidos). Descartados: **níveis extras de guilda** (mais vida e dash mudam o combate que as raias R2–R4 mediram, a Bolsa extra devolve +10% de moeda por nível e a View desenha 8 bolinhas por `Balance.UpgradeMax`); **roupas por moeda** (as roupas são IAP: vender a mesma roupa por moeda esvazia a loja); **rerolls** (o bot não rerola e o reroll custa 60–120 por dia: não drena 20 mil).
- **E2. O Gancho completa na região 3** (o 3º projeto saiu da R4-4 para a R3-5, Sarcófagos). Com os preços de E1 a região 3 só tinha a Fumaça para comprar e ficava com ~4–5 mil sem destino até o Gancho abrir na região 4; agora o saldo dela vai para o Gancho e a região 4 fica com o resto dele e a Armadilha. De jogo, o gancho chega quando a carga começa a ser levada (o fantasma possui, a Viúva puxa pelo fio).
- **E3. Cada equipamento tem 4 baús de projeto para 3 projetos** (reserva: Botas + R2-4, Fumaça + R3-6, Gancho na R4-1, Armadilha + R4-4). Com 3 baús exatos, 2 de 16 campanhas (e 1 de 24 noutra rodada) pularam um baú (sala secreta ou rota) e ficaram com **12–14 mil moedas paradas** até o fim, porque o equipamento nunca abria. A porta e o baú só mostram o projeto **enquanto o equipamento precisa dele** (`Run.ProjectAt`); depois o baú paga só as moedas (o mesmo que o projeto da semana já fazia com os 300). Com a reserva, 80 de 80 campanhas completam os 4.
- **E4. Pagamento e baús das regiões 2–4 não mudaram.** Com E1–E3 a sobra já fica abaixo de 1 nível; as tabelas de pagamento e os tempos das raias R2–R4 continuam valendo. Fica como alavanca se o playtest mostrar sobra (ver Riscos).
- **E5. Região 2 mais longa com camada, não com vida** (a raia R2 já tinha visto que nível +3/+6 mexe só 2–5 s): M2-5, M2-7 e M2-10 ganham a 6ª camada (R2-9/R2-10, R2-10/R2-8, R2-10/R2-8: emboscada ou Guarda) e o M2-14 troca a 5ª camada R2-8/R2-1 por R2-8/R2-10 (o bot vai para a Eclusa do Guarda, 27 s, em vez da Galeria Alagada, 4 s). Tempo-alvo pela mediana nova (1,6×; gelo 2,2×): M2-7 155 → 190 s e gelo 210 → 265 s, M2-10 140 → 160 s; M2-5 e M2-14 já tinham folga. **M2-4 (relíquia) ficou igual:** com a 6ª camada (R2-8/R2-9) entregava 10% (o Guarda racha a relíquia). Pagamento igual (a raia R3 fez o mesmo ao alongar contratos).

## Fim de jogo: níveis 6–8 do equipamento (raia E2, 2026-10-11)

Decisão do coordenador: o ralo do pós-campanha são os **níveis 6, 7 e 8** do equipamento lateral, abertos ao **vencer o Dragão** (M4-16 entregue; `sim.DragonBeaten`, `sim.GearCap` = 5 antes e 8 depois; `Balance.GearCampaignLevel` = 5 e `Balance.GearMaxLevel` = 8). O efeito extra do nível 5 (gancho tira a carga da mão do ladrão, armadilha prende mini-chefe, botas com fogo a cada 2 s) continua no 5.

**Como medir:** `EconomiaTests.FimDeJogo_Niveis6a8DepoisDoDragao_DuramEntreUmaEDuasTemporadas` pega o fim de 4 das campanhas completas (a mesma medição com 8 dá os mesmos números) e joga **56 dias** como um jogador diário: 1 sessão por dia (com 8 h fora o raro aparece todo dia), resgata marcos e temporada, abre o baú do dia (rewarded), joga o contrato do dia, o raro, as 3 vagas do evento da semana (até 2 tentativas por dia cada) e 1 desafio sem fim, e compra como a campanha (`Bot.BuyAll`). Sem reroll, sem modificador escolhido e sem o rewarded de dobrar o bônus. É o jogador mais assíduo possível: quem pula atividade leva mais dias.

| Moedas por dia (média de 8 jogadores) | dias 1–28 | dias 29–56 |
|---|---|---|
| contrato raro (×2) | 1.692 | 1.975 |
| contrato do dia (com o bônus e o modificador do dia) | 1.369 | 1.255 |
| sem fim (teto 800 antes da Bolsa) | 767 | 839 |
| eventos (3 vagas por semana + projeto ou 300) | 539 | 445 |
| temporada | 166 | 93 |
| baú do dia | 105 | 101 |
| marcos (os 2.000 da campanha, resgatados no 1º dia) | 71 | 0 |
| **total** | **4.709** | **4.708** |

| Custo dos níveis 6 / 7 / 8 | ralo (4 equipamentos) | dia em que o bot fecha os 4 no 8 |
|---|---|---|
| 6.000 / 8.000 / 10.000 (a faixa do coordenador) | 96.000 | **21–22** (3 semanas: menos que 1 temporada) |
| **10.000 / 13.000 / 18.000 (adotado)** | **164.000** | **35–36** (5 semanas: mais que 1 temporada, menos que 2) |

- **E6. Níveis 6–8 a 10.000 / 13.000 / 18.000** (aceito pelo coordenador em 2026-10-12) (41.000 por peça). Com a faixa sugerida (6/8/10 mil) o jogador diário fecha tudo em 3 semanas e volta a ficar sem ralo antes de terminar a 1ª temporada; com 10/13/18 mil ele leva 5 semanas, e o quadro de temporada (28 dias) termina antes do ralo. Até fechar, o saldo nunca passa do preço do nível mais caro (18.000): sempre há o que comprar. O teste exige o fechamento entre o dia 29 e o 56; com 6/8/10 mil ele falha (dia 21).
- **E7. Ganho pequeno por nível** (cada passo de 6 a 8 é no máximo o maior passo da campanha): fumaça 4,5 / 4,5 / 5 s e 3 / 4 / 4 usos; gancho com recarga de 5,5 / 5 / 4,5 s; armadilha 5,5 / 6 / 6 s e prende 2 / 2 / 3; botas com o fogo no pé a cada 2,25 / 2,5 / 3 s. Nada torna a carga indestrutível: com qualquer peça no 8 a relíquia ainda quebra com 3 golpes (`Equipamento_Niveis6a8_SoDepoisDoDragao_CustoCrescente_SemCargaIndestrutivel`).
- **E8. Reroll:** já fica mais caro a cada uso no mesmo dia (60 o 1º, 120 o 2º; `reroll_base`/`reroll_step` no `config.json`); só há 2 trocas por dia (o do dia e o raro), então ele é ralo pequeno e fica como está.
- Depois dos 4 no nível 8 (~5 semanas de jogo diário) **não há ralo de novo**: o bot chega ao dia 56 com ~100 mil moedas. O próximo ralo (cosmético por moeda, novos níveis com conteúdo novo) é decisão de produto quando a coorte mostrar quantos jogadores chegam lá.

## Riscos e pendências

- **O jogador ganha moedas que o bot não mede:** marcos (2.000 na campanha), temporada (~3.000 a cada 28 dias), eventos (650 por semana + projeto), baú do dia (80–120 por dia), Starter Pack (1.500) e o sem fim (até 800 por dia, só depois da campanha). Os projetos da temporada, do evento, do baú do dia e do Starter Pack também abrem os ralos **mais cedo** que no bot (bom), mas, com os 4 equipamentos no nível 5, não havia ralo nenhum: **resolvido pelos níveis 6–8 depois do Dragão** (seção "Fim de jogo" acima, E6–E8), que duram ~5 semanas de jogo diário.
- O bot equipa só o 1º equipamento destravado (as Botas): os níveis das outras peças não mudam o jogo dele. Com os preços novos ele chega à região 3 com as Botas no nível 4 (antes 5): teias neutralizadas (≥ 3) e a múmia com 1 golpe (≥ 4) continuam; o nível 5 (fogo a cada 2 s) chega antes da região 4.
- As partidas estimadas das raias R3 e R4 (`Bot.Regiao3StartSave`: 3.000 moedas e Botas 5; `Bot.Regiao3EndSave`: 13.900 moedas, Fumaça e Botas 5, Gancho com 2 projetos) ficaram velhas: a campanha completa entra na região 3 com ~2.300 moedas, Botas 4 e o Gancho por abrir, e na região 4 com ~2.400, Fumaça 4–5 e Gancho 4. Trocar pelo `Before` da campanha completa quando as raias refizerem as tabelas.

# Lançamento — núcleo, Leva A (2026-10-11)

Fonte: [`CONTRATO_LANCAMENTO.md`](historico/CONTRATO_LANCAMENTO.md) §2–§7 e, no que é do núcleo, [`CONTEUDO_REGIOES.md`](historico/CONTEUDO_REGIOES.md). Só o que mudou; o resto da v1 abaixo continua valendo. API no topo do `Sim.cs` (seção "LANCAMENTO"); números em `MetaDefs.cs` (`Balance` parcial, `GearDef`, `ModifierDef`, `EventDef`, `Season`, `Config`). **Todo número novo é PROVISÓRIO** até o bot das regiões 2–4 (Leva B) e o playtest medirem.

## Região 1: antes e depois

Nenhum número da região 1 mudou (salas, contratos, inimigos, cargas, perks sorteados). Os contratos gerados da região 1 (dia, código) são **idênticos aos da beta**: o teste `Codigo_RegiaoVemDoCodigo_Regiao1IgualABeta_*` compara a impressão digital de 5 payloads com a do código original. A única mudança que chega ao bot da campanha é a **maestria** (+2% de pagamento por nível na carga):

| Campanha do bot (100 sementes) | v1 (antes) | lançamento (depois) |
|---|---|---|
| fecha | 100% | 100% |
| tentativas | 31,8 | 31,8 |
| mortes | 4,6 | 4,5 |
| tempo de jogo do bot | 35,7 min | 35,7 min |
| moedas ganhas / gastas | 10.211 / 9.159 | 10.237 / 9.148 |
| estrelas | 62,6 de 72 | 62,7 de 72 |
| guilda no fim | 7,0/7,0/7,0/6,8 | 7,0/7,0/7,0/6,8 |

Tabela realista por contrato: C1–C11 e C13–C15 idênticas; C12 +4 moedas médias; C16–C24 com ruído de ±3 pontos (a guilda compra em outra ordem com +2–6% de pagamento). H1 (56/72/83%), H2 (28,8% de 139 bifurcações) e armas no C12 iguais. Os marcos de reputação **não** entram no bot: são resgatados pelo jogador (decisão L4).

## Números novos (provisórios)

| Sistema | Número |
|---|---|
| Regiões | destravam com 30 / 60 / 95 estrelas + o último contrato da anterior (o chefe); sem conteúdo ficam fechadas |
| Gerado por região (dia, código, raro, evento) | pagamento = base + 22 × camadas, base 40 / 110 / 165 / 220; por sala: 3ª estrela 18 / 20 / 22 / 22 s, ovo 23 / 25 / 28 / 28 s, gelo 27 / 27 / 30 / 30 s, mensagem 12 / 13 / 14 / 14 s; nível base 10 / 14 / 18 / 22 |
| Modificadores | abrem com 40 estrelas; 0–2 por contrato; Neblina +15% (luz 3,5 m), Pressa +20% (tempo-alvo ×0,7), Horda +25% (+1 perseguidor da região por sala e por reforço), Frágil +25% (qualquer carga quebra com 2 rachaduras; gelo falha abaixo de 40%), Pão-duro +20% (baús ×0,5); o do dia vem com 1 |
| Eventos | 1 por semana (segunda a domingo) em rodízio de 5; 3 vagas (+2 com o pacote): vaga 1 = região 1 com 4 camadas, 2 e 3 = região mais alta com 5 e 6; recompensa fixa 150 / 200 / 300 (+150 / 150 do pacote), 1× por vaga e semana; as 3 vagas = 1 projeto (sem projeto faltando: 300 moedas). Fragile Week ×1,5 · No-Damage ×3, vida 1, sem cura e sem perks de vida · Treasure baús ×2, paredes rachadas e baús enterrados abertos · Boss Run cura 2 por sala · sem resgate em evento |
| Temporada | 28 dias; 20 objetivos com recompensa fixa (100–300 moedas, 4 projetos, roupa Fantasma no último) |
| Contrato raro | depois de 8 h fora (`rare_hours`); ×2; 1× |
| Reroll | dia e raro, 1× por dia cada; 60 moedas, depois 120 no mesmo dia (`reroll_base`, `reroll_step`) |
| Equipamento | 3 projetos destravam; 5 níveis, 600 / 1.200 / 2.000 / 3.000 (**Leva C: 1.000 / 2.500 / 4.000 / 5.000, e 6–8 depois do Dragão a 10.000 / 13.000 / 18.000**, ver "Economia da campanha"); dispara sozinho (sem botão): fumaça 2,5 m, 3–4 s, 1–3 usos por contrato; gancho 1,5–5 m, cone de 30°, recarga 10→6 s; armadilha 3–5 s, 1–2 presas; botas passivas |
| Maestria | 3 / 8 / 15 / 25 / 40 entregas da carga; +2% por nível |
| Marcos | 10 / 30 / 60 / 95 / 150 estrelas: 50 / 150 / 300 / 500 / 1.000 moedas (+ roupa Fantasma no de 95) |
| Sem fim | abre com o último contrato da campanha; nível 10 + 2 por camada; mini-chefe a cada 5 camadas e chefe a cada 10; 15 moedas por camada, teto de 800 por dia |
| Perks novos | Mola ×1,4 · Segundo fôlego 2 de vida · Isolamento ovo ×1,2, gelo ÷0,6 · Caçador +2 · Couro grosso −1 em golpe de 2+ · Olhos de gato ×1,6 · Assobio 1,5 s · Primeiros socorros +1 · Corredor ×1,2; só no sorteio da região 2 em diante |
| Terreno comum | água ×0,75; dormindo acorda a 2 m (explosão 4 m, vizinho 3 m por 1 s), 1º golpe ×2; luz do courier 3,5 m (lanterna 5,5 nas mãos, 4,0 no chão), tocha 2,5; baú enterrado 1,5 s (Faro 1,0) |
| Monetização | Starter Pack 1.500 moedas + roupa Explorador + 1 projeto de cada; baú do dia 80–120 moedas (+1 projeto com o equipamento aberto); interstitial nunca nos 3 primeiros do dia, no máximo 1 a cada 3 contratos e 240 s; retomar dá vida cheia e, na mensagem, pelo menos 20 s |

## Decisões (com o porquê)

- **L1. Campanha linear em índice global** (região 1 = C1–C24, depois M2-1…M4-16 = 24–71): um `Reputation`, um vetor de estrelas e o mesmo save; a região só soma a trava de estrelas. Custo: mudar o número de contratos ou de salas de uma região desloca as seguintes no save (combinado: 16 contratos por região; aviso no `Content`).
- **L2. Perks novos só a partir da região 2** (Olhos de gato, 3): a região 1 continua com as mesmas ofertas e o bot da campanha não muda.
- **L3. Equipamento sem botão** (CONTEUDO_REGIOES §8): o controle continua de um dedo. Substituiu o "usar" manual da primeira versão desta leva.
- **L4. Marcos e temporada resgatados pelo jogador**, não automáticos: o resgate é um momento de UI e não injeta moedas no bot da campanha.
- **L5. Projetos de equipamento em salas fixas** (`RoomDef.Blueprint`, 12 baús nas regiões 2–4) em vez de sorteio: conteúdo sempre visível na porta. Até a região 2, os projetos vêm da temporada, do baú do dia e do Starter Pack.
- **L6. Código carrega a região nos 2 bits altos:** os códigos da beta com esses bits em 0 (e todo contrato da região 1) continuam iguais; os outros passam a apontar para a região 2–4 (recusados enquanto a região estiver fechada para o jogador). Contrato do dia/código em curso de um save v2 é regenerado na região 1, como era, e o save v3 grava a região no `rx` para ele continuar na 1 depois de regravado.
- **L7. Evento 1× por vaga e semana** (sem "rejogar pagando 50%" do CONTEUDO_REGIOES §9.1; fica para a decisão do coordenador).

## O que ainda não dá para medir (depende do conteúdo da Leva B)

Água, lava, baú enterrado, parede disfarçada e projeto no baú em jogo, e o resgate "só na mesma região", só têm teste de estrutura (ou nenhum) porque a região 1 não tem nada disso; as raias R2–R4 testam com as salas delas. O bot não usa equipamento, modificador, evento nem rewarded: o impacto deles na economia (sobra de ~20 mil moedas estimada no CONTEUDO_REGIOES §10.3) é a medida da Leva B.

---

# v1.0-beta (2026-10-10)

Fonte das regras: [`CONTRATO_V1.md`](historico/CONTRATO_V1.md) §2–§10 (e o que continua do [`CONTRATO_V01.md`](historico/CONTRATO_V01.md)). Núcleo em `client/Assets/_DC/Scripts/Core/` (API comentada no topo do `Sim.cs`). Números do contrato que mudaram estão nas decisões 21–41, com o antes e o depois.

## Como medir (v1)

- `dotnet test client/tools/coretests --logger "console;verbosity=detailed"` imprime tudo abaixo (teste `BalanceTests.Tabela_ParaBalanceMd`, ~40 s a suíte inteira).
- **Tabela realista por contrato:** 6 campanhas do bot (sementes 5000–5005); de cada uma, o save de antes da 1ª tentativa de cada contrato (guilda, armas e estrelas de quem chegou ali) e 20 sementes do bot a partir dele = 120 tentativas por contrato. A arma é a que a campanha escolhe pela carga (`Bot.WeaponFor`). A tabela da v0.1 era "guilda nível 0"; com 24 contratos e guilda de 8 níveis isso mede um jogador que não existe (o C20 de guilda 0 morre 70%).
- **Bot padrão:** agressividade 0,5; reação 0,25 s ± 0,08 s; ganância 0,5. Joga o verbo de cada carga: relíquia sem dash e pousando para abater quem encosta; bolo sem dash; barril pousado no máximo ~1,6 s; mensagem sem luta opcional nem baú; escolhe porta por caveiras + ícone de risco da carga; escolhe perk pela carga (acolchoada para relíquia/bolo/ovo, passo leve para pesada/mensagem, senão coração extra); defende-se de quem persegue de perto com chance 0,2 + 0,8 × agressividade por sala.
- **Campanha:** do zero, 100 sementes; joga o próximo contrato, refaz o que falha, troca a arma pela carga (bombinha nunca com ovo/barril/relíquia nem contra o Capitão) e compra o mais barato que couber (guilda ou nível de arma) antes de cada contrato.
- **H1:** % de salas normais com luta escolhida (`room_fight_choice`); Guarda, chefes e o reforço da emboscada são luta obrigatória e não contam.
- **H2:** os 24 contratos jogados com cada uma das 7 cargas no lugar da original (6 sementes): por bifurcação, a carga muda a porta?
- Determinístico (mesma semente = mesmo resultado, `Determinismo_MesmaSementeMesmoResultado` cobre os 24).

## Tabela por contrato (v1, guilda e armas da campanha, 120 tentativas cada)

| Contrato | Carga | Salas | % concluído | % morte | % falha de carga | duração média (s) | mediana (s) | % salas com luta | moedas médias | estrelas | 3 estrelas % | rachaduras |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| C1 | Caixa | 3 | 100 | 0 | 0 | 22 | 20 | 36 | 89 | 3.00 | 100 | 0.00 |
| C2 | Caixa | 3 | 100 | 0 | 0 | 16 | 15 | 71 | 97 | 3.00 | 100 | 0.00 |
| C3 | Ovo | 4 | 90 | 10 | 0 | 27 | 27 | 48 | 119 | 2.70 | 90 | 0.00 |
| C4 | Caixa | 6 | 87 | 13 | 0 | 53 | 54 | 81 | 168 | 2.50 | 77 | 0.12 |
| C5 | Relíquia frágil | 6 | 59 | 21 | 20 | 64 | 71 | 98 | 137 | 1.57 | 38 | 1.04 |
| C6 | Ovo | 6 | 91 | 9 | 0 | 62 | 61 | 90 | 194 | 2.63 | 81 | 0.12 |
| C7 | Bolo de casamento | 6 | 80 | 20 | 0 | 57 | 59 | 82 | 175 | 1.73 | 13 | 2.88 |
| C8 | Relíquia frágil | 6 | 71 | 21 | 8 | 72 | 73 | 100 | 197 | 1.68 | 27 | 0.99 |
| C9 | Caixa pesada | 7 | 49 | 51 | 0 | 73 | 79 | 75 | 226 | 1.26 | 31 | 0.33 |
| C10 | Mensagem urgente | 7 | 98 | 2 | 0 | 30 | 29 | 24 | 268 | 2.88 | 91 | 0.07 |
| C11 | Ovo | 7 | 79 | 21 | 0 | 63 | 71 | 84 | 335 | 2.30 | 72 | 0.03 |
| C12 | Caixa (+ Capitão) | 7 | 76 | 17 | 8 | 71 | 77 | 75 | 336 | 2.07 | 55 | 0.27 |
| C13 | Barril de pólvora | 7 | 72 | 28 | 0 | 83 | 81 | 73 | 337 | 2.03 | 60 | 0.26 |
| C14 | Bolo de casamento | 7 | 70 | 30 | 0 | 87 | 85 | 84 | 261 | 1.41 | 1 | 7.39 |
| C15 | Relíquia frágil | 7 | 67 | 0 | 33 | 75 | 86 | 97 | 331 | 1.48 | 15 | 1.77 |
| C16 | Mensagem urgente | 7 | 82 | 18 | 0 | 32 | 32 | 27 | 319 | 2.41 | 78 | 0.06 |
| C17 | Caixa pesada | 7 | 76 | 24 | 0 | 82 | 87 | 75 | 534 | 2.00 | 49 | 0.57 |
| C18 | Ovo | 7 | 82 | 16 | 3 | 76 | 81 | 84 | 427 | 2.08 | 45 | 0.53 |
| C19 | Barril de pólvora | 7 | 82 | 18 | 0 | 94 | 93 | 72 | 520 | 2.08 | 47 | 0.64 |
| C20 | Bolo de casamento | 7 | 98 | 2 | 0 | 102 | 94 | 74 | 504 | 2.00 | 5 | 6.55 |
| C21 | Relíquia frágil | 7 | 47 | 0 | 53 | 54 | 41 | 85 | 308 | 1.09 | 16 | 2.11 |
| C22 | Mensagem urgente | 7 | 98 | 2 | 0 | 33 | 33 | 31 | 550 | 2.88 | 92 | 0.07 |
| C23 | Barril de pólvora | 7 | 78 | 23 | 0 | 118 | 122 | 77 | 606 | 1.92 | 41 | 0.85 |
| C24 | Caixa (+ Ogro) | 7 | 95 | 2 | 3 | 180 | 185 | 78 | 677 | 2.23 | 33 | 1.07 |

"Salas" conta até a entrega, com a D1 (C4 = 5 camadas + D1). "Duração" é por tentativa (falha conta o tempo até falhar). "Moedas" = pagamento + resgate + baús + bônus, média de todas as tentativas. "Rachaduras" da carga do contrato no fim (relíquia quebrada conta 3; bolo e caixa não quebram). Falha de carga: relíquia quebrada (C5, C8, C15, C21), roubo pelo Capitão (C12), filhote (C18).

**Campanha (100 sementes):** fecha **100%** · **31,8 tentativas** · 4,6 mortes · **35,7 min de jogo do bot** · 10.211 moedas ganhas, 9.159 gastas · 62,6 de 72 estrelas. Tentativas · tempo da entrega (s) · níveis da guilda somados ao entregar: C1 1,00·22·0 · C2 1,00·17·1,3 · C3 1,18·28·2,5 · C4 1,03·57·4,5 · C5 1,52·74·6,7 · C6 1,19·63·8,7 · C7 1,28·61·10,4 · C8 1,36·73·12,0 · C9 1,88·97·13,6 · C10 1,05·29·15,6 · C11 1,37·74·16,1 · C12 1,34·84·17,3 · C13 1,28·97·18,6 · C14 1,19·91·19,8 · C15 1,52·94·20,2 · C16 1,19·33·21,1 · C17 1,61·95·21,9 · C18 1,17·87·23,5 · C19 1,21·103·24,1 · C20 1,00·102·24,6 · C21 3,13·95·25,4 · C22 1,03·33·26,3 · C23 1,23·134·27,1 · C24 1,03·181·27,8.

## Duração (CONTRATO_V1 §2: bot 1,5–3 min por contrato do C4 em diante)

| Faixa | Medido (duração da entrega na campanha) | Alvo do bot | Situação |
|---|---|---|---|
| C1–C3 (tutoriais) | 17–28 s | curtos | ok |
| C4–C12 (sem mensagem) | 57–97 s (média 76 s) | 90–180 s | **abaixo** |
| C13–C24 (sem mensagem) | 87–181 s (média 105 s) | 90–180 s | dentro (C14/C18 na borda) |
| Mensagem (C10, C16, C22) | 29–33 s | 90–180 s | **abaixo por desenho**: a mensagem abre a porta de Guarda e emboscada e o bot corre |
| Campanha inteira | 35,7 min de jogo do bot | — | humano a 2–3× o bot: ~1,2–1,8 h |

A faixa não fechou no começo (C4–C12) nem na mensagem; ver "Achados". Teste de faixa larga: `Duracao_TutoriaisCurtos_DoC4EmDianteContratosLongos` (cada C4+ entre 20 s e 5 min, média sem a mensagem ≥ 60 s, chefe ≥ 90 s).

## Curva da guilda e das armas

- Guilda no fim da campanha (Vida/Dash/Bolsa/Mochila): 7,0/7,0/7,0/6,8 de 8; custo da linha inteira 2.960 (4 linhas = 11.840) — sobra ralo para repetir contratos (`Campanha_FechaOs24Contratos_ESobraRaloNaGuilda`). Níveis somados por contrato: ~4,5 no C4, 12 no C8, 17 no C12, 21 no C16, 25 no C20, 28 no C24.
- Armas: Bumerangue destrava em média no **C4** (12 estrelas), Bombinha no **C9** (24 estrelas). Nível médio no fim: Estilingue 2,4 · Bumerangue 2,1 · Bombinha 2,0.
- Armas no C12 (guilda 3, 40 sementes; tempo médio / % entrega): Estilingue 1 41 s/18% · Estilingue 3 55 s/43% · Bumerangue 1 70 s/75% · Bumerangue 3 58 s/83% · Bombinha 1 55 s/0% · Bombinha 3 53 s/5%. O C12 tem o Capitão, que foge com a carga: a bombinha (lenta, cai onde ele estava) perde a carga; por isso a campanha não a leva para o mini-chefe. Em contrato sem ladrão a bombinha ganha (C14: 80% contra 48% do estilingue).
- Perks: 2 por contrato do C4 em diante (camadas 2 e 4; o C3 tem 1).

## H1 (lutar é escolha) e H2 (a carga muda a rota)

- **H1** (guilda 3, estilingue 2, C4–C24, 20 sementes): % de salas com luta escolhida por agressividade 0,1 / 0,5 / 0,9 = **56% / 72% / 83%** (entrega 61% / 69% / 81%). Varia 27 pontos (teste exige > 15). Nos tutoriais, passar direto entrega ≥ 90%. Com agressividade 0,5 o bot passa um pouco dos 70% do H1: relíquia luta em ~97% das salas (o verbo dela, como na v0.1) e as salas cheias de perseguidores (R4, R5, R8, R16) quase sempre têm luta. Mensagem: 24–31%.
- **H2** (24 contratos × 7 cargas × 6 sementes, 139 bifurcações): a carga **não** muda a porta (todas as cargas na mesma porta > 80% das vezes) em **28,8%** das bifurcações; nas outras 71% a porta depende da carga. Porta com ícone de ladrão escolhida quando a outra não tem: Caixa 49% · Ovo 50% · Mensagem 61% · Caixa pesada 32% · Bolo 27% · Barril 21% · **Relíquia 19%**. Somando todas as cargas, a porta mais escolhida leva 80,3% (as caveiras pesam para todas). Teste: `Rota_ACargaMudaAPorta_H2`.

## Salas: ícone de ameaça, risco da carga, dano e tempo (tabela realista)

| Sala | Ícone | Risco da carga | Visitas | Dano por visita | Tempo por visita (s) | % luta |
|---|---|---|---|---|---|---|
| R13 Ladrões em Dupla | 1 | ladrão, arqueiro | 1025 | 0,36 | 5,9 | 16 |
| R12 Guarda das Poças | 1 | — | 1474 | 0,95 | 20,6 | 81 |
| R7 Corredor do Arqueiro | 1 | arqueiro | 1128 | 1,11 | 8,3 | 83 |
| R3 Rota longa com baú | 1 | ladrão | 1876 | 1,21 | 7,4 | 69 |
| R1 Sala do Guarda | 2 | — | 1114 | 1,46 | 15,2 | 58 |
| R9 Galeria dos Arqueiros | 2 | ladrão, arqueiro | 402 | 1,49 | 5,8 | 53 |
| R6 Guarda e Ladrão | 2 | ladrão | 429 | 1,61 | 18,6 | 79 |
| R10 Emboscada | 2 | ladrão | 532 | 1,83 | 10,6 | 84 |
| R2 Sala do 1º Ladrão | 2 | ladrão | 815 | 1,84 | 5,9 | 72 |
| R15 Guarda Duplo | 2 | arqueiro | 432 | 1,86 | 19,5 | 36 |
| R14 Labirinto | 2 | — | 1113 | 1,93 | 14,1 | 81 |
| R5 Sala do Bruto | 2 | pancada | 604 | 1,94 | 13,9 | 89 |
| R8 Poça de Limo | 3 | — | 1207 | 2,12 | 10,7 | 84 |
| R11 Bruto e Arqueiro | 3 | arqueiro, pancada | 541 | 2,69 | 18,6 | 85 |
| R4 Enxame | 3 | — | 896 | 2,81 | 7,0 | 89 |
| R16 Salão Final | 3 | ladrão, arqueiro, pancada | 242 | 3,12 | 28,0 | 100 |
| S1 Depósito Secreto | 0 | — | 915 | 0,47 | 6,3 | — |
| S2 Tesouro Escondido | 1 | ladrão | 732 | 0,00 | 4,8 | — |
| M1 Toca do Capitão | 3 | ladrão | 102 | 3,23 | 30,0 | — |
| B1 Covil do Ogro | 3 | ladrão, pancada | 117 | 0,59 | 92,8 | — |

Médias por ícone: 0,91 → 1,74 → 2,69 (folga ≥ 0,3) e nenhuma sala passa da média do ícone vizinho (`IconeDeAmeaca_AcompanhaODanoMedido`). Salas secretas entram em ~0,8 por contrato. **Contratos gerados** (código, guilda 0, 60 payloads): entrega 28% · morte 52% · 42 s — o contrato do dia é feito para quem já passou do C3 com guilda; com a guilda do C13 entrega 77%.

## Decisões da v1 (com o porquê)

21. **Medir pela campanha, não pela guilda 0.** Com guilda 0 o C20 morre 70% e o C24 100% — mede um jogador que não existe. A tabela agora parte do save real da campanha antes de cada contrato.
22. **Reforço de sala com teto** (CONTRATO_V1 §2 "nunca infinito"): dispara quando o courier passa da metade da sala (Half) ou quando a carga fica 3 s no chão nesta sala (CargoDown); entra **em fila, 1 inimigo por segundo**; **1 reforço por visita até o C4 e 2 do C5 em diante** (o 2º só depois que o 1º caiu e a condição vale de novo); **nunca passa de 10 inimigos vivos** (vale também para ladrão da carga sozinha, Ogro e Capitão). Tutoriais (C1–C3) sem reforço. Testes `Reforco_VemPelaCondicao_EmFila_ComTetoPorVisitaEDeVivos`, `Emboscada_*`.
23. **Emboscada (porta fecha até o reforço cair)** em R10, R14 e R16. É luta obrigatória: matar o reforço da emboscada não conta como luta escolhida (H1), como o Guarda (decisão 4). A mensagem abre a porta da emboscada.
24. **Reforço de "metade da sala" chega por trás** (cantos de baixo) nas salas sem emboscada: quem corre para a porta escapa. Antes (reforço junto das portas): H1 da varredura 79/81/86% (não separava); depois de 23–24 e da defesa pela agressividade: 56/72/83%.
25. **Vida dos inimigos por nível do contrato:** × (1 + 0,12 × (nível − 1)) do C4 em diante (C4 ×1,24, C12 ×2,32, C24 ×3,64); morcego e limo fora (perseguidores frágeis). Acompanha o dano das armas e da guilda e alonga a luta sem aumentar o dano por segundo. Antes (sem escala): entregas de 30–60 s do C4 em diante.
26. **Cura de 1 de vida ao entrar numa sala nova** (porta ou parede rachada). Com 5–7 salas e reforço, a vida da guilda sozinha não segurava: C9–C24 morriam 50–100% antes. Não fere o §28 (reabrir não cura; a cura é por sala atravessada).
27. **Limo:** recarga do toque 1,6 s e **rebote de 1,5 m** a cada toque (o contrato só diz "toque 1 de dano"). Antes: grudado no courier parado a cada 1,0–1,2 s, a R12 matava 100% no C14 (278 de dano em 40 tentativas só dela). Velocidade 1,2 m/s e vida 4 como no contrato.
28. **Arqueiro mira o courier** (não a carga); a flecha acerta o primeiro que cruzar (courier, carga no chão, filhote) e para em obstáculo. Sem linha, ele sai da cobertura. Antes (mirava a carga no chão e ficava parado sem linha): relíquia quebrava e havia tentativas presas até 600 s (bumerangue × arqueiro atrás de pilar).
29. **Bumerangue precisa de linha livre** (`Run.CanHit`): a mira automática só escolhe alvo atingível; estilingue (teleguiado) e bombinha (arco) passam por cima de obstáculo.
30. **Ladrão em 6 das 16 salas** (R2, R3, R6, R9, R10, R13, R16): antes os reforços punham ladrão em 15 de 16 salas, o ícone de risco não dizia nada e a relíquia do C8 quebrava 95%. Ícone de risco: ladrão (ladrão/Capitão), arqueiro, pancada (Bruto/Ogro); salas sem risco: R1, R4, R8, R12, R14.
31. **Guarda 5 → 16 de vida; Capitão 14 → 24; Ogro 30 → 45** (todos × escala do nível). Antes, o Guarda caía em 4 s, a M1 durava 7 s e a B1 16 s; depois 15–20 s, 30 s e 93 s. **Proposta de mudança no CONTRATO_V1 §4** (Capitão "vida 14").
32. **Bolo: racha ao cair (ladrão), com pancada e flecha no chão; não com golpe no courier.** O verbo dele é o dash proibido (o toque rápido só balança, `CakeWobble`). Antes (rachava como a relíquia): 3,6/11,2/17,9 rachaduras nos C7/C14/C20 e 0% de 2ª estrela. Depois: 2,9/7,4/6,6 — ainda alto (ver Achados).
33. **Parede rachada só abre empurrando contra ela** (componente do movimento para a parede ≥ 0,7). Antes, andar rente à parede (o caminho natural na R3) levava à sala secreta sem querer.
34. **Guilda 4 × 8** (Vida, Dash, Bolsa, Mochila): custo 60 × 1,5ⁿ (60, 90, 135, 205, 305, 455, 685, 1025; era 60 × 1,6ⁿ em 5 níveis); Dash −6% por nível (era −10%: com 8 níveis daria 0,32 s); Mochila +0,15 m no raio de pegar e −0,02 s no pouso por nível; Vida +1 e Bolsa +10% como antes. Vida máxima possível 16 (`Balance.MaxHpCap`, com o Coração extra).
35. **Armas:** Estilingue (inicial) · Bumerangue (12 estrelas) · Bombinha (24 estrelas); nível 2 e 3 custam 300/700 · 400/900 · 500/1100. Ganho por nível suave (dano por segundo +30% e +60%). Antes (nível 2 dobrava o dano por segundo do bumerangue): as lutas do meio da campanha acabavam rápido demais.
36. **Tempos pela mediana do bot na campanha:** 3ª estrela = 1,6× (quem joga no ritmo de um humano bom pega); ovo choca em 2× (regra da decisão 1: quem joga na metade do ritmo do bot choca ~50%); cronômetro da mensagem = 2,5× (humano a 2× passa com folga de 25%). Gerados: por sala, 18 s (estrela), 23 s (ovo), 12 s (mensagem).
37. **Ícone de ameaça por terços do dano medido** (1 = R3, R7, R12, R13; 2 = R1, R2, R5, R6, R9, R10, R14, R15; 3 = R4, R8, R11, R16) e critério do teste por média do ícone (o pareamento estrito sala a sala da v0.1 vira ruído com 16 salas: R15 tem poucas visitas e oscilava de 1,51 a 2,09 conforme o ícone mudava quem passava por ela).
38. **Estrelas:** 1 = entregue; 2 = carga intacta (0 rachadura; ovo sem chocar); 3 = dentro do `ParTime`. "Sem morrer" do contrato é implícito (entregou = não morreu). Recorde por contrato (`StarsOf`), soma em `Stars` (a "reputação" da UI, destrava armas); `Reputation` continua o progresso em sequência.
39. **Contrato do dia:** `daily-AAAAMMDD` → payload; o mesmo para todos; disponível depois do C3; **falhar não gasta o dia** (só a entrega), bônus de 100 (× Bolsa) uma vez por data; data anterior à maior já vista = indisponível (relógio voltando). **Código:** 6 caracteres de 32 símbolos (A–Z sem I/O, 2–9) = 25 bits de contrato + 5 de verificação (pega ≥ 90% dos erros de 1 caractere); maiúscula/minúscula, espaço e traço aceitos.
40. **Save v2:** grava estrelas, arma equipada e níveis, maior data vista, último dia entregue, perks e oferta aberta do contrato em curso, tipo do contrato (campanha/dia/código). **v1 → v2:** Mochila 0, 1 estrela em cada contrato já concluído, carga pendente mantida, **contrato em curso descartado** (os contratos da v1 mudaram de camadas). v0 → v2 idem. v3+ recusado sem apagar.
41. **Testado e descartado:** (a) Guarda "porteiro" em R7/R9/R13 para alongar: com arqueiro em volta a relíquia quebrava e a campanha fechou 0/20; (b) emboscada de morcegos na R4: C12 com 13,6 tentativas (morcego mata o courier parado); (c) limo com vida escalada: R12 mortal; (d) reforço junto às portas em toda sala: alonga, mas o H1 deixa de separar (79/81/86%).

Evolução das medições principais (campanha do bot):

| Etapa | Campanha fecha | Tentativas | Tempo do bot | Entrega C4–C12 / C13–C24 |
|---|---|---|---|---|
| Conteúdo inicial (1 reforço, sem escala) | presos até 600 s | — | — | 15–60 s / 25–72 s |
| + emboscada sem trava, cura por sala, escala 0,04 | 20/20 | 39,9 | 64 min (com presos) | 33–75 s / 50–106 s |
| + 2º reforço, armas caras, +1 camada | 19/20 | 101,9 | 56 min | 35–74 s / 41–111 s |
| + ladrão em 7 salas, escala 0,08, defesa | 20/20 | 75,3 | 70 min | 52–111 s / 51–122 s |
| + limo sem escala com rebote | 20/20 | 29,2 | 37 min | 45–112 s / 41–137 s |
| + reforço por trás, defesa pela agressividade, escala 0,12 | 20/20 | 32,2 | 33 min | 33–83 s / 38–186 s |
| **Final** (Guarda 16, bolo, bombinha fora do Capitão, tempos calibrados) | **100/100** | **31,8** | **35,7 min** | **57–97 s / 87–181 s** (mensagem 29–33 s) |

## Achados para o design (decisão do Vinicius)

- **Duração do C4 ao C12 abaixo do alvo do bot** (57–97 s contra 90–180 s) e mensagem em ~30 s. As alavancas que alongaram sem matar (reforço com teto, escala de vida, Guarda mais duro) já estão no máximo seguro: mais inimigos perseguidores matam o bot antes de alongar o contrato; Guarda em mais salas quebra a relíquia. Opções: (1) aceitar C4–C12 em ~1–1,5 min de bot (humano 2–4 min, dentro do alvo humano de 3–6 min a partir do meio); (2) +1 camada do C5 ao C12 (6 camadas); (3) salas maiores que a tela (câmera que segue) — custo de View. Proposta: (1) agora, medir humano no playtest (diário `contract_end`).
- **H1 do bot em 72% com agressividade 0,5** (alvo 30–70%): reforço + perseguidores puxam luta; relíquia luta em quase toda sala. Varia com a agressividade (56→83%), então lutar continua escolha; o H1 de verdade é do playtest.
- **Bolo quase nunca dá a 2ª estrela** (1–13%): racha ao cair da mão do ladrão e com flecha/pancada no chão, e o bot luta perto dele. Proposta: estrela 2 do bolo = até 2 rachaduras (ele não quebra), ou o bolo só rachar ao cair.
- **Relíquia segue a carga mais dura** (C21 47%, 3,1 tentativas; C5/C8/C15 59–71%). É o verbo; o Coração extra e a Carga acolchoada ajudam. Se o playtest achar punitivo, o 1º lugar a mexer é o ladrão no reforço das salas da relíquia.
- **Bombinha nível 1** é a pior arma contra ladrão e Capitão (0% no C12) e a melhor em sala cheia sem ladrão (C14 80%). Está coerente com "lenta e em área", mas o jogador precisa saber: a UI deve avisar na troca de arma ("lenta contra quem foge com a carga").
- **Contrato do dia com guilda baixa** é duro (28% de entrega com guilda 0). Ele só abre depois do C3; com a guilda do C13, 77%.
- **Mudanças de número para o CONTRATO_V1:** Capitão vida 14 → 24 (§4); Guarda 5 → 16 (v0.1 §5); limo com rebote e recarga 1,6 s (§4); bolo não racha com golpe no courier (§3); reforço 1–2 por visita com fila e teto de 10 vivos (§2); cura de 1 por sala (novo).

---

# v0.1 — medição do bot (2026-10-09, histórico)

Fonte das regras e dos números iniciais: [`CONTRATO_V01.md`](historico/CONTRATO_V01.md). Aqui ficam a medição do bot, os custos e as decisões tomadas no núcleo (`client/Assets/_DC/Scripts/Core/`), com o porquê. As decisões 1 e 2 já foram aceitas e levadas ao `CONTRATO_V01.md` pelo coordenador; a passada 2 (relíquia frágil e ícones) está nas decisões 18–19.

## Como medir

- `dotnet test client/tools/coretests --logger "console;verbosity=detailed"` imprime a tabela abaixo (teste `BalanceTests.Tabela_ParaBalanceMd`).
- **Bot padrão:** agressividade 0,5; reação 0,25 s ± 0,08 s (o plano só é refeito nesse intervalo: o handicap humano); ganância 0,5; guilda nível 0; todos os contratos liberados; 200 sementes (base 1000). Contrato jogado isolado.
- **Campanha:** do zero, joga o próximo contrato bloqueado, refaz o que falha (aceitando o resgate quando há pendente) e compra o upgrade mais barato que couber antes de cada contrato. 100 sementes (base 5000).
- Determinístico: mesma semente = mesmo resultado (teste `Determinismo_MesmaSementeMesmoResultado`).

## Tabela por contrato (bot padrão, 200 sementes, guilda nível 0)

| Contrato | Carga | Camadas | % concluído | % morte | % falha de carga | duração média (s) | % salas com luta | moedas médias | vida na entrega | rachaduras médias |
|---|---|---|---|---|---|---|---|---|---|---|
| C1 | Caixa | 2 | 100 | 0 | 0 | 14.2 | 33 | 72 | 4.9 | 0.00 |
| C2 | Caixa | 2 | 100 | 0 | 0 | 12.9 | 55 | 82 | 3.4 | 0.00 |
| C3 | Ovo | 2 | 98 | 3 | 0 | 13.3 | 28 | 92 | 4.1 | 0.00 |
| C4 | Caixa | 3 | 97 | 3 | 0 | 22.0 | 50 | 115 | 3.6 | 0.07 |
| C5 | Relíquia frágil | 3 | 90 | 6 | 5 | 22.8 | 66 | 116 | 4.0 | 1.30 |
| C6 | Ovo | 3 | 86 | 15 | 0 | 23.6 | 79 | 134 | 1.8 | 0.07 |
| C7 | Relíquia frágil | 3 | 80 | 20 | 0 | 23.8 | 95 | 151 | 2.7 | 0.64 |
| C8 | Caixa | 4 | 69 | 32 | 0 | 37.3 | 65 | 135 | 2.2 | 0.54 |

"Camadas" conta as salas até a entrega (o C8 inclui a sala do chefe). "Moedas médias" = pagamento + resgate + baús, média de todas as tentativas (falhas pagam só o baú). "% salas com luta" = salas R1–R6 em que o bot matou ao menos 1 inimigo não-Guarda (`room_fight_choice`, H1). "Rachaduras médias" = rachaduras da carga do contrato no fim (relíquia quebrada conta 3; na Caixa só o Bruto e o Ogro racham). Em C1–C8 a soma de concluído + morte + falha pode passar de 100 por arredondamento.

**Campanha (100 sementes):** fecha os 8 contratos em 100% · 8,1 tentativas · 0,1 morte · **3,1 min de jogo do bot no total** · 1.158 moedas ganhas · níveis Vida/Dash/Bolsa 3,0/3,0/2,5 ao fim (de 5/5/5). Tentativas por contrato: C1 1,00 · C2 1,00 · C3 1,00 · C4 1,01 · C5 1,06 · C6 1,02 · C7 1,00 · C8 1,05.

### Lutar é escolha (H1): varredura de agressividade (200 sementes, nível 0)

| Contrato | % luta (agr. 0) | % luta (agr. 0,5) | % luta (agr. 1) | % concluído (agr. 0) | % concluído (agr. 1) |
|---|---|---|---|---|---|
| C1 | 23 | 33 | 43 | 100 | 100 |
| C2 | 29 | 55 | 81 | 100 | 93 |
| C3 | 4 | 28 | 48 | 100 | 93 |
| C4 | 32 | 50 | 73 | 97 | 94 |
| C5 | 66 | 66 | 67 | 95 | 93 |
| C6 | 62 | 79 | 95 | 72 | 88 |
| C7 | 91 | 95 | 97 | 69 | 86 |
| C8 | 35 | 65 | 94 | 70 | 64 |

O % de luta responde à agressividade (o teste `Lutar_EEscolha_...` exige ≥ 15 pp entre 0,1 e 0,9) e passar direto é viável no começo (C1–C3 = 100% sem lutar por escolha). Com agressividade 0 ainda há salas "com luta": morcegos que chegam enquanto o courier está parado no Guarda ou no baú e ladrões abatidos ao pegar a carga. **Exceção de propósito: C5 e C7 (relíquia) lutam quase sempre, com qualquer agressividade** — a relíquia racha com golpe e queda, então o bot pousa e abate o que vem encostar. A agressividade muda o que acontece com a relíquia: com 0, 1,68/1,14 rachadura (C5/C7); com 1, 0,36/0,30, porque o bot agressivo caça o ladrão antes de pousar.

### Dano por visita de sala (bot padrão)

| Sala | Ícone de ameaça (era) | Dano médio por visita | Fonte |
|---|---|---|---|
| R1 Guarda | 1 | 0,00 | o bot atira de fora do golpe |
| R3 Baú | 1 | 1,00 | 1 morcego |
| R5 Bruto | **2** (3) | 1,14 | 1 morcego + Bruto (o bot sai de quase todo telegraph) |
| R2 1º Ladrão | 2 | 1,29 | 2 morcegos |
| R6 Guarda+Ladrão | 3 | 2,00 | 1 morcego + Guarda |
| R4 Enxame | **3** (2) | 2,50 | 4 morcegos |
| B1 Ogro | 3 | 0,47 | pancada de 3 (fora da trava: chefe, porta única) |

O ícone segue a ordem do dano (teste `IconeDeAmeaca_AcompanhaODanoMedido`: toda sala de ícone maior tira mais vida por visita que toda sala de ícone menor, e a média sobe ≥ 0,3 por ícone; hoje 0,50 → 1,22 → 2,25). **Morcegos são quase todo o dano.**

### Ladrão, relíquia, ovo, baú (eventos por contrato, bot padrão)

| Contrato | ladrão pegou a carga | carga recuperada | ladrão escapou | ladrão da carga sozinha | rachaduras | baús abertos |
|---|---|---|---|---|---|---|
| C1 | 0,35 | 0,35 | 0 | 0 | 0 | 0,47 |
| C2 | 0,74 | 0,74 | 0 | 0 | 0 | 0,57 |
| C3 | 0,22 | 0,22 | 0 | 0 | 0 | 0,23 |
| C4 | 0,47 | 0,47 | 0 | 0 | 0,07 | 0,98 |
| C5 | 0,86 | 0,86 | 0 | 0 | 1,30 | 0,78 |
| C6 | 0,62 | 0,62 | 0 | 0 | 0,07 | 0,95 |
| C7 | 0,40 | 0,40 | 0 | 0 | 0,64 | 1,32 |
| C8 | 0,58 | 0,58 | 0 | 1,11 | 0,54 | 1,22 |

De onde vêm as rachaduras da relíquia (por contrato): C5 = queda da mão do ladrão 0,86 (R3 0,66, R2 0,20) + golpe no courier 0,44 (quase tudo morcego na R3); C7 = ladrão 0,40 + golpe 0,24. O dash quase não racha porque o bot não dá dash com a relíquia nas mãos. O bot nunca chocou o ovo (C3 termina em 12–18 s; C6 em 18–35 s; tempos-alvo 30 s e 45 s).

## Custos da guilda

Custo do próximo nível = 60 × 1,6^nível, arredondado a 5. Teste: `Guilda_CustoDosUpgrades_ECompra`.

| Nível atual → próximo | 0→1 | 1→2 | 2→3 | 3→4 | 4→5 | linha inteira | 3 linhas |
|---|---|---|---|---|---|---|---|
| Custo | 60 | 95 | 155 | 245 | 395 | 950 | 2.850 |

Efeitos: **Vida** +1 de vida máxima (6 → 11); **Dash** −10% de recarga por nível (1,6 s → 0,8 s); **Bolsa** +10% de moedas por nível em pagamento, baú e resgate. A guilda abre depois do C1 (Reputation ≥ 1); o C1 paga 60 (+25 do baú da R3), então o 1º upgrade sai antes do C2. A campanha do bot compra ~9 níveis (930 moedas) nos 8 contratos e deixa ~1.900 de ralo para repetir contratos.

## Pagamento

Pagamento = Pay × carga × integridade × Bolsa, arredondado. Carga: Caixa ×1,0 · Relíquia ×1,4 · Ovo ×1,2 (chocado ×0,8). Integridade: 0 rachadura ×1,5 · 1 ×1,2 · 2+ ×1,0 (só a Relíquia quebra, com 3). Resgate: +50% do valor da carga perdida (Pay × carga, sem integridade), com Bolsa.

| | C1 | C2 | C3 | C4 | C5 | C6 | C7 | C8 |
|---|---|---|---|---|---|---|---|---|
| Pay | 40 | 45 | 50 | 60 | 65 | 70 | 80 | 110 |
| Entrega perfeita, nível 0 | 60 | 68 | 90 | 90 | 137 | 126 | 168 | 165 |

## Decisões (com o porquê)

Números que mudaram em relação ao `CONTRATO_V01.md` (proposta: atualizar o contrato no mesmo commit):

1. **Ovo: tempo-alvo do C3 de 90 s para 30 s; C6 com 45 s** (o contrato não dava o do C6). Antes: com 90 s o ovo nunca chocava (o bot termina o C3 em 12–18 s, o C6 em 18–35 s), e o verbo da carga não existia. Regra usada: tempo-alvo ≈ 2× a duração mediana do bot; quem joga na metade do ritmo do bot choca ~50% das vezes. Hipótese a validar no playtest (diário: `contract_end` + evento de choco).
2. **R5 (Sala do Bruto): 2 morcegos → 1.** Antes (mesmas 200 sementes): C4 72% concluído / 29% morte, C7 34%/67%, C8 30%/70%; a R5 era a sala mais letal (2,9 de dano por visita, quase todo dos morcegos enquanto o bot cuidava do Bruto) e o C4 ("Bruto aparece") ficava mais difícil que o C5 e o C6. Depois: C4 96%/5%, C6 86%/15%, C7 63%/37%, C8 61%/39% — a curva sobe em ordem e a lição da sala volta a ser o telegraph do Bruto. Campanha antes: 8,9 tentativas e 0,9 morte; depois: 8,2 e 0,2.
3. **Testado e descartado:** R6 com 2 ladrões. O bot recuperou 100% dos furtos (2,07 por contrato) e a derrota não ficou mais provável; voltou a 1 ladrão, como no contrato.

Detalhes que o contrato não fixava (decididos no núcleo, todos cobertos por teste):

4. **Guarda e Ogro trancam todas as portas da sala** enquanto vivos (salas de luta obrigatória). Mais simples que "a porta dele" e igual na prática: em todas as salas com Guarda a luta já é obrigatória.
5. **O dash conta como golpe para o ladrão:** o empurrão derruba a carga que ele leva (o criativo 11 do GDD). Guarda e Ogro não são empurrados.
6. **O ladrão leva 1 s para sumir pela borda** (`Balance.ThiefExit`) depois de chegar à parede lateral mais perto. Sem isso, a carga pousada perto da parede sumia em 0,3 s, sem tempo de reagir.
7. **Sem carga no chão, o ladrão ronda o courier a 3 m** (não ataca): "ignora o courier" vale para o dano; assim, pousar perto de um ladrão vivo é sempre arriscado.
8. **Alcance dos golpes:** medido do centro do inimigo até a borda do alvo (Guarda e Bruto 1,2 m; Ogro 2 m). Carga no chão: centro a até 1,2 m do Bruto (2 m do Ogro). O golpe do Guarda não acerta carga.
9. **Filhote:** qualquer golpe ou toque tira 1 de vida (vida 2 = aguenta 1 golpe); a porta e a entrega exigem o filhote a até 3 m.
10. **Baú:** credita na hora (com Bolsa) e fica marcado no save da sala, então reabrir o app não paga de novo (`Exploit_BauNaoPagaDuasVezesAoReabrir`).
11. **Carga perdida:** valor = Pay × multiplicador da carga; o bônus é 50% disso (sempre menor que o pagamento perdido, `Exploit_MorrerDePropositoNaoCompensa`). A oferta expira ao começar o próximo contrato, aceita ou não. Resgate deixado para trás numa porta ou roubado se perde sem gerar nova pendência. Relíquia quebrada e filhote morto não geram pendência.
12. **Tiro teleguiado:** o projétil persegue o alvo e não erra; obstáculo não bloqueia. Simplificação de greybox; se a View precisar de tiro com linha de visada, é uma regra nova.
13. **Abandonar** (`Sim.Abandon`) = falha sem carga pendente (senão abandonar viraria fábrica de resgate).
14. **Inimigos comuns nascem com ±0,4 m de variação por semente**; Guarda e Ogro, no ponto exato.
15. **Determinismo:** RNG xorshift32 próprio, passo fixo de 1/30 s, nada de `System.Random`/`DateTime`, e nada de `Math.Sin/Cos` na simulação (direções por tabela, zigue-zague do morcego por onda triangular), porque o resultado delas pode variar entre x64 e ARM64.
16. **Moedas em porcentagem inteira:** pagamento = Pay × carga% × integridade% × Bolsa%, arredondado uma vez só no fim (meio para cima). Com float, `1.4f` vira 1,3999… e a relíquia perfeita do C5 pagava 136 em vez de 137 (teste `Pagamento_PorCargaIntegridadeEBolsa`).
17. **Save v0** = texto sem a linha `v`: leva só `coins`, `lv`, `rep` (não existia contrato em curso nem pendência) e marca `Dirty` para regravar em v1. Versão futura: `SaveRefused`, `Save()` devolve `null`, `Dirty` fica falso.

Passada 2 (decisões do coordenador, 2026-10-09):

18. **Relíquia frágil de verdade.** A relíquia racha +1 quando **cai** (dash que a larga; ladrão atingido que a derruba; ladrão empurrado pelo dash) e a cada **golpe no courier que a carrega** (toque de morcego incluso). Pousar não racha. O golpe do Bruto pelas costas continua rachando a carga nas mãos, mas a relíquia racha **1 vez** por golpe, não 2. Caixa e Ovo não mudaram. Testes: `Reliquia_RachaAoCair_DashELadrao_PousarNao`, `Reliquia_GolpeNoCourierQueACarrega_Racha_IncluiMorcego`, `Reliquia_VerboAparece_SemTornarC5eC7Inviaveis` (média ≥ 0,8 nos dois contratos de relíquia, cada um ≥ 0,5, C5 ≥ 80%, C7 ≥ 55%).
    - **Antes** (regra antiga): C5 96% / 0,00 rachadura; C7 63% / 0,08 — a relíquia era uma caixa que pagava 40% a mais.
    - **Regra nova com o bot antigo:** C5 39% / 2,29 rachaduras / 57% quebrada; C7 22% / 2,14 / 54%. Punitivo, mas porque o bot carregava a relíquia para dentro dos morcegos.
    - **O bot passou a jogar o verbo** (o que um jogador que viu a relíquia rachar faria): com a relíquia nas mãos, pousa e abate o que vem encostar; não dá dash de esquiva; caça o ladrão que ronda nas salas em que escolheu lutar ou depois da 1ª rachadura.
    - **Testado e descartado:** bot que sempre mata o ladrão antes de pousar (C5 95%, C7 86%, mas 0,00/0,07 rachadura: com jogo perfeito o verbo some da medição); i-frame de 1 s nas rachaduras por golpe (sem efeito: o bot nunca leva 2 golpes no mesmo segundo com a relíquia; não ficou no código).
    - **Depois:** C5 90% / 1,30 rachadura / 5% quebrada; C7 80% / 0,64 / 0%. Média 0,97.
19. **Ícone de ameaça = ordem do dano medido: R4 2 → 3 e R5 3 → 2.** Antes, a R5 (ícone 3) tirava 1,12 de vida por visita, menos que a R2 (ícone 2, 1,40), e a R4 (ícone 2) era a pior sala (2,50); subir só a R4 não bastava. Depois: ícone 1 = R1/R3 (0,00/1,00) < ícone 2 = R5/R2 (1,14/1,29) < ícone 3 = R6/R4 (2,00/2,50). A B1 fica 3 e fora da trava (chefe, porta única, sem escolha). Efeito colateral medido: com os ícones novos o bot com relíquia escolhe mais R3 → R2, as duas salas com ladrão, e o C5 caiu de 86% para 78% antes do ajuste "cuidado depois da 1ª rachadura" da decisão 18.

20. **Pouso só depois do 1º movimento na sala** (coordenador, 2026-10-09, foto `client/Builds/validation_v01/shots/02_c1_inicio.png`, fora do repositório desde 2026-10-25 e no histórico do git): com o dedo solto desde o início do C1, o courier pousava a caixa em 0,2 s e a 1ª dica era "arraste perto da carga para pegá-la", contra a FTUE. Agora `Run.AwaitingMove` segura o contador até o 1º movimento ou dash, com a dica `DragToWalk`. O bot dá um passo curto quando quer parar logo na entrada (sem isso o C5 caía de 90% para 67%, porque o bot levava golpe com a relíquia nas mãos). Teste `AoEntrar_NaoPousaAteOPrimeiroMovimento_FTUE` (vermelho provado); o teste do baú ganhou o passo inicial, sem o qual passaria por construção. Balance: 49/49, sem mudança de faixa.

## Achados para o design (precisam de decisão do Vinicius)

- **Duração.** O bot fecha um contrato em 13–36 s e a campanha inteira em ~3 min de jogo. O GDD (§2, §6) conta com contratos de 4–6 min e sessões de 4–9 min. Mesmo que um jogador novo leve 2–3× o tempo do bot, um contrato fica em ~1 min. O formato do contrato (sala 9 × 14 que cabe na tela, 2–4 camadas) gera contratos curtos; para chegar a 4–6 min é preciso mais camadas, ondas por sala ou salas maiores. Decisão de design, não de número.
- **O ladrão nunca leva a carga do bot** (fuga 0–0,01 por contrato, 100% recuperada): fugir a 1,6 m/s + 1 s na borda dá tempo de sobra. Para humanos pode ser diferente; medir no diário (`thief_grab` × `cargo_drop` motivo 2).
- **O ícone conta o dano ao courier, não o risco para a carga.** Para a relíquia, as salas mais perigosas são as do ladrão (R3 ícone 1, R2 ícone 2), porque a queda racha; quem segue as caveiras leva a relíquia justo para elas. Proposta: um 2º ícone na porta (mão/ladrão), ou o ícone contar também o risco da carga. Mexe com o H2 ("a carga muda a rota").
- **H1 do bot** fica em 28–95% de salas com luta com agressividade 0,5: a relíquia faz o C7 lutar em 95% das salas (passa dos 70% do H1) e o C5 em 66%, qualquer que seja a agressividade. É o verbo funcionando, mas no playtest o H1 tem de ser lido por carga. O H1 de verdade é do playtest.
