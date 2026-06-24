# Tesouraria (Caixa-Banco) — Rules-as-Code

Coração operacional diário do financeiro: contas bancárias/caixa do ente, movimento de caixa
(entradas/saídas com saldo por conta/dia), transferência entre contas e Boletim de Caixa/Receita/
Despesa (fechamento diário). Fecha o gap SW-A1 da paridade-PoC (vs SAPI). A conciliação bancária
(casamento com extrato do banco) é modelada aqui, mas o import de extrato OFX/CNAB é // TODO(M10)
(depende de credencial/layout do banco — Trilha B).

## Linguagem ubíqua

- **ContaFinanceira** — agregado de conta bancária OU caixa do ente (tipo, situação, saldo).
- **MovimentoFinanceiro** — entrada/saída lançada numa conta numa data (compõe o saldo por conta/dia).
- **TransferenciaEntreContas** — operação auditada que debita uma conta e credita outra (saída+entrada).
- **BoletimCaixaBanco** — relatório de fechamento diário (saldo anterior, entradas, saídas, saldo atual).
- **Conciliacao** — marcação de movimento como conciliado contra o extrato (casamento; import defere M10).

## Invariantes (domínio)

- Conta nasce válida (número, tipo, situação Ativa); saldo evolui SOMENTE por movimento/transferência.
- Movimento referencia uma conta do MESMO tenant; valor > 0; sinal definido pelo `TipoMovimentoFinanceiro`.
- Saída não pode tornar o saldo negativo além do limite parametrizado da conta (sem número mágico).
- Transferência é atômica: a saída na origem e a entrada no destino ocorrem juntas (mesma operação).
- Conta encerrada/inativa não aceita novos movimentos.
- Tudo multi-tenant (`IMustHaveTenant`) e auditado (trilha antes/depois).

## Multi-tenant, auditoria e fuso

- `TenantId` carimbado pelo interceptor; Global Query Filter isola o tenant.
- A data do movimento/boletim usa o relógio civil do tenant (`IDataHojeTenant`), nunca UTC cru.

<!-- manifest
commands: AbrirContaFinanceira, RegistrarRecebimento, RegistrarPagamentoCaixa, TransferirEntreContas, ConciliarMovimento
queries: ListarContasFinanceiras, ConsultarExtratoConta, GerarBoletimCaixaBanco
domainEvents: MovimentoFinanceiroRegistrado
integrationEventsPublished: 
integrationEventsConsumed: 
-->
