# Dungeon Courier: página da Google Play (CONTRATO_LANCAMENTO §9.4)

Textos da ficha da loja em pt-BR, EN, ES e ID, a política de privacidade, a classificação e as 8 capturas. As artes saem do `client/tools/loja_assets.py` e os vídeos do `client/tools/criativos.py` (`docs/CRIATIVOS.md`).

**Posicionamento (GDD §7, §27 C7):** o jogo de masmorra em que a carga ocupa as suas mãos. Você escolhe quando pousar para lutar, e o cliente abre a caixa no final. **Sem energia, sem gacha**: jogue quanto quiser, nenhum item aleatório à venda e o baú mostra o que tem dentro antes de abrir.

**Regra dos textos:** só promete o que o build de lançamento tem. Os números (4 regiões, 72 contratos, 15 cargas, 8 chefes, eventos) vêm do `CONTRATO_LANCAMENTO.md` §2–§7. Confira cada um no build final antes de publicar (§8). Os termos seguem o glossário do `UI_V1.md` §3: "carga" vira cargo/carga/paket e "contrato" vira contract/contrato/kontrak. O ID trata o jogador por "kamu" e o ES por "tú".

## 1. Ficha

| Campo | Valor |
|---|---|
| App | `br.com.vstack.dungeoncourier`, gratuito |
| Categoria | Jogos > **Ação** |
| Tags (escolher as equivalentes na lista do Play Console, até 5) | Ação · Roguelike · Masmorra · Offline · Um jogador |
| Contém anúncios | sim, quando o §7 do contrato entrar (o build de hoje não tem). Nunca dentro da masmorra |
| Compras no app | sim, quando o §7 entrar: Remover anúncios, Pacote inicial, roupas e pacote de evento, sempre com conteúdo fixo e listado |
| Público-alvo | 13+ (GDD §1: 13–40). Não marcar menores de 13: isso puxa a política de Famílias, que exige SDK de anúncio certificado |
| E-mail e site de contato | `<e-mail de contato da V-STACK>` · `<site da V-STACK>` (o Vinicius preenche) |
| Política de privacidade (URL) | `<URL pública com o texto do §4>` (pode ser GitHub Pages ou o site da V-STACK) |

## 2. Classificação indicativa (questionário IARC)

| Pergunta do IARC | Resposta |
|---|---|
| Violência | sim, **fantasiosa e cartunesca**: o courier ataca morcegos, ladrões, ogros e dragões com estilingue, bumerangue e bombinha. Sem sangue, sem desmembramento, sem humano realista |
| Medo/horror | leve (cripta, esqueletos e fantasmas cartunescos) |
| Linguagem imprópria, sexo, drogas, álcool, tabaco, apostas | não |
| Compra de itens aleatórios (loot box) | **não**. Nenhum item aleatório pago (ECA Digital, Lei 15.211/2025) e baú com conteúdo visível |
| Compras digitais | sim (conteúdo fixo), quando o §7 entrar |
| Usuários interagem ou trocam conteúdo | não. O contrato por código é um texto de 6 caracteres copiado pelo próprio jogador |
| Compartilha localização | não |

**Resultado esperado** (quem decide é o questionário): ClassInd **Livre** ou **10** (violência fantasiosa), PEGI **7**, ESRB **E10+** (Fantasy Violence), USK **6**.

## 3. Textos da loja

Limites da Google Play: título até 30 caracteres, descrição curta até 80, descrição longa até 4 000 e "O que há de novo" até 500. As contagens abaixo foram medidas em caracteres Unicode, com as quebras de linha.

### 3.1 pt-BR

- **Título** (25/30): `Dungeon Courier: Entregas`
- **Descrição curta** (70/80): `Leve a carga pela masmorra e pouse para lutar. Sem energia, sem gacha.`
- **O que há de novo** (196/500): `Primeira versão! 4 regiões, 72 contratos, 15 cargas que mudam o jeito de jogar e 8 chefes. Contrato do dia, eventos da semana e contrato por código para desafiar os amigos. Sem energia, sem gacha.`
- **Palavras-chave** (para o texto e para o campo de 100 caracteres da App Store): masmorra, entrega, courier, roguelite, ação, offline, um dedo, chefe, ladrão, casual
- **Descrição longa** (2 190/4 000):

```text
Você é o courier que aceita as entregas que ninguém mais aceita. Atravesse a masmorra com a carga nas mãos, escolha as portas e chegue vivo até o cliente.

A CARGA OCUPA AS SUAS MÃOS
Carregando, você só anda e dá dash. Para lutar, pare: o courier pousa a carga e ataca sozinho o inimigo mais perto. Só que os ladrões correm direto para a carga no chão. Lutar ou passar direto? Cada sala pede uma decisão.

UM DEDO SÓ
• Arraste para andar com a carga.
• Solte o dedo para pousar e lutar.
• Toque rápido para dar dash (empurra inimigos, mas larga a carga).
• Volte a andar perto da carga para pegá-la de novo.

CADA CARGA MUDA O JOGO
• Relíquia frágil: cada golpe deixa uma rachadura e a terceira quebra.
• Ovo: se você demorar, ele choca, e o filhote também tem de chegar.
• Bolo de casamento: nada de dash, porque ele balança.
• Mensagem urgente: corre contra o relógio, mas abre portas trancadas.
• Barril de pólvora: no chão por 3 segundos, explode.
• Coração arcano, cristal de gelo, lanterna, espelho encantado, balão de gás, planta carnívora, mapa do tesouro e mais: 15 cargas, cada uma com uma regra.

ESCOLHA A PORTA
As portas mostram o perigo e a recompensa antes de você entrar. Baú ou sala tranquila? A rota é sua.

A CARGA PERDIDA FICA NA MASMORRA
Caiu no caminho? A carga fica na sala. No contrato seguinte, volte para resgatá-la e ganhe um bônus.

O CLIENTE ABRE A CAIXA
Na entrega, o cliente abre a caixa na sua frente. Inteira, ele comemora. Rachada... ele dá de ombros.

4 REGIÕES, 72 CONTRATOS, 8 CHEFES
Porões da Guilda, Minas Alagadas, Cripta Assombrada e Forja do Dragão. Melhore a guilda, troque de arma (estilingue, bumerangue ou bombinha), escolha vantagens no caminho e enfrente o Capitão Ladrão, o Ogro e o Dragão Ancião. Contrato do dia, eventos da semana e contrato por código para desafiar os amigos.

JUSTO DE VERDADE
• Sem energia: jogue quanto quiser.
• Sem gacha: nenhum item aleatório à venda, e o baú mostra o que tem dentro antes de abrir.
• Anúncios nunca dentro da masmorra; os com recompensa são opcionais.
• Compras só de conteúdo fixo, listado antes de comprar.
• Joga sem internet.

Sem conta e sem login. Em português, inglês, espanhol e indonésio.
```

### 3.2 EN

- **Title** (25/30): `Dungeon Courier: Delivery`
- **Short description** (79/80): `Carry the cargo through the dungeon. Set it down to fight. No energy, no gacha.`
- **What's new** (185/500): `First release! 4 regions, 72 contracts, 15 cargos that change how you play and 8 bosses. Daily contract, weekly events and contract codes to challenge your friends. No energy, no gacha.`
- **Keywords**: dungeon, delivery, courier, roguelite, action, offline, one finger, boss, thief, casual
- **Full description** (2 136/4 000):

```text
You are the courier who takes the jobs nobody else will. Cross the dungeon with the cargo in your hands, pick your doors and reach the client alive.

THE CARGO FILLS YOUR HANDS
While carrying, you can only walk and dash. To fight, stop: the courier sets the cargo down and attacks the nearest enemy on his own. But thieves run straight for cargo on the ground. Fight or slip past? Every room asks you to decide.

ONE FINGER
• Drag to walk with the cargo.
• Lift your finger to set it down and fight.
• Quick tap to dash (pushes enemies, but drops the cargo).
• Walk near the cargo to pick it up again.

EVERY CARGO CHANGES THE GAME
• Fragile relic: every hit leaves a crack, and the third one breaks it.
• Egg: take too long and it hatches, and the chick must arrive too.
• Wedding cake: no dash, because it wobbles.
• Urgent message: races the clock, but opens locked doors.
• Powder keg: 3 seconds on the ground and it explodes.
• Arcane heart, ice crystal, lantern, enchanted mirror, gas balloon, flytrap, treasure map and more: 15 cargos, each with its own rule.

PICK YOUR DOOR
Doors show the danger and the reward before you step in. Chest or quiet room? Your route, your call.

LOST CARGO STAYS IN THE DUNGEON
Fell on the way? The cargo stays in that room. On the next contract, go back for it and earn a rescue bonus.

THE CLIENT OPENS THE BOX
On delivery, the client opens the box right in front of you. Intact, they cheer. Cracked... they shrug.

4 REGIONS, 72 CONTRACTS, 8 BOSSES
Guild Cellars, Flooded Mines, Haunted Crypt and Dragon's Forge. Upgrade the guild, switch weapons (slingshot, boomerang or little bomb), pick perks along the way and face the Thief Captain, the Ogre and the Elder Dragon. Daily contract, weekly events and contract codes to challenge your friends.

TRULY FAIR
• No energy: play as much as you want.
• No gacha: no random items for sale, and the chest shows what's inside before you open it.
• Ads never inside the dungeon; rewarded ads are optional.
• Purchases are fixed content only, listed before you buy.
• Plays offline.

No account, no login. In English, Portuguese, Spanish and Indonesian.
```

### 3.3 ES (neutro latino-americano)

- **Título** (25/30): `Dungeon Courier: Entregas`
- **Descripción breve** (76/80): `Lleva la carga por la mazmorra y déjala para luchar. Sin energía, sin gacha.`
- **Novedades** (201/500): `¡Primera versión! 4 regiones, 72 contratos, 15 cargas que cambian tu forma de jugar y 8 jefes. Contrato del día, eventos semanales y códigos de contrato para retar a tus amigos. Sin energía, sin gacha.`
- **Palabras clave**: mazmorra, entrega, mensajero, courier, roguelite, acción, sin conexión, un dedo, jefe, casual
- **Descripción completa** (2 304/4 000):

```text
Eres el courier que acepta las entregas que nadie más acepta. Cruza la mazmorra con la carga en las manos, elige las puertas y llega vivo hasta el cliente.

LA CARGA OCUPA TUS MANOS
Mientras cargas, solo puedes caminar y hacer dash. Para luchar, detente: el courier deja la carga en el suelo y ataca solo al enemigo más cercano. Pero los ladrones corren directo hacia la carga en el suelo. ¿Luchar o pasar de largo? Cada sala te pide una decisión.

UN SOLO DEDO
• Arrastra para caminar con la carga.
• Suelta el dedo para dejarla y luchar.
• Toque rápido para hacer dash (empuja enemigos, pero suelta la carga).
• Camina cerca de la carga para tomarla otra vez.

CADA CARGA CAMBIA EL JUEGO
• Reliquia frágil: cada golpe deja una grieta y la tercera la rompe.
• Huevo: si tardas, eclosiona, y la cría también tiene que llegar.
• Pastel de bodas: nada de dash, porque se tambalea.
• Mensaje urgente: va contra reloj, pero abre puertas cerradas.
• Barril de pólvora: 3 segundos en el suelo y explota.
• Corazón arcano, cristal de hielo, linterna, espejo encantado, globo de gas, planta carnívora, mapa del tesoro y más: 15 cargas, cada una con su regla.

ELIGE LA PUERTA
Las puertas muestran el peligro y la recompensa antes de entrar. ¿Cofre o sala tranquila? La ruta es tuya.

LA CARGA PERDIDA SE QUEDA EN LA MAZMORRA
¿Caíste en el camino? La carga se queda en esa sala. En el siguiente contrato, vuelve a rescatarla y gana un bono.

EL CLIENTE ABRE LA CAJA
En la entrega, el cliente abre la caja frente a ti. Entera, lo celebra. Agrietada... se encoge de hombros.

4 REGIONES, 72 CONTRATOS, 8 JEFES
Sótanos del Gremio, Minas Inundadas, Cripta Embrujada y Forja del Dragón. Mejora el gremio, cambia de arma (honda, bumerán o bombita), elige ventajas en el camino y enfrenta al Capitán Ladrón, al Ogro y al Dragón Anciano. Contrato del día, eventos semanales y códigos de contrato para retar a tus amigos.

JUSTO DE VERDAD
• Sin energía: juega todo lo que quieras.
• Sin gacha: ningún objeto aleatorio a la venta, y el cofre muestra lo que tiene antes de abrirlo.
• Anuncios nunca dentro de la mazmorra; los que dan recompensa son opcionales.
• Compras solo de contenido fijo, listado antes de comprar.
• Se juega sin conexión.

Sin cuenta y sin inicio de sesión. En español, portugués, inglés e indonesio.
```

### 3.4 ID (Bahasa Indonesia)

- **Judul** (28/30): `Dungeon Courier: Antar Paket`
- **Deskripsi singkat** (78/80): `Bawa paket menembus dungeon, taruh untuk bertarung. Tanpa energi, tanpa gacha.`
- **Yang baru** (184/500): `Rilis pertama! 4 wilayah, 72 kontrak, 15 paket yang mengubah cara bermain, dan 8 bos. Kontrak harian, event mingguan, dan kode kontrak untuk menantang teman. Tanpa energi, tanpa gacha.`
- **Kata kunci**: dungeon, kurir, antar paket, roguelite, aksi, offline, satu jari, bos, pencuri, kasual
- **Deskripsi lengkap** (2 296/4 000):

```text
Kamu adalah kurir yang menerima antaran yang tak berani diambil orang lain. Tembus dungeon dengan paket di tangan, pilih pintumu, dan sampai ke pelanggan dengan selamat.

PAKET MENGISI TANGANMU
Saat membawa paket, kamu cuma bisa berjalan dan dash. Untuk bertarung, berhenti: kurir menaruh paket dan menyerang musuh terdekat secara otomatis. Tapi pencuri langsung lari ke paket di lantai. Bertarung atau lewat saja? Tiap ruangan minta keputusan.

CUKUP SATU JARI
• Geser untuk berjalan membawa paket.
• Angkat jari untuk menaruh paket dan bertarung.
• Ketuk cepat untuk dash (mendorong musuh, tapi paket terjatuh).
• Berjalan dekat paket untuk mengambilnya lagi.

TIAP PAKET MENGUBAH PERMAINAN
• Relik rapuh: tiap pukulan bikin retakan, dan retakan ketiga memecahkannya.
• Telur: kalau kamu lama, telurnya menetas, dan anak naga juga harus sampai.
• Kue pengantin: tidak bisa dash, karena kuenya goyang.
• Pesan mendesak: berpacu dengan waktu, tapi membuka pintu terkunci.
• Tong mesiu: 3 detik di lantai, lalu meledak.
• Jantung arkana, kristal es, lentera, cermin ajaib, balon gas, tanaman karnivora, peta harta karun, dan lainnya: 15 paket, masing-masing dengan aturannya.

PILIH PINTUMU
Pintu menunjukkan bahaya dan hadiah sebelum kamu masuk. Peti atau ruangan tenang? Rutenya pilihanmu.

PAKET YANG HILANG TETAP DI DUNGEON
Tumbang di jalan? Paket tertinggal di ruangan itu. Di kontrak berikutnya, kembali untuk menyelamatkannya dan dapatkan bonus.

PELANGGAN MEMBUKA KOTAK
Saat pengantaran, pelanggan membuka kotak di depanmu. Utuh, dia bersorak. Retak... dia angkat bahu.

4 WILAYAH, 72 KONTRAK, 8 BOS
Ruang Bawah Guild, Tambang Banjir, Kripta Berhantu, dan Tempaan Naga. Tingkatkan guild, ganti senjata (ketapel, bumerang, atau bom kecil), pilih perk di jalan, dan hadapi Kapten Pencuri, Ogre, dan Naga Purba. Kontrak harian, event mingguan, dan kode kontrak untuk menantang teman.

BENAR-BENAR ADIL
• Tanpa energi: main sepuasnya.
• Tanpa gacha: tidak ada item acak yang dijual, dan peti menunjukkan isinya sebelum dibuka.
• Iklan tidak pernah muncul di dalam dungeon; iklan berhadiah itu opsional.
• Pembelian hanya untuk konten tetap yang ditampilkan sebelum membeli.
• Bisa dimainkan offline.

Tanpa akun dan tanpa login. Tersedia dalam bahasa Indonesia, Inggris, Portugis, dan Spanyol.
```

## 4. Política de privacidade (texto simples)

Coerente com o jogo de hoje (`Textos.Privacidade`, `UI_V1.md` §4): o progresso e o diário ficam no aparelho, e **nada sai dele sem o jogador tocar em Exportar diário**. Publicar numa URL pública e colar a URL na ficha (§1).

### 4.1 pt-BR

> **Política de privacidade do Dungeon Courier (V-STACK)**. Atualizada em `<data da publicação>`.
>
> 1. **O que fica no seu aparelho.** O seu progresso (contratos, moedas, melhorias e ajustes) e o diário do jogo ficam só neste aparelho. O diário é um arquivo com eventos de jogo, como "contrato começou" ou "carga entregue", a versão do app, a faixa de memória do aparelho e um código anônimo criado na primeira abertura.
> 2. **O que sai do aparelho.** Nada, a menos que você toque em Configurações > Exportar diário e escolha para onde mandar o arquivo. O jogo não tem conta nem login e não coleta nome, e-mail, contatos, fotos, localização nem o modelo do aparelho.
> 3. **Internet.** O jogo funciona sem internet e não envia dados para servidores da V-STACK.
> 4. **Crianças e adolescentes.** O jogo não pede dados pessoais de ninguém e não vende itens aleatórios.
> 5. **Como apagar.** Desinstalar o jogo apaga o progresso e o diário. O código anônimo também some.
> 6. **Contato.** `<e-mail de contato da V-STACK>`

### 4.2 EN

> **Dungeon Courier Privacy Policy (V-STACK)**. Last updated `<publication date>`.
>
> 1. **What stays on your device.** Your progress (contracts, coins, upgrades and settings) and the game log stay only on this device. The log is a file with game events, such as "contract started" or "cargo delivered", the app version, the device's memory range and an anonymous code created on first launch.
> 2. **What leaves your device.** Nothing, unless you tap Settings > Export log and choose where to send the file. The game has no account or login and does not collect your name, email, contacts, photos, location or device model.
> 3. **Internet.** The game works offline and sends no data to V-STACK servers.
> 4. **Children and teens.** The game asks no one for personal data and sells no random items.
> 5. **How to delete.** Uninstalling the game deletes the progress and the log, including the anonymous code.
> 6. **Contact.** `<V-STACK contact email>`

### 4.3 ES

> **Política de privacidad de Dungeon Courier (V-STACK)**. Actualizada el `<fecha de publicación>`.
>
> 1. **Lo que queda en tu dispositivo.** Tu progreso (contratos, monedas, mejoras y ajustes) y el registro del juego quedan solo en este dispositivo. El registro es un archivo con eventos del juego, como "contrato iniciado" o "carga entregada", la versión de la app, el rango de memoria del dispositivo y un código anónimo creado en la primera apertura.
> 2. **Lo que sale del dispositivo.** Nada, a menos que toques Ajustes > Exportar registro y elijas adónde enviar el archivo. El juego no tiene cuenta ni inicio de sesión y no recopila nombre, correo, contactos, fotos, ubicación ni el modelo del dispositivo.
> 3. **Internet.** El juego funciona sin conexión y no envía datos a servidores de V-STACK.
> 4. **Niños y adolescentes.** El juego no pide datos personales a nadie y no vende objetos aleatorios.
> 5. **Cómo borrar.** Desinstalar el juego borra el progreso y el registro, incluido el código anónimo.
> 6. **Contacto.** `<correo de contacto de V-STACK>`

### 4.4 ID

> **Kebijakan Privasi Dungeon Courier (V-STACK)**. Diperbarui `<tanggal terbit>`.
>
> 1. **Yang tersimpan di perangkatmu.** Progresmu (kontrak, koin, peningkatan, dan pengaturan) dan catatan game hanya ada di perangkat ini. Catatan itu berisi kejadian game, seperti "kontrak dimulai" atau "paket terkirim", versi aplikasi, rentang memori perangkat, dan kode anonim yang dibuat saat pertama dibuka.
> 2. **Yang keluar dari perangkat.** Tidak ada, kecuali kamu ketuk Pengaturan > Ekspor catatan dan memilih ke mana file dikirim. Game ini tidak punya akun atau login dan tidak mengumpulkan nama, email, kontak, foto, lokasi, atau model perangkat.
> 3. **Internet.** Game ini bisa dimainkan offline dan tidak mengirim data ke server V-STACK.
> 4. **Anak dan remaja.** Game ini tidak meminta data pribadi siapa pun dan tidak menjual item acak.
> 5. **Cara menghapus.** Menghapus instalan game akan menghapus progres dan catatan, termasuk kode anonim.
> 6. **Kontak.** `<email kontak V-STACK>`

### 4.5 Quando os anúncios e as compras entrarem (CONTRATO_LANCAMENTO §7 e §11)

O SDK de anúncio usa a internet e coleta dados por conta própria. **Antes de publicar o build com anúncios**, a política ganha dois itens (nos 4 idiomas), a frase 3 muda para "o jogo não envia dados para servidores da V-STACK; os anúncios usam a internet" e o formulário de segurança dos dados muda junto:

> 7. **Anúncios.** Os anúncios são exibidos pelo Unity LevelPlay e pelas redes que ele usa (Unity Ads e, se ligada, AdMob). Eles podem coletar o identificador de publicidade do aparelho, o endereço IP e dados de interação com o anúncio, conforme a política de cada um (`<links>`). Você pode redefinir ou desativar o identificador de publicidade nas configurações do Android.
> 8. **Compras.** As compras são processadas pela Google Play. A V-STACK recebe só a confirmação da compra, sem dados de pagamento.

**O build de hoje tem só o Unity IAP** (`docs/MONETIZACAO.md` §1.3, medido no APK). O manifesto ganha só `com.android.vending.BILLING` e `ACCESS_NETWORK_STATE`, esta do `datatransport`, a telemetria do Google Play Billing (permissão normal, sem dado do jogador). O `INTERNET` já existia por causa do `config_url`. **Não há SDK de anúncio no APK, então não há `AD_ID`**, nem as permissões do Privacy Sandbox e do WorkManager. O LevelPlay está pronto no código e desligado (define `DC_LEVELPLAY`). Quando ele voltar (`MONETIZACAO.md` §4), entram o item 7 da política, a frase 3 nova, `AD_ID`, `ACCESS_ADSERVICES_*`, `WAKE_LOCK`, `RECEIVE_BOOT_COMPLETED` e `FOREGROUND_SERVICE` (Unity Ads), e a declaração de ID de publicidade passa a ser **sim**.

**Segurança dos dados (Play Console):**

| Build | Coleta | Compartilha | Observação |
|---|---|---|---|
| Hoje (só Unity IAP na loja falsa, simulador de anúncio) | nenhum dado | nenhum | o diário só sai por ação do jogador (exportação feita pelo usuário, que não conta como coleta). Contém anúncios: não. ID de publicidade: **não** (sem `AD_ID` no manifesto) |
| Com compras reais (Google Play, `iap_google_play: true`) | histórico de compras, processado pela Google Play | nenhum | item 8 da política |
| Com anúncios (LevelPlay religado, com app key) | identificadores do dispositivo (ID de publicidade), interações com o app e localização aproximada (pelo IP), coletados pelo SDK de anúncio | com o LevelPlay e as redes de anúncio (publicidade, análise e prevenção de fraude) | item 7 da política e ID de publicidade: sim. Criptografia em trânsito: a dos SDKs; pedido de exclusão: desinstalar + o canal da rede. Confira a tabela de segurança dos dados publicada pelo LevelPlay e pelo Unity Ads na versão do SDK do build |

## 5. As 8 capturas (1080 × 1920)

Tire as fotos no **build de release** (`client/Builds/win-release/`), que não tem a marca "Development Build" no canto, com `-lang L`. As 6 de partida saem de uma gravação `-record` a 10 quadros/s (o passo do bot é fixo, então o quadro N é o mesmo nos 4 idiomas; só o texto muda). Copie o quadro para `client/Builds/loja_fotos/<foto>_<L>.png`. As 2 telas saem com `-shot`. Depois rode `python -I client/tools/loja_assets.py`, que amplia cada foto de 540 × 960 para 1080 × 1920 (lanczos) e grava em `client/Builds/loja/capturas/NN_<nome>_<L>.png`. Sem a foto do idioma, ele usa a `_pt` e avisa.

Base de todo comando: `client/Builds/win-release/DungeonCourier.exe -screen-width 540 -screen-height 960 -screen-fullscreen 0 -testsession -lang L`. Gravação: `... <flags> -record <pasta> -recordfps 10 -recordsecs <s> [-recordfrom <s>]`.

| # | Captura (ordem da loja) | O que mostra | Flags (depois da base) | Foto = quadro |
|---|---|---|---|---|
| 01 | `01_ladrao_quer_a_carga` | Capitão Ladrão chamando ladrões (fios lilás) e o aviso "ladrão! ele quer a sua carga" | `-contract 12 -bot`, `-recordfrom 44 -recordsecs 6` | `c12_chamando` = 35 |
| 02 | `02_pousar_para_lutar` | 1ª sala: "solte o dedo para pousar e lutar", portas com caveiras e baú | `-contract 1 -bot`, `-recordsecs 3` | `c1_ambiente` = 20 |
| 03 | `03_cliente_abre_a_caixa` | cena da entrega: o cliente abre a caixa e comemora ("Perfeito!") | `-contract 1 -bot`, `-recordfrom 21 -recordsecs 5` | `c1_fim_24` = 28 |
| 04 | `04_reliquia_fragil` | relíquia entre arqueiros, linha de mira | `-contract 8 -bot`, `-recordfrom 6 -recordsecs 3` | `c8_rachaduras` = 10 |
| 05 | `05_cristal_de_gelo` | Minas Alagadas: cristal de gelo (barra "gelo" no HUD) e o ladrão chegando | `-contract 27 -bot`, `-recordsecs 3` | `c27_gelo` = 25 |
| 06 | `06_dragao_anciao` | Dragão Ancião com a barra de chefe e o cone do sopro | `-contract 72 -room B4 -bot -weapon 1 -guilda`, `-recordsecs 9` | `b4_dragao` = 80 |
| 07 | `07_quadro_de_contratos` | quadro de contratos (72 na campanha) | `-dev-desbloqueia -tela quadro -shot <fotos>/tela_quadro_L.png -shotdelay 1.5` | — |
| 08 | `08_guilda_sem_energia` | guilda: melhorias compradas com moedas do jogo, sem energia | `-dev-desbloqueia -tela guilda -shot <fotos>/tela_guilda_L.png -shotdelay 1.5` | — |

- Quadros escolhidos em 2026-10-12 com o build da `feat/leva-c` (1.2.0-beta). Se o Core mudar, grave o pt de novo e confira o quadro antes dos outros idiomas.
- O logo do CTA e do feature graphic sai de `tela_titulo.png` (`-dev-desbloqueia -tela titulo -shot`).
- O ícone (512 × 512) e o feature graphic (1024 × 500) saem do mesmo script (`icone_512.png`, `feature_1024x500.png`), só com sprites e fotos do jogo. O pacote da loja vai como anexo da pré-release no GitHub.

## 6. Artes (resumo)

| Arquivo | Tamanho | Fonte | Limite da Play |
|---|---|---|---|
| `icone_512.png` | 512 × 512, sem transparência | courier com a caixa (parado, de costas para NW: a caixa aparece inteira) sobre o piso do jogo, contorno limão na carga | PNG 32 bits, até 1 MB |
| `feature_1024x500.png` | 1024 × 500 | logo da tela de título + Ogro, ladrão, morcego e courier com a caixa sobre o piso | JPG ou PNG 24 bits, sem texto pequeno |
| `capturas/*.png` | 1080 × 1920 | fotos `-shot` ampliadas | 2 a 8 por tipo de aparelho, 9:16 |

## 7. Vídeo da ficha (opcional)

O vídeo do YouTube da ficha pode ser o criativo 11 (`dc_11_ladrao_e_dash_<L>.mp4`) ou o 10. A Play aceita 9:16. Publicar como "não listado" no canal da V-STACK.

## 8. Antes de publicar (checklist)

- [ ] Conferir no build final cada número da descrição (4 regiões, 72 contratos, 15 cargas, 8 chefes, eventos semanais, contrato por código, 4 idiomas).
- [ ] Anúncios e compras: se o build tiver SDK de anúncio ou de compra, aplicar o §4.5 na política e no formulário de segurança dos dados, e marcar "Contém anúncios".
- [ ] URL pública da política e e-mail de contato preenchidos (§1, dependência do Vinicius).
- [ ] 8 capturas por idioma, tiradas com `-lang` no build de release (sem "Development Build").
- [ ] Questionário IARC respondido como no §2.
- [ ] AAB assinado com a chave de upload (UI_V1 §5) e Play App Signing.
