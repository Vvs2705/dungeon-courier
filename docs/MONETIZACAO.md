# Dungeon Courier: monetização real (Unity IAP + LevelPlay)

CONTRATO_LANCAMENTO §7 e §11. As regras ficam no `DC.Core`, que não conhece SDK nenhum: as ofertas, os ids `rw-*`, o teto do interstitial e os direitos de compra (`sim.Purchase`, `sim.GrantReward`, `sim.InterstitialDue`). A View só liga os SDKs a essas regras.

## 1. Como está ligado

| Peça | Versão | Onde |
|---|---|---|
| Unity IAP (`com.unity.purchasing`) | 5.4.3 | `Packages/manifest.json` |
| Ads Mediation / LevelPlay (`com.unity.services.levelplay`) | 9.5.1 | `Packages/manifest.json` |
| SDK nativo LevelPlay + adaptador Unity Ads | mediation-sdk 9.6.1, unity-ads 4.21.0 (adaptador 5.14.0), play-services-ads-identifier 18.1.0 | `Assets/LevelPlay/Editor/*Dependencies.xml` (instalados pelo Network Manager do pacote) |
| Mobile Dependency Resolver | 1.2.185 | `Assets/MobileDependencyResolver/` (importado pelo próprio pacote LevelPlay) |
| Templates do Gradle | os do Unity 6000.3.23f1 | `Assets/Plugins/Android/` (`mainTemplate.gradle`, `settingsTemplate.gradle`, `gradleTemplate.properties`), ligados e remendados pelo resolver (lista em `ProjectSettings/AndroidResolverDependencies.xml`) |

Por que a 5.4.3 e não a 5.4.4 do IAP: a 5.4.4 puxa o `com.unity.services.deployment`, que só serve ao Editor e tem arquivos com caminho longo demais para o worktree (erro de importação). Nada de runtime muda entre as duas.

**Código (View):**

- `Loja.cs`: `ILoja` (`Teste`, `Preco`, `Comprar`, `Restaurar`), a vitrine (Remover anúncios, Pacote inicial, as 8 roupas com 6 à venda, Pacote de evento e RESTAURAR COMPRAS), `Hud.Concede` (o único caminho para `sim.Purchase`) e a `LojaTeste` (janela "Loja de teste", também usada como confirmação da loja falsa do IAP).
- `LojaIap.cs`: a loja real com Unity IAP 5.
- `Anuncios.cs`: `IAnuncios`, `Hud.CriaAnuncios`, as ofertas de rewarded e o interstitial do CONTINUAR, mais o simulador `AnunciosTeste`.
- `AnunciosLevelPlay.cs`: os anúncios reais.
- `Hud.cs` troca as instâncias no construtor. `Game.cs` não abre sessão nova nem a pausa quando o app vai ao fundo por causa de um anúncio real.
- `Editor/Setup.cs`: define `IAP_FAKE_STORE_DEFAULT` (a loja falsa do Unity não abre a janela em inglês dela) e, no build Android, chama `ResolveAndroid()`, que força o `PlayServicesResolver.ResolveSync`, porque em batch a resolução automática pode não rodar antes do build.

**As chaves ficam num lugar só, `client/Assets/_DC/Resources/config.json`** (lidas do arquivo do build; o `config_url` remoto não mexe nelas):

| Chave | De fábrica | Efeito |
|---|---|---|
| `iap_google_play` | `false` | `false`: loja falsa do Unity IAP também no Android (build de teste). `true`: Google Play no Android. PC e Editor usam sempre a loja falsa. |
| `levelplay_app_key` | `""` | Vazia: simulador de anúncio e **nenhuma rede de anúncio é iniciada**. Preenchida: `LevelPlay.Init` no Android (PC e Editor continuam no simulador). |
| `levelplay_rewarded` | `""` | Id da unidade Rewarded (vazio = rewarded desligado). |
| `levelplay_interstitial` | `""` | Id da unidade Interstitial (vazio = interstitial desligado). |

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

**Fluxo em 2 passos:** COMPRAR → `PurchaseProduct` → a loja cobra → `OnPurchasePending` → `Hud.Concede` (o Core concede o conteúdo fixo e o `Game` grava o save) → `ConfirmPurchase` **só se o Core ficou com o produto** (`sim.Owns`). Se o Core não concedeu (por exemplo, pacote de evento com o quadro fechado), a compra fica sem confirmar e o Google Play devolve o dinheiro sozinho em 3 dias. Pendente de outra sessão (o app fechou no meio) e compra restaurada passam pelo mesmo caminho. Como o Core não concede duas vezes, uma compra repetida só é confirmada.

- **Ao abrir:** `FetchPurchases`. Quem reinstalou ou trocou de aparelho recebe de volta as roupas, o Remover anúncios e o Pacote inicial.
- **RESTAURAR COMPRAS:** última linha da loja (`RestoreTransactions`). O rodapé avisa "Compras restauradas." ou pede para tentar de novo com internet.
- **Compra adiada** (aprovação dos pais, pagamento pendente): o rodapé avisa e o conteúdo chega quando a loja aprovar.
- **Loja falsa** (PC, Editor e o APK de teste): `UnityIAPServices.StoreController("fake")`. COMPRAR abre a janela "Loja de teste" com o preço "R$ x,xx (teste)", e CONFIRMAR passa pelo mesmo fluxo de 2 passos. A loja falsa do Unity diz "$0.01" para tudo, então o preço mostrado é o da tabela acima, marcado "(teste)". Se o IAP não conectar, a `LojaTeste` assume sozinha.
- **Google Play** (`iap_google_play: true` no Android): o preço é o `localizedPriceString` da loja (moeda e formato do jogador). Antes de os produtos chegarem, aparece "…". Produto que não está na Play aparece como "indisponível" e nada é concedido. **No Google Play a loja de teste nunca assume.**

### 1.2 Anúncios (LevelPlay)

- **Sem `levelplay_app_key`, ou no PC/Editor:** o simulador (`AnunciosTeste`, "ANÚNCIO DE TESTE"). Nenhum `LevelPlay.Init`, nenhuma rede.
- **Com a chave, no Android:** `LevelPlay.Init(appKey)`. Depois do init, cria e carrega o rewarded e o interstitial. Se a carga falhar, tenta de novo em 30 s.
- **Quando mostrar continua sendo do Core.** Rewarded só com `sim.RewardOffer`, e a concessão é `sim.GrantReward`, com id `rw-<contrato>-<tipo>` no save (fechar e reabrir não repete). A recompensa só vale com `OnAdRewarded`, que pode chegar até 1 s depois do `OnAdClosed`. Interstitial só no CONTINUAR do resultado, quando `sim.InterstitialDue` deixa (nunca nos 3 primeiros contratos do dia, no máximo 1 a cada 3 contratos e 4 min, nunca com Remover anúncios, nunca dentro da masmorra) **e** há anúncio carregado. Sem anúncio carregado, o jogo segue e o Core não conta nada.
- **Rewarded sem anúncio pronto:** aparece a janela "Carregando o anúncio…" por até 8 s e depois "Nenhum anúncio disponível agora". FECHAR (ou voltar) cancela sem recompensa.
- Banner: não tem (GDD §7).

### 1.3 O que os SDKs acrescentam ao AndroidManifest

Medido no APK de release (`aapt dump permissions`, antes e depois dos SDKs), com a origem tirada do relatório de merge do Gradle (`manifest-merger-blame-release-report.txt`):

| Permissão | Antes | Depois | De onde vem |
|---|---|---|---|
| `android.permission.INTERNET` | sim | sim | já existia (o Unity põe por causa do `UnityWebRequest` do `config_url`) |
| `android.permission.VIBRATE` | sim | sim | o jogo (vibração) |
| `com.google.android.gms.permission.AD_ID` | não | **sim** | `play-services-ads-identifier` 18.1.0 (dependência do SDK LevelPlay) |
| `android.permission.ACCESS_NETWORK_STATE` | não | **sim** | Unity Ads 4.21.0 (e o `LevelPlayAndroidManifestProcessor`) |
| `android.permission.ACCESS_ADSERVICES_TOPICS` | não | **sim** | Unity Ads 4.21.0 (Privacy Sandbox do Android) |
| `android.permission.ACCESS_ADSERVICES_ATTRIBUTION` | não | **sim** | Unity Ads 4.21.0 (Privacy Sandbox do Android) |
| `com.android.vending.BILLING` | não | **sim** | Google Play Billing 9.0.0 (Unity IAP) |
| `android.permission.WAKE_LOCK` | não | **sim** | `androidx.work` 2.7.0 (dependência do Unity Ads) |
| `android.permission.RECEIVE_BOOT_COMPLETED` | não | **sim** | `androidx.work` 2.7.0 (dependência do Unity Ads) |
| `android.permission.FOREGROUND_SERVICE` | não | **sim** | `androidx.work` 2.7.0 (dependência do Unity Ads) |

O manifesto também ganha componentes dos SDKs: as telas de anúncio do ironSource e do Unity Ads, a Test Suite do LevelPlay, as atividades do Billing, os serviços e receptores do WorkManager e do `datatransport` (telemetria do Google Play Billing) e 3 provedores que rodam na abertura do app: `androidx.startup.InitializationProvider`, `com.ironsource.lifecycle.IronsourceLifecycleProvider` e `LevelPlayActivityLifecycleProvider`. Os dois do ironSource acompanham o ciclo de vida das telas, e o jogo não chama `LevelPlay.Init` sem a chave.

**As permissões entram no APK mesmo sem app key**: o SDK vai no build, só não é iniciado. Por isso, na Play Console, a declaração de "ID de publicidade" deve ser **sim** já no primeiro build com estes pacotes, e o formulário de segurança dos dados segue o `LOJA.md` §4.5. Para tirar o Unity Ads (e com ele as permissões do WorkManager e do Privacy Sandbox) até ter a conta, use *Ads Mediation > Network Manager > Unity Ads > Remove*: fica só a rede própria do LevelPlay.

### 1.4 Tamanho do APK de release (ARM64, IL2CPP)

| APK | Bytes | MiB |
|---|---|---|
| Antes dos SDKs (mesmo código e arte, 2026-10-11) | 56.889.340 | 54,3 |
| Com Unity IAP + LevelPlay + Unity Ads | 68.595.132 | 65,4 |
| **Diferença** | **+11.705.792** | **+11,2 (+20,6%)** |

De onde vem (tamanho sem compressão, antes → depois): `libil2cpp.so` 38,7 → 58,6 MB (o C# do IAP, do Services Core, do LevelPlay e do Newtonsoft compilado pelo IL2CPP), `classes*.dex` 6,4 → 16,8 MB (o Java/Kotlin do ironSource, do Unity Ads, do Billing e do WorkManager) e `global-metadata.dat` 4,7 → 8,1 MB. A arte não mudou. Se o tamanho pesar: *Managed Stripping Level* mais alto no IL2CPP (testar a loja e os anúncios de novo depois) e tirar o adaptador Unity Ads até haver conta.

## 2. O que falta do lado do Vinicius

1. **Play Console (compras):** criar os 9 produtos do §1.1 com **esses ids**, em "Produtos no app", com preço e ativos. A Play não separa consumível de não consumível: quem consome o pacote de evento é o app. Cadastrar testadores de licença. Subir um AAB de teste interno com `iap_google_play: true` (a Play só entrega produtos para um app publicado numa faixa de teste e assinado com a chave de upload).
   - Recomendado depois: a chave de licença da Play (Monetize > Licensing) no projeto Unity Cloud e a validação de recibo (`CrossPlatformValidator`). Hoje a compra é aceita como a loja entrega, sem validação local.
2. **LevelPlay (anúncios):**
   - Criar a conta em platform.ironsrc.com (Unity LevelPlay) e adicionar o app Android `br.com.vstack.dungeoncourier`.
   - Copiar o **App Key** e criar 2 unidades, **Rewarded** e **Interstitial**. Colar no `config.json` (`levelplay_app_key`, `levelplay_rewarded`, `levelplay_interstitial`).
   - Publicar o `app-ads.txt` no site da V-STACK (o painel do LevelPlay dá o conteúdo).
   - AdMob como rede extra (opcional): o id do app AdMob em *Ads Mediation > Developer Settings > Mediated Network Settings* e o adaptador pelo *Ads Mediation > Network Manager*. O adaptador muda o APK, então meça o tamanho de novo.
3. **Política de privacidade publicada** com os itens 7 (anúncios) e 8 (compras) do `LOJA.md` §4.5 nos 4 idiomas, a URL na ficha, o formulário de segurança dos dados e a declaração de ID de publicidade (§1.3).
4. **Decisão jurídica (ECA Digital, Lei 15.211/2025):** o público é 13+ e inclui adolescentes. A lei restringe publicidade com perfilamento para menores. Antes de ligar a chave, decidir com quem cuida do jurídico se o jogo pede anúncios não personalizados para todos ou se pergunta a idade. Do nosso lado, os dois casos são uma chamada antes do `LevelPlay.Init`, em `AnunciosLevelPlay` (por exemplo, `LevelPlay.SetMetaData("is_child_directed", "true")` ou as opções de `LevelPlayPrivacySettings`).

## 3. Como testar

- **PC (Editor ou build Windows):** loja → COMPRAR → "Loja de teste" → CONFIRMAR. O Core concede e a linha vira COMPRADO. No log aparecem "DC IAP: 9 produtos (loja falsa)" ao abrir e "iap_purchase" no diário. RESTAURAR COMPRAS → "Compras restauradas." Anúncios: o simulador de 5 s.
- **Teste automático do caminho do dinheiro (PlayMode):** `Assets/_DC/Tests/PlayMode/LojaIapTests.cs`. O teste sobe o Game de verdade e compra a roupa Noturno na loja falsa. A compra fica pendente, a `LojaIap` concede pelo Core e só então confirma; depois o `FetchPurchases` devolve a compra. Comando: `Unity -batchmode -projectPath client -runTests -testPlatform PlayMode -assemblyNames DC.PlayTests -testsession -testResults r.xml`. Sem `-testsession` o teste é ignorado, para não mexer no save do Editor. Os testes EditMode (`DC.Tests`) não dependem de SDK: a loja do IAP não existe fora do Play Mode.
- **Fotos:** `DungeonCourier.exe -testsession -dev-desbloqueia -tela loja -shot loja.png` e `-tela compra -shot compra.png` (540×960). As de referência estão em `client/Builds/validation_v2/shots/sdk_tela_loja.png` e `sdk_tela_compra.png`.
- **APK de teste (como está):** loja falsa e simulador, sem conta e sem rede de anúncio. **Falta conferir num aparelho** que, sem a chave, nada sai para domínios de anúncio (os provedores de ciclo de vida do ironSource rodam na abertura). Use o *Network Inspector* do Android Studio ou um proxy e só então mantenha "nenhum dado coletado" no formulário.
- **Anúncios reais (com a conta):** preencher as 3 chaves, gerar o APK, instalar num aparelho e conferir no `adb logcat` as linhas "DC LevelPlay: iniciado" e as cargas. Para validar as redes, use o Test Suite do LevelPlay: num build de desenvolvimento, `LevelPlay.SetMetaData("is_test_suite", "enable")` antes do `Init` e `LevelPlay.LaunchTestSuite()` no `Iniciou`. **Tire as duas linhas antes de publicar.** Sem rede (modo avião), o rewarded mostra "Nenhum anúncio disponível agora" e o CONTINUAR segue sem interstitial.
- **Compras reais:** faixa de teste interno da Play, conta de testador de licença (a compra não cobra) e `iap_google_play: true`. Teste: comprar uma roupa; desinstalar e reinstalar (a roupa volta ao abrir); RESTAURAR COMPRAS; comprar o pacote de evento com o quadro de eventos aberto.
