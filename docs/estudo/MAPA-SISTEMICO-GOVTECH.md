# MAPA SISTÊMICO GOVTECH — O Ecossistema em que o Município se Insere

> **Documento de síntese.** Consolida as quatro partes do estudo (`docs/estudo/partes/estudo-*.md`):
> funções e obrigações transversais, mapa estadual/controle (TCE-RS), mapa federal fiscal e mapa
> federal setorial. Objetivo: **visão sistêmica e relacional** — quais sistemas existem (federal,
> estadual, controle), **o que cada um faz**, **como se relacionam** e **para onde o município presta
> contas** (destino → obrigação → periodicidade → formato → canal).
>
> **Princípio-âncora (spec contábil Tensorroot):** a **contabilidade PCASP é a FONTE**.
> `LancamentoContabil` → `Balancete` → **MSC**. Da MSC derivam RREO/RGF/DCA (SICONFI) e a remessa
> SIAPC/PAD (TCE-RS). Os módulos setoriais (Saúde/Educação/Assistência/RH/Compras) são **alimentadores
> upstream** que produzem fatos contábeis e cadastros; os Tribunais e a STN são **consumidores downstream**.
> **Multi-tenant:** Executivo e Legislativo do mesmo município são **tenants distintos** (CNPJs distintos)
> → cada um presta contas separadamente. Convenção CLAUDE.md §16: `[OFICIAL]` exige leiaute/versão vigente;
> `[a confirmar]` = validar na fonte oficial do exercício antes de implementar. Data: 2026-06-22.

---

## 1. As três camadas do ecossistema (quem é quem)

O município (Poder Executivo + Poder Legislativo, CNPJs distintos) está cercado por três anéis de
sistemas. **Nenhum substitui o outro**; o ERP é a FONTE que os alimenta.

```
                          ┌──────────────────────────── ANEL FEDERAL ───────────────────────────┐
                          │  FISCAL:   SICONFI/STN (MSC→RREO/RGF/DCA) · SADIPEM (CAPAG/crédito)   │
                          │            PNCP (contratos) · RFB (eSocial/EFD-Reinf) · BACEN (PIX)  │
                          │  SETORIAL: SIOPS·FNS (saúde) · SIOPE·Educacenso·SiGPC (educação)      │
                          │            CadÚnico·SUASWeb·RMA·Censo SUAS (assistência)             │
                          └─────────────────────────────────▲───────────────────────────────────┘
                                                            │ remessas / declarações
   ┌──────────── ANEL ESTADUAL/CONTROLE (RS) ───────────────┤
   │  TCE-RS: SIAPC/PAD · Contas Anuais (DCASP) · LicitaCon │      ┌──── MUNICÍPIO ────┐
   │          · SICOE (obras) · e-Validador/e-TCERS · BLM   │◄─────┤  EXECUTIVO (CNPJ) │ ← contabilidade
   │  MPC-RS (dentro do TCE) · Câmara (julga contas governo)│      │  LEGISLATIVO(CNPJ)│   PCASP = FONTE
   │  Controle Interno (art.74 — comunica ao TCE)           │◄─────┤  + Controle Interno│
   │  SEFAZ-RS/Tesouro-RS (ICMS/IPVA/convênios estaduais)   │      └───────────────────┘
   └────────────────────────────────────────────────────────┘
                                                            │ achados / improbidade / penal
   ┌──────────── ANEL DIFUSO ───────────────────────────────┘
   │  MPE/MPF (improbidade Lei 8.429 c/ LGL 14.230/21, ação penal) · Cidadão (denúncia art.74 §2º)
   │  ANPD (LGPD) · Internet/cidadão (LAI: transparência ativa + e-SIC passiva)
   └──────────────────────────────────────────────────────────────────────────────────────────────
```

- **Anel federal — fiscal:** **SICONFI/STN é o hub** (a MSC é a peça-mãe; RREO/RGF/DCA derivam dela);
  SADIPEM consome a qualidade do SICONFI via CAPAG; PNCP é condição de eficácia dos contratos;
  RFB recebe folha/retenções (eSocial + EFD-Reinf, que substituíram a DIRF); BACEN é arranjo de
  arrecadação (PIX), não destino de prestação de contas.
- **Anel federal — setorial:** **espelham o SICONFI** (SIOPS/SIOPE são contábeis e bimestrais) ou
  condicionam o **repasse fundo a fundo** (FNS/FNDE/FNAS). A regra de ouro: repasse é automático
  **enquanto** conselhos, planos e prestação de contas estão em dia; atraso → bloqueio + **CAUC**.
- **Anel estadual/controle (RS):** **TCE-RS** recebe as remessas (SIAPC/PAD, LicitaCon, SICOE, contas
  anuais); valida canonicamente (PAD/e-Validador/e-TCERS); **emite parecer prévio** (contas de
  governo, que a **Câmara julga**) e **julga** contas de gestão. **MPC-RS** atua dentro do TCE;
  **Controle Interno** comunica irregularidades ao TCE sob pena de responsabilidade solidária (art. 74 §1º).
- **Anel difuso:** MPE/MPF, cidadão, ANPD e a própria internet (LAI). Não recebem remessa estruturada,
  mas consomem os dados como prova/transparência.

---

## 2. Relações estruturantes (como os sistemas conversam)

1. **MSC é a peça-mãe federal.** Contabilidade PCASP → Balancete → **MSC** → e dela derivam RREO, RGF
   e DCA. A mesma fonte alimenta a remessa **SIAPC/PAD** estadual. Um único fato contábil irriga os
   dois anéis (estadual e federal). É o eixo do M3/M4.
2. **SIOPS/SIOPE são "primos contábeis" do SICONFI.** Bimestrais, derivam da execução orçamentária
   (funções 10-saúde e 12-educação) para verificar os pisos constitucionais (15% LC 141; 25% art. 212).
   Reusam a fábrica de exportadores derivados da contabilidade.
3. **Cadeia de crédito:** MSC/DCA bem entregues no SICONFI ⇒ **CAPAG** saudável ⇒ acesso a operação de
   crédito com garantia da União via **SADIPEM** (PVL/CDP). Qualidade contábil = capacidade de endividar-se.
4. **Repasse fundo a fundo (FNS/FNDE/FNAS)** é regular e automático, **condicionado** a conselhos +
   planos + prestação de contas (RAG/saúde, SiGPC/educação, SUASWeb/assistência) e a índices (IGD-PBF/SUAS).
   O ERP **contabiliza o ingresso por bloco/fonte/programa** e gera os demonstrativos de prestação.
5. **PNCP é gatilho de eficácia, não periódico.** O contrato só produz efeitos após publicação
   (20 dias úteis licitação / 10 dias úteis contratação direta). O mesmo contrato vira insumo do
   **LicitaCon** (TCE-RS) e referência do empenho (execução orçamentária).
6. **eSocial + EFD-Reinf** substituem a DIRF: com vínculo → eSocial; sem vínculo (fornecedores/IRRF do
   ente) → EFD-Reinf R-4000. Ambos mensais (dia 15), XML A1 ICP-Brasil, e alimentam a folha que entra
   na contabilidade e na remessa de folha do TCE-RS (Res. 1099/2018).
7. **Validação canônica é do TCE/STN, não do ERP.** O Tensorroot **emula/produz** a remessa espelhando
   os leiautes (MT-ASCE, XBRL/CSV da MSC), mas a validação oficial é do **PAD/e-Validador** (estadual) e
   do **portal SICONFI** (federal) → tratar **versionamento de leiaute como dependência externa monitorada**.
8. **Dupla titularidade do controle externo:** **contas de governo** (anuais, globais do Prefeito) →
   parecer prévio do TCE + **julgamento pela Câmara** (parecer só cai por 2/3); **contas de gestão**
   (ordenador) → **julgadas pelo próprio TCE**. O ERP produz a base probatória de ambas.
9. **Câmara é tenant próprio:** presta SUAS contas (folha, duodécimo art. 29-A CF, limites) ao TCE-RS e
   gera MSC própria ao SICONFI — separadamente do Executivo.
10. **SEFAZ-RS/Tesouro-RS:** transferências constitucionais (cota-parte ICMS 25%, IPVA, IPI-Exp.) entram
    como **receita** (conciliação, sem prestação de contas da transferência em si); convênios estaduais
    voluntários **exigem prestação de contas ao concedente** (fluxo paralelo ao TCE).

---

## 3. Tabela-destino — PARA ONDE o município presta contas

> **Leitura:** destino → obrigação → periodicidade → formato → canal. Agrupado por anel.
> Esta é a síntese operacional; o detalhamento por **módulo** está em `docs/planejamento/MAPA-PRESTACAO-CONTAS.md`.

### 3.1 Anel FEDERAL — fiscal/financeiro

| Destino | Obrigação | Periodicidade | Formato | Canal |
|---|---|---|---|---|
| **SICONFI/STN** | **MSC Agregada** (saldos PCASP do ente) | Mensal — último dia do mês seguinte | CSV ou XBRL GL (taxonomia Siconfi) | Portal autenticado + API REST |
| **SICONFI/STN** | **MSC de Encerramento** (dez → base DCA) | Anual — até 31/mar do exercício seguinte | CSV / XBRL GL | Portal + API |
| **SICONFI/STN** | **RREO** (execução orçamentária, derivada da MSC) | Bimestral — 30 dias após o bimestre | Declaração SICONFI (form./XBRL) | Portal + API |
| **SICONFI/STN** | **RGF** (limites pessoal/dívida) | Quadrimestral — 30 dias após (semestral facult. < 50k hab.) | Declaração SICONFI | Portal + API |
| **SICONFI/STN** | **DCA** (Declaração de Contas Anuais) | Anual — até 30/abr (LC 178/2021) | Declaração SICONFI | Portal + API |
| **SADIPEM/STN** | **PVL** (operação de crédito/garantia) + **CDP** (estoque da dívida) | Por evento (PVL); CDP periódico `[a confirmar]` | Web/portal | sadipem.tesouro.gov.br + API consulta |
| **PNCP** (Min. Gestão) | Editais, atas RP, **contratos/aditivos**, contratações diretas, PCA | Por evento — **condição de eficácia**: 20 dias úteis (licitação) / 10 (direta) | API REST/JSON UTF-8 (JWT ~1h p/ manutenção) | Sistema-a-sistema (sem entrada manual) |
| **RFB — eSocial** | Tabelas, vínculos, **folha** (periódicos), SST | Mensal — periódicos até dia 15 do mês seguinte | XML assinado A1 ICP-Brasil vs XSD | WebService SOAP |
| **RFB — EFD-Reinf (R-4000)** | Retenções IRRF/CSLL/PIS/COFINS sem vínculo (fornecedores) | Mensal — até dia 15 do mês seguinte | XML assinado ICP-Brasil | WebService SPED/RFB |
| **BACEN (PIX)** | *(arrecadação — não é prestação de contas)* | — | PIX Cobrança/QR Code | via banco arrecadador/PSP |
| **Tesouro Gerencial/STN** | *(consulta de transferências da União — não é remessa)* | — | BI sobre SIAFI | acesso habilitado |

### 3.2 Anel FEDERAL — setorial (pisos e fundo a fundo)

| Destino | Obrigação | Periodicidade | Formato | Canal |
|---|---|---|---|---|
| **SIOPS** (MS/DATASUS) | Execução em saúde — piso 15% ASPS (LC 141) | Bimestral — 30 dias após o bimestre | Declaração/aplicativo SIOPS | Transmissão eletrônica DATASUS |
| **SIOPE** (FNDE) | Execução em educação — MDE 25% (art. 212) + FUNDEB | Bimestral | Sistema SIOPE (validação SIOPE-MAVS) | Portal FNDE |
| **FNS — RAG** | Relatório Anual de Gestão (aplicação fundo a fundo saúde) | Anual (apreciado pelo Conselho de Saúde) | Demonstrativo / sistema MS | Rede MS |
| **FNDE — SiGPC** | Prestação de contas PNAE/PNATE/PDDE | Anual — ~30/abr (prorrog. por portaria) | SiGPC Contas Online / Sigecon | Portal FNDE |
| **Educacenso/INEP** | Censo Escolar (Escola/Turma/Aluno/Profissional) — base do FUNDEB | Anual — ref. última quarta de maio (2026: 27/mai) | Sistema web Educacenso | educacenso.inep.gov.br |
| **CNES/SCNES** (MS) | Cadastro de estabelecimentos/equipes/profissionais | Mensal (competência) | Sistema CNES | DATASUS |
| **CadÚnico** (MDS/Caixa) | Cadastro/atualização de famílias (base Bolsa Família) | Atualização ≥ 2 anos + AVE/REV anuais | Sistema CadÚnico | descentralizado municipal |
| **SUASWeb** (MDS) | Demonstrativo Sintético da Execução Físico-Financeira | Anual (apreciação do CMAS no sistema) | Rede SUAS | suasweb |
| **RMA** (MDS) | Registro Mensal de Atendimentos (CRAS/CREAS/Centro POP) | Mensal | Sistema RMA | Rede SUAS |
| **Censo SUAS** (MDS) | Caracterização de unidades/serviços/conselhos | Anual | Questionário eletrônico | Rede SUAS |
| **IGD-PBF / IGD-SUAS** | Índices de gestão (condicionam repasse) | Apuração mensal | — (apurado pelo MDS) | repasse proporcional |

### 3.3 Anel ESTADUAL / CONTROLE (RS)

| Destino | Obrigação | Periodicidade | Formato | Canal |
|---|---|---|---|---|
| **TCE-RS — SIAPC/PAD** | Execução orçamentária/financeira/contábil/patrimonial + receita | Mensal — ~30 dias corridos após o mês | Texto largura fixa, 1 reg./linha, CR/LF, sem packed; Código de Remessa único | PAD (valida local) → e-Validador/e-TCERS |
| **TCE-RS — Folha** (Res. 1099/2018) | Cadastro de servidores, vantagens/descontos, totalizadores | Mensal — ~30 dias após o mês | Texto largura fixa (`TCE_4810.TXT`) | PAD/e-Validador |
| **TCE-RS — Contas Anuais** (Res. 1134/2020) | Balanço Geral + DCASP (7 demonstrações) + encerramento | Anual `[datas a confirmar p/ exercício]` | Leiaute Contas Anuais | e-Validador/e-TCERS (assinatura ICP-Brasil) |
| **TCE-RS — LicitaCon** (IN 13/2017) | Licitações, dispensas/inexigibilidades, contratos/aditivos | Por evento/mensal `[a confirmar]` | Arquivos leiaute LicitaCon | LicitaCon Web ou e-Validador → RVE/e-Protocolo |
| **TCE-RS — SICOE** | Acompanhamento de obras públicas (medições) | Trimestral — últ. dia útil do mês seguinte `[a confirmar]` | Leiaute SICOE | e-Validador (certificado digital) → RVE |
| **TCE-RS — BLM** | Banco de Legislação Municipal | Contínuo | Cadastro web | site TCE-RS |
| **Câmara → TCE-RS** | Contas próprias (folha, duodécimo art. 29-A, limites) — **tenant/CNPJ próprio** | Mensal (SIAPC) + Anual | Texto largura fixa `[leiaute Legislativo a confirmar]` | PAD/e-Validador |
| **SEFAZ-RS / concedente** | Prestação de contas de **convênios estaduais** (voluntários) | Por convênio | Sistema do concedente `[a confirmar]` | portal estadual |

### 3.4 Anel DIFUSO (controle social / transversal)

| Destino | Obrigação | Periodicidade | Formato | Canal |
|---|---|---|---|---|
| **Cidadão (LAI ativa, art. 8)** | Portal da Transparência (receitas, despesas, contratos, repasses) | Tempo real / contínuo | Web + dados abertos | Internet |
| **Cidadão (LAI passiva, art. 11)** | Resposta a pedido de informação | 20 dias (+10 justificados) | e-SIC | Internet |
| **ANPD (LGPD)** | Encarregado/DPO indicado; RIPD quando aplicável | Permanente / por tratamento `[RIPD a confirmar]` | — | canal ANPD |
| **TCE-RS (Controle Interno, art. 74 §1º)** | Ciência obrigatória de irregularidade (RCI/parecer anexo às contas) | Por evento + anual | Documento assinado | trilha imutável → TCE |
| **MPE/MPF, cidadão (art. 74 §2º)** | *(consomem achados — improbidade, ação penal, denúncia)* | Sob demanda | — | externo |

---

## 4. Calendário-síntese (quem envia o quê, quando)

- **Mensal** → MSC (SICONFI, últ. dia do mês seguinte) · SIAPC/PAD + Folha (TCE-RS, ~30 dias) ·
  eSocial + EFD-Reinf (RFB, dia 15) · CNES, RMA, IGD (setorial).
- **Bimestral** → RREO (SICONFI, 30 dias) · SIOPS (saúde) · SIOPE (educação).
- **Trimestral** → SICOE/obras (TCE-RS) `[a confirmar]`.
- **Quadrimestral** (ou semestral < 50k hab.) → RGF (SICONFI, 30 dias).
- **Anual** → Contas Anuais/DCASP (TCE-RS) · DCA + MSC de encerramento (SICONFI, 31/mar–30/abr) ·
  SiGPC/PNAE (~30/abr) · RAG (saúde) · SUASWeb + Censo SUAS (assistência) · Educacenso (ref. maio).
- **Por evento** → PNCP (contrato: 20/10 dias úteis) · LicitaCon (TCE) · SADIPEM/PVL (operação de crédito).
- **Contínuo/tempo real** → Transparência ativa (LAI art. 8) · arrecadação PIX.
- **Sob demanda** → e-SIC (LAI passiva, 20+10 dias) · requisições MPE/MPF.

> **Driver de negócio crítico (RNF):** a **tempestividade** das remessas setoriais e fundo a fundo
> (SiGPC/SUASWeb/SIOPS/SIOPE/eSocial) condiciona repasses; atraso → **bloqueio/CAUC**. O ERP precisa
> de **calendário de obrigações + alertas** como requisito não funcional, não como conveniência.

---

## 5. Implicações de produto (encaixe Tensorroot.Gov)

1. **A MSC é o centro de gravidade.** M3/M4 já a produzem; SIOPS/SIOPE são exportadores derivados da
   mesma contabilidade — reusar a fábrica e as fitness functions (espelho do SICONFI).
2. **Fonte/bloco/programa no PCASP** é pré-requisito para rastrear ingresso fundo a fundo → aplicação →
   prestação de contas (SUASWeb/SiGPC/RAG). Forte interseção com M4.
3. **Validação canônica fica fora** (PAD/e-Validador/portal SICONFI): tratar versão de leiaute como
   dependência externa monitorada (Outbox + Polly p/ transmissão; alerta de quebra a cada nova versão do PAD).
4. **Transparência é o TRANSMISSOR** (M11): gera remessas a partir do Balancete/MSC, **assina com A1
   (cofre/envelope encryption — A1 já implementado)** e transmite. Não recalcula o conteúdo dos módulos.
5. **eSocial/EFD-Reinf são M5**; exigem MOS/XSD da versão vigente e modelagem multi-UO/descentralizada
   (já suportada por M1).
6. **Controle interno (art. 74) não é opcional:** precisa de trilha imutável da "ciência ao TCE" — alinhado
   à auditoria imutável (CLAUDE.md §4). Câmara é tenant próprio (presta contas separadamente).
7. **Antes de codar qualquer conector:** confirmar leiaute, versão e **modalidade (API vs. digitação)** na
   fonte oficial — vários sistemas setoriais só aceitam digitação, o que muda o desenho do conector (§16).

---

## 6. Fontes e documentos correlatos

- **Partes do estudo:** `docs/estudo/partes/estudo-funcoes-e-obrigacoes.md`,
  `estudo-mapa-estadual-controle.md`, `estudo-mapa-federal-fiscal.md`, `estudo-mapa-federal-setorial.md`
  (fontes oficiais primárias listadas em cada parte: Planalto, STN/SICONFI, TCU, gov.br, TCE-RS, MS, FNDE, MDS, RFB).
- **Detalhamento por módulo:** `docs/planejamento/MAPA-PRESTACAO-CONTAS.md`.
- **Constituição de engenharia:** `CLAUDE.md` §0 (missão), §4 (11 módulos), §6 (segurança/A1), §8
  (integrações governamentais), §16 (pesquisa antes de codar).

> Itens `[a confirmar]` consolidados: versão vigente do PAD/MT-ASCE e datas das Contas Anuais (Res. 1134);
> periodicidade exata de SICOE e CDP/SADIPEM; canal de transmissão programática do SICONFI; modalidade
> (API vs. digitação) de SIOPS/SIOPE/SiGPC/SUASWeb; leiaute de remessa do Legislativo; sistema de prestação
> de contas de convênios estaduais RS; prazo do RIPD (ANPD); versões de leiaute eSocial/EFD-Reinf do exercício.
