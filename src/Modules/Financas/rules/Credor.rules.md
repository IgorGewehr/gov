# Credores / Fornecedores (beneficiários) — Rules-as-Code

Bounded Context: **Financas**. Cadastro de **credores** (fornecedores e demais beneficiários de
empenhos, ordens de pagamento e restos a pagar): pessoa física ou jurídica, identificada por CPF/CNPJ,
com dados bancários para pagamento e situação cadastral (ativo/inativo). É o beneficiário referenciado
pela despesa orçamentária (Empenho → Liquidação → Pagamento, Lei 4.320) e a base do extrato por credor.

## Linguagem ubíqua

- **Credor:** beneficiário de uma despesa pública (fornecedor, servidor, ente, etc.), com documento único.
- **Documento:** CPF (pessoa física) ou CNPJ (pessoa jurídica), validado e único por tenant.
- **Dados bancários:** conta para crédito do pagamento (banco/agência/conta); opcionais no cadastro.
- **Extrato do credor:** consolidação dos empenhos/liquidações/pagamentos do credor no período.

## Invariantes

- **CR-1:** documento (CPF/CNPJ) obrigatório, válido e **único por tenant**.
- **CR-2:** credor nasce **Ativo**; só credor ativo pode ser beneficiário de novo empenho/pagamento.
- **CR-3:** inativação é reversível (reativação); a inativação não apaga o histórico (auditoria imutável).
- **CR-4:** alteração de nome/dados bancários é auditável (trilha antes/depois — CLAUDE.md §4).

## Fluxo

Cadastrar credor → (AtualizarCredor | Inativar | Reativar)* — referenciado pela despesa (Empenho/OP/RAP).

<!-- manifest
commands: CadastrarCredor, AtualizarCredor, InativarCredor, ReativarCredor
queries: ListarCredores, ObterCredor, ObterExtratoCredor
domainEvents: CredorRegistrado
integrationEventsPublished: 
integrationEventsConsumed: 
-->
