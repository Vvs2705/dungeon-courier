# Balance — Região 4 "Forja do Dragão" (raia R4, 2026-10-11)

Fonte do desenho: [`CONTEUDO_REGIOES.md`](CONTEUDO_REGIOES.md) §1.1 (lava, respiro, fogo no chão), §4 (salas e contratos), §5 (inimigos e chefes), §6 (planta e mapa), §10 (destrave e economia). Código: `Core/Content/Regiao4.cs` (salas e contratos), `Core/Run.Regiao4.cs` (comportamento e números em `Balance`), `Core/Bot.Regiao4.cs` (bot e medição). Testes: `Tests/EditMode/CoreTestsRegiao4.cs`.

**Status:** medido pelo bot com o estado **estimado** do fim da região 3 (as raias R2 e R3 mediam em paralelo). Quando as regiões 2–3 fecharem a medição delas, refazer esta tabela partindo do save real do fim da região 3.

## Como medir

- **Ponto de partida** (`Bot.Regiao3EndSave(semente)`, HIPÓTESE pelo `CONTEUDO_REGIOES.md` §10): 132 estrelas (o bot faz ~2,2 por contrato nas regiões novas) preenchidas do C1 em diante; guilda 8/8/8/8 e as 3 armas no nível 3 (as regiões 2–3 esgotam os ~4.200 + ~2.100 que a região 1 deixa); Bomba de fumaça e Botas de lama destravadas no nível 5 (13.600 moedas) e o Gancho com 2 de 3 projetos (R3-2, R3-9); **13.900 moedas** (~33.800 ganhas nas regiões 2–3 menos esses ralos). Fumaça equipada.
- **Campanha:** `Bot.PlayCampaign(semente, region: 4, from: Bot.Regiao3EndSave(semente))` — joga M4-1…M4-16 em ordem, refaz o que falha (aceitando o resgate), troca a arma pela carga e compra o mais barato que couber antes de cada contrato. **Novo:** a campanha também compra nível de equipamento e equipa o 1º destravado (na região 1 nada destrava: números da v1 iguais).
- **Tabela realista:** `Bot.MeasureRegion4(20, 20)` — 20 campanhas (sementes 7100…); de cada uma, o save de antes da 1ª tentativa de cada contrato e 20 sementes do bot a partir dele = **400 tentativas por contrato**.
- **H1:** % de salas normais com luta escolhida (`room_fight_choice`). Morte por respiro, fogo no chão e mordida da planta **não** conta como luta (decisão R4-8); empurrão na lava pelo dash conta.
- **Bot da região 4** (`Bot.Regiao4.cs`, mesmo handicap de reação 0,25 s ± 0,08): sai dos avisos no chão (frasco, chicote, giro, sopro, pouso, surgimento do diabrete) para o lado certo; contorna a cada passo fogo no chão, respiro que vai soltar fogo em menos de 1 s e aviso (`SteerR4`); sem saída, recua devagar segurando a carga; não pousa em cima de fogo, respiro ou aviso; empurra inimigo na lava com o dash quando não está com carga nas mãos; com a planta, "dá o passo" com o dash e luta de longe dela; com o Mapa ou o Faro de baú vai aos baús enterrados (ganância); não dá dash de fuga com carga nas mãos (largaria a carga dentro do aviso); barril com o pavio adiantado vem antes de tudo.
- Reproduzir: os números abaixo saem de um programa de medição fora do repositório que chama só essas APIs (`Bot.MeasureRegion4`, `Bot.PlayCampaign`, `Bot.Play`, `Bot.Line`); o teste `Campanha_Regiao4_PeloBot_FechaOs16_DragaoVencivel` refaz a campanha com 4 sementes.

## Resultado contra o alvo

| Alvo da raia | Medido | Situação |
|---|---|---|
| Fechar 100%, com o Dragão vencível | **20 de 20** campanhas (40 de 40 numa 2ª rodada) · **17,9 tentativas** para 16 contratos (1,12 por contrato) · 1,5 morte · 27,9 min de jogo do bot · M4-16 entregue em **61%** das tentativas (1,8 tentativa na campanha); o Dragão sozinho, de vida cheia, cai em 90% | ok |
| Do 4º contrato em diante, 1,5–3 min (chefe pode passar) | média **93 s** (sem a mensagem e o chefe); M4-6, M4-7, M4-8, M4-11, M4-14, M4-15 dentro; **abaixo de 90 s:** M4-4 60, M4-5 67, M4-9 64, M4-10 83, M4-13 89; M4-12 (mensagem) 34 s por desenho; M4-16 269 s | no limite de baixo |
| H1 entre 30% e 70% | **70,0%** com agressividade 0,5 (0,1: 50% · 0,9: 83%; varia 33 pontos: lutar é escolha) | no limite de cima |
| Moedas paradas no fim | **21.600** no fim (ganhou 21.300 na região, gastou 13.600 no Gancho e na Armadilha até o nível 5; nada mais para comprar) | **acima** (ver Riscos) |

## Tabela por contrato (20 campanhas × 20 sementes = 400 tentativas cada)

| Contrato | Carga | Salas | % concluído | % morte | % falha de carga | duração média (s) | mediana (s) | % salas com luta | moedas médias | estrelas | 3 estrelas % | rachaduras |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| M4-1 | Caixa | 3 | 100 | 1 | 0 | 24 | 23 | 79 | 932 | 2.95 | 96 | 0.01 |
| M4-2 | Planta carnívora | 4 | 100 | 0 | 0 | 45 | 43 | 68 | 1247 | 2.87 | 87 | 0.01 |
| M4-3 | Mapa do tesouro | 4 | 100 | 0 | 0 | 37 | 36 | 61 | 1272 | 3.00 | 100 | 0.00 |
| M4-4 | Relíquia frágil | 6 | 65 | 0 | 35 | 52 | 52 | 91 | 863 | 1.61 | 32 | 1.61 |
| M4-5 | Cristal de gelo | 6 | 98 | 1 | 1 | 65 | 64 | 76 | 1223 | 2.08 | 11 | 1.06 |
| M4-6 | Balão de gás | 7 | 88 | 0 | 12 | 92 | 94 | 71 | 1180 | 2.47 | 70 | 0.24 |
| M4-7 | Planta carnívora | 6 | 80 | 20 | 0 | 112 | 110 | 71 | 996 | 2.22 | 64 | 0.27 |
| M4-8 | Caixa (+ Capataz) | 7 | 98 | 2 | 0 | 109 | 109 | 78 | 1166 | 2.73 | 77 | 0.31 |
| M4-9 | Ovo | 7 | 100 | 0 | 0 | 66 | 64 | 64 | 1309 | 2.83 | 84 | 0.20 |
| M4-10 | Mapa do tesouro | 7 | 100 | 0 | 0 | 85 | 85 | 61 | 1588 | 2.80 | 80 | 0.28 |
| M4-11 | Barril de pólvora | 7 | 97 | 2 | 1 | 120 | 123 | 73 | 1417 | 2.72 | 78 | 0.33 |
| M4-12 | Mensagem urgente | 7 | 99 | 1 | 0 | 35 | 34 | 40 | 1136 | 2.81 | 84 | 0.18 |
| M4-13 | Caixa pesada | 7 | 93 | 7 | 0 | 93 | 91 | 76 | 1676 | 2.65 | 79 | 0.10 |
| M4-14 | Planta carnívora | 7 | 78 | 22 | 0 | 101 | 103 | 68 | 1146 | 2.10 | 54 | 0.43 |
| M4-15 | Bolo de casamento | 7 | 88 | 13 | 0 | 113 | 117 | 74 | 1128 | 2.02 | 27 | 1.11 |
| M4-16 | Caixa (+ Dragão) | 6 | 61 | 39 | 1 | 236 | 258 | 81 | 768 | 1.45 | 23 | 1.04 |

"Salas" conta até a entrega, com a D4. "Duração" é por tentativa (falha conta o tempo até falhar). Falha de carga: relíquia quebrada (M4-4: golpe no courier com ela nas mãos, ladrão atingido, fumaça que faz o ladrão soltar), balão estourado/escapou (M4-6, verbo da raia R3). As cargas das regiões 2–3 (gelo, balão) usam o verbo das raias R2/R3 mais o fogo daqui.

**Campanha (20 sementes):** fecha **100%** · **17,9 tentativas** · 1,5 morte · **27,9 min de jogo do bot** · 21.310 moedas ganhas, 13.600 gastas, **21.610 no fim**. Tentativas · tempo da entrega (s): M4-1 1,00·20 · M4-2 1,00·53 · M4-3 1,00·39 · M4-4 1,40·60 · M4-5 1,00·67 · M4-6 1,00·101 · M4-7 1,20·109 · M4-8 1,00·108 · M4-9 1,00·64 · M4-10 1,00·83 · M4-11 1,00·121 · M4-12 1,00·34 · M4-13 1,15·89 · M4-14 1,25·96 · M4-15 1,05·124 · M4-16 1,80·269. Humano a 2–3× o bot: **~1–1,5 h** para a região 4.

## Salas: ícone de ameaça, risco da carga, dano e tempo (tabela realista)

| Sala | Ícone (antes → medido) | Risco da carga | Visitas | Dano por visita | Tempo por visita (s) | % luta |
|---|---|---|---|---|---|---|
| R4-1 Ponte de Basalto | 1 | Archer, Smash | 5678 | 0.77 | 14.7 | 78 |
| R4-2 Respiros | 2 | Smash | 4431 | 1.70 | 9.7 | 87 |
| R4-3 Ninho de Salamandras | 2 | Thief, Smash | 4716 | 1.33 | 9.0 | 71 |
| R4-4 Oficina do Diabrete | 2 | Thief, Smash | 3309 | 0.90 | 11.9 | 83 |
| R4-5 Bancada dos Kobolds | 2 → **1** | Thief, Archer, Smash | 3874 | 1.10 | 13.9 | 46 |
| R4-6 Fundição | 3 → **2** | Smash | 3560 | 1.78 | 19.6 | 91 |
| R4-7 Guarda da Forja | 2 → **3** | Thief, Archer, Smash | 887 | 2.99 | 41.2 | 86 |
| R4-8 Emboscada de Brasa | 3 | Thief, Archer, Smash | 419 | 3.30 | 56.7 | 100 |
| R4-9 Sala das Correntes | 1 | Thief | 3379 | 0.18 | 5.5 | 12 |
| R4-10 Coração da Forja | 3 | Thief, Archer, Smash | 892 | 6.07 | 55.5 | 94 |
| S4-1 Cofre do Ferreiro | 1 | Thief | 2771 | 0.11 | 7.3 | 0 |
| M4 Pátio do Capataz | 3 | Thief, Archer, Smash | 398 | 3.01 | 47.7 | — |
| B4 Ninho do Dragão | 3 | Smash | 332 | 4.81 | 119.3 | — |

Ícone = terço do dano médio por visita (como a região 1): médias 0,68 / 1,43 / 4,12 (ícone 1 / 2 / 3), nenhuma sala passa da média do ícone vizinho. Lava ou respiro mostram Pancada (`RoomDef.Risk`). A R4-10 é a sala mais dura da região (Bruto, elemental, kobold e a emboscada no nível 26–31): é onde o bot mais morre no M4-16.

## Números (o que ficou)

| Coisa | Número |
|---|---|
| Lava | sólida para andar, transparente para tiro (núcleo); inimigo empurrado contra ela pelo dash: 3 de dano 1× por empurrão; brasa morre; salamandra, elemental, ovo e Dragão imunes |
| Respiro | ciclo 4 s = 2,2 apagado + 0,8 brilhando + 1,0 de fogo (fase da sala); fogo: courier 2, inimigo 3 (forja imune), 1× por erupção; carga no chão: 1 rachadura, gelo −25, barril detona, balão fura; no B4 só da fase 2 |
| Fogo no chão | rastro da salamandra 0,45 m/3 s a cada 0,5 s andando; poça do frasco 1,0 m/4 s (Capataz 1,2 m); brasa do sopro 0,7 m/2 s; courier: **1º dano depois de 0,5 s dentro**, depois 1 por segundo (Botas 5: a cada 2 s); inimigo 1/s (forja imune); carga: gelo −10/s, balão 1 furo/s, **barril fica com 1 s de pavio** |
| Salamandra | 4 de vida (sem escala), 2,4 m/s, circula a 2,5 m por 3 s, corta até 1,5 s, toque 1 (recarga 1,2 s) |
| Diabrete | 3 (escala), 2,0 m/s, fica a 4–7 m; troca com a carga no chão com linha livre (redemoinho 1 s, recarga 5 s; golpe no redemoinho cancela e atordoa 0,5 s; Contragolpe: atordoa 1 s e devolve a carga a 1,5 m); sem carga no chão some e surge 1 m atrás (aviso 0,5 s), arranha 1, recarga 3 s |
| Kobold incendiário | 4 (escala), 1,6 m/s, fica a 3,5 m+, frasco a 6 m (aviso 0,9 s, círculo de 1 m), 1 de dano + poça; recarga 3 s; o dash no arremesso cancela |
| Elemental / brasa | 10 / 4 (brasa sem escala); pancada 2 (aviso 0,8 s, 1,2 m) racha a carga no chão e tira 20 de gelo; morre e vira 2 brasas a ±0,8 m (respeita o teto de vivos; herdam a emboscada) |
| Ovo de salamandra | 2, choca em 6 s numa salamandra |
| Capataz Kobold | 18 (66 no M4-8); chicote 3 × 0,6 m (aviso 0,7 s): 2 de dano, puxa a carga 1,5 m para ele + 1 rachadura; frasco a cada 5 s (3,5 na fase 2) a até 8 m; apito a cada 14 s: até 2 kobolds; fase 2 (50%) alterna chicote e giro de 3 m (aviso 1 s) |
| Dragão Ancião | 48 (221 no M4-16), 0,8 m/s em y ≥ 9; sopro: cone de 60° e 6 m, aviso 1,4 s, 3 de dano, pilar faz sombra, brasas; recarga 3 s; ovos: 2 a cada 20 s (o 1º aos 10 s; 12 s na fase 3); fase 2 (60%): voo a cada 18 s (o 1º 4 s depois; 14 s na fase 3): decola 0,6 s, 2,5 s no ar (intocável, sombra a 2 m/s), pouso em 2,2 m com aviso de 1,2 s: 3 de dano e empurra a carga 2 m (+1 rachadura; para na lava com mais 1), cabeça baixa 3 s com dano ×2; fissuras ligam; fase 3 (30%): o sopro varre 90° em 1,5 s |
| Planta carnívora | ×1,30; no chão morde o inimigo mais perto a 1,2 m (aviso 0,3 s, 2 de dano, recarga 1 s; chefes também; o Dragão no ar não); o courier parado sem carga a 1,2 m por 0,8 s leva 1 (recarga 1,5 s); ladrão carregando leva 1/s e a solta |
| Mapa do tesouro | ×1,20; revela baús enterrados e a parede disfarçada (núcleo); ladrões +30% de velocidade atrás dele no chão; ladrão da carga sozinha em 6 s |
| Contratos | pagamento e nível do desenho (310–480; nível 16–31); tempos pela medição (decisão R4-7) |

## Decisões (com o porquê)

- **R4-1. Fogo no chão com 0,5 s de folga** (antes: dano ao pisar). Medido: com dano na hora, o fogo era o maior dano da região (até ~65% num contrato: rastro de salamandra em volta da carga) e a campanha **não fechava** (0 de 4; M4-16 0%). Com a folga, atravessar rápido não queima e ficar queima 1 por segundo — o contra-jogo do desenho ("não pousar ali; contornar") continua. Campanha: 30,8 → 18,0 tentativas.
- **R4-2. Fogo do chão e do respiro não racham a relíquia nas mãos** (o golpe no courier racha; queimadura no pé não). O desenho só fala da carga **no chão**. Medido: a relíquia quebrava em 80% (M4-4); agora 35%.
- **R4-3. Fogo no chão "acende o pavio"** do barril (fica com 1 s), como diz a nota do M4-11; respiro, frasco e sopro (fogo direto) detonam na hora, como o §1.1. Medido: 80% de explosão → 1%.
- **R4-4. Morte por respiro, fogo no chão e planta não conta como luta** (H1 mede a luta escolhida; o empurrão na lava conta, é o dash).
- **R4-5. O "passo" da planta é o dash.** No núcleo, andar perto da carga a pega de volta (alcance de 1,5 m+), então não dá para "pousar e dar um passo" andando: o bot (e a dica "dê um passo") usa o toque rápido, que larga a planta e afasta 3 m. Sem dash, andar pega a planta de volta (melhor que a mordida).
- **R4-6. Ícones de ameaça pela medição:** R4-5 2 → 1, R4-6 3 → 2, R4-7 2 → 3 (o Guarda entre respiros prende o courier perto do fogo).
- **R4-7. Tempos pela medição** (regra da região 1): ParTime = 1,6× a duração do bot (antes: 1,6× a estimativa do desenho, que era 25–90% maior), ovo 2×, gelo 2,2× (mantido 215 s), mensagem 2,5×. Antes → depois: M4-1 60→35 · M4-2 90→65 · M4-3 85→60 · M4-4 155→80 · M4-5 160→155 · M4-6 185→150 · M4-7 170→155 · M4-8 225→185 · M4-9 195→105 (ovo 245→135) · M4-10 190→170 · M4-11 195→205 · M4-12 85→55 (limite 130→85) · M4-13 195→130 · M4-14 205→155 · M4-15 210→180 · M4-16 355→415.
- **R4-8. Dragão:** o 1º voo vem 4 s depois de entrar na fase 2 e os 1ºs ovos aos 10 s (o desenho diz "a cada 18/20 s"; com o 1º só nesses tempos, a fase 2 passava sem voo). **Capataz:** o frasco só a até 8 m (não joga na entrada).
- **R4-9. Ganchos no núcleo (compartilhados):** `Run.HitMul` (`HitMulR2/R3/R4`: 0 = intocável — não é alvo e não leva dano; 2 = janela de dano), usado pelo Dragão no ar e de cabeça baixa (e pelas raias R2/R3); no `Bot`: `PlanR4` (antes do plano geral), `SteerR4` (a cada passo), `OnRoomR4`, risco de ladrão no Mapa na escolha de porta, e o passo 1 do plano geral não foge dos inimigos com aviso da região 4 (o `PlanR4` cuida). `Bot.PlayCampaign(..., from)` começa de um save e compra equipamento.
- **R4-10. Brasas nascem no `StepR4`**, não no meio do `Hit`: o bumerangue percorre a lista de inimigos e quebrava ao nascer brasa durante a volta.

## Antes e depois (evolução da medição, mesma partida)

| Etapa | Campanha (fecha · tentativas) | Pior contrato |
|---|---|---|
| Comportamento + bot sem contornar o fogo | 0 de 4 | M4-16 0%, M4-11 0% |
| + contornar a cada passo, sair do fogo para um lugar seguro | 4 de 4 · 30,8 | M4-11 8,3 tentativas, M4-16 7,3 |
| + R4-1 (folga do fogo), R4-2, R4-3, dash sem carga | 6 de 6 · 18,0 | M4-4 1,8 |
| + R4-4, R4-6, R4-7, planta mais perto (o bot travava ao lado da própria planta com o resgate de outra: 68 tentativas numa semente) | 20 de 20 · 17,9 | M4-16 1,8 |

## Riscos e pendências

- **Moedas paradas: ~21.600 no fim da região 4** (13.900 estimadas na entrada + 21.300 ganhas − 13.600 do Gancho e da Armadilha). O alvo do desenho (§10.3) era sobrar no máximo 1 nível de equipamento (~3.000). As alavancas do desenho, em ordem: (1) pagamento das regiões 3–4 −10% (aqui tira ~2.100); (2) baú −20% (~900 na região); (3) um ralo novo — **decisão do coordenador**. A região 4 sozinha deixa ~7.700 (ganha 21.300, só tem 13.600 para gastar).
- **Ponto de partida estimado:** guilda e armas no máximo, Fumaça e Botas no nível 5 e 13.900 moedas são hipótese; refazer com o save real do fim da região 3 quando a R3 medir.
- **Mais duras por tentativa:** M4-16 (61%), M4-4 relíquia (65%), M4-14 e M4-7 planta (78–80%), M4-15 bolo (88%). A campanha fecha com ≤ 1,8 tentativa em cada.
- **Duração:** 5 contratos do 4º em diante abaixo de 90 s (60–89 s). Alavanca, se o coordenador quiser: nível (vida) da região, que hoje sobe 16→31 (×2,8→×4,6) — subir mais aumenta a morte no M4-16.
- **H1 = 70%** com agressividade 0,5, no limite: a região tem muito perseguidor rápido (salamandra cortando, diabrete surgindo atrás, brasas) e o "pare e atire" de defesa conta como luta.
- **Gelo (raia R2):** o fogo direto daqui tira gelo pelo `MeltIce` da R2; o ×3 do cristal ainda olha só a lava (`NearLava`), não o fogo no chão nem o respiro — a R2 pode trocar por `run.NearFire(p)`.
- **Courier empurrado contra a lava (2 de dano)** não foi implementado: nada na região 4 empurra o courier (o pouso e o chicote só mexem na carga).
