# Certidão de Regularidade Fiscal (CND/CPEN) — Rules-as-Code

Serviço de balcão de altíssimo uso (exigido em licitações/contratos): emite ao contribuinte a
**Certidão Negativa de Débitos (CND)** ou a **Certidão Positiva com Efeito de Negativa (CPEN)**
quando há débito com exigibilidade suspensa (parcelamento). Fecha o gap SW-A3 da paridade-PoC.
Fundamento: CTN art. 205/206. A emissão pelo próprio cidadão é feita via Portal Cidadão
(autosserviço, dado-próprio resolvido server-side).

## Linguagem ubíqua

- **CertidaoRegularidadeFiscal** — agregado da certidão (número, tipo, contribuinte, validade, hash).
- **TipoCertidaoRegularidade** — Negativa (CND) | PositivaComEfeitoNegativa (CPEN) | Positiva.
- **SituacaoFiscalContribuinte** — apuração que decide o tipo: lançamentos vencidos em aberto +
  dívida ativa exigível vs. suspensa, na data-base.
- **CodigoAutenticacao** — hash/código para conferência da autenticidade da certidão.

## Invariantes (domínio)

- O tipo é decidido deterministicamente pela situação fiscal apurada (sem débito exigível ⇒ CND;
  só débito suspenso ⇒ CPEN; débito exigível ⇒ Positiva — não atesta regularidade).
- A certidão nasce com data de emissão (relógio do tenant), validade parametrizada e código de
  autenticação; sequencial por exercício isolado por tenant.
- Conferência por número/código devolve a certidão emitida (autenticidade) sem vazar terceiros.
- Multi-tenant (`IMustHaveTenant`) e auditada.

<!-- manifest
commands: EmitirCertidaoRegularidade
queries: ConferirCertidaoRegularidade
domainEvents: CertidaoRegularidadeEmitida
integrationEventsPublished: 
integrationEventsConsumed: 
-->
