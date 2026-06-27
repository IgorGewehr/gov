# Credenciamento — Rules-as-Code

Bounded Context: **Administracao**. Credenciamento é forma auxiliar de contratação (Lei 14.133/2021,
art. 78, I) processada por **inexigibilidade de licitação** (art. 74, IV): como todos os interessados
que satisfaçam as condições do edital podem ser contratados, inexiste competição. O chamamento público
é **permanentemente aberto** (art. 79, parágrafo único), admitindo ingresso a qualquer tempo enquanto
vigente o edital.

## Hipóteses autorizadoras (art. 79, I a III)
- **ParalelaNaoExcludente** (I): contratação simultânea de todos os credenciados, sem disputa.
- **SelecaoCriterioBeneficiario** (II): escolha do credenciado pelo beneficiário direto do serviço.
- **MercadosFluidos** (III): flutuação constante de valor/condições inviabiliza a competição por preço.

## Invariantes
- I-1: edital nasce **EmElaboracao**; itens/condições só são incluídos/removidos nesse estado.
- I-2: a hipótese autorizadora (art. 79, I a III) é obrigatória e imutável após a abertura.
- I-3: publicação do chamamento (EmElaboracao → ChamamentoAberto) exige número de edital e ao menos um item.
- I-4: inscrições de interessados só são recebidas com o chamamento **ChamamentoAberto** (permanente).
- I-5: inscrição nasce **EmAnalise**; deferimento (→ Credenciado) exige habilitação documental; indeferimento
  (→ Indeferido) exige motivo; ambos são atos terminais da análise daquela inscrição.
- I-6: descredenciamento (→ Descredenciado) é terminal e exige motivo (pedido próprio, descumprimento ou sanção).
- I-7: encerramento/anulação/revogação do edital (→ Encerrado | Anulado | Revogado) é terminal e bloqueia
  novas inscrições; anulação exige ilegalidade e revogação exige conveniência/oportunidade motivadas.
- I-8: todo ato (abertura, publicação, inscrição, deferimento, descredenciamento, encerramento) é
  multi-tenant e auditado, preservando a trilha imutável para o Tribunal de Contas.

## Fluxo
Abrir → (AdicionarItem | RemoverItem)* → PublicarChamamento → (AlterarChamamento)?
→ (InscreverInteressado → (DeferirInscricao | IndeferirInscricao) → (AlterarCredenciado | Descredenciar)?)*
→ EncerrarCredenciamento.

<!-- manifest
commands: AbrirCredenciamento, AdicionarItemCredenciamento, RemoverItemCredenciamento, PublicarChamamento, AlterarChamamento, EncerrarCredenciamento, InscreverInteressado, DeferirInscricao, IndeferirInscricao, AlterarCredenciado, Descredenciar
queries: ObterCredenciamentoPorId, ListarCredenciamentos
domainEvents: CredenciamentoAberto, ChamamentoCredenciamentoPublicado, InteressadoInscrito, InteressadoCredenciado, InteressadoDescredenciado, CredenciamentoEncerrado
integrationEventsPublished: 
integrationEventsConsumed: 
-->
