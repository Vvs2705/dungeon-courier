# Dungeon Courier: os 13 criativos de UA (GDD §17, CONTRATO_LANCAMENTO §9.3)

Roteiros dos vídeos do teste de mercado (GDD §15–§16), gravados do jogo real com a flag `-record` e montados pelo `client/tools/criativos.py`. A montagem lê as cenas, os tempos e as legendas das tabelas `G` (gravações) e `R` (roteiros) do script. **Mudou aqui, mude lá também.** O `--autoteste` confere as regras do §1 em cima dessas tabelas.

## 1. Regras

- **Só jogo real** (guardrail do estúdio). Cada cena é um trecho de uma gravação do build, com o bot ou com uma pessoa jogando. Nenhuma cena é montada, encenada ou desenhada. Por cima vai só a legenda e, no fim, o CTA.
- **Gancho nos 3 primeiros segundos com carga e combate na tela** (GDD §25: o tema "entrega" pode ser lido como idle-arcade). A legenda do gancho começa em 0 s.
- **9:16, 1080 × 1920, 30 fps, H.264 + AAC, de 15 a 30 s** com o CTA.
- **CTA de 1,5 s** no fim: logo da tela de título, o botão verde "Baixe grátis" e "Sem energia. Sem gacha." no idioma (GDD §27 C7).

  | pt | en | es | id |
  |---|---|---|---|
  | Baixe grátis · Sem energia. Sem gacha. | Download free · No energy. No gacha. | Descarga gratis · Sin energía. Sin gacha. | Unduh gratis · Tanpa energi. Tanpa gacha. |
- **Som:** o do jogo, gravado junto (`audio.wav`, AudioRenderer). Onde não há gravação de som, entra silêncio. Nenhuma música de terceiros.
- **4 idiomas** (pt, en, es, id). Legenda curta, glossário do `UI_V1.md` §3. Grave cada idioma com `-lang`, para o HUD sair no idioma do vídeo. Sem a gravação do idioma, o script usa a `_en` e depois a `_pt`.
- **13 = o 11 com outra roupa do courier.** O GDD pedia "mascote mímico × courier humano", mas o mascote foi descartado. O teste A/B passa a ser de roupa (CONTRATO_LANCAMENTO §8: 6 roupas).

**O que cada criativo testa** (GDD §16): carregar ocupa as mãos → 1, 7, 11, 12 · decisão/rota → 2, 6 · cliente e humor → 3, 5, 9 · ação pura → 8, 10 · carga nova → 4 · roupa (A/B) → 11 × 13.

## 2. Gravação

```text
client/Builds/win-release/DungeonCourier.exe -screen-width 540 -screen-height 960 -screen-fullscreen 0 -testsession -lang L <flags>
    -record client/Builds/rec/<gravacao>_L -recordfrom <s> -recordsecs <s>
```

- `-record` (`Game.cs`, só com `-testsession`) trava o passo em 1/30 s por quadro (`Time.captureFramerate`) e pula `-recordfrom` segundos de jogo sem gravar. Depois grava `-recordsecs` segundos em `00000.png`… (540 × 960, o tamanho da janela) e o som em `audio.wav`, e sai. O jogo não pausa se a janela perder o foco. A linha `RECORD inicio`/`RECORD fim` do log diz o contrato, a sala e o `t=` de cada ponta.
- **Use o build de release**: o de desenvolvimento escreve "Development Build" no canto.
- `python -I client/tools/criativos.py --comandos --lang pt` imprime todos os comandos de gravação (ou só os de `--roteiro 1,11`).
- **Disco:** cada quadro tem cerca de 0,55 MB. As gravações do bot somam 181 s por idioma, cerca de **3 GB por idioma** (mais 0,5 GB da gravação humana). Faça um idioma por vez: grave, monte com `--limpar` (apaga os quadros usados) e só depois grave o próximo.
- **Tempos:** medidos em 2026-10-11 com o Core da `feat/lancamento`, semente 1, `Bot(1)` e guilda 0 (a mesma partida do `-bot`). O passo do bot é fixo, então a partida se repete quadro a quadro. Se o Core mudar, confira o momento com uma foto antes de gravar (`-shot x.png -shotdelay <t>`).

### 2.1 Gravações

| Gravação | Flags | `-recordfrom` | `-recordsecs` | O que acontece (t do contrato, s) |
|---|---|---|---|---|
| `c01` | `-contract 1 -bot` | 9.5 | 16.5 | R3: ladrão 14,2; entrega 20,3; cena do cliente 21,4–25,0 (perfeita) |
| `c02` | `-contract 2 -bot` | 0.5 | 17.0 | R3 3,2; pousa 5,4; ladrão 7,0; dash 7,1; entrega 12,4; cena 13,5–17,1 |
| `c03` | `-contract 3 -bot` | 7.5 | 10.0 | portas do R1; entra no R4 (3 caveiras) 11,0; cai 13,9; resultado 15,0 |
| `c04_inicio` | `-contract 4 -bot` | 0.0 | 13.0 | R5 com Bruto; baú 5,4; R3 7,3; baú 11,7 |
| `c04_fim` | `-contract 4 -bot` | 49.5 | 17.5 | vida 2; vida 1 50,2; S1 52,7 (+1); vida 1 54,4; D1 56,4 (+1, sala nova cura); entrega 59,0 com 2; cena 60,1–63,7 |
| `c05_bumerangue` | `-contract 5 -bot -weapon 1` | 14.0 | 15.5 | guardas e limo 14–20; R3 ~20; relíquia: rachaduras 22,5 / 24,3 / 25,5 (quebra); resultado 26,6 |
| `c06_bomba` | `-contract 6 -bot -weapon 2` | 0.5 | 10.5 | as explosões da bombinha racham o ovo: 3,2 / 4,3 / 9,8 |
| `c06_bumerangue` | `-contract 6 -bot -weapon 1` | 0.5 | 6.5 | R6: ladrão 4,2; dash 4,9 |
| `c06_bumerangue_fim` | `-contract 6 -bot -weapon 1` | 56.5 | 4.5 | entrega 55,7; cena 56,8–60,4 (ovo intacto) |
| `c07` | `-contract 7 -bot` | 32.5 | 15.5 | bolo, vida 1; baú 38,0; R2 40,1 (vida 3); vida 1 41,8; cai 42,8; resultado 43,9 |
| `c12_r4` | `-contract 12 -bot` | 0.0 | 5.0 | R4: baú 2,0; dano 3,2; reforço 4,1 |
| `c12_r14` | `-contract 12 -bot` | 24.3 | 5.0 | R14 24,6; dano 28,5; baú 28,8 |
| `c12_m1` | `-contract 12 -bot` | 43.0 | 10.5 | M1 43,5; o Capitão chama ladrões 46,5–50,1; foge com a carga 51,1; resultado 52,2 |
| `c24_ogro` | `-contract 24 -bot -weapon 1` | 112.0 | 7.0 | B1: pancada do Ogro 113,0/114,0; ladrão 114,8; dash 115,0 |
| `c24_fim` | `-contract 24 -bot -weapon 1` | 165.5 | 16.5 | ladrão 166–167; o Ogro cai ~172,5; entrega 176,5; cena 177,6–181,2 ("Bom! Só 1 rachadura") |
| `quadro` | `-tela quadro` | 0.0 | 2.0 | quadro de contratos parado |
| `cod_ovo` | `-codigo JLQWHG -bot` | 145.5 | 23.0 | região 2 por código (ovo, 6 camadas): choca 150,0; entrega 163,7; cena 164,8–168,4 |
| `c29_coracao` | `-contract 29 -bot` | 0.0 | 14.0 | M2-5 (coração arcano): reforço 1,8; dano 2,2/3,4; baú 4,1 |
| `c02_roupa` | `-contract 2 -bot -roupa noturno` | 0.5 | 17.0 | igual ao `c02`, com a roupa Noturno |

No C24 o bot só vence o Ogro com o bumerangue (`-weapon 1`). Com o estilingue, perde a carga no R16 aos 16,6 s.

## 3. Roteiros

Formato das cenas: `gravação [de–até]`, em segundos da gravação (0 = o quadro `00000.png`). As legendas estão no tempo do vídeo, sem o CTA.

### 1. Carga frágil (GDD: "pacote frágil em dungeon cheia de espinhos") · 16 s · bot

O jogo não tem espinhos. O perigo da relíquia vem dos arqueiros e dos ladrões.

- **Gancho (0–4 s):** a relíquia, os guardas e a luta na sala.
- **Cenas:** 0–14,5 `c05_bumerangue [0,5–15,0]`: luta com os guardas e o limo, 2 rachaduras (vídeo 8,0 e 9,8), a 3ª quebra a relíquia (11,0) e o resultado de falha. Até 2026-10-11 começava no C8, mas no build atual a relíquia do C8 chega intacta.

| t (s) | pt | en | es | id |
|---|---|---|---|---|
| 0–4 | Relíquia frágil. Guardas na sala. | Fragile relic. Guards in the room. | Reliquia frágil. Guardias en la sala. | Relik rapuh. Ada penjaga di ruangan. |
| 4–8 | Pousa para lutar, pega e segue | Set it down to fight, grab it, go | Déjala para luchar, tómala y sigue | Taruh untuk bertarung, ambil, lanjut |
| 8–11 | Cada golpe deixa uma rachadura | Every hit leaves a crack | Cada golpe deja una grieta | Tiap pukulan bikin retakan |
| 11–14,5 | ...quebrou! Você entregaria inteira? | ...broken! Could you deliver it whole? | ...¡se rompió! ¿Tú la entregarías entera? | ...pecah! Kamu bisa antar utuh? |

### 2. Duas portas: segura × tesouro · 17 s · bot · legenda embaixo (as portas ficam livres)

- **Gancho:** o courier com a caixa e o Bruto na sala R5. As portas mostram as caveiras (ameaça) e o baú (recompensa).
- **Cenas:** 0–12,5 `c04_inicio [0–12,5]` luta, baú da sala, escolha da porta com baú e o outro baú · 12,5–15,5 `c04_fim [14,3–17,3]` resultado com as moedas dos baús.

| t (s) | pt | en | es | id |
|---|---|---|---|---|
| 0–3 | Porta segura ou porta do tesouro? | Safe door or treasure door? | ¿Puerta segura o puerta del tesoro? | Pintu aman atau pintu harta? |
| 3–7,3 | Primeiro, o baú desta sala... | First, this room's chest... | Primero, el cofre de esta sala... | Pertama, peti di ruangan ini... |
| 7,3–12,5 | ...depois, a porta com outro baú | ...then the door with another chest | ...luego, la puerta con otro cofre | ...lalu pintu dengan peti lain |
| 12,5–15,5 | Baús pagam. Mas cada porta cobra. | Chests pay. But every door has a price. | Los cofres pagan. Pero cada puerta cobra. | Peti memberi koin. Tapi tiap pintu ada harganya. |

### 3. Entrega por um triz (GDD: "entrega com 1% de vida") · 15,2 s · bot

- **Gancho:** 2 corações, a caixa nas mãos e a luta na sala.
- **Cenas:** 0–13,7 `c04_fim [0,5–14,2]` cai para 1 coração na sala secreta (vídeo 4,4 s), a porta da entrega cura 1 (sala nova, `Balance.RoomHeal`, vídeo 6,4 s) e a cena do cliente (perfeita). Até 2026-10-11 a legenda dizia "entregue com 1 de vida", mas a entrega acontece com 2 corações: a legenda segue a tela.

| t (s) | pt | en | es | id |
|---|---|---|---|---|
| 0–4,4 | Quase sem vida, com a caixa nas mãos | Almost out of health, crate in hand | Casi sin vida, con la caja en las manos | Nyawa hampir habis, kotak di tangan |
| 4,4–6,4 | Sobrou 1 coração... | Down to 1 heart... | Queda 1 corazón... | Tinggal 1 nyawa... |
| 6,4–9 | Porta nova: +1 coração. Corre! | New room: +1 heart. Run! | Sala nueva: +1 corazón. ¡Corre! | Ruang baru: +1 nyawa. Lari! |
| 9–13,7 | ENTREGUE por um triz! | DELIVERED by a hair! | ¡ENTREGADO por un pelo! | TERKIRIM nyaris saja! |

### 4. Carga que atrai monstros (coração arcano) · 15 s · bot

Contrato M2-5 (`-contract 29`), medido em 2026-10-12: R2-3 com reforço aos 1,8 s, dano 2,2/3,4, baú 4,1; R2-1 5,2; R2-10 8,5; R2-8 13,2.

- **Gancho:** o coração nas mãos, o raio visível puxando moedas e inimigos ao mesmo tempo.
- **Cenas:** 0–13,5 `c29_coracao [0–13,5]`.

| t (s) | pt | en | es | id |
|---|---|---|---|---|
| 0–4 | Coração arcano: puxa moedas... | Arcane heart: pulls in coins... | Corazón arcano: atrae monedas... | Jantung arkana: menarik koin... |
| 4–9 | ...e puxa monstros também | ...and pulls in monsters too | ...y también atrae monstruos | ...dan juga menarik monster |
| 9–13,5 | Vale o risco? | Worth the risk? | ¿Vale el riesgo? | Sepadan risikonya? |

### 5. O ganancioso pega o baú e cai · 16 s · bot

- **Gancho:** 1 coração, o bolo de casamento e a luta na sala R5.
- **Cenas:** 0–14,5 `c07 [0,5–15,0]`: vai ao baú com 2 de vida, entra na sala seguinte, cai e mostra o resultado. A carga fica na masmorra, e o resgate entra no contrato seguinte (diferencial C4 do GDD §27).

| t (s) | pt | en | es | id |
|---|---|---|---|---|
| 0–3 | 1 coração e um bolo de casamento... | 1 heart and a wedding cake... | 1 corazón y un pastel de bodas... | 1 nyawa dan kue pengantin... |
| 3–7,1 | ...mas olha aquele baú! | ...but look at that chest! | ...¡pero mira ese cofre! | ...tapi lihat peti itu! |
| 7,1–10,9 | A ganância custa caro | Greed costs a lot | La codicia sale cara | Serakah itu mahal |
| 10,9–14,5 | A carga fica na masmorra: volte para resgatar | The cargo stays in the dungeon: come back for it | La carga se queda en la mazmorra: vuelve por ella | Paket tertinggal di dungeon: kembali selamatkan |

### 6. Rota lotada × rota tranquila (GDD: "rota curta lotada vs longa segura") · 21,5 s · bot · legenda embaixo

No jogo, toda rota tem o mesmo número de camadas. A escolha real é entre salas com mais ou menos caveiras.

- **Gancho:** o courier com o ovo lutando no R1, com as duas portas (2 e 3 caveiras) no topo.
- **Cenas:** 0–9 `c03 [0,5–9,5]` escolhe a de 3 caveiras e cai em 3 s · 9–20 `c01 [0,5–11,5]` outro contrato: a porta de 1 caveira, o ladrão e a entrega.

| t (s) | pt | en | es | id |
|---|---|---|---|---|
| 0–3 | 3 caveiras ou 2 caveiras? | 3 skulls or 2 skulls? | ¿3 calaveras o 2 calaveras? | 3 tengkorak atau 2 tengkorak? |
| 3–9 | Escolheu a lotada... caiu em 3 segundos | Picked the crowded one... down in 3 seconds | Eligió la llena... cayó en 3 segundos | Pilih yang ramai... tumbang dalam 3 detik |
| 9–13 | Agora: a porta de 1 caveira | Now: the 1-skull door | Ahora: la puerta de 1 calavera | Sekarang: pintu 1 tengkorak |
| 13–20 | Rota certa, carga entregue | Right route, cargo delivered | Ruta correcta, carga entregada | Rute tepat, paket terkirim |

### 7. "Não use explosão perto do ovo" · 20,6 s · bot

- **Gancho:** o ovo no chão, a bombinha explodindo do lado e a 1ª rachadura aos 2,2 s.
- **Cenas:** 0–9,5 `c06_bomba [0,5–10,0]` 3 rachaduras · 9,5–15,5 `c06_bumerangue [0–6]` a mesma sala com o bumerangue · 15,5–19,1 `c06_bumerangue_fim [0,3–3,9]` cliente com o ovo intacto.

| t (s) | pt | en | es | id |
|---|---|---|---|---|
| 0–3 | Bombinha perto do ovo? | Little bomb next to the egg? | ¿Bombita junto al huevo? | Bom kecil di dekat telur? |
| 3–9,5 | Cada explosão racha o ovo | Every blast cracks the egg | Cada explosión agrieta el huevo | Tiap ledakan bikin telur retak |
| 9,5–15,5 | Troque a arma: bumerangue | Switch weapons: boomerang | Cambia de arma: bumerán | Ganti senjata: bumerang |
| 15,5–19,1 | Ovo intacto, cliente feliz | Egg intact, happy client | Huevo intacto, cliente feliz | Telur utuh, pelanggan senang |

### 8. Dash salvando a carga (com o Ogro) · 18,6 s · bot

- **Gancho:** o Ogro telegrafa a pancada, o ladrão agarra a caixa e o courier dá dash nele.
- **Cenas:** 0–6 `c24_ogro [0,5–6,5]` · 6–13,5 `c24_fim [0–7,5]` a luta com o Ogro até ele cair · 13,5–17,1 `c24_fim [12,1–15,7]` cena da entrega ("Bom! Só 1 rachadura").

| t (s) | pt | en | es | id |
|---|---|---|---|---|
| 0–3 | O Ogro bate. O ladrão rouba. | The Ogre smashes. The thief steals. | El Ogro golpea. El ladrón roba. | Ogre menghantam. Pencuri mencuri. |
| 3–6 | Dash! A carga volta para as mãos | Dash! The cargo is back in your hands | ¡Dash! La carga vuelve a tus manos | Dash! Paket kembali ke tangan |
| 6–13,5 | Pouse, lute, pegue de novo | Set it down, fight, grab it again | Déjala, lucha, tómala otra vez | Taruh, lawan, ambil lagi |
| 13,5–17,1 | Chefe vencido. Carga entregue. | Boss down. Cargo delivered. | Jefe vencido. Carga entregada. | Bos kalah. Paket terkirim. |

### 9. O cliente reage: rachada × perfeita · 15,7 s · bot

- **Gancho:** a relíquia levando golpe (2 rachaduras aos 1,1 s e 2,9 s).
- **Cenas:** 0–4 `c05_bumerangue [6,9–10,9]` · 4–7,6 `c24_fim [12,1–15,7]` o cliente abre a caixa do Ogro: "Bom! Só 1 rachadura" · 7,6–10,6 `c01 [3,0–6,0]` luta com o ladrão protegendo a caixa · 10,6–14,2 `c01 [11,9–15,5]` o cliente comemora ("Perfeito!").

| t (s) | pt | en | es | id |
|---|---|---|---|---|
| 0–4 | Relíquia levando golpe... | The relic is taking hits... | La reliquia recibe golpes... | Relik kena pukulan... |
| 4–7,6 | O cliente abre a caixa: 1 rachadura | The client opens the box: 1 crack | El cliente abre la caja: 1 grieta | Pelanggan buka kotak: 1 retakan |
| 7,6–10,6 | Agora, protegendo a caixa... | Now, guarding the crate... | Ahora, cuidando la caja... | Sekarang, jaga kotaknya... |
| 10,6–14,2 | Perfeito! O cliente comemora | Perfect! The client cheers | ¡Perfecto! El cliente celebra | Sempurna! Pelanggan bersorak |

### 10. Contrato simples → masmorra caótica · 24,4 s · bot

O gancho mostra o caos primeiro e depois volta ao começo, porque abrir no quadro quebraria a regra dos 3 s.

- **Gancho:** o Capitão Ladrão chamando ladrões e agarrando a caixa.
- **Cenas:** 0–3 `c12_m1 [3,5–6,5]` · 3–4,5 `quadro [0,3–1,8]` · 4,5–9 `c12_r4 [0–4,5]` baú, dano e reforço · 9–13,4 `c12_r14 [0,3–4,7]` · 13,4–22,9 `c12_m1 [0,5–10,0]` o Capitão foge com a carga e o resultado.

| t (s) | pt | en | es | id |
|---|---|---|---|---|
| 0–3 | Era só levar uma caixa... | It was just one crate... | Era solo llevar una caja... | Cuma antar satu kotak... |
| 3–4,5 | Contrato: leve a caixa | Contract: deliver the crate | Contrato: lleva la caja | Kontrak: antar kotaknya |
| 4,5–13,4 | Baús, emboscadas, ladrões... | Chests, ambushes, thieves... | Cofres, emboscadas, ladrones... | Peti, sergapan, pencuri... |
| 13,4–22,9 | ...e o Capitão Ladrão chama reforços! | ...and the Thief Captain calls for backup! | ...¡y el Capitán Ladrón pide refuerzos! | ...dan Kapten Pencuri memanggil bantuan! |

### 11. O ladrão corre para a caixa pousada → dash · 16,5 s · bot

- **Gancho:** o courier com a caixa leva dano na R2. Logo depois pousa para lutar.
- **Cenas:** 0–11,4 `c02 [0,5–11,9]`: pousa aos 4,4 s, o ladrão pega aos 6,0, o dash recupera aos 6,1 e a entrega · 11,4–15 `c02 [13,0–16,6]` cena do cliente.

| t (s) | pt | en | es | id |
|---|---|---|---|---|
| 0–4,4 | Com a carga nas mãos, só dá dash | Cargo in hand: you can only dash | Con la carga en las manos, solo dash | Paket di tangan: cuma bisa dash |
| 4,4–5,8 | Parou: pousa e luta | Stop: set it down and fight | Para: déjala y lucha | Berhenti: taruh dan lawan |
| 5,8–8,6 | Ladrão na carga? Dash nele! | Thief on your cargo? Dash him! | ¿Ladrón en tu carga? ¡Dash contra él! | Pencuri ambil paket? Dash dia! |
| 8,6–11,4 | Pegue de volta e corra | Grab it back and run | Recupérala y corre | Ambil lagi dan lari |
| 11,4–15 | Entregue! | Delivered! | ¡Entregado! | Terkirim! |

### 12. "Não deixe o ovo chocar" · 22,8 s · bot

O bot do 1º contrato do ovo entrega antes de chocar. O contrato por código `JLQWHG` (região 2, ovo em 6 camadas) é longo o bastante para o ovo chocar com o bot jogando: `-codigo JLQWHG -bot` (`cod_ovo`, §2.1). Medido em 2026-10-12 no núcleo (busca por código com `Bot(sim.Seed)`): choca 150,0; entrega 163,7; cena 164,8–168,4 (chocou e o filhote chegou). O `JAAKNB` da 1ª medição passou a entregar antes de chocar.

- **Gancho:** o ovo nas mãos, o cronômetro do ovo e a luta.
- **Cenas:** 0–17,7 `cod_ovo [0,5–18,2]` + 17,7–21,3 `cod_ovo [19,3–22,9]`.

| t (s) | pt | en | es | id |
|---|---|---|---|---|
| 0–4 | O ovo choca se você demorar... | The egg hatches if you take too long... | El huevo eclosiona si tardas... | Telurnya menetas kalau kamu lama... |
| 4–9 | Chocou! O filhote agora te segue | It hatched! Now the chick follows you | ¡Eclosionó! Ahora la cría te sigue | Menetas! Anak naga mengikutimu |
| 9–17,7 | Leve o filhote até o cliente | Get the chick to the client | Lleva la cría al cliente | Antar anak naga ke pelanggan |
| 17,7–21,3 | Entregue com o filhote! | Delivered with the chick! | ¡Entregado con la cría! | Terkirim bersama anak naga! |

### 13. O mesmo vídeo com outra roupa do courier · 16,5 s · bot

Cópia exata do 11 (mesmos cortes e legendas), gravada com a roupa Noturno: `c02_roupa` = `-contract 2 -bot -roupa noturno` (a roupa não muda a partida). O resultado do A/B entre o 11 e o 13 mede a roupa sozinha.

## 4. Montagem

```text
python -I client/tools/loja_assets.py                                    # gera client/Builds/loja/logo.png (usado no CTA)
python -I client/tools/criativos.py --listar                             # roteiros, duração, estado
python -I client/tools/criativos.py --comandos --lang pt                 # comandos de gravação
python -I client/tools/criativos.py --roteiro all --lang pt --limpar     # monta e apaga os quadros usados
```

- Saída: `client/Builds/criativos/dc_NN_<slug>_<lang>.mp4`. Cada vídeo é conferido com o ffprobe (1080 × 1920, 30 fps, duração e áudio) e a linha `CRIATIVO` mostra o resultado.
- `--escala nearest` amplia 2× sem suavizar (pixel duro). O padrão é lanczos.
- Legenda: Inter SemiBold (SIL OFL, da instalação do Unity) ou `--fonte`. Corpo de 68 px, que encolhe até 44 px. Se a frase não couber em 900 px, quebra em 2 linhas. Fica a 330 px do topo (logo abaixo do HUD) ou a 1 290 px nos roteiros de porta (2 e 6), longe da coluna de botões das redes à direita.
- Precisa de ffmpeg 6.1 ou mais novo (`drawtext text_align`). O do WinGet (Gyan, 9.0) serve.
