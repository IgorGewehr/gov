# Regras — ISS (apuração sobre NFS-e ingeridas do ADN)

<!-- manifest
commands: ConfigurarTabelaAliquotaIss, ApurarIssMensal, EntregarGiaIss
queries: 
domainEvents: TabelaAliquotaIssCriada, TabelaAliquotaIssPublicada, ApuracaoIssEncerrada, DeclaracaoGiaIssEntregue
integrationEventsPublished: 
integrationEventsConsumed: 
-->

> Bounded Context: **Tributos** — apuração do **ISS** a partir das NFS-e já **INGERIDAS** do Ambiente
> de Dados Nacional (ADN). **NÃO emitimos nem assinamos NFS-e** (integração PASSIVA — ADR-0003 /
> CLAUDE.md §8). Base legal: **LC 116/2003** + Código Tributário Municipal. Ver M6-DESIGN §2.

---

## 1. Linguagem ubíqua

- **NFS-e (`NotaFiscalServico`)** — read model fiscal ingerido do ADN; carrega item da lista LC 116,
  indicador de retenção, município de incidência e situação (normal/cancelada/substituída).
- **Tabela de alíquotas do ISS (`TabelaAliquotaIss`)** — alíquota por item da lista LC 116, indicação
  de retenção obrigatória e substituição tributária; **lei municipal**, versionada por vigência.
- **Apuração mensal (`ApuracaoIss`)** — livro/escrituração eletrônica do ISS por contribuinte/competência:
  consolida ISS próprio, retido e por substituição derivado das notas vigentes.
- **Modalidade do ISS** — próprio (prestador local), retido na fonte (tomador), substituição tributária.

---

## 2. Comandos (escrita)

- **`ConfigurarTabelaAliquotaIssCommand`** → cria a tabela de alíquotas do ISS por item LC 116 (lei
  municipal), opcionalmente publica. Retorna o `Guid` da tabela.
- **`ApurarIssMensalCommand`** → apura o ISS mensal de um contribuinte (prestador) a partir das NFS-e
  vigentes ingeridas: classifica próprio/retido/substituição pela tabela vigente, escritura o livro
  (`ApuracaoIss`) e LANÇA o ISS próprio (`Lancamento` `TipoTributo.Iss`). Retorna o resumo da apuração.

> Nenhuma alíquota é hardcoded — todas vêm da tabela municipal vigente na competência.

---

## 3. Eventos

- **Domínio (in-process):**
  - `TabelaAliquotaIssCriada(TabelaAliquotaIssId, TenantId, VigenciaInicioAaaaMm)`
  - `TabelaAliquotaIssPublicada(TabelaAliquotaIssId, TenantId, VigenciaInicioAaaaMm)`
  - `ApuracaoIssEncerrada(ApuracaoIssId, TenantId, ContribuinteId, IssProprio)`
- **Integração:** nenhum publicado/consumido nesta versão.

---

## 4. Pendências `// TODO(validar-oficial)`

- Alíquotas por item LC 116, hipóteses de retenção e lista de substitutos conforme o **CTM de
  Maximiliano de Almeida/RS**.
- Campo exato do item da lista, indicador de retenção e município de incidência no **XSD da NFS-e
  nacional** + códigos dos **eventos** (cancelamento/substituição) no manual do ADN.
