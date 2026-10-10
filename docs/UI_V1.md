# Dungeon Courier — UI/UX da v1.0-beta (CONTRATO_V1 §1, §10, §11, §14)

Código: `client/Assets/_DC/Scripts/View/` — `Hud.cs` (estrutura, faixa do contrato, oferta, pausa, cena, resultado), `Menus.cs` (título, menu, quadro, código, guilda/armas, perk, configurações, créditos), `Textos.cs` (pt-BR/EN), `Art.cs` (ícones, `Ajustes`), `Exportar.cs` (diário e código para fora do aparelho), `Game.cs` (fluxo, diário, áudio por evento). uGUI por código, referência 1080 × 1920 casando a largura, dentro da área segura.

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

## 3. Idioma (pt-BR / EN)

- `Ajustes.Ingles` fica no PlayerPrefs `dc_idioma`. Na primeira abertura, vale português se o aparelho estiver em português e inglês nos outros casos. A troca vale na hora: `Hud.Relocalizar` relê todo texto fixo, registrado em `Loc`, e redesenha a tela aberta.
- Cada texto é `L("pt", "en")` com a frase inteira, e números entram por `{0}`. O conteúdo do Core é traduzido na View pelo enum ou código: dicas (`Hint`), cargas, salas, perks e armas. O pt vem do próprio Core. Texto novo do Core sem linha EN aparece em pt até ganhar tradução.
- O diário grava códigos crus (`DIA`, `C12`, nomes de evento) e nunca texto traduzido.

## 4. Telemetria e privacidade (§10)

- `persistentDataPath/diario.csv` tem as colunas `utc,instalacao,versao,sessao,t_sessao,t_contrato,evento,a,b,contrato,sala`. `instalacao` é um GUID gerado na primeira abertura (PlayerPrefs `dc_instalacao`), sem dado pessoal. O `-reset` não apaga esse GUID. Um diário com o cabeçalho da v0.1 é renomeado para `diario_anterior.csv`.
- Os eventos do Core vão para o diário sozinhos (`SimEvent.DiaryName`, incluindo `contract_stars`, `perk_pick`, `weapon_equip`, `daily_start/end` e `code_start`). A View acrescenta `session_start`, `settings`, `share_code` e `diary_export`.
- **Exportar diário** (Configurações): no Android 10+, o CSV é copiado para `Download/DungeonCourier` (MediaStore) e vai pelo seletor de apps (`ACTION_SEND`, `EXTRA_STREAM`). No Android 8–9, vai como texto (`EXTRA_TEXT`, no máximo os últimos 400 KB). No Windows, o Explorer abre com o arquivo selecionado. Nada sai do aparelho sem esse toque. O aviso curto aparece no título e o completo nas Configurações e nos Créditos.

## 5. Builds (§14)

- `Setup.Version = "1.0.0-beta"`. O `versionCode` é gerado da versão: `(maior·10000 + menor·100 + patch)·10 + estágio`, com alpha 1, beta 2, rc 5 e final 9, o que dá 100002. Nunca reutilizar o número.
- `DC.EditorTools.Setup.BuildAndroidRelease` gera `Builds/android/DungeonCourier-1.0.0-beta.apk`: não-development, ARM64, IL2CPP Release, APK (não AAB) e chave de debug do Unity. Os APKs anteriores são apagados antes. `BuildWindowsRelease` gera `Builds/win-release/`. Os builds dev (`BuildWindows`, `BuildAndroidDev`) continuam como estavam.
- **A Play exige chave própria e AAB:** keystore de upload fora do repositório, senha por variável de ambiente lida no build e Play App Signing. Entra quando o jogo for para a loja.

## 6. Fotos sem clicar (dev)

`-testsession -tela titulo|menu|quadro|codigo|guilda|armas|config|creditos -shot f.png`. O perk e o resultado precisam de contrato: use `-contract K -bot` para o resultado. Para o perk, jogue até a camada 2 de um contrato C4+.
