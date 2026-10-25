# VIEW_SPRITES — como a View usa as folhas (Dungeon Courier)

**Estado em 2026-10-09 (raia F, polimento da View).** Código compilado só pelo `viewcheck` (`dotnet build client/tools/viewcheck/view`). **Não rodado no Unity:** o shader, o material e as fotos ainda dependem do coordenador.

**Arquivos:**
- `client/Assets/_DC/Scripts/View/WorldView.cs`: mundo, medidas e Lote 2.
- `client/Assets/_DC/Scripts/View/SpriteSheet.cs`: `stride_mps`, ritmo.
- `client/Assets/_DC/Scripts/View/Art.cs`: `FxMat` e `Fx`.
- `client/Assets/_DC/Scripts/View/Hud.cs`: cena da entrega.
- `client/Assets/_DC/Shaders/SpriteFlash.shader` e `client/Assets/_DC/Resources/Materials/SpriteFlash.mat`.

O formato das folhas está em `RENDER_SPRITES.md` §4. Sem folha, sem material ou com `-nographics`, a View desenha o procedural de sempre (§6).

## 1. Ritmo da locomoção (`stride_mps`)

- O `meta.json` pode trazer `stride_mps` por clipe: a velocidade em que o pé não patina no fps do clipe.
- A View avança o tempo do clipe por `dt × velocidade ÷ stride_mps`, com teto de **2,3×** (receita do COE) e piso de **0,5×** (corrida travada na parede não congela). É o `fps × (velocidade ÷ stride_mps)` do `RENDER_SPRITES.md` §4.
- Clipe sem `stride_mps` (Idle, Throw, Lurk, props): o fps do meta, 1×.
- **Velocidade usada:**

| Ator | Velocidade | Ritmo com as folhas do Lote 1 |
|---|---|---|
| Courier | medida na Sim (média móvel de 0,1 s: a Sim anda em passos de 1/30 s) | Carry 4,2 m/s → 1,18×; sem carga 4,83 → 1,36×; dedo a meio caminho → menos |
| Ladrão | 2,0 m/s (`Def.Speed`); fugindo com a carga 1,6 (`ThiefFlee`) | 1,96× / 1,57× |
| Bruto, ogro | `Def.Speed` (1,6 / 1,2) | conforme o `stride_mps` do Lote 2 |

- **Ladrão / Grab:** o clipe inteiro (0,77 s) cabe nos 0,5 s do Core (`GrabT`). Assim ele termina na pose do `Hold`, que emenda no `CarryRun`.

## 2. Medidas calibradas

| Constante (`WorldView`) | Valor | Fonte |
|---|---|---|
| `ThrowRelease` | 3/8 | `ASSETS.md` §5: courier/Throw solta no quadro 3 de 8 |
| `ImpactFrac` (por tipo) | guarda 6/12; bruto e ogro 0,6 | guarda/Bash: quadro 6 de 12 (`ASSETS.md` §5). Smash e Slam são **estimativa** até medir o Lote 2 |
| `ArchTop` | 1,78 m | PNG do `arco`: topo opaco na linha 64, pivô na linha 277,7 (448 px, 120 px/m) |
| `ArchOpen` / `ArchOpenW` | 0,52 m / 1,2 m | Vão escuro **opaco** de 1,21 m de largura, do pivô até 0,55 m, com quinas curvas desde 0,45 m |
| `CargoSize` | caixa 0,6; relíquia e ovo 0,5 | Caixa: silhueta de 0,58 × 0,69 m de tela no PNG. Relíquia e ovo pela `ART_BIBLE.md` §4, até as folhas do Lote 2 |
| `CargoLine` | 0,06 m (7,2 texels a 120 px/m) | Contorno limão da carga no chão: ~6,7 px em 1080 e ~3,3 px em 540 |
| `PetHop` | 0,15 m de altura real (0,075 m na tela) | `ASSETS.md` §4 |
| `Hud.CrateCell` / `CrateTop` | 450 px de referência / 0,36 | A silhueta da caixa ocupa 70 × 83 de 112 px, centrada na célula: ~280 px de largura, como o retângulo procedural. Topo dela a 0,375 da célula acima do centro |

**Como a porta mudou com o arco:**
- O vão do sprite é opaco: a grade procedural ficava escondida atrás dele (fotos `shots_arte`, 20 e 21).
- Agora a grade é desenhada **por cima** do arco, no vão medido (1,2 × 0,52 m), com o cadeado por cima dela.
- A placa ia a 1,4 m e ficava atrás do topo do arco (1,78 m). Agora fica **a cavalo na borda de cima do arco**: metade sobre a pedra.
- Ordens de desenho fora de `SortingGroup`:
  - decalques do chão: sombra e anéis dos atores, 7 a 9;
  - soleira 10, arco 11, grade 12, cadeado 13;
  - placa 14 a 16.

## 3. Enquadramento (largura de 9,7 m)

**Pedido:** `ART_BIBLE.md` §2 pede ~9,6 m visíveis (112,5 px/m em 1080 px).

**O que a View mostra agora:**
- Largura: de −0,35 a 9,35 m, sala mais as duas paredes, rente (antes: −0,55 a 9,55 = 10,1 m).
- Embaixo: até −0,4 m (a parede de baixo, mais 5 cm).
- Em cima: até a placa, mais 0,14 m.

O `Fit` usa o menor entre o px/m da largura e o da altura. Por isso nada é cortado em nenhuma tela: se a altura apertar, sobra um pouco de largura.

Conta com a HUD em 15% da área segura (área segura = tela inteira):

| Tela | Altura do conteúdo | px/m (em 1080 px) | Largura visível | Limite |
|---|---|---|---|---|
| 540×960 / 1080×1920 (16:9) | 14,70 m | 111,0 | 9,73 m | altura |
| 1080×2400 (20:9) e 1080×2340 | 14,70 m | 111,3 | 9,70 m | largura (sobram ~210 px em cima e embaixo) |
| sem a folha `arco` (16:9 e 20:9) | 13,78 m | 111,3 | 9,70 m | largura |

- O sprite de 120 px/m aparece a 0,93× (antes: 0,89×).
- Se a placa ficasse inteira acima do arco, a largura em 16:9 seria 9,92 m (108,9 px/m).
- Celular com entalhe perde altura útil: com 100 px a menos em 1080×2400, ainda sobra (17,6 m de altura para 14,7).

## 4. Shader `DC/SpriteFlash` (flash branco e contorno)

**O problema:** o `Sprites/Default` só multiplica a cor, então o flash de dano era um rosa (`Flash`). O contorno da carga era um retângulo arredondado atrás dela.

**O shader** (`Assets/_DC/Shaders/SpriteFlash.shader`) é Cg simples do Built-in, `#pragma target 3.0`, sobre o `UnitySprites.cginc`. Isso mantém a cor do `SpriteRenderer`, o `flipX` (`_Flip`) e o alfa pré-multiplicado.
- `_Flash` (0..1): mistura a textura para branco **antes** da tinta do renderer.
  - Dano: flash 1 com a cor branca.
  - O filhote usa o valor parcial: desbota em 0,3 s.
- `_Outline` (texels) e `_OutlineColor`: contorno por baixo do sprite, com 8 amostras `tex2Dlod` num raio fixo.
  - Ramo uniforme: com 0, as amostras não rodam. Os atores pagam só o flash.
  - Só vale para folha de 1 célula (as cargas): numa folha com várias células, a amostra leria a célula vizinha.
- Os valores vão por renderer em `MaterialPropertyBlock` (`Art.Fx`). É um bloco só, sem alocação por quadro. O `Fx` é chamado depois de trocar o sprite do quadro.
- **Quem usa:** o sprite dos atores (courier, inimigos, filhote) e o das cargas. Partículas, cenário, cliente e baú continuam no `Sprites/Default`.

**Como entra no build:** o material `Assets/_DC/Resources/Materials/SpriteFlash.mat` referencia o shader pelo GUID, e tudo em `Resources/` vai para o build com as dependências.
- O `Setup.cs` e os Always Included Shaders **não foram mexidos**: não foi preciso.
- O shader não tem keyword nem `multi_compile`: é uma variante só, e nada some por strip.

**Reserva:**
- `Art.FxMat` carrega o material uma vez e confere `shader.isSupported`.
- Sem material ou sem suporte, a View volta à tinta antiga e ao retângulo atrás da carga.
- O shader tem `Fallback "Sprites/Default"`.
- Para desligar tudo, basta apagar o `.mat`.

**Custo:**
- Atores: 1 amostra, como antes, mais um `lerp`.
- Carga no chão: +8 amostras em ~105 × 105 px de quadro.
- Cada sprite com o material tem bloco próprio e não entra no batch dinâmico com outro. Os atores já usam texturas diferentes, então quase nada muda. Medir no aparelho: Batches e SetPass no Stats.

**Como conferir:**
1. No log (`Player.log` / logcat), uma linha só: `DC SpriteFlash: ok (DC/SpriteFlash)`. Se aparecer `DC SpriteFlash: material ausente ou shader sem suporte`, o shader não entrou ou não roda.
2. **Foto `-shot` com a carga no chão** (o `21_c1_bot_4s` serve): o contorno limão abraça a silhueta da caixa (cordas, quinas), não um retângulo arredondado.
3. **Flash** (0,06–0,08 s, difícil de pegar numa foto):
   - no Editor, `-contract 1` em Play, pausar no quadro do "-1" vermelho: o courier fica **branco**, não rosa;
   - ou no Frame Debugger, o draw do `Sprite` do courier com `DC/SpriteFlash` e `_Flash = 1`.
4. **Sprite magenta = erro de compilação do shader** (ver o Console). Para voltar ao estado anterior, apague o `.mat`.

## 5. Cena da entrega

- **Carga Caixa com a folha `caixa`:** o sprite estático substitui o retângulo com cinta, na mesma posição (`Hud.CratePos`). A tampa procedural assenta no topo da silhueta e abre por cima, como antes.
- **Relíquia, ovo e ovo chocado:** a caixa continua procedural. O item que sobe usa o sprite (`reliquia`, `ovo` ou `filhote`) quando a folha existe, com escala 1,6, porque o objeto ocupa ~60% da célula.

## 6. Lote 2 (o que a View já faz quando as pastas aparecerem)

| Conjunto | O que a View faz |
|---|---|
| `reliquia`, `ovo` (Static) | Carga nas mãos pela âncora `hands`, como a caixa, no chão pelo pé, com o contorno pela silhueta; o ovo balança nos últimos 5 s; e o item da cena da entrega |
| `filhote` (Static) | Sprite no lugar do corpo procedural, com sombra de contato. Pulo de 0,15 m e squash no pé, flipX pelo movimento (a mesma convenção do morcego: vira quando anda para a direita) e flash que desbota no `PetHit` |
| `bruto` (Walk, Idle, Smash) | Walk perseguindo, no ritmo da passada. Smash pelo telegraph: quadros [0, impacto) esticados no `Tele` de 0,6 s e o resto em 0,15 s. Idle parado |
| `ogro` (Walk, Idle, Slam, Roar) | Slam pelo telegraph de 1 s. **Roar** inteiro quando nasce um ladrão com o ogro vivo (`Ev.ThiefSpawn`; o Core chama um a cada 10 s no B1) |
| `ogro_fim` (Dying) | No `EnemyDie` do ogro, um corpo toca o Dying uma vez onde ele caiu, para no último quadro e some com a sala |

**Pendências do Lote 2 (medir na folha e trocar 1 número cada):**
- quadro de impacto do Smash e do Slam (`ImpactFrac`);
- `CargoSize` da relíquia e do ovo;
- se o filhote olhar para a direita no S, inverter o `FlipX` no `DrawPet`.

**Limite conhecido:** o `ThiefSpawn` não diz quem fez o ladrão nascer. O da carga sozinha, com o ogro vivo, também faz o ogro rugir. Se incomodar, o Core precisa de um evento próprio.

## 7. Revisão de mesa: sem folhas

| Faltando | Comportamento |
|---|---|
| `Resources/Sprites/<ator>` | `SpriteSheet.Get` devolve null e o ator fica procedural (`UseSprite` só liga com folha; o filhote nem chama `UseSprite`) |
| `arco` | Vão e grade procedurais na parede; placa acima do tampo (conta da §3, linha "sem a folha") |
| `caixa`, `reliquia`, `ovo` | Forma procedural; contorno = a própria forma |
| `ogro_fim` | Sem corpo: o ogro some com a poeira, como antes |
| `SpriteFlash.mat` ou suporte ao shader | Tinta `Flash` e retângulo de contorno (comportamento de antes) |
