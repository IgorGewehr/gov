# MAPA DE PARIDADE — Tensorroot.Gov × Betha (escopo-alvo)

> Consolidação de `docs/planejamento/BENCHMARK-BETHA.md` + `docs/diagnostico/ESTADO-ATUAL.md`.
> Lista COMPLETA de módulos/sub-módulos necessários para competir com a Betha Sistemas.
> Legenda estado: ✅ **temos** (real e exposto) · 🟡 **parcial** (domínio existe / API ou substância incompleta) · 🔴 **falta** (ausente).
> Esforço relativo (a partir do que já existe): **P** pequeno · **M** médio · **G** grande.
> Datas-base das fontes: ESTADO-ATUAL 2026-06-22; BENCHMARK-BETHA escopo-alvo.

---

## Eixo 1 — Contábil / Financeiro (prioridade nº1 do dono)

Módulo nosso: **Financas** (+ Transparencia para prestação de contas).

| Sub-módulo / Capacidade alvo | Produto Betha | Estado | Esforço | Observação (estado-atual) |
|---|---|---|---|---|
| Execução da despesa (Empenho→Liquidação→Pagamento) | Contábil | 🟡 | M | Domínio Lei 4.320 forte; só 4 de 22 handlers expostos via HTTP |
| Receita arrecadada / Restos a Pagar | Contábil | 🟡 | M | Existe `ReceitaArrecadada`, `RestoAPagar`; sem API completa |
| **Contabilidade PCASP/MCASP** (plano de contas, partida dobrada, evento contábil, balancete, lançamento automático) | Contábil | 🔴 | **G** | **0 linhas.** A FONTE de toda prestação de contas. Prioridade nº2 absoluta |
| **Planejamento PPA / LDO / LOA** (programas, metas, anexos) | Planejamento | 🔴 | **G** | Só existe `DotacaoOrcamentaria` (execução), não o planejamento |
| Tesouraria / Controle de Caixa / Conciliação bancária | Tesouraria, Controle de Caixa | 🔴 | M | CNAB 240 gerado (arquivo), sem conciliação nem caixa |
| **Prestação de Contas TCE-RS (SIAPC/PAD)** — leiaute oficial + e-Validador + transmissão | Prestação de Contas | 🔴 | **G** | Máquina de estados real, mas leiaute fake (3 linhas hardcoded); e-Validador e transmissão são stub |
| **Prestação de Contas União (SICONFI / MSC)** | Prestação de Contas | 🔴 | **G** | `MatrizSaldos` é read model órfão (0 publishers); pipeline nunca dispara |
| Controladoria / Controle Interno | Controladoria, Controle interno | 🔴 | M | Inexistente |
| Convênios | Convênios | 🔴 | M | Inexistente |
| Terceiro Setor | Terceiro Setor | 🔴 | M | Inexistente |
| Assinatura A1 (XMLDSig) + Azure Key Vault em runtime | (transversal) | 🔴 | M | Pré-requisito de TCE/eSocial; pacotes+Bicep prontos, integração não fiada |

---

## Eixo 2 — Suprimentos / Patrimônio / Contratos

Módulos nossos: **Administracao** + **Patrimonio**.

| Sub-módulo / Capacidade alvo | Produto Betha | Estado | Esforço | Observação |
|---|---|---|---|---|
| Compras / Licitações (Lei 14.133) / Contratos / Aditivos / Fornecedores | Compras, Contratos | ✅ | P | Maduro; saga orçamento↔contrato real. PNCP/Receita são placeholders |
| Integração **PNCP** (transmissão real) | Compras | 🟡 | M | Só grava nº + emite evento, sem cliente HTTP |
| Patrimônio / Tombamento / Depreciação MCASP | Patrimônio | ✅ | P | O módulo mais completo; falta contrapartida contábil em Finanças |
| **Almoxarifado** dedicado (gestão de estoque/requisições) | Almoxarifado | 🟡 | M | `ItemEstoque` + PEPS/Médio existem dentro de Patrimônio; sem módulo/fluxo dedicado |
| **Monitor DF-e** (captura automática de NF-e de fornecedores) | Monitor DF-e | 🔴 | M | Inexistente |
| **Frotas** completa (abastecimento, manutenção, multas, CNH) | Frotas | 🟡 | M | `Veiculo` existe em Patrimônio; falta ciclo de frota completo |
| **Obras / ObrasPro** (medições, fiscalização, cronograma físico-financeiro) | Obras, ObrasPro | 🔴 | **G** | Inexistente |
| Apps mobile (Almoxarifado, Patrimônio, Frotas) | +App | 🔴 | **G** | Camada mobile inexistente |

---

## Eixo 3 — Arrecadação / Tributos

Módulo nosso: **Tributos**.

| Sub-módulo / Capacidade alvo | Produto Betha | Estado | Esforço | Observação |
|---|---|---|---|---|
| Dívida Ativa / CDA / prescrição / cobrança | Tributos | ✅ | P | Maduro |
| Ingestão NFS-e via ADN (passiva) | eNota (parcial) | ✅ | P | Única integração REAL de produção (HTTP+Polly) |
| **Motor IPTU** (apuração/lançamento) | Tributos | 🔴 | **G** | Sem motor de cálculo |
| **Motor ISS / eNota / Livro Eletrônico** | eNota, Livro Eletrônico | 🔴 | **G** | Ingestão existe; apuração/livro não |
| **Motor ITBI** | Tributos | 🔴 | M | Sem motor |
| **Taxas e Alvarás** (apuração) | Alvarás | 🔴 | M | Sem motor; Alvará é sub-módulo próprio na Betha |
| **Cadastro Imobiliário** (base do IPTU) | Cadastro Imobiliário | 🔴 | **G** | Base cadastral inexistente — bloqueia IPTU |
| **Alvarás** (emissão/licenciamento de funcionamento) | Alvarás | 🔴 | M | Inexistente |
| **Meio Ambiente** (licenciamento ambiental) | Meio Ambiente | 🔴 | M | Inexistente |
| **Planejamento Urbano** | Planejamento Urbano | 🔴 | M | Inexistente |
| **Cemitério** (gestão de jazigos/sepultamentos) | Cemitério | 🔴 | M | Inexistente |
| **Procuradoria** (execução fiscal / protesto da dívida) | Procuradoria | 🔴 | M | Inexistente |
| Gestão Fiscal / Fatura | Gestão Fiscal, Fatura | 🔴 | M | Inexistente |

---

## Eixo 4 — Pessoal / RH

Módulo nosso: **RecursosHumanos**.

| Sub-módulo / Capacidade alvo | Produto Betha | Estado | Esforço | Observação |
|---|---|---|---|---|
| Cadastro de servidores / cargos / plano de cargos | Recursos Humanos | ✅ | P | Maduro; ciclo estatutário completo |
| Folha de pagamento (estrutura + abate-teto) | Folha | 🟡 | M | `FolhaPagamento`/`EventoFolha` existem; sem motor de cálculo |
| **Motor de cálculo de folha** (INSS / RPPS / IRRF / consignados) | Folha | 🔴 | **G** | Inexistente |
| **Ponto** (AFD/AEJ, Portaria MTP 671/2021) | Ponto, Pontual | 🔴 | **G** | Inexistente |
| **eSocial** real (eventos S-1000/1200/2200, transmissão) | eSocial | 🔴 | **G** | Só valida rubrica S-1010 local; sem envio nem demais eventos |
| **Minha Folha** (self-service do servidor) | Minha Folha | 🔴 | M | Inexistente |

---

## Eixo 5 — Atendimento / Cidadão

Módulos nossos: **Protocolo** + **Transparencia**.

| Sub-módulo / Capacidade alvo | Produto Betha | Estado | Esforço | Observação |
|---|---|---|---|---|
| Protocolo / Processo administrativo eletrônico (NUP/CONARQ) | Protocolo | ✅ | P | Domínio rico; carimbo de tempo é relógio local (sem ACT ICP) |
| Transparência / Dados abertos (LAI) | Transparência, Cidadão Web | 🟡 | M | Andaime real; conteúdo depende do pipeline contábil |
| **Portal do Cidadão / Minha Cidade** (serviços online, 2ª via, agendamento) | Portal Minha Cidade, App Minha Cidade | 🔴 | **G** | Camada voltada ao cidadão INEXISTENTE |
| Apps mobile do cidadão | App Minha Cidade | 🔴 | **G** | Inexistente |
| Assistente / chatbot | BETH | 🔴 | M | Inexistente |

---

## Eixo 6 — NoPaper / GED

Módulo nosso: **Protocolo/Documento**.

| Sub-módulo / Capacidade alvo | Produto Betha | Estado | Esforço | Observação |
|---|---|---|---|---|
| GED básico (documento, assinatura, hash) | Documentos | 🟡 | M | Existe metadado; GED de binário (armazenamento) ausente |
| GED completo (temporalidade CONARQ, assinatura em lote) | Documentos | 🔴 | M | Temporalidade e lote ausentes |
| **Conecta** (interoperabilidade/integração entre órgãos) | Conecta | 🔴 | **G** | Inexistente |

---

## Eixo 7 — Educação

Módulo nosso: **Educacao**.

| Sub-módulo / Capacidade alvo | Produto Betha | Estado | Esforço | Observação |
|---|---|---|---|---|
| Escolas / Matrículas / Diário de Classe | Educação | ✅ | P | Domínio rico |
| **Transporte Escolar (PNATE)** | Transporte Escolar | 🔴 | M | Ausente |
| **Merenda Escolar (PNAE)** | Merenda Escolar | 🔴 | M | Ausente |
| Portal/App **Professores** | Professores +App | 🔴 | **G** | Ausente |
| Portal/App **Pais e Alunos** | Pais e Alunos | 🔴 | **G** | Ausente |
| Biblioteca | Biblioteca | 🔴 | M | Ausente |
| **EducaCenso / INEP** (envio real) | SADI | 🔴 | M | Só armazena local; sem envio |

---

## Eixo 8 — Saúde

Módulo nosso: **Saude**.

| Sub-módulo / Capacidade alvo | Produto Betha | Estado | Esforço | Observação |
|---|---|---|---|---|
| PEP / Paciente / Atendimento (SOAP/CID/CIAP) / Regulação | Saúde | ✅ | P | Domínio rico; toda integração externa é `Simulado*` |
| **Farmácia (HÓRUS)** | Saúde | 🔴 | **G** | Ausente |
| **Imunização (SI-PNI)** | Saúde | 🔴 | M | Ausente |
| **Vigilância Sanitária** | Vigilância Sanitária | 🔴 | M | Ausente |
| Integração **RNDS / SISAB / CNES / SISREG / CADSUS** (real) | Saúde | 🔴 | **G** | Todas stub |
| App Saúde Domiciliar (agentes) | APP Saúde Domiciliar | 🔴 | **G** | Ausente |

---

## Eixo 9 — Assistência Social

Módulo nosso: **AssistenciaSocial**.

| Sub-módulo / Capacidade alvo | Produto Betha | Estado | Esforço | Observação |
|---|---|---|---|---|
| SUAS / Família / Benefícios / Prontuário (trilha LGPD) | Social | ✅ | P | Domínio rico, parametrização por vigência |
| Integração **CadÚnico / MDS** (real) | Social | 🔴 | M | Só leitura simulada |

---

## Eixo 10 — Legislativo (sem concorrente direto no recorte Betha)

Módulo nosso: **Legislativo**.

| Sub-módulo / Capacidade alvo | Estado | Esforço | Observação |
|---|---|---|---|
| Sessões / Votação / Painel / Proposições | ✅ | P | Maior cobertura de casos de uso; sem integração externa (correto) |

---

## Eixo 11 — Gestão / BI (transversal, diferencial de produto Betha)

| Capacidade alvo | Produto Betha | Estado | Esforço | Observação |
|---|---|---|---|---|
| **Portal do Gestor** (painel executivo prefeito/secretários) | Portal do Gestor | 🔴 | **G** | Inexistente; Betha empacota em TODA linha |
| **BI por área** (Contábil, Saúde, Educação, Administrativo) | BI / BI Saúde / BI Educação | 🔴 | **G** | Inexistente |
| Planejamento Estratégico | Planejamento Estratégico | 🔴 | M | Inexistente |

---

## Eixo 12 — Plataforma / Identidade (base, não compete diretamente mas habilita tudo)

| Capacidade | Estado | Esforço | Observação |
|---|---|---|---|
| Multi-tenant (DB-per-tenant, query filter, gating de licença) | ✅ | P | Sólido; bug crítico: ordem de interceptors zera `TenantId` em INSERTs (corrigir) |
| JWT / RBAC negar-por-padrão / BCrypt | ✅ | P | Maduro; `/admin/tenants` sem RBAC (corrigir) |
| Auditoria imutável + trilha de leitura LGPD | 🟡 | M | AuditTrail não-imutável de fato; sem trilha de leitura |
| Apps mobile (servidor/gestor/cidadão/agentes/professores) | 🔴 | **G** | Camada mobile inexistente em todo o sistema |

---

## Síntese de paridade (contagem)

- **✅ temos (núcleo competitivo):** Administração/Licitação, Patrimônio, RH (cadastro), Saúde (PEP), Educação (escolar), Assistência Social, Protocolo, Legislativo, ingestão NFS-e/ADN, Dívida Ativa, plataforma multi-tenant/segurança. — base de domínio forte.
- **🟡 parcial:** Execução da despesa/Finanças (API incompleta), Folha (sem motor), Almoxarifado/Frotas (dentro de Patrimônio), GED, Transparência, PNCP, Auditoria LGPD.
- **🔴 falta (maiores lacunas competitivas):**
  1. **Contabilidade PCASP + PPA/LDO/LOA + Prestação de Contas TCE/SICONFI** (G) — prioridade nº1 do dono.
  2. **Camada do cidadão** (Portal/App Minha Cidade) (G) — inexistente.
  3. **Portal do Gestor + BI** por área (G) — diferencial Betha, inexistente.
  4. **Sub-módulos de receita:** Cadastro Imobiliário, Alvarás, Meio Ambiente, Cemitério, Procuradoria, motores IPTU/ISS/ITBI/Taxas (G/M).
  5. **RH completo:** motor de folha, Ponto, eSocial, Minha Folha (G).
  6. **Setoriais:** Saúde (Farmácia/Imunização/Vigilância/RNDS), Educação (PNAE/PNATE/portais) (G/M).
  7. **Suprimentos:** Almoxarifado dedicado, Monitor DF-e, Frotas, Obras/ObrasPro (G/M).
  8. **GED/Conecta** e **apps mobile** transversais (G).
