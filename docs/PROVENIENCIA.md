# Dungeon Courier — Proveniência e licença de arte

## Regras

- **Sem linha, sem build:** cada asset gerado ou de terceiros entra aqui (ferramenta, plano, prompt, data, task id, SHA-256, licença). Sem essa linha, o asset não entra em build comercial (`ART_BIBLE.md` §13). O formato é o do `03_FORGE_STREET/docs/PROVENIENCIA.md`.
- **Abrir antes, completar depois:**
  - A linha da peça é aberta **antes** de gerar, com prompt e créditos previstos.
  - Depois se completa com task id, arquivo baixado, SHA-256, créditos gastos e saldo.
  - O saldo se lê no site antes e depois de cada geração.
- **Aval por lote:** créditos só com aval explícito do Vinicius para aquele lote (guardrail). O teto do lote vai na 1ª linha do livro-razão, e o saldo final não pode passar dele.
- **Hash:** SHA-256 com `certutil -hashfile <arquivo> SHA256` (Windows) ou `sha256sum <arquivo>`.
- **Onde ficam os arquivos crus:** `arte/tripo/` e `arte/mixamo/`, fora do git (`.gitignore`).

**Estado (2026-10-09):** vazio. Nenhum lote aprovado, nada gerado, 0 crédito gasto.

## 1. Livro-razão de créditos (Tripo, conta do estúdio, plano Max)

| Data/hora | Evento | Saldo visto no site | Gasto | Obs |
|---|---|---|---|---|
| | Abertura do Lote 1 (aval: ___; orçado 485, teto 550) | | 0 | conferir "Gerar 45" e Privado no 1º modelo |
| | | | | |

## 2. Registro por peça (Tripo)

Preencha cada linha. Prompt e parâmetros completos vão no bloco do §6, se a linha não comportar.

| Asset (pasta) | Lote/# | Data | Ferramenta (imagem → modelo → rig) | Imagem escolhida | Task id | Prompt (texto exato) | Créditos (previsto / gasto) | Arquivo baixado | SHA-256 do arquivo baixado | Licença | Visibilidade |
|---|---|---|---|---|---|---|---|---|---|---|---|
| courier | 1/#1 | | | | | | 65 / | `arte/tripo/courier/` | | | |
| ladrao | 1/#2 | | | | | | 65 / | `arte/tripo/ladrao/` | | | |
| guarda | 1/#3 | | | | | | 65 / | `arte/tripo/guarda/` | | | |
| cliente | 1/#4 | | | | | | 65 / | `arte/tripo/cliente/` | | | |
| morcego | 1/#5 | | | | | | 45 / | `arte/tripo/morcego/` | | | |
| caixa | 1/#6 | | | | | | 45 / | `arte/tripo/caixa/` | | | |
| bau | 1/#7 | | | | | | 45 / | `arte/tripo/bau/` | | | |
| bau_aberto | 1/#8 | | | | | | 45 / | `arte/tripo/bau_aberto/` | | | |
| arco | 1/#9 | | | | | | 45 / | `arte/tripo/arco/` | | | |
| bruto | 2/#10 | | | | | | 65 / | `arte/tripo/bruto/` | | | |
| ogro | 2/#11 | | | | | | 65 / | `arte/tripo/ogro/` | | | |
| reliquia | 2/#12 | | | | | | 45 / | `arte/tripo/reliquia/` | | | |
| ovo | 2/#13 | | | | | | 45 / | `arte/tripo/ovo/` | | | |
| filhote | 2/#14 | | | | | | 45 / | `arte/tripo/filhote/` | | | |

Os prompts previstos estão em `ASSETS.md` §0–§3 (assunto + sufixos).
- Registre aqui o **texto exato usado**: no Forge, o Lote 4 ficou sem o prompt exato no ledger e precisou ser reconstruído.
- Registre um SHA para o zip e outro para o FBX extraído.

## 3. Clipes Mixamo

| Arquivo (`arte/mixamo/...`) | Conjunto | Clipe no Mixamo (nome exibido) | Configuração | Data | SHA-256 |
|---|---|---|---|---|---|
| `Hold.fbx` | courier, ladrao | | FBX Binary, Without Skin, 30 fps, sem redução, X Bot | | |
| `courier/CarryRun.fbx` | courier | Running (cópia de `courier/Run.fbx`) | In Place | | |
| | | | | | |

Fonte: mixamo.com, na conta Adobe do Vinicius, personagem X Bot (nenhum modelo nosso é enviado).

## 4. Imagens grátis, texturas e fontes (0 crédito)

| Arquivo | Ferramenta / origem | Prompt ou URL | Escolhida | Data | SHA-256 | Licença |
|---|---|---|---|---|---|---|
| `piso` (textura) | Tripo, gerador de imagem embutido | | #_ de 4 | | | tripo_pago |
| `parede` (textura) | idem | | | | | tripo_pago |
| Lilita One | Google Fonts (github.com/google/fonts, `ofl/lilitaone`) | — | — | | | SIL OFL 1.1 |
| Nunito | Google Fonts (`ofl/nunito`) | — | — | | | SIL OFL 1.1 |

## 5. Sprites renderizados (0 crédito)

| Pasta `Resources/Sprites/` | Data | Entrada (FBX + SHA-256) | Clipes / Hold | SHA-256 do `render_sprites.py` | Comando | Folha de conferência |
|---|---|---|---|---|---|---|
| | | | | | | |

O SHA dos PNG não é reprodutível bit a bit, porque o AA do Workbench varia alguns níveis entre renders (Forge §6). Registre o SHA da entrada e o do script.

## 6. Modelo de bloco por peça (copiar quando a linha do §2 não bastar)

```
### <asset>
- categoria / lote: Personagem | Prop — ASSETS.md Lote _ #_
- estado: PLANEJADO | GERADO | RIGADO | EXPORTADO | RENDERIZADO | APROVADO (data, operador)
- ferramenta: Tripo Studio — imagem (Nano Banana 2 | GPT Image 2, 1:1, Multi-View s/n), modelo HD H3.1, rig Auto Rig Mixamo
- plano: pago (conta do estúdio, plano Max) · comprovante_plano: nº da fatura (nunca dado de cartão)
- prompt_imagem (exato):
- prompt_vistas (Multi-View: lado / costas, exatos):
- parametros: ~8 000 polígonos, triângulos, 2K, Ultra, remover iluminação, PBR, Gerar em Partes off, 8K off, Pose T, PRIVADO
- imagem_escolhida: #_ de 4 — motivo (ART_BIBLE §4: silhueta, cor de papel, mãos vazias, pernas à mostra)
- veredito_vinicius (antes do rig): data + aprovado/reprovado
- creditos: previsto _ · gasto _ · saldo antes _ → depois _
- task_id / url:
- arquivo_baixado: arte/tripo/<asset>/<zip> (SHA-256 ___, bytes ___) → <fbx> (SHA-256 ___)
- validacao_blender: tris, altura antes de normalizar, nº de ossos e prefixo, escala do armature
- licenca: tripo_pago · termos: https://www.tripo3d.ai/terms (versão "Last updated: July 11, 2025" segundo o COE; reler no dia)
- visibilidade: privada
- obs:
```

## 7. Licenças e termos

- **Tripo:** só o plano pago permite uso comercial. O gratuito é "Non-Commercial Use" e pode publicar o modelo: `tripo_free` reprova a peça. Usa-se a conta do estúdio, plano Max (`ARKANA/design/cenario/TRIPO-STUDIO.md`, 30/09), com Privado marcado **antes** de gerar.
- **Mixamo:** o FAQ da Adobe fala em uso comercial royalty-free e proíbe redistribuir os arquivos crus. Por isso `arte/mixamo/` fica fora do git. FAQ não é termo: reler os termos completos antes do lançamento (pendência herdada do PIPELINE §5).
- **Fontes:** SIL OFL 1.1. Embutir o `OFL.txt` junto do arquivo da fonte no projeto.
- **Reuso do Forge** (baú, clipes): só com aval do Vinicius neste chat. Nesse caso, a linha copia o task id e o SHA do ledger do Forge e anota "reuso 03_FORGE_STREET".
