# Changelog — Dungeon Courier

Formato: [Keep a Changelog](https://keepachangelog.com/pt-BR/1.1.0/); versões em [SemVer](https://semver.org/lang/pt-BR/).

## [Unreleased]

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
- **v1.0-beta**, versão completa para teste com jogadores reais (`docs/CONTRATO_V1.md`):
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
