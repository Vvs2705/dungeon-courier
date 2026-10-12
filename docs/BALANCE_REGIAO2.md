# Balance — Região 2 "Minas Alagadas" (raia R2, Leva B, 2026-10-11)

Fonte do desenho: [`CONTEUDO_REGIOES.md`](CONTEUDO_REGIOES.md) §1.1, §2, §5, §6. Conteúdo em `client/Assets/_DC/Scripts/Core/Content/Regiao2.cs`, comportamento em `Run.Regiao2.cs`, bot em `Bot.Regiao2.cs`, testes em `Tests/EditMode/CoreTestsRegiao2.cs`. **Todo número do desenho foi mantido** (vida, velocidade, alcance, telegraph, recarga, pagamento, tempos dos contratos, `PayMul` das cargas); o que mudou está em "Decisões", com o porquê.

## Como medir

- **Ponto de partida = fim da região 1 medido pelo bot** (`BALANCE.md`, lançamento): 63 estrelas (`Sim.DevStars(63)`), guilda 7/7/7/7, armas 2/2/2, sobra de 1.089 moedas (`BalanceTestsRegiao2.EndOfRegion1`).
- **Campanha da região 2:** `Bot.PlayCampaign(semente, region: 2, from: save)` (6 sementes, 6000–6005): joga M2-1…M2-16 em ordem, refaz o que falha, compra o mais barato que cabe (guilda, arma, equipamento destravado) e troca a arma pela carga.
- **Tabela realista:** cada contrato medido a partir do save que a campanha tinha antes da 1ª tentativa dele, 6 campanhas × 10 sementes = 60 tentativas por contrato. **H1:** a mesma coisa com agressividade 0,1 e 0,9 (5 sementes cada), do M2-4 em diante.
- Teste que imprime tudo: `BalanceTestsRegiao2.Campanha_Regiao2_PeloBot_FechaOs16_DuracaoH1EMoedas` e `IconeDeAmeaca_Regiao2_AcompanhaODanoMedido` (`dotnet test client/tools/coretests --filter BalanceTestsRegiao2 --logger "console;verbosity=detailed"`).

## Antes e depois

| Campanha do bot (6 sementes) | bot inicial (desenho puro) | desenho + bot corrigido | **depois** (ícones medidos) |
|---|---|---|---|
| fecha a região | **4 de 6** | 6 de 6 | **6 de 6 (100%)** |
| tentativas / mortes | 63,2 / 50,0 | 18,3 / 0,7 | **17,5 / 0,7** |
| tempo de jogo do bot | 29,0 min | 20,1 min | **21,4 min** |
| duração média M2-4…M2-16 sem a mensagem | — | 79 s | **84 s** |
| H1 (% de salas com luta) agressividade 0,1 / 0,5 / 0,9 | — | 55 / 73 / 84% | **55 / 69 / 82%** |
| M2-4 relíquia / M2-6 barril entregues | 33% / 57% | 50% / 72% | **60% / 77%** |
| moedas paradas no fim da região | — | 3.401 | **3.679** (nada para comprar) |
| estrelas no fim (63 da região 1 + região 2) | 93,2 | 107,5 | **105,8** |

- **Bot inicial:** 2 campanhas presas no M2-3 (148 mortes): com o cristal o bot oscilava entre "parar no baú" e "parar na poça" com um morcego batendo e nunca atirava; e o M2-10 travava (5 de 60) porque o **dash da gaiola atravessava a porta sem a carga** (bug do núcleo, decisão R2-3).
- **Experimento de vida (descartado):** nível dos contratos +3 e +6 deu 81 s e 84 s de duração média (contra 79 s): a vida não é a alavanca da duração na região 2 (ver "Duração").

## Tabela por contrato (depois)

| Contrato | Carga | Salas | % concluído | % morte | % falha de carga | duração média (s) | mediana (s) | % salas com luta | moedas médias | estrelas | 3 estrelas % | rachaduras |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| M2-1 | Caixa | 3 | 100 | 0 | 0 | 17 | 17 | 70 | 580 | 3.00 | 100 | 0.00 |
| M2-2 | Ovo | 4 | 100 | 0 | 0 | 24 | 25 | 70 | 719 | 3.00 | 100 | 0.00 |
| M2-3 | Cristal de gelo | 4 | 100 | 0 | 0 | 27 | 26 | 74 | 891 | 2.98 | 98 | 0.02 |
| M2-4 | Relíquia frágil | 6 | 60 | 0 | 40 | 54 | 56 | 93 | 541 | 1.35 | 15 | 1.77 |
| M2-5 | Coração arcano | 6 | 97 | 3 | 0 | 54 | 53 | 72 | 905 | 2.75 | 82 | 0.22 |
| M2-6 | Barril de pólvora | 7 | 77 | 2 | 22 | 86 | 85 | 66 | 775 | 2.22 | 68 | 0.05 |
| M2-7 | Cristal de gelo | 6 | 93 | 5 | 2 | 88 | 87 | 76 | 907 | 2.48 | 63 | 0.43 |
| M2-8 | Caixa (Rainha) | 7 | 98 | 2 | 0 | 93 | 95 | 77 | 928 | 2.83 | 87 | 0.12 |
| M2-9 | Mensagem urgente | 7 | 100 | 0 | 0 | 27 | 26 | 19 | 776 | 3.00 | 100 | 0.00 |
| M2-10 | Gaiola do grifinho | 6 | 97 | 3 | 0 | 64 | 62 | 73 | 1030 | 2.87 | 93 | 0.03 |
| M2-11 | Coração arcano | 7 | 95 | 3 | 2 | 77 | 80 | 78 | 1116 | 2.68 | 78 | 0.43 |
| M2-12 | Bolo de casamento | 7 | 93 | 7 | 0 | 88 | 90 | 74 | 687 | 1.87 | 0 | 7.30 |
| M2-13 | Caixa pesada | 7 | 93 | 3 | 3 | 84 | 89 | 70 | 1175 | 2.62 | 75 | 0.23 |
| M2-14 | Gaiola do grifinho | 7 | 97 | 2 | 2 | 74 | 72 | 74 | 1056 | 2.53 | 60 | 0.53 |
| M2-15 | Cristal de gelo | 7 | 90 | 5 | 5 | 110 | 114 | 68 | 1096 | 2.57 | 77 | 0.17 |
| M2-16 | Caixa (Golem) | 7 | 78 | 20 | 2 | 129 | 131 | 67 | 736 | 1.67 | 10 | 2.03 |

**Campanha:** tentativas · tempo da entrega (s): M2-1 1,00·17 · M2-2 1,00·24 · M2-3 1,00·27 · M2-4 1,50·71 · M2-5 1,17·61 · M2-6 1,50·106 · M2-7 1,00·87 · M2-8 1,00·93 · M2-9 1,00·26 · M2-10 1,00·62 · M2-11 1,17·80 · M2-12 1,00·94 · M2-13 1,00·89 · M2-14 1,00·78 · M2-15 1,00·128 · M2-16 1,17·141.

## Salas: ícone, dano, tempo (depois)

Dano por visita da tabela acima (60 tentativas por contrato); tempo e % de luta de uma medição de 8 × 12 com os mesmos ícones.

| Sala | Ícone (desenho → medido) | Risco da carga | Visitas | Dano por visita | Tempo por visita (s) | % luta |
|---|---|---|---|---|---|---|
| R2-1 Galeria Alagada | 1 → 1 | ladrão | 911 | 0,25 | 4,4 | 60 |
| R2-8 Eclusa do Guarda | 2 → **1** | ladrão, pancada | 454 | 0,45 | 27,2 | 76 |
| R2-5 Lodaçal do Sapo | 2 → **1** | ladrão | 896 | 0,47 | 10,4 | 45 |
| R2-6 Corredor dos Besouros | 3 → **2** | pancada | 505 | 0,66 | 12,6 | 94 |
| R2-2 Trilho do Vagonete | 2 → 2 | ladrão, arqueiro, pancada | 209 | 0,83 | 5,8 | 72 |
| R2-7 Cruzamento de Trilhos | 2 → 2 | ladrão, arqueiro, pancada | 434 | 0,93 | 7,3 | 84 |
| R2-4 Toca da Toupeira | 2 → 2 | pancada | 516 | 1,29 | 8,7 | 83 |
| R2-3 Mina Adormecida | 1 → **3** | arqueiro | 406 | 1,33 | 11,8 | 90 |
| R2-9 Desmoronamento | 3 → 3 | arqueiro, pancada | 50 | 1,34 | 21,1 | 92 |
| R2-10 Salão da Bomba d'Água | 3 → 3 | ladrão, arqueiro, pancada | 281 | 2,85 | 23,9 | 56 |
| S2-1 Veio de Ouro | 0 | arqueiro | — | 0,04 | 8,1 | — |
| M2 Ninho da Rainha | 3 | ladrão | — | 3,41 | 50,3 | — |
| B2 Câmara do Golem | 3 | pancada | — | 5,84 | 51,6 | — |

Médias por ícone: 0,39 → 0,93 → 1,84 (folga ≥ 0,3) e nenhuma sala passa da média do ícone vizinho (`IconeDeAmeaca_Regiao2_AcompanhaODanoMedido`).

## Duração (alvo do bot: 1,5–3 min do 4º contrato em diante)

| Faixa | Medido | Alvo | Situação |
|---|---|---|---|
| M2-1…M2-3 (tutoriais) | 17–27 s | curtos | ok |
| M2-4…M2-16 sem a mensagem | 54–129 s (**média 84 s**) | 90–180 s | **abaixo** (como o C4–C12 da região 1, média 76 s) |
| M2-9 (mensagem) | 27 s | curta por desenho | ok (a mensagem abre o Guarda e as emboscadas) |
| M2-8 (Rainha) / M2-16 (Golem) | 93 s / 129 s | — | dentro |

**Por que fica abaixo:** o bot chega à região 2 com a guilda em 7/8 e a fecha em 8/8 com as armas no nível 3; com isso as salas que se atravessam (R2-1, R2-2, R2-7: 4–8 s por visita) dominam, e só a Eclusa do Guarda e as duas emboscadas passam de 20 s. Subir a vida (nível +3/+6) mudou a média em 2–5 s: o que alonga contrato aqui é **luta obrigatória** (Guarda/emboscada) ou **mais salas**, decisão de desenho (ver recomendações).

## H1 (lutar é escolha)

Agressividade 0,1 / 0,5 / 0,9 = **55% / 69% / 82%** das salas com luta (varia 27 pontos; o teste pede > 15). Com 0,5 fica **dentro** do alvo de 30–70%, na borda (a região 1 mede 72%). As salas de perseguidor (R2-6 besouros 94%, R2-3 mineiros acordados 90%) quase sempre têm luta; o sapo (R2-5) e o salão com emboscada de carga no chão (R2-10) ficam em 45–56%.

## Moedas (1 moeda)

- Região 2 inteira: **ganha ~15.057**, gasta ~12.467 (do fim da região 1: guilda 7→8 = 4.100, armas 2→3 = 2.700, **Botas de lama** 1→5 = 6.800 — os 3 projetos estão nos baús da R2-1, R2-5 e S2-1). Antes do M2-16 a campanha já comprou tudo o que existe (guilda 8/8/8/8, armas 3/3/3, Botas 5); a Bomba de fumaça tem 1 de 3 projetos (R2-7).
- **Moedas paradas no fim da região: ~3.700** (1,6–3,5 mil por campanha) **sem nada para comprar** até a região 3 dar projetos. É o risco do `CONTEUDO_REGIOES.md` §10.3 começando na região 2.

## Decisões da raia R2 (com o porquê)

- **R2-1. Números do desenho mantidos.** Vida, velocidade, alcance, telegraph e recarga dos 5 inimigos e dos 2 chefes, `PayMul` das 3 cargas (1,35 / 1,45 / 1,30) e pagamentos/tempos dos 16 contratos como no `CONTEUDO_REGIOES.md`. `Provisional` desligado.
- **R2-2. Golem com vida em meios pontos** (`EnemyDef` 80 = 40 de vida): o ponto fraco do desenho é "de frente metade, pelas costas ×2". O gancho `HitMul` do núcleo só multiplica por inteiro, então o tiro de frente tira 1 meio ponto e o de trás (ou atordoado) 4; o vagonete tira 6 cheios (12). A barra da View (`Hp / Def.Hp × HpMul`) continua certa.
- **R2-3. Bug do núcleo corrigido:** a porta aberta no passo anterior deixava atravessar o courier cujo dash daquele passo acabara de largar a carga — ele saía da sala sem ela e o contrato travava (achado pelo dash que acalma a gaiola; afetaria qualquer jogador que desse dash a um passo da porta). Agora a porta pede a carga nas mãos no instante de atravessar (`Run.MoveCourier`; teste `Porta_DashQueLargaACargaNaoAtravessa`). A região 1 não muda (o bot nunca fazia isso).
- **R2-4. Ícones de ameaça pela medição** (o desenho já previa): R2-3 1→3 (o baú ao lado de quem dorme acorda a mina), R2-6 3→2 (abrindo distância o besouro morre longe), R2-5 e R2-8 2→1.
- **R2-5. Gelo:** a integridade do cristal é a faixa do gelo (≥ 70 = 0 rachadura, ≥ 40 = 1, senão 2), escrita em `Cargo.Cracks` a cada passo (entra na entrega, nas estrelas e no save). Golpe na carga vira −10 de gelo em vez de rachadura. O gelo fica com o `Run` (por id de carga); ao retomar do save recomeça no topo da faixa gravada.
- **R2-6. Rainha na água:** o monte sai na borda seca a 0,75 m da poça e não empurra o que está na água. **Toupeira** espera a 1,5 m da borda (como o desenho).
- **R2-7. Bot da região 2** (`Bot.Regiao2.cs`, ganchos `PlanR2` antes e `AfterR2` depois do plano geral): sai do círculo da picareta, do monte, do pavio, da patada/pancada e da faixa da investida; sai do trilho com o sino e espera o vagonete para cruzar; abre distância do besouro (morto perto explode no courier); passa a mais de 2 m de quem dorme quando não escolheu lutar; não pousa no anel do sapo; pousa na poça contra a toupeira e com o cristal (só para lutar de longe; perseguido ou no baú, pousa ali); recongela o cristal abaixo de 85% (até 97%); na Rainha luta de dentro da poça; acalma a gaiola com o dash quando o grito acordaria ou atrairia alguém e não há ladrão; pousa o coração como isca com 3+ perseguidores. O passo geral 1 (fugir de telegraph) ignora os inimigos da região 2 (os avisos deles são no chão: círculo, monte, faixa).

## Achados e pendências (para o coordenador)

- **Duração abaixo do alvo** (84 s contra 90–180 s): alavancas de desenho, não de número — mais uma sala nos contratos M2-4…M2-7 ou Guarda/emboscada em mais salas (hoje só R2-8, R2-9, R2-10).
- **Moedas paradas (~3.700) e o ralo acabando na região 2:** as Botas saem inteiras na região 2 e a Fumaça só completa na região 3. Alavancas do §10.3 (baú −20%, pagamento das regiões 3–4 −10%) ou trazer um projeto da Fumaça para a região 2.
- **Relíquia (M2-4) a 60%** e **barril (M2-6) a 77%**: os golpes vêm do besouro, dos mineiros acordados e da toupeira sob o courier; a campanha refaz 1,5× em média. Aceitável para o 4º contrato? (A região 1 pede ≥ 40%.)
- **Bolo (M2-12) com 7,3 rachaduras em média e 0% de 3 estrelas:** o bot leva a bombinha com o bolo (regra da região 1) e o ladrão derrubado racha o bolo; não mexi para a região 1 não mudar.
- **Frágil + cristal:** com o modificador Frágil, um golpe no cristal com gelo entre 40 e 70% leva a 2 rachaduras e quebra (em vez de −10 de gelo). Borda rara; documentada.
- **Equipamento na medição:** a campanha (`Bot.PlayCampaign`, mudança da raia R4) compra e equipa o 1º equipamento destravado; com as Botas de lama (nível 5 no meio da região) a água deixa de frear o courier do bot, então o verbo da água pesa menos na segunda metade da medição. O bot não escolhe equipamento pela sala.
- **Leva C (raia E), resolvido em `BALANCE.md`, "Economia da campanha":** duração com a 6ª camada no M2-5, M2-7 e M2-10 e R2-8/R2-10 na 5ª do M2-14 (média 84 → 92 s nesta medição; 94 s a partir da campanha completa; M2-4 igual); moedas com o equipamento a 12.500 por peça e 4 baús para 3 projetos (Botas também na R2-4): a campanha completa sai da região 2 com ~2.300 moedas, guardando para as Botas 5.
