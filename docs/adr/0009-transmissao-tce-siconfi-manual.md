# ADR-0009 — Transmissão TCE-RS / SICONFI é MANUAL (geramos artefatos + reconciliamos; sem API de envio)

- **Status:** Aceito (corrige a premissa de `EnviarRemessaTce` via POST)
- **Data:** 2026-06-22
- **Fonte:** design e verificações adversariais consolidados neste ADR.

## Contexto

O esqueleto inicial de Transparência assumia que a remessa ao TCE-RS e ao SICONFI seria
**transmitida por API** (`EnviarRemessaTce` fazia um `POST`; `ISiconfiGateway.TransmitirAsync`
retornava protocolo). A pesquisa + verificação adversarial das fontes oficiais **refutou essa
premissa**:

- O **TCE-RS não tem web service público de envio**. A transmissão é feita no **PAD desktop**
  operado por um servidor humano, com assinatura do RVE/RDI no **Processo Eletrônico
  (e-Protocolo)** usando **certificado pessoal ICP-Brasil** do responsável.
- O **SICONFI** só tem **API de consulta** (Dados Abertos), **não** de upload. A MSC é enviada
  por **upload manual** no portal e homologada com **e-CPF A3 em token** do gestor.
- O leiaute SIAPC/PAD conferido é de **2010** com **mudança anunciada para 2026** — nenhuma
  grade de campo pode ser congelada sem o material oficial vigente.

## Decisão

O ERP **gera artefatos e reconcilia; não transmite por API**. O valor automatizável é:

1. **Gerar** os `.TXT` posicionais do SIAPC no leiaute exato (ISO-8859-1) + **ZIP nomeado**.
2. **Pré-validar localmente** as críticas que bloqueiam (e-Validador / RDI).
3. **Empacotar** (ZIP + hash + artefato de auditoria).
4. **Gerar a MSC** (CSV/XBRL-GL zipada) para o SICONFI.
5. **Reconciliar** via a API de **consulta** (Dados Abertos) do SICONFI.
6. **Registrar recibo/protocolo** retornado pelo ato humano como artefato de auditoria.

O passo de **envio** e a **assinatura do RVE/RDI / homologação** são **ato humano documentado**
no portal (certificado **pessoal**, não o A1 institucional). O `EnviarRemessaTce` (POST) é
**removido/ressignificado** para "empacotar + registrar protocolo + reconciliar".

## Alternativas consideradas

- **Implementar transmissão por API (premissa original):** **impossível** — não existe endpoint
  público de upload no TCE-RS nem no SICONFI. Rejeitada por refutação factual.
- **Automação de UI (RPA/headless no portal):** frágil, contra os termos de uso e arriscado para
  ato com efeito legal que exige certificado pessoal do responsável. Rejeitada.
- **Congelar o leiaute 2010 e ajustar depois:** retrabalho garantido — o MT 2026 muda
  Balanço Patrimonial/Financeiro + DFC. Em vez disso, modelamos fiéis ao conceito e marcamos cada
  campo exato com `// TODO(validar-leiaute-oficial)` para troca cirúrgica.

## Consequências

- ➕ Arquitetura honesta com a realidade regulatória — sem prometer integração que não existe.
- ➕ O esforço vai para onde há valor real: **gerar artefatos corretos** e **reconciliar**, com a
  garantia de auditoria (hash, protocolo).
- ➕ A separação "ato humano com certificado pessoal" respeita a responsabilização legal do gestor.
- ➖ **Não há "um clique e transmitiu"** — o fechamento depende de um servidor operando o portal.
  É uma **divergência consciente** de roadmaps govtech que vendem transmissão automática.
- ➖ Reconciliação depende da API de consulta do SICONFI (disponibilidade) — atrás de Polly/ACL,
  idempotente, via Outbox.
- ➖ Pendências de fonte oficial **bloqueiam** o congelamento: MT SIAPC 2026, Tabela do PAD 2026,
  Regras Gerais MSC 2026, Manual de certificação do SICONFI.
- 🔗 Reforça ADR-0008: o A1 institucional **não** homologa no SICONFI (A3 pessoal) nem assina o
  RVE/RDI no e-Protocolo — cobre só o que assinamos server-side.
