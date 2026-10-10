# Dungeon Courier — perguntas do playtest (pronto para copiar)

Este questionário faz parte do protocolo em [`TESTE_JOGADORES.md`](TESTE_JOGADORES.md). Use o bloco A logo depois da sessão 1, falado, em 5 a 8 minutos. Use o bloco B no D+8, falado ou por mensagem.

**Regras:**
- Faça as perguntas abertas antes das específicas e deixe a escala para o fim.
- Leia cada pergunta como está escrita. Não dê exemplos, não sugira palavras e não diga "carga", "entrega", "porta" nem "resgate" antes de a pessoa usar a palavra.
- Anote a resposta **literal**, entre aspas. Silêncio e "não sei" também são resposta.
- As respostas ficam só com o id: `playtest/rodadaN/respostas/<id8>.md`. Nada de nome.

---

## Bloco A: depois da sessão 1

```
id: ________   data: ____/____/____   plataforma: Android | Windows   jogou até: C__ (anotar da tela)

A1. Como você explicaria este jogo para um amigo, em uma ou duas frases?
    "                                                                      "

A2. Ele te lembrou algum outro jogo? Qual, e em quê?
    "                                                                      "

A3. O que você mais gostou? E o que mais te incomodou?
    gostou: "                                                              "
    incomodou: "                                                           "

A4. Teve algum momento em que você não sabia o que fazer? Onde?
    "                                                                      "

A5. Quando você parava para lutar, o que fazia você parar?
    "                                                                      "

A6. Como você escolhia por qual porta ir?
    "                                                                      "

A7. (só se perdeu algum contrato) O que aconteceu quando você perdeu?
    "                                                                      "

A8. Escala de 1 a 5 (leia os extremos):
    a) Diversão:                     1 nada divertido ... 5 muito divertido      [ ]
    b) Entender os controles:        1 muito confuso  ... 5 muito claro          [ ]
    c) Dificuldade:                  1 fácil demais   ... 3 na medida ... 5 difícil demais   [ ]
    d) Vontade de jogar de novo hoje ou amanhã: 1 nenhuma ... 5 muita        [ ]

A9. (comportamento, não pergunte) Quando ouviu "pode parar quando quiser":
    [ ] parou na hora   [ ] pediu para terminar o contrato   [ ] quis continuar depois do contrato
```

## Bloco B: no D+8 (junto com o envio do diário)

```
id: ________   data: ____/____/____

B1. Você jogou depois da primeira sessão? (sim/não) O que te fez voltar, ou o que te fez não voltar?
    "                                                                      "

B2. Pensando em tudo que jogou: o que faz este jogo ser diferente de outros parecidos?
    "                                                                      "

B3. Lembra de alguma coisa que você carregava no jogo? O que ela mudava no seu jeito de jogar?
    "                                                                      "

B4. Algo travou, fechou sozinho ou ficou lento? Quando?
    "                                                                      "

B5. De 0 a 10, quanto você recomendaria este jogo para um amigo que gosta de jogos de celular?   [ ]

B6. Pagaria por ele, ou jogaria com anúncios? (só para saber; não existe compra no teste)
    "                                                                      "
```

---

## Codificação (o Vinicius preenche depois, por id)

| Código | Regra | Alimenta |
|---|---|---|
| `carga_espontanea` | A1, A2, A3 ou B2 cita carga, entrega, carregar, pacote, cliente ou "levar" **sem** a palavra ter sido dita antes. B3 não conta, porque induz | GDD §26 CONTINUAR (portão MANUAL "carga") |
| `archero` | A1, A2 ou B2 compara com Archero, Survivor.io ou "jogo de matar tudo" como se fosse o mesmo jogo | GDD §26 CANCELAR (sinal) |
| `combate_obrigatorio` | A5 descreve a luta como sem escolha ("tinha que", "não dava pra passar") | H1 refutada se for a maioria |
| `combate_inutil` | A5 ou A3 diz que lutar não vale a pena ("só passava correndo") | H1 refutada se for a maioria |
| `porta_por_carga` | A6 cita a carga ou o ícone de risco ao explicar a escolha da porta | H2 (complementa o diário) |
| `porta_aleatoria` | A6: "qualquer uma", "a do meio", "a mais perto" | H2 (sinal contra) |
| `entendeu_resgate` | A7 diz que a carga ficou na masmorra ou que dava para buscar | H3 / FTUE |
| `travou_em` | A4: tela ou momento (FTUE, porta, guilda, perk...) | FTUE; vira padrão com 2 ou mais sessões |
| `crash` | B4 relata fechamento ou travamento | crash-free (portão MANUAL) |
| `voltou_sozinho` | B1 = sim, por motivo próprio, sem lembrete | retenção espontânea (confere com o diário) |

**Como contar:** um código por pessoa, sem repetir (a mesma pessoa que fala 3 vezes conta 1). Escreva como "6 de 8 (ids ...)". Um achado só vira padrão em 2 ou mais sessões independentes (`TESTE_JOGADORES.md` §6). Escala: mediana e faixa, nunca só a média.
