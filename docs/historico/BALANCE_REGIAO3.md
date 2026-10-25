# Balance — região 3 "Cripta Assombrada" (raia R3, Leva B, 2026-10-11)

Fonte: [`CONTEUDO_REGIOES.md`](CONTEUDO_REGIOES.md) §1.1, §3, §5, §6 e §10–§11 (desenho, todo número era HIPÓTESE). Núcleo: `Core/Content/Regiao3.cs` (salas e contratos), `Core/Run.Regiao3.cs` (comportamentos, números em `Balance.*` da região 3), `Core/Bot.Regiao3.cs` (bot e medição). Testes: `Tests/EditMode/CoreTestsRegiao3.cs`.

Reproduzir: `dotnet test client/tools/coretests --filter Name~Tabela_Regiao3 --logger "console;verbosity=detailed"` imprime as tabelas abaixo (~25 s).

## Como medir

- **Estado inicial = fim estimado da região 2** (`Bot.Regiao3StartSave`). A raia R2 mede a região 2 em paralelo; até lá vale a estimativa do desenho (§8 e §10.3) mais a campanha da região 1 medida no `BALANCE.md`: guilda 8/8/8/8, armas no nível 3, 97 estrelas, Botas de lama destravadas e no nível 5 (os 3 projetos da região 2), 1 projeto da Bomba de fumaça (R2-7) e **~3.000 moedas** (1.089 do fim da região 1 + ~4 × 3.768 de pagamento da região 2 − 4.237 da guilda − 2.100 das armas − 6.800 das Botas 2–5). Trocar pelo save medido da R2 quando ela publicar.
- **Campanha:** 10 sementes (6100–6109) com `Bot.PlayCampaign(..., region: 3, from: Regiao3StartSave)`: joga o próximo contrato, refaz o que falha, troca a arma pela carga e compra o que couber (guilda, armas e equipamento destravado) antes de cada contrato.
- **Tabela realista:** de cada campanha, o save de antes da 1ª tentativa de cada contrato e 10 sementes do bot a partir dele = **100 tentativas por contrato**.
- **Bot:** o mesmo da região 1 (agressividade 0,5, reação 0,25 s ± 0,08, ganância 0,5) mais as regras da região (`Bot.Regiao3.cs`): sai das faixas e círculos de aviso (`run.Warns`); com o espelho inteiro encara o arqueiro, o cuspe e o raio, rachado sai de lado; com espelho ou lanterna não usa dash para fugir de golpe; leva a lanterna apagada até a tocha; pega o balão com ~2,3 s no chão; leva a carga até a pilha de ossos (a 3 m o esqueleto não volta) e espera o esqueleto chegar a ~2,6 m antes de pousar; agarrado pela múmia pousa e luta (dash com vida ≤ 2); com o fantasma levando a carga anda até ela (pegar de volta não racha); com a relíquia e fantasma perto pousa e fica; no escuro chega perto do Guarda/chefe fora da luz (com a lanterna para a ~3,7 m: pousada ela ilumina 4 m e o Necromante só salta a 3,4 m). Não desvia de teia nem de gárgula no caminho (ponytail).

## Alvos e situação

| Alvo | Medido | Situação |
|---|---|---|
| fechar a campanha da região | **100%** das 10 campanhas · 17,2 tentativas · 0,3 morte · **25,8 min** de jogo do bot | ok |
| 1,5–3 min por contrato do 4º em diante | entregas da campanha de **91 a 157 s** (média 110 s); mensagem M3-10 em 49 s | ok (a mensagem é corrida por desenho, como C10/C16/C22) |
| H1: luta escolhida em 30–70% das salas | **65%** (agressividade 0,5; a região 1 dá 72%) | ok, perto do teto |
| moedas paradas no fim da região | **14.077** (3.000 de entrada + 17.877 ganhas − 6.800 da fumaça 2–5) | **acima** do alvo do desenho (≤ 1 nível de equipamento): ver "Economia" |

## Tabela por contrato (100 tentativas cada)

| Contrato | Carga | Salas | % concluído | % morte | % falha de carga | duração média (s) | mediana (s) | % salas com luta | moedas médias | estrelas | 3 estrelas % | rachaduras |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| M3-1 | Lanterna | 3 | 100 | 0 | 0 | 22 | 22 | 52 | 865 | 2.96 | 96 | 0.00 |
| M3-2 | Caixa | 4 | 99 | 0 | 1 | 35 | 37 | 53 | 767 | 2.97 | 99 | 0.00 |
| M3-3 | Espelho encantado | 4 | 100 | 0 | 0 | 31 | 29 | 59 | 870 | 2.04 | 8 | 1.08 |
| M3-4 | Ovo | 7 | 100 | 0 | 0 | 92 | 91 | 66 | 1097 | 2.95 | 95 | 0.06 |
| M3-5 | Balão de gás | 7 | 100 | 0 | 0 | 89 | 90 | 65 | 1252 | 2.90 | 90 | 0.10 |
| M3-6 | Relíquia frágil | 7 | 68 | 0 | 32 | 98 | 104 | 98 | 820 | 1.52 | 16 | 1.79 |
| M3-7 | Lanterna | 7 | 97 | 3 | 0 | 109 | 110 | 70 | 1072 | 2.88 | 94 | 0.05 |
| M3-8 | Caixa (+ Viúva) | 7 | 98 | 2 | 0 | 125 | 123 | 63 | 971 | 2.57 | 61 | 0.42 |
| M3-9 | Espelho encantado | 7 | 90 | 2 | 8 | 94 | 96 | 59 | 1038 | 1.85 | 5 | 1.37 |
| M3-10 | Mensagem urgente | 7 | 100 | 0 | 0 | 46 | 46 | 28 | 973 | 2.99 | 99 | 0.01 |
| M3-11 | Balão de gás | 7 | 100 | 0 | 0 | 102 | 104 | 74 | 1191 | 2.74 | 74 | 0.28 |
| M3-12 | Coração arcano | 7 | 100 | 0 | 0 | 105 | 105 | 73 | 1498 | 2.81 | 81 | 0.23 |
| M3-13 | Barril de pólvora | 7 | 80 | 9 | 11 | 114 | 112 | 65 | 980 | 2.18 | 59 | 0.40 |
| M3-14 | Gaiola do grifinho | 7 | 99 | 1 | 0 | 94 | 95 | 62 | 1210 | 2.73 | 76 | 0.31 |
| M3-15 | Espelho encantado | 7 | 77 | 5 | 18 | 103 | 106 | 67 | 874 | 1.53 | 0 | 1.68 |
| M3-16 | Lanterna (+ Necromante) | 6 | 91 | 9 | 0 | 166 | 167 | 69 | 1203 | 2.23 | 41 | 0.85 |

"Salas" conta até a entrega, com a D3. Falha de carga: relíquia quebrada (M3-6), espelho quebrado (M3-3/9/15, quase sempre por reflexo), barril (M3-13). O espelho de 3 estrelas é raro de propósito: refletir racha, e a 2ª estrela pede 0 rachadura.

**Campanha (10 sementes):** fecha 100% · 17,2 tentativas · 0,3 morte · 25,8 min · 17.877 moedas ganhas, 6.800 gastas · 41,7 de 48 estrelas. Tentativas · tempo da entrega (s): M3-1 1,00·19 · M3-2 1,00·30 · M3-3 1,00·31 · M3-4 1,00·97 · M3-5 1,00·92 · M3-6 1,20·108 · M3-7 1,00·95 · M3-8 1,00·149 · M3-9 1,10·96 · M3-10 1,00·49 · M3-11 1,00·111 · M3-12 1,00·92 · M3-13 1,10·121 · M3-14 1,00·91 · M3-15 1,70·111 · M3-16 1,10·157.

**Tempos dos contratos** (mesma regra da região 1): 3ª estrela = 1,6 × a mediana; ovo = 2×; mensagem = 2,5×, arredondados a 5 s. M3-1 35 · M3-2 60 · M3-3 45 · M3-4 145 (ovo 180) · M3-5 145 · M3-6 165 · M3-7 175 · M3-8 195 · M3-9 155 · M3-10 75 (limite 115) · M3-11 165 · M3-12 170 · M3-13 180 · M3-14 150 · M3-15 170 · M3-16 265.

## Salas: ícone de ameaça, risco da carga, dano e tempo

| Sala | Ícone | Risco da carga | Visitas | Dano por visita | Tempo por visita (s) | % luta |
|---|---|---|---|---|---|---|
| R3-1 Corredor das Tochas | 1 | — | 983 | 0.58 | 9.6 | 67 |
| R3-2 Ossário | 1 | Ladrão | 1618 | 0.84 | 15.1 | 55 |
| R3-3 Teia Grande | 1 | — | 972 | 0.18 | 8.5 | 50 |
| R3-4 Capela Fantasma | 2 | Ladrão, Arqueiro | 931 | 1.31 | 9.4 | 81 |
| R3-5 Sarcófagos | 1 | — | 1461 | 0.52 | 16.5 | 53 |
| R3-6 Galeria das Gárgulas | 2 | Arqueiro, Pancada | 730 | 1.62 | 12.1 | 62 |
| R3-7 Emboscada na Cripta | 2 | Ladrão | 244 | 1.09 | 23.8 | 91 |
| R3-8 Guardião do Mausoléu | 2 | Ladrão | 197 | 1.70 | 22.8 | 87 |
| R3-9 Biblioteca Sombria | 2 | Ladrão | 875 | 1.77 | 17.3 | 83 |
| R3-10 Nave da Catedral | 3 | Ladrão, Arqueiro, Pancada | 147 | 4.93 | 35.2 | 100 |
| S3-1 Cripta do Tesouro | 1 | — | 796 | 0.26 | 11.1 | — |
| M3 Covil da Viúva | 3 | Ladrão | 100 | 4.44 | 51.6 | — |
| B3 Altar do Necromante | 3 | Arqueiro | 100 | 5.78 | 104.1 | — |

Médias por ícone: 0,53 → 1,50 → 4,93 (folga ≥ 0,3) e nenhuma sala passa da média do ícone vizinho (`IconeDeAmeaca_Regiao3_AcompanhaODanoMedido`). **Atenção:** o bot mede com as Botas no nível 5 (teia não prende nem freia); sem as botas a R3-3 dói mais (o ícone 1 dela vale para quem já tem as botas da região 2).

## Economia

O bot entra com ~3.000 e sai com **~14.000 moedas paradas**. A região 3 paga 4.672 de base (desenho §10.3) e a Bolsa no máximo leva a ~17.900 por campanha; o único ralo novo é a Bomba de fumaça (3º projeto na S3-1; 600 + 1.200 + 2.000 + 3.000 = 6.800). O Gancho e a Armadilha só destravam na região 4 (13.600 de ralo), então a sobra é "estacionada" para lá. As alavancas do desenho (pagamento das regiões 3–4 −10% ≈ −1.800 aqui; baú −20%) não fecham a diferença sozinhas: é decisão do coordenador (ralo novo ou destravar o Gancho na região 3).

**Leva C (raia E), resolvido em `BALANCE.md`, "Economia da campanha":** equipamento a 12.500 por peça, o Gancho completa aqui (3º projeto na R3-5) e a Fumaça tem um baú de reserva na R3-6. Na campanha completa, do zero, a região 3 termina com ~2.400 moedas (máx. 4.800) e ~700 sem nada para comprar.

## Ajustes sobre o desenho (com o porquê)

- **R3.1 Morcego fora da R3-1, R3-3 e R3-5.** Era a 1ª morte (e o que fazia a sala "contar luta") em 41/52, 21/24 e 47/54 visitas: H1 de 77% para 65%. As salas ficam com o verbo delas (esqueleto, aranhas, múmias).
- **R3.2 Esqueletos erguidos pelo Necromante com vida sem nível** (5, não 5 × 4,12 = 21). Com a vida do nível o bot morria 100% no B3 (as ondas de 3 a cada 15 s tanqueavam). Agora o B3 leva ~104 s por visita e o M3-16 entrega 91%.
- **R3.3 Pegar de volta a carga possuída repele o fantasma 1,5 m** (e o toque recarrega 1,5 s). Sem isso, o fantasma colado tocava o courier no mesmo passo e a relíquia rachava de qualquer jeito: o M3-6 entregava 23%; agora 68%.
- **R3.4 Rotas.** M3-4 e M3-5 ganharam uma 6ª camada (o desenho tinha 5 e o bot entregava em 72 s); M3-5 troca R3-1 por R3-9 na 2ª camada; M3-7 ganha a 6ª camada (R3-5/R3-2; era 85 s); M3-9 começa na R3-2 em vez da R3-1 (85 s); M3-16 começa na R3-1 em vez da R3-10 e a 2ª camada vira R3-4/R3-8 (era 202 s e 26% de morte: a R3-10 tira 4,9 de vida por visita antes do chefe). O pagamento somado (4.672) não mudou.
- **R3.5 Ícones de ameaça pela medição** (dano por visita): R3-2, R3-3 e R3-5 descem para 1; R3-7 (emboscada) fica em 2; R3-10 é a única 3.
- **R3.6 Avisos em linha não usam o `EnemyState.Telegraph`.** Cuspe, mergulho da gárgula, leque e fio da Viúva e raio do Necromante ficam em `Lurk` + `R3Foe.Act` + `run.Warns`; o `Telegraph` fica só para o golpe corpo a corpo (mordida, agarrão). O bot e a dica "dash" da região 1 tratam `Telegraph` como golpe de alcance `Reach` (6 m na gárgula, 9 m no raio) e fugiam/davam dash de qualquer lugar. O fantasma possuindo fica em `Grab` (o dash cancela, como no ladrão).
- **R3.7 Números que o desenho não deu** (hipóteses): Viúva sobe ao teto a cada 8 s na fase 2 (o 1º 1 s depois dos 50%), fio com recarga de 4 s, 1º leque em 2 s; Necromante ergue a 1ª onda em 5 s; teia "dentro" com o centro do courier a raio + 0,2 m; o espelho reflete a flecha que chegaria no próximo passo (até ~0,5 m antes do contato).
- **R3.8 Núcleo (edições pontuais, marcadas no código):** `Cargo.PickExtra` (teia +1 s, casulo 1,5 s; o pegar usa o mesmo `PickT` da caixa pesada); gancho `DropR3` no `ToGround` (queda apaga a lanterna e racha o espelho); `Run.Untouchable(e)` (o bot e a View leem o `HitMul` 0); `Breaks` com espelho e balão; o ladrão e o Capitão ignoram o balão no chão; `InLight` só conta a lanterna nas mãos se acesa e subtrai `LightCut`/`TorchCut`; o bot ignora intocáveis no `Nearest` e ganhou o gancho `PlanR3`.

## Pendências e riscos

- **Estado inicial estimado**: trocar `Regiao3StartSave` pelo save medido da R2 (moedas, Botas, estrelas). Com menos moedas ou sem as Botas, a R3-3 e a M3 (teias) ficam mais duras.
- **Não implementado (ponytail):** explosão não espalha os ossos (não há gancho de explosão no núcleo; o dash espalha); o grito da gaiola (R2) não acorda a gárgula (o núcleo só tem `Wake` para dormindo; a R2 pode chamar um gancho se quiser); `Cargo.Out` não vai para o save (retomar a sala reacende a lanterna).
- **Mais duros que a média:** M3-6 relíquia 68%, M3-15 espelho 77%, M3-13 barril 80% (morte 9%). Dentro da faixa de sanidade (≥ 40%), mas são candidatos a ajuste fino no playtest.
- **Balanço do bot ≠ humano:** o bot vê inimigo fora da luz (só não atira nele) e não desvia de teia; o humano deve levar 2–3× o tempo do bot (~50–80 min na região 3).
