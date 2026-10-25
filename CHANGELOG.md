# Changelog — Dungeon Courier

Formato: [Keep a Changelog](https://keepachangelog.com/pt-BR/1.1.0/); versões em [SemVer](https://semver.org/lang/pt-BR/).

## [Unreleased]
### Documentação
- **Auditoria do GDD contra o código** (2026-10-25): o GDD ganhou o §0 "Estado real", com cada seção, o que foi feito, como e onde está no código, e as divergências (guilda com 8 níveis em vez de 5, 63 salas, FTUE com o ovo no C3, `seed_code_entered` → `code_start`, mascote descartado). Notas curtas nas seções que o jogo mudou.
- Planos e relatórios já cumpridos (`CONTRATO_V01`, `CONTRATO_V1`, `CONTRATO_LANCAMENTO`, `CONTEUDO_REGIOES`, `BALANCE_REGIAO2-4`, `ASSETS`, `VIEW_SPRITES`, `VIEW_V1_MUNDO`) foram para `docs/historico/`, com um índice; o código continua citando-os pelo nome.
- `MONETIZACAO.md` reescrito no topo: dizia "LevelPlay desligado", mas os anúncios estão ligados no Android desde a 1.3.0-beta (versões, chaves, manifesto e tamanho do APK atuais). README, CLAUDE.md, `client/README.md`, `MAPA_CODIGO.md` (linhas, `verify.sh`), CONTRIBUTING, modelo de PR, `RENDER_SPRITES.md` (elenco HD), `ART_BIBLE.md`, `UI_V1.md` e `COMPETITIVO.md` atualizados. O termo do playtest dizia "o jogo não usa a internet"; agora cita os anúncios.

### Removido
- Fotos de validação `client/Builds/validation_v01`, `_v1`, `_v2` e `_v3` (145 PNGs, ~40 MB, todas de antes da arte v2; continuam no histórico do git), inclusive o `shots$name.png` gerado por engano.
- `client/tools/regioes_check.py`: cópia em Python dos dados de desenho das regiões 2–4 para checar o level design antes de existir o C#. Os testes `LevelDesign_*` do núcleo checam as salas de verdade.

### Mudado
- **Unity 6000.3.27f1 LTS** (era 6000.3.23f1), com o Input System 1.20.1 que vem junto. `verify.sh` e o viewcheck apontam para o editor novo. No PC ficou um Unity Hub só, o do site da Unity, que tem linha de comando para instalar editores.

## [1.4.3-beta] - 2026-10-22
### Corrigido
- **Inimigo grande preso na quina da sala** (feedback do Vinicius no celular): o mapa de caminhos era do tamanho do courier e mandava o Bruto (1,1 m) por frestas de 1 m entre bloco e parede. Agora cada tamanho de corpo tem o seu (`Run.NavFor`), e um teste confere todas as salas. Valia também para o Ogro, o Golem e o Dragão nas salas de chefe.
- O dash cancela o aviso do Mineiro e do Sapo (antes o golpe caía mesmo assim). A Bota de ferro não congela mais o vagonete.
- A mira automática atira primeiro em quem foge com a carga.
- Toques não empilham: depois de um toque (morcego, limo), outro só tira vida 0,5 s depois. Quatro morcegos juntos tiravam 4 de vida de uma vez.
- O Necromante na fase 2 errava quem ficava parado (raios gêmeos abertos demais); agora fecham a ±7°.

### Mudado
- **Guarda sentinela:** em vez de ficar parado esperando morrer, o guarda que leva tiro sai do posto atrás do courier e, fora dos tutoriais, avisa e investe (empurrão de escudo). Errou, fica atordoado: é a hora de atirar. Quem só passa andando encara o guarda no posto, como antes. Animação de marcha nova com o escudo firme.
- Mineiro e Kobold arremessam antes de fugir: ficar colado neles deixou de ser seguro. A língua do Sapo alcança o courier a 4,4 m.
- Salamandra para e encara antes do corte (era sem aviso). Aranha e Diabrete avisam um pouco antes.
- Gárgula com 6 de vida e mais tempo pousada; Gárgula e Toupeira sem escala de vida por nível (só ficam vulneráveis em janelas curtas). Dragão com 42 de vida (era o chefe que mais matava).
- Caveiras das portas refeitas pelo dano medido: R8 3 → 2, R12 1 → 2, R2-5 1 → 2.
- **Anúncios:** SDK do ironSource 9.7.0 e adaptador do Unity Ads 5.16 no LevelPlay (antes 9.6.2 e 5.15).

## [1.4.2-beta] - 2026-10-21
### Adicionado
- **Cartão da carga nova no tutorial:** no 1º contrato com cada carga (relíquia, ovo, bolo, mensagem, caixa pesada, barril e as 8 das regiões 2-4) a tela congela sobre a carga e um cartão explica a regra dela e o que fazer (4 idiomas). Antes, só a faixa de dica e a linha do quadro explicavam. Pular tutorial também pula esses cartões; REVER TUTORIAL mostra de novo.
- **Portão em um comando:** `client/tools/verify.sh quick|full` (núcleo, viewcheck, EditMode, build Windows e `-autoplay 30`), com logs em `client/Builds/verify/` e só OK ou o erro na saída; mesmo nome e uso do Forge Street.
- **Mapa do código** (`docs/MAPA_CODIGO.md`): para quê e pontos de entrada de cada arquivo do Core, da View, do Editor, dos testes e das ferramentas, para ler o trecho certo em vez do arquivo inteiro.

## [1.4.1-beta] - 2026-10-16
### Mudado
- **Joystick invisível por padrão:** o controle é o mesmo (arrastar anda, soltar pousa, toque rápido = dash), mas nada é desenhado no dedo; o desenho tampava o courier (feedback do Vinicius). Configurações > "Controle na tela" mostra de novo.

## [1.4.0-beta] - 2026-10-16
### Adicionado
- **Tutorial guiado** (`View/Tour.cs`): a tela congela, só o alvo fica aceso (seta, anel e contorno dourado) e um cartão diz o que é, quando e por quê. Na 1ª partida: você, arrastar, vida, carga e pausa; na 1ª vez de cada coisa: pousar para lutar, ladrão, dash, portas, baú e cliente; nos menus: JOGAR, o 1º contrato, estrelas, vantagem, guilda e o giro pelo menu. Passo de botão: só o botão aceso responde. ENTENDI, Pular tutorial e REVER TUTORIAL nas configurações. Desligado no `-testsession` (fotos, gravações) e no `-bot`; `-tutorial` liga.
- **Cartão do cliente** no 1º passo do tutorial: o cliente pede a caixa num balão de fala (quem contratou e o que pede) e "courier" é apresentado como o entregador da Guilda, sem cutscene.
- **Retratos pintados no estilo do logo** (Tripo, 0 crédito): o cliente do cartão e os 4 chefes de região no mapa (silhueta enquanto a região está fechada), no desafio sem fim e na temporada (`arte/retratos`, `ui_assets.py`).

## [1.3.0-beta] - 2026-10-16
### Adicionado
- **LevelPlay 9.5.1 + Unity Ads de volta no Android** (`DC_LEVELPLAY`): anúncio não personalizado para todos (GDPR sem consentimento, CCPA opt-out, COPPA desligado) e APK/AAB sem `AD_ID` nem Privacy Sandbox (`Setup.SemIdDePublicidade`). Sem as chaves da conta no `config.json`, segue no simulador.
- **AAB assinado com a chave de upload** (`Setup.BuildAndroidBundle`; chave e senhas fora do git, em `%USERPROFILE%/.dungeoncourier/`). Conferido: targetSdk 36, bibliotecas nativas alinhadas a 16 KB, Google Play Billing 9.0.0.
- **Interface v2:** logo novo (Tripo), fundo de masmorra, ícones pintados no menu e na guilda, fontes Lilita One + Nunito ExtraBold (SIL OFL), botões com relevo, cartões com moldura de madeira e pergaminho, HUD da partida no estilo novo e blocos de obstáculo com a pedra da região.
- **Ícone do app no celular** (antes, o ícone padrão do Unity): adaptativo do Android com o mascote do logo no arco da masmorra, e o ícone da loja igual (`client/tools/icone_app.py`, `Setup.Icones`).
- **Cena da entrega nova:** o baú do Tripo abre de verdade (10 quadros), com luz, raios, moedas pulando e a carga subindo; cliente nítido em 3/4. Tela de teste `-testsession -tela cena`.
- **Elenco maior e nítido:** 30 folhas re-renderizadas com mais pixels por metro e mostradas 25% maiores (`meta.json` `escala`; `SpriteSheet.Escala` divide o ppu, então sprite, âncoras e barra de vida acompanham). Aba do chapéu do courier menor (×0,78), saco do ladrão menor e as 6 roupas por troca de cor direto na folha (`roupas_2d.py`). Comandos em `client/tools/elenco_hd.sh`.
- **Política de privacidade pública** em https://vvs2705.github.io/dungeon-courier/ (4 idiomas; Unity LevelPlay, Unity Ads e ironSource Ads); o texto de privacidade do jogo cita os anúncios só quando eles são reais. Conta Unity Ads ligada ao LevelPlay (2026-10-16).
- **Telas de meta no estilo novo:** vantagens com ícones pintados, roupas pelo retrato da folha, mapa com o chefe de cada região (silhueta enquanto fechada), Dragão no desafio sem fim, temporada e marcos com ícones do que pedem; fora da sala, nas telas 20:9, a pedra da região continua até a borda.
- **Página da loja e 13 criativos refeitos com a arte nova:** 8 capturas × 4 idiomas (`client/tools/loja_fotos.sh`), ícone 512 igual ao do aparelho, feature graphic com o logo novo.
### Mudado
- APK de release: 83,0 MiB (era 67,7) por causa das folhas HD. No POCO F4 (peças paralelas) o fps fica em 29–38 até no menu: medir num aparelho original.
### Corrigido
- Documentação da Play: a declaração de ID de publicidade é **não** (estava "sim") e o formulário de segurança dos dados segue a tabela do Unity Ads (`docs/LOJA.md` §4.5).

## [1.2.0-beta] - 2026-10-12
### Adicionado
- **Sprites de todo o elenco:** múmia, gárgula, necromante e toda a região 4 (diabrete, kobold, magma/brasa, Capataz, salamandra, planta, mapa, decoração) e o **Dragão Ancião armado à mão no Blender**; 7 roupas do courier (troca de cor, 0 crédito); ícone da loja.
- **Economia medida na campanha inteira** (sobra no fim 23,7 mil → 2,6 mil moedas) e **fim de jogo**: níveis 6–8 do equipamento depois do Dragão (10/13/18 mil).
- **Unity IAP 5.4.3**: loja falsa no PC, compra em 2 passos (o núcleo concede e só então confirma), restaurar compras; 9 produtos de conteúdo fixo.
- HUD das cargas novas (gelo, espelho, lanterna, gaiola, balão), faixas de fase dos 6 chefes, elenco pelos estados do núcleo, rota segura na porta, roupa no mundo, texturas da cripta e da forja; flags de teste `-room` e `-roupa`.
- **Página da loja nos 4 idiomas** (8 capturas, ícone 512, feature graphic com o Dragão) e os **13 criativos** gravados do jogo real em pt, en, es e id; flag de teste `-guilda` (fotos dos chefes).
### Corrigido (revisão de correção de 2026-10-12)
- Compra: a roupa Explorador do Pacote inicial não aparece mais à venda; o mesmo pedido do Pacote de evento não concede duas vezes (app fechado antes da confirmação); o Pacote inicial com equipamento já destravado dá 500 moedas por projeto que não cabe; interstitial que não abriu não conta como exibido; o baú do dia não volta depois de 64 rewarded.
- Save: o gelo do cristal e a lanterna apagada sobrevivem a fechar o app; o contrato do dia vindo da 1.0 (save v2) não some; o save não depende do idioma do aparelho; o raro trocado não volta ao original no mesmo dia; o "hoje" vira à meia-noite com o quadro aberto.
- Exploit: o perk Caçador tem o teto de 20 moedas por visita de sala (antes, sem teto).
- Desempenho no Android: `vSyncCount 0` no início (a qualidade Medium prendia o quadro ao vsync de 120 Hz): o POCO F4 foi de 28–36 para ~55 fps; o logcat mostra `DC fps=` a cada 60 s.
- Visual: o brilho de dano vem no máximo 1 vez a cada 0,3 s (e a 60% nos chefes, que viravam um vulto branco com o bumerangue); o ícone das cargas novas no HUD (caía num ovo marrom); a dica da sala trancada diz "derrote o chefe/os guardas para abrir a porta".
### Mudado
- **LevelPlay fora do build** até existir a conta (código atrás de `DC_LEVELPLAY`): o APK não pede `AD_ID` (privacidade dos testadores; ECA Digital). Ver `docs/MONETIZACAO.md`.

## [1.1.0-beta] - 2026-10-11
### Adicionado
- **4 regiões e 72 contratos:** Minas Alagadas, Cripta Assombrada e Forja do Dragão no núcleo, com salas, terreno (água, trilhos, escuro com luz, teias, lava, respiros, fogo), 14 inimigos novos, 6 chefes (Rainha Toupeira, Golem, Viúva Aranha, Necromante, Capataz Kobold, Dragão Ancião), 8 cargas novas e o bot jogando cada região (148 testes).
- **Sistemas de longo prazo:** eventos semanais, modificadores, temporada, contrato raro, rerolls, equipamento lateral com projetos, maestria e marcos, desafio sem fim, 25 perks, save v3.
- **Telas:** mapa de regiões, eventos, temporada, modificadores, equipamento, maestria, sem fim, roupas, loja e anúncios **de teste** (sem SDK real).
- **4 idiomas** (pt-BR, EN, ES, ID), áudio das regiões e dos chefes, sprites da região 2 e parte da 3, página da loja e roteiros dos criativos.

## [1.0.0-beta] - 2026-10-10
### Adicionado
- **v1.0-beta**, versão completa para teste com jogadores reais (`docs/historico/CONTRATO_V1.md`):
  - núcleo: 24 contratos na região "Porões da Guilda" (16 salas + 2 secretas + M1 + B1 + D1), cargas bolo, mensagem, caixa pesada e barril; arqueiro, limo, Capitão Ladrão (M1); armas estilingue, bumerangue e bombinha; 10 perks (escolha 1 de 3 após as camadas 2 e 4, sorteio pela semente, sem pagamento); reforço com teto; estrelas; contrato do dia e por código de 6 caracteres; guilda 4×8; save v2 com migração; 76 testes;
  - telas: título, menu, quadro, código, guilda/armas, configurações (som, música, vibração, idioma), créditos, pausa, perks; pt-BR e EN;
  - mundo: piso e parede texturizados, tochas, pilares, mira do arqueiro, poças do limo, barril com contagem, sala secreta, barra do chefe, VFX em pool;
  - áudio sintetizado por código (`client/tools/audio_gen.py`), sem asset de terceiros;
  - diário local de teste (CSV, sem dado pessoal; faixa de RAM em vez do modelo do aparelho; partidas do bot não entram) com Exportar diário; `client/tools/diario_report.py`; roteiro e perguntas de playtest;
  - arte dos Lotes 2 e 3 (bruto, ogro, arqueiro com arco preso à mão, Capitão, limo, cargas, tocha, pilar) e polimento das animações do Lote 1;
  - builds de release `Setup.BuildWindowsRelease` e `Setup.BuildAndroidRelease` (versão 1.0.0-beta, versionCode 100002).
- v0.1 jogável no PC (2026-10-09): projeto Unity 6000.3.23f1; núcleo `DC.Core` determinístico (carregar/pousar/dash, ladrão/guarda/bruto/morcego/ogro, caixa/relíquia/ovo, 6 salas + chefe + entrega, 8 contratos, resgate da carga perdida, guilda, save v1) com 49 testes e bot de balance; View de um dedo com HUD, quadro, guilda, resgate e cena da entrega; projeção 60°.
- Arte do Lote 1 pelo Tripo (530 créditos com aval): courier, ladrão, guarda (escudo como peça separada), cliente, morcego, caixa, baú, arco; sprites renderizados no Blender com clipes Mixamo reaproveitados e animações feitas no Blender.
- `Setup.BuildAndroidDev` apaga os APKs anteriores antes de gerar (fica só o atual).
- Repositório criado (2026-10-09) com o GDD revisado (comparativo de mercado no topo), `docs/COMPETITIVO.md` e a estrutura de pastas do projeto Unity.

[Unreleased]: https://github.com/Vvs2705/dungeon-courier/compare/v1.2.0-beta...HEAD
[1.2.0-beta]: https://github.com/Vvs2705/dungeon-courier/compare/v1.1.0-beta...v1.2.0-beta
[1.1.0-beta]: https://github.com/Vvs2705/dungeon-courier/compare/v1.0.0-beta...v1.1.0-beta
[1.0.0-beta]: https://github.com/Vvs2705/dungeon-courier/releases/tag/v1.0.0-beta
