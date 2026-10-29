# Lições do Dungeon Courier para os outros jogos do estúdio

Playbook escrito em 2026-10-16, quando o Dungeon Courier chegou à 1.4.1-beta: GDD inteiro feito, arte v2, tutorial guiado, anúncios e compras integrados, AAB assinado e política publicada. Falta só a conta da Play Console, que espera o D-U-N-S.

Cada lição foi paga com tempo, crédito ou retrabalho. O formato é **o que fazer**, **por quê** (o que aconteceu no DC) e **reaproveitar** (arquivo do repositório `Vvs2705/dungeon-courier` que dá para copiar ou adaptar).

Para começar um jogo novo, use o prompt pronto em [`PROMPT_MESTRE_NOVOS_JOGOS.md`](PROMPT_MESTRE_NOVOS_JOGOS.md), que resume este playbook com a autorização permanente.

Ideia central: **um jogo novo do estúdio não começa do zero.** O que dá para copiar está nas tabelas da §12. O que não se copia, mas se repete, está nas regras de processo da §1.

---

## 1. Processo e ritmo

1. **Contrato da versão antes de codar.** Escreva um `CONTRATO_Vx.md` com o escopo, os números iniciais e o critério de pronto, e uma matriz "GDD × feito" (`CONTRATO_LANCAMENTO.md` §1). Só dê o jogo por pronto quando todas as linhas estiverem feitas ou explicitamente dependendo de alguém.
   - **Por quê:** no DC, com o contrato da v0.1 e da v1 escritos antes, o núcleo e a View andaram em paralelo sem quebrar a API.
2. **Uma branch e um PR por assunto**, com commits em português com escopo (`feat(dc-tutorial): ...`).
   - **Merge com CI verde.** CHANGELOG em `[Unreleased]`; na versão, vira `[x.y.z-beta]` e a pré-release no GitHub leva os anexos.
3. **Memória de retomada.** Mantenha um `STATUS_<jogo>.md` na memória do estúdio com um bloco "CONTINUAR DAQUI" a cada pausa: o que está pronto, o que falta e os comandos.
   - **Por quê:** o Vinicius retoma dizendo só "vamos continuar"; sem isso, cada sessão reabre a investigação.
4. **Um processo pesado por vez** (Unity OU Blender OU emulador). No PC de 7,7 GB, dois juntos travam ou o sistema mata o Blender.
   - **Rotina:** render em segundo plano e Unity depois.
5. **Fotos antes do PR.** O coordenador olha as fotos de cada tela (`-shot`) antes de abrir o PR.
   - **Por quê:** no DC, as fotos acharam o pouso antes do 1º toque, o texto sobreposto, o "!!" em cima de "ladrão!" e o joystick tampando o personagem. Nenhum desses aparecia nos 169 testes verdes.
6. **Arte crua fora do git, mas com backup.**
   - Ficam fora: os zips do Tripo, os FBX do Mixamo e os `.blend` com modelo pago dentro. O repositório é público e a licença não permite redistribuir.
   - Ficam no checkout principal, não no worktree, que some quando é apagado.
   - Vão para um drive privado.
   - O que é só imagem de interface (logo, ícones, retratos) entra no git (`arte/logo`, `arte/ui`, `arte/retratos`).
7. **Créditos do Tripo:** confira o saldo antes e depois de cada geração e registre em `docs/PROVENIENCIA.md`.
   - Na 1ª peça de cada sessão, confira a receita do painel (o Tripo já voltou com 8K, textura 4K e 2 M polígonos).
   - Ordem de grandeza no DC: modelo HD **45**, Auto Rig **20**. O gerador de imagens (Nano Banana 2) é **grátis**.

## 2. Arquitetura de código (Unity 6000.3.23f1, C#)

1. **Núcleo em C# puro e determinístico.** O `Scripts/Core` não depende do Unity: passo fixo de 1/30 s, xorshift com semente, `Sim` como fachada.
   - Testado por `dotnet test client/tools/coretests` sem abrir o Unity.
   - A View só desenha o estado e lê os eventos (`Sim.Events`).
   - **Por quê:** o DC tem 169 testes rodando em 2–3 minutos; bot e balance rodam fora do Unity.
   - **Reaproveitar:** a estrutura `Core/` + `View/` + `Editor/Setup.cs` + `tools/coretests` + `tools/viewcheck` (o mesmo padrão já usado no Forge Street e no Rune Relay).
2. **API do núcleo documentada no topo do `Sim.cs`**, para os workers paralelos não quebrarem a View.
3. **Bot e autoplay.** Um `Bot` joga o próprio jogo, e `-autoplay N` roda N contratos sem gráfico e imprime a economia.
   - Serve para o balance (`BALANCE.md`), para o portão e para os criativos.
   - **Atenção:** a duração real da partida se mede com gente (diário), não com o bot.
4. **Diário CSV de playtest** (`Game.Log` e `Exportar`) com eventos do jogo, sem dado pessoal, exportado pelo jogador. Relatório: `tools/diario_report.py`.
5. **Tabelas da View indexadas por enum, com reserva procedural**, para que um enum novo sem linha não quebre com `IndexOutOfRange`.
6. **Flags de teste no build** (todas só com `-testsession`, que não lê nem grava save nem diário e usa semente 1):

   | Flag | O que faz |
   |---|---|
   | `-tela <nome>` | abre direto em qualquer tela |
   | `-contract K` | entra no contrato K |
   | `-room S` | entra na sala S |
   | `-bot` | o bot joga |
   | `-guilda` | compra tudo na memória (fotos dos chefes) |
   | `-lang pt\|en\|es\|id` | idioma das fotos |
   | `-dev-desbloqueia` | campanha toda aberta |
   | `-shot arq -shotdelay s` | foto |
   | `-record pasta -recordfps -recordsecs -recordfrom` | grava quadros com `Time.captureFramerate`, determinístico |
   | `-tutorial` | liga o tour; com o `-bot`, o ENTENDI se aperta sozinho |
   | `-adtestsuite` | abre o Test Suite do LevelPlay |

   **Por quê:** com elas, fotos da loja, criativos e QA saem do jogo real sem tocar no save do jogador.
7. **`Setup.cs` por script.** Settings, versão → versionCode, builds Windows/Android/AAB, ícones e manifesto sem `AD_ID`, tudo idempotente e em batch (`-executeMethod`).
8. **Textos em 4 idiomas num lugar só** (`Textos.cs`): `L(pt, en, es, id)`, frase inteira por idioma, números por `{0}`, só Latin-1.
   - Todo texto fixo é registrado com `Loc()` para reaplicar na troca de idioma.
9. **Áudio sintetizado** (`tools/audio_gen.py`): sem asset de terceiros, sem licença a rastrear.

## 3. Portão de qualidade (antes de todo PR)

`dotnet test coretests` · `dotnet build viewcheck` (compila a View fora do Unity) · Unity EditMode · build Windows · `-autoplay 30` com 0 exceções · fotos 540×960 conferidas.

- **NUnit diferente:** o dotnet usa NUnit 3.14 e o Unity usa 3.5 (tupla `int × long` passa num e falha no outro). Rode o EditMode antes do PR.
- **viewcheck em worktree novo** precisa das DLLs do `Library/ScriptAssemblies` (UnityEngine.UI, InputSystem, LevelPlay) copiadas.

## 4. Pipeline de arte: Tripo → Mixamo → Blender → sprites 2D

1. **Ordem:**
   1. imagem grátis (Nano Banana 2, 4 opções);
   2. escolher pela leitura no ângulo do jogo (60°);
   3. modelo HD (45);
   4. Auto Rig Mixamo (20, pela URL `workspace/rigging/<task>`);
   5. FBX com predefinição Mixamo 2K (65 ossos `mixamorig:`).
2. **Item separado do personagem** (regra do Vinicius): arma, escudo, cajado e ferramenta são gerados à parte e presos ao osso no Blender (`render_sprites.py --attach item:Osso:pos:rot:escala`). Roupa e bolsa vestidas contam como corpo.
   - **Por quê:** quando o Tripo gera junto, ele funde item e mão, e o rig arrasta o item errado.
3. **Animação reaproveitada.** Clipes do Mixamo servem a todos os personagens com o esqueleto `mixamorig:`, e servem entre jogos: o DC reaproveitou clipes do ARKANA e do Forge Street (tabela em `PROVENIENCIA.md` §3).
   - Clipe que o Mixamo não tem se anima direto no Blender (`tools/anim_blender.py`: golpe, sono, emergir, mergulhar, patada).
   - **Retarget por delta de rotação sobre o repouso**, nunca action crua entre esqueletos.
4. **`render_sprites.py`** (reaproveitar inteiro):
   - presets por personagem e 4/8 direções + espelho;
   - luz chapada com contorno; `meta.json` com pivô e âncora da mão por quadro (`--hold`);
   - `--align-arms` para rig em pose A; contact sheet por folha; teto de quadros;
   - `timeout` em todo render (um render travou 15 h com o PC suspenso).
5. **Nitidez e tamanho.** Se o personagem parecer pequeno ou borrado, re-renderize com `--hd K` (mais px por metro) e `--escala E` (a View mostra E× maior; `SpriteSheet.Escala` divide o ppu, então sprite, âncora e barra de vida acompanham).
   - Regra: `K = min(1,5; 4096 / (quadros × célula))`, porque a folha não passa de 4096 px.
   - Comandos por personagem num script (`tools/elenco_hd.sh`).
6. **Ajuste de forma no Blender em vez de gerar de novo** (0 crédito). Exemplos do DC:
   - aba do chapéu ×0,78 (`roupas_courier.py`);
   - saco do ladrão ×0,7, achando os vértices pela cor da textura (`saco_ladrao.py`);
   - tampa do baú que abre de verdade (`bau_abrindo.py`, corte com bmesh bisect e dobradiça).
7. **Roupas e variantes por troca de cor**, sem crédito: `roupas_cor.recolor` muda matiz, saturação e brilho por faixa de cor. Com luz chapada, dá para trocar direto na folha renderizada (`roupas_2d.py`), sem Blender.
8. **Cache do import:** o FBX do Tripo leva mais de 10 min para importar. Salve um `.blend` e renderize a partir dele.
9. **Arte de interface pintada** no mesmo gerador grátis, sobre fundo azul-marinho liso, recortada por `tools/ui_assets.py` (fundo pela cor da borda):
   - logo;
   - 20 ícones numa folha 4×2 ou 4×3, cortada em células;
   - fundo do menu;
   - retratos do cliente e dos chefes.

   **Por quê:** o retrato pintado no estilo do logo ficou muito mais vistoso que o render 3D recortado.

## 5. Interface e marca

1. **UI sem PNG de moldura:** sprites 9-fatias gerados por código e tingidos pelo `Image.color` (`Art.cs`):
   - `Botao` (contorno, degradê, aba de relevo);
   - `Moldura` (madeira com rebites);
   - `Pergaminho`;
   - `Contorno` (destaque do tour);
   - `Balao` (fala).

   Mais: verniz no botão, `Aperta` (escala 0,95 no toque) e `Letreiro` (contorno + sombra no texto).
2. **Fontes OFL** (Lilita One para títulos e botões, Nunito ExtraBold no corpo), em `Resources/Fonts`, com a licença junto.
3. **Ícone pintado ganha medalhão claro** atrás; o disco escuro fica só para silhuetas procedurais.
   - Retrato de personagem para ícone: recorte do quadro em volta do ator (`SpriteSheet.Retrato`, medindo o bbox no contact sheet).
4. **CanvasScaler** com referência 1080×1920 e casando a largura; `AreaSegura` para notch; `Dp` para alvo de toque de 48 dp.
5. **Telas 20:9:** preencha fora da área de jogo com o cenário escurecido (no DC, a pedra da região continua até a borda). Faixa lisa parece bug.
6. **Ícone do app:** adaptativo do Android, com fundo + frente e o personagem a no máximo 0,64 da camada. O launcher mostra o meio 2/3 e corta em círculo; confira nas máscaras círculo, squircle e quadrado (`tools/icone_app.py`, `Setup.Icones`).
   - O Unity 6 marca redondo e legado como obsoletos com API 26+.
   - O ícone da loja (512) é o mesmo do aparelho.
   - Confira no APK com `aapt2 dump badging` (ícone = `.xml` adaptativo).

## 6. Onboarding (FTUE)

1. **Tutorial guiado ("coach marks") é obrigatório** (pedido do Vinicius): a tela congela, só o alvo fica aceso (véu com buraco, contorno dourado e seta), e um cartão diz o que é, quando usar e por quê.
   - Nos passos de botão, só o botão aceso responde (anel pulsando).
   - Cada passo aparece uma vez (PlayerPrefs), com Pular tutorial e REVER TUTORIAL nas configurações.
   - Fica desligado no `-testsession`, para não estragar fotos e gravações.
   - **Reaproveitar:** `View/Tour.cs` é genérico. Os gatilhos leem o estado da tela e do núcleo; troque só a lista de passos e os textos.
2. **Passo de botão se cancela se a tela mudar.**
   - **Por quê:** no DC, a gravação com o bot pegou um passo preso sem alvo que congelava o jogo depois do resultado.
3. **Contexto sem cutscene.** Num jogo de partida curta, uma cutscene de introdução atrasa o 1º toque e soma com o tutorial (parede de texto).
   - O DC usa um **cartão do personagem que pede a missão** (retrato pintado + balão de fala), no lugar do 1º cartão do tutorial. Fecha um ciclo com a cena de entrega.
   - Para nome em inglês, traduza a palavra uma vez ("o courier, o entregador da Guilda").
4. **Controle de um dedo sem desenho na tela por padrão.** O joystick flutuante desenhado no dedo tampava o personagem. O controle é o mesmo, sem desenho, com a opção "Controle na tela" nas configurações (`Joystick.Visivel`).
5. **Ensine o verbo central antes da 1ª escolha.** No DC: arrastar com a carga → soltar para pousar e lutar → 1ª bifurcação. Pouso só depois do 1º movimento na sala, para não pousar sozinho antes do 1º toque.

## 7. Monetização, privacidade e compliance (Brasil + Play)

1. **Guardrails do estúdio:**
   - 1 moeda no MVP;
   - **nada aleatório pago** (ECA Digital, Lei 15.211/2025): conteúdo fixo e listado na loja;
   - rewarded sempre opcional;
   - interstitial desligado no começo;
   - offline com teto de 2 h.
2. **Anúncio não personalizado para todos.** `LevelPlayPrivacySettings` com GDPR sem consentimento, CCPA opt-out e COPPA desligado, antes do `Init`.
   - **Sem `AD_ID`:** `Setup.SemIdDePublicidade` tira `AD_ID` e as 3 permissões do Privacy Sandbox do manifesto.
   - Na Play: declaração de ID de publicidade = **não**; segurança dos dados pela tabela do Unity Ads (`LOJA.md` §4.5).
3. **LevelPlay:**
   - app e ad units na conta da V-STACK; chaves no `config.json` (não são segredo);
   - sem chave, o jogo cai no simulador e não chama o `Init`;
   - Test Suite só em build de desenvolvimento;
   - para ligar o Unity Ads, o painel pede a **chave da Monetization Stats API** (o Vinicius cria e cola) e o **core ID da organização** Unity.
   - **Reaproveitar:** `AnunciosLevelPlay.cs`, `Anuncios.cs` (interface + simulador) e o passo a passo em `MONETIZACAO.md` §4.
4. **IAP:** Unity IAP 5.4.3 atrás de uma interface, com loja falsa no PC.
   - **Compra em 2 passos:** o núcleo concede e só então confirma, para não perder compra se o app fechar.
   - Restaurar compras; produtos de conteúdo fixo.
   - **Reaproveitar:** `Loja.cs` e `LojaIap.cs`.
5. **Política de privacidade pública** no GitHub Pages (branch `gh-pages`, um `index.html` com 4 idiomas). Cite exatamente as redes usadas (LevelPlay, Unity Ads, ironSource Ads). O texto de privacidade do jogo só cita anúncios quando o build tem anúncio real.

## 8. Play Console e lançamento

1. **D-U-N-S cedo.** A conta de **organização** (recomendada para credibilidade e para contratar depois) exige D-U-N-S. O grátis (CIAL) leva ~30 dias úteis.
   - **Peça no começo do projeto**, não no fim. No DC, tudo ficou pronto e a conta espera o D-U-N-S.
   - O plano pago (~R$ 1.700/ano) não compensou.
2. **Chave de upload** fora do git (`%USERPROFILE%/.<jogo>/`), com **backup fora do PC**. Se perder a chave, é preciso pedir reset ao suporte.
3. **AAB assinado** por `Setup.BuildAndroidBundle`. Conferido no DC: targetSdk 36, bibliotecas nativas alinhadas a 16 KB, Google Play Billing 9.
4. **Página da loja e criativos automatizados**, refeitos sempre que a arte muda:
   - `tools/loja_fotos.sh` tira as 8 capturas × 4 idiomas no build de release (sem "Development Build"), de quadros do `-record`;
   - `tools/loja_assets.py` gera ícone 512, feature graphic 1024×500 e capturas 1080×1920;
   - `tools/criativos.py` monta 13 vídeos 9:16 × 4 idiomas com legenda e CTA, do jogo real, via ffmpeg.
   - **Regra do estúdio:** criativo só de gameplay real.
5. **Pré-release no GitHub** a cada versão, com APK, AAB, zip Windows e pacote da loja/criativos.

## 9. Android, build e aparelho

1. **`QualitySettings.vSyncCount = 0` e `Application.targetFrameRate = 60`** no início.
   - **Por quê:** o POCO F4 foi de 28–36 para 55 fps (a qualidade Medium prendia ao vsync de 120 Hz).
2. **APK de worktree via `subst R: <client>`.** Caminho longo com espaço quebra o CMake do Gradle (`prefab_command.bat`). Desfaça com `subst R: /D` e `MSYS_NO_PATHCONV=1` no Git Bash.
3. **adb no Git Bash:** `adb shell "am start -n <pkg>/<act> -e unity '-a -b'"`, com aspas por dentro. Sem elas, só o 1º argumento chega ao jogo.
4. **Só o APK atual:** o build apaga os anteriores.
5. **Celular do Vinicius:** pergunte "pode testar" antes de usar e tire foto só com o jogo em primeiro plano (notificação pessoal aparece por cima; apague a foto se aparecer).
6. **Desempenho se mede em aparelho original.** O POCO de 4 anos, com tela, bateria e cabo paralelos, se limita depois de 1 min, até no menu (CPU do jogo em 7%, GPU com teto baixo). Não sirva de régua.
7. **Folhas HD aumentam o APK** (DC: 67,7 → 83 MiB). Fique abaixo do limite da Play e meça a memória (PSS) no aparelho.

## 10. Armadilhas já pagas (copie a solução)

- **Unity sobrescreve o `Packages/manifest.json`** com Purchasing/Analytics na criação do projeto: confira logo depois.
- **Tripo no Chrome:**
  - no máximo 2 abas (WebGL come RAM);
  - o 1º clique em "Gerar" numa aba de fundo não dispara: clique fora da caixa antes;
  - Ctrl+A numa caixa vazia digita "a";
  - downloads vão para `Downloads` como `image_N.png`.
- **TaskStop no Claude Code** mata só o shell de fora: um laço `for` de render continua. Mate o laço, não só o processo.
- **Caminho longo:** um worktree no scratchpad estoura o MAX_PATH. Use `%TEMP%\curto` ou um drive `subst`.
- **Python do Windows não enxerga o `/tmp` do Git Bash:** scripts no scratchpad com caminho Windows.
- **PlayerPrefs:** não leia em inicializador de campo de MonoBehaviour (erro do Unity); guarde em cache estático.
- **Conflito de nome `Rect`** entre o Core e o Unity: use `using Rect = UnityEngine.Rect;`.

## 11. O que entra no GDD de cada jogo (checklist)

- [ ] Contrato da 1ª versão e matriz GDD × feito.
- [ ] Núcleo C# puro com testes dotnet, bot e `-autoplay`, diário de playtest.
- [ ] Flags de teste (`-testsession`, `-tela`, `-shot`, `-record`, `-bot`, `-lang`, `-tutorial`).
- [ ] Lista de personagens e itens separados por lote, com o reaproveitamento de clipes do Mixamo, ARKANA, Forge e DC.
- [ ] Plano de arte de interface pintada (logo, ícones, retratos) pelo gerador grátis.
- [ ] Tutorial guiado (lista de passos: o que, quando, por quê) e cartão de contexto em vez de cutscene.
- [ ] Controle sem desenho que tampe o personagem, ou justificativa se o gênero pedir um.
- [ ] Monetização dentro dos guardrails (nada aleatório pago, anúncio não personalizado, sem `AD_ID`).
- [ ] Pedido do D-U-N-S e conta da Play no início; chave de upload com backup.
- [ ] Página da loja e criativos gerados do build (scripts do DC).
- [ ] Android: vSync 0 / 60 fps, ícone adaptativo, telas 20:9 preenchidas, teste em aparelho original.

## 12. Mapa do que copiar do Dungeon Courier

| Arquivo (repo `dungeon-courier`) | Para quê | Adaptar |
|---|---|---|
| `client/Assets/_DC/Editor/Setup.cs` | settings, builds, AAB, ícones, manifesto sem AD_ID | nomes, appId, versão |
| `client/Assets/_DC/Editor/SpriteImport.cs` | import de sprites e da UI (ASTC no Android) | pastas |
| `View/Art.cs` | sprites procedurais (9-fatias, glow, raios), fontes, botões, `Letreiro`, `Ajustes` | paleta |
| `View/Tour.cs` | tutorial guiado (coach marks) | passos e gatilhos |
| `View/Joystick.cs` | controle de 1 dedo (arrastar, toque rápido), invisível por padrão | raio, zona morta |
| `View/SpriteSheet.cs` | folhas do `render_sprites.py` (clipes, direções, âncoras, escala, retrato) | — |
| `View/AreaSegura.cs`, `View/Dp.cs` | notch e dp | — |
| `View/Anuncios.cs`, `View/AnunciosLevelPlay.cs` | rewarded/interstitial + simulador + LevelPlay | chaves, regras |
| `View/Loja.cs`, `View/LojaIap.cs` | IAP com loja falsa e 2 passos | produtos |
| `View/Exportar.cs` + `tools/diario_report.py` | diário de playtest | eventos |
| `View/Textos.cs` | padrão de localização em 4 idiomas | textos |
| `tools/render_sprites.py`, `render_sprites.ps1`, `folhas.py`, `anim_blender.py` | 3D → sprites 2D, clipes no Blender | presets |
| `tools/elenco_hd.sh` | re-render do elenco maior e nítido | lista |
| `tools/roupas_cor.py`, `roupas_2d.py`, `roupas_courier.py` | variantes por cor e ajuste de malha | faixas de cor |
| `tools/ui_assets.py`, `tools/icone_app.py` | recorte de logo, ícones e retratos; ícone adaptativo | cortes |
| `tools/loja_fotos.sh`, `loja_assets.py`, `criativos.py` | página da loja e criativos | capturas, roteiros |
| `tools/audio_gen.py` | áudio sintetizado | sons |
| `tools/coretests`, `tools/viewcheck` | portão fora do Unity | csproj |
| `docs/PROVENIENCIA.md`, `ASSETS.md`, `ART_BIBLE.md`, `MONETIZACAO.md`, `LOJA.md`, `CRIATIVOS.md`, `TESTE_JOGADORES.md` | modelos de documento | conteúdo |
