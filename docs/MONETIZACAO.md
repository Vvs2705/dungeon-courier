# Dungeon Courier: monetização real (Unity IAP + LevelPlay com Unity Ads)

Origem: `historico/CONTRATO_LANCAMENTO.md` §7 e §11. As regras ficam no `DC.Core`, que não conhece SDK nenhum: as ofertas, os ids `rw-*`, o teto do interstitial e os direitos de compra (`sim.Purchase`, `sim.GrantReward`, `sim.InterstitialDue`). A View só liga os SDKs a essas regras.

**Estado (1.4.3-beta, conferido em 2026-10-25):** compras pelo Unity IAP e anúncios reais pelo LevelPlay com o Unity Ads, **ligados no build Android** desde a 1.3.0-beta (2026-10-16). Anúncio não personalizado para todos (decisão do ECA Digital), sem `AD_ID`. PC e Editor usam sempre a loja falsa e o simulador de anúncio. Falta só cadastrar os produtos na Play Console, que espera a conta de organização (D-U-N-S, previsão 2026-11-25).

## 1. Como está ligado

| Peça | Versão | Onde |
|---|---|---|
| Unity IAP (`com.unity.purchasing`) | 5.4.3 | `Packages/manifest.json` (traz o `com.unity.services.core` 1.18.0 e o `com.unity.nuget.newtonsoft-json`) |
| LevelPlay (`com.unity.services.levelplay`) | pacote 9.5.1; SDK ironSource 9.7.0 (Android `mediation-sdk` 9.7.0) | `Packages/manifest.json`, `Assets/LevelPlay/Editor/*.xml`, Gradle em `Assets/Plugins/Android/mainTemplate.gradle` |
| Adaptador Unity Ads | 5.16 (Android `unityads-adapter` 5.15.0, que já traz o Unity Ads) | `Assets/LevelPlay/Editor/ISUnityAdsAdapterDependencies.xml` |
| Define `DC_LEVELPLAY` | ligado só no Android (`ProjectSettings`: `IAP_FAKE_STORE_DEFAULT;DC_LEVELPLAY`) | `View/AnunciosLevelPlay.cs` só compila com ele; `View/DC.asmdef` referencia `Unity.LevelPlay`; o viewcheck compila com o define |

Por que a 5.4.3 e não a 5.4.4 do IAP: a 5.4.4 puxa o `com.unity.services.deployment`, que só serve ao Editor e tem arquivos com caminho longo demais para o worktree (erro de importação). Nada de runtime muda entre as duas. O IAP põe o Google Play Billing no Gradle sozinho.

**Código (View):**

- `Loja.cs`: `ILoja` (`Teste`, `Preco`, `Comprar`, `Restaurar`), a vitrine (Remover anúncios, Pacote inicial, as 8 roupas com 6 à venda, Pacote de evento e RESTAURAR COMPRAS), `Hud.Concede` (o único caminho para `sim.Purchase`) e a `LojaTeste` (janela "Loja de teste", também usada como confirmação da loja falsa do IAP).
- `LojaIap.cs`: a loja real com Unity IAP 5.
- `Anuncios.cs`: `IAnuncios`, `Hud.CriaAnuncios`, as ofertas de rewarded, o interstitial do CONTINUAR e o simulador `AnunciosTeste`.
- `AnunciosLevelPlay.cs`: os anúncios reais, inteiros dentro de `#if DC_LEVELPLAY`; privacidade aplicada antes do `Init`.
- `Hud.cs` troca as instâncias no construtor. `Game.cs` não abre sessão nova nem a pausa quando o app vai ao fundo por causa de um anúncio (`Hud.AnuncioAberto`).
- `Editor/Setup.cs`: define `IAP_FAKE_STORE_DEFAULT` (a loja falsa do Unity não abre a janela em inglês dela), `ResolveAndroid()` força o resolvedor antes de cada build Android e `SemIdDePublicidade` tira do manifesto o `AD_ID` e as permissões do Privacy Sandbox.

**As chaves ficam num lugar só, `client/Assets/_DC/Resources/config.json`** (lidas do arquivo do build; o `config_url` remoto não mexe nelas). Não são segredo: vão dentro do APK.

| Chave | Hoje | Efeito |
|---|---|---|
| `iap_google_play` | `false` | `false`: loja falsa do Unity IAP também no Android (build de teste). `true`: Google Play no Android (ligar quando os produtos existirem na Play). PC e Editor usam sempre a loja falsa. |
| `levelplay_app_key` | `28bb8d98d` | Preenchida: `LevelPlay.Init` no Android. Vazia: simulador, e nenhuma rede de anúncio é iniciada. |
| `levelplay_rewarded` | `ilah7d1mcs7v1cno` | Id da unidade Rewarded (vazio = rewarded desligado). |
| `levelplay_interstitial` | `cepd2rejxi2ykfxw` | Id da unidade Interstitial (vazio = interstitial desligado). |

### 1.1 Compras (Unity IAP 5)

| Product (Core) | Id na loja | Tipo | Preço sugerido (o Vinicius decide) |
|---|---|---|---|
| RemoveAds | `br.com.vstack.dungeoncourier.remove_ads` | não consumível | R$ 9,90 |
| StarterPack | `br.com.vstack.dungeoncourier.starter_pack` | não consumível | R$ 4,90 |
| OutfitNight | `br.com.vstack.dungeoncourier.outfit_night` | não consumível | R$ 4,90 |
| OutfitRoyal | `br.com.vstack.dungeoncourier.outfit_royal` | não consumível | R$ 4,90 |
| OutfitExplorer | `br.com.vstack.dungeoncourier.outfit_explorer` | não consumível | R$ 4,90 |
| OutfitForge | `br.com.vstack.dungeoncourier.outfit_forge` | não consumível | R$ 4,90 |
| EventPack | `br.com.vstack.dungeoncourier.event_pack` | **consumível** (vale a semana) | R$ 2,90 |
| OutfitMiner | `br.com.vstack.dungeoncourier.outfit_miner` | não consumível | R$ 4,90 |
| OutfitGravekeeper | `br.com.vstack.dungeoncourier.outfit_gravekeeper` | não consumível | R$ 4,90 |

Os ids seguem a ordem do enum `Product` (`LojaIap.Ids`). **Depois de cadastrado na Play Console, um id nunca muda.** Produto novo no Core entra no fim do enum e no fim da lista. A Clássica é grátis e a Fantasma só sai por marco ou temporada: nenhuma das duas está na loja.

**Fluxo em 2 passos:** COMPRAR → `PurchaseProduct` → a loja cobra → `OnPurchasePending` → `Hud.Concede` (o Core concede o conteúdo fixo e o `Game` grava o save) → `ConfirmPurchase` **só se o Core ficou com o produto** (`sim.PurchaseGranted`: tem o produto ou já concedeu aquele pedido). Se o Core não concedeu (por exemplo, pacote de evento com o quadro fechado), a compra fica sem confirmar e o Google Play devolve o dinheiro sozinho em 3 dias. Pendente de outra sessão (o app fechou no meio) e compra restaurada passam pelo mesmo caminho. Como o Core não concede duas vezes, uma compra repetida só é confirmada. O pacote de evento (consumível) guarda o id do pedido no save: o mesmo pedido reentregue na semana seguinte não vale outra semana. A roupa Explorador que veio no Pacote inicial aparece como comprada (a loja não a vende de novo).

- **Ao abrir:** `FetchPurchases`. Quem reinstalou ou trocou de aparelho recebe de volta as roupas, o Remover anúncios e o Pacote inicial.
- **RESTAURAR COMPRAS:** última linha da loja. No Google Play é o `RestoreTransactions`; na loja falsa, que não tem restaurar, é o `FetchPurchases`. O rodapé avisa "Compras restauradas." ou pede para tentar de novo com internet.
- **Compra adiada** (aprovação dos pais, pagamento pendente): o rodapé avisa e o conteúdo chega quando a loja aprovar.
- **Loja falsa** (PC, Editor e o APK de teste): `UnityIAPServices.StoreController("fake")`. COMPRAR abre a janela "Loja de teste" com o preço "R$ x,xx (teste)", e CONFIRMAR passa pelo mesmo fluxo de 2 passos. A loja falsa do Unity diz "$0.01" para tudo, então o preço mostrado é o da tabela acima, marcado "(teste)". Se o IAP não conectar, a `LojaTeste` assume sozinha.
- **Google Play** (`iap_google_play: true` no Android): o preço é o `localizedPriceString` da loja (moeda e formato do jogador). Antes de os produtos chegarem, aparece "…". Produto que não está na Play aparece como "indisponível" e nada é concedido. **No Google Play a loja de teste nunca assume.**

### 1.2 Anúncios

- **PC e Editor:** sempre o simulador (`AnunciosTeste`, "ANÚNCIO DE TESTE", 5 s).
- **Android (build atual, com `DC_LEVELPLAY` e a chave):** `LevelPlay.Init(appKey)`, com `LevelPlayPrivacySettings` antes (GDPR sem consentimento, CCPA opt-out, COPPA desligado: anúncio não personalizado para todos). Depois do init, cria e carrega o rewarded e o interstitial; se a carga falhar, tenta de novo em 30 s. Sem a chave, ou no PC/Editor, continua o simulador.
- **Quando mostrar é sempre do Core.** Rewarded só com `sim.RewardOffer`, e a concessão é `sim.GrantReward`, com id `rw-<contrato>-<tipo>` no save (fechar e reabrir não repete). A recompensa só vale com `OnAdRewarded`, que pode chegar até 1 s depois do `OnAdClosed`. Interstitial só no CONTINUAR do resultado, quando `sim.InterstitialDue` deixa (nunca nos 3 primeiros contratos do dia, no máximo 1 a cada 3 contratos e 4 min, nunca com Remover anúncios, nunca dentro da masmorra) **e** há anúncio carregado (`IntersticialPronto`). Sem anúncio carregado, o jogo segue e o Core não conta nada.
- **Rewarded real sem anúncio pronto:** aparece a janela "Carregando o anúncio…" por até 8 s e depois "Nenhum anúncio disponível agora". FECHAR (ou voltar) cancela sem recompensa.
- Banner: não tem (GDD §7).

### 1.3 AndroidManifest

Medido com o `aapt dump permissions` do SDK Android do Unity nos APKs de release. A origem vem do relatório de merge do Gradle (`manifest-merger-blame-release-report.txt`).

| Permissão | Build atual (IAP + LevelPlay + Unity Ads) | De onde vem |
|---|---|---|
| `android.permission.INTERNET` | sim | o Unity (`UnityWebRequest` do `config_url`) e os SDKs |
| `android.permission.VIBRATE` | sim | o jogo (vibração) |
| `com.android.vending.BILLING` | sim | Google Play Billing 9.0.0 (Unity IAP) |
| `android.permission.ACCESS_NETWORK_STATE` | sim | `datatransport` (telemetria do Billing 9) e o Unity Ads. Permissão normal, sem dado do jogador |
| `WAKE_LOCK`, `RECEIVE_BOOT_COMPLETED`, `FOREGROUND_SERVICE` | sim | `androidx.work` (dependência do Unity Ads) |
| `com.google.android.gms.permission.AD_ID` | **não** | removido por `Setup.SemIdDePublicidade` (o SDK pede; a Play Console declara "não usa ID de publicidade") |
| `ACCESS_ADSERVICES_TOPICS` / `_ATTRIBUTION` / `_AD_ID` | **não** | Privacy Sandbox do Unity Ads, removidas pelo mesmo `SemIdDePublicidade` |

### 1.4 Tamanho do APK de release (ARM64, IL2CPP)

| APK | MiB |
|---|---|
| Sem SDKs (2026-10-11) | 54,3 |
| Só Unity IAP (2026-10-12) | 63,9 |
| IAP + LevelPlay + Unity Ads (2026-10-14) | 67,7 |
| **Com o elenco HD (2026-10-16, 1.3.0-beta em diante)** | **83,0** |

Quase todo o peso dos SDKs é C# do IAP, do Services Core e do Newtonsoft no `libil2cpp.so`; o salto de 67,7 para 83,0 MiB é arte (folhas maiores do elenco HD). Se o tamanho pesar, suba o *Managed Stripping Level* do IL2CPP e teste a loja de novo.

## 2. O que falta do lado do Vinicius

1. **Play Console (compras),** quando a conta de organização sair (D-U-N-S):
   - Criar os 9 produtos do §1.1 com **esses ids**, em "Produtos no app", com preço e ativos. A Play não separa consumível de não consumível: quem consome o pacote de evento é o app.
   - Cadastrar testadores de licença.
   - Subir um AAB de teste interno com `iap_google_play: true`. A Play só entrega produtos para um app publicado numa faixa de teste e assinado com a chave de upload.
   - Recomendado: a chave de licença da Play (Monetize > Licensing) no projeto Unity Cloud e a validação de recibo (`CrossPlatformValidator`). Hoje a compra é aceita como a loja entrega, sem validação local.
2. **LevelPlay depois da publicação:** ligar o app à loja no painel (Apps > Dungeon Courier > Live app, package `br.com.vstack.dungeoncourier`) e publicar o `app-ads.txt` no site da V-STACK (o painel dá o conteúdo).
3. **Validar no aparelho:** Test Suite num build de desenvolvimento (§4, passo 6).

Já feito: conta LevelPlay da V-STACK com as chaves (2026-10-14), Unity Ads ligado ao LevelPlay (2026-10-16), política de privacidade publicada com os itens 7 (anúncios) e 8 (compras) nos 4 idiomas (`LOJA.md` §4.5), e a decisão do ECA Digital (Lei 15.211/2025): anúncio não personalizado para todos, aplicado antes do `Init`.

## 3. Como testar

- **PC (Editor ou build Windows):** loja → COMPRAR → "Loja de teste" → CONFIRMAR. O Core concede e a linha vira COMPRADO. No log aparecem "DC IAP: 9 produtos (loja falsa)" ao abrir e "iap_purchase" no diário. RESTAURAR COMPRAS → "Compras restauradas." Anúncios: o simulador de 5 s.
- **Teste automático do caminho do dinheiro (PlayMode):** `Assets/_DC/Tests/PlayMode/LojaIapTests.cs`.
  - O teste sobe o Game de verdade e compra a roupa Noturno na loja falsa. A compra fica pendente, a `LojaIap` concede pelo Core e só então confirma; depois o `FetchPurchases` devolve a compra.
  - Comando: `Unity -batchmode -projectPath client -runTests -testPlatform PlayMode -assemblyNames DC.PlayTests -testsession -testResults r.xml`.
  - Sem `-testsession` o teste é ignorado, para não mexer no save do Editor.
  - Os testes EditMode (`DC.Tests`) não dependem de SDK: a loja do IAP não existe fora do Play Mode.
- **Fotos:** `DungeonCourier.exe -testsession -dev-desbloqueia -tela loja -shot loja.png` e `-tela compra -shot compra.png` (540×960). As fotos antigas de referência (anteriores à arte v2) saíram do repositório em 2026-10-25; tire novas com esses comandos.
- **APK de teste (como está):** loja falsa do IAP (`iap_google_play: false`) e anúncios reais do LevelPlay (sem anúncio pronto, o jogo segue sem recompensa nem interstitial). Sem `AD_ID`.
- **Compras reais:** faixa de teste interno da Play, conta de testador de licença (a compra não cobra) e `iap_google_play: true`. Teste: comprar uma roupa; desinstalar e reinstalar (a roupa volta ao abrir); RESTAURAR COMPRAS; comprar o pacote de evento com o quadro de eventos aberto.

## 4. Como o LevelPlay foi ligado (registro, para refazer ou atualizar)

Em 2026-10-12 o coordenador tirou o SDK de anúncio do build enquanto não havia conta (os APKs iam para testadores e o jogo prometia que nada saía do aparelho). Ele voltou pela branch `feat/levelplay` (2026-10-12), com as chaves da conta em 2026-10-14, e está no build desde a 1.3.0-beta. A branch trouxe:
- o pacote 9.5.1 com o Unity Ads e o resolver;
- o define `DC_LEVELPLAY` no Android e o `Unity.LevelPlay` no asmdef e no viewcheck;
- anúncio não personalizado para todos antes do `Init`: `LevelPlayPrivacySettings` com GDPR sem consentimento, CCPA com opt-out e COPPA desligado (público 13+);
- `Setup.SemIdDePublicidade`, que tira do APK o `AD_ID` e as 3 permissões do Privacy Sandbox.

**Atualização de 2026-10-22** (pedido do Vinicius: usar a versão nova em vez de desfazer a atualização automática do LevelPlay a cada abertura do Unity):
- SDK do ironSource 9.6.2 → 9.7.0 (Android `mediation-sdk` 9.7.0);
- adaptador do Unity Ads 5.15 → 5.16 (Android `unityads-adapter` 5.15.0, que já traz o Unity Ads; saiu a dependência `unity-ads` 4.21.0 separada).
- Foi pelo mesmo instalador do *Update all* da tela do LevelPlay (`SdkInstaller`; o painel só compila com `LEVELPLAY_DEPENDENCIES_INSTALLED`).
- A conta (*Connect your account*) e as chaves não mudam com isso. Se o painel pedir de novo, refaça como em 2026-10-16.

Conferido com `aapt`. O APK fica com INTERNET, VIBRATE, ACCESS_NETWORK_STATE, BILLING e o WorkManager do SDK (WAKE_LOCK, RECEIVE_BOOT_COMPLETED, FOREGROUND_SERVICE), sem AD_ID, e tem 67,7 MiB. O passo 3 foi feito em 2026-10-14, na conta LevelPlay da V-STACK (vinicius@vstack-solutions.com.br), com o app "Dungeon Courier" (Android, "Not live yet", COPPA "Not directed"):

| Chave do `config.json` | Valor | No painel |
|---|---|---|
| `levelplay_app_key` | `28bb8d98d` | App Key |
| `levelplay_rewarded` | `ilah7d1mcs7v1cno` | ad unit `rewarded`, recompensa "Recompensa" ×1 (o núcleo decide o prêmio) |
| `levelplay_interstitial` | `cepd2rejxi2ykfxw` | ad unit `interstitial` |

Em 2026-10-16, a conta Unity Ads da mesma organização (Unity, core ID `4674267143380`) foi ligada ao LevelPlay em *Connect your account > Unity Ads*, com a chave da Monetization Stats API, que o Vinicius criou e colou (a chave fica fora do repositório). O painel mostra a receita de "ironSource Ads and Unity Ads". As chaves do `config.json` não são segredo: vão dentro do APK. Falta o 6 (Test Suite num build de desenvolvimento, no POCO) e o 5 na Play Console. Quando o app for publicado, ligue-o à loja no painel (Apps > Dungeon Courier > Live app, package `br.com.vstack.dungeoncourier`). Sem chave, o jogo segue no simulador e não chama o `Init`.


Já foi feito uma vez e funcionou: commit `bd10ca9`, APK de 65,4 MiB. Na ordem:

1. **Pacote:** em *Window > Package Manager*, instalar o **Ads Mediation** (`com.unity.services.levelplay`; o testado foi o 9.5.1) e aceitar o **Import** do Mobile Dependency Resolver.
   - Em batch não há janela: abra o Unity uma vez com a variável de ambiente `UNITY_THISISABUILDMACHINE=1` e ele importa o resolver sozinho.
   - Depois, em *Ads Mediation > Network Manager*, instale o SDK do LevelPlay e as redes (o padrão é o Unity Ads). Eles viram os XMLs em `Assets/LevelPlay/Editor/`.
   - O resolver cria e remenda os templates do Gradle em `Assets/Plugins/Android/` e lista tudo em `ProjectSettings/AndroidResolverDependencies.xml`.
   - O `Setup.ResolveAndroid()` já força a resolução antes de cada build Android.
2. **Código:**
   - Acrescentar `"Unity.LevelPlay"` às `references` de `Assets/_DC/Scripts/View/DC.asmdef`.
   - Ligar o define `DC_LEVELPLAY` em *Project Settings > Player > Android > Scripting Define Symbols*, ao lado do `IAP_FAKE_STORE_DEFAULT`.
   - No `client/tools/viewcheck/view/DC.View.csproj`, acrescentar a referência `Library\ScriptAssemblies\Unity.LevelPlay.dll` e `<DefineConstants>$(DefineConstants);DC_LEVELPLAY</DefineConstants>`.
3. **Chaves:** `levelplay_app_key`, `levelplay_rewarded` e `levelplay_interstitial` no `config.json`. Sem a chave, o jogo continua no simulador e não chama o `Init`, mas o SDK vai no APK do mesmo jeito.
4. **Manifesto:** gerar o APK de release e conferir com `aapt dump permissions` (o `aapt.exe` fica em `.../AndroidPlayer/SDK/build-tools/36.0.0/`). Com o Unity Ads, entram as permissões da coluna do meio do §1.3. Conferir também, num aparelho, o tráfego de rede na abertura, com o *Network Inspector* do Android Studio ou um proxy.
5. **Privacidade:**
   - Na Play Console, marcar "Contém anúncios" e responder **não** na declaração de ID de publicidade (o APK não tem `AD_ID`, `Setup.SemIdDePublicidade`).
   - Preencher o formulário de segurança dos dados com a tabela do Unity Ads (`LOJA.md` §4.5). O item 7 da política já está publicado nos 4 idiomas (2026-10-13; em 2026-10-16 passou a citar também a rede ironSource Ads).
   - Decisão do ECA Digital (§2.4): anúncio não personalizado para todos, já aplicado antes do `Init`.
6. **Validar:**
   - Test Suite: build de desenvolvimento (`Setup.BuildAndroidDev`) aberto com `adb shell am start -S -n br.com.vstack.dungeoncourier/<activity> -e unity "-adtestsuite"`. O `AnunciosLevelPlay` chama `LevelPlay.SetMetaData("is_test_suite", "enable")` antes do `Init` e `LevelPlay.LaunchTestSuite()` no `Iniciou` só com `Debug.isDebugBuild` e a flag; o release nunca abre o Test Suite, então não há o que tirar antes de publicar.
   - No `adb logcat`, procure "DC LevelPlay: iniciado".
   - Em modo avião, o rewarded deve mostrar "Nenhum anúncio disponível agora" e o CONTINUAR deve seguir sem interstitial.
