# PLANO DE CONFORMIDADE — Etapa (a)

> Plano de auditoria de conformidade legal/fiscal, ordenado por **risco para o PoC**.
> Fontes: ver `FONTES-NORMATIVAS.md` (IDs A1, B1, D1… referenciam aquele catálogo).
> Para cada item: **fonte → módulo/arquivo nosso a auditar → o que conferir (regra fina) → resultado esperado**.
> Jurisdição TCE-RS · Maximiliano de Almeida/RS · estrutura de código já mapeada em `src/Modules/*`.

**Como usar:** trabalhar de cima para baixo. P0 são bloqueantes do PoC (saída que o TCE-RS/SICONFI valida ou base de cálculo que, se errada, corrompe todo o resto). Resolver primeiro as **pré-condições** (abaixo) porque elas mudam o que auditar.

## Pré-condições (resolver ANTES de auditar — destravam decisões)

| Pré-cond | Fonte | Decisão a tomar | Bloqueia |
|----------|-------|-----------------|----------|
| PC1 | D5 | **RPPS (IPE-Prev/próprio) ou RGPS/INSS?** em Maximiliano de Almeida | motor de contribuição do RH (D5 vs. INSS) |
| PC2 | A1 | PCASP 2026 já incorporado na 11ª ed. MCASP ou portaria complementar? | plano de contas `Financas` |
| PC3 | B1, B7 | Versão corrente do leiaute SIAPC/PAD Vol. V e LicitaCon (1.4.010 é a última?) | remessas TCE-RS |
| PC4 | E3, E7 | Versão vigente do leiaute NFS-e ADN (NT 004 Reforma) e DES-IF ABRASF | motor ISS/NFS-e |

---

## P0 — BLOQUEANTES DO PoC (saída validada externamente + base de cálculo)

### P0.1 — MSC → SICONFI (meta nº1) · domínio Contábil
- **Fonte:** A2 (MSC Regras Gerais 2026), B4 (SICONFI RGF 2026).
- **Auditar:** `src/Modules/Financas/.../Domain/Contabilidade/Msc/` — `MatrizSaldosContabeis.cs`, `LinhaMsc.cs`, `EnumsMsc.cs`, `InformacoesComplementaresMsc.cs`; e `.../Application/Contabilidade/Msc/`.
- **Regra fina:** estrutura da MSC agregada vs. encerramento; conjunto de informações complementares obrigatórias (Anexo I 2026); tipos de valor/natureza; regras de consistência que o SICONFI rejeita. Conferir leiaute Anexo II (rev. 05/08/2026).
- **Esperado:** MSC gerada passa nas validações do SICONFI; RREO/RGF derivam dela.

### P0.2 — PCASP / DCASP (fundação contábil) · domínio Contábil
- **Fonte:** A1 (MCASP 11ª ed.), C1 (Lei 4.320), A4 (SIAFIC).
- **Auditar:** `Financas/.../Domain/Contabilidade/PlanoDeContas/`, `/EventosContabeis/`, `/Lancamentos/`, `/Encerramento/`; `/Empenhos/`, `/Liquidacoes/`, `/Pagamentos/`, `/RestosAPagar/`.
- **Regra fina:** PCASP estendido (natureza/atributos das contas); partidas dobradas dos eventos contábeis; ciclo empenho→liquidação→pagamento conforme Lei 4.320; estrutura das DCASP; integração única exigida pelo SIAFIC. Validar PCASP 2026 (PC2).
- **Esperado:** plano de contas e lançamentos batem com a 11ª ed.; balanços fecham.

### P0.3 — RREO/RGF (LRF) · domínio Fiscal
- **Fonte:** A3 (MDF 15ª ed.), C2 (LRF), B4.
- **Auditar:** `Financas` (geração demonstrativos) + `Transparencia/.../DeclaracoesFiscais/`, `/Fiscal/`; limite de pessoal em `PainelGestor`.
- **Regra fina:** leiaute RREO (bimestral) e RGF (quadrimestral p/ municípios); cálculo do limite de despesa com pessoal (LRF arts. 18–23); harmonização MDF. **Garantir que o limite NÃO usa LC 173/2020 (expirada).**
- **Esperado:** demonstrativos no formato MDF 15ª ed.; limite de pessoal pela LRF.

### P0.4 — SIAPC/PAD → TCE-RS · domínio Contábil/TCE
- **Fonte:** B1 (leiaute Vol. V), B2 (Res. 1099/2018), B3 (IN 06/2019), B5.
- **Auditar:** `Transparencia/.../RemessasTce/` + geração no `Financas`.
- **Regra fina:** estrutura dos arquivos de dados à disposição (balancetes, empenhos, liquidações, pagamentos, receita); periodicidade mensal (30 dias após competência); integração SISCAD/Processo Eletrônico. Confirmar versão do leiaute (PC3).
- **Esperado:** PAD válido aceito pelo e-Validador do TCE-RS.

### P0.5 — eSocial (folha) · domínio RH
- **Fonte:** D1 (MOS S-1.3), D2 (leiautes + XSD), D4 (TCE_4810/4820), B6 (SIAPESweb).
- **Auditar:** `RecursosHumanos/.../Domain/ESocial/`, `/Folha/`, `/Calculo/`, `/Rubricas/`, `/Servidores/`, `/Portarias/`.
- **Regra fina:** eventos S-1000/1010/1200/1210/2200/2206/2299/2300/3000/5001/5002/5003 conforme XSD S-1.3 (NT 06/2026); regras de validação do MOS (NO 07/2026); arquivos TCE_4810 (folha) e TCE_4820 (cadastro) no leiaute IN 06/2019; atos de admissão p/ SIAPESweb.
- **Esperado:** XML eSocial valida no XSD; TXTs do TCE batem com IN 06/2019.

### P0.6 — IRRF folha: EFD-Reinf, NÃO DIRF · domínio RH
- **Fonte:** D3 (EFD-Reinf 2.1.2, NT 01/2026).
- **Auditar:** `RecursosHumanos/.../Calculo/` (retenção IRRF) + qualquer geração de obrigação acessória.
- **Regra fina:** retenções via R-4010/4020/4099 (série R-4000). **DIRF EXTINTA — remover qualquer geração de DIRF.** Garantir que IRRF flui para S-1210 + EFD-Reinf.
- **Esperado:** zero geração de DIRF; R-4000 alimentado.

### P0.7 — Licitações: NLLC + PNCP + LicitaCon · domínio Licitações
- **Fonte:** C3/F1 (Lei 14.133), F2 (PNCP 2.3.5), F3 (Dec. 12.343/2024 valores), B7 (LicitaCon 1.4.010).
- **Auditar:** `Administracao/.../Domain/Licitacoes/`, `/Contratos/`, `/RegistroPrecos/`, `/Pca/`, `/Fornecedores/`.
- **Regra fina:** modalidades/fases NLLC; **tabela de limites de dispensa/modalidade na versão Dec. 12.343/2024** (muda anualmente); envio ao PNCP como condição de eficácia (art. 94) na API 2.3.5; remessa LicitaCon (14 CSV, inclui PDE). Confirmar versão (PC3).
- **Esperado:** processo válido na NLLC; publica no PNCP; remessa aceita pelo e-Validador LicitaCon.

---

## P1 — ALTO (jurídico-tributário + previdenciário + paridade)

### P1.1 — CTN: decadência/prescrição/CDA · domínio Tributos
- **Fonte:** C4/E1 (CTN), C6/E6 (Lei 6.830).
- **Auditar:** `Tributos/.../Domain/Lancamentos/`, `/Calculo/`, `/Dividas/`, `/Certidoes/`.
- **Regra fina:** decadência 5 anos (art. 173); prescrição 5 anos + causas de interrupção (art. 174); requisitos da CDA (art. 2º §5º Lei 6.830); suspensão/extinção (151/156).
- **Esperado:** cálculo correto de prazos; CDA com todos os requisitos legais.

### P1.2 — ITBI (Tema 1.113/STJ) · domínio Tributos
- **Fonte:** E2.
- **Auditar:** `Tributos/.../Domain/Itbi/`.
- **Regra fina:** base = valor de mercado; **NÃO** usar valor de referência/PGV como piso automático; valor declarado com presunção de veracidade; arbitramento só via processo administrativo (art. 148).
- **Esperado:** fluxo permite valor declarado + arbitramento; sem piso unilateral.

### P1.3 — ISS / NFS-e ADN · domínio Tributos
- **Fonte:** C5/E4 (LC 116), E3 (NFS-e ADN).
- **Auditar:** `Tributos/.../Domain/Iss/`, `/Nfse/`.
- **Regra fina:** lista de serviços espelha anexo LC 116; leiaute DPS/NFS-e/eventos do ADN na versão vigente (PC4); local da prestação/base de cálculo.
- **Esperado:** cadastro de serviços alinhado; XML NFS-e válido no ADN.

### P1.4 — IPTU / PGV · domínio Tributos
- **Fonte:** E5 (CTN art. 33, Tema 1084/STF, Súmula 160).
- **Auditar:** `Tributos/.../Domain/Pgv/`, `/Imoveis/`, `/Calculo/`.
- **Regra fina:** atualização de valor venal só por **lei** (decreto apenas correção monetária); avaliação de imóvel novo exige base legal + contraditório.
- **Esperado:** sistema não majora valor venal por decreto além da correção.

### P1.5 — RPPS / repasse previdenciário · domínio RH
- **Fonte:** D5 (IPE-Prev / LC est. 13.758).
- **Auditar:** `RecursosHumanos/.../Calculo/`, `/Folha/`, `/Consignacoes/`, `/Pasep/`.
- **Regra fina:** **condicionado a PC1** — se RPPS: alíquota servidor 11% + patronal, repasse mensal (30 dias); se RGPS: INSS via GPS/eSocial. PASEP via EFD-Reinf/eSocial.
- **Esperado:** motor de contribuição correto para o vínculo real do município.

### P1.6 — Atos de pessoal (SIAPESweb) · domínio RH/TCE
- **Fonte:** B6.
- **Auditar:** `RecursosHumanos/.../Servidores/`, `/Portarias/`, `/Cargos/`.
- **Regra fina:** remessa de atos de admissão (concurso/temporário) no leiaute SIAPESweb (import consist. 57); inativação/pensão via SAPIEM.
- **Esperado:** atos de pessoal exportáveis ao SIAPESweb.

### P1.7 — Convênios / MROSC · domínio Convênios
- **Fonte:** C8/G1 (Lei 13.019), G2 (Manual MROSC 2025), G3 (Transferegov), G4 (Dec. 8.726).
- **Auditar:** `Convenios/.../Domain/Mrosc/`, `/Recebidos/`, `/Parametros/`.
- **Regra fina:** termos (colaboração/fomento/cooperação); chamamento público; fases e prestação de contas conforme Manual MROSC 2025; integração Transferegov p/ recursos federais.
- **Esperado:** ciclo de parceria completo conforme Lei 13.019 + Manual 2025.

### P1.8 — Paridade funcional (TRs reais) · transversal
- **Fonte:** H1 (Porto Alegre — gold-standard), H3/H5 (General Câmara/Sede Nova — porte/jurisdição).
- **Auditar:** todos os módulos vs. checklist dos TRs.
- **Regra fina:** **≥90% de atendimento por módulo, 100% nos itens essenciais.** Mapear cada item do TR Porto Alegre a `src/Modules/*`; calibrar profundidade por General Câmara (município pequeno TCE-RS).
- **Esperado:** matriz de paridade ≥90%/módulo; lacunas priorizadas.

---

## P2 — MÉDIO (apoio + limpeza de desatualizações)

### P2.1 — SRP / Agente de contratação / PCA · Licitações
- **Fonte:** F4 (Dec. 11.462 SRP), F5 (Dec. 11.246), F6 (Dec. 10.947 PCA).
- **Auditar:** `Administracao/.../RegistroPrecos/`, `/Pca/`, fluxo de papéis em `/Licitacoes/`.
- **Regra fina:** atas de RP/carona (arts. 82–86); papéis (agente, comissão, fiscalização); PCA anual. Confirmar decreto municipal próprio.

### P2.2 — DES-IF (ISS bancos) · Tributos
- **Fonte:** E7. **Auditar:** `Tributos/Iss`. Recepção de declaração ABRASF (versão vigente). Criticidade menor p/ município pequeno.

### P2.3 — SIOPE (remuneração educação) · RH/Educação
- **Fonte:** D6. **Auditar:** `RecursosHumanos/Servidores`,`/Cargos` + `Educacao`. Classificação de profissionais da educação (Fundeb/mínimo constitucional).

### P2.4 — Protesto de CDA · Tributos
- **Fonte:** C7/E8. **Auditar:** `Tributos/Dividas`. Funcionalidade facultativa de protesto extrajudicial.

### P2.5 — Limpeza de obrigações substituídas/expiradas · transversal
- **Fonte:** D7 (RAIS/CAGED), D8 (LC 173/2020).
- **Ação:** confirmar que RH **não** gera RAIS/CAGED autônomos (derivam do eSocial) e **não** referencia LC 173/2020 como regra ativa (usar LRF — C2).

---

## Ordem de ataque sugerida para a etapa (a)

1. Resolver **PC1–PC4** (pré-condições) — destravam P0.5/P0.4/P0.2/P1.3.
2. **Bloco contábil-fiscal** P0.1 → P0.2 → P0.3 → P0.4 (encadeados: MSC depende do PCASP; RREO/RGF/PAD derivam da MSC).
3. **Bloco RH** P0.5 → P0.6 → P1.5 → P1.6.
4. **Bloco licitações** P0.7 → P1.7 → P2.1.
5. **Bloco tributos** P1.1 → P1.2 → P1.3 → P1.4 → P2.x.
6. **Paridade** P1.8 em paralelo (transversal).
7. **Limpeza** P2.5 ao final de cada bloco.
