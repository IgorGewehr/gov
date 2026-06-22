# Pesquisa eSocial — eventos relevantes para ente público / RPPS (M5 RH)

> Piloto: Maximiliano de Almeida/RS — administração pública direta municipal (Grupo 4 do cronograma eSocial).
> Regra de ouro (CLAUDE.md §16): toda afirmação factual abaixo tem FONTE (URL oficial) OU está marcada `[a confirmar]` com o documento a obter.
> Data da pesquisa: 2026-06. Priorizadas fontes gov.br (eSocial / MTP / Receita).

---

## 1. Versão do leiaute vigente (MOS)

- **Leiaute vigente: eSocial versão S-1.3.** A versão S-1.3 foi aprovada pela **Portaria Conjunta RFB/MPS/MTE nº 13, de 25/06/2024**.
  - Fonte: https://www.gov.br/esocial/pt-br/documentacao-tecnica/leiautes-esocial-v-1.3/index.html
- **Consolidação técnica mais recente identificada: leiautes S-1.3 consolidados até a NT 06/2026 (rev. 09/04/2026).** As revisões evoluem por Notas Técnicas dentro da mesma versão S-1.3 (NT 01/2024 → NT 03/2025 → NT 04/2025 → NT 06/2026).
  - Fonte: https://www.gov.br/esocial/pt-br/documentacao-tecnica/leiautes-esocial-versao-s-1-3-nt-06-2026-rev-09-04-2026/index.html
- **MOS (manual textual) mais recente identificado: MOS S-1.3 consolidado até a NS S-1.3 07/2026.**
  - Fonte: https://www.gov.br/esocial/pt-br/documentacao-tecnica/manuais/mos-s-1-3-consolidada-ate-a-no-s-1-3-07-2026.pdf
  - Versões anteriores do MOS S-1.3 (03/2025, 06/2025): mesma pasta `/documentacao-tecnica/manuais/`.

> `[a confirmar — obter doc oficial]` Travar para o M5 a **versão exata do MOS + NT** a implementar (ex.: "S-1.3 NT 06/2026 rev. 09/04/2026"), baixando o PDF do MOS e o Anexo I (Leiautes) e o pacote de XSD/schemas correspondente. NTs futuras podem alterar regras antes do go-live.

---

## 2. Eventos relevantes para ente público / RPPS

Nomes/escopos confirmados na página de leiautes S-1.3 (NT 06/2026):
- Fonte (lista de eventos): https://www.gov.br/esocial/pt-br/documentacao-tecnica/leiautes-esocial-versao-s-1-3-nt-06-2026-rev-09-04-2026/index.html

### 2.1 Eventos de tabela (base — enviar PRIMEIRO)
| Evento | Título |
|--------|--------|
| **S-1000** | Informações do Empregador/Contribuinte/Órgão Público |
| **S-1005** | Tabela de Estabelecimentos, Obras ou Unidades de Órgãos Públicos |
| **S-1010** | Tabela de Rubricas |
| **S-1020** | Tabela de Lotações Tributárias |
| **S-1070** | Tabela de Processos Administrativos/Judiciais |

- Regra relevante S-1010: rubrica com `{codIncFGTS}` = [21, 93] só pode ser usada em desligamento (S-2299) / término TSVE (S-2399) ou no grupo `{remunPerAnt}` do S-1200. Fonte: MOS/leiautes S-1.3.

### 2.2 Eventos não-periódicos
| Evento | Título |
|--------|--------|
| **S-2200** | Cadastramento Inicial do Vínculo e Admissão/Ingresso de Trabalhador |
| **S-2300** | Trabalhador Sem Vínculo de Emprego/Estatutário (TSVE) — Início |
| **S-2299** | Desligamento |
| **S-2399** | Trabalhador Sem Vínculo de Emprego/Estatutário (TSVE) — Término |

- **S-2200**: uso permitido p/ categorias [101,102,103,104,105,106,107,108,111, 301,302,303,306,307,309,310,312,314] — inclui categorias de servidor público (3XX). Deve ser enviado ANTES dos eventos de remuneração (S-1200/S-1202/S-1207). Fonte: MOS/leiautes S-1.3.
- **S-2299/S-2399**: exclusão não permitida se houver pagamento informado em S-1210 vinculado; se o período já tiver fechamento (S-1299), exclusão só após reabertura (S-1298). Fonte: leiautes S-1.3.

> `[a confirmar — obter doc oficial]` Confirmar no MOS quais categorias 3XX (servidores estatutários/comissionados/RPPS) entram via **S-2200** vs **S-2300 (TSVE)** no caso de Maximiliano de Almeida (estatutários do RPPS municipal, contratos temporários, agentes políticos, conselheiros tutelares). Doc: MOS S-1.3, descrição S-2200 e S-2300 + tabela de categorias de trabalhador (anexo do MOS).

### 2.3 Eventos periódicos
| Evento | Título |
|--------|--------|
| **S-1200** | Remuneração de trabalhador vinculado ao **RGPS** |
| **S-1202** | Remuneração de servidor vinculado ao **RPPS** |
| **S-1207** | Benefícios — Entes Públicos (aposentadorias/pensões RPPS) |
| **S-1210** | Pagamentos de Rendimentos do Trabalho |
| **S-1299** | Fechamento dos Eventos Periódicos |
| **S-1298** | Reabertura dos Eventos Periódicos |

- **Distinção RGPS x RPPS** (confirmada no MOS S-1.3):
  - **S-1200** — remuneração de categorias [1XX] com `{tpRegPrev}`=[1,3] ou inexistente, [2XX],[5XX],[7XX],[9XX] e servidores [301,302,303,304,306,307,309,310,312,314] com `{tpRegPrev}`=[1,3].
  - **S-1202** — SOMENTE servidores `{codCateg}`=[301,302,303,304,306,307,309,310,312,314] com `{tpRegPrev}`=[2,4] (vinculados ao RPPS).
  - Fonte: busca MOS/leiautes S-1.3 (gov.br/esocial).
- **S-1207** — benefícios pagos pelo ente público (aposentados e pensionistas do RPPS). Relevante porque o município pode pagar inativos/pensionistas pelo regime próprio.
- **S-1210** — não pode ser excluído sem reverter dependências; campo `{indGuia}` segue o evento original (S-1299/S-1298 = fechamento/reabertura da folha).

> `[a confirmar — obter doc oficial]` Mapear se o RPPS de Maximiliano de Almeida paga inativos/pensionistas (→ exige S-1207) ou se há apenas vinculação ao RGPS / fundo previdenciário separado. Doc: lei municipal do RPPS + MOS S-1.3 descrição S-1207.

---

## 3. O que muda no setor público (Grupo 4)

- **Cronograma de implantação (Grupo 4 — órgãos públicos), consolidado pela Portaria Conjunta SEPRT/RFB/ME nº 71/2021 e ajustes posteriores:**
  - **Fase 1 — Tabelas (S-1000 a S-1080):** envio a partir de **21/07/2021 até 21/11/2021**.
  - **Fase 2 — Não-periódicos (S-2190 a S-2420, exceto SST):** **22/11/2021 a 21/04/2022** — inclui cadastro inicial de servidores ativos, aposentados e pensionistas.
  - **Fase 3 — Periódicos (folha: S-1200, S-1202, S-1207, S-1298, S-1299):** obrigatórios — início **22/04/2022**, posteriormente **adiado para competência agosto/2022** (Portaria Conjunta MTP/RFB/ME nº 2, de 20/04/2022; fechamento da folha de agosto/22).
  - **Fase 4 — SST (S-2210, S-2220, S-2240):** **NÃO obrigatórios para servidores vinculados ao RPPS**; obrigatórios p/ celetistas e estatutários vinculados ao RGPS (data adiada p/ 01/2023).
  - Fontes:
    - https://www.gov.br/esocial/pt-br/noticias/implantado-o-esocial-para-os-orgaos-publicos
    - https://www.gov.br/esocial/pt-br/noticias/orgaos-publicos-inicio-dos-eventos-periodicos-e-fechamento-da-folha-de-agosto-22
    - https://www.gov.br/receitafederal/pt-br/assuntos/noticias/2021/julho/cronograma-de-implantacao-do-esocial-e-atualizado
    - https://www.gov.br/esocial/pt-br/acesso-ao-sistema/cronograma-de-implantacao

- **Especificidades do setor público:**
  - Evento próprio de remuneração RPPS (**S-1202**) e de benefícios de ente público (**S-1207**) — não existem no setor privado puro.
  - **SST não obrigatório para RPPS** (servidores estatutários do regime próprio).
  - Campo `{tpRegPrev}` (tipo de regime previdenciário: 1=RGPS, 2=RPPS, 3=RPPS exterior?, 4=…) é o discriminador entre S-1200 e S-1202. `[a confirmar]` valores exatos da tabela `tpRegPrev` no MOS.

> `[a confirmar — obter doc oficial]` Como o piloto inicia operação muito depois das datas acima (2026), confirmar regime de **cargas iniciais / retroativas** e situação cadastral atual do município no ambiente de produção do eSocial (se já transmite, se há passivo). Não é regra de leiaute — é situação operacional do ente.

---

## 4. Simplificação (eSocial Simplificado — contexto)

- O **eSocial Simplificado** reduziu **mais de 30% dos campos** dos leiautes e **excluiu 12 eventos** (transmitidos pelas empresas). Previsto na **Lei nº 13.874/2019** (Lei da Liberdade Econômica).
- Leiaute final simplificado = **versão S-1.0**, aprovado pela **Portaria Conjunta SEPRT/RFB nº 82**. A linha S-1.x (S-1.0 → S-1.1 → S-1.2 → S-1.3) é a família "simplificada" atual; substituiu a antiga numeração 2.x.
- **Substituição de obrigações:** INSS/FGTS migraram de GFIP/Conectividade Social para recolhimento unificado via **DAE (Documento de Arrecadação do eSocial)** e **DCTFWeb**, conforme calendário de substituição (a partir da competência out/2021 para grupos anteriores).
- Fontes:
  - https://www.gov.br/esocial/pt-br/noticias/publicada-versao-final-do-leiaute-do-esocial-simplificado-s-1-0
  - https://www.gov.br/esocial/pt-br/documentacao-tecnica/manuais/manual-de-orientacao-do-esocial-mos-v-s-1-0.pdf

> `[a confirmar — obter doc oficial]` Confirmar **forma de recolhimento e declaração** para ente público municipal (DAE/DCTFWeb x retenções RGPS x contribuições RPPS) — pode divergir do regime de empresas. Doc: MOS S-1.3 (módulo de tributação) + orientações DCTFWeb para órgãos públicos (Receita).

---

## 5. Implicações para o M5 (assinatura A1 / arquitetura)

- Todos os eventos eSocial são **XML assinados digitalmente (XMLDSig) com certificado A1 server-side** — alinhado ao serviço de envelope encryption em construção. `[a confirmar]` política de assinatura exata (canonicalização, `<Reference>`, transform enveloped, algoritmo de hash) — Doc: MOS S-1.3, seção de assinatura/segurança + XSD dos eventos.
- Ordem de dependência obrigatória: **S-1000 → tabelas (S-1005/S-1010/S-1020/S-1070) → S-2200/S-2300 → S-1200/S-1202/S-1207/S-1210 → S-1299**. O domínio RH (Servidor, Cargo, FolhaDePagamento) precisa mapear para essas entidades.
- Recibos/protocolos e processamento **assíncrono por lote** (envio de lote + consulta de retorno via S-5xxx — eventos de retorno/totalizadores). `[a confirmar]` eventos de retorno S-5001/S-5002/S-5003/S-5011/S-5013 e fluxo de webservice (produção restrita vs produção). Doc: MOS S-1.3 + manual dos Web Services do eSocial.

---

## Pendências consolidadas [a confirmar — obter doc oficial]
1. Travar versão exata MOS + NT a implementar (S-1.3 NT 06/2026 rev. 09/04/2026?) + baixar XSD/schemas. — *MOS S-1.3 + Anexo I Leiautes + pacote de schemas*
2. Categorias 3XX: S-2200 vs S-2300 (TSVE) para os vínculos reais do município. — *MOS S-1.3, tabela de categorias*
3. Tabela `tpRegPrev` (valores 1/2/3/4) e regra de roteamento S-1200 x S-1202. — *MOS S-1.3*
4. RPPS paga inativos/pensionistas? (→ necessidade de S-1207). — *Lei municipal do RPPS + MOS S-1.3*
5. Situação operacional/cargas retroativas do município no ambiente de produção. — *operacional (eSocial produção)*
6. Recolhimento/declaração (DAE/DCTFWeb) para órgão público. — *MOS S-1.3 tributação + orientações DCTFWeb*
7. Política de assinatura XMLDSig dos eventos. — *MOS S-1.3 seção segurança + XSD*
8. Eventos de retorno S-5xxx e fluxo de Web Services (lote/protocolo). — *Manual de Orientação dos Desenvolvedores / Web Services eSocial*
