# Conteúdo das regiões 2, 3 e 4 — desenho para implementar (2026-10-10)

**Raia M (design de conteúdo), leva A do [`CONTRATO_LANCAMENTO.md`](CONTRATO_LANCAMENTO.md) §12.** Fonte: `CONTRATO_LANCAMENTO.md` §2–§6, `CONTRATO_V1.md`, `GDD.md` §2–§10, `BALANCE.md` (v1), `ART_BIBLE.md` e `client/Assets/_DC/Scripts/Core/Defs.cs`.

**Status: PROPOSTA.** Todo número aqui é **HIPÓTESE** até o bot medir (regra do `BALANCE.md`: antes e depois). Quem digita no Core são as raias R2/R3/R4 (leva B), em `Core/Content/Regiao2.cs`…`Regiao4.cs` e `Run.Regiao2.cs`…; os nomes de enum, os códigos de sala, o registro por região (`Content/RegiaoN.cs`, ids locais) e o `Content.Make(...)` seguem o que a raia N já pôs no núcleo nesta leva; onde o núcleo tem número provisório diferente do daqui, o texto avisa.

**Guardrails respeitados:** nada aleatório pago (projetos de equipamento saem de baús de conteúdo visível, do quadro de temporada e do Starter Pack de conteúdo fixo); 1 moeda; o sorteio de perks e de contratos continua pela semente, sem pagamento.

---

## 0. Como ler

**Ids e códigos (só crescem no fim, vão para o save).** Cada região tem o próprio arquivo com **ids locais** (0–13) e o registro do `Content` soma a base global (região 1 = 0–20). Código da sala: a 1ª letra é o tipo (`R` normal, `S` secreta, `M` mini, `B` chefe, `D` entrega).

| Região | Salas normais | Secreta | Mini | Chefe | Entrega | Ids globais | Contratos (código · índice global) |
|---|---|---|---|---|---|---|---|
| 2 Minas Alagadas | R2-1…R2-10 | S2-1 | M2 | B2 | D2 | 21–34 | M2-1…M2-16 · 24–39 |
| 3 Cripta Assombrada | R3-1…R3-10 | S3-1 | M3 | B3 | D3 | 35–48 | M3-1…M3-16 · 40–55 |
| 4 Forja do Dragão | R4-1…R4-10 | S4-1 | M4 | B4 | D4 | 49–62 | M4-1…M4-16 · 56–71 |

- Constantes locais iguais nas três regiões: `R1 = 0 … R10 = 9, S1 = 10, M = 11, B = 12, D = 13`. A entrega de cada região tem a planta da D1 (é cena) com a pele da região.
- "M2" é a sala do mini-chefe da região 2; "M2-5" é o 5º contrato da região 2 (nome do contrato §2).
- Contratos: `Content.Make(código, carga, pagamento, nota, ParTime, ovo/gelo, limite, Tier, reforços, D, camadas...)` com ids locais; os 3 que ensinam vão com reforços desligados, como C1–C3.

**Helpers nos blocos de código** (os da região 1: `O` retângulo cx, cy, meia-largura, meia-altura; `S` spawn; `P` ponto; `L` camada; e apelidos `const EnemyType GoblinMiner = EnemyType.GoblinMiner, …` como no `Regiao1.cs`):
- `Z(tipo, x, y)` = spawn **dormindo** (§1.1).
- Elementos de terreno por sala, escritos como inicializador (`{ Water = … }`); a raia de região escolhe se viram campos do `RoomDef` ou uma tabela no próprio `RegiaoN.cs`: `Dark`, `Torches` (V2[]), `Water` (Rect[], não sólido), `Lava` (Rect[], sólido para andar e transparente para tiro), `Webs` (`C(x, y, raio)`), `Vents` (`V(x, y, raio, fase em s)`), `Rails` (`T(x1, y1, x2, y2, fase em s)`), `Buried` (`B(x, y, moedas)` = baú enterrado), `SecretHidden` (parede rachada disfarçada), `Blinks` (pontos de salto do Necromante). O núcleo já tem `Run.Dark` e `Run.LightMul` para o escuro.
- Sala com trilho tem um `S(Minecart, ...)` numa ponta de cada trilho (o vagonete entra na lista de inimigos, mas não conta no teto de vivos nem é alvo: §5).

**Regras de level design** (as do comentário acima de `Content.Rooms`, conferidas por um script que reimplementa `Run.Blocked` e o `Nav` de 0,5 m, com o mesmo critério de `CoreTests.LevelDesign_*`; todas as 39 salas passam):
- nada sólido (obstáculo, lápide, lava) na faixa das portas (y > 12,2) nem a 1,2 m da entrada (4,5; 0,9);
- spawn e reforço a 4 m ou mais da entrada e fora de obstáculo (pelo raio do inimigo);
- toda porta (1, 2 ou 3 portas), baú, baú enterrado, ponto de resgate e parede rachada alcançáveis andando;
- **novas:** teia, respiro e água a mais de 1,2 m da entrada; teia e respiro fora da faixa das portas; trilho só em 4 ≤ y ≤ 11,5, sem obstáculo em cima; nenhum spawn, baú ou resgate a menos de 1 m de um trilho nem em cima de respiro; tocha só nas paredes laterais (x = 0,3 ou 8,7); sala escura com 2+ tochas;
- só Guarda, chefes e emboscada (`waveLocks`) obrigam a lutar. Dormindo, vagonete, respiro e lava se atravessam sem lutar.

As raias R2–R4 devem acrescentar as regras novas ao teste C# (`LevelDesign_Regioes_*`).

---

## 1. Regras novas comuns (terreno, nível, ícones, dicas)

### 1.1 Terreno

| Elemento | Região | Regra (números) | Telegraph / leitura | Contra-jogo |
|---|---|---|---|---|
| **Água** (`Water`) | 2 | quem anda nela vai a **×0,75** (courier, ladrão, limo, mineiro, esqueleto…); não afeta voadores (morcego, fantasma, gárgula no mergulho), vagonete nem chefes. **Cristal de gelo recongela +10%/s** (no chão dentro dela, ou nas mãos do courier que está nela). Toupeira e Rainha **não emergem** na água. Fogo no chão apaga na água | poça azul-petróleo com brilho; ondas no pé | pousar a carga na água atrasa o ladrão e a protege da toupeira |
| **Trilho + vagonete** (`Rails`) | 2 | vagonete a **6 m/s** (cruza 9 m em 1,5 s), espera **2,0 s** no túnel e volta; **sino 1,2 s** antes de cada passagem: ciclo de **4,7 s** por sentido; `fase` adianta o ciclo. Atropela: courier **2 de dano** + empurrão de 1,5 m para fora do trilho (com a carga nas mãos vale a regra de golpe no courier: relíquia racha); carga no chão no trilho: empurrada 1,5 m + 1 rachadura; inimigo: **6 de dano** (sem escala) + empurrão; chefe: 6 de dano e **atordoa 1,5 s**; ladrão com a carga a derruba | o trilho acende em vermelho e o sino toca 1,2 s antes | atravessar logo depois da passagem; atrair ladrão/Golem para o trilho |
| **Dormindo** (`Z`) | 2 | não age até: courier a **≤ 2,0 m**, grito da gaiola a ≤ 5 m, explosão a ≤ 4 m, golpe nele, ou vizinho acordado a ≤ 3 m (1 s depois). **1º golpe num dormindo dá dano ×2** | "zZ" sobre a cabeça; anel fino de 2 m no chão ao chegar a 3 m | passar longe; abater do jeito furtivo |
| **Escuro** (`Dark` + `Torches`) | 3 (e Midnight/Neblina) | luz do courier **3,5 m**; tocha **2,5 m** em volta; lanterna nas mãos **5,5 m** (substitui a do courier), no chão **4,0 m** em volta dela; Olhos de gato ×1,6. **A mira automática só escolhe inimigo cujo centro esteja dentro de alguma luz** | fora da luz o chão cai a ~30% do valor (véu `#0B0D16` a 70%, nunca preto) e o inimigo aparece só como olhos e sombra; **telegraph no chão e projéteis sempre visíveis** (justiça) | andar de tocha em tocha; lanterna; pousar a carga na luz |
| **Teia** (`Webs`) | 3 | courier **carregando** que entra numa teia fica **preso 1,0 s** (depois 1,5 s imune àquela teia); sem carga, **×0,5** de velocidade; carga no chão dentro da teia leva **+1,0 s** para pegar (o ladrão também leva +1,0 s); **o dash rasga a teia** (some até o fim da visita); fogo a queima. Teia cuspida pela aranha: raio 0,6 m, dura 6 s | teia branca a 70% no chão | dash para rasgar; contornar |
| **Lápide** | 3 | obstáculo comum (sólido); no B3 é ponto de erguer esqueleto | pedra com caveira | cobertura contra raio e flecha |
| **Lava** (`Lava`) | 4 | **sólida para andar** (Nav e colisão, como obstáculo) e **transparente** para tiro, flecha, frasco e sopro. Inimigo empurrado contra ela (dash, vagonete, explosão) leva **3 de dano** (1× por empurrão) e volta para a borda; **brasa de magma morre na hora**; salamandra, elemental grande e dragão são imunes. Courier empurrado contra ela: 2 de dano. Carga empurrada para ela para na borda com 1 rachadura. **Gelo a ≤ 1,5 m de lava derrete ×3** | laranja com miolo amarelo e crosta escura, ondulando | empurrar inimigos com o dash; atirar por cima |
| **Respiro** (`Vents`) | 4 | ciclo **4,0 s** = 2,2 s apagado + **0,8 s brilhando** + **1,0 s de fogo**; `fase` em s. Fogo: courier **2 de dano** (1× por erupção); carga no chão dentro: 1 rachadura, gelo −25, **barril explode**, balão 1 furo; inimigo **3 de dano** (1× por erupção; os da forja imunes); queima teia | grade no chão que brilha laranja 0,8 s antes | passar logo depois do fogo; atrair inimigos para ele |
| **Fogo no chão** | 4 | rastro da salamandra (raio 0,45 m, 3 s), poça do frasco (raio 1,0 m, 4 s), brasa do sopro (2 s): courier 1 de dano a cada 1,0 s dentro; carga no chão dentro: gelo −10/s, barril explode, balão 1 furo/s; inimigos da forja imunes, os outros 1 de dano/s; água apaga | chão em chamas laranja | não pousar ali; contornar |
| **Baú enterrado** (`Buried`) | todas | invisível sem o Mapa (carga), o perk Faro de baú ou o evento Treasure Detour; abre ficando **1,5 s** parado a 1,2 m (Faro: 1,0 s); moedas fixas, pagas uma vez por visita como o baú comum | "X" no chão e pá no ícone da porta (só quando revelado) | levar o Mapa |
| **Parede rachada disfarçada** (`SecretHidden`) | 4 (R4-9) | só aparece e só abre com o Mapa, o Faro de baú ou o Treasure Detour | igual à rachada quando revelada | idem |

### 1.2 Nível (Tier) e reforço por região

A vida dos inimigos continua `× Balance.HpMul(Tier)` (morcego, limo, besouro, fantasma, salamandra, brasa e ovo de salamandra ficam fora: perseguidores frágeis). Como guilda e armas param de crescer no fim da região 1 (7 de 8 níveis em média, `BALANCE.md`), **a vida não continua subindo em linha reta**: cada região recomeça um pouco abaixo e a dificuldade nova vem do verbo do lugar.

- **Tier da campanha = base da região + índice local (0–15):** região 2 base 8 (×1,84 → ×3,64, termina igual ao C24); região 3 base 12 (×2,32 → ×4,12); região 4 base 16 (×2,80 → ×4,60). Vai no parâmetro `tier` do `Content.Make`.
- **Reforço:** os 3 contratos que ensinam vão com `waves = false` (como C1–C3); do 4º em diante, ligado (2 por visita pelo `MaxWaves`, teto de 10 vivos).
- **Contrato do dia, código, raro e evento:** o `TierBase` provisório do núcleo (14 / 18 / 22) é exatamente base da região + 6, então fica. **Proposta que muda o núcleo:** pagamento gerado = **base + 22 × camadas** com base 40 / 110 / 165 / 220 nas regiões 1–4 (hoje é 40 + 22 × camadas em todas: um contrato do dia na região 4 pagaria menos que o M2-1); tempos por sala (s) nas regiões 2/3/4: ParTime 20/22/22, ovo 25/28/28, gelo 27/30/30, mensagem 13/14/14 (hoje 18 / 23 / 12 em todas).

### 1.3 Ícone de risco da carga

Sem valor novo: os 3 ícones que existem cobrem as regiões novas, como o núcleo já fez nos provisórios. **Ladrão (mão)** = leva ou mexe na carga (sapo, fantasma, diabrete, Rainha, Viúva); **Arqueiro (flecha)** = projétil ou raio que acerta a carga no chão (mineiro, kobold, Capataz); **Pancada** = golpe, explosão ou fogo que racha a carga no chão (toupeira, besouro, vagonete, gárgula, elemental, Golem, Dragão). Esqueleto, aranha e múmia miram o courier: sem ícone. Três acertos nos provisórios: **Necromante → Arqueiro** (o raio racha a carga no chão), **salamandra → Pancada** (o rastro de fogo derrete o gelo, fura o balão e acende o barril) e **Capataz → Arqueiro + Ladrão** (o chicote puxa a carga). Sala com respiro ou lava mostra Pancada mesmo sem inimigo que a dê.

### 1.4 Dicas novas (`Hint`, texto em `Hints.Text`)

| Hint | Texto |
|---|---|
| `Water` | "água freia todo mundo: o ladrão também" |
| `CartBell` | "vagonete! saia do trilho" |
| `Asleep` | "mineiro dormindo: passe a mais de 2 m" |
| `MoleTrail` | "toupeira por baixo: carregue a carga" |
| `FrogTongue` | "sapo: não pouse a menos de 3 m dele" |
| `BeetleFuse` | "besouro vai explodir: dash ou atire de longe" |
| `HeartPull` | "o coração atrai inimigos e moedas" |
| `IceMelt` | "o gelo derrete: passe pela água" |
| `CageScream` | "o grifinho vai gritar: o dash acalma" |
| `Dark` | "no escuro só se mira o que está na luz" |
| `LanternOut` | "a lanterna apagou: leve-a até uma tocha" |
| `Web` | "teia: o dash rasga" |
| `GhostCargo` | "o fantasma está levando a carga: pegue de volta" |
| `SkeletonRise` | "lute perto da carga: o esqueleto não volta" |
| `MummyGrab` | "agarrado: dash ou pouse e lute" |
| `GargoyleWake` | "a gárgula acorda com a carga por perto" |
| `MirrorFront` | "o espelho reflete o que vem de frente" |
| `BalloonRise` | "balão no chão escapa em 4 s: pegue!" |
| `Lava` | "empurre inimigos na lava com o dash" |
| `Vent` | "respiro brilhando: vai sair fogo" |
| `PlantBite` | "a planta morde quem fica perto: dê um passo" |
| `MapReveal` | "o mapa mostra baús enterrados" |
| `ImpSwap` | "diabrete vai trocar de lugar com a carga" |

---

## 2. Região 2 — Minas Alagadas

**Tema:** mina inundada de goblins, escorada em madeira, com trilhos de vagonete e lanternas de mina. **Verbo do lugar:** *molhar e cronometrar* — a água freia todo mundo (inclusive quem vem roubar) e protege a carga da toupeira; o vagonete corta a sala num ritmo fixo e atropela inimigos; mineiros dormem e só acordam com barulho ou com você perto.

**Destrava:** 30 estrelas e o Ogro (B1) vencido. **Inimigos novos:** goblin mineiro, toupeira, sapo do lodo, besouro-bomba, vagonete. **Mini:** Rainha Toupeira. **Chefe:** Golem de Pedra. **Cargas novas:** cristal de gelo (M2-3), coração arcano (M2-5), gaiola do grifinho (M2-10).

**Paleta** (o cenário não usa cores de papel): piso de terra e pedra `#4A4540` (sombra `#34302C`, luz `#625B52`); parede `#5A5249` (face `#433D37`); escoras de madeira `#7A5A3A`; água `#2D5F73` com brilho `#7FC9E0` a 60%; trilho `#5E6670` com dormentes `#5C4632`; luz das lanternas de mina `#FFD27A` aditiva a 18–25%.

### 2.1 Salas

Constantes locais (`Regiao2.cs`): `public const int R1 = 0, R2 = 1, R3 = 2, R4 = 3, R5 = 4, R6 = 5, R7 = 6, R8 = 7, R9 = 8, R10 = 9, S1 = 10, M = 11, B = 12, D = 13;`

Ameaça (caveiras) = hipótese pelo desenho, no mesmo terço de dano da região 1 (1 = R2-1, R2-3; 2 = R2-2, R2-4, R2-5, R2-7, R2-8; 3 = R2-6, R2-9, R2-10); o teste `IconeDeAmeaca_AcompanhaODanoMedido` reordena depois da medição.

**R2-1 — Galeria Alagada** (id local R1 = 0, global 21; ameaça 1). Lição: água freia todos 25% (courier, ladrão, limo): pouse a carga na poça e o ladrão chega mais devagar; o cristal recongela na água; parede rachada à direita. *baú: 1º projeto das Botas de lama.*
```csharp
new RoomDef(R1, "R2-1", "Galeria Alagada", "água freia todos 25% (courier, ladrão, limo): pouse a carga na poça e o ladrão chega mais devagar; o cristal recongela na água; parede rachada à direita", 1,
    new[] { O(1.5f, 8.5f, 0.5f, 0.5f), O(7.5f, 8.5f, 0.5f, 0.5f), O(4.5f, 11.0f, 0.8f, 0.4f) },
    new[] { S(Thief, 8.0f, 10.0f), S(Bat, 1.5f, 11.5f) }, P(2.0f, 3.0f), P(8.2f, 6.5f), 30,
    wave: new[] { S(Slime, 0.8f, 3.6f) }, trigger: Half, secret: S1, secretPos: P(Balance.RoomW, 4.0f))
{ Water = new[] { O(4.5f, 6.5f, 2.5f, 1.4f) } },
```

**R2-2 — Trilho do Vagonete** (id local R2 = 1, global 22; ameaça 2). Lição: o vagonete cruza a sala a cada ~4,7 s (sino e trilho acesos 1,2 s antes): atravesse logo depois dele; ele atropela inimigos: traga o ladrão até o trilho.
```csharp
new RoomDef(R2, "R2-2", "Trilho do Vagonete", "o vagonete cruza a sala a cada ~4,7 s (sino e trilho acesos 1,2 s antes): atravesse logo depois dele; ele atropela inimigos: traga o ladrão até o trilho", 2,
    new[] { O(2.0f, 9.6f, 0.6f, 0.6f), O(7.0f, 9.6f, 0.6f, 0.6f), O(4.5f, 4.0f, 0.8f, 0.3f) },
    new[] { S(Minecart, 0.6f, 6.5f), S(Thief, 1.5f, 11.5f), S(GoblinMiner, 7.0f, 11.6f), S(Bat, 4.5f, 11.8f) }, P(1.5f, 3.0f),
    wave: new[] { S(Thief, 8.0f, 12.5f) }, trigger: Down)
{ Rails = new[] { T(0.0f, 6.5f, 9.0f, 6.5f, 0.0f) } },
```

**R2-3 — Mina Adormecida** (id local R3 = 2, global 23; ameaça 1). Lição: mineiros dormindo (zZ) só acordam com o courier a 2 m, grito, explosão ou golpe: o corredor do meio passa em silêncio; o baú fica colado num dorminhoco.
```csharp
new RoomDef(R3, "R2-3", "Mina Adormecida", "mineiros dormindo (zZ) só acordam com o courier a 2 m, grito, explosão ou golpe: o corredor do meio passa em silêncio; o baú fica colado num dorminhoco", 1,
    new[] { O(2.5f, 6.0f, 1.0f, 0.4f), O(6.5f, 6.0f, 1.0f, 0.4f), O(4.5f, 9.5f, 0.6f, 0.6f) },
    new[] { Z(GoblinMiner, 0.8f, 10.5f), Z(GoblinMiner, 8.2f, 10.5f), S(Bat, 4.5f, 12.0f) }, P(2.0f, 3.0f), P(8.2f, 12.0f), 40,
    wave: new[] { S(Bat, 0.8f, 3.6f) }, trigger: Half),
```

**R2-4 — Toca da Toupeira** (id local R4 = 3, global 24; ameaça 2). Lição: rastro de terra = toupeira por baixo: ela sai embaixo da carga no chão e a empurra 1,5 m; carregue a carga ou acerte-a nos 2 s em que fica fora; na água ela não sai.
```csharp
new RoomDef(R4, "R2-4", "Toca da Toupeira", "rastro de terra = toupeira por baixo: ela sai embaixo da carga no chão e a empurra 1,5 m; carregue a carga ou acerte-a nos 2 s em que fica fora; na água ela não sai", 2,
    new[] { O(2.0f, 7.5f, 0.6f, 0.6f), O(7.0f, 7.5f, 0.6f, 0.6f) },
    new[] { S(Mole, 4.5f, 10.0f), S(Mole, 1.5f, 11.5f), S(Bat, 7.5f, 11.5f) }, P(1.5f, 3.0f), P(0.8f, 9.5f), 35,
    wave: new[] { S(Mole, 8.0f, 12.0f) }, trigger: Down)
{ Water = new[] { O(6.8f, 4.6f, 1.4f, 1.1f) },
  Buried = new[] { B(8.2f, 12.0f, 30) } },
```

**R2-5 — Lodaçal do Sapo** (id local R5 = 4, global 25; ameaça 2). Lição: o sapo puxa a carga do chão a até 3 m (linha de aviso 0,7 s): pouse fora do anel dele e abata-o de 3 a 5 m. *baú: 2º projeto das Botas de lama.*
```csharp
new RoomDef(R5, "R2-5", "Lodaçal do Sapo", "o sapo puxa a carga do chão a até 3 m (linha de aviso 0,7 s): pouse fora do anel dele e abata-o de 3 a 5 m", 2,
    new[] { O(4.5f, 5.0f, 0.6f, 0.6f), O(4.5f, 10.6f, 0.6f, 0.6f) },
    new[] { S(BogFrog, 2.0f, 9.5f), S(BogFrog, 7.0f, 7.0f), S(Thief, 8.0f, 11.5f) }, P(2.0f, 3.0f), P(0.8f, 6.0f), 35,
    wave: new[] { S(Slime, 0.8f, 3.6f) }, trigger: Half)
{ Water = new[] { O(2.0f, 9.5f, 1.4f, 1.2f), O(7.0f, 7.0f, 1.4f, 1.2f) } },
```

**R2-6 — Corredor dos Besouros** (id local R6 = 5, global 26; ameaça 3). Lição: o besouro corre até você, brilha 1 s e explode (1,5 m): atire de longe ou dê dash; a explosão acerta inimigos: leve-o até o Bruto.
```csharp
new RoomDef(R6, "R2-6", "Corredor dos Besouros", "o besouro corre até você, brilha 1 s e explode (1,5 m): atire de longe ou dê dash; a explosão acerta inimigos: leve-o até o Bruto", 3,
    new[] { O(2.0f, 6.0f, 0.5f, 0.5f), O(7.0f, 6.0f, 0.5f, 0.5f), O(4.5f, 8.5f, 0.5f, 0.5f) },
    new[] { S(BombBeetle, 1.5f, 11.0f), S(BombBeetle, 7.5f, 11.0f), S(Brute, 4.5f, 11.0f) }, P(2.0f, 3.0f), P(8.2f, 9.0f), 40,
    wave: new[] { S(BombBeetle, 0.8f, 3.6f), S(BombBeetle, 8.2f, 3.6f) }, trigger: Half),
```

**R2-7 — Cruzamento de Trilhos** (id local R7 = 6, global 27; ameaça 2). Lição: dois trilhos com ritmos opostos: espere na ilha do meio; o ladrão que corre para a carga cruza o trilho e pode ser atropelado; parede rachada à esquerda. *baú: 1º projeto da Bomba de fumaça.*
```csharp
new RoomDef(R7, "R2-7", "Cruzamento de Trilhos", "dois trilhos com ritmos opostos: espere na ilha do meio; o ladrão que corre para a carga cruza o trilho e pode ser atropelado; parede rachada à esquerda", 2,
    new[] { O(2.5f, 7.2f, 0.6f, 0.5f), O(6.5f, 7.2f, 0.6f, 0.5f), O(4.5f, 11.6f, 0.8f, 0.3f) },
    new[] { S(Minecart, 0.6f, 4.8f), S(Minecart, 8.4f, 9.6f), S(Thief, 8.0f, 7.2f), S(Archer, 7.5f, 11.6f), S(Bat, 1.5f, 11.8f) }, P(1.5f, 2.8f), P(0.8f, 11.2f), 40,
    wave: new[] { S(Thief, 8.0f, 12.5f) }, trigger: Down, secret: S1, secretPos: P(0f, 7.2f))
{ Rails = new[] { T(0.0f, 4.8f, 9.0f, 4.8f, 0.0f), T(0.0f, 9.6f, 9.0f, 9.6f, 2.35f) } },
```

**R2-8 — Eclusa do Guarda** (id local R8 = 7, global 28; ameaça 2). Lição: luta obrigatória: pouse a carga na água (ladrão mais lento, a toupeira não sai ali) e derrube o Guarda.
```csharp
new RoomDef(R8, "R2-8", "Eclusa do Guarda", "luta obrigatória: pouse a carga na água (ladrão mais lento, a toupeira não sai ali) e derrube o Guarda", 2,
    new[] { O(1.2f, 9.5f, 0.4f, 1.0f), O(7.8f, 9.5f, 0.4f, 1.0f) },
    new[] { S(Guard, 4.5f, 12.4f), S(Thief, 1.0f, 6.5f), S(Mole, 7.8f, 6.0f) }, P(7.0f, 2.8f),
    wave: new[] { S(Thief, 8.0f, 12.5f) }, trigger: Down)
{ Water = new[] { O(4.5f, 7.0f, 2.2f, 1.2f) } },
```

**R2-9 — Desmoronamento** (id local R9 = 8, global 29; ameaça 3). Lição: emboscada: passar da metade faz mineiros e um besouro caírem do teto e a porta fecha; a explosão do besouro acerta os mineiros.
```csharp
new RoomDef(R9, "R2-9", "Desmoronamento", "emboscada: passar da metade faz mineiros e um besouro caírem do teto e a porta fecha; a explosão do besouro acerta os mineiros", 3,
    new[] { O(2.5f, 5.5f, 0.6f, 0.6f), O(6.5f, 5.5f, 0.6f, 0.6f), O(4.5f, 9.0f, 1.2f, 0.4f) },
    new[] { Z(GoblinMiner, 8.0f, 7.6f), S(Bat, 1.5f, 10.5f) }, P(2.0f, 2.8f),
    wave: new[] { S(GoblinMiner, 1.5f, 12.5f), S(GoblinMiner, 7.5f, 12.5f), S(BombBeetle, 4.5f, 12.8f) }, trigger: Half, waveLocks: true)
{ Buried = new[] { B(8.2f, 4.0f, 35) } },
```

**R2-10 — Salão da Bomba d'Água** (id local R10 = 9, global 30; ameaça 3). Lição: Bruto, sapo e mineiro juntos: o Bruto empurra você para o anel do sapo; carga 3 s no chão chama reforço e fecha a porta.
```csharp
new RoomDef(R10, "R2-10", "Salão da Bomba d'Água", "Bruto, sapo e mineiro juntos: o Bruto empurra você para o anel do sapo; carga 3 s no chão chama reforço e fecha a porta", 3,
    new[] { O(1.5f, 5.5f, 0.6f, 0.6f), O(7.5f, 5.5f, 0.6f, 0.6f), O(1.5f, 10.0f, 0.6f, 0.6f), O(7.5f, 10.0f, 0.6f, 0.6f) },
    new[] { S(Brute, 2.6f, 11.8f), S(BogFrog, 4.5f, 8.0f), S(GoblinMiner, 7.5f, 12.0f) }, P(2.0f, 2.8f), P(0.8f, 8.0f), 45,
    wave: new[] { S(BombBeetle, 1.0f, 12.5f), S(Thief, 8.0f, 12.5f), S(Bat, 4.5f, 12.8f) }, trigger: Down, waveLocks: true)
{ Water = new[] { O(4.5f, 8.0f, 1.6f, 1.2f) } },
```

**S2-1 — Veio de Ouro** (id local S1 = 10, global 31; ameaça 0). Lição: sala secreta: baú grande ao lado de um mineiro dormindo. *baú: 3º projeto das Botas de lama.*
```csharp
new RoomDef(S1, "S2-1", "Veio de Ouro", "sala secreta: baú grande ao lado de um mineiro dormindo", 0,
    new[] { O(2.0f, 6.0f, 0.5f, 0.5f), O(7.0f, 6.0f, 0.5f, 0.5f) },
    new[] { Z(GoblinMiner, 6.0f, 10.5f) }, P(2.0f, 2.5f), P(4.5f, 10.0f), 90),
```

**M2 — Ninho da Rainha Toupeira** (id local M = 11, global 32; ameaça 3). Lição: mini-chefe: a Rainha cava, sai embaixo da carga no chão e chama toupeiras; nas poças ela não sai.
```csharp
new RoomDef(M, "M2", "Ninho da Rainha Toupeira", "mini-chefe: a Rainha cava, sai embaixo da carga no chão e chama toupeiras; nas poças ela não sai", 3,
    new[] { O(2.5f, 9.5f, 0.5f, 0.5f), O(6.5f, 9.5f, 0.5f, 0.5f) },
    new[] { S(MoleQueen, 4.5f, 10.8f), S(Mole, 7.5f, 11.5f) }, P(2.0f, 2.5f))
{ Water = new[] { O(1.6f, 6.0f, 1.0f, 1.0f), O(7.4f, 6.0f, 1.0f, 1.0f) } },
```

**B2 — Câmara do Golem** (id local B = 12, global 33; ameaça 3). Lição: chefe: pancada em área, pedras que fecham rotas, ponto fraco nas costas; o vagonete acerta o Golem (6 de dano e atordoa).
```csharp
new RoomDef(B, "B2", "Câmara do Golem", "chefe: pancada em área, pedras que fecham rotas, ponto fraco nas costas; o vagonete acerta o Golem (6 de dano e atordoa)", 3,
    new[] { O(1.8f, 7.6f, 0.5f, 0.5f), O(7.2f, 7.6f, 0.5f, 0.5f), O(1.8f, 11.2f, 0.5f, 0.5f), O(7.2f, 11.2f, 0.5f, 0.5f) },
    new[] { S(StoneGolem, 4.5f, 9.5f), S(Minecart, 0.6f, 4.6f) }, P(2.0f, 2.5f))
{ Rails = new[] { T(0.0f, 4.6f, 9.0f, 4.6f, 0.0f) } },
```

**D2 — Entrega** (id local D = 13, global 34). Mesma planta da D1, com a pele da região.
```csharp
new RoomDef(D, "D2", "Entrega", "o cliente espera no topo", 0,
    new[] { O(2.0f, 7.0f, 0.5f, 0.5f), O(7.0f, 7.0f, 0.5f, 0.5f) },
    new Spawn[0], P(2.0f, 2.5f)),
```

### 2.2 Contratos (16)

Pagamento continua a região 1 (C23 = 195, C24 = 260) com degrau leve; os tempos saem da estimativa do bot (§10.2): ParTime = 1,6×, ovo = 2×, gelo = 2,2×, limite da mensagem = 2,5× (mesmas regras da decisão 36 do `BALANCE.md`). "Ovo/Gelo" vai no campo `EggTarget` (no gelo, é o `MeltTime`: tempo para derreter do cheio ao zero sem água).

| Código | Índice global | Carga | Pagamento | Nível (Tier) | Camadas (salas de cada camada) | Bot estimado (s) | ParTime | Ovo/Gelo | Limite |
|---|---|---|---|---|---|---|---|---|---|
| M2-1 | 24 | Caixa | 195 | 8 | R2-1 · R2-2/R2-3 · D2 | 32 | 50 | — | — |
| M2-2 | 25 | Ovo | 200 | 9 | R2-4 · R2-3/R2-5 · R2-2/R2-1 · D2 | 50 | 80 | 100 | — |
| M2-3 | 26 | Cristal de gelo | 205 | 10 | R2-1 · R2-5/R2-8 · R2-4/R2-7 · D2 | 52 | 85 | 115 | — |
| M2-4 | 27 | Relíquia | 210 | 11 | R2-6 · R2-3/R2-2 · R2-5/R2-4 · R2-9/R2-7 · R2-1/R2-8 · D2 | 92 | 145 | — | — |
| M2-5 | 28 | Coração arcano | 214 | 12 | R2-3 · R2-6/R2-1 · R2-4/R2-8 · R2-7/R2-5 · R2-10/R2-2 · D2 | 88 | 140 | — | — |
| M2-6 | 29 | Barril | 218 | 13 | R2-1 · R2-2/R2-5 · R2-8/R2-3 · R2-4/R2-6 · R2-7/R2-9 · R2-5/R2-1 · D2 | 103 | 165 | — | — |
| M2-7 | 30 | Cristal de gelo | 222 | 14 | R2-8 · R2-1/R2-7 · R2-5/R2-3/R2-4 · R2-6/R2-2 · R2-10/R2-9 · D2 | 96 | 155 | 210 | — |
| M2-8 | 31 | Caixa | 270 | 15 | R2-2 · R2-4/R2-6 · R2-9/R2-1 · R2-8/R2-5 · R2-3/R2-7 · M2 · D2 | 125 | 200 | — | — |
| M2-9 | 32 | Mensagem | 230 | 16 | R2-5 · R2-8/R2-3 · R2-10/R2-2 · R2-4/R2-9 · R2-1/R2-6 · R2-7/R2-8 · D2 | 48 | 75 | — | 120 |
| M2-10 | 33 | Gaiola do grifinho | 234 | 17 | R2-3 · R2-1/R2-4 · R2-9/R2-7 · R2-2/R2-5 · R2-6/R2-8 · D2 | 88 | 140 | — | — |
| M2-11 | 34 | Coração arcano | 238 | 18 | R2-7 · R2-9/R2-3 · R2-6/R2-10 · R2-5/R2-2 · R2-8/R2-4 · R2-1/R2-7 · D2 | 110 | 175 | — | — |
| M2-12 | 35 | Bolo | 242 | 19 | R2-4 · R2-6/R2-1 · R2-2/R2-10 · R2-9/R2-8 · R2-3/R2-5 · R2-7/R2-4 · D2 | 110 | 175 | — | — |
| M2-13 | 36 | Caixa pesada | 246 | 20 | R2-1 · R2-5/R2-7 · R2-8/R2-10/R2-3 · R2-2/R2-6 · R2-9/R2-4 · R2-5/R2-1 · D2 | 106 | 170 | — | — |
| M2-14 | 37 | Gaiola do grifinho | 250 | 21 | R2-6 · R2-3/R2-10 · R2-7/R2-2 · R2-4/R2-9 · R2-8/R2-1 · R2-5/R2-3 · D2 | 110 | 175 | — | — |
| M2-15 | 38 | Cristal de gelo | 254 | 22 | R2-10 · R2-2/R2-8 · R2-6/R2-5 · R2-9/R2-1/R2-4 · R2-3/R2-7 · R2-10/R2-6 · D2 | 119 | 190 | 260 | — |
| M2-16 | 39 | Caixa | 340 | 23 | R2-10 · R2-8/R2-7 · R2-9/R2-6/R2-3 · R2-5/R2-2 · R2-4/R2-1 · B2 · D2 | 196 | 315 | — | — |

```csharp
Content.Make("M2-1", CargoType.Box, 195, "região 2: água e vagonete; mineiros dormindo", 50f, 0f, 0f, 8, false, D, L(R1), L(R2, R3)),   // global 24
Content.Make("M2-2", CargoType.Egg, 200, "toupeira e sapo; o ovo choca no tempo-alvo", 80f, 100f, 0f, 9, false, D, L(R4), L(R3, R5), L(R2, R1)),   // global 25
Content.Make("M2-3", CargoType.IceCrystal, 205, "cristal de gelo: derrete; a água recongela", 85f, 115f, 0f, 10, false, D, L(R1), L(R5, R8), L(R4, R7)),   // global 26
Content.Make("M2-4", CargoType.Relic, 210, "relíquia entre picaretas e trilhos", 145f, 0f, 0f, 11, true, D, L(R6), L(R3, R2), L(R5, R4), L(R9, R7), L(R1, R8)),   // global 27
Content.Make("M2-5", CargoType.ArcaneHeart, 214, "coração arcano: ímã de moedas e de inimigos", 140f, 0f, 0f, 12, true, D, L(R3), L(R6, R1), L(R4, R8), L(R7, R5), L(R10, R2)),   // global 28
Content.Make("M2-6", CargoType.Barrel, 218, "barril com besouros: a explosão detona o barril", 165f, 0f, 0f, 13, true, D, L(R1), L(R2, R5), L(R8, R3), L(R4, R6), L(R7, R9), L(R5, R1)),   // global 29
Content.Make("M2-7", CargoType.IceCrystal, 222, "cristal em 5 camadas", 155f, 210f, 0f, 14, true, D, L(R8), L(R1, R7), L(R5, R3, R4), L(R6, R2), L(R10, R9)),   // global 30
Content.Make("M2-8", CargoType.Box, 270, "mini-chefe: Rainha Toupeira", 200f, 0f, 0f, 15, true, D, L(R2), L(R4, R6), L(R9, R1), L(R8, R5), L(R3, R7), L(M)),   // global 31
Content.Make("M2-9", CargoType.Message, 230, "mensagem: trilhos não esperam", 75f, 0f, 120f, 16, true, D, L(R5), L(R8, R3), L(R10, R2), L(R4, R9), L(R1, R6), L(R7, R8)),   // global 32
Content.Make("M2-10", CargoType.GriffinCage, 234, "gaiola do grifinho: o grito acorda a mina", 140f, 0f, 0f, 17, true, D, L(R3), L(R1, R4), L(R9, R7), L(R2, R5), L(R6, R8)),   // global 33
Content.Make("M2-11", CargoType.ArcaneHeart, 238, "coração em 6 camadas", 175f, 0f, 0f, 18, true, D, L(R7), L(R9, R3), L(R6, R10), L(R5, R2), L(R8, R4), L(R1, R7)),   // global 34
Content.Make("M2-12", CargoType.Cake, 242, "bolo sobre trilhos", 175f, 0f, 0f, 19, true, D, L(R4), L(R6, R1), L(R2, R10), L(R9, R8), L(R3, R5), L(R7, R4)),   // global 35
Content.Make("M2-13", CargoType.Heavy, 246, "caixa pesada na água", 170f, 0f, 0f, 20, true, D, L(R1), L(R5, R7), L(R8, R10, R3), L(R2, R6), L(R9, R4), L(R5, R1)),   // global 36
Content.Make("M2-14", CargoType.GriffinCage, 250, "gaiola em 6 camadas", 175f, 0f, 0f, 21, true, D, L(R6), L(R3, R10), L(R7, R2), L(R4, R9), L(R8, R1), L(R5, R3)),   // global 37
Content.Make("M2-15", CargoType.IceCrystal, 254, "cristal em 6 camadas", 190f, 260f, 0f, 22, true, D, L(R10), L(R2, R8), L(R6, R5), L(R9, R1, R4), L(R3, R7), L(R10, R6)),   // global 38
Content.Make("M2-16", CargoType.Box, 340, "chefe: Golem de Pedra", 315f, 0f, 0f, 23, true, D, L(R10), L(R8, R7), L(R9, R6, R3), L(R5, R2), L(R4, R1), L(B)),   // global 39
```

---

## 3. Região 3 — Cripta Assombrada

**Tema:** cripta sob a guilda, lápides, capela, biblioteca e catedral afundadas no escuro. **Verbo do lugar:** *iluminar* — no escuro a mira só pega o que está na luz; tochas, a sua luz e a lanterna decidem onde dá para lutar. Teias prendem quem passa carregando; mortos-vivos voltam se você lutar longe da carga.

**Destrava:** 60 estrelas e o Golem (B2) vencido. **Inimigos novos:** fantasma, esqueleto, aranha, múmia, gárgula. **Mini:** Viúva Aranha. **Chefe:** Necromante. **Cargas novas:** lanterna (M3-1), espelho encantado (M3-3), balão de gás (M3-5). **Todas as salas são escuras** (a entrega D3 fica iluminada).

**Paleta:** piso de lajes frias `#2F3340` (sombra `#22252F`, luz `#454B5C`); parede `#3A3F4F`; lápide `#6E7385`; teia `#E8E8F0` a 70%; tochas e velas em âmbar `#FFB347` como na região 1; acento de morto-vivo verde `#7CFF9E` (olhos de esqueleto, múmia e Necromante); fantasma `#BFF3FF`. **Escuro legível:** fora da luz o chão fica a ~30% do valor (véu `#0B0D16` a 70%) e nada some no preto; isso pede uma exceção explícita ao `ART_BIBLE.md` §12 ("escuridão de clima"): aqui o escuro é regra de jogo, com telegraph e projétil sempre visíveis.

Constantes locais (`Regiao3.cs`): `public const int R1 = 0, R2 = 1, R3 = 2, R4 = 3, R5 = 4, R6 = 5, R7 = 6, R8 = 7, R9 = 8, R10 = 9, S1 = 10, M = 11, B = 12, D = 13;`

Ameaça hipotética: 1 = R3-1; 2 = R3-2, R3-3, R3-4, R3-5, R3-6, R3-8, R3-9; 3 = R3-7, R3-10.

**R3-1 — Corredor das Tochas** (id local R1 = 0, global 35; ameaça 1). Lição: no escuro a mira só pega o que está na luz (sua luz 3,5 m, tocha 2,5 m, lanterna 5,5 m): ande de tocha em tocha.
```csharp
new RoomDef(R1, "R3-1", "Corredor das Tochas", "no escuro a mira só pega o que está na luz (sua luz 3,5 m, tocha 2,5 m, lanterna 5,5 m): ande de tocha em tocha", 1,
    new[] { O(2.5f, 5.0f, 0.6f, 0.6f), O(6.5f, 5.0f, 0.6f, 0.6f), O(2.5f, 9.0f, 0.6f, 0.6f), O(6.5f, 9.0f, 0.6f, 0.6f) },
    new[] { S(Skeleton, 4.5f, 10.5f), S(Bat, 7.5f, 11.5f) }, P(1.5f, 2.5f),
    wave: new[] { S(Bat, 8.2f, 3.6f) }, trigger: Half)
{ Dark = true,
  Torches = new[] { P(0.3f, 4.0f), P(8.7f, 7.0f), P(0.3f, 10.5f) } },
```

**R3-2 — Ossário** (id local R2 = 1, global 36; ameaça 2). Lição: esqueleto derrubado se remonta em 4 s, a menos que a carga esteja a 3 m dele ou que o dash espalhe os ossos: lute perto da carga. *lápides = obstáculos; baú: 1º projeto do Gancho.*
```csharp
new RoomDef(R2, "R3-2", "Ossário", "esqueleto derrubado se remonta em 4 s, a menos que a carga esteja a 3 m dele ou que o dash espalhe os ossos: lute perto da carga", 2,
    new[] { O(2.0f, 7.0f, 0.6f, 0.3f), O(7.0f, 7.0f, 0.6f, 0.3f), O(4.5f, 9.5f, 0.6f, 0.3f) },
    new[] { S(Skeleton, 2.5f, 10.5f), S(Skeleton, 6.5f, 10.5f), S(Thief, 8.0f, 8.5f) }, P(2.0f, 2.5f), P(0.8f, 9.0f), 40,
    wave: new[] { S(Skeleton, 0.8f, 3.6f) }, trigger: Half)
{ Dark = true,
  Torches = new[] { P(0.3f, 5.5f), P(8.7f, 5.5f), P(0.3f, 11.0f) } },
```

**R3-3 — Teia Grande** (id local R3 = 2, global 37; ameaça 2). Lição: teia prende 1 s quem passa carregando e freia 50% sem carga; carga pousada na teia leva +1 s para pegar; o dash rasga a teia.
```csharp
new RoomDef(R3, "R3-3", "Teia Grande", "teia prende 1 s quem passa carregando e freia 50% sem carga; carga pousada na teia leva +1 s para pegar; o dash rasga a teia", 2,
    new[] { O(1.0f, 8.5f, 0.4f, 0.4f), O(8.0f, 8.5f, 0.4f, 0.4f) },
    new[] { S(Spider, 4.5f, 11.5f), S(Spider, 1.5f, 9.6f), S(Bat, 7.5f, 12.0f) }, P(1.5f, 2.5f),
    wave: new[] { S(Spider, 8.2f, 3.6f) }, trigger: Half)
{ Dark = true,
  Torches = new[] { P(0.3f, 4.0f), P(8.7f, 4.0f), P(8.7f, 10.0f) },
  Webs = new[] { C(2.2f, 6.0f, 0.9f), C(6.8f, 6.0f, 0.9f), C(4.5f, 8.5f, 1.0f), C(2.2f, 11.0f, 0.9f), C(6.8f, 11.0f, 0.9f) } },
```

**R3-4 — Capela Fantasma** (id local R4 = 3, global 38; ameaça 2). Lição: fantasma atravessa parede e possui a carga no chão: ela flutua até a parede e some em 2 s; pegue de volta ou acerte-o (só na luz); parede rachada à direita. *baú: 2º projeto da Bomba de fumaça.*
```csharp
new RoomDef(R4, "R3-4", "Capela Fantasma", "fantasma atravessa parede e possui a carga no chão: ela flutua até a parede e some em 2 s; pegue de volta ou acerte-o (só na luz); parede rachada à direita", 2,
    new[] { O(2.4f, 6.0f, 1.2f, 0.3f), O(6.6f, 6.0f, 1.2f, 0.3f), O(2.4f, 8.5f, 1.2f, 0.3f), O(6.6f, 8.5f, 1.2f, 0.3f), O(4.5f, 10.8f, 0.8f, 0.4f) },
    new[] { S(Ghost, 4.5f, 12.5f), S(Ghost, 1.0f, 10.0f), S(Archer, 7.5f, 11.8f) }, P(2.0f, 2.5f), P(8.2f, 10.0f), 40,
    wave: new[] { S(Ghost, 0.8f, 3.6f) }, trigger: Half, secret: S1, secretPos: P(Balance.RoomW, 7.2f))
{ Dark = true,
  Torches = new[] { P(0.3f, 4.5f), P(8.7f, 4.5f), P(0.3f, 11.5f), P(8.7f, 11.5f) } },
```

**R3-5 — Sarcófagos** (id local R5 = 4, global 39; ameaça 2). Lição: múmia é lenta (1 m/s) mas agarra: segura até levar 2 golpes ou até o dash (que larga a carga); não deixe ela encostar com a carga nas mãos.
```csharp
new RoomDef(R5, "R3-5", "Sarcófagos", "múmia é lenta (1 m/s) mas agarra: segura até levar 2 golpes ou até o dash (que larga a carga); não deixe ela encostar com a carga nas mãos", 2,
    new[] { O(2.0f, 7.5f, 0.4f, 0.9f), O(7.0f, 7.5f, 0.4f, 0.9f), O(4.5f, 10.5f, 0.9f, 0.4f) },
    new[] { S(Mummy, 2.0f, 10.5f), S(Mummy, 7.0f, 10.5f), S(Bat, 4.5f, 12.5f) }, P(2.0f, 2.5f), P(4.5f, 8.0f), 45,
    wave: new[] { S(Skeleton, 8.2f, 3.6f) }, trigger: Half)
{ Dark = true,
  Torches = new[] { P(0.3f, 5.0f), P(8.7f, 5.0f), P(8.7f, 11.0f) },
  Buried = new[] { B(4.5f, 5.0f, 35) } },
```

**R3-6 — Galeria das Gárgulas** (id local R6 = 5, global 40; ameaça 2). Lição: gárgula é pedra (invulnerável) até o courier passar a 3 m COM a carga; aí mergulha em linha (aviso 0,9 s) e fica 2,5 s no chão, vulnerável; sem a carga nas mãos ela dorme.
```csharp
new RoomDef(R6, "R3-6", "Galeria das Gárgulas", "gárgula é pedra (invulnerável) até o courier passar a 3 m COM a carga; aí mergulha em linha (aviso 0,9 s) e fica 2,5 s no chão, vulnerável; sem a carga nas mãos ela dorme", 2,
    new[] { O(2.8f, 5.5f, 0.5f, 0.5f), O(6.2f, 5.5f, 0.5f, 0.5f) },
    new[] { S(Gargoyle, 1.0f, 9.0f), S(Gargoyle, 8.0f, 9.0f), S(Archer, 4.5f, 12.0f) }, P(2.0f, 2.5f), P(0.8f, 11.5f), 45,
    wave: new[] { S(Bat, 0.8f, 3.6f), S(Bat, 8.2f, 3.6f) }, trigger: Half)
{ Dark = true,
  Torches = new[] { P(0.3f, 6.0f), P(8.7f, 6.0f), P(0.3f, 12.0f), P(8.7f, 12.0f) } },
```

**R3-7 — Emboscada na Cripta** (id local R7 = 6, global 41; ameaça 3). Lição: emboscada: passar da metade ergue dois esqueletos e um fantasma por cima e a porta fecha; mate perto da carga ou eles voltam.
```csharp
new RoomDef(R7, "R3-7", "Emboscada na Cripta", "emboscada: passar da metade ergue dois esqueletos e um fantasma por cima e a porta fecha; mate perto da carga ou eles voltam", 3,
    new[] { O(2.0f, 6.0f, 0.6f, 0.3f), O(7.0f, 6.0f, 0.6f, 0.3f), O(2.0f, 9.0f, 0.6f, 0.3f), O(7.0f, 9.0f, 0.6f, 0.3f) },
    new[] { S(Spider, 4.5f, 10.5f), S(Bat, 1.5f, 11.5f) }, P(2.0f, 2.5f),
    wave: new[] { S(Skeleton, 1.5f, 12.5f), S(Skeleton, 7.5f, 12.5f), S(Ghost, 4.5f, 12.8f) }, trigger: Half, waveLocks: true)
{ Dark = true,
  Torches = new[] { P(0.3f, 7.5f), P(8.7f, 7.5f), P(0.3f, 3.5f) } },
```

**R3-8 — Guardião do Mausoléu** (id local R8 = 7, global 42; ameaça 2). Lição: luta obrigatória no escuro: pouse a carga na luz (o fantasma só aparece nela) e longe das teias.
```csharp
new RoomDef(R8, "R3-8", "Guardião do Mausoléu", "luta obrigatória no escuro: pouse a carga na luz (o fantasma só aparece nela) e longe das teias", 2,
    new[] { O(4.5f, 6.0f, 1.4f, 0.4f) },
    new[] { S(Guard, 4.5f, 12.4f), S(Spider, 1.5f, 8.0f), S(Ghost, 8.0f, 7.0f) }, P(7.0f, 2.5f),
    wave: new[] { S(Ghost, 8.0f, 12.5f) }, trigger: Down)
{ Dark = true,
  Torches = new[] { P(0.3f, 11.0f), P(8.7f, 11.0f), P(0.3f, 4.0f) },
  Webs = new[] { C(2.5f, 9.6f, 0.9f), C(6.5f, 9.6f, 0.9f) } },
```

**R3-9 — Biblioteca Sombria** (id local R9 = 8, global 43; ameaça 2). Lição: três estantes em zigue-zague, só as passagens têm tocha; o fantasma corta por dentro das estantes; carga 3 s no chão chama reforço e fecha a porta; parede rachada à esquerda. *baú: 2º projeto do Gancho.*
```csharp
new RoomDef(R9, "R3-9", "Biblioteca Sombria", "três estantes em zigue-zague, só as passagens têm tocha; o fantasma corta por dentro das estantes; carga 3 s no chão chama reforço e fecha a porta; parede rachada à esquerda", 2,
    new[] { O(3.0f, 4.5f, 3.0f, 0.3f), O(6.0f, 8.0f, 3.0f, 0.3f), O(3.0f, 11.0f, 3.0f, 0.3f) },
    new[] { S(Ghost, 1.5f, 6.5f), S(Skeleton, 7.5f, 9.6f), S(Bat, 4.5f, 12.5f) }, P(7.0f, 2.5f), P(0.8f, 9.6f), 45,
    wave: new[] { S(Skeleton, 1.0f, 12.5f), S(Spider, 8.0f, 12.5f) }, trigger: Down, waveLocks: true, secret: S1, secretPos: P(0f, 2.6f))
{ Dark = true,
  Torches = new[] { P(8.7f, 4.5f), P(0.3f, 8.0f), P(8.7f, 11.0f) },
  Buried = new[] { B(4.5f, 9.6f, 40) } },
```

**R3-10 — Nave da Catedral** (id local R10 = 9, global 44; ameaça 3). Lição: múmia, arqueiro, gárgula e esqueleto; passar da metade chama reforço e fecha a porta.
```csharp
new RoomDef(R10, "R3-10", "Nave da Catedral", "múmia, arqueiro, gárgula e esqueleto; passar da metade chama reforço e fecha a porta", 3,
    new[] { O(2.0f, 6.0f, 0.6f, 0.6f), O(7.0f, 6.0f, 0.6f, 0.6f), O(4.5f, 9.0f, 0.6f, 0.6f) },
    new[] { S(Mummy, 2.5f, 10.5f), S(Archer, 7.5f, 11.5f), S(Gargoyle, 4.5f, 11.8f), S(Skeleton, 6.2f, 8.4f) }, P(2.0f, 2.5f), P(8.2f, 8.0f), 50,
    wave: new[] { S(Bat, 1.0f, 12.5f), S(Skeleton, 8.0f, 12.5f), S(Ghost, 4.5f, 12.8f) }, trigger: Half, waveLocks: true)
{ Dark = true,
  Torches = new[] { P(0.3f, 6.0f), P(8.7f, 6.0f), P(0.3f, 11.0f), P(8.7f, 11.0f) } },
```

**S3-1 — Cripta do Tesouro** (id local S1 = 10, global 45; ameaça 1). Lição: sala secreta: baú grande guardado por um esqueleto. *baú: 3º projeto da Bomba de fumaça.*
```csharp
new RoomDef(S1, "S3-1", "Cripta do Tesouro", "sala secreta: baú grande guardado por um esqueleto", 1,
    new[] { O(2.0f, 6.0f, 0.6f, 0.3f), O(7.0f, 6.0f, 0.6f, 0.3f) },
    new[] { S(Skeleton, 4.5f, 11.0f) }, P(2.0f, 2.5f), P(4.5f, 9.0f), 100)
{ Dark = true,
  Torches = new[] { P(0.3f, 9.0f), P(8.7f, 9.0f) } },
```

**M3 — Covil da Viúva** (id local M = 11, global 46; ameaça 3). Lição: mini-chefe: teias pela sala, puxa a carga com o fio, despenca do teto em cima da carga.
```csharp
new RoomDef(M, "M3", "Covil da Viúva", "mini-chefe: teias pela sala, puxa a carga com o fio, despenca do teto em cima da carga", 3,
    new[] { O(2.0f, 10.0f, 0.5f, 0.5f), O(7.0f, 10.0f, 0.5f, 0.5f) },
    new[] { S(SpiderWidow, 4.5f, 11.2f), S(Spider, 1.5f, 12.0f) }, P(2.0f, 2.5f))
{ Dark = true,
  Torches = new[] { P(0.3f, 4.0f), P(8.7f, 4.0f), P(0.3f, 10.0f), P(8.7f, 10.0f) },
  Webs = new[] { C(2.0f, 7.0f, 0.9f), C(7.0f, 7.0f, 0.9f), C(4.5f, 5.0f, 0.8f) } },
```

**B3 — Altar do Necromante** (id local B = 12, global 47; ameaça 3). Lição: chefe: ergue esqueletos das lápides em ondas, raio telegrafado, salta de lápide em lápide quando você chega perto.
```csharp
new RoomDef(B, "B3", "Altar do Necromante", "chefe: ergue esqueletos das lápides em ondas, raio telegrafado, salta de lápide em lápide quando você chega perto", 3,
    new[] { O(1.8f, 5.5f, 0.5f, 0.3f), O(7.2f, 5.5f, 0.5f, 0.3f), O(1.8f, 8.5f, 0.5f, 0.3f), O(7.2f, 8.5f, 0.5f, 0.3f), O(1.8f, 11.5f, 0.5f, 0.3f), O(7.2f, 11.5f, 0.5f, 0.3f), O(4.5f, 9.8f, 0.8f, 0.4f) },
    new[] { S(Necromancer, 4.5f, 11.6f) }, P(4.5f, 3.0f))
{ Dark = true,
  Torches = new[] { P(0.3f, 4.0f), P(8.7f, 4.0f), P(0.3f, 10.0f), P(8.7f, 10.0f) },
  Blinks = new[] { P(3.0f, 5.5f), P(6.0f, 5.5f), P(3.0f, 8.5f), P(6.0f, 8.5f), P(3.0f, 11.6f), P(6.0f, 11.6f) } },
```

**D3 — Entrega** (id local D = 13, global 48). Mesma planta da D1, com a pele da região (iluminada: o escuro acaba na entrega).
```csharp
new RoomDef(D, "D3", "Entrega", "o cliente espera no topo", 0,
    new[] { O(2.0f, 7.0f, 0.5f, 0.5f), O(7.0f, 7.0f, 0.5f, 0.5f) },
    new Spawn[0], P(2.0f, 2.5f)),
```

### 3.1 Contratos (16)

| Código | Índice global | Carga | Pagamento | Nível (Tier) | Camadas (salas de cada camada) | Bot estimado (s) | ParTime | Ovo/Gelo | Limite |
|---|---|---|---|---|---|---|---|---|---|
| M3-1 | 40 | Lanterna | 255 | 12 | R3-1 · R3-2/R3-3 · D3 | 38 | 60 | — | — |
| M3-2 | 41 | Caixa | 258 | 13 | R3-1 · R3-4/R3-5 · R3-2/R3-3 · D3 | 56 | 90 | — | — |
| M3-3 | 42 | Espelho | 261 | 14 | R3-3 · R3-6/R3-4 · R3-1/R3-8 · D3 | 60 | 95 | — | — |
| M3-4 | 43 | Ovo | 265 | 15 | R3-2 · R3-5/R3-1 · R3-3/R3-6 · R3-7/R3-4 · R3-9/R3-8 · D3 | 102 | 160 | 205 | — |
| M3-5 | 44 | Balão de gás | 269 | 16 | R3-5 · R3-1/R3-4 · R3-8/R3-2 · R3-3/R3-6 · R3-10/R3-9 · D3 | 102 | 160 | — | — |
| M3-6 | 45 | Relíquia | 273 | 17 | R3-4 · R3-2/R3-6 · R3-5/R3-1 · R3-7/R3-3 · R3-8/R3-9 · R3-4/R3-2 · D3 | 120 | 190 | — | — |
| M3-7 | 46 | Lanterna | 277 | 18 | R3-8 · R3-1/R3-7 · R3-3/R3-5/R3-4 · R3-6/R3-2 · R3-10/R3-9 · D3 | 108 | 170 | — | — |
| M3-8 | 47 | Caixa | 325 | 19 | R3-2 · R3-4/R3-6 · R3-9/R3-1 · R3-8/R3-5 · R3-3/R3-7 · M3 · D3 | 140 | 225 | — | — |
| M3-9 | 48 | Espelho | 285 | 20 | R3-1 · R3-6/R3-3 · R3-10/R3-2 · R3-4/R3-9 · R3-5/R3-8 · R3-7/R3-6 · D3 | 122 | 195 | — | — |
| M3-10 | 49 | Mensagem | 289 | 21 | R3-5 · R3-8/R3-3 · R3-10/R3-2 · R3-4/R3-9 · R3-1/R3-6 · R3-7/R3-8 · D3 | 54 | 85 | — | 135 |
| M3-11 | 50 | Balão de gás | 293 | 22 | R3-7 · R3-9/R3-3 · R3-6/R3-10 · R3-5/R3-2 · R3-8/R3-4 · R3-1/R3-7 · D3 | 132 | 210 | — | — |
| M3-12 | 51 | Coração arcano | 297 | 23 | R3-4 · R3-6/R3-1 · R3-2/R3-10 · R3-9/R3-8 · R3-3/R3-5 · R3-7/R3-4 · D3 | 124 | 200 | — | — |
| M3-13 | 52 | Barril | 301 | 24 | R3-1 · R3-5/R3-7 · R3-8/R3-10/R3-3 · R3-2/R3-6 · R3-9/R3-4 · R3-5/R3-1 · D3 | 118 | 190 | — | — |
| M3-14 | 53 | Gaiola do grifinho | 305 | 25 | R3-6 · R3-3/R3-10 · R3-7/R3-2 · R3-4/R3-9 · R3-8/R3-1 · R3-5/R3-3 · D3 | 124 | 200 | — | — |
| M3-15 | 54 | Espelho | 309 | 26 | R3-10 · R3-2/R3-8 · R3-6/R3-5 · R3-9/R3-1/R3-4 · R3-3/R3-7 · R3-10/R3-6 · D3 | 131 | 210 | — | — |
| M3-16 | 55 | Lanterna | 410 | 27 | R3-10 · R3-8/R3-7 · R3-9/R3-6/R3-3 · R3-5/R3-2 · B3 · D3 | 202 | 325 | — | — |

```csharp
Content.Make("M3-1", CargoType.Lantern, 255, "região 3: escuro; lanterna ilumina 5,5 m", 60f, 0f, 0f, 12, false, D, L(R1), L(R2, R3)),   // global 40
Content.Make("M3-2", CargoType.Box, 258, "esqueletos, teias e fantasmas sem lanterna", 90f, 0f, 0f, 13, false, D, L(R1), L(R4, R5), L(R2, R3)),   // global 41
Content.Make("M3-3", CargoType.Mirror, 261, "espelho encantado: reflete flecha e teia", 95f, 0f, 0f, 14, false, D, L(R3), L(R6, R4), L(R1, R8)),   // global 42
Content.Make("M3-4", CargoType.Egg, 265, "ovo no escuro", 160f, 205f, 0f, 15, true, D, L(R2), L(R5, R1), L(R3, R6), L(R7, R4), L(R9, R8)),   // global 43
Content.Make("M3-5", CargoType.Balloon, 269, "balão de gás: leve, mas escapa do chão em 4 s", 160f, 0f, 0f, 16, true, D, L(R5), L(R1, R4), L(R8, R2), L(R3, R6), L(R10, R9)),   // global 44
Content.Make("M3-6", CargoType.Relic, 273, "relíquia entre múmias e gárgulas", 190f, 0f, 0f, 17, true, D, L(R4), L(R2, R6), L(R5, R1), L(R7, R3), L(R8, R9), L(R4, R2)),   // global 45
Content.Make("M3-7", CargoType.Lantern, 277, "lanterna em 5 camadas", 170f, 0f, 0f, 18, true, D, L(R8), L(R1, R7), L(R3, R5, R4), L(R6, R2), L(R10, R9)),   // global 46
Content.Make("M3-8", CargoType.Box, 325, "mini-chefe: Viúva Aranha", 225f, 0f, 0f, 19, true, D, L(R2), L(R4, R6), L(R9, R1), L(R8, R5), L(R3, R7), L(M)),   // global 47
Content.Make("M3-9", CargoType.Mirror, 285, "espelho em 6 camadas", 195f, 0f, 0f, 20, true, D, L(R1), L(R6, R3), L(R10, R2), L(R4, R9), L(R5, R8), L(R7, R6)),   // global 48
Content.Make("M3-10", CargoType.Message, 289, "mensagem no escuro", 85f, 0f, 135f, 21, true, D, L(R5), L(R8, R3), L(R10, R2), L(R4, R9), L(R1, R6), L(R7, R8)),   // global 49
Content.Make("M3-11", CargoType.Balloon, 293, "balão em 6 camadas", 210f, 0f, 0f, 22, true, D, L(R7), L(R9, R3), L(R6, R10), L(R5, R2), L(R8, R4), L(R1, R7)),   // global 50
Content.Make("M3-12", CargoType.ArcaneHeart, 297, "coração no escuro: a horda vem para a luz", 200f, 0f, 0f, 23, true, D, L(R4), L(R6, R1), L(R2, R10), L(R9, R8), L(R3, R5), L(R7, R4)),   // global 51
Content.Make("M3-13", CargoType.Barrel, 301, "barril entre esqueletos", 190f, 0f, 0f, 24, true, D, L(R1), L(R5, R7), L(R8, R10, R3), L(R2, R6), L(R9, R4), L(R5, R1)),   // global 52
Content.Make("M3-14", CargoType.GriffinCage, 305, "gaiola na cripta: o grito acorda as gárgulas", 200f, 0f, 0f, 25, true, D, L(R6), L(R3, R10), L(R7, R2), L(R4, R9), L(R8, R1), L(R5, R3)),   // global 53
Content.Make("M3-15", CargoType.Mirror, 309, "espelho em 6 camadas", 210f, 0f, 0f, 26, true, D, L(R10), L(R2, R8), L(R6, R5), L(R9, R1, R4), L(R3, R7), L(R10, R6)),   // global 54
Content.Make("M3-16", CargoType.Lantern, 410, "chefe: Necromante", 325f, 0f, 0f, 27, true, D, L(R10), L(R8, R7), L(R9, R6, R3), L(R5, R2), L(B)),   // global 55
```

---

## 4. Região 4 — Forja do Dragão

**Tema:** forja anã tomada por kobolds e criaturas de fogo, rios de lava, respiros no chão, correntes e bigornas. **Verbo do lugar:** *empurrar e ritmar* — a lava é parede baixa (tiro passa, gente não) e quem é empurrado nela se queima; respiros soltam fogo num ritmo fixo; fogo no chão nega o lugar onde você pousaria a carga.

**Destrava:** 95 estrelas e o Necromante (B3) vencido. **Inimigos novos:** salamandra, diabrete, kobold incendiário, elemental de magma. **Mini:** Capataz Kobold. **Chefe final:** Dragão Ancião. **Cargas novas:** planta carnívora (M4-2), mapa do tesouro (M4-3).

**Paleta:** piso de basalto `#35303A` (sombra `#25212A`, luz `#4A4350`); parede `#423A40`; lava `#FF6A00` com miolo `#FFC14D` e crosta `#7A2E12`; respiro apagado `#5A4A44`, brilho `#FF8C1A`; bigorna `#4F5866`; corrente `#7A7F8A`; fumaça `#2A2730`. **Laranja-fogo = queima** (lava, respiro e inimigos de fogo dividem a cor de propósito); o laranja-ferrugem fosco do Bruto continua só dele.

Constantes locais (`Regiao4.cs`): `public const int R1 = 0, R2 = 1, R3 = 2, R4 = 3, R5 = 4, R6 = 5, R7 = 6, R8 = 7, R9 = 8, R10 = 9, S1 = 10, M = 11, B = 12, D = 13;`

Ameaça hipotética: 1 = R4-1, R4-9; 2 = R4-2, R4-3, R4-4, R4-5, R4-7; 3 = R4-6, R4-8, R4-10.

**R4-1 — Ponte de Basalto** (id local R1 = 0, global 49; ameaça 1). Lição: lava é parede baixa: não se anda nela, mas tiro, flecha e frasco passam por cima; inimigo empurrado na lava pelo dash leva 3 de dano.
```csharp
new RoomDef(R1, "R4-1", "Ponte de Basalto", "lava é parede baixa: não se anda nela, mas tiro, flecha e frasco passam por cima; inimigo empurrado na lava pelo dash leva 3 de dano", 1,
    new[] { O(4.5f, 10.5f, 0.4f, 0.4f) },
    new[] { S(Salamander, 4.5f, 9.4f), S(KoboldBomber, 7.5f, 11.5f) }, P(2.0f, 2.5f), P(4.5f, 5.0f), 35,
    wave: new[] { S(Salamander, 0.8f, 3.6f) }, trigger: Half)
{ Lava = new[] { O(0.7f, 7.0f, 0.7f, 0.7f), O(4.5f, 7.0f, 1.5f, 0.7f), O(8.3f, 7.0f, 0.7f, 0.7f) } },
```

**R4-2 — Respiros** (id local R2 = 1, global 50; ameaça 2). Lição: respiro brilha 0,8 s e solta fogo por 1 s (2 de dano; racha a carga no chão; derrete gelo; 3 de dano em inimigo): passe logo depois do fogo.
```csharp
new RoomDef(R2, "R4-2", "Respiros", "respiro brilha 0,8 s e solta fogo por 1 s (2 de dano; racha a carga no chão; derrete gelo; 3 de dano em inimigo): passe logo depois do fogo", 2,
    new[] { O(1.0f, 8.5f, 0.5f, 0.3f), O(8.0f, 8.5f, 0.5f, 0.3f) },
    new[] { S(Bat, 1.5f, 11.0f), S(Slime, 7.5f, 10.5f) }, P(2.0f, 2.5f), P(8.2f, 6.6f), 40,
    wave: new[] { S(Bat, 8.2f, 3.6f) }, trigger: Half)
{ Vents = new[] { V(2.0f, 5.0f, 1.0f, 0.0f), V(4.5f, 5.0f, 1.0f, 2.0f), V(7.0f, 5.0f, 1.0f, 0.0f), V(3.2f, 8.5f, 1.0f, 1.0f), V(5.8f, 8.5f, 1.0f, 3.0f), V(4.5f, 11.0f, 1.0f, 2.0f) },
  Buried = new[] { B(0.8f, 3.4f, 40) } },
```

**R4-3 — Ninho de Salamandras** (id local R3 = 2, global 51; ameaça 2). Lição: a salamandra deixa rastro de fogo por 3 s (1 de dano ao pisar; derrete o gelo) e circula você a 2–3 m: não deixe ela fechar o círculo em volta da carga; parede rachada à esquerda. *baú: 1º projeto da Armadilha de urso.*
```csharp
new RoomDef(R3, "R4-3", "Ninho de Salamandras", "a salamandra deixa rastro de fogo por 3 s (1 de dano ao pisar; derrete o gelo) e circula você a 2–3 m: não deixe ela fechar o círculo em volta da carga; parede rachada à esquerda", 2,
    new[] { O(2.5f, 7.0f, 0.6f, 0.6f), O(6.5f, 7.0f, 0.6f, 0.6f), O(4.5f, 10.5f, 1.0f, 0.4f) },
    new[] { S(Salamander, 1.5f, 10.0f), S(Salamander, 7.5f, 10.0f), S(Thief, 8.0f, 12.0f) }, P(2.0f, 2.5f), P(0.8f, 12.0f), 40,
    wave: new[] { S(Salamander, 8.2f, 3.6f) }, trigger: Half, secret: S1, secretPos: P(0f, 5.0f))
{ Lava = new[] { O(4.5f, 4.6f, 1.0f, 0.4f) } },
```

**R4-4 — Oficina do Diabrete** (id local R4 = 3, global 52; ameaça 2). Lição: o diabrete troca de lugar com a carga no chão (redemoinho de 1 s nos dois pontos; precisa ver a carga): acerte-o no redemoinho ou pouse atrás de uma bigorna. *baú: 3º projeto do Gancho.*
```csharp
new RoomDef(R4, "R4-4", "Oficina do Diabrete", "o diabrete troca de lugar com a carga no chão (redemoinho de 1 s nos dois pontos; precisa ver a carga): acerte-o no redemoinho ou pouse atrás de uma bigorna", 2,
    new[] { O(2.0f, 6.5f, 0.6f, 0.4f), O(7.0f, 6.5f, 0.6f, 0.4f), O(4.5f, 9.5f, 0.6f, 0.4f) },
    new[] { S(Imp, 7.5f, 11.0f), S(Imp, 2.0f, 12.0f), S(Bat, 4.5f, 12.5f) }, P(2.0f, 2.5f), P(8.2f, 9.0f), 45,
    wave: new[] { S(Imp, 8.2f, 3.6f) }, trigger: Half)
{ Lava = new[] { O(0.6f, 10.5f, 0.6f, 1.0f) },
  Buried = new[] { B(4.5f, 11.5f, 45) } },
```

**R4-5 — Bancada dos Kobolds** (id local R5 = 4, global 53; ameaça 2). Lição: o kobold joga frasco em arco (círculo de aviso 0,9 s) que vira poça de fogo de 1 m por 4 s: não pouse onde o frasco cai, mude de lugar.
```csharp
new RoomDef(R5, "R4-5", "Bancada dos Kobolds", "o kobold joga frasco em arco (círculo de aviso 0,9 s) que vira poça de fogo de 1 m por 4 s: não pouse onde o frasco cai, mude de lugar", 2,
    new[] { O(2.0f, 6.0f, 0.6f, 0.4f), O(7.0f, 6.0f, 0.6f, 0.4f), O(4.5f, 8.5f, 1.4f, 0.3f) },
    new[] { S(KoboldBomber, 2.0f, 11.5f), S(KoboldBomber, 7.0f, 11.5f), S(Thief, 8.0f, 9.6f) }, P(2.0f, 2.5f), P(0.8f, 9.0f), 40,
    wave: new[] { S(Salamander, 1.0f, 12.5f), S(Bat, 8.0f, 12.5f) }, trigger: Down),
```

**R4-6 — Fundição** (id local R6 = 5, global 54; ameaça 3). Lição: o elemental de magma ao morrer se divide em 2 brasas; lava dos lados: empurre as brasas na lava com o dash. *baú: 2º projeto da Armadilha de urso.*
```csharp
new RoomDef(R6, "R4-6", "Fundição", "o elemental de magma ao morrer se divide em 2 brasas; lava dos lados: empurre as brasas na lava com o dash", 3,
    new[] { O(4.5f, 10.8f, 0.8f, 0.4f) },
    new[] { S(MagmaElemental, 4.5f, 9.0f), S(MagmaElemental, 2.6f, 11.6f), S(Bat, 7.0f, 11.6f) }, P(2.0f, 2.5f), P(8.2f, 4.0f), 45,
    wave: new[] { S(Salamander, 0.8f, 3.6f), S(Salamander, 8.2f, 3.6f) }, trigger: Half)
{ Lava = new[] { O(1.0f, 7.5f, 1.0f, 1.5f), O(8.0f, 7.5f, 1.0f, 1.5f) },
  Buried = new[] { B(4.5f, 5.5f, 45) } },
```

**R4-7 — Guarda da Forja** (id local R7 = 6, global 55; ameaça 2). Lição: luta obrigatória entre dois respiros: pouse a carga no lado frio e conte o ritmo.
```csharp
new RoomDef(R7, "R4-7", "Guarda da Forja", "luta obrigatória entre dois respiros: pouse a carga no lado frio e conte o ritmo", 2,
    new[] { O(4.5f, 6.0f, 1.4f, 0.4f) },
    new[] { S(Guard, 4.5f, 12.4f), S(KoboldBomber, 1.5f, 10.5f), S(Imp, 7.5f, 10.5f) }, P(7.0f, 2.5f),
    wave: new[] { S(Imp, 8.0f, 12.5f) }, trigger: Down)
{ Vents = new[] { V(2.5f, 8.2f, 1.0f, 0.0f), V(6.5f, 8.2f, 1.0f, 2.0f) } },
```

**R4-8 — Emboscada de Brasa** (id local R8 = 7, global 56; ameaça 3). Lição: emboscada: passar da metade traz um elemental e dois diabretes e a porta fecha; os respiros acertam inimigos também.
```csharp
new RoomDef(R8, "R4-8", "Emboscada de Brasa", "emboscada: passar da metade traz um elemental e dois diabretes e a porta fecha; os respiros acertam inimigos também", 3,
    new[] { O(2.5f, 5.5f, 0.6f, 0.4f), O(6.5f, 5.5f, 0.6f, 0.4f), O(4.5f, 9.0f, 1.2f, 0.4f) },
    new[] { S(Salamander, 8.0f, 10.5f), S(KoboldBomber, 1.5f, 11.5f) }, P(2.0f, 2.5f),
    wave: new[] { S(MagmaElemental, 4.5f, 12.5f), S(Imp, 1.5f, 12.8f), S(Imp, 7.5f, 12.8f) }, trigger: Half, waveLocks: true)
{ Vents = new[] { V(1.5f, 8.0f, 1.0f, 1.0f), V(7.5f, 8.0f, 1.0f, 3.0f) },
  Buried = new[] { B(8.2f, 3.8f, 50) } },
```

**R4-9 — Sala das Correntes** (id local R9 = 8, global 57; ameaça 1). Lição: baú enterrado (X) e parede rachada disfarçada só aparecem com o Mapa ou o perk Faro de baú; ladrões correm 30% mais atrás do Mapa.
```csharp
new RoomDef(R9, "R4-9", "Sala das Correntes", "baú enterrado (X) e parede rachada disfarçada só aparecem com o Mapa ou o perk Faro de baú; ladrões correm 30% mais atrás do Mapa", 1,
    new[] { O(4.5f, 7.5f, 0.6f, 0.4f), O(2.0f, 10.0f, 0.5f, 0.5f), O(7.0f, 10.0f, 0.5f, 0.5f) },
    new[] { S(Thief, 1.0f, 8.0f), S(Thief, 8.0f, 8.0f), S(Imp, 4.5f, 12.0f) }, P(2.0f, 2.5f),
    wave: new[] { S(Thief, 8.0f, 12.5f) }, trigger: Down, secret: S1, secretPos: P(Balance.RoomW, 5.5f))
{ Buried = new[] { B(1.0f, 5.5f, 60) },
  SecretHidden = true },
```

**R4-10 — Coração da Forja** (id local R10 = 9, global 58; ameaça 3). Lição: Bruto, elemental, kobold e salamandra entre respiros e um poço de lava; passar da metade chama reforço e fecha a porta.
```csharp
new RoomDef(R10, "R4-10", "Coração da Forja", "Bruto, elemental, kobold e salamandra entre respiros e um poço de lava; passar da metade chama reforço e fecha a porta", 3,
    new[] { O(1.5f, 10.5f, 0.5f, 0.5f), O(7.5f, 10.5f, 0.5f, 0.5f) },
    new[] { S(Brute, 3.0f, 11.8f), S(MagmaElemental, 6.0f, 11.8f), S(KoboldBomber, 8.2f, 8.5f) }, P(2.0f, 2.5f), P(0.8f, 8.5f), 55,
    wave: new[] { S(Imp, 1.0f, 12.8f), S(Salamander, 8.0f, 12.8f), S(KoboldBomber, 4.5f, 12.8f) }, trigger: Half, waveLocks: true)
{ Lava = new[] { O(4.5f, 9.5f, 1.0f, 0.5f) },
  Vents = new[] { V(2.0f, 6.5f, 1.0f, 0.0f), V(7.0f, 6.5f, 1.0f, 2.0f) },
  Buried = new[] { B(8.2f, 11.6f, 55) } },
```

**S4-1 — Cofre do Ferreiro** (id local S1 = 10, global 59; ameaça 1). Lição: sala secreta: baú grande, um baú enterrado e um diabrete. *baú: 3º projeto da Armadilha de urso.*
```csharp
new RoomDef(S1, "S4-1", "Cofre do Ferreiro", "sala secreta: baú grande, um baú enterrado e um diabrete", 1,
    new[] { O(2.0f, 6.0f, 0.5f, 0.5f), O(7.0f, 6.0f, 0.5f, 0.5f) },
    new[] { S(Imp, 6.5f, 11.0f) }, P(2.0f, 2.5f), P(4.5f, 10.0f), 110)
{ Buried = new[] { B(1.0f, 11.5f, 50) } },
```

**M4 — Pátio do Capataz** (id local M = 11, global 60; ameaça 3). Lição: mini-chefe: chicote de 3 m, chama kobolds e joga frascos; respiros dos lados.
```csharp
new RoomDef(M, "M4", "Pátio do Capataz", "mini-chefe: chicote de 3 m, chama kobolds e joga frascos; respiros dos lados", 3,
    new[] { O(4.5f, 6.0f, 0.8f, 0.4f) },
    new[] { S(KoboldForeman, 4.5f, 11.0f), S(KoboldBomber, 8.0f, 12.0f) }, P(2.0f, 2.5f))
{ Vents = new[] { V(2.0f, 8.4f, 1.0f, 0.0f), V(7.0f, 8.4f, 1.0f, 2.0f) } },
```

**B4 — Ninho do Dragão** (id local B = 12, global 61; ameaça 3). Lição: chefe final: sopro em cone (os pilares cobrem), voo e pouso em área, ovos de salamandra; fissuras de lava na fase 2.
```csharp
new RoomDef(B, "B4", "Ninho do Dragão", "chefe final: sopro em cone (os pilares cobrem), voo e pouso em área, ovos de salamandra; fissuras de lava na fase 2", 3,
    new[] { O(2.0f, 6.0f, 0.6f, 0.6f), O(7.0f, 6.0f, 0.6f, 0.6f), O(2.0f, 9.5f, 0.6f, 0.6f), O(7.0f, 9.5f, 0.6f, 0.6f) },
    new[] { S(ElderDragon, 4.5f, 11.0f) }, P(4.5f, 3.0f))
{ Lava = new[] { O(0.4f, 11.4f, 0.4f, 0.7f), O(8.6f, 11.4f, 0.4f, 0.7f) },
  Vents = new[] { V(4.5f, 7.8f, 1.2f, 0.0f), V(1.0f, 4.2f, 1.0f, 2.0f), V(8.0f, 4.2f, 1.0f, 4.0f) } },
```

**D4 — Entrega** (id local D = 13, global 62). Mesma planta da D1, com a pele da região.
```csharp
new RoomDef(D, "D4", "Entrega", "o cliente espera no topo", 0,
    new[] { O(2.0f, 7.0f, 0.5f, 0.5f), O(7.0f, 7.0f, 0.5f, 0.5f) },
    new Spawn[0], P(2.0f, 2.5f)),
```

### 4.1 Contratos (16)

| Código | Índice global | Carga | Pagamento | Nível (Tier) | Camadas (salas de cada camada) | Bot estimado (s) | ParTime | Ovo/Gelo | Limite |
|---|---|---|---|---|---|---|---|---|---|
| M4-1 | 56 | Caixa | 310 | 16 | R4-1 · R4-2/R4-3 · D4 | 38 | 60 | — | — |
| M4-2 | 57 | Planta carnívora | 313 | 17 | R4-3 · R4-1/R4-4 · R4-2/R4-5 · D4 | 58 | 90 | — | — |
| M4-3 | 58 | Mapa do tesouro | 316 | 18 | R4-9 · R4-1/R4-5 · R4-4/R4-3 · D4 | 54 | 85 | — | — |
| M4-4 | 59 | Relíquia | 320 | 19 | R4-2 · R4-4/R4-1 · R4-5/R4-3 · R4-7/R4-9 · R4-6/R4-2 · D4 | 96 | 155 | — | — |
| M4-5 | 60 | Cristal de gelo | 324 | 20 | R4-1 · R4-5/R4-9 · R4-3/R4-4 · R4-7/R4-2 · R4-10/R4-6 · D4 | 98 | 160 | 215 | — |
| M4-6 | 61 | Balão de gás | 328 | 21 | R4-4 · R4-2/R4-9 · R4-1/R4-6 · R4-3/R4-5 · R4-8/R4-7 · R4-4/R4-1 · D4 | 116 | 185 | — | — |
| M4-7 | 62 | Planta carnívora | 332 | 22 | R4-7 · R4-1/R4-8 · R4-3/R4-5/R4-2 · R4-6/R4-9 · R4-10/R4-4 · D4 | 106 | 170 | — | — |
| M4-8 | 63 | Caixa | 380 | 23 | R4-5 · R4-3/R4-6 · R4-9/R4-1 · R4-7/R4-4 · R4-2/R4-8 · M4 · D4 | 140 | 225 | — | — |
| M4-9 | 64 | Ovo | 340 | 24 | R4-1 · R4-6/R4-2 · R4-10/R4-3 · R4-4/R4-9 · R4-5/R4-7 · R4-8/R4-6 · D4 | 122 | 195 | 245 | — |
| M4-10 | 65 | Mapa do tesouro | 344 | 25 | R4-9 · R4-4/R4-2 · R4-6/R4-10 · R4-1/R4-5 · R4-7/R4-3 · R4-9/R4-8 · D4 | 119 | 190 | — | — |
| M4-11 | 66 | Barril | 348 | 26 | R4-3 · R4-7/R4-1 · R4-5/R4-10 · R4-2/R4-9 · R4-8/R4-4 · R4-6/R4-3 · D4 | 122 | 195 | — | — |
| M4-12 | 67 | Mensagem | 352 | 27 | R4-4 · R4-6/R4-9 · R4-1/R4-8/R4-5 · R4-10/R4-2 · R4-7/R4-3 · R4-4/R4-6 · D4 | 53 | 85 | — | 130 |
| M4-13 | 68 | Caixa pesada | 356 | 28 | R4-2 · R4-9/R4-5 · R4-8/R4-3 · R4-4/R4-6 · R4-1/R4-10 · R4-7/R4-2 · D4 | 122 | 195 | — | — |
| M4-14 | 69 | Planta carnívora | 360 | 29 | R4-6 · R4-3/R4-8 · R4-10/R4-1 · R4-5/R4-7/R4-4 · R4-2/R4-9 · R4-8/R4-3 · D4 | 127 | 205 | — | — |
| M4-15 | 70 | Bolo | 364 | 30 | R4-10 · R4-2/R4-7 · R4-4/R4-9 · R4-6/R4-1/R4-5 · R4-3/R4-8 · R4-10/R4-4 · D4 | 130 | 210 | — | — |
| M4-16 | 71 | Caixa | 480 | 31 | R4-10 · R4-8/R4-7 · R4-6/R4-5/R4-2 · R4-3/R4-4 · B4 · D4 | 222 | 355 | — | — |

```csharp
Content.Make("M4-1", CargoType.Box, 310, "região 4: lava e respiros", 60f, 0f, 0f, 16, false, D, L(R1), L(R2, R3)),   // global 56
Content.Make("M4-2", CargoType.Flytrap, 313, "planta carnívora: torre que morde todo mundo", 90f, 0f, 0f, 17, false, D, L(R3), L(R1, R4), L(R2, R5)),   // global 57
Content.Make("M4-3", CargoType.TreasureMap, 316, "mapa do tesouro: baús enterrados e ladrões mais rápidos", 85f, 0f, 0f, 18, false, D, L(R9), L(R1, R5), L(R4, R3)),   // global 58
Content.Make("M4-4", CargoType.Relic, 320, "relíquia na forja", 155f, 0f, 0f, 19, true, D, L(R2), L(R4, R1), L(R5, R3), L(R7, R9), L(R6, R2)),   // global 59
Content.Make("M4-5", CargoType.IceCrystal, 324, "cristal perto da lava: derrete 3x mais rápido", 160f, 215f, 0f, 20, true, D, L(R1), L(R5, R9), L(R3, R4), L(R7, R2), L(R10, R6)),   // global 60
Content.Make("M4-6", CargoType.Balloon, 328, "balão entre respiros: fogo fura", 185f, 0f, 0f, 21, true, D, L(R4), L(R2, R9), L(R1, R6), L(R3, R5), L(R8, R7), L(R4, R1)),   // global 61
Content.Make("M4-7", CargoType.Flytrap, 332, "planta em 5 camadas", 170f, 0f, 0f, 22, true, D, L(R7), L(R1, R8), L(R3, R5, R2), L(R6, R9), L(R10, R4)),   // global 62
Content.Make("M4-8", CargoType.Box, 380, "mini-chefe: Capataz Kobold", 225f, 0f, 0f, 23, true, D, L(R5), L(R3, R6), L(R9, R1), L(R7, R4), L(R2, R8), L(M)),   // global 63
Content.Make("M4-9", CargoType.Egg, 340, "ovo na forja", 195f, 245f, 0f, 24, true, D, L(R1), L(R6, R2), L(R10, R3), L(R4, R9), L(R5, R7), L(R8, R6)),   // global 64
Content.Make("M4-10", CargoType.TreasureMap, 344, "mapa em 6 camadas", 190f, 0f, 0f, 25, true, D, L(R9), L(R4, R2), L(R6, R10), L(R1, R5), L(R7, R3), L(R9, R8)),   // global 65
Content.Make("M4-11", CargoType.Barrel, 348, "barril perto do fogo: fogo no chão acende o pavio", 195f, 0f, 0f, 26, true, D, L(R3), L(R7, R1), L(R5, R10), L(R2, R9), L(R8, R4), L(R6, R3)),   // global 66
Content.Make("M4-12", CargoType.Message, 352, "mensagem na forja", 85f, 0f, 130f, 27, true, D, L(R4), L(R6, R9), L(R1, R8, R5), L(R10, R2), L(R7, R3), L(R4, R6)),   // global 67
Content.Make("M4-13", CargoType.Heavy, 356, "caixa pesada entre respiros", 195f, 0f, 0f, 28, true, D, L(R2), L(R9, R5), L(R8, R3), L(R4, R6), L(R1, R10), L(R7, R2)),   // global 68
Content.Make("M4-14", CargoType.Flytrap, 360, "planta em 6 camadas", 205f, 0f, 0f, 29, true, D, L(R6), L(R3, R8), L(R10, R1), L(R5, R7, R4), L(R2, R9), L(R8, R3)),   // global 69
Content.Make("M4-15", CargoType.Cake, 364, "bolo na forja", 210f, 0f, 0f, 30, true, D, L(R10), L(R2, R7), L(R4, R9), L(R6, R1, R5), L(R3, R8), L(R10, R4)),   // global 70
Content.Make("M4-16", CargoType.Box, 480, "chefe final: Dragão Ancião", 355f, 0f, 0f, 31, true, D, L(R10), L(R8, R7), L(R6, R5, R2), L(R3, R4), L(B)),   // global 71
```

---

## 5. Inimigos (14 novos) e chefes (6 novos)

Os 20 novos já estão no `EnemyType` do núcleo, com números provisórios (`Provisional = true`); os daqui os substituem. Faltam **duas variações no fim do enum**, que não contam nos 20: `MagmaShard` (brasa de magma) e `SalamanderEgg` (ovo de salamandra do Dragão). `Boss = true` nos 6 chefes novos, como o núcleo já fez (a mensagem não abre a porta deles; o dash não os empurra).

Colunas no formato do `EnemyDef(tipo, nome, vida, velocidade, raio, dano, alcance, gatilho, telegraph, recarga, tranca)`. Alcance e gatilho medidos do centro do inimigo até a borda do alvo, como na região 1.

| Enum | Nome | Vida | Vel. (m/s) | Raio | Dano | Alcance | Gatilho | Telegraph (s) | Recarga (s) | Tranca | Vida escala | Ícone de risco (§1.3) |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| `GoblinMiner` | Goblin mineiro | 4 | 1,5 | 0,35 | 1 | 6,0 (arremesso) | 6,0 | 0,9 | 2,6 | não | sim | Arqueiro |
| `Mole` | Toupeira | 3 | 2,2 (por baixo) | 0,35 | 1 | 0,9 (emergir) | — | 0,8 | 2,0 (fora) | não | sim | Pancada |
| `BogFrog` | Sapo do lodo | 5 | 0 | 0,45 | 1 | 3,0 (língua) | 3,0 | 0,7 | 2,5 | não | sim | Ladrão |
| `BombBeetle` | Besouro-bomba | 2 | 3,0 | 0,30 | 2 | 1,5 (explosão) | 0,8 | 1,0 (pavio) | — (1 vez) | não | não | Pancada |
| `Minecart` | Vagonete | invulnerável | 6,0 | 0,60 | 2 (6 em inimigo) | 0,6 | — | 1,2 (sino) | 2,0 (túnel) | não | — | Pancada |
| `Ghost` | Fantasma | 3 | 1,8 | 0,35 | 1 (toque) | 0,5 (possuir) | — | 0,6 | 1,5 | não | não | Ladrão |
| `Skeleton` | Esqueleto | 5 | 1,5 | 0,40 | 1 | 1,0 | 0,9 | 0,5 | 1,2 | não | sim | — |
| `Spider` | Aranha | 3 | 2,0 | 0,35 | 1 | 0,8 (mordida) / 5,0 (teia) | 0,7 | 0,4 / 0,6 | 1,4 / 4,0 | não | sim | — |
| `Mummy` | Múmia | 9 | 1,0 | 0,45 | 1 | 0,9 | 0,8 | 0,6 | 2,0 | não | sim | — |
| `Gargoyle` | Gárgula | 8 | 0 (mergulho 9,0) | 0,50 | 2 | 6,0 (linha) | 3,0 (com a carga) | 0,9 | 4,0 | não | sim | Pancada |
| `Salamander` | Salamandra | 4 | 2,4 | 0,35 | 1 (toque) | — | — | — | 1,2 | não | não | Pancada (acerto) |
| `Imp` | Diabrete | 3 | 2,0 | 0,30 | 1 | 0,8 | 0,7 | 1,0 (troca) / 0,5 | 5,0 / 3,0 | não | sim | Ladrão |
| `KoboldBomber` | Kobold incendiário | 4 | 1,6 | 0,35 | 1 | 6,0 (frasco) | 6,0 | 0,9 | 3,0 | não | sim | Arqueiro |
| `MagmaElemental` | Elemental de magma | 10 | 1,2 | 0,60 | 2 | 1,2 | 1,0 | 0,8 | 1,6 | não | sim | Pancada |
| `MagmaShard` | Brasa de magma | 4 | 1,8 | 0,40 | 1 | 0,8 | 0,7 | 0,5 | 1,2 | não | não | — |
| `SalamanderEgg` | Ovo de salamandra | 2 | 0 | 0,30 | — | — | — | 6,0 (choca) | — | não | não | — |
| `MoleQueen` | Rainha Toupeira | 22 | 2,4 (por baixo) | 0,70 | 2 | 1,4 (emergir) / 1,2 (patada) | 1,2 | 1,0 / 0,6 | 3,5 (fora) | sim | sim | Ladrão |
| `StoneGolem` | Golem de Pedra | 40 | 1,0 | 1,00 | 3 | 2,2 | 1,8 | 1,1 | 2,2 | sim | sim | Pancada |
| `SpiderWidow` | Viúva Aranha | 20 | 1,8 | 0,70 | 2 | 1,1 / 7,0 (fio) | 1,0 | 0,6 / 1,0 | 1,5 | sim | sim | Ladrão |
| `Necromancer` | Necromante | 36 | 0 (salta) | 0,50 | 2 | 9,0 (raio) | 9,0 | 1,2 | 3,0 | sim | sim | Arqueiro (acerto) |
| `KoboldForeman` | Capataz Kobold | 18 | 1,8 | 0,55 | 2 | 3,0 (chicote) | 2,6 | 0,7 | 1,8 | sim | sim | Arqueiro, Ladrão (acerto) |
| `ElderDragon` | Dragão Ancião | 48 | 0,8 | 1,30 | 3 | 6,0 (cone) | 6,0 | 1,4 | 3,0 | sim | sim | Pancada |

Vida dos chefes no contrato deles (× `HpMul`): Rainha 59 (M2-8, Tier 15) · Golem 146 (M2-16, Tier 23; ver ponto fraco) · Viúva 63 (M3-8, Tier 19) · Necromante 148 (M3-16, Tier 27) · Capataz 66 (M4-8, Tier 23) · Dragão 221 (M4-16, Tier 31). Referência medida: Capitão 53 no C12 (30 s na M1), Ogro 164 no C24 (93 s na B1).

### 5.1 Comportamento, passo a passo

**Goblin mineiro** (`GoblinMiner`, região 2). Estados: `Idle` (ou dormindo) → `Chase` até ter o courier a ≤ 6 m → `Telegraph` 0,9 s: **círculo de 0,8 m no chão onde o courier estava** no início do aviso (a picareta vai em arco, passa por cima de obstáculo) → impacto: courier no círculo leva 1; carga no chão no círculo racha 1 → `Recover` 2,6 s. Recua se o courier chega a 3 m (como o arqueiro). Contra: sair do círculo (0,9 s a 4,2 m/s = 3,8 m; o círculo tem 0,8 m), pousar a carga longe dele, matá-lo dormindo (golpe ×2).

**Toupeira** (`Mole`). `Under` (invisível e intocável; um rastro de terra mostra onde ela está) anda a 2,2 m/s até o alvo: **a carga do contrato no chão**, se houver e não estiver na água; senão o courier → `Telegraph` 0,8 s: monte de terra de 0,9 m cresce sob o alvo → `Emerge`: courier no monte leva 1; carga no monte é **empurrada 1,5 m** para longe do courier (cai: relíquia, bolo e espelho racham) → `Up` 2,0 s: fora e vulnerável, não ataca → mergulha (0,3 s) e fica pelo menos 2,0 s por baixo antes do próximo alvo. Na água não emerge: espera por baixo a 1,5 m da borda. Contra: carregar a carga (aí ela só tira 1 de vida e você sai do monte), pousar na água, acertá-la nos 2 s em que fica fora.

**Sapo do lodo** (`BogFrog`). Parado na poça. Com a carga no chão a ≤ 3 m e linha livre: `Telegraph` 0,7 s (linha lilás dele até a carga) → a língua **puxa a carga até 0,8 m na frente dele** em 0,3 s (arrastar não racha) → `Recover` 2,5 s. Sem carga no chão e o courier a ≤ 3 m: a língua acerta o courier (1). A carga ao lado dele fica no alcance dos golpes de quem estiver perto. Contra: pousar fora do anel de 3 m (a View mostra o anel quando a carga está nas mãos e o sapo está a ≤ 5 m), matá-lo de 3–5 m (vida 5).

**Besouro-bomba** (`BombBeetle`). `Chase` a 3,0 m/s até a borda do courier a ≤ 0,8 m → `Telegraph` (pavio) 1,0 s: para e pisca em vermelho → explode: raio 1,5 m, courier 2 (com a carga nas mãos vale a regra de golpe no courier), inimigos 4, carga no chão no raio racha 1, **ovo a ≤ 1,5 m racha** (regra de explosão), **barril no chão detona**; acorda dormindo a ≤ 4 m. Morto por tiro **explode na hora** onde estiver (mate-o a mais de 1,5 m). O dash o empurra 1,5 m sem desarmar. Contra: atirar de longe (vida 2), dash para longe, levá-lo até o Bruto ou os mineiros.

**Vagonete** (`Minecart`). Não é alvo da mira, não conta no teto de vivos, não morre. Ciclo por trilho (§1.1): `Idle` no túnel 2,0 s → `Telegraph` (sino) 1,2 s → passa a 6 m/s acertando tudo num raio de 0,6 m da linha → túnel do outro lado. Contra: ritmo; usar a passagem para matar inimigos e o Golem.

**Fantasma** (`Ghost`, região 3). Atravessa obstáculos e paredes. Sem carga no chão: persegue o courier (toque 1, recarga 1,5 s). Com a carga no chão: vai até ela → `Telegraph` 0,6 s (aura lilás) → **possui a carga**: ela flutua a 0,8 m/s até a parede lateral mais perto, espera 2,0 s e some com ela (`CargoStolen`). Pegar a carga de volta (chegar a `PickupRange`) quebra a possessão; acertar o fantasma (só se ele estiver na luz) derruba a carga (relíquia, bolo e espelho racham, como no ladrão). Contra: lanterna, pousar a carga na luz, Contragolpe/Gancho.

**Esqueleto** (`Skeleton`). Corpo a corpo comum (`Chase` → `Telegraph` 0,5 s → golpe de 1,0 m). Ao morrer vira **pilha de ossos**: se a carga do contrato (nas mãos ou no chão) estiver a ≤ 3,0 m da pilha, **morre de vez**; senão **se remonta em 4,0 s** com a vida cheia, no máximo 2 vezes (a 3ª morte é definitiva). Dash que passa a ≤ 0,6 m da pilha ou explosão nela **espalham os ossos** (morte definitiva). Contra: lutar perto da carga (o oposto do "limpe longe dela" da região 1), dash na pilha.

**Aranha** (`Spider`). Com o courier a 2–5 m: `Telegraph` 0,6 s (empina, linha de mira) → **cospe teia** (projétil 6 m/s, raio 0,25 m, para em obstáculo): acerta o courier = preso 1,0 s (o dash solta e larga a carga); acerta a carga no chão = pegar leva +1,0 s; se não acerta nada, vira teia de 0,6 m por 6 s onde parou; recarga 4,0 s. Perto: mordida (0,4 s, 1 de dano, 0,8 m). O espelho reflete a teia: a aranha fica presa 2 s. Contra: não ficar parado na linha, dash, espelho.

**Múmia** (`Mummy`). Lenta (1,0 m/s). Golpe de 0,9 m com aviso de 0,6 s que **agarra**: o courier não anda e leva 1 de dano a cada 1,2 s. Solta com **dash** (que larga a carga, como sempre) ou com **2 golpes** na múmia (o courier parado pousa a carga e ataca sozinho). Recarga 2,0 s depois de soltar. Contra: não deixar ela encostar com a carga nas mãos; atirar de longe (ela é lenta).

**Gárgula** (`Gargoyle`). `Stone` (pedra no pedestal, **invulnerável**, não é alvo da mira) até o courier passar a ≤ 3,0 m **com a carga nas mãos** → `Telegraph` 0,9 s: olhos vermelhos e faixa de 6,0 × 0,9 m no chão na direção do courier → **mergulho** a 9 m/s pela faixa: courier 2, carga no chão na faixa racha 1, inimigos na faixa 2 → `Perched` 2,5 s no fim da faixa, **vulnerável** → voa de volta ao pedestal em 1,5 s (intocável) → pedra; recarga 4,0 s. Contra: passar a mais de 3 m com a carga, ou levar a carga pelo outro lado e voltar sem ela; acordá-la de propósito, sair da faixa e acertá-la nos 2,5 s.

**Salamandra** (`Salamander`, região 4). Circula o courier a 2,5 m por 3 s, então corta na direção dele (toque 1, recarga 1,2 s) e volta a circular. **Deixa fogo no chão** (raio 0,45 m, 3 s) a cada 0,5 s em movimento. Imune a fogo e lava. Contra: matar cedo (vida 4, sem escala), não deixar ela fechar o círculo em volta da carga, água (fora da região 4) apaga o rastro.

**Diabrete** (`Imp`). Fica a 4–7 m do courier. Com a carga no chão **e linha livre até ela**: `Telegraph` 1,0 s (redemoinho lilás nele e na carga) → **troca de lugar com a carga** (a carga aparece onde ele estava) → recarga 5,0 s. **Um golpe nele durante o redemoinho cancela** e o atordoa 0,5 s. Sem carga no chão: some e aparece 1,0 m atrás do courier (aviso 0,5 s) e arranha (1); recarga 3,0 s. Contra: pousar atrás de bigorna (sem linha), atirar no redemoinho, Contragolpe.

**Kobold incendiário** (`KoboldBomber`). Fica a ≥ 3,5 m. `Telegraph` 0,9 s: círculo de 1,0 m onde o courier estava → frasco em arco (passa por cima de obstáculo) → impacto 1 de dano + **poça de fogo de 1,0 m por 4 s**; carga no chão sob o impacto racha 1 (gelo −20, barril detona). Recarga 3,0 s. Contra: mudar de lugar, não pousar onde o frasco cai, matar primeiro.

**Elemental de magma** (`MagmaElemental`). Corpo a corpo pesado (aviso 0,8 s, 1,2 m, 2 de dano); a pancada racha a carga no chão no alcance (como o Bruto) e tira 20 do gelo. **Ao morrer se divide em 2 brasas** (`MagmaShard`) a ±0,8 m, se o teto de 10 vivos deixar. Brasas: rápidas e frágeis, **morrem na hora se empurradas na lava**. Contra: lutar perto da lava e empurrar as brasas com o dash.

**Ovo de salamandra** (`SalamanderEgg`, só no B4). Parado; choca em 6,0 s numa salamandra; vida 2. Contra: estourar antes.

### 5.2 Chefes, com fases

**Rainha Toupeira** (M2, mini-chefe). Tranca as portas.
1. `Under` 3,0 s (intocável, rastro de terra largo) → mira a **carga no chão** (fora da água), senão o courier.
2. `Telegraph` 1,0 s: monte de 1,4 m → `Emerge`: courier no monte 2; carga no monte **empurrada 2,5 m + 1 rachadura** (qualquer carga).
3. `Up` 3,5 s: vulnerável; patada se o courier chega a 1,2 m (aviso 0,6 s, 1 de dano).
4. A cada 2ª emersão chama **2 toupeiras** (no máximo 3 vivas).
- **Fase 2 (≤ 50%):** `Under` 2,0 s; **emerge duas vezes seguidas** (o 2º monte 0,6 s depois, onde o alvo estiver); `Up` 2,5 s.
- Na água não emerge (sai na borda seca mais perto, sem empurrar o que está na água). **Contra:** carga nas mãos enquanto ela está por baixo, ou pousada numa das duas poças; atirar só no `Up`.

**Golem de Pedra** (B2, chefe). Tranca as portas. Gira devagar (90°/s).
- **Ponto fraco:** tiro vindo de trás (courier atrás do plano de frente dele) dá **×2**; de frente, **metade** (vida em meios pontos). Cristais ciano nas costas mostram o ponto fraco.
1. **Pancada** (gatilho 1,8 m, aviso 1,1 s, área de 2,2 m, 3 de dano; racha a carga no chão na área).
2. Depois de cada pancada caem **2 pedras** (sombra de 0,6 m por 0,8 s; obstáculo de 1,2 × 1,2 m por 12 s ou até a próxima pancada a quebrar; no máximo 4) em pontos livres a ≥ 1,5 m do courier e da carga, fora da faixa das portas, da entrada e do trilho; pedra que cai no courier tira 1; na carga, racha 1 e empurra.
- **Fase 2 (≤ 50%):** **investida**: aviso 1,0 s (faixa de 6 m), corre a 7 m/s; bate em parede, pilar ou pedra → **atordoado 2,0 s** (×2 de qualquer lado); acerta o courier: 3 + empurrão de 2 m. A partir de 30 s de luta chama 1 goblin mineiro a cada 12 s (no máximo 2).
- **Vagonete:** a passagem acerta o Golem por **6 (cheio, sem metade)** e o atordoa 1,5 s. **Contra:** circular (ele gira devagar), atraí-lo para o trilho, fazê-lo investir contra pilar.

**Viúva Aranha** (M3, mini-chefe). Tranca as portas. Sala com 4 tochas.
1. **Leque de teia** a cada 6 s: 3 cuspes a −20°, 0°, +20° (aviso 0,8 s).
2. **Fio:** com a carga no chão a ≤ 7 m e linha livre: aviso 1,0 s (fio lilás brilhando dela até a carga) → **puxa a carga a 2,5 m/s** até ela. Corta o fio: pegar a carga, **dash que cruza o fio** ou 2 golpes nela durante o puxão. Carga que chega nela vira **casulo** (pegar leva 1,5 s ao lado dela).
3. Mordida (1,1 m, aviso 0,6 s, 2 de dano).
- **Fase 2 (≤ 50%):** sobe ao teto (3,0 s intocável) e **despenca** sobre a carga no chão (ou o courier, se carregando): círculo de 1,3 m por 1,2 s, 2 de dano e 1 rachadura. Chama 2 aranhas aos 50% e aos 25%.
- **Contra:** carregar a carga e pousar só para atirar; dash no fio; lanterna/tochas para mirar.

**Necromante** (B3, chefe). Tranca as portas. Não anda: salta entre os 6 pontos (`Blinks`).
1. **Raio:** aviso 1,2 s (faixa verde de 9 × 0,6 m do Necromante até onde o courier estava), dispara 0,3 s: courier 2, carga no chão na faixa racha 1. Recarga 3,0 s. **O espelho reflete o raio:** volta e tira 6 dele + atordoa 1,0 s (o espelho racha 1).
2. **Erguer:** a cada 15 s ergue 3 esqueletos nas lápides mais longe do courier (runa verde 1,0 s antes); no máximo 4 vivos. Os erguidos seguem a regra do esqueleto (carga a 3 m para ficarem no chão).
3. **Salto:** com o courier a ≤ 3,0 m: fumaça 0,5 s e salta para o ponto mais longe do courier (recarga 3,0 s).
- **Fase 2 (≤ 50%):** raio duplo (duas faixas a ±15°); ergue 4.
- **Fase 3 (≤ 20%):** salta a cada 4 s mesmo sem o courier perto; a luz do courier perde 1,0 m e as tochas 0,5 m.
- Morto: todos os esqueletos caem de vez. **Contra:** cobertura das lápides contra o raio, lutar perto da carga, espelho ou lanterna (contrato M3-16 é de lanterna).

**Capataz Kobold** (M4, mini-chefe). Tranca as portas.
1. **Chicote:** aviso 0,7 s, faixa de 3,0 × 0,6 m: courier 2; carga no chão na faixa é **puxada 1,5 m para ele** + 1 rachadura. Recarga 1,8 s.
2. **Frasco** a cada 5 s (como o kobold, poça de 1,2 m).
3. **Apito** a cada 14 s: chama 2 kobolds (no máximo 2 vivos chamados por ele).
- **Fase 2 (≤ 50%):** alterna chicote em linha com **giro** (círculo de 3,0 m, aviso 1,0 s); frasco a cada 3,5 s.
- **Contra:** ficar a mais de 3 m, atirar entre os avisos, empurrar os kobolds nos respiros.

**Dragão Ancião** (B4, chefe final). Tranca as portas. Anda devagar na metade de cima (y ≥ 9).
1. **Sopro:** aviso 1,4 s (cone de 60° e 6 m para onde o courier estava; o chão brilha), sopro 0,8 s: courier 3; carga no chão no cone racha 1, gelo −40, balão estoura; deixa brasa 2 s. **Pilares bloqueiam o cone** (sombra atrás do pilar é segura). Recarga 3,0 s.
2. **Ovos:** põe 2 ovos de salamandra em pontos livres a ≥ 3 m do courier (choca em 6 s; no máximo 2 ovos).
3. **Voo e pouso:** decola (0,6 s), fica 2,5 s no ar (intocável; a sombra segue o courier a 2,0 m/s), círculo de 2,2 m por 1,2 s onde a sombra parou → pouso: 3 de dano; carga no chão no círculo empurrada 2,0 m + 1 rachadura → **cabeça baixa 3,0 s: dano ×2** (a janela de dano).
- **Fase 1 (100–60%):** sopro + ovos a cada 20 s.
- **Fase 2 (60–30%):** + voo a cada 18 s; os 3 respiros da sala (fissuras) começam a funcionar.
- **Fase 3 (≤ 30%):** sopro em varredura (o cone gira 90° em 1,5 s); ovos a cada 12 s; voo a cada 14 s.
- **Contra:** pilar contra o sopro, estourar ovos, punir o pouso.

---

## 6. Cargas novas (8)

As 8 já estão no `CargoType` do núcleo (`ArcaneHeart, IceCrystal, GriffinCage, Lantern, Mirror, Balloon, Flytrap, TreasureMap`) como provisórias; entre parênteses, o `PayMul` provisório que este desenho substitui (lanterna e mapa ajudam o jogador, por isso pagam menos). `Outcome` ganha no fim `Melted` (gelo) e `Escaped` (balão); espelho quebrado e balão estourado usam `CargoBroken`. `CargoDef.Fragile` do núcleo (relíquia, ovo, bolo, cristal, espelho) fica como está: o balão não entra na Fragile Week.

| Enum | `CargoDef` (nome, `PayMul`) | Verbo | Falha | Estreia |
|---|---|---|---|---|
| `ArcaneHeart` | "Coração arcano", **1,35** (1,4) | ímã de moedas e de inimigos | — (horda maior) | M2-5 |
| `IceCrystal` | "Cristal de gelo", **1,45** (1,5) | derrete; a água recongela, o fogo acelera | derreteu | M2-3 |
| `GriffinCage` | "Gaiola do grifinho", **1,30** (1,4) | grita e chama inimigos; o dash acalma | — | M2-10 |
| `Lantern` | "Lanterna", **1,20** (1,3) | ilumina o escuro; apaga se cair | — (escuro) | M3-1 |
| `Mirror` | "Espelho encantado", **1,45** (1,5) | reflete o que vem de frente; cada reflexo racha | quebrou | M3-3 |
| `Balloon` | "Balão de gás", **1,35** (1,3) | leve (+15%); no chão escapa em 4 s; ladrão não alcança | escapou / estourou | M3-5 |
| `Flytrap` | "Planta carnívora", **1,30** (1,4) | no chão vira torre que morde (inclusive você) | — | M4-2 |
| `TreasureMap` | "Mapa do tesouro", **1,20** (1,3) | revela baús enterrados e paredes disfarçadas; ladrões o preferem | — | M4-3 |

**Integridade e 2ª estrela:** as que não dizem outra coisa racham como a caixa (pancada, flecha, picareta, frasco e pedra na carga no chão; queda do ladrão não racha), e a 2ª estrela é 0 rachadura.

**Coração arcano.**
- Anel visível de **4,0 m** em volta do coração (nas mãos ou no chão).
- **Ímã de inimigos:** perseguidores comuns dentro do anel (morcego, limo, besouro, esqueleto, múmia, salamandra, brasa) passam a ter o coração como alvo; com ele no chão, vão até ele e ficam em volta (não o golpeiam). Dormindo dentro do anel acorda. Ladrões já vão nele; atiradores continuam mirando o courier.
- **Horda maior:** cada reforço da sala vem com **+1 inimigo** (repete o 1º do `Wave`).
- **Ímã de moedas:** cada inimigo derrotado a ≤ 4,0 m do coração rende **+3 moedas** (× Bolsa), com **teto de 30 por visita de sala** (fecha o farm no B1, onde o Ogro chama ladrões a cada 10 s).
- O verbo: pousar o coração como isca, juntar a horda e limpar em área (bombinha), sabendo que o reforço vem maior.

**Cristal de gelo.**
- Barra de gelo de 100%. Derrete **100 / `MeltTime` por segundo** (`MeltTime` = coluna "Ovo/Gelo" do contrato; perk Isolamento: 40% mais devagar).
- **×3** a ≤ 1,5 m de lava, fogo no chão ou respiro em erupção; fogo direto em cima: respiro −25, frasco −20, sopro −40, pancada de magma −20; outros golpes na carga no chão: −10.
- **Água: +10%/s** até 100 (no chão dentro da poça, ou nas mãos do courier que está na poça).
- 0% = **derreteu** (`Outcome.Melted`). Integridade pelo gelo na entrega: **≥ 70% = 0 rachadura** (×1,5 e 2ª estrela); ≥ 40% = 1 (×1,2); < 40% = 2 (×1,0).
- O verbo: escolher a rota pela água; na região 4, fugir do fogo.

**Gaiola do grifinho.**
- O grifinho **grita a cada 6,0 s** (contados desde a entrada na sala ou do último grito/calma). Aviso: no último 1,0 s a gaiola treme e um anel amarelo cresce até 5 m.
- **Grito (onda de 5 m):** acorda dormindo; inimigos comuns no raio (não Guarda nem chefe) vão até a gaiola por 3 s; **o 2º grito na mesma sala dispara o reforço** se ele ainda não veio.
- **O dash acalma:** zera o contador. Como todo dash, larga a gaiola: acalmar custa largar a carga perto de ladrões. Com o perk Alça firme, o 1º dash da sala acalma sem largar.
- Pegar de volta não zera o contador.

**Lanterna.**
- Nas mãos: luz de **5,5 m** (substitui a de 3,5 m do courier). No chão: **4,0 m** em volta dela (o courier mantém os 3,5 m dele).
- **Apaga** quando cai (dash que a larga, ladrão atingido, empurrão de toupeira/pouso), quando leva golpe no chão (pancada, flecha, picareta, frasco, fogo) ou quando o courier leva golpe de 2+ com ela nas mãos.
- **Reacende** carregada a ≤ 1,0 m de uma tocha por 0,5 s.
- Fora de sala escura é só uma carga leve (não apaga). No evento Midnight Delivery a carga é a lanterna (o núcleo já faz assim).

**Espelho encantado.**
- Carregado, flecha, teia cuspida e raio que chegam **de frente** (ângulo ≤ 60° com o `Facing`) são **refletidos**: voltam pela mesma linha como projétil do courier. Flecha refletida: 2 de dano no primeiro inimigo. Teia refletida: prende a aranha 2 s. Raio do Necromante refletido: 6 de dano e atordoa 1 s. Frasco e picareta (arco) não refletem.
- **Cada reflexo = 1 rachadura.** Por trás ou de lado, o golpe acerta o courier normalmente e **não** racha o espelho.
- Também racha ao cair (dash, ladrão atingido, empurrão) e com golpe no chão. **3 = quebra** (`CargoBroken`).
- O verbo: virar de frente para o arqueiro e escolher quantos reflexos pagar.

**Balão de gás.**
- Carregando: **+15% de velocidade** (soma com Passo leve).
- **No chão sobe e escapa em 4,0 s** (contador visível; pegar zera) → `Outcome.Escaped`.
- **Ladrão e Capitão não alcançam** o balão (ignoram). Fantasma, sapo, diabrete, Viúva, Capataz e o Gancho alcançam.
- Furo: flecha, picareta, frasco, pancada ou fogo no balão no chão = 1 rachadura; **3 = estoura** (`CargoBroken`).
- O verbo: o oposto do barril — pousar é seguro contra ladrão, mas o tempo corre.

**Planta carnívora.**
- **No chão morde inimigos** a ≤ 1,2 m: aviso 0,3 s, **2 de dano**, recarga 1,0 s (o mais perto; morde chefes também). Ladrão que a pega leva 1 de dano por segundo carregando-a (e solta ao ser atingido, como sempre).
- **Morde o courier** parado e sem carga a ≤ 1,2 m dela por mais de 0,8 s: 1 de dano, recarga 1,5 s. Como o pouso a põe a 0,6 m, o verbo é **pousar e dar um passo** antes de lutar.
- Nas mãos não morde ninguém.

**Mapa do tesouro.**
- Com o mapa no contrato (nas mãos ou no chão), **baús enterrados e paredes rachadas, inclusive as disfarçadas**, aparecem na sala e no ícone da porta da camada seguinte (pá e rachadura).
- **Ladrões o preferem:** +30% de velocidade correndo para ele no chão, e o ladrão da carga sozinha nasce em 6 s (em vez de 8).
- Baús enterrados (moedas): R2-4 30 · R2-9 35 · R3-5 35 · R3-9 40 · R4-2 40 · R4-4 45 · R4-6 45 · R4-8 50 · R4-9 60 · R4-10 55 · S4-1 50.

---

## 7. Perks novos (15)

`Perk` ganha no fim: `Spring, SecondWind, ChestSense, SureFoot, Insulated, FirmGrip, Counter, Hunter, ThickSkin, CatEyes, Whistle, FirstAid, Runner, CargoShield, Scout`. O sorteio continua pela semente; **perk que não se aplica não entra na oferta** (Isolamento só com gelo ou ovo; Olhos de gato só em sala escura, Midnight ou Neblina).

| Enum | Nome | Texto (UI) | Número |
|---|---|---|---|
| `Spring` | Mola | dash +40% de distância | `DashDist` 3,0 → 4,2 m no mesmo 0,18 s |
| `SecondWind` | Segundo fôlego | 1× por contrato volta com 2 de vida | ao chegar a 0 de vida; 1,5 s invulnerável; desligado no No-Damage |
| `ChestSense` | Faro de baú | mostra baús enterrados e paredes rachadas | revela como o Mapa; enterrado abre em 1,0 s |
| `SureFoot` | Pé firme | água, lama e teia não freiam | ignora água, poça de limo e teia (não prende) |
| `Insulated` | Isolamento | gelo derrete 40% mais devagar; ovo choca 20% mais tarde | `MeltTime` ×1/0,6; `EggTarget` ×1,2 |
| `FirmGrip` | Alça firme | o 1º dash de cada sala leva a carga junto | não larga; vale para o grifinho |
| `Counter` | Contragolpe | dash em quem mexe na carga cancela e atordoa | ladrão, Capitão, sapo, fantasma, diabrete (redemoinho), Viúva (fio), toupeira (aviso): cancela, atordoa 1,0 s; carga a ≤ 1,5 m volta às mãos |
| `Hunter` | Caçador | +2 moedas por inimigo derrotado | × Bolsa; teto de 20 por visita de sala |
| `ThickSkin` | Couro grosso | −1 no dano de golpes de 2+ | 2 → 1, 3 → 2 |
| `CatEyes` | Olhos de gato | +60% de luz no escuro | 3,5 → 5,6 m (lanterna 5,5 → 8,8 m) |
| `Whistle` | Assobio | ladrão que pega a carga fica parado 1,5 s | vale para o Capitão |
| `FirstAid` | Primeiros socorros | +1 de vida ao abrir baú | até a vida máxima; enterrado conta |
| `Runner` | Corredor | +20% de velocidade sem a carga | só no estado `Walking` |
| `CargoShield` | Escudo da carga | o 1º golpe em cada sala na carga pousada é ignorado | por sala, só no chão (a Carga acolchoada é 1 por contrato, nas mãos ou no chão) |
| `Scout` | Batedor | as portas mostram o número exato de inimigos | spawns + reforço ("3 + 2") |

O bot escolhe perk pela carga como hoje; acrescentar: gelo → Isolamento, espelho/lanterna → Escudo da carga, gaiola → Alça firme, região 3 → Olhos de gato.

---

## 8. Equipamento lateral e projetos (contrato §6.6)

1 espaço além da arma, trocado fora do contrato. **Sem botão novo**: o controle continua de um dedo, então o equipamento dispara sozinho pela regra abaixo. Destrava com **3 projetos**; depois sobe de nível com moedas. **Proposta que muda o provisório do núcleo** (3 níveis a 350/800, +0,5 por nível, abre depois do C6): **5 níveis** e preços maiores, porque é o único ralo que cresce com as regiões 2–4 (§10.3). O "abre depois do C6" fica: até a região 2 os projetos vêm do quadro de temporada, do baú do dia e do Starter Pack.

| Equipamento | Dispara quando | N1 | N2 | N3 | N4 | N5 |
|---|---|---|---|---|---|---|
| **Bomba de fumaça** | um inimigo pega, possui, troca, puxa ou enrola a carga (ladrão, Capitão, fantasma, diabrete, sapo, Viúva, Capataz) | nuvem de 2,5 m; quem mexe na carga a solta e perde a carga de vista **3 s**; 1 por contrato | 3,5 s; 1 | 3,5 s; 2 | 4 s; 2 | 4 s; 3 |
| **Gancho** | o courier anda sem carga na direção da carga do contrato no chão (±30°) a 1,5–5 m por 0,25 s | puxa a carga para as mãos em 0,3 s (não racha); recarga **10 s** | 9 s | 8 s | 7 s | 6 s e puxa também da mão do ladrão e da possessão |
| **Armadilha de urso** | ao pousar a carga pela 1ª vez em cada sala (fica a 0,6 m do outro lado da carga) | o 1º inimigo não-chefe a ≤ 0,5 m fica preso **3 s** (voadores não caem) | 4 s | 2 armadilhas, 4 s | 2, 5 s | 2, 5 s e prende mini-chefe 1,5 s |
| **Botas de lama** | passivo | água e poça de limo não freiam | + teia não freia sem carga | + teia não prende com carga | + agarrão da múmia e teia cuspida duram metade | + fogo no chão tira 1 a cada 2 s |

- **Custo** do nível 2 ao 5: **600 · 1.200 · 2.000 · 3.000** (6.800 por equipamento, 27.200 os quatro): é o ralo principal das regiões 2–4 (§10.3).
- **Projetos (12)**, cada um no baú de uma sala, entregue na 1ª abertura (campanha, dia ou código); o ícone da porta mostra o projeto enquanto ele não foi pego, então o conteúdo é sempre visível:
  - Botas de lama: R2-1 · R2-5 · S2-1 (região 2)
  - Bomba de fumaça: R2-7 · R3-4 · S3-1
  - Gancho: R3-2 · R3-9 · R4-4
  - Armadilha de urso: R4-3 · R4-6 · S4-1
- Caminhos alternativos, também fixos (já no núcleo): quadro de temporada (1 projeto de cada equipamento entre os 20 objetivos), baú do dia e Starter Pack (1 de cada, conteúdo listado).

---

## 9. Eventos (5) e modificadores (5)

### 9.1 Contratos dos eventos (contrato §6.1)

Um evento por semana, em rodízio pela **semana ISO mod 5** (voltar o relógio não repete recompensa); abre com a região 2 (Ogro vencido). Cada evento tem **3 contratos** (+2 com o pacote de evento), gerados por `Content.EventContract` só com conteúdo destravado. Proposta de forma para as 3 vagas (hoje o núcleo sorteia a região pelo payload):
- **E1:** região 1, 4 camadas;
- **E2:** a região mais alta destravada, 5 camadas;
- **E3:** a região mais alta destravada, 6 camadas.

Recompensa fixa por vaga, 1 vez por semana: os valores provisórios do núcleo (`Balance.EventRewards`) ficam. Proposta a mais: **completar as 3 vagas na mesma semana dá o projeto da semana** (equipamento = semana mod 4; se já tiver os 12, 300 moedas) e o selo do evento no quadro. Rejogar na mesma semana paga 50% e não repete a recompensa. Sem resgate de carga em contrato de evento.

| Semana mod 5 | Evento | Regra | Pagamento |
|---|---|---|---|
| 0 | **Fragile Week** | só cargas `Fragile` destravadas (relíquia, ovo, bolo, cristal, espelho) | ×1,5 |
| 1 | **Midnight Delivery** | todas as salas escuras com as regras da região 3 (sala sem tocha ganha tochas em (0,3; 5,0) e (8,7; 10,0)); a carga é a **lanterna** | ×1,0 (+ o que a lanterna paga) |
| 2 | **No-Damage Contract** | vida máxima 1; sem cura por sala; Segundo fôlego, Coração extra e Primeiros socorros fora do sorteio | ×3 |
| 3 | **Treasure Detour** | moedas de baú ×2; paredes rachadas **abertas** (passagem livre, disfarçadas inclusive); baús enterrados visíveis; Pão-duro proibido | ×1,0 (o ganho são os baús) |
| 4 | **Boss Courier Run** | caixa; as salas de chefe já vencidas em fila (M1, B1, M2, B2…), como o núcleo monta; proposta: cura de 2 por sala nesse evento (sem salas normais entre os chefes, a cura de 1 não segura 4 ou mais chefes) | gerado do núcleo |

### 9.2 Modificadores (contrato §6.2)

Destravam com **40 estrelas**. O jogador escolhe 0–2 por contrato (qualquer contrato já liberado, dia e código inclusive); o bônus soma e vai no pagamento antes de integridade e Bolsa. O contrato do dia sai com 1 modificador pela semente. Pagamentos = os provisórios do núcleo (`ModifierDef`); a regra de cada um, com números:

| Modificador | Regra | Pagamento |
|---|---|---|
| **Neblina** | luz de **3,5 m** (`FogRadius`) em qualquer sala e mira só no que está na luz (§1.1); na região 3, a luz perde 1,0 m | +15% |
| **Pressa** | `ParTime` × 0,7 (`RushPar`): o bônus e a 3ª estrela só valem dentro dele | +20% |
| **Horda** | +1 perseguidor da região em cada sala, nascendo em (4,5; 12,0) ou no ponto livre mais perto (região 1 morcego, 2 besouro, 3 esqueleto, 4 salamandra); cada reforço vem com +1 | +25% |
| **Frágil** | **toda carga quebra com 2 rachaduras** (falha), a caixa inclusive; gelo falha abaixo de 40% | +25% |
| **Pão-duro** | moedas de baú × 0,5 (`StingyChestPct`; enterrado também) | +20% |

---

## 10. Destrave, duração e economia

### 10.1 Curva de destrave

| Quando | O que abre |
|---|---|
| início | região 1, Estilingue |
| C3 | contrato do dia, por código e contrato raro |
| 12 / 24 estrelas | Bumerangue / Bombinha (como hoje) |
| C6 | equipamento lateral (`GearOpenAfter`; os projetos dos baús começam na região 2) |
| **B1 + 30 estrelas** | **região 2**, eventos |
| **40 estrelas** | modificadores |
| **B2 + 60 estrelas** | **região 3** |
| **B3 + 95 estrelas** | **região 4** |
| **B4** | desafio sem fim |
| 10 / 30 / 60 / 95 / 150 estrelas (marcos) | moedas e a roupa Fantasma no de 95 (valores do núcleo, `Balance.Milestones`) |

- **Estrelas possíveis:** 72 (região 1) + 48 × 3 = **216**. O bot fecha a região 1 com 62,6; com ~2,2 por contrato nas novas, chega a ~97 no fim da região 2 e ~132 no fim da 3: as barreiras de estrela não seguram o bot. Um humano com ~1,7 por contrato chega a ~41 / ~68 / ~95: a barreira da região 4 pede rejogar alguns contratos por estrela. **HIPÓTESE** (medir no playtest pelo `contract_stars`).
- **Maestria por carga:** níveis 1–5 com **3 / 8 / 15 / 25 / 40 entregas** daquela carga; +2% de pagamento por nível naquela carga e selo no quadro.
- **Desafio sem fim** (o núcleo já monta: 2 salas por camada das regiões destravadas, Tier 10 + 2 por camada, 15 moedas por camada). Proposta de conteúdo: **a cada 5 camadas um mini-chefe** (M1→M4 em ciclo) e **a cada 10 um chefe** (B1→B4), que dão o ritmo de "andar de chefe"; **teto de 800 moedas por dia** no sem fim, para ele não virar a fonte principal de moedas.

### 10.2 Duração estimada (HIPÓTESE do desenho)

Estimativa da mediana do bot por visita, calibrada na região 1 (C13–C24: 87–181 s em 7 salas): sala de ameaça 1/2/3 = 13/16/20 s na região 2 e 15/18/22 s nas regiões 3 e 4 (escuro e fogo atrasam); +4 s com Guarda ou emboscada; mini 35/38/40 s; chefe 100/110/130 s; mensagem a 42% do tempo da sala (a região 1 mede ~4,5 s por sala); entrega 5 s. Por camada usa a média das salas dela.

| Região | Tutoriais (1–3) | Do 4º em diante, sem mensagem | Mensagem | 16 contratos (1 tentativa) |
|---|---|---|---|---|
| 2 | 32–52 s | **88–196 s** (média 112) | 48 s | 25,5 min |
| 3 | 38–60 s | **102–202 s** (média 127) | 54 s | 28,8 min |
| 4 | 38–58 s | **96–222 s** (média 127) | 53 s | 28,7 min |

- Dentro do alvo de 1,5–3 min do bot a partir do 4º contrato, menos: **chefes** (M2-16 196 s, M3-16 202 s, M4-16 222 s; o C24 medido fica em 181 s; o B3 e o B4 já perderam 1 camada por isso) e **mensagem** (~50 s, curta por desenho, como na região 1).
- Com ~1,3 tentativas por contrato (região 1: 31,8 tentativas em 24), as regiões 2–4 somam ~1 h 50 de bot; humano a 2–3× = **3,5–5,5 h**, mais 1,2–1,8 h da região 1.

### 10.3 Economia (1 moeda)

- **Pagamento somado:** região 2 = 3.768, região 3 = 4.672, região 4 = 5.567 (região 1 = 2.970). Na região 1 o bot ganhou 3,4 moedas por moeda de pagamento (baús, carga, integridade e Bolsa); com a Bolsa quase no máximo a razão deve passar de 4: **~56 mil moedas** nas regiões 2–4.
- **Ralos:** guilda que falta (~4.200), níveis de arma que faltam (~2.100), equipamento lateral (27.200), rerolls (pouco). Somam **~34 mil**.
- **Sobra estimada ~20 mil** no fim da região 4. **HIPÓTESE e risco:** o bot deve medir "moedas paradas no fim de cada região" com alvo de, no máximo, 1 nível de equipamento sobrando. Se sobrar, as alavancas em ordem são: (1) baixar o pagamento das regiões 3–4 em 10%; (2) baú −20%; (3) um ralo novo (decisão do coordenador; não proponho aqui para não inventar sistema).

---

## 11. O que o bot precisa jogar (para medir)

O bot da região 1 joga o verbo de cada carga (relíquia sem dash, bolo sem dash, barril pousado ≤ 1,6 s, mensagem sem luta). Para as regiões 2–4, no mesmo espírito e com o mesmo handicap (reação 0,25 s ± 0,08):
- **Terreno:** sai do círculo/faixa/cone de aviso; espera o vagonete passar antes de cruzar o trilho; passa a mais de 2 m de quem dorme (com chance de atacar dormindo pela agressividade); nunca pisa em fogo de propósito; usa a ordem dos respiros.
- **Escuro:** só "vê" inimigo dentro de alguma luz (mais o telegraph e o projétil); prefere caminho com tocha.
- **Cargas novas:** gelo pela água e longe do fogo; coração pousado como isca quando a sala tem 3+ perseguidores; gaiola com dash de calma quando não há ladrão vivo; lanterna nunca largada no dash (sem Alça firme), reacende na tocha; espelho de frente para o arqueiro só com até 1 rachadura; balão pousado no máximo 2,5 s; planta pousada e 1 passo; mapa vai aos baús enterrados pela ganância.
- **Inimigos:** pousa a carga na água contra a toupeira; fora do anel do sapo; atira no besouro a > 1,5 m; luta a ≤ 3 m da carga contra esqueleto; dash na múmia quando a vida está baixa; atira no diabrete no redemoinho; dash empurrando para a lava quando dá.
- **Novas medidas no `BALANCE.md`:** tabela por contrato dos 48 contratos novos; dano por visita das 30 salas novas (para o ícone); taxa de cada falha nova (derreteu, escapou, espelho quebrado, fantasma levou); H1 por região; H2 com as 15 cargas; moedas paradas no fim de cada região.

---

## 12. Arte necessária

Pipeline e regras do `ASSETS.md`: Tripo (45 por modelo, 20 por rig) → Mixamo ou Blender → `render_sprites.py` a 60°; sufixos de prompt `[S][T][I]` (personagem) e `[P][IP]` (prop). **Regra do Vinicius:** arma, escudo, cajado, frasco, chicote e qualquer item de mão saem **separados** ("empty hands, no weapon") e são presos ao osso no Blender (`--attach`); roupa, capacete, coroa e cristais no corpo contam como corpo. Clipes do Mixamo quando houver; o resto por keyframe no Blender sobre o rig Mixamo, com o nome que o preset espera. Criatura não humanoide fica prop animado por código (squash, flutuar), como o morcego e o limo. Cores pelo §2–§4 (papel novo: **ciano-petróleo** = ataca de longe, como o arqueiro; **lilás `#B58CFF`** como acento de quem mexe na carga, família do roxo do roubo; **laranja-fogo** = queima; **verde `#7CFF9E`** = morto-vivo).

Estilo comum no prompt: "stylized fantasy comic, chunky shapes, flat two-tone colors, high readability from a 60° top-down camera". Quadros: 12 fps, teto 12 por clipe; o impacto vai no quadro de `ImpactFrac` da View (medir na folha).

### 12.1 Lote 4 — região 2 (≈ 665 créditos; teto pedido 750)

| Peça (preset) | Tipo | Assunto do prompt (inglês) | Altura | Separado | Clipes |
|---|---|---|---|---|---|
| `mineiro` | personagem rigado (65) | goblin miner, olive green skin, long pointy ears, dented copper mining helmet with a small lamp, dark teal work vest, rolled trousers, leather tool belt with pouches, big boots, empty hands, no weapon, no pickaxe | 1,00 m | **picareta** → `RightHand` | `Idle` (Breathing Idle, janela 199–289), `Walk` (Walking Backwards, recua), `Throw` (Blender, arremesso por cima; soltura ~0,6), `Sleep` (Mixamo "Sleeping Idle" ou Blender sentado encostado, laço), `Wake` (Blender, 0,4 s), `Hit` (Blender) |
| `picareta` | prop (45) | small mining pickaxe, dark iron head, short wooden handle wrapped with teal cloth | 0,45 m | — | `Static` (o projétil é o mesmo sprite girando por código) |
| `toupeira` | personagem rigado (65) | chubby cartoon mole standing on two legs, brown velvet fur, big pink nose, tiny squinting eyes, huge digging claws, small lilac miner's scarf | 0,90 m | — | `Idle`, `Look` (Looking Around), `Emerge` (Blender: sobe do chão em 0,3 s; a View recorta pelo chão), `Burrow` (o inverso), `Hit`; monte de terra e rastro procedurais |
| `sapo` | prop (45) | fat swamp toad, mud-green warty skin, wide mouth, big golden eyes, lilac spots on the back, sitting pose | 0,90 m de largura | — | `Static` + respiração por squash; língua procedural (linha lilás); opcional 2ª pose de boca aberta no Blender (0 crédito) |
| `besouro` | prop (45) | round bomb beetle, glossy dark plum shell with orange warning stripes, short legs, fuse-like antenna, angry eyes | 0,60 m | — | `Static` + balanço 6 Hz e pulso vermelho do pavio por código |
| `vagonete` | prop (45) | old wooden mine cart on iron wheels, iron bands, dents, small brass bell on the front, empty | 1,20 m | — | `Static` + flipX; sacolejo por código |
| `rainha` / `rainha_fim` | personagem rigado (65) | giant queen mole, plump body, dark brown fur, short stiff lilac royal cape, crown of raw amethyst crystals, huge digging claws, big pink nose | 1,60 m | — (coroa é corpo) | `Idle`, `Emerge`, `Burrow`, `Swipe` (Blender; impacto 0,5), `Call` (Blender, como o Roar do Ogro), `Hit` / `Dying` |
| `golem` / `golem_fim` | personagem rigado (65) | massive stone golem, grey-blue boulder body with moss patches, tiny head sunk between huge shoulders, enormous rock fists, cluster of glowing cyan crystals on its back | 2,60 m | — (cristais = corpo, ponto fraco) | `Idle`, `Walk` (Walking Forward a ~0,6×), `Slam` (Blender, reuso do do Ogro; impacto 0,6), `Charge` (Blender, inclinado correndo), `Stun` (Blender, sentado tonto, laço) / `Dying` (Blender, desmorona) |
| `coracao` | prop/carga (45) | glowing arcane heart crystal, deep magenta faceted heart-shaped gem, held in a small golden cage frame with two handles | 0,45 m | — | `Static`; pulso e anel de 4 m por código |
| `cristal` | prop/carga (45) | big block of magical ice, pale cyan translucent, frosty edges, small rune glowing inside, on a wooden carrying tray with handles | 0,50 m | — | `Static`; derreter = escala Y e gotas por código |
| `gaiola` | prop/carga (45) | small brass birdcage with a baby griffin inside, fluffy cream feathers, tiny eagle head, lion-cub body, big eyes, handle on top | 0,50 m | — | `Static`; tremor do aviso e onda do grito por código |
| `lanterna_mina` | prop/decoração (45) | miner's oil lantern hanging from an iron wall hook, warm glass, dented metal | 0,50 m | — | `Static` (+ poça de luz âmbar) |
| `escora` | prop/decoração (45) | wooden mine support frame, two thick vertical beams and a cross beam, iron brackets | 1,20 m de largura | — | `Static` (veste os obstáculos de pilar) |
| trilho, poça, rastro de terra, pedras do Golem | procedural | — | — | — | 0 crédito |
| texturas piso/parede da mina | gerador grátis | seamless tileable stylized mine floor of packed earth and stones / rough rock wall with wooden planks, top-down, hand-painted, flat light | — | — | 0 crédito |

### 12.2 Lote 5 — região 3 (≈ 755 créditos; teto pedido 850)

| Peça (preset) | Tipo | Assunto do prompt (inglês) | Altura | Separado | Clipes |
|---|---|---|---|---|---|
| `esqueleto` | personagem rigado (65) | undead skeleton warrior, bone-white bones, dented dark iron helmet, tattered dark green rags, glowing green eye sockets, empty hands, no weapon, no shield | 1,35 m | **clava** → `RightHand` | `Idle`, `Walk` (buscar "zombie walk"; fallback Walking), `Attack` (Blender; impacto 0,5), `Collapse` (Blender: desmonta em pilha em 0,3 s), `Rise` (o Collapse invertido; a View estica para 4 s), `Hit` |
| `clava` | prop (45) | crude bone club wrapped with rope | 0,60 m | — | `Static` |
| `mumia` | personagem rigado (65) | shambling mummy wrapped in dirty beige bandages, loose bandage strips, glowing green eyes through the wraps, hunched, empty hands | 1,50 m | — | `Idle`, `Walk` (zombie walk a 0,7×), `Grab` (Blender; impacto 0,6), `Hold` (Blender, laço apertando), `Hit` |
| `gargula` | personagem rigado (65) | stone gargoyle demon, grey-green weathered stone skin with cracks, folded bat wings on the back, small horns, claws, glowing red eyes, empty hands | 1,40 m | — (asas = corpo) | `Statue` (1 quadro agachado, Blender), `Wake` (Blender), `Dive` (Blender, corpo na horizontal, 6 quadros), `Perch` (Blender, agachado ofegando), `Hit` |
| `necromante` / `necromante_fim` | personagem rigado (65) | gaunt necromancer, black hooded robe ending above the knees over dark trousers, sickly green glowing trims and runes, bony pale hands, skull belt buckle, glowing green eyes under the hood, empty hands, no staff | 1,60 m | **cajado** → `RightHand` | `Idle`, `Cast` (Blender; aponta o cajado, impacto 0,8), `Summon` (Blender; ergue o cajado com as duas mãos), `Hit` / `Dying`; salto = fumaça e alfa por código |
| `cajado` | prop (45) | necromancer staff, twisted black wood topped with a horned skull and a glowing green gem | 1,40 m | — | `Static` |
| `fantasma` | prop (45) | cute spooky ghost, floating white-cyan sheet body with a wavy tail, no legs, big dark eye holes, small grin, faint lilac glow | 0,90 m | — | `Static`; flutuar ±0,08 m a 1,5 Hz e alfa 0,75 por código |
| `aranha` | prop (45) | big cartoon dungeon spider, round dark violet-black body, eight thick hairy legs, many shiny eyes | 0,80 m de largura | — | `Static` + squash; 2ª pose "empinada" no Blender para o aviso da teia (0 crédito) |
| `viuva` | prop (45) | giant widow spider queen, glossy black body with a glowing lilac hourglass mark, long thin legs, crown-like spikes on the head, many red eyes | 2,00 m de largura | — | `Static` + pose empinada (Blender); fio e casulo procedurais |
| `lanterna` | prop/carga (45) | old brass hand lantern with a glass globe and a warm flame, carrying ring on top | 0,45 m | — | `Static` + versão apagada por troca de material (0 crédito) |
| `espelho` | prop/carga (45) | enchanted hand mirror, oval silver mirror in an ornate gold frame with small cyan gems, sturdy handle, glowing surface | 0,50 m | — | `Static`; rachaduras procedurais (regra das rachaduras) |
| `balao` | prop/carga (45) | big round gas balloon with sunny yellow and cream stripes, small wicker basket tied with ropes | 0,60 m (balão 1 m acima das mãos) | — | `Static`; subida e contador por código |
| `lapide` | prop/decoração (45) | cracked rounded stone tombstone with a carved skull and moss | 1,20 m de largura | — | `Static` (veste a lápide 1,2 × 0,6) |
| `vela` | prop/decoração (45) | cluster of melted candles on an iron candelabra, small flames | 0,70 m | — | `Static` (+ luz âmbar decorativa; a luz de jogo é a `tocha` do Lote 3, reuso) |
| `sarcofago` | prop/decoração (45) | stone sarcophagus with a carved mummy face lid, cracks | 1,80 m de comprimento | — | `Static` (veste os obstáculos 0,8 × 1,8) |
| teia, véu do escuro, runa, raio, fio, casulo, bancos e estantes | procedural | — | — | — | 0 crédito |
| texturas piso/parede da cripta | gerador grátis | seamless tileable stylized crypt floor slabs, cold blue-grey / crypt brick wall with niches, top-down, hand-painted | — | — | 0 crédito |

### 12.3 Lote 6 — região 4 (≈ 710 créditos; teto pedido 900 por causa do dragão)

| Peça (preset) | Tipo | Assunto do prompt (inglês) | Altura | Separado | Clipes |
|---|---|---|---|---|---|
| `diabrete` | personagem rigado (65) | small mischievous imp, charcoal-red skin, tiny horns, bat ears, long thin tail, big toothy grin, lilac glowing eyes, leather loincloth, empty hands | 0,90 m | — | `Idle`, `Run` (Running, reuso), `Swap` (Blender: gira e some em 0,4 s), `Scratch` (Blender; impacto 0,5), `Hit` |
| `kobold` | personagem rigado (65) | small kobold lizard creature, teal-green scales, orange belly, short snout, small horns, leather apron with flask holders, goggles on the forehead, empty hands, no flask | 1,00 m | **frasco** → `RightHand` | `Idle`, `Walk` (Walking Backwards, recua), `Toss` (Blender; arremesso por baixo, soltura 0,6), `Hit` |
| `frasco` | prop (45) | round glass flask with bubbling orange liquid and a cork with a burning rag | 0,30 m | — | `Static` (o projétil é o mesmo sprite) |
| `magma` / `brasa` | personagem rigado (65) | magma elemental, body of black cooled basalt chunks with glowing orange lava cracks between them, huge rocky fists, small head with fiery eyes | 1,90 m / **1,10 m** (mesmo modelo, 2º render, 0 crédito) | — | `Idle`, `Walk` (Walking Forward), `Smash` (reuso do Blender do Bruto; impacto 0,6), `Split` (Blender ou partículas), `Hit` / brasa: `Idle`, `Run`, `Punch` |
| `capataz` / `capataz_fim` | personagem rigado (65) | kobold foreman, bigger and bulkier kobold, teal scales with scars, thick leather apron and shoulder pad, brass goggles, hard hat with a horn, empty hands, no whip | 1,40 m | **chicote** → `RightHand` | `Idle`, `Walk`, `Whip` (Blender; impacto 0,6), `Spin` (Blender; giro 360°), `Toss` (reuso do kobold), `Call` (Blender, apito), `Hit` / `Dying`; o laço do chicote no chão é procedural |
| `chicote` | prop (45) | long braided leather whip coiled, iron handle | 0,60 m | — | `Static` |
| `salamandra` | prop (45) | fire salamander, sleek charcoal-black body with bright orange ember stripes, glowing yellow eyes, flat head, long tail, four short legs, walking pose | 0,80 m de comprimento | — | `Static` + ondulação ±8° a 4 Hz por código |
| `dragao` / `dragao_fim` | modelo (45) + **armadura manual no Blender** (0 crédito, ~2–3 h) | ancient dragon, dark emerald scales, bronze horns and belly plates, glowing fiery throat, folded large wings, long tail, four legs, crouching, chunky stylized proportions | 3,50 m de comprimento | — | poses por script: `Idle` (2 quadros respirando), `Breath` (pescoço esticado, boca aberta), `Fly` (asas abertas, alto), `Land` (agachado, cabeça baixa = janela de dano), `Dying`. Plano B (HIPÓTESE): 4 poses geradas à parte no Tripo (+180) |
| `planta` | prop/carga (45) | carnivorous plant in a clay pot with handles, big green venus-flytrap head with pink inside and sharp teeth, leafy vines | 0,55 m | — | `Static` + 2ª pose de boca aberta no Blender (0 crédito) |
| `mapa` | prop/carga (45) | rolled old treasure map tied with a red cord, partly unrolled showing a dotted path and an X, parchment | 0,45 m | — | `Static` |
| `bigorna` | prop/decoração (45) | heavy blacksmith anvil on a wooden stump | 0,90 m de largura | — | `Static` (veste os obstáculos 1,2 × 0,8) |
| `braseiro` | prop/decoração (45) | iron brazier bowl on three legs filled with glowing coals | 0,70 m | — | `Static` (+ luz) |
| `corrente` | prop/decoração (45) | heavy iron chain hanging down with a big hook at the end | 1,50 m | — | `Static` (pendurada, sem colisão) |
| `respiro` | prop/terreno (45) | round iron floor grate over a glowing fire vent, soot around | 2,00 m de diâmetro | — | `Static` + brilho e chama por material e partícula |
| ovo de salamandra | reuso do `ovo` do Lote 2 com troca de material (preto com pintas laranja) | — | — | — | 0 crédito |
| lava, fogo no chão, fumaça | procedural | — | — | — | 0 crédito |
| texturas piso/parede da forja | gerador grátis | seamless tileable stylized basalt floor with soot / dwarven forge brick wall with iron plates, top-down, hand-painted | — | — | 0 crédito |

**Courier (0 crédito):** clipe novo `Struggle` (Blender, laço de 8 quadros: preso na teia e agarrado pela múmia); atropelado e empurrado usam o `Hit`.

**Total previsto dos 3 lotes: ≈ 2.130 créditos** (12 personagens rigados e 30 modelos sem rig), mais 180 de plano B do dragão. Vai ao `PROVENIENCIA.md` por lote antes de gerar (contrato §8).

**Memória (HIPÓTESE):** cada região só carrega o próprio elenco. Estimativa em ASTC por região ~12–16 MB de inimigos e chefes (Golem e Dragão em célula de 448–576 px são os maiores, como o Ogro com 9,6 MB). Se passar do teto de 32 MB residentes com o courier, corta-se quadro de `Idle`/`Walk` dos chefes primeiro, como no Ogro.

---

## 13. Pendências e o que o bot mede antes de fechar

- **Ainda falta no núcleo para este desenho** (o resto a raia N já pôs): elementos de terreno por sala (§0), `Spawn` com `Asleep`, `EnemyType.MagmaShard` e `SalamanderEgg` no fim, `Hint` novos (§1.4), `Outcome.Melted` e `Escaped`, `EggTarget` reaproveitado como `MeltTime` do gelo (ou campo próprio), mira só na luz, ícone de risco pelo terreno (respiro/lava) e as propostas que mudam provisórios: pagamento e tempos do contrato gerado por região (§1.2), equipamento em 5 níveis (§8), projeto da semana do evento e cura de 2 no Boss Run (§9.1), mini/chefe e teto diário no sem fim (§10.1).
- **Toda linha com número aqui é HIPÓTESE:** em especial vida e tempo dos 6 chefes, o ciclo do vagonete (4,7 s), o raio de 2 m de quem dorme, a taxa do gelo, os 6 s do grifinho, os 4 s do balão, o teto de moedas do coração, o pagamento das regiões 3–4 e a sobra de ~20 mil moedas (§10.3).
- **Ícones de ameaça** de todas as 30 salas novas saem da medição (o desenho só chuta o terço).
- **Arte:** o escuro da região 3 precisa da exceção no `ART_BIBLE.md` §12; o dragão sem rig humanoide é o maior risco de produção do Lote 6.
