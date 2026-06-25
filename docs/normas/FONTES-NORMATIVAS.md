# FONTES NORMATIVAS — Tensorroot.Gov

> Catálogo consolidado de fontes autoritativas para conformidade legal/fiscal.
> Jurisdição: **TCE-RS** · Cliente PoC: **Prefeitura de Maximiliano de Almeida/RS** (município pequeno).
> Aquisição ao vivo (jun/2026). Datas/versões vigentes confirmadas via busca web.
> Módulos = `src/Modules/*`. Esta é a **fonte da verdade** para a etapa (a) de auditoria de conformidade — ver `PLANO-CONFORMIDADE.md`.

**Convenções:**
- **Criticidade-PoC**: CRÍTICA (bloqueia o PoC — é saída que o TCE-RS/SICONFI valida ou base contábil/fiscal de cálculo) · ALTA · MÉDIA · BAIXA/INFORMATIVA (substituída/expirada — pode exigir REMOÇÃO de código desatualizado).
- Quando uma fonte estiver "em transição" (leiaute migrando, Reforma Tributária), confirmar a versão corrente no portal **antes** de extrair regras.

Índice de domínios:
- [A. Contábil / Fiscal — STN (fundação)](#a-contábil--fiscal--stn-tesouro-nacional)
- [B. Prestação de contas — TCE-RS / SICONFI](#b-prestação-de-contas--tce-rs--siconfi)
- [C. Base legal federal (lei seca)](#c-base-legal-federal-lei-seca)
- [D. RH / Folha / eSocial](#d-rh--folha--esocial--repasses-previdenciários)
- [E. Tributos](#e-tributos)
- [F. Licitações / Compras](#f-licitações--compras)
- [G. Convênios / Parcerias (MROSC)](#g-convênios--parcerias-mrosc)
- [H. TRs/Editais de ERP municipal (paridade funcional)](#h-trseditais-de-erp-municipal--paridade-funcional)
- [Lacunas a resolver (jurisdição local)](#lacunas-a-resolver-na-etapa-a)

---

## A. Contábil / Fiscal — STN (Tesouro Nacional)

| # | Documento | URL oficial | Versão/data vigente | O que rege | Módulo nosso | Criticidade-PoC |
|---|-----------|-------------|---------------------|------------|--------------|-----------------|
| A1 | **MCASP — Manual de Contabilidade Aplicada ao Setor Público** (inclui PCASP + DCASP) | https://www.gov.br/tesouronacional/pt-br/contabilidade-e-custos/manuais/manual-de-contabilidade-aplicada-ao-setor-publico-mcasp-1 | **11ª ed., vigente desde jan/2025**; consulta pública de mudanças no PCASP p/ vigência 2026 (jul–ago/2025) | Plano de contas (PCASP estendido), reconhecimento/mensuração, estrutura das demonstrações (DCASP) | `Financas` (Contabilidade/PlanoDeContas, /EventosContabeis, /Lancamentos) | **CRÍTICA** |
| A2 | **MSC — Matriz de Saldos Contábeis: Regras Gerais** (Anexo I, Portaria STN 642/2019) | https://siconfi.tesouro.gov.br/siconfi/pages/public/arquivo/conteudo/2026_Anexo_I_Portaria_STN_642_Regras_Gerais_MSC.pdf | **Regras Gerais 2026**; leiaute (Anexo II) atualizado 05/08/2026 | Estrutura/regras da MSC (agregada/encerramento) enviada ao SICONFI; base de RREO/RGF | `Financas` (Contabilidade/Msc) | **CRÍTICA** |
| A3 | **MDF — Manual de Demonstrativos Fiscais** (RREO, RGF, AMF, ARF) | https://www.gov.br/tesouronacional/pt-br/contabilidade-e-custos/manuais/manual-de-demonstrativos-fiscais-mdf | **15ª ed., atualizada 16/09/2025** | Leiaute dos demonstrativos da LRF: RREO (bimestral), RGF (quadrimestral p/ municípios), AMF, ARF | `Financas`, `Transparencia` (DeclaracoesFiscais/Fiscal), `PainelGestor` | **CRÍTICA** |
| A4 | **Decreto 10.540/2020 — SIAFIC** | https://www.planalto.gov.br/ccivil_03/_ato2019-2022/2020/decreto/d10540.htm | Vigente | Requisitos mínimos do sistema único/integrado de execução orçamentária, financeira e contábil. Citado por TODO TR de ERP municipal | `Financas` (núcleo M2/M3), `Transparencia` | **CRÍTICA** |

> Pendência (etapa a): validar se as **alterações do PCASP com vigência 2026** (consulta pública STN jul–ago/2025) já estão na 11ª ed. do MCASP ou em portaria complementar — impacta o plano de contas de `Financas`.

## B. Prestação de contas — TCE-RS / SICONFI

| # | Documento | URL oficial | Versão/data vigente | O que rege | Módulo nosso | Criticidade-PoC |
|---|-----------|-------------|---------------------|------------|--------------|-----------------|
| B1 | **SIAPC/PAD — Leiaute de Dados à Disposição** (Manual Técnico Vol. V) | http://www.tce.rs.gov.br/sistemas_controle/SIAPC/pdf/ResumoLeiauteDadosADisposicao_Siapc_MT_Vol_V_V2.0.pdf · http://www.tce.rs.gov.br/sistemas_controle/SIAPC/pdf/MT_Vol_V_Arq_DispTCE_4320.pdf | MT-ASCE-0105-06, Vol. V (estrutura PAD/MCI) | Leiaute dos arquivos de auditoria/prestação mensal (balancetes, empenhos, liquidações, pagamentos, receita) ao TCE-RS via Processo Eletrônico | `Financas`, `Transparencia` (RemessasTce) | **CRÍTICA** |
| B2 | **TCE-RS — Resolução 1099/2018** (remessa SIAPC/PAD, inclui FOLHA mensal) | https://atosoficiais.com.br/tcers/resolucao-n-1099-2018 | Vigente; folha mensal desde jan/2019 (até 30 dias após competência) | Prazos/documentos/informações ao TCE-RS; arquivos de folha integram o SIAPC/PAD mensal | `Financas`, `RecursosHumanos`, `Transparencia` (RemessasFolha) | **CRÍTICA** |
| B3 | **TCE-RS — Instrução Normativa 06/2019** (leiautes SIAPC/PAD) | https://atosoficiais.com.br/tcers/instrucao-normativa-n-6-2019 | Vigente | Critérios/layouts dos relatórios SIAPC/PAD. Folha: **TCE_4810.TXT** (Folha de Pagamento) e **TCE_4820.TXT** (Cadastro de Servidores) | `RecursosHumanos` (Folha, Servidores), `Financas` | **CRÍTICA** |
| B4 | **SICONFI — RGF Regras de Preenchimento 2026** | https://siconfi.tesouro.gov.br/siconfi/pages/public/conteudo/conteudo.jsf?id=12503 · https://siconfi.tesouro.gov.br/siconfi/pages/public/arquivo/conteudo/2026_Regras_Gerais_e_Instrucoes_de_preenchimento_RGF_04032026.pdf | RGF 2026 (rev. 04/03/2026) | Regras de transmissão/declarações ao SICONFI a partir da MSC | `Financas`, `Transparencia` (DeclaracoesFiscais) | ALTA |
| B5 | **SIAPC — Portal do jurisdicionado** (hub de manuais/versões) | http://portal.tce.rs.gov.br/portal/page/portal/tcers/jurisdicionados/sistemas_controle_externo/siapc · https://tcers.tc.br/sistemas-de-controle-externo/ | Portal vigente (migração p/ tcers.tc.br) | Ponto de entrada p/ versões correntes do leiaute, prazos, integração SISCAD/Processo Eletrônico | `Financas`, `Transparencia` | ALTA |
| B6 | **TCE-RS — SIAPESweb** (auditoria de atos de pessoal: admissões/aposentadorias) | https://tcers.tc.br/sistemas-de-controle-externo/ · leiaute import: https://www.tce.rs.gov.br/sistemas_controle/SIAPES/arquivos/pdf/ImportDadosSIAPESConsist57.pdf | SIAPESweb (substituiu SIAPES Desktop, desativado 20/04/2023); base Resolução 682/2004 | Remessa de atos de admissão (concurso/temporário) p/ apreciação de legalidade. Inativação/pensão: sistema correlato **SAPIEM** | `RecursosHumanos` (Servidores, Portarias) | ALTA |
| B7 | **e-Validador / LicitaCon — Manual e Leiaute** (TCE-RS) | https://tcers.tc.br/sistemas-de-controle-externo/?section=LICITACON · PDF: https://tcers.tc.br/repo/cex/licitacon/eValidador_LicitaCon_Manual_Leiaute_1.4.pdf · dados: https://dados.tce.rs.gov.br/dataset/licitacoes-consolidado-2025 | **1.4.010** (inclui modalidade PDE — Dispensa Eletrônica) | Leiaute de remessa obrigatória das licitações/contratos do município ao TCE-RS (14 arquivos CSV) | `Administracao` (Licitacoes, Contratos), `Convenios` | **CRÍTICA** |
| B8 | **SIAPC — Perguntas Frequentes / regras de entrega** | http://www.tce.rs.gov.br/sistemas_controle/SIAPC/pdf/perguntas_frequentes.pdf | Vigente | Regras operacionais de geração/remessa, integração SISCAD, periodicidade | `Financas`, `Transparencia` | MÉDIA |

> **Nota de nomenclatura:** SICAP/SICOM/SIAPES(Desktop) são de OUTROS tribunais ou versões antigas. No **TCE-RS** o conjunto canônico vigente é **SIAPC/PAD + MCI + LicitaCon + SISCAD + SIAPESweb/SAPIEM**, validados pelo **e-Validador**.

## C. Base legal federal (lei seca)

| # | Documento | URL oficial | Versão/data | O que rege | Módulo nosso | Criticidade-PoC |
|---|-----------|-------------|-------------|------------|--------------|-----------------|
| C1 | **Lei 4.320/1964** | http://www.planalto.gov.br/ccivil_03/leis/l4320.htm | Vigente (consolidada) | Normas gerais de direito financeiro: orçamento, empenho/liquidação/pagamento, balanços | `Financas` (Empenhos, Liquidacoes, Pagamentos, RestosAPagar) | **CRÍTICA** |
| C2 | **LC 101/2000 (LRF)** | http://www.planalto.gov.br/ccivil_03/leis/lcp/lcp101.htm | Vigente | Limites de pessoal/dívida, metas fiscais, RREO/RGF, transparência (arts. 18–23 = limite de pessoal vigente) | `Financas`, `RecursosHumanos`, `Transparencia`, `PainelGestor` | **CRÍTICA** |
| C3 | **Lei 14.133/2021** (NLLC) | http://www.planalto.gov.br/ccivil_03/_ato2019-2022/2021/lei/l14133.htm | Única lei válida desde 01/04/2023 (8.666/93 revogada) | Ciclo completo de licitações/contratos: planejamento, modalidades, contratação direta, sanções | `Administracao` | **CRÍTICA** |
| C4 | **Lei 5.172/1966 — CTN** | https://www.planalto.gov.br/ccivil_03/leis/l5172compilado.htm | Vigente (compilado) | Lançamento (142,147–150), decadência (173), prescrição (174), suspensão (151), extinção (156), arbitramento (148), valor venal (33) | `Tributos` (Lancamentos, Calculo, Dividas, Certidoes) | **CRÍTICA** |
| C5 | **LC 116/2003 — ISS + lista de serviços** | https://www.planalto.gov.br/ccivil_03/leis/lcp/lcp116.htm | Vigente | ISSQN: fato gerador, base, local da prestação, lista anexa de serviços | `Tributos` (Iss, Nfse) | ALTA |
| C6 | **Lei 6.830/1980 — Execução Fiscal** | https://www.planalto.gov.br/ccivil_03/leis/l6830.htm | Vigente | Inscrição em dívida ativa, requisitos da CDA (art. 2º §5º), cobrança judicial | `Tributos` (Dividas) | ALTA |
| C7 | **Lei 9.492/1997 art. 1º §ún.** (Lei 12.767/2012) — protesto de CDA | https://www.planalto.gov.br/ccivil_03/leis/l9492.htm | Vigente | Autoriza protesto extrajudicial de CDA (cobrança alternativa, facultativa) | `Tributos` (Dividas) | MÉDIA |
| C8 | **Lei 13.019/2014 — MROSC** | https://www.planalto.gov.br/ccivil_03/_ato2011-2014/2014/lei/l13019.htm | Vigente | Parcerias com OSCs: termo de colaboração/fomento, acordo de cooperação, chamamento, prestação de contas | `Convenios` (Mrosc) | ALTA |

## D. RH / Folha / eSocial / repasses previdenciários

| # | Documento | URL oficial | Versão/data vigente | O que rege | Módulo nosso | Criticidade-PoC |
|---|-----------|-------------|---------------------|------------|--------------|-----------------|
| D1 | **eSocial — MOS (Manual de Orientação)** — S-1.3 | https://www.gov.br/esocial/pt-br/documentacao-tecnica/manuais/mos-s-1-3-consolidada-ate-a-no-s-1-3-07-2026.pdf | **S-1.3, consolidada até NO 07/2026** (base Portaria Conjunta RFB/MPS/MTE 13 de 25/06/2024) | Regras de preenchimento, semântica e validações de todos os eventos (folha, vínculos, remuneração, tributos) | `RecursosHumanos` (ESocial, Folha, Calculo, Rubricas) | **CRÍTICA** |
| D2 | **eSocial — Leiautes S-1.3 + Esquemas XSD** | https://www.gov.br/esocial/pt-br/documentacao-tecnica/leiautes-esocial-v-1.3/index.html · https://www.gov.br/esocial/pt-br/documentacao-tecnica | **S-1.3, XSD até NT 06/2026** (rev. 09/04/2026); produção 01/07/2026 | Estrutura XML/campos/tabelas: S-1000, S-1010, S-1200/S-1210, S-1207, S-2200/2206/2299/2300, S-3000, S-5001/5002/5003 | `RecursosHumanos` (ESocial) | **CRÍTICA** |
| D3 | **EFD-Reinf — Manual + Leiaute 2.1.2** (NT 01/2026) — série R-4000, substitui DIRF | http://sped.rfb.gov.br/pasta/show/1573 · NT 01/2026: http://sped.rfb.gov.br/arquivo/show/8070 | Leiaute 2.1.2, NT 01/2026 (pub. 09/03/2026) | Retenções na fonte (R-4010/4020/4099) que **substituem a DIRF** (IN RFB 2.181/2024). **DIRF EXTINTA — não entregar em 2026** | `RecursosHumanos` (Calculo IRRF) | **CRÍTICA** |
| D4 | **TCE-RS — IN 06/2019: TCE_4810/4820** | (ver B3) | Vigente | Arquivos TXT de folha de pagamento e cadastro de servidores ao TCE-RS | `RecursosHumanos` (Folha, Servidores) | **CRÍTICA** *(= B3, ótica RH)* |
| D5 | **IPE-Prev (ex-IPERGS) — RPPS/RS** | https://ipeprev.rs.gov.br/rpps-rs-5bd1bb156e223 · LC est. 13.758/2011 (FUNDOPREV): http://www.ipe.rs.gov.br/upload/1497637260_LCP%2013.758.pdf | Vigente | Alíquota (servidor 11% + patronal), retenção/repasse mensal de contribuições ao RPPS; demonstrativo (30 dias após competência) | `RecursosHumanos` (Calculo, Folha) | ALTA *(condicional — ver nota)* |
| D6 | **SIOPE — Remuneração dos Profissionais da Educação (FNDE)** | https://www.fnde.gov.br/siope/legislacao.do · https://www.fnde.gov.br/siope/consultarRemuneracaoMunicipal.do | Vigente | Coleta da remuneração dos profissionais da educação (Fundeb / mínimo constitucional de pessoal da educação) | `RecursosHumanos` (Servidores, Cargos), `Educacao` | MÉDIA |
| D7 | **RAIS / CAGED** — substituídos pelo eSocial | — | Substituídos | Para ente público no eSocial **não há entrega autônoma** — dado deriva do eSocial | `RecursosHumanos` | BAIXA/INFORMATIVA |
| D8 | **LC 173/2020** — congelamento de pessoal | — | **Vedações vigoraram até 31/12/2021 — NÃO aplicável a 2026** | Limite de pessoal vigente é a **LRF arts. 18–23** (= C2) | `RecursosHumanos`, `PainelGestor` | INFORMATIVA *(remover se referenciado como ativo)* |

> **Pré-condição da etapa (a):** confirmar se os servidores de Maximiliano de Almeida são vinculados a **RPPS** (IPE-Prev / RPPS próprio) ou ao **RGPS/INSS** (municípios sem RPPS recolhem ao INSS via GPS/eSocial). Isso determina o motor de contribuição (D5 vs. INSS).

## E. Tributos

| # | Documento | URL oficial | Versão/data | O que rege | Módulo nosso | Criticidade-PoC |
|---|-----------|-------------|-------------|------------|--------------|-----------------|
| E1 | **CTN** (= C4) | https://www.planalto.gov.br/ccivil_03/leis/l5172compilado.htm | Vigente | Decadência/prescrição/lançamento/CDA — transversal a todo o módulo | `Tributos` (Lancamentos, Calculo, Dividas, Certidoes) | **CRÍTICA** |
| E2 | **Tema 1.113/STJ — ITBI** (REsp 1.937.821, repetitivo, mar/2022) | https://www.stj.jus.br/sites/portalp/Paginas/Comunicacao/Noticias/09032022-Base-de-calculo-do-ITBI-e-o-valor-do-imovel-transmitido-em-condicoes-normais-de-mercado--define-Primeira-Secao.aspx | Vigente | Base do ITBI = valor de mercado, NÃO vinculada ao IPTU nem a piso; valor declarado tem presunção de veracidade (só afastável via art. 148); **Município NÃO pode arbitrar "valor de referência" unilateral prévio** | `Tributos` (Itbi) | ALTA |
| E3 | **NFS-e Nacional / ADN** — manuais APIs + leiautes DPS/NFS-e/Eventos | https://www.gov.br/nfse/pt-br/biblioteca/documentacao-tecnica · manual APIs ADN: https://www.gov.br/nfse/pt-br/biblioteca/documentacao-tecnica/documentacao-atual/manual-contribuintes-apis-adn-sistema-nacional-nfse.pdf | NT 004 (jun/2025) atualiza leiaute p/ Reforma Tributária (CBS/IBS) — **em transição** | Emissão/transmissão de NFS-e ao ADN, DPS, leiaute XML, eventos | `Tributos` (Nfse, Iss) | ALTA |
| E4 | **LC 116/2003** (= C5) | https://www.planalto.gov.br/ccivil_03/leis/lcp/lcp116.htm | Vigente | Lista anexa de serviços; cadastro municipal deve espelhá-la | `Tributos` (Iss, Nfse) | ALTA |
| E5 | **IPTU/PGV — CTN art. 33 + Tema 1084/STF (ARE 1245097) + Súmula 160/STJ** | STF Tema 1084: https://portal.stf.jus.br/noticias/verNoticiaDetalhe.asp?idConteudo=508528 · Súmula 160: https://www.stj.jus.br/docs_internet/revista/eletronica/stj-revista-sumulas-2010_11_capSumula160.pdf | Vigente | Atualização de valor venal exige **lei** (não decreto, salvo correção monetária); avaliação individual de imóvel novo precisa de base legal + contraditório. Observar EC 132/2023 | `Tributos` (Pgv, Imoveis, Calculo) | ALTA |
| E6 | **Lei 6.830/1980 — Execução Fiscal** (= C6) | https://www.planalto.gov.br/ccivil_03/leis/l6830.htm | Vigente | CDA como título executivo (art. 2º §5º), ciclo de inscrição em dívida ativa | `Tributos` (Dividas) | ALTA |
| E7 | **DES-IF (ABRASF)** — ISS de instituições financeiras | https://notacarioca.rio.gov.br/files/manuais/desif_abrasf_modelo_conceitual.pdf *(espelho; confirmar versão em abrasf.org.br)* | Modelo conceitual + PGCC | Recepção/validação de declaração de bancos p/ ISS (padrão nacional) | `Tributos` (Iss) | MÉDIA |
| E8 | **Lei 9.492/1997** — protesto de CDA (= C7) | https://www.planalto.gov.br/ccivil_03/leis/l9492.htm | Vigente | Protesto extrajudicial de CDA (facultativo) | `Tributos` (Dividas) | MÉDIA |

> **Correção:** o tema STF de PGV/IPTU é **Tema 1084** (não "1124"). · **NFS-e/DES-IF**: leiautes em evolução (Reforma Tributária / versão ABRASF) — confirmar a versão vigente na etapa (a).

## F. Licitações / Compras

| # | Documento | URL oficial | Versão/data vigente | O que rege | Módulo nosso | Criticidade-PoC |
|---|-----------|-------------|---------------------|------------|--------------|-----------------|
| F1 | **Lei 14.133/2021 (NLLC)** (= C3) | http://www.planalto.gov.br/ccivil_03/_ato2019-2022/2021/lei/l14133.htm | Vigente desde 01/04/2023 | Base legal de todo o ciclo de licitação/contratação | `Administracao` (Licitacoes, Contratos, Fornecedores) | **CRÍTICA** |
| F2 | **Manual de Integração PNCP** (API de envio/manutenção) | https://www.gov.br/pncp/pt-br/pncp/integre-se-ao-pncp/manual-de-integracao · Swagger: https://pncp.gov.br/api/pncp/swagger-ui/index.html · homolog.: https://treina.pncp.gov.br | **2.3.5 (12/02/2025)** | Divulgação obrigatória de editais/atas/contratos no PNCP (condição de eficácia, art. 94 NLLC) | `Administracao`, `Transparencia` | **CRÍTICA** |
| F3 | **Decreto 12.343/2024** — atualização dos valores da Lei 14.133 | https://www.planalto.gov.br/ccivil_03/_ato2023-2026/2024/decreto/d12343.htm | **Vigente desde 01/01/2025** | Limites/faixas de valor (dispensa, modalidades) — tabela muda anualmente | `Administracao` (Licitacoes — limites de dispensa/modalidade) | ALTA |
| F4 | **Decreto 11.462/2023 — SRP** (Sistema de Registro de Preços) | https://www.planalto.gov.br/ccivil_03/_ato2023-2026/2023/decreto/d11462.htm | Vigente | Regulamenta arts. 82–86 NLLC: atas, carona | `Administracao` (RegistroPrecos) | ALTA |
| F5 | **Decreto 11.246/2022** — agente de contratação / fiscalização | https://www.planalto.gov.br/ccivil_03/_ato2019-2022/2022/decreto/d11246.htm | Vigente desde 01/11/2022 | Papéis/fluxo do processo (agente, equipe de apoio, comissão, gestão/fiscalização) | `Administracao` (Licitacoes, Contratos) | ALTA |
| F6 | **Decreto 10.947/2022 — PCA / PGC** | (referência federal do PCA — art. 12,VII NLLC) | Vigente *(confirmar atualização 2024/2025)* | Plano de Contratações Anual | `Administracao` (Pca) | MÉDIA-ALTA |
| F7 | **Manual das APIs de Consulta PNCP** (público) | https://pncp.gov.br/api/consulta/swagger-ui/index.html | Vigente | Consulta pública de editais/contratos | `Administracao`, `Transparencia` | MÉDIA |

> Federalmente o município **regulamenta por decreto municipal próprio** (a NLLC delega regulamentação aos entes). Os decretos federais acima são referência subsidiária/modelo — verificar decreto municipal de Maximiliano de Almeida na etapa (a). · **LicitaCon (remessa ao TCE-RS) = B7.**

## G. Convênios / Parcerias (MROSC)

| # | Documento | URL oficial | Versão/data vigente | O que rege | Módulo nosso | Criticidade-PoC |
|---|-----------|-------------|---------------------|------------|--------------|-----------------|
| G1 | **Lei 13.019/2014 — MROSC** (= C8) | https://www.planalto.gov.br/ccivil_03/_ato2011-2014/2014/lei/l13019.htm | Vigente | Parcerias com OSCs: termos, chamamento público, prestação de contas | `Convenios` (Mrosc) | ALTA |
| G2 | **Manual MROSC (Transferegov) — ed. 2025** | https://www.gov.br/transferegov/pt-br/legislacao/portarias/MANUALMROSCDoPlanejamentoPrestaodeContasreduzido13082025.pdf | Entregue ago/2025 (Confoco) | Procedimentos operacionais de todas as fases das parcerias na plataforma Transferegov.br | `Convenios` (Mrosc) | ALTA |
| G3 | **Plataforma Transferegov.br** (sucessor do SICONV) | https://www.gov.br/transferegov/pt-br | Vigente | Processamento de convênios/parcerias com transferência de recursos federais | `Convenios` (Recebidos) | MÉDIA-ALTA |
| G4 | **Decreto 8.726/2016** — regulamento federal da Lei 13.019 | *(a confirmar na etapa a)* | — | Regulamenta a Lei 13.019 no âmbito federal | `Convenios` (Mrosc) | MÉDIA *(a confirmar)* |

## H. TRs/Editais de ERP municipal — paridade funcional

> Não são fonte de conformidade legal; são o **checklist de requisitos funcionais** (paridade de mercado). Critério de aceite recorrente nos editais: **≥90% de atendimento por módulo, 100% nos itens essenciais** — usar como métrica de paridade na etapa (a).

| # | TR/Edital | URL | Tier / por quê | Cobertura → módulos nossos |
|---|-----------|-----|----------------|-----------------------------|
| H1 | **Porto Alegre/RS — TR Sistema Integrado de Gestão Pública** (PMAV/SMF) | https://app.comprasbr.com.br/licitacao/hal/public/arquivos?uri=repo1%3Alicitacao%2FTERMODEREFERENCIASISTEMAPMAVistotributosassinado.pdf · https://transparencia.portoalegre.rs.gov.br/licitacoes-contratos/licitacoes | **TIER 1 gold-standard** — capital, jurisdição TCE-RS, cita SIAFIC + LRF art. 48. Mapa 1:1 com `src/Modules/*` | Planejamento/Orçamento, Contabilidade, Tesouraria, Folha, Ponto, Compras/Licitações, Patrimônio, Almoxarifado, Frota, Transparência, Portal Serviços, Tributos (IPTU/ITBI/ISS/taxas), Dívida Ativa, NFS-e → **todos os 5 núcleos** |
| H2 | **Natal/RN — PE 24.015/2026 ERP (SRP)** | http://siaianalise.tce.rn.gov.br/downloadanexoportalgestor/Edital/PMNATAL/472128/180192/EDITAL%20-%20PE%2024.015-2026%20-%20Servi%C3%A7o%20de%20informatiza%C3%A7%C3%A3o%20da%20administra%C3%A7%C3%A3o%20p%C3%BAblica%20(ERP)%20-%20SRP.pdf | **TIER 1** — capital, edital recente (2026, Lei 14.133) | ERP integrado: financeiro/contábil, tributos, RH, compras, patrimônio |
| H3 | **General Câmara/RS — Edital ERP** (PNCP 2025/40, 2026/13) | https://pncp.gov.br/pncp-api/v1/orgaos/88117726000150/compras/2025/40/arquivos/1 · https://pncp.gov.br/pncp-api/v1/orgaos/88117726000150/compras/2026/13/arquivos/2 | **TIER 2** — mesma jurisdição TCE-RS, porte pequeno/médio = **paridade direta** com o nosso caso | Define ≥90%/módulo, 100% essenciais |
| H4 | **Canoas/RS — Edital 266/2021 ERP** | https://www.canoas.rs.gov.br/wp-content/uploads/2021/10/EDITAL_266_2021_com_alteracoes_APROVADO.pdf | TIER 2 — RM Porto Alegre, TCE-RS | Licenciamento + implantação + treinamento |
| H5 | **Sede Nova/RS** (PNCP 2024) · **Paraíso do Sul/RS** | https://pncp.gov.br/pncp-api/v1/orgaos/91997056000118/compras/2024/9/arquivos/1 · https://www.paraisodosul.rs.gov.br/public/admin/globalarq/contratacao/arquivo/d7ded6168aff0b8083b825ad5a7bee91.pdf | TIER 2 — municípios pequenos RS, paridade de porte | Calibram profundidade de município pequeno |
| H6 | **Timbó/SC — ETP SIGP** · **CIM-AMFRI/SC — PE 05/2025** | https://www.timbo.sc.gov.br/wp-content/uploads/2024/07/ETP-Software-Integrado-de-Gestao-Publica.pdf · https://cim-amfri.sc.gov.br/uploads/sites/584/2025/07/4-Edital-e-anexos-Sistema-de-Gestao-licenca-de-uso-de-software-CIMAMFRI.pdf | TIER 3 — racional de requisitos/justificativa reutilizável | Multi-módulo |
| H7 | **PNCP — busca de editais** (fonte viva) | https://pncp.gov.br/app/editais | Para achar mais TRs por filtro | — |

> **Âncoras recomendadas:** paridade primária = **Porto Alegre (H1)**; calibragem de porte/jurisdição = **General Câmara + Sede Nova (H3, H5)** — evita over/under-scoping vs. capital.
> **Nota técnica:** PDFs/DOCs de H2–H5 não renderizam por WebFetch (binário comprimido); confirmados por metadados de busca. Para extração regra-a-regra na etapa (a), baixar localmente e abrir como texto.

---

## Lacunas a resolver na etapa (a)

1. **PCASP 2026** — confirmar se as alterações da consulta pública STN (jul–ago/2025) estão na 11ª ed. do MCASP ou em portaria complementar (A1). Impacta `Financas/PlanoDeContas`.
2. **Versão corrente do leiaute SIAPC/PAD Vol. V** (B1) e **LicitaCon** (B7, visto 1.4.010) — portal TCE-RS migrando p/ tcers.tc.br e bloqueando fetch automático; confirmar no histórico do e-Validador.
3. **Vínculo previdenciário de Maximiliano de Almeida**: RPPS (IPE-Prev/próprio) vs. RGPS/INSS (D5) — **pré-condição** do motor de contribuição do RH.
4. **NFS-e ADN** (E3) e **DES-IF** (E7) — leiautes em transição (Reforma Tributária / versão ABRASF); confirmar versão vigente.
5. **Código Tributário Municipal de Maximiliano de Almeida** e atos do TCE-RS sobre arrecadação/renúncia/dívida ativa — não localizados; busca dedicada no portal da prefeitura + tce.rs.gov.br.
6. **Decreto municipal** regulamentando a NLLC (limites, agente de contratação local) — checar portal da prefeitura.
7. **Decreto 8.726/2016** (regulamento federal MROSC, G4) e **Decreto 10.947/2022** (PCA, F6) — confirmar nº e atualização.

## Desatualizações a corrigir no código (etapa a)

- **DIRF** — extinta; migrar para eSocial S-1210 + EFD-Reinf R-4000 (D3). Se o RH gera DIRF, está desatualizado.
- **RAIS/CAGED** — substituídos pelo eSocial (D7); garantir que o RH não duplica e deriva do eSocial.
- **LC 173/2020** — vedações expiradas em 31/12/2021 (D8); se referenciado como regra ativa, REMOVER e ancorar limite de pessoal na LRF arts. 18–23 (C2).
- **ITBI** — não usar "valor de referência" fixo/PGV como piso automático (E2); permitir valor declarado + fluxo de arbitramento administrativo (art. 148 CTN).
- **IPTU/PGV** — atualização de valor venal só por lei, salvo correção monetária (E5).
