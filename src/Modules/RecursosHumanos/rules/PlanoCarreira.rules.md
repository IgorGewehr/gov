---
modulo: RecursosHumanos
agregado: PlanoCarreira
contexto: RecursosHumanos (plano de cargos, carreiras e salários — PCCS)
poder: Ambos
schema: recursoshumanos
ativavel_por_tenant: true
versao_regras: 1.0.0
fontes_legais: ["CF/1988 art. 39 §1 (planos de carreira do serviço público)", "CF/1988 art. 37, II/V (provimento por concurso; carreira)", "Lei 8.112/1990 art. 10 (progressão/promoção como formas de desenvolvimento na carreira — referência federal)", "Lei municipal do PCCS (parametrizável por tenant)"]
---

<!-- manifest
commands: InstituirPlanoCarreira, EnquadrarServidor, ConcederProgressao, ConcederPromocao
queries: ListarPlanosCarreira, ObterPlanoCarreira, ObterEnquadramentoDoServidor
domainEvents: PlanoCarreiraInstituido, ServidorEnquadrado, ProgressaoConcedida, PromocaoConcedida
integrationEventsPublished:
integrationEventsConsumed:
-->

# Plano de Carreira (PCCS) — Regras-as-Code

> **Plano de Carreira** institui a estrutura de **classes** e **referências** (a matriz salarial) de uma
> carreira do serviço público e governa a **posição** de cada servidor nessa matriz. O servidor é
> **enquadrado** numa posição inicial e evolui por **progressão** (horizontal — avanço de referência na
> mesma classe) e **promoção** (vertical — avanço de classe, retornando à referência inicial da nova
> classe). Os parâmetros (passos, interstícios, percentuais) são **lei municipal** e portanto
> **parametrizáveis por tenant** — nunca *hardcoded*. Este arquivo é **normativo e versionado**.
>
> ⚠️ `// TODO(validar-oficial)`: a **estrutura exata** (nº de classes/referências, interstícios mínimos e
> critérios de avaliação) deve ser confirmada contra o **PCCS vigente** do tenant antes do go-live (M10).

---

## 1. Linguagem Ubíqua

| Termo (identificador-no-código) | Definição |
|---|---|
| Plano de carreira (`PlanoCarreira`) | Estrutura de classes/referências de uma carreira. Raiz de agregado. |
| Posição (`PosicaoCarreira`) | Par (Classe, Referência) com o vencimento correspondente. VO. |
| Enquadramento (`EnquadramentoServidor`) | Vínculo do servidor a uma posição vigente. Raiz de agregado. |
| Movimentação (`MovimentacaoCarreira`) | Registro histórico de progressão/promoção do servidor. Entidade. |
| Progressão (`ProgressaoConcedida`) | Avanço **horizontal** de referência na mesma classe. |
| Promoção (`PromocaoConcedida`) | Avanço **vertical** de classe, voltando à referência inicial. |

---

## 2. Invariantes (Rules-as-Code)

- **I-1 — Posições únicas e contínuas.** Um plano não admite duas posições com o mesmo par
  (Classe, Referência); o vencimento é estritamente positivo.
- **I-2 — Enquadramento único por servidor.** Cada servidor possui **no máximo um** enquadramento vigente
  no tenant (unicidade por servidor/tenant).
- **I-3 — Movimentação só dentro do plano.** Progressão/promoção só destinam a uma posição **existente**
  no plano do enquadramento; destino fora da matriz falha.
- **I-4 — Progressão é horizontal.** Mantém a classe e avança a referência; nunca retrocede.
- **I-5 — Promoção é vertical.** Avança a classe e reposiciona na referência inicial da nova classe.
- **I-6 — Histórico imutável.** Cada movimentação preserva origem→destino, data e fundamento; o histórico
  não é reescrito (auditoria para o Tribunal de Contas).

---

## 3. Cenários BDD (resumo)

1. **Instituir plano** → nasce com a matriz de posições; emite `PlanoCarreiraInstituido`.
2. **Enquadrar servidor** → posição inicial vigente; emite `ServidorEnquadrado`; reenquadrar falha (I-2).
3. **Conceder progressão** → referência avança na mesma classe; emite `ProgressaoConcedida`.
4. **Conceder promoção** → classe avança e referência volta à inicial; emite `PromocaoConcedida`.
5. **Destino inexistente** → progressão/promoção para posição fora da matriz **falha** (I-3).

> Cobertura em `tests/Tensorroot.Gov.Modules.RecursosHumanos.Tests/`.
