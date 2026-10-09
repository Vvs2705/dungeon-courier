# Balance da v0.1 — medição do bot (2026-10-09)

Fonte das regras e dos números iniciais: [`CONTRATO_V01.md`](CONTRATO_V01.md). Aqui ficam a medição do bot, os custos e as decisões tomadas no núcleo (`client/Assets/_DC/Scripts/Core/`), com o porquê. Os números do contrato que mudaram estão em "Decisões"; o `CONTRATO_V01.md` não foi editado (a mudança está proposta ao coordenador).

## Como medir

- `dotnet test client/tools/coretests --logger "console;verbosity=detailed"` imprime a tabela abaixo (teste `BalanceTests.Tabela_ParaBalanceMd`).
- **Bot padrão:** agressividade 0,5; reação 0,25 s ± 0,08 s (o plano só é refeito nesse intervalo: o handicap humano); ganância 0,5; guilda nível 0; todos os contratos liberados; 200 sementes (base 1000). Contrato jogado isolado.
- **Campanha:** do zero, joga o próximo contrato bloqueado, refaz o que falha (aceitando o resgate quando há pendente) e compra o upgrade mais barato que couber antes de cada contrato. 100 sementes (base 5000).
- Determinístico: mesma semente = mesmo resultado (teste `Determinismo_MesmaSementeMesmoResultado`).

## Tabela por contrato (bot padrão, 200 sementes, guilda nível 0)

| Contrato | Carga | Camadas | % concluído | % morte | % falha de carga | duração média (s) | % salas com luta | moedas médias | vida na entrega |
|---|---|---|---|---|---|---|---|---|---|
| C1 | Caixa | 2 | 100 | 0 | 0 | 14.2 | 33 | 72 | 4.9 |
| C2 | Caixa | 2 | 99 | 2 | 0 | 12.8 | 55 | 81 | 3.3 |
| C3 | Ovo | 2 | 96 | 5 | 0 | 13.7 | 35 | 94 | 3.6 |
| C4 | Caixa | 3 | 96 | 5 | 0 | 21.8 | 51 | 113 | 3.5 |
| C5 | Relíquia frágil | 3 | 96 | 5 | 0 | 19.2 | 43 | 152 | 2.7 |
| C6 | Ovo | 3 | 86 | 15 | 0 | 22.5 | 74 | 129 | 1.7 |
| C7 | Relíquia frágil | 3 | 63 | 37 | 0 | 18.5 | 62 | 133 | 1.9 |
| C8 | Caixa | 4 | 61 | 39 | 1 | 35.4 | 72 | 126 | 1.8 |

"Camadas" conta as salas até a entrega (o C8 inclui a sala do chefe). "Moedas médias" = pagamento + resgate + baús, média de todas as tentativas (falhas pagam só o baú). "% salas com luta" = salas R1–R6 em que o bot matou ao menos 1 inimigo não-Guarda (`room_fight_choice`, H1). Em C1–C8 a soma de concluído + morte + falha pode passar de 100 por arredondamento.

**Campanha (100 sementes):** fecha os 8 contratos em 100% · 8,2 tentativas · 0,2 morte · **3,0 min de jogo do bot no total** · 1.228 moedas ganhas · níveis Vida/Dash/Bolsa 3,0/3,0/2,8 ao fim (de 5/5/5). Tentativas por contrato: C1 1,00 · C2 1,01 · C3 1,00 · C4 1,03 · C5 1,01 · C6 1,02 · C7 1,01 · C8 1,11.

### Lutar é escolha (H1): varredura de agressividade (200 sementes, nível 0)

| Contrato | % luta (agr. 0) | % luta (agr. 0,5) | % luta (agr. 1) | % concluído (agr. 0) | % concluído (agr. 1) |
|---|---|---|---|---|---|
| C1 | 23 | 33 | 43 | 100 | 100 |
| C2 | 29 | 55 | 82 | 100 | 89 |
| C3 | 21 | 35 | 50 | 100 | 91 |
| C4 | 32 | 51 | 73 | 97 | 91 |
| C5 | 30 | 43 | 63 | 100 | 88 |
| C6 | 55 | 74 | 94 | 93 | 83 |
| C7 | 40 | 62 | 87 | 66 | 66 |
| C8 | 48 | 72 | 95 | 51 | 60 |

O % de luta responde à agressividade (o teste `Lutar_EEscolha_...` exige ≥ 15 pp entre 0,1 e 0,9) e passar direto é viável no começo (C1–C3 ≥ 99% sem lutar por escolha). Mesmo com agressividade 0 há 21–55% de salas "com luta": morcegos que chegam enquanto o courier está parado no Guarda ou no baú e ladrões abatidos ao pegar a carga. Lutar mais custa vida no começo (C2: 100% → 89%) e compensa no fim (C8: 51% → 60%).

### Dano por visita de sala (bot padrão)

| Sala | Ícone de ameaça | Dano médio por visita | Fonte |
|---|---|---|---|
| R1 Guarda | 1 | 0,0 | o bot atira de fora do golpe |
| R3 Baú | 1 | 1,1 | 1 morcego |
| R2 1º Ladrão | 2 | 1,3–1,5 | 2 morcegos |
| R4 Enxame | 2 | 2,2–2,7 | 4 morcegos |
| R5 Bruto | 3 | 1,0 (morcego) + 0,2 (Bruto) | o bot sai de quase todo telegraph |
| R6 Guarda+Ladrão | 3 | 2,0 | 1 morcego + Guarda |
| B1 Ogro | 3 | 0,4 | pancada de 3 |

**Morcegos são quase todo o dano.** A R4 (ícone 2) é a sala que mais tira vida; ver "Achados".

### Ladrão, relíquia, ovo, baú (eventos por contrato, bot padrão)

| Contrato | ladrão pegou a carga | carga recuperada | ladrão escapou | ladrão da carga sozinha | rachaduras | baús abertos |
|---|---|---|---|---|---|---|
| C1 | 0,35 | 0,35 | 0 | 0 | 0 | 0,47 |
| C2 | 0,71 | 0,71 | 0 | 0 | 0 | 0,56 |
| C3 | 0,03 | 0,03 | 0 | 0 | 0 | 0,42 |
| C4 | 0,41 | 0,41 | 0 | 0 | 0,08 | 0,96 |
| C5 | 0,38 | 0,38 | 0 | 0 | 0 | 0,92 |
| C6 | 0,69 | 0,69 | 0 | 0 | 0,04 | 0,80 |
| C7 | 0,28 | 0,28 | 0 | 0 | 0,08 | 1,08 |
| C8 | 0,50 | 0,49 | 0,01 | 1,00 | 0,49 | 1,39 |

O bot nunca chocou o ovo (C3 termina em 12–18 s; C6 em 18–35 s; tempos-alvo 30 s e 45 s).

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

## Achados para o design (precisam de decisão do Vinicius)

- **Duração.** O bot fecha um contrato em 13–36 s e a campanha inteira em ~3 min de jogo. O GDD (§2, §6) conta com contratos de 4–6 min e sessões de 4–9 min. Mesmo que um jogador novo leve 2–3× o tempo do bot, um contrato fica em ~1 min. O formato do contrato (sala 9 × 14 que cabe na tela, 2–4 camadas) gera contratos curtos; para chegar a 4–6 min é preciso mais camadas, ondas por sala ou salas maiores. Decisão de design, não de número.
- **A relíquia quase nunca racha** (0–0,08 rachadura por contrato; 0,49 no C8 por causa do Ogro): só Bruto e Ogro acertam carga, e o bot sai do telegraph. Hoje a relíquia joga como uma caixa que paga 40% a mais. Propostas: o dash ou a queda (ladrão atingido) racham a relíquia; morcego que toca o courier com a relíquia nas mãos racha.
- **O ladrão nunca leva a carga do bot** (fuga 0–0,01 por contrato, 100% recuperada): fugir a 1,6 m/s + 1 s na borda dá tempo de sobra. Para humanos pode ser diferente; medir no diário (`thief_grab` × `cargo_drop` motivo 2).
- **Ícone de ameaça da R4 (2)** está abaixo do dano real (é a sala que mais tira vida). Proposta: ícone 3, ou 3 morcegos.
- **H1 do bot** fica em 33–74% de salas com luta com agressividade 0,5 (C6 e C8 passam um pouco de 70%). É um indicador do bot; o H1 de verdade é do playtest.
