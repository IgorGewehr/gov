# Estudo — Mapa Federal da Dimensão Fiscal/Financeira

> Para onde o **MUNICÍPIO** presta contas / integra no nível **FEDERAL** na dimensão fiscal/financeira.
> Escopo: SICONFI/STN, Tesouro Gerencial, SADIPEM, PNCP, Receita Federal (EFD-Reinf/eSocial), Banco Central (PIX/arrecadação), TSE.
> Convenção (CLAUDE.md §16): `[a confirmar]` = data/leiaute/versão a validar na fonte oficial do exercício antes de implementar; `[OFICIAL]` = exige leiaute/versão vigente.
> Princípio Tensorroot (spec contábil): a **contabilidade PCASP é a FONTE**. `LancamentoContabil` → `Balancete` → **MSC**; da MSC derivam RREO/RGF/DCA. Tudo o que vai ao SICONFI no nível fiscal nasce da MSC.

---

## 1. SICONFI / STN — núcleo da prestação de contas fiscal federal

**Órgão:** Secretaria do Tesouro Nacional (STN) / Ministério da Fazenda.
**O que é:** sistema que recebe informações contábeis, financeiras e estatísticas fiscais de Municípios, Estados, DF e União. É o **hub federal** da dimensão fiscal: tudo o que o município "manda para Brasília" no plano contábil/fiscal passa por aqui.

| Obrigação | O que envia | Periodicidade | Formato | Canal | Base legal / [OFICIAL] |
|---|---|---|---|---|---|
| **MSC Agregada** | Matriz de Saldos Contábeis: conta PCASP + saldo + natureza + informações complementares de todos os órgãos/poderes do ente | **Mensal**, até o último dia do mês seguinte ao mês de referência | **CSV** ou **XBRL GL** (taxonomia Siconfi) | Upload no portal + **API REST** | `[OFICIAL]` Port. STN 642 (Regras Gerais MSC, anexo do exercício) |
| **MSC de Encerramento** | MSC de dezembro com encerramento contábil → base do rascunho da DCA | **Anual**, até o **último dia de março** do exercício seguinte | CSV / XBRL GL | Portal + API | `[OFICIAL]` |
| **RREO** — Rel. Resumido da Execução Orçamentária | Execução orçamentária (derivada da MSC); anexos LRF | **Bimestral**, até **30 dias** após o fim do bimestre | Declaração SICONFI (formulário/XBRL) | Portal + API | `[OFICIAL]` LRF (LC 101/2000) art. 52-53 |
| **RGF** — Rel. de Gestão Fiscal | Limites de pessoal, dívida, garantias, RP (derivado da MSC) | **Quadrimestral**, até **30 dias** após o quadrimestre. Municípios **< 50.000 hab.** podem optar por publicação **semestral** | Declaração SICONFI | Portal + API | `[OFICIAL]` LRF arts. 54-55 |
| **DCA** — Declaração de Contas Anuais | Contas anuais consolidadas de todos os poderes/órgãos do ente (deriva da MSC de encerramento) | **Anual**, até **30 de abril** do exercício seguinte (LC 178/2021 antecipou de 31/mai para 30/abr) | Declaração SICONFI | Portal + API | `[OFICIAL]` LRF art. 51; LC 178/2021 |

**Notas de integração:**
- A **API de consulta** do SICONFI é REST, **pública e sem autenticação** (extração de dados já entregues por +5.500 municípios e 27 UFs). Doc: `apidatalake.tesouro.gov.br/docs/siconfi/`.
- O **envio** (transmissão de declarações) é feito pelo portal autenticado / carga de arquivo (CSV ou XBRL GL para a MSC). `[a confirmar]` se há canal de transmissão programática (não apenas consulta) para o exercício corrente.
- **Multi-tenant (Tensorroot):** Executivo e Legislativo do mesmo município têm **CNPJs distintos** → cada um gera **MSC própria**; o ente consolida. A Câmara presta contas separadamente ao SICONFI.

---

## 2. Tesouro Gerencial — consulta/BI sobre SIAFI (não é ponto de entrega do município)

**Órgão:** STN. **O que é:** plataforma de **business intelligence** para consulta e análise da execução orçamentária/financeira/patrimonial registrada no **SIAFI** (sistema da União).
**Relação com o município:** o **município NÃO escritura no SIAFI nem no Tesouro Gerencial** — esses são da União. O município **aparece** no SIAFI/Tesouro Gerencial como **beneficiário de transferências federais** (convênios, fundo a fundo, repasses). Serve para o gestor municipal **consultar** transferências recebidas e para o controle federal cruzar dados. Acesso requer cadastro/habilitação no SIAFI (perfil específico, mesma senha do SIAFI). `[a confirmar]` perfis de acesso para entes municipais.
**Conclusão:** ponto de **consulta/conciliação** (receitas de transferência da União), não de **remessa** obrigatória do município.

---

## 3. SADIPEM — dívida pública e operações de crédito

**Órgão:** STN. **Sigla:** Sistema de Análise da Dívida Pública, Operações de Crédito e Garantias da União, Estados e Municípios.
**O que envia:**
- **PVL** (Pedido de Verificação de Limites e Condições): submissão para **contratar operação de crédito** ou obter **garantia da União** — exigência da LRF e Res. Senado 40/43.
- **CDP** (Cadastro da Dívida Pública): atualização e consulta do estoque da dívida do ente.

**Periodicidade:** **por evento** (cada nova operação de crédito/garantia gera um PVL); o **CDP** é atualizado periodicamente `[a confirmar]`.
**Formato/canal:** sistema web `sadipem.tesouro.gov.br` (portal); há **API SADIPEM** documentada nos manuais da STN (consulta).
**Relação com SICONFI/CAPAG:** a análise da **CAPAG** (Capacidade de Pagamento, notas A-D) que habilita o ente a tomar empréstimo com garantia da União **usa os dados que o ente declarou no SICONFI** (a STN/COREM abre prazo para o município disponibilizar documentos **via SICONFI**). Logo: **MSC/DCA bem entregues no SICONFI ⇒ CAPAG saudável ⇒ acesso a crédito via SADIPEM.** É a cadeia fiscal completa: contabilidade → SICONFI → CAPAG → SADIPEM.

---

## 4. PNCP — Portal Nacional de Contratações Públicas (contratos)

**Órgão:** Federal (gestão Min. Gestão/Economia). **Base legal:** Lei 14.133/2021, art. 174; Dec. 12.807/2025 `[a confirmar versão vigente]`.
**O que envia:** editais, atas de registro de preços, **contratos e aditivos**, contratações diretas (dispensa/inexigibilidade), PCA (Plano de Contratações Anual).
**Periodicidade (por evento, como condição de eficácia — art. 94):** contado da assinatura —
- **20 dias úteis** quando decorrente de **licitação**;
- **10 dias úteis** em **contratação direta**;
- Obras: quantidades/preços em até **25 dias úteis** após assinatura; executados em até **45 dias úteis** após conclusão (publicidade adicional no site oficial).

**Formato/canal:** **API REST / JSON (UTF-8)**; as **APIs de manutenção** (inserção/correção/exclusão) exigem **autenticação/autorização** (plataforma credenciada → login/senha; token **JWT** com expiração ~1h). As **APIs de consulta** são públicas. **Sem entrada manual** no fluxo Tensorroot (integração sistema-a-sistema).
**Relação:** a publicação no PNCP é **condição de eficácia** do contrato — sem ela o contrato não produz efeitos. Conecta-se à execução orçamentária (empenho referencia o contrato) e, no Estado, ao SIAPC/LicitaCon do TCE-RS.

---

## 5. Receita Federal — EFD-Reinf e eSocial (retenções e folha)

**Órgão:** RFB (SPED). O município, como **fonte pagadora**, escritura retenções na fonte.

### 5a. eSocial
- **O que envia:** eventos de tabelas (S-1000), remunerações (S-1200/S-1202 – servidor RPPS, S-1207), admissão/desligamento (S-2200/S-2299), fechamento (S-1299). Retenções de IR **decorrentes de relação de trabalho** vão pelo eSocial.
- **Periodicidade:** **mensal** — periódicos até **dia 15** do mês seguinte.
- **Formato/canal:** **XML assinado A1 ICP-Brasil** validado contra XSD; **WebService SOAP**.
- **[OFICIAL]** leiaute eSocial S-1.3 `[a confirmar versão do exercício]`.

### 5b. EFD-Reinf (série R-4000)
- **O que envia:** **série R-4000** = pagamentos/créditos com **IRRF** e retenções de **CSLL/PIS/COFINS** a pessoas físicas/jurídicas **sem vínculo de trabalho** (fornecedores, prestadores). Para o setor público, o titular da receita do **IRRF** sobre pagamentos do próprio ente é o **município** (CF art. 158, I) — relevância fiscal direta.
- **Periodicidade:** **mensal**, transmissão até **dia 15** do mês subsequente ao fato gerador; evento de fechamento mensal.
- **Formato/canal:** XML assinado ICP-Brasil; WebService SPED/RFB.
- **Relação:** EFD-Reinf + eSocial **substituem a DIRF** (extinta). Divisão de competência: **com vínculo → eSocial; sem vínculo → EFD-Reinf**. Ambos alimentam a apuração da RFB e a DCTFWeb.
- **[OFICIAL]** Manual EFD-Reinf vigente `[a confirmar versão]`.

> Escopo Tensorroot: módulo **RecursosHumanos** (eSocial) + **Financas/Tributos** (EFD-Reinf R-4000, retenções de fornecedores na liquidação/pagamento). Estes são o ponto de contato **fiscal-federal do RH e da despesa**.

---

## 6. Banco Central — PIX / arrecadação

**Órgão:** BACEN. **Natureza:** **arrecadação** (entrada de receita), não "prestação de contas".
**O que muda:** o município pode **receber tributos municipais** (IPTU, ISSQN, ITBI, taxas, dívida ativa) **via PIX** — QR Code dinâmico vinculado ao DAM/guia. Reduz custo de arrecadação e amplia a rede de recebimento.
**Formato/canal:** **PIX Cobrança / QR Code** (padrão BACEN — Manual de Padrões para Iniciação do PIX); na prática integrado via **banco arrecadador** (ex.: BB) ou PSP credenciado, não diretamente no DICT pela prefeitura. O DICT (diretório de chaves) é infra do arranjo PIX, acessado pelos PSPs.
**Relação fiscal:** a receita arrecadada via PIX → **baixa do crédito tributário** no módulo Tributos → **lançamento contábil de receita** (PCASP) → compõe a **MSC** → reflete em RREO/DCA no SICONFI. É a **ponta de entrada** que fecha o ciclo até a prestação de contas federal. `[a confirmar]` se há obrigação de reporte ao BACEN além do papel de recebedor (não há prestação de contas do município ao BACEN; relação é de **arranjo de pagamento**).

---

## 7. TSE — prestação de contas eleitoral (condicional)

**Órgão:** Justiça Eleitoral (TSE/TREs). **Aplicabilidade:** **NÃO é obrigação do ENTE/prefeitura** — é de **candidatos, partidos e federações** em ano eleitoral. Entra no escopo de um ERP municipal apenas marginalmente (ex.: cessão de dados, transparência) e quando o município é palco de eleição municipal (prefeito/vereador).
**O que envia (quem deve):** movimentação de campanha (receitas/gastos), por esfera de competência (municipal → TRE/Juízo Eleitoral local).
**Periodicidade/formato:** conforme **Resolução** do exercício eleitoral; preenchimento no **SPCE** (Sistema de Prestação de Contas Eleitorais), instalado localmente, arquivo gerado pelo próprio sistema e transmitido pela internet via SPCE.
**Conclusão para Tensorroot:** **fora do core** da prestação de contas fiscal do ente. Listado por completude; marcar como **não aplicável ao ERP municipal** salvo requisito específico. `[a confirmar]` se há requisito do piloto.

---

## 8. Visão integrada — como tudo se relaciona

```
[Arrecadação]                 [Despesa/Contratação]            [Folha/Retenções]
 PIX/BACEN ──► Tributos         PNCP (contratos) ──► empenho     eSocial (vínculo)
 (baixa crédito)                 |                                EFD-Reinf R-4000
        │                        ▼                                (sem vínculo / IRRF do ente)
        └──────────► CONTABILIDADE PCASP (LancamentoContabil) ◄────────────┘
                              │
                              ▼
                        BALANCETE ──► MSC (Matriz de Saldos Contábeis)
                              │
        ┌─────────────────────┼──────────────────────────┐
        ▼                     ▼                           ▼
   SICONFI/STN          SADIPEM (CAPAG usa            (TCE-RS: SIAPC/PAD —
   MSC / RREO /          dados do SICONFI →            dimensão ESTADUAL,
   RGF / DCA             crédito c/ garantia União)    fora deste estudo)

   Tesouro Gerencial = consulta da União (transferências p/ o município) — não é remessa.
   TSE = candidatos/partidos, não o ente — não aplicável ao core.
```

**Eixo central:** **SICONFI é o destino federal fiscal por excelência**, e a **MSC é a peça-mãe** (RREO, RGF e DCA derivam dela). PNCP, eSocial e EFD-Reinf são **alimentadores upstream** (contratos, folha, retenções) que produzem os fatos contábeis; PIX é a **entrada de receita**; SADIPEM **consome** a qualidade do SICONFI (CAPAG); Tesouro Gerencial e TSE são **periféricos** (consulta / não-aplicável).

---

## 9. Resumo executivo (prioridade para o ERP municipal)

| Destino | Tipo | É remessa do ENTE? | Prioridade Tensorroot |
|---|---|---|---|
| **SICONFI** (MSC/RREO/RGF/DCA) | Prestação de contas fiscal | **Sim** | **Alta** (núcleo M3/M4 — deriva da contabilidade) |
| **PNCP** (contratos) | Publicidade/eficácia | **Sim** | **Alta** (condição de eficácia; módulo Administracao) |
| **eSocial** | Folha/trabalhista-fiscal | **Sim** | **Alta** (módulo RH, M5) |
| **EFD-Reinf R-4000** | Retenções IRRF/contrib. | **Sim** | **Média-Alta** (RH + despesa, M5) |
| **SADIPEM** | Dívida/crédito | **Sim** (por evento) | **Média** (quando houver operação de crédito) |
| **PIX/BACEN** | Arrecadação (entrada) | Não (arranjo de pagamento) | **Média** (módulo Tributos — recebimento) |
| **Tesouro Gerencial** | Consulta/BI da União | Não (consulta) | **Baixa** (conciliação de transferências) |
| **TSE/SPCE** | Contas eleitorais | **Não** (candidatos/partidos) | **N/A** ao core |

---

## Fontes (oficiais)

- SICONFI — Catálogo de APIs gov.br: https://www.gov.br/conecta/catalogo/apis/siconfi-extratos-das-declaracoes-contabeis
- SICONFI — API de Dados Abertos (Tesouro Transparente): https://www.tesourotransparente.gov.br/consultas/consultas-siconfi/siconfi-api-de-dados-abertos
- Matriz de Saldos Contábeis (Tesouro Transparente): https://www.tesourotransparente.gov.br/consultas/consultas-siconfi/matriz-de-saldos-contabeis-msc
- MSC — Regras Gerais (Anexo I Port. STN 642), exercício 2026: https://siconfi.tesouro.gov.br/siconfi/pages/public/arquivo/conteudo/2026_Anexo_I_Portaria_STN_642_Regras_Gerais_MSC.pdf
- RGF — Regras Gerais e Instruções de Preenchimento (STN): https://siconfi.tesouro.gov.br/siconfi/pages/public/arquivo/conteudo/2021_Regras_Gerais_e_Instrucoes_de_preenchimento_RGF_28_05_2021.pdf
- DCA — prazo 30/abr (Siconfi, Contas Anuais 2024): https://siconfi.tesouro.gov.br/siconfi/pages/public/conteudo/conteudo.jsf?id=48503
- Relatórios Contábeis e Fiscais de Estados, DF e Municípios (Tesouro Transparente): https://www.tesourotransparente.gov.br/temas/contabilidade-e-custos/relatorios-contabeis-e-fiscais-de-estados-df-e-municipios
- Tesouro Gerencial (STN): https://www.gov.br/tesouronacional/pt-br/siafi e http://www.tesouro.fazenda.gov.br/modelo-artigo-siafi/-/asset_publisher/G4pwX6fShrZj/content/tesouro-gerencial
- SADIPEM (STN): https://www.gov.br/tesouronacional/pt-br/estados-e-municipios/operacoes-de-credito/sadipem e portal https://sadipem.tesouro.gov.br/
- SADIPEM — Catálogo de APIs gov.br: https://www.gov.br/conecta/catalogo/apis/sadipem-sistema-de-analise-da-divida-publica-operacoes-de-credito-e-garantias-da-uniao-estados-e-municipios
- CAPAG (Tesouro Transparente): https://www.tesourotransparente.gov.br/temas/estados-e-municipios/capacidade-de-pagamento-capag
- PNCP (gov.br): https://www.gov.br/pncp/pt-br/pncp
- PNCP — Art. 94 / prazos (TCE-SP, legislação comentada Lei 14.133): https://www.tce.sp.gov.br/legislacao-comentada/lei-14133-1o-abril-2021/94
- PNCP — Manual de Integração (APIs de manutenção): https://repositorio.ufsc.br/bitstream/item/51718abf-525e-4867-a7dd-035657231915/Manual%20de%20Integra%C3%A7%C3%A3o%20PNCP%20%E2%80%93%20Vers%C3%A3o%201.0.0.pdf
- EFD-Reinf — Manual (SPED/RFB): http://sped.rfb.gov.br/estatico/28/40FAAC1C636CC110D4C12D2790B43C641C6BCA/Manual%20da%20EFD-Reinf%20vers%C3%A3o%202.1.2.1.pdf
- EFD-Reinf setor público (cartilha CGE-PB): https://paraiba.pb.gov.br/diretas/controladoria-geral-do-estado/arquivos/cartilha-efd-reinf.pdf
- PIX — Manual de Padrões para Iniciação (BACEN): https://www.bcb.gov.br/content/estabilidadefinanceira/pix/Regulamento_Pix/II_ManualdePadroesparaIniciacaodoPix.pdf
- TSE — Contas eleitorais: https://www.tse.jus.br/eleicoes/contas-eleitorais
- TSE — SPCE: https://www.tse.jus.br/eleicoes/eleicoes-2024-content/prestacao-de-contas/prestacao-de-contas-eleicoes-2024

> Itens marcados `[a confirmar]`: versões de leiaute/portaria do exercício corrente (MSC/Port. 642, eSocial S-1.3, EFD-Reinf), canal de transmissão programática do SICONFI, periodicidade exata do CDP no SADIPEM, e perfis de acesso municipal ao Tesouro Gerencial. Validar na fonte oficial antes de implementar (CLAUDE.md §7/§16).
