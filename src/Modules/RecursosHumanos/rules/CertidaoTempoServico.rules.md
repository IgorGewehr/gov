---
modulo: RecursosHumanos
agregado: CertidaoTempoServico
contexto: RecursosHumanos (atos de pessoal / tempo de serviço e contribuição)
poder: Ambos
schema: recursoshumanos
ativavel_por_tenant: true
versao_regras: 1.0.0
fontes_legais: ["EC 103/2019 (contagem recíproca de tempo de contribuição; art. 25 §9 — vedações)", "Lei 8.213/1991 art. 96 (vedação de contagem em dobro e de contagem concomitante)", "Lei 9.717/1998 (RPPS)", "Portaria MPS 154/2008 (modelo/campos da CTC)", "CF/1988 art. 40/41 §3 (aposentadoria; disponibilidade)"]
---

<!-- manifest
commands: EmitirCertidaoTempoServico, AnularCertidaoTempoServico
queries: ListarCertidoesDoServidor, ObterCertidao, ValidarCertidao
domainEvents: CertidaoTempoServicoEmitida, CertidaoTempoServicoAnulada
integrationEventsPublished:
integrationEventsConsumed:
-->

# Certidão de Tempo de Serviço/Contribuição (CTC) — Regras-as-Code

> **Certidão de Tempo de Serviço/Contribuição** apura e declara o tempo do servidor (efetivo exercício
> próprio + tempo averbado de outros órgãos/regimes), com a **finalidade** que determina o regime de
> contagem. Numeração **sequencial por exercício/tenant** (`NNNN/AAAA`) e **código de autenticação** para
> validação pública. Documento de alto uso (aposentadoria, disponibilidade, adicionais por tempo). Este
> arquivo é **normativo e versionado**.
>
> ⚠️ `// TODO(validar-oficial)`: os **campos exatos** da CTC (Portaria MPS 154/2008), os **fatores de
> conversão** especial→comum e os **textos-fim** por finalidade devem ser confirmados contra o regulamento
> do RPPS do tenant e o manual de CTC do regime de destino antes do go-live (M10).

---

## 1. Linguagem Ubíqua

| Termo (identificador-no-código) | Definição |
|---|---|
| Certidão (`CertidaoTempoServico`) | Documento que apura/declara o tempo. Raiz de agregado. |
| Numeração (`NumeroCertidao`) | Sequencial por exercício/tenant, formatada `NNNN/AAAA`. |
| Finalidade (`FinalidadeCertidao`) | Aposentadoria / Disponibilidade / Adicional / Licença-prêmio / Declaratória. |
| Período (`PeriodoTempo`) | Intervalo `[Início, Fim]` com natureza, fator e dias não-computáveis. VO. |
| Natureza (`NaturezaPeriodo`) | Efetivo exercício próprio **ou** averbado de outro órgão/regime. |
| Fator (`Fator`) | Multiplicador de conversão do tempo (1,0 = comum; > 1,0 = especial→comum). |
| Dias equivalentes (`DiasEquivalentes`) | Dias líquidos × fator — o que entra no total. |
| Código de autenticação (`CodigoAutenticacao`) | Digest opaco para validação pública. |

---

## 2. Invariantes (Rules-as-Code)

- **I-1 — Pelo menos um período.** Certidão sem período não tem efeito (`CertidaoTempoServicoException`).
- **I-2 — Vedação de contagem CONCOMITANTE** (Lei 8.213/1991, art. 96, II). Períodos **não podem se
  sobrepor** no tempo; tempo concomitante é contado **uma única vez**. Emissão com sobreposição falha.
- **I-3 — Total = soma dos dias equivalentes.** Dias líquidos = brutos − não-computáveis; dias
  equivalentes = líquidos × fator (fator ≥ 1,0 — conversão apenas **majora**).
- **I-4 — Numeração preservada na anulação.** Anular é terminal; a sequência do exercício **não retrocede**.
- **I-5 — Autenticação selada uma vez.** O código é selado na emissão (digest determinístico calculado na
  borda) e emite `CertidaoTempoServicoEmitida`. Validação pública nunca confirma certidão **anulada**.

---

## 3. Apuração do efetivo exercício (`ApuradorTempoServidor`)

- Período próprio = `[DataExercicio .. data-base]` (data-base = informada → desligamento → hoje do tenant).
- Abate os **dias não-computáveis** dos afastamentos com `ContaTempo=false` (licença sem contagem de
  tempo), fazendo a **união** dos intervalos (sem dupla contagem) e recortando aos limites da apuração.
- Serviço de domínio **puro** (sem relógio/IO): a data e os afastamentos entram pela borda (Application).

---

## 4. Cenários BDD (resumo)

1. **Apuração sem afastamentos** → tempo = intervalo cheio `[exercício, data-base]` (inclusivo).
2. **Afastamentos sobrepostos** → o dia coberto por duas licenças conta **uma vez** (união).
3. **Conversão especial** → período com fator 1,40 sobre 365 dias = 511 dias equivalentes.
4. **Concomitância** → emissão com dois períodos que se sobrepõem **falha** (I-2).
5. **Emissão + autenticação** → nasce `Emitida`, sela o código, emite evento.
6. **Validação pública** → código vigente confirma (número/nome/finalidade/tempo); anulada **não** confirma.
7. **Anulação** → terminal, preserva numeração, emite evento; reanular falha.

> Cobertura em `tests/Tensorroot.Gov.Modules.RecursosHumanos.Tests/CertidaoTempoServicoDominioTests.cs`.
