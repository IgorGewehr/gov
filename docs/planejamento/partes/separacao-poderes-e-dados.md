# Separação de Poderes e Segregação de Dados Sensíveis — Tensorroot.Gov

> Parte do planejamento. Define os **fundamentos jurídico-arquiteturais** de quem PODE ver o quê.
> Complementa: `docs/diagnostico/REQUISITOS-GOVTECH.md`, `docs/architecture/contabilidade-pcasp-tce.md`.
> Foco: isolamento Executivo×Legislativo, LGPD em módulos sensíveis e papéis de controle.
> Data: 2026-06-22. Piloto: Maximiliano de Almeida/RS (TCE-RS).

---

## A. Separação de Poderes — Executivo × Legislativo (tenants DISTINTOS)

### A.1. Fundamento constitucional

A separação de Poderes é **cláusula pétrea** (CF art. 2º: "São Poderes da União, independentes e
harmônicos entre si, o Legislativo, o Executivo e o Judiciário"). Por **simetria** (CF arts. 25, 29 e
31), o princípio é de observância **obrigatória no Município**: o Governo Municipal é composto pelo
**Poder Executivo** (chefiado pelo Prefeito) e pelo **Poder Legislativo** (exercido pela Câmara de
Vereadores). São órgãos **independentes entre si** — não há subordinação hierárquica de um ao outro.

> Consequência direta para o produto: **a Prefeitura e a Câmara do MESMO município são duas
> instituições distintas**, com gestão, ordenação de despesa, folha de pagamento, contabilidade,
> patrimônio e prestação de contas **próprias e separadas**. Por isso são **tenants distintos** no
> Tensorroot.Gov (CLAUDE.md §4: "Executivo e Legislativo do mesmo município são tenants DISTINTOS").

### A.2. CNPJ próprio da Câmara

A Câmara Municipal possui **CNPJ próprio** (inscrição como órgão público do Poder Legislativo,
distinta da inscrição da Prefeitura). Decisões dos Tribunais de Contas (ex.: TCE-SC, Relatório
Técnico) confirmam que a Câmara, embora seja **unidade orçamentária** dentro do orçamento único do
Município (LOA municipal), atua com **autonomia administrativa e financeira** e mantém CNPJ próprio
para fins de obrigações acessórias, eSocial, contratos e remessas. O CNPJ é o **discriminador natural
de tenant** no sistema (CLAUDE.md §0/§5: multi-tenant por CNPJ/poder).

> Nuance contábil (importante para o módulo Finanças): há TCEs que entendem que, mesmo com CNPJ
> próprio, a contabilidade da Câmara **consolida** no balanço do Município (o orçamento é uno — LOA
> aprovada pela própria Câmara). No produto isso NÃO quebra o isolamento de tenant: cada poder é um
> tenant, e a **consolidação** ocorre por troca de artefatos (remessas/MSC), nunca por acesso direto
> ao banco do outro poder. Ver A.5.

### A.3. Autonomia financeira — o duodécimo (CF art. 168 e art. 29-A)

A Câmara não executa o orçamento do Município; ela **recebe recursos** do Executivo por meio do
**duodécimo**: os recursos correspondentes às dotações orçamentárias do Legislativo são entregues
**até o dia 20 de cada mês** (CF art. 168), em parcelas de **um doze avos** (duodécimo). O **art.
29-A da CF** fixa o **teto** do repasse ao Legislativo municipal como percentual da receita tributária
+ transferências do exercício anterior (faixas por população, de 7% a 3,5%). Descumprir o repasse no
prazo é **crime de responsabilidade do Prefeito**; gastar acima do teto, do Presidente da Câmara.

> Implicação de produto: o **repasse do duodécimo** é um fluxo financeiro **entre dois tenants** e deve
> ser modelado como **transferência inter-tenant auditada** (evento de integração / conciliação),
> jamais como leitura cruzada de banco. O Executivo empenha/paga o repasse no seu tenant; a Câmara
> registra a receita de repasse no tenant dela. Os dois lados conciliam por **artefato**, não por acesso.

### A.4. Por que o Prefeito JAMAIS acessa o sistema da Câmara (e vice-versa)

1. **Independência dos Poderes (CF art. 2º/29/31):** o Executivo não exerce gestão administrativa
   sobre o Legislativo. Permitir que o Prefeito leia/edite dados da Câmara (proposições, votações,
   folha dos vereadores e servidores da Casa, contratos da Câmara) seria **ingerência de um Poder
   sobre o outro** — inconstitucional e materialmente uma violação de isolamento de tenant.
2. **Sigilo do processo legislativo:** sessões, pareceres, votações e tramitação de proposições têm
   regramento próprio (Regimento Interno + Lei Orgânica). O Executivo só conhece o produto público
   (LAI/Transparência), não o sistema interno.
3. **Controle externo recíproco:** o Legislativo **fiscaliza** o Executivo (CF art. 31, com auxílio do
   TCE). Se o controlado (Prefeito) pudesse acessar o sistema do controlador (Câmara), o controle
   seria fraudável.
4. **Ordenação de despesa distinta:** Prefeito e Presidente da Câmara são **ordenadores de despesa
   diferentes**, cada um responde por suas contas perante o TCE-RS. Misturar acessos misturaria
   responsabilidades.

**Enforcement no Tensorroot.Gov:** isolamento já é regra constitucional do código (CLAUDE.md §5):
`IMustHaveTenant`, Global Query Filter por `TenantId`, `TenantInterceptor` que carimba e **lança
exceção em gravação cross-tenant**, database/schema-per-tenant, JWT resolve o tenant por requisição.
**Não existe** papel "super-prefeito" que enxergue os dois tenants. Mesmo o administrador da
plataforma (Tensorroot) **não tem leitura de negócio** — só provisiona tenants e licenças (e isso é
um gap atual: `/admin/tenants` está sem RBAC — ver ESTADO-ATUAL §Plataforma, deve ser corrigido).

### A.5. O único acoplamento legítimo: consolidação e prestação de contas

A interação Executivo↔Legislativo permitida é **por troca de artefatos auditados**, nunca por acesso
direto:

| Fluxo | Mecanismo no produto | Direção |
|---|---|---|
| Repasse do duodécimo | Integration Event / conciliação inter-tenant (Outbox) | Exec → Legisl |
| Consolidação contábil municipal (balanço geral) | Cada poder gera sua MSC/remessa; consolidação por artefato | Ambos → TCE/SICONFI |
| Remessas SIAPC/PAD ao TCE-RS | Cada tenant gera e transmite **a sua** remessa | Cada poder isolado |
| Transparência (LAI) | Cada tenant publica seu portal; dados abertos por poder | Cada poder isolado |

---

## B. Segregação de Dados Sensíveis sob a LGPD (Lei 13.709/2018)

### B.1. Regra estruturante — dado sensível tem regime próprio (art. 11)

A LGPD classifica como **dado pessoal sensível** (art. 5º, II) os dados sobre **origem racial/étnica,
convicção religiosa, opinião política, filiação sindical, dado referente à saúde ou à vida sexual,
dado genético ou biométrico**. O **art. 11** submete o tratamento desses dados a **regime mais
restritivo**: as hipóteses de tratamento são **mais estreitas** que as do art. 7º (dado comum).

Para o **Poder Público**, as bases legais típicas que dispensam consentimento são:
- **Art. 11, II, "a" e "b"** — cumprimento de **obrigação legal/regulatória** e **execução de
  políticas públicas** previstas em lei pelo controlador (é o caso de SUS, SUAS, Educação básica).
- **Art. 11, II, "f"** — **tutela da saúde**, exclusivamente em procedimento realizado por
  **profissionais de saúde, serviços de saúde ou autoridade sanitária**.
- **Art. 7º, II e III / art. 23** — tratamento por pessoa jurídica de direito público para o
  **cumprimento de competência legal** e **execução de políticas públicas**.

> Princípios obrigatórios sobre TODO acesso (art. 6º): **finalidade, adequação, necessidade
> (minimização), segurança e prevenção, e prestação de contas (accountability)**. No produto isso se
> traduz em: acesso por **necessidade-de-conhecer**, escopo mínimo, e **trilha de acesso (quem leu o
> quê, quando, por quê)** — exigida pela CLAUDE.md §6 e hoje AUSENTE (gap LGPD em ESTADO-ATUAL).

### B.2. Quadro de "quem PODE ver o quê" por módulo sensível

| Módulo | Natureza do dado | Base legal LGPD | Quem PODE acessar (necessidade-de-conhecer) | Quem NÃO acessa |
|---|---|---|---|---|
| **Saúde** | Dado **sensível de saúde** (PEP, CID/CIAP, prescrição, imunização) | Art. 11, II, "f" (tutela da saúde por profissional/serviço de saúde); art. 11, II, "a/b" (política pública SUS) + **sigilo profissional** (CFM/Cód. Ética Médica; CF art. 5º X/XIV) | **Profissional de saúde no vínculo do atendimento** (médico, enfermeiro, farmacêutico), regulador (SISREG), gestor de saúde para fins epidemiológicos **agregados/anonimizados** | RH, Tributos, Educação, Patrimônio, **qualquer gestor administrativo**, e o **Prefeito** (não acessa prontuário individual) |
| **Assistência Social** | Dado de **vulneráveis** (Prontuário SUAS, CRAS/CREAS, violência, CadÚnico) | Art. 11, II, "a/b" (política pública SUAS — LOAS 8.742/93); **sigilo do CadÚnico** (Dec. 11.016/2022 e normas MDS); ECA p/ crianças/adolescentes | **Técnico de referência do SUAS** (assistente social, psicólogo) vinculado ao caso; gestor para dados **agregados** | Demais módulos, gestor administrativo, **Prefeito** (não acessa prontuário individual) |
| **Educação** | Dados de **menores** (matrícula, diário, frequência, BNCC, alimentação) | Art. 11, II, "a/b" (educação básica — política pública, LDB 9.394/96); **ECA** + **art. 14 LGPD** (dados de criança/adolescente, melhor interesse) | Equipe escolar vinculada (direção, secretaria, professor da turma); responsável legal sobre o próprio menor; INEP via EducaCenso (dados conforme leiaute) | Saúde, Tributos, RH-folha, **Prefeito** sem necessidade funcional; uso comercial/discriminatório é **vedado** |
| **Tributos** | **Sigilo fiscal** (situação econômico-financeira do contribuinte) | **CTN art. 198** (sigilo) e exceções; CF art. 5º X/XII | **Auditor/fiscal tributário** no exercício do ofício; Procuradoria para **dívida ativa/execução fiscal**; o **próprio contribuinte** sobre seus dados | Saúde, Educação, Assistência, RH, Patrimônio; divulgação a terceiros sem base legal é **infração** (art. 198) |
| **RH / Folha** | Dado pessoal comum + **sensível** (saúde ocupacional, sindical), CPF, dependentes | Art. 7º (obrigação legal trabalhista/eSocial) e art. 11 quando houver dado sensível | Setor de Pessoal/RH; servidor sobre seus dados; eSocial (gov. federal, leiaute) | Demais módulos; remuneração individual é **público** (LAI/Transparência) mas dado sensível NÃO |

> **Regra de produto:** a segregação não é só RBAC entre **papéis** — é também **isolamento por
> módulo** (CLAUDE.md §2/§4: módulo nunca acessa o interno de outro; só `*.Contracts`). Um handler de
> RH **não consegue** consultar o PEP do paciente nem o prontuário SUAS porque esses agregados vivem em
> outro Bounded Context, com outro DbContext/schema e sem contrato exposto para isso. LGPD aqui é
> **reforçada pela arquitetura**, não apenas por permissão.

### B.3. Dado público × dado sensível (não confundir com Transparência/LAI)

A **LAI (Lei 12.527/2011)** e a transparência ativa **NÃO** se aplicam a dado sensível/pessoal: a
LGPD e a LAI **convivem** — publica-se **remuneração de cargo** (transparência), mas **não** dados de
saúde, prontuário SUAS, dados de menores, ou situação fiscal individual. O módulo **Transparencia**
deve aplicar **anonimização/agregação** antes de publicar qualquer recorte que toque dado sensível.

### B.4. Trilha de acesso LGPD (requisito a implementar)

A LGPD (art. 6º, X — accountability; art. 37 — registro das operações) e o TCE exigem **registro de
acesso de leitura** a dados sensíveis: usuário, papel, tenant, registro acessado, finalidade,
timestamp, IP. Hoje o sistema tem trilha de **mutação** (AuditTrail) mas **não tem trilha de leitura**
(gap em ESTADO-ATUAL). É **obrigatório** para Saúde, Assistência Social e Tributos.

---

## C. Papéis especiais de controle e seus acessos

> Estes papéis **transcendem** a RBAC ordinária e precisam de modelagem dedicada. Atenção: todos
> respeitam o **isolamento de tenant** — um Controlador da Prefeitura NÃO vê a Câmara.

### C.1. Controladoria / Controle Interno (CF art. 31 e art. 74)

A CF art. 74 obriga os Poderes a manterem **sistema de controle interno integrado**. Cada poder
(Executivo e Legislativo) tem o **seu** controle interno. A Controladoria-Geral do Município (CGM,
no Executivo) fiscaliza a **legalidade, economicidade e regularidade** da gestão — acompanha
empenho/liquidação/pagamento, contratos, folha, contabilidade.

| Aspecto | Definição no produto |
|---|---|
| Escopo | **Amplo dentro do próprio tenant** — leitura de Finanças, Administração, RH, Patrimônio, Tributos |
| Dado sensível individual | **NÃO por padrão** — opera sobre dados financeiro-administrativos e **agregados**; prontuário de saúde/SUAS exige processo formal e finalidade específica (necessidade-de-conhecer) |
| Mutação | **Não altera** dados de negócio — papel de **leitura + apontamento** (recomendações/ressalvas) |
| Auditoria | Toda leitura do Controlador é registrada (trilha LGPD) |
| Tenant | Restrito ao seu poder; **não cruza** Executivo↔Legislativo |

### C.2. Procuradoria (Jurídico)

A Procuradoria do Município representa judicial e extrajudicialmente o ente e dá parecer.

| Aspecto | Definição no produto |
|---|---|
| Sigilo fiscal | **PODE** acessar dados fiscais do contribuinte **para dívida ativa e execução fiscal** (CTN art. 198 — exceção de interesse da Administração, processo regularmente instaurado) |
| Contratos/Licitações | Acesso a Administração (pareceres, recursos, sanções) |
| Dado sensível (saúde/SUAS/menor) | Apenas mediante **processo formal e finalidade específica**, com recibo/registro (art. 198 CTN aplica safeguard análoga; LGPD art. 11) |
| Mutação | Limitada (anota pareceres, inscreve CDA); não gere o módulo finalístico |

### C.3. Auditoria do TCE-RS (controle externo)

O TCE-RS é **órgão externo** (CF arts. 31, 70, 71): fiscaliza a aplicação dos recursos públicos
municipais. **Não é usuário interativo do tenant**; o acesso se dá por **remessa de artefatos**
(SIAPC/PAD) e, quando há auditoria in loco, por **requisição formal**.

| Aspecto | Definição no produto |
|---|---|
| Modo de acesso primário | **Recebimento de remessas** (SIAPC/PAD) e **MSC/SICONFI** — pull/push de artefatos assinados (A1), não login no sistema operacional |
| Acesso a dados protegidos | Pode requisitar dado sob sigilo (fiscal/sensível) **no exercício do controle externo** (CF arts. 70/71; CTN art. 198 — exceção), via **processo formalizado, com recibo, finalidade específica e preservação do sigilo** (Revista TCU 146) |
| Perfil técnico no sistema (se concedido) | **Somente leitura, auditado, com escopo e prazo definidos** — equivalente a "consulta administrativa dedicada" da CLAUDE.md §5; nunca o filtro global é desligado |
| Tenant | Por poder — TCE analisa contas do Prefeito e do Presidente da Câmara **separadamente** |
| Imutabilidade | A trilha (AuditTrail) que o TCE consome **deve ser imutável de fato** (WORM/hash-chain) — gap atual a corrigir |

### C.4. Resumo — matriz de poderes especiais

| Papel | Tenant | Leitura financeiro-admin | Leitura dado sensível individual | Mutação | Base legal |
|---|---|---|---|---|---|
| Controlador (Controle Interno) | Próprio poder | Sim (amplo) | Não (só agregado; exceção c/ processo) | Não (aponta) | CF art. 74 |
| Procurador | Próprio poder | Sim (jurídico/fiscal/contratos) | Só c/ processo e finalidade | Limitada (CDA/parecer) | CTN 198; LGPD 11 |
| Auditor TCE-RS | Por poder (externo) | Via remessa/requisição | Só por requisição formal | Não | CF 70/71; CTN 198 |
| Admin Plataforma (Tensorroot) | Cross (técnico) | **Não** (só provisiona/licença) | **Não** | Tenant/licença | Operador LGPD art. 39 |

---

## D. Implicações de implementação (checklist)

1. **Tenant por CNPJ+poder** — Executivo e Legislativo nunca compartilham banco/schema (já implementado; manter).
2. **Sem papel cross-tenant de negócio** — corrigir `/admin/tenants` (hoje sem RBAC) e nunca criar "super-prefeito".
3. **Repasse de duodécimo** como integração inter-tenant auditada (Outbox), não leitura cruzada.
4. **RBAC por papel especial** — modelar Controlador, Procurador e perfil-TCE com escopo e finalidade.
5. **Trilha de LEITURA LGPD** (quem leu dado sensível, quando, por quê) — **a implementar** (gap atual).
6. **AuditTrail imutável de fato** (WORM/hash-chain) — pré-requisito do controle externo (gap atual).
7. **Isolamento por módulo reforça LGPD** — RH não enxerga PEP/SUAS por contrato; manter `*.Contracts` mínimos.
8. **Transparência aplica anonimização** antes de publicar qualquer recorte que toque dado sensível.

---

## Fontes

- [CF/88 — separação de Poderes, simetria municipal (arts. 2º, 25, 29, 31)](https://www.estrategiaconcursos.com.br/blog/o-principio-separacao-dos-poderes/)
- [TCE-SC — Câmara de Vereadores: inscrição no CNPJ e autonomia (Relatório Técnico)](https://consulta.tce.sc.gov.br/relatoriosdecisao/relatoriotecnico/3712642.HTML)
- [Lei 4.320/1964 — Normas Gerais de Direito Financeiro](https://www2.camara.leg.br/legin/fed/lei/1960-1969/lei-4320-17-marco-1964-376590-promulgacaodevetos-30916-pl.html)
- [TCM-GO — art. 29-A da CF, cálculo do duodécimo](https://www.tcm.go.gov.br/site/wp-content/uploads/2017/08/RC009-2017.pdf)
- [LGPD — Art. 11 (tratamento de dados sensíveis) — texto comentado](https://lgpd-brasil.info/capitulo_02/artigo_11)
- [Art. 11 da Lei 13.709/2018 (Jusbrasil)](https://www.jusbrasil.com.br/topicos/200399171/artigo-11-da-lei-n-13709-de-14-de-agosto-de-2018)
- [Ministério da Saúde — LGPD na saúde pública (manual e-SUS APS)](https://sisaps.saude.gov.br/sistemas/esusaps/docs/manual/LGPD/)
- [LGPD na Saúde — cartilha OAB-DF (sigilo médico e bases legais)](https://oabdf.org.br/wp-content/uploads/2022/01/Cartilha-LGPD-na-Saude.pdf)
- [Tratamento de dados de saúde: bases legais e limites (Migalhas)](https://www.migalhas.com.br/depeso/449916/tratamento-de-dados-em-saude-bases-legais-limites-e-boas-praticas)
- [Receita Federal — Sigilo fiscal e exceções segundo o CTN (art. 198)](https://www.gov.br/receitafederal/pt-br/assuntos/orientacao-tributaria/sigilo-fiscal/sigilo-fiscal-excecoes-de-acordo-com-CTN)
- [CTN art. 198 — Lei 5.172/66 (Jusbrasil)](https://www.jusbrasil.com.br/topicos/10566268/artigo-198-da-lei-n-5172-de-25-de-outubro-de-1966)
- [CGU — Sistema de Controle Interno (CF art. 74)](https://www.gov.br/cgu/pt-br/assuntos/auditoria-e-fiscalizacao/sistema-de-controle-interno)
- [Revista TCU 146 — O sigilo fiscal e a prestação de contas (acesso de órgãos de controle)](https://revista.tcu.gov.br/ojs/index.php/RTCU/article/download/1674/1824/3288)
- [TCE-MA — fiscalização do cumprimento da LRF pelos municípios](https://site.tce.ma.gov.br/index.php/noticias/2083-fiscalizacao-do-tce-representa-contra-prefeitos-que-descumprem-regras-de-transparencia-da-lrf)
