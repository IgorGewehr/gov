# Catálogo de materiais e serviços (CATMAT/CATSER) — Rules-as-Code

Bounded Context: **Administracao**. Item padronizado base das compras e contratações
(Lei 14.133/2021): código único por tenant, natureza (material/serviço), unidade de
fornecimento e classe. Substitui descrições livres por itens identificáveis, evitando
duplicidade e habilitando comparação de preços (insumo de Atas/ARP e do PCA).

## Invariantes
- I-1: código, descrição e unidade de fornecimento são obrigatórios.
- I-2: natureza deve ser valor válido (Material | Servico).
- I-3: código é único por tenant (`(TenantId, Codigo)`), garantido por handler + índice único.
- I-4: item nasce **Ativo**; item **Inativo** não pode ser atualizado nem usado em novas contratações.

## Fluxo
Cadastrar → (Atualizar)* → Inativar ↔ Reativar.

<!-- manifest
commands: CadastrarItemCatalogo, AtualizarItemCatalogo, InativarItemCatalogo, ReativarItemCatalogo
queries: ListarItensCatalogo
domainEvents: 
integrationEventsPublished: 
integrationEventsConsumed: 
-->
