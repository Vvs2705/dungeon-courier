# Lote do prazo — Tripo3D até 2026-11-29 (Dungeon Courier)

A conta A do Tripo3D vence em **2026-11-29**. O Dungeon Courier já tem as 72 peças dos Lotes 1–6 (`PROVENIENCIA.md` §2) e o cenário é procedural por decisão (ART_BIBLE §11), então este lote é pequeno e só pega o que o jogo **já usa e está errado ou é provisório**:

1. **Golem v2.** O golem do Lote 4 saiu diferente do desenho: gema âmbar no peito, pedra bege e nenhum cristal ciano nas costas (`RENDER_SPRITES.md`, "Golem do Tripo diferente do prompt"). O ponto fraco do chefe é **por trás** (×2), e o sprite de hoje sugere o contrário. O prompt do `ASSETS.md` §9 já pedia o núcleo no peito; o certo é o do `CONTEUDO_REGIOES.md` (cristais ciano nas costas).
2. **Roupas com silhueta própria.** As 7 roupas da loja (`Hud.FolhaRoupa`) são o mesmo modelo com troca de cor (`roupas_courier.py`). Uma roupa paga que só muda de cor lê pouco a 60° numa tela de 6,5". Este lote gera a versão com forma própria das 6 que têm corpo (a `fantasma` é shader e fica).

Mesmo formato do `chronicles-of-existence/docs/arte/LOTE_PRAZO_29.md`. **Gerar agora, aprovar depois:** nada daqui entra em `client/Assets/_DC/Resources/` antes do render (`render_sprites.py --hd K --escala 1,25`), da foto 540×960 sobre os 4 pisos e do `verify.sh full`. A roupa nova não pode mudar o tamanho do corpo nem a leitura da carga nas mãos (ART_BIBLE §1, pilar 1).

## 0. Regras que não mudam (leia uma vez)

1. **Privado sempre.** **Não usar o Agentic Canvas do Tripo**: ele não tem geração privada (testado no COE em 2026-11-27).
2. **Baixar no mesmo dia, tudo.** Zip/FBX de cada malha (o do rig em FBX Mixamo 2K) e o PNG da imagem escolhida. Último download até **29/11 às 20h**.
3. **Onde salvar (no PC, fora do git):** `dungeon-courier\arte\tripo\_prazo29\<id>\`, nome `<id>_<tipo>_v01.<ext>` (`v02`… nunca sobrescreve). `arte/tripo/` já está no `.gitignore` e o repositório é **público**.
4. **Uma linha por geração** em [`LOTE_PRAZO_29.csv`](LOTE_PRAZO_29.csv): `data,conta,id,tipo,task_id,creditos,arquivo,obs`. A linha completa da `PROVENIENCIA.md` §2 (SHA-256 do zip e do FBX) se escreve na integração.
5. **Encher a fila:** 5 a 10 tarefas de uma vez.
6. **Créditos reais do COE em 2026-11-27:** imagem GPT Image 2.5 ≈ 8 (Nano Banana 2 grátis enquanto houver cota do dia: use primeiro), malha HD H3.5 ≈ 60, Auto Rig ≈ 20.

## 1. Receita

| Tipo | Onde no Tripo | Opções |
|---|---|---|
| `img` | Imagem (Nano Banana 2; se acabar, GPT Image 2.5), 1:1, 4 variações | assunto + `[S][T][I]`; fica com a que está em pose T de verdade, braços livres, mãos vazias |
| `malha` | Imagem para 3D | HD (H3.5), textura ligada, PBR ligado, 8K desligado, **Privado** |
| `rig` | Auto Rig humanoide, esqueleto Mixamo | baixar FBX Mixamo 2K |
| `var` | 2ª malha de outra das 4 imagens | só onde a linha pede |

## 2. Bloco de estilo (os sufixos do `historico/ASSETS.md`, os mesmos dos 6 lotes)

```text
[S] stylized fantasy comic game character, chunky proportions 3.5 heads tall, big hands and boots, flat colors with soft two-tone shading, clean readable silhouette, single character, centered, no background, no text, no logo
[T] T-pose, arms straight out horizontal, legs slightly apart, open empty hands, nothing held in the hands, neutral face, symmetrical, fitted clothes on the legs
[I] full body front view, T-pose arms horizontal, soft even studio light, plain light grey background, 3D render look, no ground shadow
```

Reforço deste lote (fim de toda linha): `Do not copy any existing game, film or artist.`

**Base do courier** (todas as roupas partem dela; é o prompt do Lote 1 #1): `young human courier of an adventurers' delivery guild, friendly determined face, big wide-brim courier cap with a small brass guild badge, short tunic over a cream shirt with rolled sleeves, bulky leather cross-body messenger satchel with buckles, short scarf, fingerless leather gloves, sturdy boots`. Cada roupa troca as cores e acrescenta **uma** forma nova acima dos ombros ou na cintura (o que lê a 60°), nunca nas mãos nem à frente do peito, onde vai a carga.

## 3. Lote

| # | id | passos | assunto do prompt (+ `[S][T][I]` + reforço) | créd. |
|---|---|---|---|---|
| D01 | `golem` v2 | img → malha → rig → var | `massive stone golem boss, grey-blue boulder body with moss patches, tiny head sunk between huge shoulders, enormous rock fists, a big cluster of glowing cyan crystals growing out of its BACK between the shoulder blades, plain rock chest with no gem and no glow on the front, empty hands`. Na escolha da imagem, preferir a que mostra os cristais por cima do ombro na vista frontal | 148 |
| D02 | `courier_noturno` | img → malha → rig | base + `deep indigo blue tunic and cap, almost black leather satchel, boots and gloves, a short hooded night cloak over the shoulders with the hood down, small silver crescent badge` | 88 |
| D03 | `courier_real` | img → malha → rig | base + `royal purple tunic and cap with gold trim, gold-brown leather, a short purple shoulder cape with a gold clasp, a small gold feather on the cap` | 88 |
| D04 | `courier_explorador` | img → malha → rig | base + `khaki tunic, dark leather, the cap replaced by a khaki explorer hat with a wide stiff brim, a rolled bedroll strapped across the top of the satchel, a small brass compass on the belt` | 88 |
| D05 | `courier_forja` | img → malha → rig | base + `fire red tunic, charcoal leather, a short heavy leather smith apron over the tunic, protective round goggles pushed up on the cap, soot marks on the gloves` | 88 |
| D06 | `courier_mineiro` | img → malha → rig | base + `brown work tunic, the cap replaced by an amber miner helmet with a small lamp on the front, a coil of rope over one shoulder, knee pads` (âmbar, não o amarelo-limão da carga `#FFE14D`) | 88 |
| D07 | `courier_coveiro` | img → malha → rig | base + `greyish sage green long coat to the knees, dark leather, a tall narrow top hat instead of the cap, a small iron lantern hanging from the belt` (sálvia acinzentada, longe do `#6F8A4B` do Ogro e do `#7CFF9E` dos mortos-vivos) | 88 |

**Total do lote ≈ 676 créditos.** As cores de papel do ART_BIBLE §3 valem: nenhuma roupa usa roxo de ladrão com saco nas costas, vermelho-escudo de guarda nem o amarelo da carga.

**Reserva (só se sobrar no fim da fila conjunta):** `var` das 6 roupas (outra imagem → malha; o rig só na escolhida) ≈ 360.

## 4. Ordem global com o Forge Street

A conta é uma só. Ordem de execução das duas filas: **D01** → Forge F01–F15 → **D02–D07** → Forge F16–F23 → reservas (Forge R1, R2 e a daqui). Fila do Forge: `forge-street/docs/LOTE_PRAZO_29.md`.

## 5. Depois do dia 29 (sem pressa)

1. Golem: `render_sprites.py` com o preset do golem (2,60 m, os mesmos clipes: Idle, Walk, Slam, Charge, Stun, Dying), foto contra o antigo; se ler melhor, troca a folha `golem`/`golem_fim` e some o brilho procedural das costas que a nota do `RENDER_SPRITES.md` pedia.
2. Roupas: render com os 7 clipes do courier (`-Preset courier`), mesma altura do `courier` (normalizar no Blender, nunca no Unity). Foto sobre `piso`, `piso_mina`, `piso_cripta` e `piso_forja`. Se o APK apertar, vale o corte do `RENDER_SPRITES.md` (Idle, CarryIdle, Run e CarryRun por roupa).
3. Linha completa na `PROVENIENCIA.md` §2 e §5.
