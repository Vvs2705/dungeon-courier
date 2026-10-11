# Dungeon Courier — UI/UX da v1.0-beta (CONTRATO_V1 §1, §10, §11, §14)

Código: `client/Assets/_DC/Scripts/View/` — `Hud.cs` (estrutura, faixa do contrato, oferta, pausa, cena, resultado), `Menus.cs` (título, menu, quadro, código, guilda/armas, perk, configurações, créditos), `Textos.cs` (pt-BR, EN, ES e ID; §3), `Art.cs` (ícones, `Ajustes`), `Exportar.cs` (diário e código para fora do aparelho), `Game.cs` (fluxo, diário, áudio por evento). uGUI por código, referência 1080 × 1920 casando a largura, dentro da área segura.

## 1. Fluxo

```
Título ──toque──> Menu ──JOGAR──> Quadro ──contrato/dia/código──> [Oferta de resgate] ──> Contrato
                   │                 │                                                      │  (Perk 1 de 3 depois das camadas 2 e 4)
                   ├─GUILDA (abas Melhorias | Armas)                                        │  (Pausa: Continuar, Configurações, Desistir)
                   ├─CONFIGURAÇÕES                                                          v
                   └─CRÉDITOS                     Quadro <──CONTINUAR── Resultado <── Cena da entrega (só entregue; toque pula)
                                                                           └─GUILDA (atalho, se a guilda já abriu)
```

| Tela | Voltar (Android / Esc) | Música |
|---|---|---|
| Título, Menu | grava e sai | `menu` |
| Quadro | Menu | `menu` |
| Guilda, Código, Oferta, Configurações, Créditos | fecha a janela | a da tela de baixo |
| Contrato | abre a Pausa | `masmorra` (M1/B1: `chefe`) |
| Pausa | fecha (na confirmação de desistir: volta à pausa) | — |
| Perk | não fecha (escolha obrigatória; o Core para o tempo) | — |
| Cena da entrega | pula para o Resultado | silêncio + jingle |
| Resultado | Quadro | silêncio |

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

`-testsession -tela titulo|menu|quadro|codigo|guilda|armas|config|creditos|perk -shot f.png`. O resultado precisa de contrato: use `-contract K -bot`. A pausa sai com `-contract K -pausa`.

`-lang pt|en|es|id` (só com `-testsession`) força o idioma na memória, sem gravar a escolha do jogador. Exemplo: `-testsession -lang id -tela config -shot config_id.png`.
