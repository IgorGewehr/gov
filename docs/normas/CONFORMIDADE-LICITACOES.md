# CONFORMIDADE — LICITAÇÕES (módulo Administracao)

> Auditoria de conformidade normativa READ-ONLY · TCE-RS · Maximiliano de Almeida/RS · jun/2026
> Cruzamento código ↔ fonte oficial lida ao vivo. Cobre `src/Modules/Administracao/.../{Licitacoes,Contratos,Pncp}` contra: **Lei 14.133/2021 (art. 28/33/74/75/94/124-136/174/176)**, **Manual de Integração PNCP 2.3.5 (12/02/2025)**, **leiaute LicitaCon TCE-RS 1.4 (IN 13/2017)** e a **tabela de limites — Dec. 12.807/2025 (vigente 2026; revogou o Dec. 12.343/2024)**.
> Complementa `CONFORMIDADE-ACHADOS.md §3.3` (que ficou em branco — "não auditado nesta rodada").

---

## 1. VEREDITO (honesto)

O **miolo de domínio das Licitações/Contratos é sólido e bem ancorado na NLLC**: ciclo do certame (abrir→julgar→habilitar→homologar / fracassada/deserta/revogada/anulada) com invariantes corretas; melhor-proposta-pelo-critério (art. 33); fail-closed de fornecedor sancionado na habilitação/homologação/celebração (art. 14/156); tetos de aditivo separados acréscimo/supressão e reforma exclusiva de acréscimos (art. 125 §1º); garantia 5%/10% (art. 96/98); **invariante de bloqueio do empenho sem nº de controle PNCP (art. 94)** e alerta de prazo a vencer/vencido. Isso passaria numa avaliação de *engenharia de domínio*.

Mas a **casca de transmissão oficial — o que efetivamente vai ao PNCP e ao TCE-RS — está incompleta/placeholder**, exatamente como no resto do M10:
- **PNCP**: gateway é `PncpGatewaySimulado` (nº de controle SHA-256 determinístico, sem rede/credenciais — honestamente marcado `// TODO(M10)`). O **DTO de publicação (`PublicacaoContratoPncpRequest`) é um esqueleto** que omite quase todos os campos obrigatórios do Manual 2.3.5 (`tipoContrato`, `categoriaProcesso`, `codigoUnidadeCompradora`, `numeroControlePNCPCompra`, `tipoPessoaFornecedor`, `niFornecedor`, `modalidadeId`, `amparoLegal`, etc.). **Editais não são publicados** (o handler `PublicarEditalNoPncp` só grava um `NumeroEditalPncp` informado de fora — não transmite). **Aditivos** são marcados publicados mas **não têm transmissão nem prazo PNCP próprio** (art. 94 cobre os aditivos).
- **Prazo do art. 94 modelado pela metade**: o sistema usa **sempre 20 d.u.** para divulgação, ignorando que o art. 94 fixa **20 d.u. para licitação (inc. I)** e **10 d.u. para contratação direta (inc. II)** — contrato de dispensa/inexigibilidade fica com prazo de tempestividade errado (folga indevida de 10 d.u.).
- **LicitaCon (remessa ao TCE-RS) NÃO EXISTE**: zero código de exportação dos 14 CSV do leiaute 1.4. É a saída que o e-Validador do TCE-RS efetivamente valida — hoje inexistente.
- **Tabela de limites de dispensa/modalidade (Dec. 12.807/2025) AUSENTE**: não há nenhum enquadramento valor→modalidade nem teto de dispensa em lugar nenhum do módulo. Um contrato de dispensa "por valor" pode ser celebrado acima do limite legal sem nenhuma trava. Além disso, a `FONTES-NORMATIVAS.md` (F3) ainda aponta o **Dec. 12.343/2024, que foi REVOGADO** pelo Dec. 12.807/2025 (vigente desde 01/01/2026).
- **Contexto art. 176 (relevante p/ o PoC)**: Maximiliano de Almeida (~5 mil hab., ≤20 mil) está no **prazo de transição de 6 anos** (até **01/04/2027**) em que pode publicar em **diário oficial** em vez do PNCP. O modelo trata PNCP como invariante dura única, sem caminho de diário oficial — aceitável como estado-alvo, mas o bloqueio do empenho por "sem PNCP" pode travar a operação real do município antes da adesão.

**Diagnóstico**: domínio nota ~9; transmissão oficial (PNCP real + LicitaCon + limites) é o trabalho bloqueante de M10. Contagem: **2 ✅ · 6 🟡 · 5 🔴** (13 regras finas auditadas). **P0 = 4** (LicitaCon ausente, limites Dec. 12.807 ausentes, prazo art. 94 inc. II, campos obrigatórios PNCP 2.3.5).

---

## 2. PRÉ-CONDIÇÕES / NOTAS DE FONTE (lidas ao vivo jun/2026)

- **PC-LIC1 — Dec. de limites vigente**: o **Dec. 12.343/2024 foi REVOGADO pelo Dec. 12.807/2025** (vigência 01/01/2026, IPCA). Valores **2026**: dispensa **art. 75, I (obras/eng.) = R$ 130.984,20**; **art. 75, II (demais bens/serviços) = R$ 65.492,11**; grande vulto = R$ 261.968.421,04. **Corrigir a F3 da `FONTES-NORMATIVAS.md`** (aponta o decreto revogado). Como o módulo é parametrizável por tenant (CLAUDE.md §16), a tabela deve ser **seed parametrizável**, não literal.
- **PC-LIC2 — art. 94 prazos**: divulgação no PNCP é condição de eficácia, contada da assinatura — **20 d.u. licitação (I)** / **10 d.u. contratação direta (II)**; obras: 25 d.u. (preços) / 45 d.u. (executados) — art. 94 §3. Aditivos seguem o mesmo regime.
- **PC-LIC3 — LicitaCon 1.4**: remessa de **14 arquivos CSV** (PESSOAS, MEMBRO_CONSORCIO, COMISSAO, MEMBRO_COMISSAO, LICITACAO, LICITANTE, DOTACAO_LICITACAO, EVENTO_LICITACAO, LOTE, ITEM, PROPOSTA, LOTE_PROPOSTA, ITEM_PROPOSTA, DOCUMENTO_LICITACAO) + contratos/empenhos correlatos; inclui modalidade **PDE (Dispensa Eletrônica)**; prazos pela **IN 13/2017 do TCE-RS**. Manual/leiaute exato a confirmar no PDF oficial (403 via WebFetch — extrair localmente).
- **PC-LIC4 — art. 176**: município ≤20 mil hab. tem 6 anos (→ 01/04/2027) p/ PNCP obrigatório; antes disso publica em diário oficial (extrato admitido).

---

## 3. TABELA DE DIVERGÊNCIAS

Legenda: ✅ CONFORME · 🟡 DIVERGENTE/PARCIAL · 🔴 AUSENTE

| # | Regra fina da norma | Status | Norma (fonte + versão) | Nosso arquivo:linha | Sev | Fix (1 linha) |
|---|---|---|---|---|---|---|
| L1 | **Prazo divulgação PNCP por origem**: 20 d.u. licitação (art. 94, I) **vs** 10 d.u. contratação direta (art. 94, II), contados da assinatura | 🟡 | Lei 14.133/2021 art. 94, I e II | `PncpOptions.cs:17` (`DivulgacaoQuantidade=20` único); `CelebrarContrato.cs:106` (usa `Divulgacao()` p/ toda origem); `Contrato.cs:231-237` | **P0** | Adicionar `DivulgacaoDiretaQuantidade=10`; `CelebrarContrato` escolhe o prazo pela `Origem` (Licitacao→20 / Dispensa-Inexig.→10) |
| L2 | **Campos obrigatórios da publicação de contrato** no PNCP (Manual 2.3.5): `tipoContrato`, `categoriaProcesso`, `codigoUnidadeCompradora`, `numeroControlePNCPCompra`, `tipoPessoaFornecedor`, `niFornecedor`, `modalidadeId`, `amparoLegal`, `valorInicial/Global`, datas vigência | 🔴 | Manual de Integração PNCP **2.3.5 (12/02/2025)**, serviço de contratos | `IPncpGateway.cs:50-61` (`PublicacaoContratoPncpRequest` só tem 11 campos genéricos) | **P0** | Modelar o DTO com os campos do schema de contrato 2.3.5 // TODO(M10-validate) contra `treina.pncp.gov.br` |
| L3 | **Transmissão real ao PNCP** (login JWT ~1h → órgão→unidade→compra→contrato→arquivos), idempotente, Polly | 🟡 | Manual 2.3.5; Lei 14.133 art. 174; Dec. 10.764/2021 | `PncpGatewaySimulado.cs:22-66` (nº SHA-256 determinístico, sem rede) | P1 (M10) | `// TODO(M10)` já honesto; substituir por `PncpGatewayHttp` + credenciais Key Vault |
| L4 | **Publicação de EDITAL no PNCP** (divulgação obrigatória do edital — art. 54/174) | 🔴 | Lei 14.133 art. 54/174; Manual 2.3.5 (serviço compra/edital) | `PublicarEditalNoPncp.cs:38-47` (só grava `NumeroEditalPncp` recebido de fora; não transmite) | **P0** | Rotear edital pela ACL `IPncpGateway` (precadastro compra/edital), como já se faz no contrato |
| L5 | **Divulgação de ADITIVO no PNCP** é condição de eficácia do aditivo (art. 94 cobre aditivos) com prazo próprio | 🟡 | Lei 14.133 art. 94 (caput: "contratos e seus aditamentos") | `Contrato.Aditivos.cs:151-156` (`MarcarAditivoPublicado` sem transmissão nem `PrazoPncp`); `CelebrarAditivo.cs` (sem prazo PNCP) | P1 | Dar ao aditivo `PrazoPncp` próprio + transmissão pela ACL; corrigir docstring "art. 174"→"art. 94" |
| L6 | **Remessa LicitaCon (14 CSV)** ao TCE-RS — saída validada pelo e-Validador | 🔴 | LicitaCon **Leiaute 1.4** + **IN 13/2017** TCE-RS | (nenhum arquivo — inexistente no módulo) | **P0** | Implementar gerador dos 14 CSV (LICITACAO/LOTE/ITEM/PROPOSTA/CONTRATO/…) + prazo IN 13/2017, em `Transparencia/RemessasTce` ou `Administracao` |
| L7 | **Tabela de limites dispensa/modalidade** (enquadramento valor→modalidade; teto art. 75 I/II) | 🔴 | **Dec. 12.807/2025** (art. 75 I=R$130.984,20; II=R$65.492,11), vigente 2026 | (nenhum arquivo — sem enquadramento/teto em lugar nenhum) | **P0** | Seed parametrizável por tenant + guarda em `AbrirLicitacao`/`CelebrarContrato` (dispensa-valor não excede o limite vigente) |
| L8 | **`FONTES-NORMATIVAS.md` aponta decreto de limites REVOGADO** | 🟡 | Dec. 12.807/2025 revogou o 12.343/2024 | `docs/normas/FONTES-NORMATIVAS.md` (F3 cita 12.343/2024) | P2 | Atualizar F3 p/ Dec. 12.807/2025 (manter 12.343 como histórico) |
| L9 | **Regime de transição art. 176** (município ≤20 mil hab. → diário oficial até 01/04/2027) | 🟡 | Lei 14.133 art. 176, I e §§ | `Contrato.Pncp.cs:55-58` (`PodeEmpenhar` exige PNCP duro, sem caminho diário oficial) | P2 | Parametrizar "ente aderiu ao PNCP?" por tenant; antes da adesão, eficácia/empenho via publicação em D.O. |
| L10 | **Modalidades NLLC** (Pregão/Concorrência/Diálogo + Dispensa/Inexig.) e **critérios art. 33** | ✅ | Lei 14.133 art. 28/74/75 e 33 | `Licitacoes/Enums.cs:4-42`; `Licitacao.cs:142-146` (Pregão só menor preço/maior desconto) | — | — |
| L11 | **Melhor proposta pelo critério + fail-closed sanção** (art. 14/33/156) na habilitação/homologação/celebração | ✅ | Lei 14.133 art. 14/33/156 | `Licitacao.cs:238-243,299-307,357-361`; `Contrato.cs:192-196` | — | — |
| L12 | **Concorrência Eletrônica / leilão eletrônico / SRP carona** — modalidades/atributos exigidos pelo LicitaCon (PDE etc.) | 🟡 | Lei 14.133 art. 28/82-86; LicitaCon 1.4 (PDE) | `Licitacoes/Enums.cs:4-20` (sem "forma eletrônica" como atributo; RegistroPrecos existe em `/RegistroPrecos`) | P1 | Adicionar atributo "forma" (eletrônica/presencial) à licitação — exigido na remessa LicitaCon |
| L13 | **Invariante de bloqueio do empenho sem nº de controle PNCP** (eficácia art. 94) — cross-module via Contracts | ✅ | Lei 14.133 art. 94 | `Contrato.Pncp.cs:55-58` (`PodeEmpenhar`); consumido por Financas via `IConsultaContratoParaEmpenho` | — | — |

---

## 4. FILA DE FIX PRIORIZADA

### P0 — BLOQUEANTES (validador oficial / legalidade da contratação)
1. **L7 — Tabela de limites Dec. 12.807/2025** (dispensa I=R$130.984,20 / II=R$65.492,11), seed parametrizável + guarda de teto na dispensa-por-valor. *Sem isso, dispensa acima do limite passa sem trava.*
2. **L6 — Remessa LicitaCon (14 CSV, leiaute 1.4 + IN 13/2017)**. *Saída que o e-Validador TCE-RS valida — hoje inexistente.*
3. **L1 — Prazo art. 94 por origem (20 d.u. licitação / 10 d.u. direta)**. *Construível já; o número vem dos parâmetros do tenant.*
4. **L2 + L4 — Campos obrigatórios da publicação PNCP 2.3.5 (contrato + edital)**. *Modelar correto agora + `// TODO(M10-validate)` contra `treina.pncp.gov.br`.*

### P1 — CONFORMIDADE FINA (não bloqueia 1ª remessa, mas exigido)
- **L5** — aditivo com `PrazoPncp` próprio + transmissão (art. 94 cobre aditivos); corrigir docstring art. 174→art. 94.
- **L3** — `PncpGatewayHttp` real (já honestamente `// TODO(M10)`).
- **L12** — atributo "forma eletrônica/presencial" na licitação (exigido no LicitaCon).

### P2 — APOIO / LIMPEZA
- **L8** — atualizar `FONTES-NORMATIVAS.md` F3 (Dec. 12.343→12.807/2025).
- **L9** — caminho diário oficial (art. 176) parametrizável por adesão do tenant ao PNCP.

---

## 5. NOTA DE MÉTODO E FONTES (lidas ao vivo, jun/2026)

WebFetch/WebSearch sobre as fontes oficiais; o PDF do leiaute LicitaCon 1.4 retorna **HTTP 403** por WebFetch (extrair localmente para fechar os campos exatos dos 14 CSV — `// TODO(M10-validate)`). O PNCP 2.3.5 e os limites Dec. 12.807/2025 foram confirmados em fontes oficiais/especializadas; o nº/ordem exatos de campos do schema de contrato 2.3.5 fecham só contra `treina.pncp.gov.br` (credenciamento M10).

- Lei 14.133/2021 (art. 94 prazos / art. 176 transição): https://www.planalto.gov.br/ccivil_03/_ato2019-2022/2021/lei/l14133.htm · TCU 5.11.7 Divulgação; TCE-SP art. 94/176 comentados
- **Dec. 12.807/2025** (revoga o 12.343/2024; valores 2026): https://www.planalto.gov.br/ccivil_03/_ato2023-2026/2025/decreto/d12807.htm
- Dec. 12.343/2024 (REVOGADO — histórico): http://www.planalto.gov.br/ccivil_03/_ato2023-2026/2024/decreto/d12343.htm
- Manual de Integração **PNCP 2.3.5 (12/02/2025)**: https://www.gov.br/pncp/pt-br/pncp/integre-se-ao-pncp/manual-de-integracao · Swagger: https://pncp.gov.br/api/pncp/swagger-ui/index.html
- **LicitaCon 1.4** (leiaute/manual + 14 CSV + PDE): https://tcers.tc.br/repo/cex/licitacon/eValidador_LicitaCon_Manual_Leiaute_1.4.pdf (403) · dataset: https://dados.tce.rs.gov.br/dataset/licitacoes-consolidado-2025 · IN 13/2017: https://atosoficiais.com.br/tcers/instrucao-normativa-n-13-2017
