# Plano de Contratações Anual (PCA) — Rules-as-Code

Bounded Context: **Administracao**. Consolida, por exercício, as contratações pretendidas do ente,
para subsidiar a lei orçamentária e a governança das aquisições (art. 12, VII, Lei 14.133/2021;
Dec. 11.246/2022). Construído em elaboração, aprovado pela autoridade e publicado no PNCP.

## Invariantes
- I-1: um PCA por exercício/tenant (`(TenantId, Exercicio)` único); exercício plausível (2000-2100).
- I-2: plano nasce **EmElaboracao**; itens só podem ser incluídos/removidos nesse estado.
- I-3: item vincula item de catálogo **Ativo**, quantidade > 0, valor estimado > 0 e trimestre desejado (1..4).
- I-4: o mesmo item de catálogo não consta duas vezes no plano.
- I-5: aprovação exige ao menos um item e congela os itens (EmElaboracao → Aprovado).
- I-6: publicação no PNCP exige plano Aprovado (Aprovado → Publicado), com número de controle.

## Fluxo
Abrir → (IncluirItem | RemoverItem)* → Aprovar → PublicarNoPncp.

<!-- manifest
commands: AbrirPca, IncluirItemPca, RemoverItemPca, AprovarPca, PublicarPcaNoPncp
queries: ObterPcaPorExercicio
domainEvents: 
integrationEventsPublished: 
integrationEventsConsumed: 
-->
