# M7 — Verificação cética: pesquisa-saude.md vs. fontes oficiais

> Auditoria de fatos da `pesquisa-saude.md`. Cada afirmação relevante checada contra
> fonte oficial (gov.br, DATASUS, BVSMS/Planalto, manuais MS). Classificação:
> **CONFIRMADO** (fonte oficial bate) · **PLAUSÍVEL-SEM-FONTE** (conceito correto, detalhe
> técnico não validado em doc oficial obtido) · **INCERTO/DIVERGENTE** (não bate, contradiz,
> ou exige doc oficial antes de implementar fiel).
>
> Data da verificação: 2026-06-22. Regra CLAUDE.md §16 mantida: nada hardcoded sem fonte.

---

## Placar

- **CONFIRMADO:** 14 afirmações-chave
- **PLAUSÍVEL-SEM-FONTE:** 7
- **INCERTO/DIVERGENTE (exigem doc oficial / correção):** 6

**Correções materiais que a pesquisa precisa absorver:**
1. **RNDS — token vale 15 min, NÃO 30 min** (Manual de Integração do Barramento, DATASUS).
2. **RNDS — autenticação é "Two-way SSL" (mTLS)** no `POST@/token`; o A1 é o **certificado
   cliente do handshake TLS**, não credencial em corpo de requisição. Implica config de
   transporte (client-cert), não só montar um POST.
3. **SISAB — cadência de envio p/ financiamento é MENSAL** (até o 10º dia útil do mês
   seguinte), NÃO "fechamento quadrimestral". O quadrimestre é só a **janela de média** dos
   indicadores dos Componentes II/III, não o prazo de remessa.
4. **SISAB está sendo SUBSTITUÍDO pelo SIAPS** como sistema de financiamento
   (**Portaria GM/MS nº 7.639/2025**, calendário oficial 2026). A pesquisa trata SIAPS como
   "mencionado em fontes CONASEMS / MÉDIA" — na verdade é o sistema-alvo, não um detalhe.

---

## 1. SIOPS — mínimo 15% ASPS

| Afirmação na pesquisa | Veredito | Evidência |
|---|---|---|
| Declaração **bimestral**, prazo **até 30 dias após o bimestre** (LC 141/2012) | **CONFIRMADO** | FAQ SIOPS/MS: periodicidade passou a bimestral a partir de 2013; alimentação até 30 dias após o encerramento do bimestre. |
| **Mín. 15%** de impostos+transferências em ASPS (municípios/DF), ou maior por Lei Orgânica | **CONFIRMADO** | LC 141/2012 art. 7º; FAQ SIOPS/MS. |
| Descumprir 15%/não-alimentar SIOPS pode **bloquear/suspender transferências** | **CONFIRMADO** (princípio) | FAQ/SIOPS e LC 141 (condicionante de repasse). Detalhe sancionatório por ente: **PLAUSÍVEL-SEM-FONTE** (obter redação atual). |
| Classificação ASPS×não-ASPS (LC 141 arts. 3º/4º) p/ o `ApuradorAsps` | **PLAUSÍVEL-SEM-FONTE** | Arts. 3º/4º existem; a **tabela operacional vigente** de classificação depende do **Manual SIOPS atual** — obter antes de codar regras de cômputo. |
| **Canal técnico de envio** (importação por arquivo / web service vs. digitação) | **INCERTO/DIVERGENTE** | Não confirmado. Há aplicativo SIOPS de coleta; **não localizado** leiaute oficial de importação. `[obter manual técnico SIOPS]`. |

Fontes: FAQ SIOPS/MS (`gov.br/saude/.../siops/faq`); LC 141/2012 (Planalto).

---

## 2. FNS — fundo a fundo e blocos

| Afirmação | Veredito | Evidência |
|---|---|---|
| Repasse **regular e automático, sem convênio**, direto ao FMS | **CONFIRMADO** | LC 141/2012 **art. 18**: transferência direta fundo a fundo, regular e automática, sem convênio/instrumento congênere. |
| Condicionado a **Conselho de Saúde + Fundo + Plano de Saúde** (e RAG + alimentação dos sistemas) | **CONFIRMADO (e mais completo)** | LC 141 art. 30 §1º (condicionantes): Conselho paritário; Fundo; previsão no Plano/Programação Anual; **apresentação do RAG**; **alimentação regular dos sistemas de informação**. A pesquisa cita 2 dos ~5 — registrar os 5. |
| **Portaria 204/2007** e **3.992/2017**: reorganização em **2 blocos** (Custeio / Investimento) | **CONFIRMADO** | Portaria GM/MS 3.992/2017 (BVSMS): Bloco de Custeio das ASPS e Bloco de Investimento na Rede; substitui os antigos blocos; custeio passa a **conta financeira única**. |
| Vedada transposição livre custeio↔investimento; recurso "carimbado" por bloco | **CONFIRMADO** | 3.992/2017 + LC 141 (aplicação no bloco correspondente, conforme Plano/Programação). |
| Importação de extrato/parcelas do FNS por **API/arquivo** | **INCERTO/DIVERGENTE** | Existência/contrato de API ou arquivo de parcelas por bloco **não confirmado**. `[obter manual de consulta/extração FNS]`. |
| Prestação de contas via **RAG / SARGSUS** | **PLAUSÍVEL-SEM-FONTE** | RAG é exigido (art. 36 LC 141); SARGSUS como ferramenta: conceito correto, **confirmar estado atual** (possível migração para DigiSUS/Gestor). |

Fontes: LC 141/2012 arts. 18, 30, 36 (Planalto); Portaria 3.992/2017 (BVSMS); CONASS/CONASEMS notas técnicas.

---

## 3. RNDS — interoperabilidade FHIR

| Afirmação | Veredito | Evidência |
|---|---|---|
| Plataforma oficial de interoperabilidade; recebe **HL7 FHIR (R4)** | **CONFIRMADO** | Guia RNDS; Manual de Integração do Barramento (DATASUS). |
| Autenticação por **certificado ICP-Brasil A1** (e-CNPJ/e-CPF); **A3 não suportado** p/ API | **CONFIRMADO (A1)** / **PLAUSÍVEL (A3)** | Manual Barramento: `POST@/token` exige certificado A1 e-CNPJ/e-CPF ICP-Brasil. "A3 não suportado" é coerente com mTLS por arquivo, mas **não vi a frase literal "A3 não suportado"** no manual obtido — vem de docs de terceiros (Voa). Marcar a vedação A3 como **PLAUSÍVEL-SEM-FONTE oficial literal**. |
| **`POST /token`** para autenticar | **CONFIRMADO (endpoint) / DIVERGENTE (mecanismo)** | Manual: serviço **`POST@/token`** no componente **EHR Auth**. PORÉM o cert é o **certificado cliente do "Two-way SSL" (mTLS)** — handshake TLS bidirecional, não credencial no corpo. **Implicação de engenharia: configurar client-certificate no HttpClient/handler.** A pesquisa diz "POST /token com certificado A1" sem explicitar o mTLS. |
| Token **válido 30 min** | **INCERTO/DIVERGENTE → CORRIGIR** | Manual Barramento (DATASUS): **access_token com tempo de vida de 15 minutos**. A pesquisa diz 30 min (figura que circula em docs de terceiros). **Usar 15 min** e parametrizar margem de renovação. |
| **CPF como identificador nacional** do cidadão | **CONFIRMADO** | MS oficializou RNDS e adoção do CPF (notícia MS / COSEMS-SP). CNS segue em uso. |
| **CNS no header `Authorization`** em todas as chamadas | **PLAUSÍVEL-SEM-FONTE** | Padrão conhecido (X-Authorization-Server p/ token; CNS do profissional em header próprio), mas **nomes exatos de headers não confirmados** no trecho obtido. `[confirmar no manual vigente]`. |
| Ambientes **homologação + produção** | **CONFIRMADO** | Guia/Manual RNDS. |
| Cenários de envio: **resultado de exame lab. COVID-19 obrigatório** (Portaria 1.792/2020), RAC, Sumário de Alta, imunização | **CONFIRMADO (COVID)** / **PLAUSÍVEL (demais)** | Portaria GM/MS **1.792/2020**: notificação obrigatória de **todo** resultado de teste COVID-19, em **até 24h**, via **RNDS** (Bundle: Resultado de Exame, Condição de Saúde, Diagnóstico Lab. Clínico, Amostra Biológica). RAC/Sumário de Alta existem como cenários — **lista vigente de perfis/Bundles** deve sair do Guia RNDS seção Modelos. |
| Lista completa de perfis/Bundles FHIR é **bloqueador de fidelidade** | **CONFIRMADO (postura correta)** | Não inventar StructureDefinitions; gerar a partir do Guia. |

Fontes: **Manual de Integração do Barramento RNDS** (DATASUS, `SOA-RNDS_ManualIntegracaoBarramento_vSite.pdf`) — trechos literais "POST@/token", "Two-way SSL", "token (access_token) com tempo de vida de 15 minutos"; Portaria 1.792/2020 (notícia MS + Guia RNDS).

---

## 4. e-SUS APS / SISAB / cofinanciamento APS

| Afirmação | Veredito | Evidência |
|---|---|---|
| e-SUS APS (PEC/CDS) + SISAB centraliza; registro da APS no e-SUS ou sistema próprio integrado | **CONFIRMADO** | Documentação SISAB/e-SUS APS (MS). "Exclusivo desde 2017": **PLAUSÍVEL** — o registro é direcionado ao e-SUS/SISAB, mas "exclusivo" merece nuance (sistemas próprios integrados continuam válidos). |
| Envio com **fechamento quadrimestral** p/ financiamento | **INCERTO/DIVERGENTE → CORRIGIR** | **Envio p/ financiamento é MENSAL** (até o **10º dia útil** do mês seguinte ao registro). O **quadrimestre** é a **janela de média dos indicadores** dos **Componentes II (Vínculo) e III (Qualidade)**, não a cadência de remessa. Corrigir a tabela-resumo. |
| **Previne Brasil substituído pela Portaria 3.493/2024**; novo modelo | **CONFIRMADO** | Portaria GM/MS **3.493/2024** (BVSMS): institui nova metodologia do Piso da APS; **extingue o Previne Brasil (Portaria 2.979/2019)**; efeitos financeiros desde **maio/2024**. |
| Modelo em **6 componentes**; classificação por **IED** | **CONFIRMADO** | 3.493/2024: (I) Fixo; (II) Vínculo e Acompanhamento Territorial; (III) Qualidade; (IV) Implantação/manutenção de programas/serviços; (V) Saúde Bucal; (VI) Per capita. IED (vulnerabilidade + porte) como classificador. **Atenção:** a descrição da pesquisa dos componentes I/II difere em rótulos — usar os nomes oficiais acima. |
| Componente de desempenho passa a valer "a partir da parcela 5/12 de 2025"; transição 12 parcelas mai/24→abr/25 | **PLAUSÍVEL-SEM-FONTE** | Há regras de transição e fases de desempenho; **datas/parcelas exatas mudam por nota técnica** — `[obter anexos 3.493/2024 + NTs tripartite vigentes]`. |
| Valores R$/equipe, fórmulas, indicadores são **parametrizáveis (mudam por portaria)** | **CONFIRMADO (postura)** | Correto — nunca hardcodar. |
| **SIAPS** (Sistema de Informação da APS) + novos indicadores | **CONFIRMADO E SUBESTIMADO** | A pesquisa classifica SIAPS como "MÉDIA / mencionado por CONASEMS". Na verdade: **Portaria GM/MS nº 7.639/2025** torna o **SIAPS o sistema de informação para fins de financiamento e adesão a programas da PNAB, substituindo progressivamente o SISAB**; há **calendário oficial 2026** e versão 1.4. **Reposicionar SIAPS como sistema-alvo de financiamento, não SISAB.** |

Fontes: Portaria 3.493/2024 (BVSMS); FAQ Novo Cofinanciamento APS (MS/SAPS); Manual SIAPS (`sisaps.saude.gov.br/sistemas/siaps/docs/manual/`); Portaria 7.639/2025 (calendário SIAPS 2026); CONASEMS (prazo mensal até 10º dia útil).

---

## 5. SI-PNI — imunização

| Afirmação | Veredito | Evidência |
|---|---|---|
| Novo SI-PNI (módulo rotina) **integrado à RNDS desde 01/06/2023**, alinhado ao **RNVe/OMS** | **CONFIRMADO** | Notícia MS jun/2023: módulo rotina disponível a partir de 01/06/2023; integrado à RNDS e RNVe; entrada de dados nas versões web/desktop antigas encerrada em **31/05/2023**. |
| Dose pode ir **direto ao SI-PNI** ou via **sistemas locais integrados**; e-SUS sem integração direta → SISAB→RNDS com **latência ~30 dias** | **PLAUSÍVEL-SEM-FONTE** | Fluxo coerente com notas COSEMS; a latência "~30 dias" não foi confirmada em doc oficial primário. **Nuance importante omitida:** o novo SI-PNI módulo rotina destina-se a **salas de vacina NÃO vinculadas à APS / sem adesão ao e-SUS APS** (ex.: CRIE, privados) — a pesquisa generaliza demais. |
| Cobertura vacinal compõe **indicadores da APS** → impacta repasse indiretamente | **PLAUSÍVEL-SEM-FONTE** | Conceitualmente correto; **quais indicadores vacinais entram no cofinanciamento vigente (3.493/2024)** deve ser confirmado nos anexos. |

Fontes: Notícia MS (novo SI-PNI, jun/2023); apresentação COSEMS-SP RNDS.

---

## 6. CNES

| Afirmação | Veredito | Evidência |
|---|---|---|
| Atualização eletrônica **mínima MENSAL** ou a cada alteração — **arts. 371/372 Portaria de Consolidação nº 01/2017** | **CONFIRMADO** | Portaria de Consolidação nº 1/2017 (BVSMS), arts. 371/372: atualização eletrônica sempre que houver alteração, frequência **mínima mensal**; permite Transmissão Direta; base nacional aceita envio diário; envio sem alteração = "certidão negativa". |
| CNES é **pré-condição de repasse** (equipes no CNES definem APS; base p/ MAC/BPA/AIH/APAC) | **CONFIRMADO** | Equipes cadastradas no CNES condicionam cofinanciamento APS (3.493/2024) e produção MAC. |
| Canal de **importação/integração CNES** (arquivo vs. só app SCNES) | **INCERTO/DIVERGENTE** | Não confirmado leiaute de importação. `[obter manual SCNES/integração]`. |

Fontes: Portaria de Consolidação nº 1/2017 arts. 371/372 (BVSMS/portalsinan); Wiki CNES.

---

## 7. SISREG

| Afirmação | Veredito | Evidência |
|---|---|---|
| Sistema de regulação MS/DATASUS; módulos **Ambulatorial, Internação, APAC** | **PLAUSÍVEL-SEM-FONTE** | Conceito correto (CONASS/Wiki SISREG); **divisão exata dos módulos** na versão vigente (SISREG III) `[confirmar manual]`. |
| Painéis públicos atualizados **~24h** | **PLAUSÍVEL-SEM-FONTE** | Plausível; não confirmado em doc primário. |
| **Existência de API** p/ integração (vs. só web) | **INCERTO/DIVERGENTE** | **Bloqueador**: não confirmado se há API/contrato. `[verificar wiki SISREG / manual III]`. |

---

## 8. HÓRUS / e-SUS AF / BNAFAR

| Afirmação | Veredito | Evidência |
|---|---|---|
| BNAFAR consolida estoque/dispensação (RENAME etc.); transmissão **diária obrigatória** | **CONFIRMADO** | Portaria 5.713/2024 (BVSMS) + FAQ BNAFAR/MS: envio **diário** obrigatório; produtos RENAME/REMUME/RESME e demanda judicial. |
| **Portaria GM/MS 5.713/2024** atualiza modelos da BNAFAR | **CONFIRMADO** (corrigir data) | É de **09/12/2024** (publicada 17/12/2024), não a data implícita na pesquisa. Altera a Portaria de Consolidação nº 1/2017. |
| **Novo e-SUS AF substitui o HÓRUS** | **CONFIRMADO** | 5.713/2024: portaria específica tratará do e-SUS AF, que **substituirá o HÓRUS**. Meios disponibilizados: HÓRUS, e-SUS AF, **web service de envio à BNAFAR**. |
| Apresentação do novo e-SUS AF em **abr/2026** | **PLAUSÍVEL-SEM-FONTE** | Consistente com cronograma; tratar HÓRUS como legado e codar contra BNAFAR/web service + e-SUS AF. |
| Alimentar BNAFAR é **condição p/ CBAF**/ressarcimentos | **PLAUSÍVEL-SEM-FONTE** | Princípio correto; **regra sancionatória vigente** `[confirmar]`. |
| Web service BNAFAR (leiaute/XSD/endpoints) | **INCERTO/DIVERGENTE** | Existe (manual de interoperabilidade BNAFAR no portal MS), mas **XSD/endpoints não obtidos** — bloqueador de fidelidade. |

Fontes: Portaria 5.713/2024 (BVSMS); FAQ BNAFAR/MS; CONASS Informa 213/2024; Manual de Integração do Serviço de Interoperabilidade da BNAFAR (portal MS).

---

## 9. Itens NÃO cobertos pela pesquisa que a auditoria recomenda incluir

1. **SIAPS substituindo SISAB (Portaria 7.639/2025)** — decisão de escopo: monitorar/integrar **SIAPS**, não SISAB, para fins de financiamento APS.
2. **Cadência mensal (10º dia útil) de remessa APS** — calendário oficial SIAPS 2026; modelar como prazo parametrizável distinto da janela quadrimestral de indicadores.
3. **mTLS (Two-way SSL) na RNDS** — requisito de infraestrutura (client-cert no handler HTTP), não só montagem de POST. Reusar A1/Key Vault do M2, mas validar suporte a client-cert + renovação a cada **15 min**.
4. **DigiSUS Gestor / Plano de Saúde / RAG digitais** — onde o RAG e o Plano são submetidos hoje (possível substituto do SARGSUS) — confirmar para o eixo de prestação de contas FMS (liga ao M4).

---

## 10. Três principais RISCOS

**RISCO 1 — Janela móvel regulatória (portarias mudam mais rápido que o sprint).**
SISAB→SIAPS (7.639/2025), Previne→3.493/2024, HÓRUS→e-SUS AF, BNAFAR (5.713/2024) — quatro
domínios em transição simultânea em 2024–2026. Codar fiel a um leiaute "atual" gera dívida
imediata. **Mitigação:** tratar TUDO (sistema-alvo, percentuais, valores R$/equipe, indicadores,
calendários) como **configuração versionada por tenant+vigência**; ACL/Outbox por sistema com
versão de contrato explícita; nunca hardcodar o nome do sistema-destino.

**RISCO 2 — Detalhe técnico de transmissão é a parte mais frágil e a menos confirmada.**
Os mecanismos de ENVIO (RNDS mTLS+15min; web service BNAFAR XSD; importação SIOPS/CNES;
API SISREG) são exatamente onde faltam docs oficiais obtidos. Erro de token (30 vs 15 min),
de header (CNS), ou de mTLS quebra a integração em produção e, por tabela, **bloqueia repasse**
(CNES/SIOPS/BNAFAR são condicionantes). **Mitigação:** não implementar fiel sem o manual
vigente de cada barramento; cobrir homologação RNDS antes de produção; testes de contrato.

**RISCO 3 — Confiar em fonte secundária para número que vira regra.**
A pesquisa adotou "token 30 min" (terceiros) contra os "15 min" do manual oficial DATASUS, e
"fechamento quadrimestral" como se fosse a cadência de remessa (é mensal). São exatamente o
tipo de número que, hardcoded, causa falha silenciosa. **Mitigação:** toda constante temporal/
percentual/financeira deve citar **fonte primária (BVSMS/Planalto/manual MS)**; valores de
terceiros ficam como `[a confirmar]` até validação no doc oficial; parametrizar por vigência.

---

## Fontes primárias consultadas nesta verificação

- LC 141/2012 (Planalto) — arts. 7º, 18, 30, 36.
- Portaria GM/MS 3.992/2017 (BVSMS) — 2 blocos Custeio/Investimento.
- Portaria GM/MS 3.493/2024 (BVSMS) — novo cofinanciamento APS; extinção Previne.
- Portaria GM/MS 5.713/2024 (BVSMS, 09/12/2024) — BNAFAR; e-SUS AF substitui HÓRUS.
- Portaria de Consolidação nº 1/2017 (BVSMS) — CNES arts. 371/372.
- Portaria GM/MS 1.792/2020 — notificação obrigatória COVID-19 via RNDS (24h).
- Portaria GM/MS 7.639/2025 — SIAPS substitui SISAB (calendário 2026).
- Manual de Integração do Barramento RNDS (DATASUS) — `POST@/token`, Two-way SSL, token 15 min.
- FAQ SIOPS/MS; FAQ Novo Cofinanciamento APS/SAPS; FAQ BNAFAR/MS; Manual SIAPS (sisaps).
- Notícia MS jun/2023 (novo SI-PNI/RNDS); CONASS Informa 213/2024.
