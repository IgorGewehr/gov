---
modulo: RecursosHumanos
agregado: RemessaSicapPessoal
contexto: RecursosHumanos (remessa de atos de pessoal ao TCE-RS — SIAPES/SICAP)
poder: Ambos
schema: recursoshumanos
ativavel_por_tenant: true
versao_regras: 1.0.0
fontes_legais: ["TCE-RS — SIAPC/SICAP módulo de Pessoal (SIAPES): leiaute de atos de pessoal", "CF/1988 art. 71 (controle externo pelo Tribunal de Contas)", "LC estadual e Resoluções TCE-RS (obrigatoriedade de remessa de atos de admissão/pessoal)"]
---

<!-- manifest
commands: AbrirRemessaSicap, AdicionarAtoDeServidor, GerarRemessaSicap, TransmitirRemessaSicap
queries: ListarRemessasSicap, ObterRemessaSicap
domainEvents: RemessaSicapAberta, RemessaSicapGerada
integrationEventsPublished:
integrationEventsConsumed:
-->

# Remessa SICAP Pessoal (TCE-RS / SIAPES) — Regras-as-Code

> **Remessa SICAP Pessoal** consolida os **atos de pessoal** (admissões e movimentos) de um órgão num
> **lote sequencial** para envio ao **Tribunal de Contas do RS** (módulo de auditoria de pessoal —
> SIAPES). A remessa é **aberta** para um órgão/sequencial, recebe os **atos de servidor**, é **gerada**
> (materializa o arquivo posicional conforme o leiaute) e então **transmitida**. Códigos de regime
> jurídico, motivo de extinção e movimento seguem as **tabelas oficiais do leiaute** (sem número mágico).
> Este arquivo é **normativo e versionado**.
>
> ⚠️ `// TODO(validar-oficial)`: o **leiaute posicional exato** (tamanho/ordem dos campos, tabelas de
> domínio e regras de validação do SIAPES) deve ser confirmado contra a documentação vigente do TCE-RS
> antes do go-live (M10).

---

## 1. Linguagem Ubíqua

| Termo (identificador-no-código) | Definição |
|---|---|
| Remessa (`RemessaSicapPessoal`) | Lote sequencial de atos de pessoal de um órgão. Raiz de agregado. |
| Ato de admissão (`AtoAdmissaoSicap`) | Ato de pessoal de um servidor incluído na remessa. Entidade. |
| Sequencial de lote (`SequencialLote`) | Número do lote por órgão/exercício, monotônico. |
| Regime jurídico (`RegimeJuridicoSiapes`) | Código (Tabela 5) do vínculo: Celetista/Estatutário/Administrativo. |
| Movimento (`MovimentoSiapes`) | Operação do ato (Inserção, no escopo PoC) — Tabela de movimentos. |

---

## 2. Invariantes (Rules-as-Code)

- **I-1 — Remessa nasce aberta.** A abertura fixa órgão e sequencial e emite `RemessaSicapAberta`.
- **I-2 — Atos só em remessa aberta.** Adicionar ato a remessa já gerada/transmitida falha.
- **I-3 — Geração exige conteúdo.** Gerar remessa sem ao menos um ato falha; a geração emite
  `RemessaSicapGerada` com a quantidade de atos consolidados.
- **I-4 — Transmissão é terminal.** Após transmitida, a remessa não recebe novos atos nem é regerada.
- **I-5 — Códigos pela tabela oficial.** Regime, movimento e motivo de extinção usam os códigos do
  leiaute (enums com valor = código oficial), nunca constantes soltas no fluxo.

---

## 3. Cenários BDD (resumo)

1. **Abrir remessa** → fixa órgão/sequencial; emite `RemessaSicapAberta`.
2. **Adicionar ato** → inclui ato de servidor na remessa aberta; em remessa gerada **falha** (I-2).
3. **Gerar remessa** → materializa o arquivo; emite `RemessaSicapGerada`; sem atos **falha** (I-3).
4. **Transmitir remessa** → marca como transmitida; novo ato/regeração **falha** (I-4).

> Cobertura em `tests/Tensorroot.Gov.Modules.RecursosHumanos.Tests/`.
