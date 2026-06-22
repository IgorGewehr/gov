# Pesquisa — ISS & NFS-e Nacional (ADN): ingestão, apuração e Reforma Tributária (M6 Tributos)

> Escopo: como o município **INGERE** os XMLs de NFS-e do **ADN** (Ambiente de Dados Nacional), deduplica por chave de acesso, **apura o ISS** (próprio / retido / substituição), produz o **Livro Eletrônico**, e a relação com a **Reforma Tributária (ISS→IBS)**. **NÃO emitimos NFS-e** (constituição §8; ADR-0003 — integração PASSIVA).
> Regra de ouro (CLAUDE.md §16): toda afirmação factual tem FONTE (URL) ou está marcada `[a confirmar — <doc oficial>]`. Alíquotas/PGV/regras são SEMPRE parametrizáveis por tenant (lei municipal), nunca hardcoded.
> Data da pesquisa: 2026-06-22.

---

## 0. Contexto e princípio (não-emissão)

- O **padrão nacional da NFS-e** está em produção; a nota **nasce e é assinada no Ambiente Nacional** (gov.br / Receita Federal). O município é **titular do ISS** e **consumidor passivo** dos dados, não emissor. FONTE: ADR-0003 (`docs/adr/0003-nfse-nacional-adn-integracao-passiva.md`); CLAUDE.md §8.
- O **ADN** disponibiliza a **API DF-e** para **receber** os documentos fiscais compartilhados pelos municípios aderentes e **distribuí-los** aos municípios que tenham **papel de interesse** (prestador / tomador / intermediário) no documento. FONTE: <https://www.gov.br/nfse/pt-br/biblioteca/documentacao-tecnica/apis-prod-restrita-e-producao>

---

## 1. Ambientes e portais de API do ADN

| Ambiente | Base URL | Doc Municípios | Doc Contribuintes |
|---|---|---|---|
| **Produção** | `https://adn.nfse.gov.br/` | `https://adn.nfse.gov.br/municipios/docs/index.html` | `https://adn.nfse.gov.br/contribuintes/docs/index.html` |
| **Produção Restrita** (homologação) | `https://adn.producaorestrita.nfse.gov.br/` | `https://adn.producaorestrita.nfse.gov.br/municipios/docs/index.html` | `https://adn.producaorestrita.nfse.gov.br/contribuintes/docs/index.html` |

FONTE: <https://www.gov.br/nfse/pt-br/biblioteca/documentacao-tecnica/apis-prod-restrita-e-producao>

- O município consome da família de endpoints **`/municipios`** do ADN; o contribuinte/ERP usa **`/contribuintes`**. FONTE: idem acima.
- Documentação técnica atual (manuais, anexos, leiautes XSD): <https://www.gov.br/nfse/pt-br/biblioteca/documentacao-tecnica/documentacao-atual>
- Manual de APIs do ADN para municípios (gov.br): <https://www.gov.br/nfse/pt-br/biblioteca/documentacao-tecnica/documentacao-atual/manual-contribuintes-apis-adn-sistema-nacional-nfse.pdf> `[a confirmar — PDF não parseável via fetch; baixar e extrair endpoints/limites exatos da família /municipios]`

---

## 2. Ingestão dos XMLs — distribuição por NSU

- **Padrão de distribuição:** consumo incremental por **NSU** (Número Sequencial Único). O consumidor (município) guarda o **último NSU conhecido** e pede os seguintes. Se o NSU informado for menor que o primeiro disponível, o ADN entrega a partir do primeiro disponível. FONTE (conceito, busca na doc gov.br/ADN): <https://www.gov.br/nfse/pt-br/biblioteca/documentacao-tecnica/apis-prod-restrita-e-producao>
- **Método (padrão DF-e):** `GET /DFe/{NSU}` retorna os documentos fiscais (NFS-e e **eventos** de NFS-e) a partir do NSU informado. FONTE (conceito): idem.
- **Tamanho do lote por requisição:** fontes secundárias indicam **até 50 DF-e** por chamada a partir do último NSU. FONTE (secundária, a validar contra manual oficial): <https://notagateway.com.br/blog/api-nfse-nacional/> · `[a confirmar — Manual APIs ADN Municípios: nº exato de documentos/lote e nome exato do recurso da família /municipios]`
- **Consulta unitária por chave:** `GET /nfse/{chaveAcesso}` retorna a NFS-e (XML compactado **GZip + Base64**); `GET /NFSe/{ChaveAcesso}/Eventos` retorna os eventos. FONTE (secundária): <https://notagateway.com.br/blog/api-nfse-nacional/> · `[a confirmar — leiaute/encoding exatos no manual oficial]`
- **Papéis de interesse:** o ADN entrega ao município os DF-e em que ele tem interesse (titular do ISS — município de incidência); ao contribuinte, os documentos em que é **prestador, tomador ou intermediário**. FONTE: <https://www.gov.br/nfse/pt-br/biblioteca/documentacao-tecnica/apis-prod-restrita-e-producao>
- **Autenticação:** acesso autenticado por **certificado digital** + credenciamento/escopo do tenant no ambiente nacional. FONTE (conceito, ADR-0003 §"credenciamento/escopo de acesso"); `[a confirmar — Manual APIs ADN: mTLS vs token, perfil do certificado (e-CNPJ do ente), escopo município]`

### Implicação de arquitetura (Worker `NfseSync`)
- Já existe `src/Workers/Tensorroot.Gov.Workers.NfseSync` (ADR-0003). Estratégia: **cursor de NSU por tenant** persistido, sync **diária/incremental**, **Polly** (retry + circuit breaker), reprocessamento **idempotente pela chave de acesso**. FONTE: ADR-0003 §Decisão/Consequências.
- Domínio atual: `NotaFiscalServico` (AggregateRoot, `IMustHaveTenant`) com `ChaveAcesso`, `PrestadorCnpj`, `TomadorDocumento`, `ValorServico`, `ValorIss`, `DataEmissao`, `Competencia` e evento `NotaFiscalServicoImportada`. FONTE: `src/Modules/Tributos/.../Domain/Nfse/NotaFiscalServico.cs`.

---

## 3. Deduplicação e eventos

- **Chave de acesso (50 dígitos)** — identificador nacional único da NFS-e; composição: **Cód. Município IBGE (7) + Ambiente de geração (1) + Tipo de inscrição federal (1) + Inscrição federal/CNPJ-CPF (14) + Nº NFS-e (13) + Ano/Mês de emissão (AAMM) (4) + Código numérico (9) + Dígito verificador (1)**. FONTE (conceito, busca doc gov.br/ADN): <https://www.gov.br/nfse/pt-br/biblioteca/documentacao-tecnica/apis-prod-restrita-e-producao> · `[a confirmar — manual técnico: ordem/largura exata de cada campo e algoritmo do DV]`
- **Dedup:** a chave de acesso é a **idempotency key**. Constraint único por `(TenantId, ChaveAcesso)`; reprocessamentos/lotes sobrepostos por NSU são absorvidos sem duplicar. (Decisão de arquitetura, alinhada ao ADR-0003.)
- **Eventos da NFS-e** alteram a situação da nota e precisam ser ingeridos junto com o documento: **cancelamento** e **substituição** (a substituta referencia a substituída pela chave). FONTE (conceito): <https://www.gov.br/nfse/pt-br/biblioteca/documentacao-tecnica/apis-prod-restrita-e-producao> · `[a confirmar — lista completa/códigos dos eventos no manual de eventos do leiaute nacional]`
- **Implicação de apuração:** ISS de nota cancelada/substituída deve sair da base; manter histórico imutável (auditoria, §6 da constituição) e refletir a situação vigente no painel/livro.

---

## 4. Apuração do ISS (próprio / retido / substituição)

Base legal nacional: **LC 116/2003** (normas gerais do ISSQN; lista de serviços) + **lei/código tributário municipal** (alíquotas, responsáveis, obrigações acessórias). FONTE: <https://netcpa.com.br/colunas/iss-retido-como-funciona-quando-aplicar-e-quem-paga/25854>

Modalidades a apurar a partir do XML ingerido:
- **ISS próprio (normal):** devido pelo **prestador** estabelecido no município; regra geral, o imposto é do município do estabelecimento prestador (exceções de local da prestação na LC 116). FONTE: <https://netcpa.com.br/colunas/issqn-quando-o-municipio-deve-ou-nao-efetuar-a-retencao-do-imposto-sobre-servicos/24978>
- **ISS retido na fonte (pelo tomador):** em hipóteses da LC 116, o **tomador** calcula, retém e recolhe o ISS ao município competente no pagamento ao prestador (ex.: construção civil, vigilância, limpeza, eventos). FONTE: <https://www.contabilizei.com.br/contabilidade-online/iss-retido/>
- **Substituição tributária do ISS:** a lei municipal **pode** (faculdade — "poderão", art. 6º LC 116) atribuir a terceiro (substituto) a responsabilidade pelo crédito tributário. NÃO é automático: depende de lei municipal. FONTE: <https://revista.ibdt.org.br/index.php/RDTA/article/download/1715/1198>

**Regra de parametrização (§16):** **alíquotas por item da lista, mapa de itens retidos/substituídos, e responsáveis** são **configuráveis por tenant** (lei de Maximiliano de Almeida/RS). O motor de apuração só classifica/soma a partir dos campos do XML (valor do serviço, ISS destacado, indicador de retenção, item da lista, local de incidência) e dos parâmetros do tenant. `[a confirmar — obter Código Tributário Municipal de Maximiliano de Almeida/RS: alíquotas por item LC 116, hipóteses de retenção e lista de substitutos]`

### Saída da apuração
- Consolidação por **competência** e por contribuinte: ISS **a recolher (próprio)**, **retido (a recolher pelo responsável)** e **substituído**.
- Divergências (nota com prestador do município sem recolhimento) → gancho para **Lançamento** (já existe `Domain/Lancamentos/Lancamento.cs`) e, em inadimplência, **Dívida Ativa/CDA** (foco do ADR-0003).

---

## 5. Livro Eletrônico (escrituração fiscal de serviços)

- O **Livro Eletrônico / escrituração mensal de serviços** (historicamente "DMS"/livro fiscal) consolida, por competência e por contribuinte, as notas **prestadas e tomadas**, base de cálculo e ISS (próprio/retido/substituído). FONTE (conceito municipal): <https://portal.londrina.pr.gov.br/perguntas-frequentes-nota-fiscal-dms/sistema-declaracao-mensal-de-servicos-dms/145/1856-o-que-significa-ser-substituto-tributario-do-iss>
- No modelo nacional, a **escrituração tende a derivar diretamente do acervo de NFS-e do ADN** (a nota já contém os dados), reduzindo declaração manual. O Livro Eletrônico do Tensorroot.Gov é, portanto, um **read model gerado da ingestão** (notas + eventos + apuração), por tenant/competência. (Decisão de arquitetura.) `[a confirmar — leiaute/obrigação acessória municipal de Maximiliano de Almeida/RS: formato e periodicidade do livro fiscal]`

---

## 6. Reforma Tributária — ISS → IBS (transição)

Base legal: **EC 132/2023** + **LC 214/2025** (IBS, CBS, Imposto Seletivo). FONTE: <https://www.jettax.com.br/blog/cronograma-e-fases-da-reforma-tributaria-de-2026-a-2033/>; <https://www.gov.br/fazenda/pt-br/acesso-a-informacao/acoes-e-programas/reforma-tributaria/arquivos/perguntas-e-respostas-reforma-tributaria_.pdf>

Cronograma (impacto no ISS):
| Ano | Evento | Efeito no ISS |
|---|---|---|
| **2026** | Fase de teste: destaque de **CBS 0,9% + IBS 0,1%** na nota | ISS **inalterado**; tributos antigos vigentes |
| **2027** | CBS plena; extinção PIS/COFINS; IPI→0 | ISS/ICMS **sem alteração** |
| **2029** | Início da transição estadual/municipal — **IBS ~10%** | ISS reduzido proporcionalmente |
| **2030** | **IBS ~20%** | ISS reduzido |
| **2031** | **IBS ~30%** | ISS reduzido |
| **2032** | **IBS ~40%** | ISS reduzido |
| **2033** | IBS pleno | **ISS extinto** |

FONTES: <https://www.jettax.com.br/blog/cronograma-e-fases-da-reforma-tributaria-de-2026-a-2033/>; <https://www.camara.leg.br/noticias/1237089-reforma-tributaria-comeca-fase-de-transicao-com-testes-de-novos-impostos-em-2026/>

- **Governança/repartição:** **Comitê Gestor do IBS** (54 membros: 27 estados/DF + 27 municípios) gere arrecadação e distribuição. Na **transição federativa (2033–2078)**, a distribuição usa a **média histórica de ICMS+ISS de 2019–2026**. **Alíquota de referência** revisada anualmente pelo Senado para manter a carga. **Split payment** (pagamento fracionado automático) previsto. FONTE: <https://www.gov.br/fazenda/pt-br/acesso-a-informacao/acoes-e-programas/reforma-tributaria/arquivos/perguntas-e-respostas-reforma-tributaria_.pdf>
- **Implicação M6:** o motor de tributos deve ser **temporal e parametrizável por competência** — coexistência ISS + IBS em 2029–2032 (percentuais por ano), e a base 2019–2026 importa para projeção de repartição. Os **percentuais de transição 2029–2032 são de fontes secundárias** → `[a confirmar — LC 214/2025 e atos do Comitê Gestor: percentuais oficiais de redução do ISS e da fração IBS por ano]`. NÃO hardcodar: tabela de transição por ano como parâmetro do tenant.

> Observação: 2026/2027 ainda **não** alteram a apuração do ISS — prioridade do M6 segue sendo **ingestão ADN + apuração ISS + Dívida Ativa**; o IBS entra como **camada parametrizável** preparada para 2029.

---

## 7. Pendências consolidadas `[a confirmar — obter doc oficial]`

1. **Manual de APIs do ADN — família `/municipios`**: nomes exatos dos recursos, método de distribuição por NSU, **nº exato de documentos por lote**, paginação/cursor, e contrato dos **eventos** (cancelamento/substituição) — PDFs oficiais não parseáveis via fetch; baixar de gov.br/nfse e extrair.
2. **Autenticação ADN**: mTLS vs token, perfil/uso do certificado (e-CNPJ do ente), escopo de acesso do município.
3. **Chave de acesso (50 díg.)**: largura/ordem exata de cada campo e **algoritmo do DV** (manual técnico).
4. **Leiaute XSD da NFS-e nacional e dos eventos**: campos de retenção/substituição, item da lista de serviços, local de incidência.
5. **Código Tributário Municipal de Maximiliano de Almeida/RS**: alíquotas por item LC 116, hipóteses de retenção, lista de substitutos, leiaute/periodicidade do **Livro Eletrônico** municipal.
6. **Reforma Tributária**: percentuais oficiais de **transição ISS↔IBS 2029–2032** (LC 214/2025 e atos do **Comitê Gestor do IBS**) — substituir as estimativas secundárias por fonte normativa.

---

## Fontes oficiais (prioritárias)
- APIs Prod. Restrita e Produção (gov.br/nfse): <https://www.gov.br/nfse/pt-br/biblioteca/documentacao-tecnica/apis-prod-restrita-e-producao>
- Documentação técnica atual (gov.br/nfse): <https://www.gov.br/nfse/pt-br/biblioteca/documentacao-tecnica/documentacao-atual>
- Manual APIs ADN Municípios (gov.br): <https://www.gov.br/nfse/pt-br/biblioteca/documentacao-tecnica/documentacao-atual/manual-contribuintes-apis-adn-sistema-nacional-nfse.pdf>
- Reforma Tributária — Perguntas e Respostas (Min. Fazenda): <https://www.gov.br/fazenda/pt-br/acesso-a-informacao/acoes-e-programas/reforma-tributaria/arquivos/perguntas-e-respostas-reforma-tributaria_.pdf>
- Câmara dos Deputados — fase de transição/testes 2026: <https://www.camara.leg.br/noticias/1237089-reforma-tributaria-comeca-fase-de-transicao-com-testes-de-novos-impostos-em-2026/>
