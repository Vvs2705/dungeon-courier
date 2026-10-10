# Contrato da v1.0-beta — versão completa para teste com jogadores reais (2026-10-10)

**Pedido do Vinicius (2026-10-10):** "esse projeto tem que ficar pronto ao ponto de teste real com outros jogadores com a versão final do jogo e não um MVP". Avanço contínuo, sem pausas; commit/push/PR/merge fazem parte do fluxo; sem teste no POCO (teste no PC primeiro). Base: `CONTRATO_V01.md` (regras que continuam valendo), `GDD.md` (§2–§10, §27–§28) e `BALANCE.md`.

Números iniciais são hipóteses: o bot mede e o `BALANCE.md` registra antes/depois.

## 1. O que é a v1.0-beta
- Uma região completa ("Porões da Guilda") com começo, meio e fim: FTUE, 24 contratos, mini-chefe, chefe, contrato do dia e contrato por código, guilda, armas e perks. ~2–3 h de conteúdo para um jogador novo.
- **Sem anúncios e sem IAP** na beta (guardrails: nada de item aleatório pago; interstitial desligado). A monetização (rewarded com id de transação, GDD §7) entra depois da leitura da beta.
- Builds: **APK Android de release** (não-development, ARM64; só o atual, os anteriores apagados) e **Windows** (540×960). Sem backend: tudo local.

## 2. Duração e ritmo (resolve o achado do BALANCE)
- Alvo humano: **3–6 min por contrato**, sessão de 2–3 contratos. Alvo do bot (handicap): **1,5–3 min** por contrato a partir do C4 (C1–C3 tutoriais podem ser mais curtos).
- Como: contratos de **4–6 camadas** (+ mini-chefe/chefe quando houver); salas com **evento** (reforço de inimigos quando o courier passa da metade da sala ou quando a carga fica no chão; nunca infinito); **perk** escolhido a cada 2 camadas.

## 3. Cargas (7) — cada uma muda um verbo
| Carga | Verbo | Falha | Pagamento |
|---|---|---|---|
| Caixa | — | — | 100% |
| Relíquia frágil | racha com golpe, queda e golpe carregando (regra da v0.1) | 3 rachaduras | 140% |
| Ovo | choca no tempo-alvo; filhote segue e tem de chegar; **explosão perto racha o ovo** | filhote morto | 120% (chocado 80%) |
| Bolo de casamento | **proíbe o dash** (o toque rápido só balança e mostra o aviso) | — (rachaduras só reduzem bônus) | 130% |
| Mensagem urgente | **cronômetro do contrato**; abre **portas trancadas** sozinha | tempo esgotado | 125% |
| Caixa pesada | **−25% de velocidade** carregando; pousar/pegar mais lentos (+0,2 s) | — | 160% |
| Barril de pólvora | **no chão por 3 s explode** (dano em área 2,5 m, acerta inimigos e courier) | explosão = contrato falho | 150% |

## 4. Inimigos (6) + mini-chefe + chefe
Morcego, Ladrão, Guarda, Bruto (v0.1) e:
- **Arqueiro:** parado ou recuando; mira com linha de telegraph (0,8 s) e dispara projétil em linha reta que acerta courier **e carga**; obstáculos bloqueiam. Vida 3.
- **Limo:** lento (1,2 m/s), toque 1 de dano; ao morrer deixa poça por 4 s que reduz a velocidade em 40%. Vida 4.
- **Mini-chefe Capitão Ladrão (M1):** rápido (2,6 m/s); prioriza a carga no chão; com ela, foge e chama 2 ladrões; atingido 2× derruba. Vida 14.
- **Chefe Ogro (B1):** como na v0.1 (pancada em área, chama ladrão), vida 30 → ajustar pelo bot.

## 5. Armas (3, escolhidas na Guilda; troca fora do contrato)
| Arma | Padrão | Notas |
|---|---|---|
| Estilingue | tiro teleguiado (o atual) | inicial |
| Bumerangue | vai e volta em linha, atravessa inimigos, recarga maior | desbloqueia com reputação |
| Bombinha | arco, dano em área 1,5 m, lenta | **explosão perto do ovo racha; perto do barril detona**; desbloqueia com reputação |
Cada arma tem 3 níveis comprados com moedas (dano/recarga).

## 6. Perks (10, só na run; escolha 1 de 3 depois das camadas 2 e 4; sorteio pela semente do contrato, sem pagamento)
Passo leve (+10% velocidade carregando) · Pouso rápido (pouso em 0,1 s) · Dash duplo (2 cargas) · Carga acolchoada (1º golpe na carga ignorado) · Mão rápida (+20% cadência) · Ímã (raio de moedas) · Coração extra (+2 vida máx. e cura 2) · Vigia (ladrão da carga sozinha demora +4 s) · Bota de ferro (dash empurra mais e atordoa 0,6 s) · Sorte de baú (+30% moedas de baú).

## 7. Salas e contratos
- **Região 1 "Porões da Guilda":** 16 salas normais (R1–R16) com lições claras e variedade de layout, 2 **salas secretas** (parede rachada visível; atravessar dá baú extra), M1 (mini-chefe), B1 (chefe) e D1 (entrega). Ícone de ameaça coerente com o dano medido (teste da v0.1) **e** um ícone de **risco para a carga** (ladrão/arqueiro/explosão) — fecha a pendência do H2.
- **24 contratos** (C1–C24) com curva: C1–C3 tutoriais (2–3 camadas), C4+ com 4–6 camadas, cada carga nova estreia num contrato próprio, C12 = mini-chefe, C24 = chefe. Repetir contratos é permitido.
- **Estrelas por contrato (0–3):** entregue · carga intacta · sem morrer e dentro do tempo-alvo. Reputação = estrelas; destrava contratos e armas.
- **Contrato do dia:** semente pela data (`daily-AAAAMMDD`), 1×/dia, recompensa extra mostrada antes; voltar o relógio não repete (o jogo guarda a última data vista).
- **Contrato por código:** código de 6 caracteres (A–Z, 2–9, sem ambíguos) gera um contrato com as salas/cargas da região; paga como contrato normal; o código aparece no resultado para compartilhar.
- **Resgate da carga perdida** continua (v0.1).

## 8. Meta e economia (1 moeda)
- Moedas: pagamento + bônus + baús + resgate + contrato do dia. Ralos: Guilda e armas.
- Guilda com 4 linhas × 8 níveis: Vida, Dash, Bolsa, **Mochila** (+0,15 m no raio de pegar e −0,02 s no pouso por nível). Custo crescente; nenhum upgrade deixa a carga indestrutível.
- Sem energia, sem renda passiva, sem compra.

## 9. Save
`v=2` com migração explícita de v1 (e v0); versão futura recusada sem apagar; estado do contrato em curso gravado a cada dano/sala (regra da v0.1).

## 10. Telemetria local (para o teste com jogadores)
Diário CSV com os eventos do GDD §14 + `contract_stars`, `perk_pick`, `weapon_equip`, `daily_start/end`, `code_start`; **id anônimo de instalação** (GUID local, sem dado pessoal), versão do app. Botão **"Exportar diário"** nas configurações (compartilhar o arquivo no Android; abrir a pasta no Windows). Nada sai do aparelho sem o jogador mandar.

## 11. UI/UX final
Tela de título, menu (Jogar, Guilda, Configurações, Créditos), quadro de contratos com estrelas e o contrato do dia, entrada de código, guilda com armas, escolha de perk, pausa, resultado com estrelas e código, configurações (Som, Música, Vibração, Idioma pt-BR/EN, Exportar diário), créditos (inclui "modelos 3D gerados com IA (Tripo) e animações Mixamo/Blender"), aviso curto de privacidade. Alvos de toque ≥ 48 dp, texto legível em 540×960, sem emoji em texto renderizado.

## 12. Áudio
Banco de SFX sintetizados por script (sem assets de terceiros) com variação de altura; 3 músicas em laço sintetizadas por script (menu, masmorra, chefe); mixer com volume de Som e Música; háptica nos momentos do GDD §12.

## 13. Arte
Lotes 1 e 2 integrados. **Lote 3** (Tripo, com registro em PROVENIENCIA): arqueiro (+ arco como item separado), limo, Capitão Ladrão, bolo, mensagem, caixa pesada, barril; decoração (tocha, pilar, grade) e texturas de piso/parede pelo gerador de imagem grátis. Itens separados do corpo e presos ao osso no Blender (regra do Vinicius). VFX procedurais (poeira, golpe, explosão, moedas).

## 14. Builds e versão
`1.0.0-beta` no `Setup`. `Setup.BuildAndroidRelease` (não-development, ARM64, apaga APKs anteriores; assinatura de teste — a da Play vem depois). Windows idem. Notas da versão em `CHANGELOG.md`.

## 15. Portões (toda leva)
`dotnet test client/tools/coretests` · `dotnet build client/tools/viewcheck/view` (0/0) · Unity EditMode · build Windows · `-autoplay 24` (campanha inteira do bot) · fotos 540×960 conferidas · BALANCE atualizado. Merge na `main` por PR com CI verde.
