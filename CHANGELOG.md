# Changelog — Dungeon Courier

Formato: [Keep a Changelog](https://keepachangelog.com/pt-BR/1.1.0/); versões em [SemVer](https://semver.org/lang/pt-BR/).

## [Unreleased]
- Falta para o GDD completo (`docs/CONTRATO_LANCAMENTO.md`): sprites da múmia, gárgula, necromante e de toda a região 4 (com o dragão armado no Blender), peça `corrente` travada no Tripo, roupas do courier renderizadas, SDKs reais de anúncio e compra (dependem das contas do Vinicius), gravação dos criativos e fotos da loja, balance da economia (sobra de moedas) e da duração.

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

[Unreleased]: https://github.com/Vvs2705/dungeon-courier/compare/v1.1.0-beta...HEAD
[1.1.0-beta]: https://github.com/Vvs2705/dungeon-courier/compare/v1.0.0-beta...v1.1.0-beta
[1.0.0-beta]: https://github.com/Vvs2705/dungeon-courier/releases/tag/v1.0.0-beta
