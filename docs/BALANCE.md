# Balance da v0.1 — medição do bot (2026-10-09)

Fonte das regras e dos números iniciais: [`CONTRATO_V01.md`](CONTRATO_V01.md). Aqui ficam a medição do bot, os custos e as decisões tomadas no núcleo (`client/Assets/_DC/Scripts/Core/`), com o porquê. As decisões 1 e 2 já foram aceitas e levadas ao `CONTRATO_V01.md` pelo coordenador; a passada 2 (relíquia frágil e ícones) está nas decisões 18–19.

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

## Achados para o design (precisam de decisão do Vinicius)

- **Duração.** O bot fecha um contrato em 13–36 s e a campanha inteira em ~3 min de jogo. O GDD (§2, §6) conta com contratos de 4–6 min e sessões de 4–9 min. Mesmo que um jogador novo leve 2–3× o tempo do bot, um contrato fica em ~1 min. O formato do contrato (sala 9 × 14 que cabe na tela, 2–4 camadas) gera contratos curtos; para chegar a 4–6 min é preciso mais camadas, ondas por sala ou salas maiores. Decisão de design, não de número.
- **O ladrão nunca leva a carga do bot** (fuga 0–0,01 por contrato, 100% recuperada): fugir a 1,6 m/s + 1 s na borda dá tempo de sobra. Para humanos pode ser diferente; medir no diário (`thief_grab` × `cargo_drop` motivo 2).
- **O ícone conta o dano ao courier, não o risco para a carga.** Para a relíquia, as salas mais perigosas são as do ladrão (R3 ícone 1, R2 ícone 2), porque a queda racha; quem segue as caveiras leva a relíquia justo para elas. Proposta: um 2º ícone na porta (mão/ladrão), ou o ícone contar também o risco da carga. Mexe com o H2 ("a carga muda a rota").
- **H1 do bot** fica em 28–95% de salas com luta com agressividade 0,5: a relíquia faz o C7 lutar em 95% das salas (passa dos 70% do H1) e o C5 em 66%, qualquer que seja a agressividade. É o verbo funcionando, mas no playtest o H1 tem de ser lido por carga. O H1 de verdade é do playtest.
