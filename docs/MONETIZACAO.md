# Dungeon Courier: monetização real (Unity IAP; LevelPlay pronto e desligado)

CONTRATO_LANCAMENTO §7 e §11. As regras ficam no `DC.Core`, que não conhece SDK nenhum: as ofertas, os ids `rw-*`, o teto do interstitial e os direitos de compra (`sim.Purchase`, `sim.GrantReward`, `sim.InterstitialDue`). A View só liga os SDKs a essas regras.

**Decisão do coordenador (2026-10-12): o SDK de anúncio fica fora do build até haver conta LevelPlay.** Motivos:

- os APKs vão para jogadores reais de playtest, e o jogo promete que nada sai do aparelho;
- o SDK do ironSource roda na abertura do app e põe no manifesto o `AD_ID` e as permissões do WorkManager e do Privacy Sandbox mesmo sem chave (§1.3);
- falta a decisão jurídica sobre o ECA Digital para menores.

O código dos anúncios reais está pronto atrás do define `DC_LEVELPLAY`. O §4 diz como religar.

## 1. Como está ligado

| Peça | Versão | Onde |
|---|---|---|
| Unity IAP (`com.unity.purchasing`) | 5.4.3 | `Packages/manifest.json` (traz o `com.unity.services.core` 1.18.0 e o `com.unity.nuget.newtonsoft-json`) |
| Anúncios | o simulador `AnunciosTeste` | `View/Anuncios.cs`. O LevelPlay (`View/AnunciosLevelPlay.cs`) só compila com `DC_LEVELPLAY`, que está **desligado** |

Por que a 5.4.3 e não a 5.4.4 do IAP: a 5.4.4 puxa o `com.unity.services.deployment`, que só serve ao Editor e tem arquivos com caminho longo demais para o worktree (erro de importação). Nada de runtime muda entre as duas. O IAP põe o Google Play Billing no Gradle sozinho e não precisa de resolvedor de dependências nem de templates do Gradle.

**Código (View):**

- `Loja.cs`: `ILoja` (`Teste`, `Preco`, `Comprar`, `Restaurar`), a vitrine (Remover anúncios, Pacote inicial, as 8 roupas com 6 à venda, Pacote de evento e RESTAURAR COMPRAS), `Hud.Concede` (o único caminho para `sim.Purchase`) e a `LojaTeste` (janela "Loja de teste", também usada como confirmação da loja falsa do IAP).
- `LojaIap.cs`: a loja real com Unity IAP 5.
- `Anuncios.cs`: `IAnuncios`, `Hud.CriaAnuncios`, as ofertas de rewarded, o interstitial do CONTINUAR e o simulador `AnunciosTeste`.
- `AnunciosLevelPlay.cs`: os anúncios reais, inteiros dentro de `#if DC_LEVELPLAY`.
- `Hud.cs` troca as instâncias no construtor. `Game.cs` não abre sessão nova nem a pausa quando o app vai ao fundo por causa de um anúncio (`Hud.AnuncioAberto`).
- `Editor/Setup.cs`: define `IAP_FAKE_STORE_DEFAULT` (a loja falsa do Unity não abre a janela em inglês dela). O `ResolveAndroid()` só age se o Mobile Dependency Resolver estiver no projeto, o que só acontece com o LevelPlay de volta (§4).

**As chaves ficam num lugar só, `client/Assets/_DC/Resources/config.json`** (lidas do arquivo do build; o `config_url` remoto não mexe nelas):

| Chave | De fábrica | Efeito |
|---|---|---|
| `iap_google_play` | `false` | `false`: loja falsa do Unity IAP também no Android (build de teste). `true`: Google Play no Android. PC e Editor usam sempre a loja falsa. |
| `levelplay_app_key` | `""` | Só vale com `DC_LEVELPLAY`. Vazia: simulador, e **nenhuma rede de anúncio é iniciada**. Preenchida: `LevelPlay.Init` no Android (PC e Editor continuam no simulador). |
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

**Fluxo em 2 passos:** COMPRAR → `PurchaseProduct` → a loja cobra → `OnPurchasePending` → `Hud.Concede` (o Core concede o conteúdo fixo e o `Game` grava o save) → `ConfirmPurchase` **só se o Core ficou com o produto** (`sim.PurchaseGranted`: tem o produto ou já concedeu aquele pedido). Se o Core não concedeu (por exemplo, pacote de evento com o quadro fechado), a compra fica sem confirmar e o Google Play devolve o dinheiro sozinho em 3 dias. Pendente de outra sessão (o app fechou no meio) e compra restaurada passam pelo mesmo caminho. Como o Core não concede duas vezes, uma compra repetida só é confirmada. O pacote de evento (consumível) guarda o id do pedido no save: o mesmo pedido reentregue na semana seguinte não vale outra semana. A roupa Explorador que veio no Pacote inicial aparece como comprada (a loja não a vende de novo).

- **Ao abrir:** `FetchPurchases`. Quem reinstalou ou trocou de aparelho recebe de volta as roupas, o Remover anúncios e o Pacote inicial.
- **RESTAURAR COMPRAS:** última linha da loja. No Google Play é o `RestoreTransactions`; na loja falsa, que não tem restaurar, é o `FetchPurchases`. O rodapé avisa "Compras restauradas." ou pede para tentar de novo com internet.
- **Compra adiada** (aprovação dos pais, pagamento pendente): o rodapé avisa e o conteúdo chega quando a loja aprovar.
- **Loja falsa** (PC, Editor e o APK de teste): `UnityIAPServices.StoreController("fake")`. COMPRAR abre a janela "Loja de teste" com o preço "R$ x,xx (teste)", e CONFIRMAR passa pelo mesmo fluxo de 2 passos. A loja falsa do Unity diz "$0.01" para tudo, então o preço mostrado é o da tabela acima, marcado "(teste)". Se o IAP não conectar, a `LojaTeste` assume sozinha.
- **Google Play** (`iap_google_play: true` no Android): o preço é o `localizedPriceString` da loja (moeda e formato do jogador). Antes de os produtos chegarem, aparece "…". Produto que não está na Play aparece como "indisponível" e nada é concedido. **No Google Play a loja de teste nunca assume.**

### 1.2 Anúncios

- **Build atual:** sempre o simulador (`AnunciosTeste`, "ANÚNCIO DE TESTE", 5 s). Sem SDK de anúncio no APK.
- **Com `DC_LEVELPLAY` (§4), a chave e o Android:** `LevelPlay.Init(appKey)`. Depois do init, cria e carrega o rewarded e o interstitial; se a carga falhar, tenta de novo em 30 s. Sem a chave, ou no PC/Editor, continua o simulador.
- **Quando mostrar é sempre do Core.** Rewarded só com `sim.RewardOffer`, e a concessão é `sim.GrantReward`, com id `rw-<contrato>-<tipo>` no save (fechar e reabrir não repete). A recompensa só vale com `OnAdRewarded`, que pode chegar até 1 s depois do `OnAdClosed`. Interstitial só no CONTINUAR do resultado, quando `sim.InterstitialDue` deixa (nunca nos 3 primeiros contratos do dia, no máximo 1 a cada 3 contratos e 4 min, nunca com Remover anúncios, nunca dentro da masmorra) **e** há anúncio carregado (`IntersticialPronto`). Sem anúncio carregado, o jogo segue e o Core não conta nada.
- **Rewarded real sem anúncio pronto:** aparece a janela "Carregando o anúncio…" por até 8 s e depois "Nenhum anúncio disponível agora". FECHAR (ou voltar) cancela sem recompensa.
- Banner: não tem (GDD §7).

### 1.3 AndroidManifest

Medido com o `aapt dump permissions` do SDK Android do Unity, nos três APKs de release. A origem vem do relatório de merge do Gradle (`manifest-merger-blame-release-report.txt`).

| Permissão | Sem SDKs | Com IAP + LevelPlay (`bd10ca9`) | **Build atual (só IAP)** | De onde vem |
|---|---|---|---|---|
| `android.permission.INTERNET` | sim | sim | **sim** | já existia (o Unity põe por causa do `UnityWebRequest` do `config_url`) |
| `android.permission.VIBRATE` | sim | sim | **sim** | o jogo (vibração) |
| `com.android.vending.BILLING` | não | sim | **sim** | Google Play Billing 9.0.0 (Unity IAP) |
| `com.google.android.gms.permission.AD_ID` | não | sim | **não** | `play-services-ads-identifier` (SDK LevelPlay) |
| `android.permission.ACCESS_NETWORK_STATE` | não | sim | **sim** | hoje: `com.google.android.datatransport:transport-backend-cct` 3.1.8, a telemetria do Google Play Billing 9 (com o LevelPlay, também o Unity Ads). Permissão normal, sem dado do jogador |
| `ACCESS_ADSERVICES_TOPICS` / `_ATTRIBUTION` | não | sim | **não** | Unity Ads (Privacy Sandbox) |
| `WAKE_LOCK`, `RECEIVE_BOOT_COMPLETED`, `FOREGROUND_SERVICE` | não | sim | **não** | `androidx.work` (dependência do Unity Ads) |

Componentes que o build atual ganha no manifesto merged (`Library/Bee/.../launcher/build/intermediates/merged_manifest`): as atividades do Billing (`ProxyBillingActivity`, `ProxyBillingActivityV2`), a `GoogleApiActivity` e o agendador do `datatransport` (2 serviços e 1 receptor). Com a loja falsa, o Billing nem é criado. **Nenhum** componente do ironSource ou do Unity Ads, nenhum WorkManager e nenhum `AD_ID` (conferido com `aapt dump permissions` e no merged: 0 ocorrências).

### 1.4 Tamanho do APK de release (ARM64, IL2CPP)

| APK | Bytes | MiB |
|---|---|---|
| Sem SDKs (mesmo código e arte, 2026-10-11) | 56.889.340 | 54,3 |
| Com Unity IAP + LevelPlay + Unity Ads (`bd10ca9`) | 68.595.132 | 65,4 |
| **Build atual: só Unity IAP** | **67.053.160** | **63,9** |

Do APK sem SDKs para o atual são +10,2 MB. Cerca de 2,2 MB disso não é SDK: são as folhas novas das roupas Mineiro e Coveiro (raia G), já no `Resources`; os dados do jogo foram de 32,4 para 34,6 MB comprimidos. O resto, cerca de 8 MB, é o IAP:

- `libil2cpp.so`: 38,7 → 58,4 MB sem compressão. É o C# do IAP, do Services Core e do Newtonsoft.
- `classes.dex`: 6,4 → 8,7 MB. É o Billing e o `datatransport`.
- `global-metadata.dat`: 4,7 → 8,0 MB.

Tirar o LevelPlay economizou pouco no C# (a maior parte do IL2CPP é do IAP) e cerca de 2,9 MB no Java. Se o tamanho pesar, suba o *Managed Stripping Level* do IL2CPP e teste a loja de novo.

## 2. O que falta do lado do Vinicius

1. **Play Console (compras):**
   - Criar os 9 produtos do §1.1 com **esses ids**, em "Produtos no app", com preço e ativos. A Play não separa consumível de não consumível: quem consome o pacote de evento é o app.
   - Cadastrar testadores de licença.
   - Subir um AAB de teste interno com `iap_google_play: true`. A Play só entrega produtos para um app publicado numa faixa de teste e assinado com a chave de upload.
   - Recomendado depois: a chave de licença da Play (Monetize > Licensing) no projeto Unity Cloud e a validação de recibo (`CrossPlatformValidator`). Hoje a compra é aceita como a loja entrega, sem validação local.
2. **Conta LevelPlay**, para quando os anúncios voltarem (§4):
   - Criar a conta em platform.ironsrc.com e adicionar o app Android `br.com.vstack.dungeoncourier`.
   - Copiar o **App Key** e criar 2 unidades, **Rewarded** e **Interstitial**.
   - Publicar o `app-ads.txt` no site da V-STACK (o painel do LevelPlay dá o conteúdo).
   - AdMob como rede extra é opcional.
3. **Política de privacidade publicada:** o item 8 (compras) do `LOJA.md` §4.5 já vale. O item 7 (anúncios) entra só junto com o LevelPlay.
4. **Decisão jurídica (ECA Digital, Lei 15.211/2025), antes de religar os anúncios:** o público é 13+ e inclui adolescentes, e a lei restringe publicidade com perfilamento para menores. Decidir com quem cuida do jurídico se o jogo pede anúncios não personalizados para todos ou se pergunta a idade. Do nosso lado, os dois casos são uma chamada antes do `LevelPlay.Init`, em `AnunciosLevelPlay`: por exemplo, `LevelPlay.SetMetaData("is_child_directed", "true")` ou as opções de `LevelPlayPrivacySettings`.

## 3. Como testar

- **PC (Editor ou build Windows):** loja → COMPRAR → "Loja de teste" → CONFIRMAR. O Core concede e a linha vira COMPRADO. No log aparecem "DC IAP: 9 produtos (loja falsa)" ao abrir e "iap_purchase" no diário. RESTAURAR COMPRAS → "Compras restauradas." Anúncios: o simulador de 5 s.
- **Teste automático do caminho do dinheiro (PlayMode):** `Assets/_DC/Tests/PlayMode/LojaIapTests.cs`.
  - O teste sobe o Game de verdade e compra a roupa Noturno na loja falsa. A compra fica pendente, a `LojaIap` concede pelo Core e só então confirma; depois o `FetchPurchases` devolve a compra.
  - Comando: `Unity -batchmode -projectPath client -runTests -testPlatform PlayMode -assemblyNames DC.PlayTests -testsession -testResults r.xml`.
  - Sem `-testsession` o teste é ignorado, para não mexer no save do Editor.
  - Os testes EditMode (`DC.Tests`) não dependem de SDK: a loja do IAP não existe fora do Play Mode.
- **Fotos:** `DungeonCourier.exe -testsession -dev-desbloqueia -tela loja -shot loja.png` e `-tela compra -shot compra.png` (540×960). As de referência estão em `client/Builds/validation_v2/shots/sdk_tela_loja.png` e `sdk_tela_compra.png`.
- **APK de teste (como está):** loja falsa e simulador. Sem conta, sem SDK de anúncio e sem `AD_ID`.
- **Compras reais:** faixa de teste interno da Play, conta de testador de licença (a compra não cobra) e `iap_google_play: true`. Teste: comprar uma roupa; desinstalar e reinstalar (a roupa volta ao abrir); RESTAURAR COMPRAS; comprar o pacote de evento com o quadro de eventos aberto.

## 4. Religar o LevelPlay (quando houver conta e decisão do §2.4)

**Estado em 2026-10-12: a branch `feat/levelplay` já tem os passos 1, 2 e a privacidade do 5.** Ela traz:
- o pacote 9.5.1 com o Unity Ads e o resolver;
- o define `DC_LEVELPLAY` no Android e o `Unity.LevelPlay` no asmdef e no viewcheck;
- anúncio não personalizado para todos antes do `Init`: `LevelPlayPrivacySettings` com GDPR sem consentimento, CCPA com opt-out e COPPA desligado (público 13+);
- `Setup.SemIdDePublicidade`, que tira do APK o `AD_ID` e as 3 permissões do Privacy Sandbox.

Conferido com `aapt`. O APK fica com INTERNET, VIBRATE, ACCESS_NETWORK_STATE, BILLING e o WorkManager do SDK (WAKE_LOCK, RECEIVE_BOOT_COMPLETED, FOREGROUND_SERVICE), sem AD_ID, e tem 67,7 MiB. **Falta só o passo 3** (as 3 chaves da conta do Vinicius no `config.json`), depois o 5 na Play Console e o 6. Sem chave, o jogo segue no simulador e não chama o `Init`.


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
   - Preencher o formulário de segurança dos dados com a tabela do Unity Ads (`LOJA.md` §4.5). O item 7 da política já está publicado nos 4 idiomas (2026-10-13).
   - Decisão do ECA Digital (§2.4): anúncio não personalizado para todos, já aplicado antes do `Init`.
6. **Validar:**
   - Num build de desenvolvimento, use o Test Suite: `LevelPlay.SetMetaData("is_test_suite", "enable")` antes do `Init` e `LevelPlay.LaunchTestSuite()` no `Iniciou`. **Tire as duas linhas antes de publicar.**
   - No `adb logcat`, procure "DC LevelPlay: iniciado".
   - Em modo avião, o rewarded deve mostrar "Nenhum anúncio disponível agora" e o CONTINUAR deve seguir sem interstitial.
