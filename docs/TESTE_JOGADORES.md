# Dungeon Courier — teste com jogadores reais (escrito para a v1.0-beta; vale para a 1.4.3-beta)

Protocolo do teste da v1.0-beta (`CONTRATO_V1.md`) com pessoas de fora do estúdio. O teste não coleta dado pessoal. O jogo grava um diário local (`CONTRATO_V1.md` §10, `UI_V1.md` §4), o jogador o exporta no fim e o Vinicius lê os diários com `client/tools/diario_report.py`. As perguntas prontas estão em [`PERGUNTAS_PLAYTEST.md`](PERGUNTAS_PLAYTEST.md).

**Status:** PROPOSTO. Nenhuma hipótese está validada. Um portão "OK" no relatório quer dizer que o número ficou dentro do alvo nesta amostra, não que a hipótese foi validada.

## 1. O que o teste responde

| Pergunta | Como medimos | Fonte do alvo |
|---|---|---|
| O jogo ensina sozinho? (FTUE) | diário: % que sai da 1ª sala do C1 (alvo ≥ 88%) e % que entrega o C1 (alvo ≥ 85%). Observação: onde a pessoa travou e o que disse | GDD §3, §15 |
| A pessoa quer o 2º contrato? | diário: começou outro contrato na mesma sessão em que entregou o C1 (alvo ≥ 55%) | GDD §15, §26 |
| O ritmo está certo? | diário: duração do contrato a partir do C4, alvo de 3–6 min | CONTRATO_V1 §2 |
| **H1** Lutar é escolha? | diário: % de salas com luta por jogador, alvo de 30–70%. Pergunta: o combate parece "obrigatório" ou "inútil"? | GDD §28 |
| **H2** A carga muda a rota? | diário: porta escolhida por carga e por risco. Refutada se a mesma porta for escolhida em mais de 80% das vezes | GDD §28 |
| **H3** O resgate puxa o próximo contrato? | diário: depois de uma derrota, a pessoa começa outro contrato quando há carga para resgatar (comparado com quando não há) | GDD §28 |
| O diferencial aparece? | pergunta aberta: "carga/entrega" é citada sem ninguém sugerir? Alguém diz "Archero genérico"? | GDD §26 |
| A pessoa volta sozinha? | diário: dias com jogo fora das sessões combinadas (D1/D7 só como sinal) | GDD §15 |

O que este teste **não** mede: CPI, receita e retenção de coorte (isso é o teste de mercado, com 1,5–2 mil installs, GDD §16), nem crash-free (o diário não tem evento de crash).

## 2. Quem e quantos

- **Rodada 1: 5–8 pessoas.** Foco em compreensão, toque e H1. É pouca gente para números, então o que vale é o padrão que se repete.
- **Rodada 2: 15–30 pessoas**, depois das correções da rodada 1, com a mesma versão para todos. Os portões do relatório começam a ter sentido aqui.
- **Perfil (GDD §1):** 13–40 anos, jogam no celular pelo menos algumas vezes por semana, gostam de ação casual, roguelite leve ou masmorra (ex.: já jogaram Archero, Survivor.io ou similares). Misture quem joga muito com quem joga pouco.
- **Plataforma:** a maioria no Android, que é o alvo. O Windows serve para quem não tem Android compatível. Cada pessoa joga **num aparelho só** do começo ao fim, porque cada aparelho gera um id diferente. O relatório separa as plataformas com `--plataforma`.
- **Ficam de fora:** quem ajudou a fazer o jogo, quem já viu o jogo rodando, desenvolvedores de jogos (mudam o jeito de jogar e de falar) e menores de 13 anos (fora do público).
- **Idade:** maiores de 18 participam com o próprio termo. Entre 13 e 17 anos, só com o termo assinado por pai, mãe ou responsável, e com o responsável ciente das duas sessões (LGPD art. 14).
- **Não se mistura população:** registre a mão (destro/canhoto), a plataforma e a versão de cada pessoa. Se metade da rodada jogar outra versão, rode o relatório separado com `--versao`.

## 3. Antes da rodada (checklist do Vinicius)

1. Gere **um** build de cada, com a mesma versão (`Setup.Version`): `Setup.BuildAndroidRelease` gera `client/Builds/android/DungeonCourier-1.0.0-beta.apk` (apaga os APKs anteriores) e `Setup.BuildWindowsRelease` gera `client/Builds/win-release/`. Teste os dois no PC primeiro: o Windows inteiro e o APK num aparelho seu.
2. Rode `python client/tools/diario_report.py --autoteste`. Ele precisa terminar com `autoteste OK`.
3. Crie a pasta da rodada **na raiz do repositório**: `playtest/rodada1/` com `diarios/`, `observacao/` e `respostas/`. `/playtest/` está no `.gitignore` (conferido com `git check-ignore`). Não use `client/playtest/`, que **não** está ignorado.
4. Imprima ou separe o termo (§9), a ficha de observação (§6) e as perguntas (`PERGUNTAS_PLAYTEST.md`).
5. Combine as datas: sessão 1 (D0), período livre em casa até o D+8 e exportação do diário no D+8.
6. Guarde a lista de contato (nome e telefone, só para marcar as sessões) **fora** de `playtest/` e de qualquer repositório, por exemplo na agenda do celular. Os arquivos do teste levam só o id anônimo.

## 4. Instalação

### Android (APK)
- **Caminho preferido: cabo + adb no PC do Vinicius.** Com a pessoa presente, rode `adb install client/Builds/android/DungeonCourier-1.0.0-beta.apk`. O adb continua funcionando com a verificação de desenvolvedor.
- **APK enviado por arquivo** (WhatsApp, Drive): a pessoa toca no arquivo e, no Android 8 ou mais novo, permite "Instalar apps desconhecidos" **só para o app que abriu o arquivo**. Depois disso, ela pode desligar a permissão de novo.
- **Atenção, verificação de desenvolvedor:** desde 30/09/2026, no Brasil, aparelhos certificados exigem que o app venha de um desenvolvedor verificado. Um APK solto de conta não verificada pode ser **bloqueado** na instalação. O adb segue igual, e a "distribuição limitada" cobre até 20 aparelhos (fonte: https://developer.android.com/developer-verification/guides, consultado em 2026-09-30 pela ficha 46; confira antes da rodada).
- **Alternativa quando houver conta na Play:** use o **teste interno da Play Console**, que aceita até 100 testadores (fonte: https://support.google.com/googleplay/android-developer/answer/9845334, consultado em 2026-09-30). Ele exige AAB, chave de upload própria e Play App Signing (`UI_V1.md` §5). Uma conta pessoal nova ainda precisa de um teste fechado com 12 testadores por 14 dias antes da produção (fonte: https://support.google.com/googleplay/android-developer/answer/14151465, mesma data). Dá para montar esse grupo com os testadores da rodada 2.
- **Não troque o APK no meio da rodada.** O APK é assinado com a chave de debug do Unity. Instalar outra assinatura obriga a desinstalar, e isso apaga o progresso, o diário e o id. Se for inevitável, peça para a pessoa **exportar o diário antes**.
- Antes da sessão, peça para ligar o **Não perturbe**, para que nenhuma notificação com nome apareça na tela ou na gravação.

### Windows
- Compacte a pasta `client/Builds/win-release/` num .zip. A pessoa extrai e abre `DungeonCourier.exe` (a janela é 540 × 960).
- O executável não é assinado, então o SmartScreen mostra "O Windows protegeu o computador". O caminho é **Mais informações → Executar assim mesmo**. Avise antes, para a pessoa não achar que é vírus.
- O diário fica em `%USERPROFILE%\AppData\LocalLow\V-STACK\Dungeon Courier\diario.csv`.

## 5. Roteiro

### O que NÃO explicar (o jogo tem de ensinar sozinho)
Não explique os controles (arrastar, soltar o dedo para pousar e lutar, toque rápido = dash, que o dash larga a carga). Também não explique as portas e os ícones (caveiras, risco para a carga, baú), o que cada carga faz, o resgate da carga perdida, perks, guilda, armas, contrato do dia e contrato por código. Se a pessoa perguntar "como faz X?", devolva com "O que você acha que acontece se tentar?" ou "Faça como achar melhor". Só intervenha se ela ficar **2 minutos travada no mesmo ponto** ou pedir para parar. Anote a ajuda dada, com o minuto: a partir dali, a sessão tem uma assistência e não se compara com as outras.

### Sessão 1 (D0, moderada, ~40 min, presencial ou chamada com tela compartilhada)
1. **(3 min) Abertura.** Leia o termo (§9) junto com a pessoa e confirme o sim. Diga:
   > "Estamos testando o jogo, não você. Não existe jeito errado de jogar. Se puder, fale em voz alta o que está pensando, o que espera que aconteça, o que te surpreende. Eu não vou ajudar durante o jogo, porque quero ver se o jogo se explica sozinho."
2. **(2 min) Instalação**, se ainda não estiver instalado (§4). Anote quanto tempo levou e onde a pessoa travou.
3. **(25–30 min) Jogo livre.** Diga só: *"Abra o jogo e jogue como jogaria em casa."* Observe sem interferir (§6). Não comente acerto nem erro.
4. **Aos 25–30 min**, diga: *"Pode parar quando quiser."* Anote se a pessoa para na hora ou pede para terminar o contrato. O "quer continuar?" é um dado de comportamento.
5. **(5–8 min) Perguntas pós-sessão** (`PERGUNTAS_PLAYTEST.md`, bloco A), nesta ordem: abertas primeiro, escala depois.
6. **(1 min) Combinar a parte em casa:**
   > "Agora o jogo é seu. Jogue em casa quando der vontade, quanto quiser, ou não jogue: as duas coisas me ajudam. No dia [D+8], abra o jogo, vá em Configurações, toque em EXPORTAR DIÁRIO e me mande o arquivo."

   Anote na ficha o **id anônimo** que aparece nas Configurações ("id anônimo do teste: xxxxxxxx"), de 8 caracteres.

### Sessão 2 (D+8, curta, ~10 min, pode ser por mensagem ou chamada)
1. A pessoa exporta e envia o diário (§8), mesmo que não tenha jogado depois da sessão 1. O envio marca até onde o diário observou, e sem ele não dá para calcular D1/D7.
2. Perguntas finais (`PERGUNTAS_PLAYTEST.md`, bloco B).

> **Retenção induzida:** a sessão 1 é combinada e a exportação do D+8 também. Só os dias com jogo **entre** essas duas datas são retorno espontâneo. O relatório não conta a sessão que só exporta (sem evento de jogo) como dia ativo. Mesmo assim, leia D1 e D7 como sinal e nunca compare com o alvo de coorte do GDD sem ressalva.

## 6. Observação sem interferir

Uma ficha por pessoa: `playtest/rodadaN/observacao/<id8>_s1.md`. **Não escreva o nome.**

```
id: ________  plataforma/aparelho: Android | Windows   mão: destra | canhota   versão: ______
som: sim | não   vibração: sim | não   idioma: pt | en   ajudas dadas (minuto + o quê): ______

mm:ss | etiqueta | o que fez / frase literal entre aspas
00:40 | compreensão | "ah, se eu soltar ele larga a caixa"
```

- **Etiquetas fixas:** `compreensão`, `confusão`, `frustração`, `prazer`, `bug`, `toque errado` e `ajuda`. Escreva a **frase inteira, literal**: muitas vezes ela já traz o diagnóstico.
- **Momentos para cronometrar:**
  - 1º pouso para lutar: descobriu sozinho? Leu a dica?
  - 1º dash: percebeu que larga a carga?
  - 1ª porta: olhou os ícones? Disse por que escolheu?
  - 1º ladrão: entendeu que ele quer a carga?
  - 1ª derrota: entendeu que a carga ficou na masmorra? Aceitou o resgate?
  - 1º perk, 1ª visita à guilda, 1ª carga nova (relíquia, ovo, bolo, mensagem, pesada, barril): o verbo foi percebido?
  - Ponto e minuto em que parou, e o motivo.
- **H1:** quando a pessoa luta, foi por escolha (pousou para limpar a sala) ou porque não tinha saída? Ela diz algo como "tenho que matar todo mundo" ou "nem precisa lutar"?
- **Toque (celular):** conte os toques errados por botão (alvo pretendido × alvo tocado). Dedo cobrindo a carga ou o inimigo também conta.
- **Não induza:** nada de "isso foi legal, né?" ou "viu o ícone?". Se precisar falar, use "O que você está pensando agora?".
- **Gravação de tela (opcional, só com o sim do termo):** só a tela do jogo, **sem câmera e sem microfone**, com o Não perturbe ligado. Apague o vídeo depois de codificar a ficha.
- **Regra de evidência:** um achado só vira **padrão** se aparecer em pelo menos 2 sessões independentes. Antes disso é sinal fraco. Toda conclusão cita as sessões (id) e o minuto.

## 7. Perguntas pós-sessão

As perguntas prontas para copiar estão em [`PERGUNTAS_PLAYTEST.md`](PERGUNTAS_PLAYTEST.md): o bloco A depois da sessão 1 e o bloco B no D+8. A ordem é perguntas abertas → perguntas específicas → escala de 1 a 5, e as respostas ficam associadas só ao id. Não use formulário que exija login ou colete e-mail. Anote as respostas em `playtest/rodadaN/respostas/<id8>.md`, ou use um formulário sem login e sem coleta de e-mail.

## 8. Exportar e enviar o diário (instrução para o jogador)

> No jogo: **Menu → CONFIGURAÇÕES → EXPORTAR DIÁRIO.**
> - **Android 10 ou mais novo:** o jogo salva uma cópia em `Download/DungeonCourier` e abre a lista de apps. Escolha WhatsApp, e-mail ou Drive e mande o arquivo `DungeonCourier_diario_....csv`.
> - **Android 8 ou 9:** o diário vai como **texto** (os últimos 400 KB). Mande a mensagem inteira, sem editar.
> - **Windows:** abre a pasta com o arquivo `diario.csv` selecionado. Mande esse arquivo.
> - Se aparecer "Ainda não há diário", é porque você ainda não jogou nenhum contrato. Tudo bem: avise e pronto.

Ao receber, o Vinicius:
1. Salva o arquivo como `playtest/rodadaN/diarios/<id8>.csv`. Se veio como texto (Android 8–9), cola o texto num `<id8>.txt`, porque o script lê `.csv` e `.txt`.
2. Apaga o anexo ou a mensagem da conversa e da pasta de downloads do PC. O arquivo fica só em `playtest/`.
3. Se a mesma pessoa mandar dois arquivos (exportou duas vezes), guarde os dois: o script tira as linhas repetidas.

## 9. Termo de consentimento (ler junto e pedir um sim)

> **Teste do jogo Dungeon Courier — termo de participação**
>
> Você foi convidado(a) a testar um jogo que ainda está em produção. O que estamos testando é o jogo, não você.
>
> **O que você vai fazer:** jogar cerca de 40 minutos com alguém observando e responder a algumas perguntas. Depois, se quiser, jogar em casa e, no dia combinado, mandar o "diário" do jogo.
>
> **O que o diário guarda:** só o que acontece no jogo (salas, portas, lutas, entregas, moedas do jogo), com data e hora, a versão do jogo, o tipo de aparelho (modelo e sistema) e um código aleatório criado pelo jogo. **Não** guarda seu nome, e-mail, telefone, localização, contatos, fotos nem áudio. O diário não vai pela internet: ele só sai do aparelho quando você toca em EXPORTAR DIÁRIO e escolhe para onde mandar. No Android o jogo mostra anúncios não personalizados (redes ironSource e Unity Ads), que usam a internet como diz a política de privacidade do jogo; as compras do teste são de mentira e não cobram nada.
>
> **Anotações:** quem observa anota o que você faz e frases que você diz enquanto joga, **sem o seu nome**, só com o código aleatório.
>
> **Gravação da tela (opcional):** [ ] aceito que a tela do jogo seja gravada, sem câmera e sem microfone. O vídeo é apagado depois da análise.
>
> **Para que serve:** só para melhorar o jogo. Os dados ficam guardados no computador do desenvolvedor, fora da internet e de repositórios públicos. Os resultados só aparecem em números agrupados (por exemplo, "6 de 8 pessoas"). Os arquivos com o seu código são apagados até 30 dias depois da decisão sobre a rodada.
>
> **Você pode desistir a qualquer momento**, sem dar motivo. Também pode pedir para apagar seus dados: basta informar o código que aparece em Configurações ("id anônimo do teste").
>
> **Idade:** declaro que tenho 18 anos ou mais **ou** que sou pai, mãe ou responsável legal por ___________ (13 a 17 anos) e autorizo a participação dele(a) nas condições acima.
>
> Participação: [ ] sim, aceito   Data: ____/____/______   Código (id anônimo): ____________
> *(Não escreva o nome neste papel. Se precisar de assinatura do responsável, guarde a folha separada dos dados do teste.)*

Este texto é simples de propósito e não é parecer jurídico. Revise-o com a ficha 48 se o teste crescer, se passar a envolver mais menores de idade ou se algum dado sair do aparelho de outro jeito.

## 10. Onde guardar os dados

```
playtest/                     <- na raiz do repositório; ignorado pelo git (/playtest/ no .gitignore)
  rodada1/
    diarios/<id8>.csv|.txt    <- diários exportados (só id; nada de nome no nome do arquivo)
    observacao/<id8>_s1.md    <- fichas de observação
    respostas/<id8>.md        <- respostas das perguntas
    relatorio.md              <- saída do diario_report.py
```

- Nunca coloque diários, fichas, vídeos ou respostas no git, na memória do estúdio ou em chat de IA com o id junto. Para documentos versionados (GDD, BALANCE) vão só **conclusões agrupadas**, sem a tabela por jogador.
- O relatório tem uma tabela por jogador com o id de 8 caracteres. Ele fica em `playtest/`.
- **Apague** diários, fichas, respostas e vídeos até 30 dias depois da decisão da rodada. Antes, copie as conclusões agrupadas para o documento de decisão.
- O diário traz o **modelo do aparelho** (`session_start`). Com poucos testadores, um modelo raro pode identificar a pessoa. Por isso o diário é tratado como dado pseudonimizado: fica em `playtest/` e é apagado no prazo. O relatório não mostra o modelo.

## 11. Rodar o relatório

Na raiz do repositório (PowerShell):

```powershell
python client/tools/diario_report.py --autoteste                                    # tem de terminar com "autoteste OK"
python client/tools/diario_report.py playtest/rodada1/diarios --saida playtest/rodada1/relatorio.md
python client/tools/diario_report.py playtest/rodada1/diarios --plataforma Android --saida playtest/rodada1/relatorio_android.md
python client/tools/diario_report.py playtest/rodada1/diarios --versao 1.0.0-beta    # se o relatório avisar "versões misturadas"
```

- Use `--saida`: o arquivo sai em UTF-8, e o console do Windows pode trocar os acentos por `?`.
- `--fuso -3` (o padrão) converte o UTC do diário para o horário de Brasília antes de contar os dias. Sem isso, quem joga depois das 21h "voltaria no dia seguinte". Para testadores em outro fuso, mude o valor.
- O script lê as salas e os contratos de `client/Assets/_DC/Scripts/Core/Defs.cs` (`--defs` para outro arquivo). Analise a rodada **com o código da mesma versão do APK**. Se as salas mudarem entre o build e a análise, a linha "Escolhas de porta que não batem com o conteúdo" deixa de ser 0.

**O que o relatório mostra:**
- **Portões** contra o GDD §15, §26 e §28, com status OK, ATENÇÃO, FALHA, SEM DADOS ou MANUAL. ATENÇÃO é a faixa ITERAR do GDD quando ela existe (D1 de 20–26%, D7 de 3,5–5%, H1 fora de 30–70%, H2 acima de 80%). Quando o GDD não dá faixa, ATENÇÃO vai até 10 pp abaixo do alvo, por convenção do relatório.
- **Funil:** abriu → começou o C1 → saiu da 1ª sala → entregou o C1 → começou o 2º contrato → C4 → mini-chefe (M1) → chefe (B1) → C24 entregue. A contagem é por engajamento (cada passo ≤ anterior), e diários sem o começo ficam de fora.
- **Por contrato e por carga:** conclusão, morte, falha de carga, desistência, duração (mediana e p75) e estrelas.
- **H1 por carga** e **H2:** porta com risco para a carga, ícone, caveiras e baú, com a mesma régua do `Bot.MeasureRoute`.
- **H3:** reinício depois de derrota com ou sem resgate pendente.
- Perks, armas, guilda, moedas, contrato do dia e por código, retenção D1/D3/D7/D30 por dia local, sinais de exploit do §28 e a tabela por jogador.
- **Qualidade dos dados:** linhas duplicadas, cortadas, da v0.1 (ignoradas, porque é outro jogo) e versões misturadas.

## 12. Critérios de decisão

O playtest **não cancela o jogo**. O GDD §26 só cancela depois de 2 iterações com coorte de 1,5–2 mil installs. O playtest decide entre seguir, corrigir e iterar.

**Rodada 1 (5–8):**
- **CORRIGIR A FTUE e repetir a rodada 1** se o portão de tutorial ou de 1º contrato der FALHA, ou se 2 ou mais pessoas travarem no mesmo ponto sem ajuda.
- **ITERAR o design** (H1, H2 ou carga) se 2 ou mais pessoas descreverem o combate como "obrigatório" ou "inútil", se H1 ficar fora de 30–70%, se H2 passar de 80%, ou se ninguém citar a carga ou a entrega sozinho.
- **SEGUIR para a rodada 2** se não houver FALHA de FTUE, nem sinal de design que se repete, nem bug de exploit (portão de exploit OK).

**Rodada 2 (15–30):**
- **CONTINUAR (preparar o teste de mercado, GDD §16)** se tutorial, 1º contrato e 2º contrato estiverem OK, se a duração do C4+ estiver em 3–6 min, se H1 estiver em 30–70%, se H2 não for refutada, se H3 apontar a favor e se metade ou mais citar "carga/entrega" sem ser perguntada.
- **ITERAR** se algum desses ficar em ATENÇÃO, ou se "combate bom, mas carga irrelevante" ou "rota não produz decisões" aparecer como padrão (GDD §26 ITERAR). Corrija e faça uma nova rodada de 15–30.
- **PARAR E REVER COM O VINICIUS** se, em 2 rodadas seguidas, a maioria descrever o jogo como "Archero genérico" e a carga não aparecer no feedback. Esse é o sinal do GDD §26 CANCELAR, mas a decisão continua dependendo da coorte de mercado.
- Um **FALHA no portão de exploit** é bug: vai para o núcleo com teste nomeado antes da próxima rodada (GDD §28).

## 13. Limites do diário (o que ele não mede)

- **Não há `session_end`.** A duração da sessão vai até o último evento gravado, e o tempo parado em menu no fim se perde.
- **Não há crash, FPS nem nascimento de ladrão.** O crash vem da observação e do relato. No celular, o Android vitals entra quando houver Play Console.
- **`perk_offer` não vai ao diário.** Dá para ver o que foi escolhido, mas não o que foi recusado.
- **O modo `-bot` também grava no diário.** Não rode o bot no aparelho de um testador e nunca junte o diário do PC de desenvolvimento aos da rodada.
- **A coluna `sala` é a sala no momento da gravação.** No `route_choice` ela já é a sala de destino, então o relatório usa os campos `a`/`b`.
- **O H3 é observacional.** Os grupos diferem pelo tipo de derrota. O A/B de verdade (com e sem oferta) precisa de remote config (v0.2).
