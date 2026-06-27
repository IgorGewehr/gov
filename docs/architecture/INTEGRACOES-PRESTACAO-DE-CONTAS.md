# Prestação de Contas & Remessas Obrigatórias — Arquitetura de Geração

> **A prefeitura não é uma ilha: ela presta contas.** O sistema é, na prática, refém de
> quatro grandes saídas de dados. Este documento define **como** cada uma é gerada,
> assinada, transmitida e auditada — e o framework comum que as sustenta.

As quatro saídas críticas:

| # | Saída | Origem (módulo) | Formato | Periodicidade | Destino |
|---|---|---|---|---|---|
| 1 | **Remessa TCE-RS (SIAPC/PAD)** | Finanças + Tributos + RH + Patrimônio | XML/posicional assinado (A1) | Mensal + anual | TCE-RS (webservice) |
| 2 | **Remessa bancária CNAB 240/400** | Finanças (Tesouraria) + Tributos (arrecadação) | Posicional largura fixa | Diária/sob demanda | Banco (Febraban) |
| 3 | **Portal da Transparência** | Transparência (consome eventos) | CSV/JSON (dados abertos LAI) | Diária/contínua | Web pública + SICONFI |
| 4 | **e-Social** | RecursosHumanos | XML assinado (A1) | Por evento + fechamento | Ambiente Nacional eSocial |

---

## 0. Princípio: a remessa é um **projeção**, não um efeito colateral

Nenhum gerador de remessa lê o domínio "ao vivo" no meio de uma transação de negócio. Toda
remessa é uma **projeção determinística** de dados já consolidados (empenhos pagos, folhas
fechadas, NFS-e ingeridas), gerada por um **Worker agendado**, **idempotente** e **auditada**.
Isso garante que reprocessar uma competência produz o mesmo arquivo (ou um arquivo retificador
explícito), requisito de qualquer fiscalização.

```
Domínio  ──(Outbox/eventos)──►  Tabelas de projeção  ──►  Gerador de Remessa  ──►  Arquivo
   │                                                            │
   └── consolidação (fechamento de competência)                └── assina (A1) + transmite (ACL+Polly) + RemessaLog
```

---

## 1. Framework comum de remessas (`Tensorroot.Gov.Integracoes`)

Toda remessa compartilha a mesma espinha dorsal:

### 1.1 Geração de layout posicional (largura fixa) — `Span<T>`
Arquivos posicionais (CNAB, vários layouts SIAPC) são montados com um construtor de linha de
largura fixa, base-1, sem alocação desnecessária (ver `Cnab/Cnab240Gerador.Linha`):
`Num(posicao, tamanho, valor)` (numérico, zeros à esquerda) e `Alfa(posicao, tamanho, texto)`
(alfa, espaços à direita, upper-invariant). Cada linha nasce com 240/400 posições preenchidas.

### 1.2 Idempotência — `RemessaLog`
Cada remessa gerada é registrada (tenant, tipo, competência, hash do conteúdo, protocolo de
retorno, status). Um novo disparo para a **mesma competência** que já foi **aceita** não
regenera nem retransmite — produz uma **retificadora** explícita, nunca uma duplicata silenciosa.

### 1.3 Assinatura A1 — Azure Key Vault, por tenant
Remessas assinadas (TCE, e-Social) buscam o **certificado A1 (.pfx) no Key Vault do tenant**
(nunca no repositório). A assinatura XMLDSig é aplicada na borda de Infraestrutura, atrás de uma
abstração `IAssinadorXml` — o domínio nunca toca em chave privada.

### 1.4 Transmissão resiliente — ACL + Polly
Toda submissão a webservice (TCE, eSocial) passa por um **Anti-Corruption Layer** (`I<Destino>Gateway`)
com **Polly** (timeout + retry exponencial + circuit breaker) e **mapeamento explícito de erros**
do destino para o domínio. O envio é disparado por **Integration Event via Outbox** — só sai
depois que o estado foi committado.

### 1.5 Agendamento — Workers por tenant
Geração/transmissão roda em **Workers** (como o `NfseSync`), que **iteram os tenants** licenciados
para aquele módulo e, para cada um, resolvem o **banco dedicado** (`ITenantConnectionResolver`),
geram e transmitem. Falha de um tenant não derruba os demais.

### 1.6 Auditoria
Geração e transmissão são mutações → passam pelo `AuditSaveChangesInterceptor` (trilha imutável:
quem disparou, quando, qual competência, qual protocolo de retorno) — pronta para o TCE.

---

## 2. Remessa TCE-RS (SIAPC/PAD) — módulo Transparência

**O que é:** prestação de contas mensal (SIAPC) e de gestão (PAD) ao Tribunal de Contas do RS.
**Como o sistema gera:**
1. **Fechamento de competência** consolida, do módulo Finanças (PCASP/MCASP), Tributos (arrecadação),
   RH (folha) e Patrimônio (bens/depreciação), as tabelas de projeção da remessa.
2. O **gerador SIAPC** monta os arquivos no **layout da versão vigente do TCE-RS** (`Span`),
   parametrizado por `IOptions` versionado — **layouts são dados, não código** (nunca *hardcoded*).
3. **Assina (A1/Key Vault)** quando exigido e empacota.
4. Transmite via **`ITceGateway`** (ACL + Polly); registra protocolo no `RemessaLog`.
5. **Retorno** (aceito/rejeitado + inconsistências) volta como evento → painel de pendências.

> ⚠️ Layout do TCE-RS muda por exercício: a versão é confirmada em fonte oficial antes de cada
> ciclo (regra CONVENCOES-ENGENHARIA.md §8). O motor é genérico; o **layout é plugável por versão**.

---

## 3. Remessa bancária CNAB 240/400 — módulo Finanças (Tesouraria) ✅ implementado (240)

**O que é:** arquivo de remessa para o banco — **pagamentos** (fornecedores, folha) e
**cobrança/arrecadação** (retorno de tributos).
**Como o sistema gera (já em código — `Cnab240Gerador`):**
- Header de Arquivo (reg. 0) → Header de Lote (reg. 1) → **Segmento A** por pagamento (reg. 3) →
  Trailer de Lote (reg. 5) → Trailer de Arquivo (reg. 9). Linhas de **240 posições**.
- Entradas: `EmpresaCnab` (ente pagador, banco/agência/conta) e `PagamentoCnab[]` (favorecido,
  valor em centavos, data). Valores monetários em centavos; documentos só-dígitos.
- **Retorno bancário** (baixa de pagamentos/arrecadação) é lido pelo processador de retorno e
  concilia os `Empenho`/`Lancamento` correspondentes (idempotente por nosso-número/sequencial).

**Próximos passos:** Segmento B (endereço/PIX), CNAB 400 legado, e os layouts específicos por
banco (BB 001, Caixa 104, Sicredi 748) sobre a base FEBRABAN.

---

## 4. Portal da Transparência — módulo Transparência

**O que é:** publicação de dados abertos (LAI/Lei 12.527) + insumo para SICONFI/MSC.
**Como o sistema gera:**
1. O módulo Transparência **consome Integration Events** dos demais (despesa empenhada/liquidada/paga,
   receita arrecadada, contrato firmado, servidor admitido) — **sem acoplar** aos internos deles.
2. Materializa **datasets** (despesas, receitas, licitações, contratos, folha, diárias) em tabelas
   de leitura desnormalizadas.
3. **Exportadores** geram **CSV e JSON** (dados abertos) e alimentam o **portal público** e a
   geração da **MSC (Matriz de Saldos Contábeis)** para o SICONFI.
4. Atualização **contínua/diária** por Worker; cada publicação é versionada e auditada.

> Aqui o Outbox é essencial: a transparência é a **consumidora** dos eventos de todos os módulos.
> Por isso o Outbox (interceptor + publisher) foi a fundação endurecida antes deste motor.

---

## 5. e-Social — módulo RecursosHumanos

**O que é:** eventos trabalhistas ao Ambiente Nacional (S-1000 empregador, S-2200 admissão,
S-1200 remunerações, S-1210 pagamentos, fechamentos).
**Como o sistema gera:**
1. Eventos de domínio do RH (servidor admitido, folha fechada, rescisão) viram, via projeção,
   **eventos eSocial** no schema XSD vigente.
2. **Assina (A1/Key Vault)** cada evento (XMLDSig) e monta os **lotes**.
3. Transmite via **`IESocialGateway`** (ACL + Polly), respeitando a **ordem e dependências**
   (S-1000 antes de S-2200 antes de S-1200) e o **controle de recibos** (idempotência por recibo).
4. Retorno/recibo volta como evento → consolida status e libera o próximo evento da cadeia.

---

## 6. Estado atual e sequência recomendada

| Item | Estado |
|---|---|
| Framework de largura fixa (`Span`) | ✅ (base do CNAB) |
| **CNAB 240 (pagamentos)** | ✅ implementado + testado |
| Outbox (fundação da Transparência) | ✅ interceptor + publisher testados |
| A1/Key Vault, `RemessaLog`, gateways ACL | ⏳ abstrações definidas; implementação por destino |
| SIAPC/PAD, Transparência/MSC, e-Social | ⏳ motor desenhado; layouts/gateways a implementar por módulo |

**Sequência:** (1) `RemessaLog` + `IAssinadorXml` + base de gateway resiliente → (2) CNAB retorno +
Segmento B → (3) Transparência (consumindo o Outbox já pronto) → (4) SIAPC/PAD → (5) e-Social.
Cada um nasce do respectivo `*.rules.md` e é gerado/auditado pela fábrica Rules-as-Code.
