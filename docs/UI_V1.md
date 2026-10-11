# Dungeon Courier — UI/UX da v1.0-beta (CONTRATO_V1 §1, §10, §11, §14)

Código: `client/Assets/_DC/Scripts/View/` — `Hud.cs` (estrutura, faixa do contrato, oferta, pausa, cena, resultado), `Menus.cs` (título, menu, quadro, código, guilda/armas/equipamento, perk, configurações, créditos), `MetaTelas.cs` (mapa, modificadores, eventos, temporada, marcos e maestria, sem fim, roupas, som de meta; §7), `Loja.cs` (`ILoja` + loja falsa), `Anuncios.cs` (`IAnuncios` + simulador, ofertas de rewarded, interstitial), `ConfigLocal.cs` (remote config), `Textos.cs` (pt-BR, EN, ES e ID; §3), `Art.cs` (ícones, `Ajustes`), `Exportar.cs` (diário e código para fora do aparelho), `Game.cs` (fluxo, diário, áudio por evento, sessão). uGUI por código, referência 1080 × 1920 casando a largura, dentro da área segura.

## 1. Fluxo

```
Título ──toque──> Menu ──JOGAR──> Quadro ──contrato/dia/raro/código──> [Modificadores] ──> [Oferta de resgate] ──> Contrato
                   │  [Baú do dia]   │  ├─(título) Mapa das regiões                                                 │  (Perk 1 de 3)
                   ├─GUILDA (Melhorias | Armas | Equipamento)  ├─EVENTOS ──vaga──> Contrato   (Pausa: Continuar, Rota segura,
                   ├─LOJA ──> Loja de teste (confirmar)        └─SEM FIM ──JOGAR──> Contrato    Configurações, Desistir)
                   ├─TEMPORADA · MARCOS (Marcos | Maestria)                                                         v
                   ├─ROUPAS ──LOJA──> Loja          Quadro <──CONTINUAR [interstitial]── Resultado <── Cena da entrega
                   ├─CONFIGURAÇÕES                                     ├─RETOMAR DA SALA / DOBRAR BÔNUS (anúncio de teste)
                   └─CRÉDITOS                                          └─GUILDA (atalho, se a guilda já abriu)
```

| Tela | Voltar (Android / Esc) | Música |
|---|---|---|
| Título, Menu | grava e sai | `menu` |
| Quadro | Menu | `menu` |
| Guilda, Código, Oferta, Configurações, Créditos, Mapa, Modificadores, Sem fim, Marcos, Roupas, Loja | fecha a janela | a da tela de baixo |
| Quadro de eventos, Quadro de temporada | fecha a janela | `evento` (fechou: `menu`) |
| Loja de teste (confirmar) | cancela a compra | — |
| Anúncio de teste | recompensado: fecha (antes do fim = sem recompensa); interstitial: só depois dos 5 s | — (som `anuncio`) |
| Contrato | abre a Pausa | `masmorra` da região (M/B: `chefe` da região) |
| Pausa | fecha (na confirmação de desistir: volta à pausa) | — |
| Perk | não fecha (escolha obrigatória; o Core para o tempo) | — |
| Cena da entrega | pula para o Resultado | silêncio + jingle |
| Resultado | Quadro (com o interstitial, se o Core deixar) | silêncio |

## 2. Regras de tela

- **Alvo de toque:** todo botão tem pelo menos 144 px de referência de altura e de largura, o que dá 48 dp num aparelho de 360 dp de largura (o canvas casa a largura). Aparelho mais largo em dp ganha alvo maior. A pausa da faixa de cima tem área de toque estendida (`Art.Toque`). O volume usa botões `-`/`+` em vez de slider.
- **Estado nunca só por cor:** contrato fechado tem cadeado e fica apagado. Estrela ganha é cheia e a não ganha é vazada. Arma equipada tem visto, aba ativa tem sublinhado, idioma ativo tem visto, e o contrato do dia tem faixa e estrela.
- **Contraste:** texto de tinta `#1B1724` sobre papel, botões verde, dourado e papel-escuro passa de 7:1. Erro escrito no papel usa `Art.BadInk` (5,8:1), porque o `#FF3B30` dá só 2,8:1. Texto apagado `#8A90A8` sobre o fundo dá 5,7:1.
- **Texto:** corpo de 28–36 px de referência, com mínimo de 18 px no encolhimento automático. Fonte embutida do Unity (cobre acentos). Nenhum caractere fora do Latin-1 e nenhum emoji: todo símbolo é sprite do `Art`.
- **Sem alocação por quadro:** a faixa do contrato só reescreve texto quando o valor muda. As dicas são literais (`Textos.Dica`).

## 3. Localização (pt-BR, EN, ES, ID)

Quatro idiomas (CONTRATO_LANCAMENTO §9.2, geos do teste BR, PH e ID): português do Brasil, inglês, espanhol neutro latino-americano e Bahasa Indonesia. Tabela única em `Textos.cs`.

**Escolha do idioma.** `Ajustes.Idioma` (enum `Textos.Lingua`: `Pt`, `En`, `Es`, `Id`) fica no PlayerPrefs `dc_idioma` com o código `pt`, `en`, `es` ou `id`. Sem escolha gravada, vale o idioma do aparelho (`Application.systemLanguage`): Portuguese dá pt, Spanish dá es, Indonesian dá id, e o resto dá en. A tela de Configurações mostra os 4 em grade 2×2, cada um no próprio idioma ("Português", "English", "Español", "Bahasa Indonesia"), com visto e cor no ativo. A troca vale na hora: `Hud.Relocalizar` relê todo texto fixo registrado em `Loc` e redesenha a tela aberta. O diário registra `settings,idioma,<código>`.

**Como acrescentar texto (regra para todas as raias):**

1. Todo texto que o jogador vê mora em `Textos.cs`. Não escreva frase em `Hud.cs`, `Menus.cs` ou `WorldView*.cs`; chame uma propriedade ou um método de `Textos`.
2. Texto fixo: `L("pt", "en", "es", "id")` com a frase inteira em cada idioma. Texto com número ou nome: `F("pt {0}", "en {0}", "es {0}", "id {0}", valor)`, com até 3 argumentos. Nada de montar frase com pedaços, porque a ordem das palavras muda entre os idiomas. Se faltar `es` ou `id` (`null` ou chamada antiga `L(pt, en)`), o jogo mostra o EN.
3. Conteúdo do Core (cargas, inimigos, perks, armas, dicas, salas) é traduzido na View pelo enum ou pelo código da sala. O pt vem do próprio Core (`CargoDef.Name`, `EnemyDef.Name`, `PerkDef`, `WeaponDef`, `Hints.Text`, `RoomDef.Name`): `case X: return L(pt, "en", "es", "id");`. O Core não muda. Enum novo sem linha no `switch` aparece em pt até ganhar tradução.
4. Tela nova registra o texto com `Loc(texto, () => Textos.Algo)` ou `Btn(..., () => Textos.Algo, ...)`, para a troca de idioma alcançar. Texto que muda com o estado é reescrito no `Refresh*` da tela, que o `Relocalizar` já chama.
5. Caixa de texto que pode receber frase longa usa `Shrink(t, min)` ou `resizeTextForBestFit`, com mínimo de 18 px (16 px em linhas de apoio). Botão do `Art.NewButton` já encolhe até a metade. Conte com o ES até ~25% mais longo que o EN e o ID até ~30% mais longo. Se não couber nem no mínimo, encurte a tradução. Palavra única num botão estreito (`NAIKKAN`, `SELAMATKAN`) precisa caber inteira na largura: o uGUI corta a palavra no meio em vez de encolher.
6. Rótulo que flutua no mundo (`WorldView.Float`) sai centrado no courier e pode encostar na borda da tela perto da parede: no máximo o comprimento do pt.
7. Só caracteres Latin-1 (a fonte embutida cobre á, ñ, ¡, ¿, ª, º, ×, ·). Nada de emoji, aspas curvas ou travessão longo. Ícone é sprite do `Art`.
8. Antes de entregar, fotografe as telas novas em 540×960 nos 4 idiomas com `-lang` (§6).

**Tom.** Casual, curto e direto, no imperativo e tratando o jogador por você: "tú" no ES (sem "vos" nem "usted") e "kamu" no ID. Números e unidades: vírgula decimal em pt, es e id (`0,6 s`), ponto em en. Segundos são "s" em pt, en e es, e "dtk" no texto corrido do ID. O relógio da HUD usa "s" em todos (`Textos.Segundos`), por falta de espaço. O ES usa "celular" e não "móvil".

**Glossário** (use sempre o mesmo termo em todas as telas):

| pt-BR | EN | ES | ID |
|---|---|---|---|
| carga | cargo | carga | paket |
| contrato | contract | contrato | kontrak |
| contrato do dia | daily contract | contrato del día | kontrak harian |
| guilda | guild | gremio | guild |
| vantagem / perk | perk | ventaja | perk |
| baú | chest | cofre | peti |
| resgate / resgatar | rescue | rescate / rescatar | penyelamatan / selamatkan |
| moedas | coins | monedas | koin |
| estrelas | stars | estrellas | bintang |
| entrega / entregue | delivery / delivered | entrega / entregado | pengantaran / terkirim |
| cliente | client | cliente | pelanggan |
| courier | courier | courier | kurir |
| pousar (a carga) | set down | dejar (la carga) | menaruh / taruh |
| rachadura | crack | grieta | retakan |
| dash | dash | dash | dash |
| sala | room | sala | ruang / ruangan |
| porta | door | puerta | pintu |
| chefe / mini-chefe | boss / mini-boss | jefe / minijefe | bos / mini-bos |
| masmorra | dungeon | mazmorra | dungeon |
| ladrão | thief | ladrón | pencuri |
| filhote (dragãozinho do ovo) | chick | cría | anak naga |
| vida | health | vida | nyawa |
| melhorias / melhorar | upgrades / upgrade | mejoras / mejorar | peningkatan / naikkan |
| armas / equipar | weapons / equip | armas / equipar | senjata / pakai |
| diário (CSV do teste) | log | registro | catatan |
| configurações | settings | ajustes | pengaturan |
| desistir | abandon | abandonar | batalkan |
| pagamento | payment | pago | bayaran |
| tempo-alvo | target time | a tiempo | tepat waktu |
| camada (do contrato) / piso (sem fim) | floor | piso | lantai |
| projeto (de equipamento) | blueprint | plano | cetak biru |
| equipamento (lateral) | gear | equipo | perlengkapan |
| modificador | modifier | modificador | pengubah |
| contrato raro | rare contract | contrato raro | kontrak langka |
| evento / vaga | event / slot | evento / puesto | event / slot |
| temporada | season | temporada | musim |
| marcos / maestria | milestones / mastery | logros / maestría | pencapaian / keahlian |
| desafio sem fim | endless challenge | desafío sin fin | tantangan tanpa akhir |
| roupa | outfit | traje | pakaian |
| loja / comprar | shop / buy | tienda / comprar | toko / beli |
| anúncio (de teste) | (test) ad | anuncio (de prueba) | iklan (uji) |
| trocar (reroll) | swap | cambiar | ganti |
| coletar (recompensa) | claim | reclamar | ambil |

Regiões: Porões da Guilda / Guild Cellars / Sótanos del Gremio / Ruang Bawah Guild · Minas Alagadas / Flooded Mines / Minas Inundadas / Tambang Banjir · Cripta Assombrada / Haunted Crypt / Cripta Embrujada / Kripta Berhantu · Forja do Dragão / Dragon's Forge / Forja del Dragón / Tempaan Naga.

- O diário grava códigos crus (`DIA`, `C12`, nomes de evento, `pt`/`en`/`es`/`id`) e nunca texto traduzido.

## 4. Telemetria e privacidade (§10)

- `persistentDataPath/diario.csv` tem as colunas `utc,instalacao,versao,sessao,t_sessao,t_contrato,evento,a,b,contrato,sala`. `instalacao` é um GUID gerado na primeira abertura (PlayerPrefs `dc_instalacao`), sem dado pessoal. O `-reset` não apaga esse GUID. Um diário com o cabeçalho da v0.1 é renomeado para `diario_anterior.csv`.
- Os eventos do Core vão para o diário sozinhos (`SimEvent.DiaryName`, incluindo `contract_stars`, `perk_pick`, `weapon_equip`, `daily_start/end` e `code_start`). A View acrescenta `session_start`, `settings`, `share_code` e `diary_export`.
- **Exportar diário** (Configurações): no Android 10+, o CSV é copiado para `Download/DungeonCourier` (MediaStore) e vai pelo seletor de apps (`ACTION_SEND`, `EXTRA_STREAM`). No Android 8–9, vai como texto (`EXTRA_TEXT`, no máximo os últimos 400 KB). No Windows, o Explorer abre com o arquivo selecionado. Nada sai do aparelho sem esse toque. O aviso curto aparece no título e o completo nas Configurações e nos Créditos.

## 5. Builds (§14)

- `Setup.Version = "1.0.0-beta"`. O `versionCode` é gerado da versão: `(maior·10000 + menor·100 + patch)·10 + estágio`, com alpha 1, beta 2, rc 5 e final 9, o que dá 100002. Nunca reutilizar o número.
- `DC.EditorTools.Setup.BuildAndroidRelease` gera `Builds/android/DungeonCourier-1.0.0-beta.apk`: não-development, ARM64, IL2CPP Release, APK (não AAB) e chave de debug do Unity. Os APKs anteriores são apagados antes. `BuildWindowsRelease` gera `Builds/win-release/`. Os builds dev (`BuildWindows`, `BuildAndroidDev`) continuam como estavam.
- **A Play exige chave própria e AAB:** keystore de upload fora do repositório, senha por variável de ambiente lida no build e Play App Signing. Entra quando o jogo for para a loja.

## 6. Fotos sem clicar (dev)

`-testsession -tela titulo|menu|quadro|codigo|guilda|armas|config|creditos|perk -shot f.png`. O resultado precisa de contrato: use `-contract K -bot`. A pausa sai com `-contract K -pausa` (com a oferta ROTA SEGURA quando a sala tem 2+ portas).

Telas do lançamento (§7), de preferência com `-dev-desbloqueia`:

| `-tela` | O que mostra |
|---|---|
| `mapa` | quadro + mapa das 4 regiões (destrave por estrelas e chefe, progresso por região) |
| `raro` | quadro com o contrato do dia e o contrato raro (com `-dev-desbloqueia` o raro é oferecido), TROCAR com preço |
| `modificadores` | escolha de 0–2 modificadores antes do próximo contrato, com o pagamento extra |
| `eventos` | quadro de eventos da semana: 3 vagas (+2 com o pacote), recompensa fixa, projeto da semana |
| `semfim` | desafio sem fim: regras, recorde, recorde da semana, código da semana, moedas do dia |
| `temporada` | quadro de temporada: 20 objetivos com barra e recompensa fixa |
| `equipamento` | guilda na aba Equipamento: 4 itens, projetos 0/3, níveis, equipar |
| `marcos` / `maestria` | marcos de estrelas / maestria das 15 cargas |
| `roupas` | as 6 roupas (tinta procedural até existir a folha `courier_<roupa>`) |
| `loja` / `compra` | loja de teste / confirmação da loja falsa (Pacote inicial) |
| `anuncio` / `intersticial` | simulador de anúncio recompensado (baú do dia) / interstitial, com a contagem de 5 s |
| `oferta` / `bonus` | resultado de exemplo com RETOMAR DA SALA (falha) / DOBRAR BÔNUS (entrega) |
| `bau` | menu com o baú do dia |

`-dev-desbloqueia` (só com `-testsession`, nada é gravado): campanha inteira aberta, estrelas cheias, +5000 moedas, 2ª sessão (o "retomar" do rewarded nunca aparece na 1ª) e o último instante ativo 9 h atrás, para o `OpenSession` oferecer o contrato raro. Não dá projetos (o Core não tem atalho de teste): o equipamento aparece com 0/3.

`-lang pt|en|es|id` (só com `-testsession`) força o idioma na memória, sem gravar a escolha do jogador. Exemplo: `-testsession -lang id -tela config -shot config_id.png`.

## 7. Telas do lançamento (CONTRATO_LANCAMENTO §6–§7)

Todas as listas novas usam o mesmo **Painel** (`MetaTelas.cs`): cartão com título, cabeçalho, lista rolável de linhas de 176 px (48 dp), rodapé e VOLTAR (+ botão principal). A **linha inteira** é o alvo de toque; o selo da direita diz a ação (JOGAR, COLETAR, EQUIPAR, COMPRAR) ou o estado (cadeado = fechado e apagado; visto = feito/equipado/escolhido; "3/5" = progresso). Nada só por cor.

- **Menu:** baú do dia no topo (cartão com o conteúdo escrito antes de assistir: moedas + projeto, se o equipamento estiver aberto), JOGAR e a grade GUILDA | LOJA, TEMPORADA | MARCOS, ROUPAS | CONFIGURAÇÕES, CRÉDITOS. LOJA pulsa quando o Pacote inicial acabou de ser oferecido (depois do C3), até abrir a loja; GUILDA pulsa também com nível de equipamento que cabe no bolso. "A guilda abre depois do 1º contrato" foi para a linha de aviso.
- **Quadro:** o título é a região mostrada (toque abre o **Mapa**); a lista só traz os contratos dessa região, com selo de maestria da carga. Cartão do **contrato do dia** (`DailyFor`: região sorteada entre as abertas, com o modificador do dia quando os modificadores abrem) e do **contrato raro** (×2; sem raro, "volte depois de 8 h fora"), cada um com **TROCAR** (1×/dia, preço em moedas). Botões EVENTOS, SEM FIM e DIGITAR CÓDIGO (cadeado enquanto fechados).
- **Mapa das regiões:** 4 linhas; aberta = progresso (contratos e estrelas) e VER/NO QUADRO; fechada = cadeado e "abre com N estrelas e vencendo: chefe"; sem conteúdo = "em breve".
- **Modificadores:** antes de contrato da campanha ou por código, com 40 estrelas: 5 linhas com "+%", até 2 escolhidos (o 3º fica com cadeado), rodapé "Pagamento extra: +X%", JOGAR. Raro, evento, sem fim e dia não escolhem (o dia já vem com 1).
- **Quadro de eventos:** cabeçalho com o evento da semana, a regra e os dias que faltam; vagas (3, +2 com o pacote) com região, camadas, carga e recompensa fixa; rodapé com o projeto da semana; PACOTE DE EVENTO leva à loja. Sem resgate em evento.
- **Quadro de temporada:** 28 dias, 20 objetivos com barra, recompensa fixa (moedas, projeto ou roupa) e COLETAR. Sem passe pago.
- **Marcos e maestria:** abas MARCOS (10/30/60/95/150 estrelas, moedas e a roupa Fantasma, COLETAR) e MAESTRIA (15 cargas, nível 0–5 em pontos, entregas até o próximo nível, +2% por nível).
- **Desafio sem fim:** regras, recorde, recorde da semana, moedas que ainda rendem hoje e o código da semana; JOGAR com cadeado até o último chefe. Na faixa do contrato, "piso N · recorde M" no lugar das bolinhas das camadas.
- **Guilda > Equipamento:** 4 linhas: nome, efeito, projetos 0/3 (travado) ou níveis 1–5, EQUIPAR (tocar no equipado tira) e MELHORAR com preço, lado a lado (~188 × 220 px cada).
- **Roupas:** 6 linhas: a Clássica (grátis), 4 à venda (preço "(teste)", selo LOJA) e a Fantasma (temporada/marco, não se compra). Retrato = folha `courier_<roupa>` (`Hud.FolhaRoupa`: `courier_noturno`, `courier_real`, `courier_explorador`, `courier_forja`, `courier_fantasma`) e, enquanto ela não existe, o `courier` com a tinta `Hud.TintaRoupa`. **No mundo, a WorldView (raia K2) precisa usar o mesmo par.**
- **Loja:** Remover anúncios, Pacote inicial (só depois do C3), as 6 roupas e o pacote de evento; cada linha lista o conteúdo fixo e o preço "R$ x,xx (teste)". COMPRAR abre a loja falsa ("Comprar X por R$ x,xx (teste)? Compra de teste: nada é cobrado.", CONFIRMAR/CANCELAR); confirmada, o Core concede (`sim.Purchase`).
- **Anúncio de teste:** tela cheia "ANÚNCIO DE TESTE", "simulador no PC: nenhum anúncio real", o que se ganha, anel e contagem de 5 s. Recompensado: PULAR (sem recompensa) até o fim, depois FECHAR concede. Interstitial: FECHAR só depois dos 5 s.
- **Ofertas de rewarded** (o Core decide quando; cada concessão tem id `rw-<contrato>-<tipo>` no save): **RETOMAR DA SALA** no resultado de falha (nunca na 1ª sessão; nota "Proteja a carga e retome"), **DOBRAR BÔNUS +N** no resultado de entrega com bônus, **ROTA SEGURA** na pausa (a dica da faixa passa a dizer "rota segura: a porta da esquerda/do meio/da direita"; **o destaque da porta no mundo é da K2**), **baú do dia** no menu.
- **Interstitial:** só no CONTINUAR do resultado, quando `sim.InterstitialDue` deixar (nunca nos 3 primeiros do dia, 1 a cada 3 contratos e 4 min, nunca com Remover anúncios, nunca dentro da masmorra).
- **Interfaces:** `IAnuncios` (`Recompensado(prêmio, fim(bool))`, `Intersticial(fim)`) e `ILoja` (`Preco`, `Comprar(produto, fim(bool))`). Hoje só as implementações de teste (`Hud.AnunciosTeste`, `Hud.LojaTeste`); LevelPlay/AdMob e Unity IAP entram numa raia própria trocando a instância no construtor do `Hud`.

**Sessão, config e diário (Game.cs):** `sim.OpenSession(agora, data)` no início e ao voltar do fundo (1 vez por volta), `sim.Touch(agora)` ao ir para o fundo, ao sair e a cada 60 s; `device_fps_bucket` a cada 60 s reais. `Resources/config.json` → `sim.Config` no Awake; `"config_url"` (vazio de fábrica) só aceita https e sobrescreve as chaves locais (timeout 5 s; sem URL, nada de rede). O diário recebe sozinho os eventos novos do Core (`rare_offer`, `reroll`, `event_*`, `season_*`, `rewarded_*`, `interstitial_show`, `iap_purchase`, `modifier_pick`, `endless_*`...); a View acrescenta `rewarded_offer` (1 vez por id mostrado), `rewarded_cancel` e `iap_cancel`.

**Som de meta** (`Hud.SomMeta`, AUDIO.md "Meta e loja"; sem háptica): `EventStart` → `evento`, `SeasonProgress` → `temporada`, `EventReward`/`SeasonClaim`/`Milestone`/`RewardedGrant` → `recompensa`, `BlueprintFound` → `blueprint`, `IapPurchase` → `compra_loja`, `GearUpgrade`/`GearUnlock`/`MasteryUp` → `compra`, anúncio abre/fecha → `anuncio`. Música `evento` nos quadros de eventos e de temporada.
