# Remessa Bancária CNAB240 (FEBRABAN) — Rules-as-Code (Finanças)

> Geração do arquivo de **remessa de pagamento a fornecedores/servidores** no layout posicional
> **CNAB240** padrão **FEBRABAN** (Tipo de Serviço 20 — pagamento a fornecedores). O arquivo reúne as
> ordens de pagamento já liquidadas e produz os registros de 240 posições (Header de Arquivo, Header de
> Lote, Segmentos A e B, Trailer de Lote e Trailer de Arquivo). A transmissão efetiva ao banco é diferida
> (M10): aqui geramos e validamos o conteúdo do arquivo.

<!-- manifest
commands:
queries: GerarRemessaCnab240
domainEvents:
integrationEventsPublished:
integrationEventsConsumed:
-->

---

## 1. Linguagem ubíqua

| Termo | Definição |
|---|---|
| **Remessa CNAB240** | Arquivo de pagamento em lote no layout FEBRABAN; cada linha tem **exatamente 240** posições. |
| **Pagador** | Ente público origem do crédito (banco/agência/conta/convênio e inscrição). |
| **Favorecido** | Credor/servidor beneficiário do crédito (banco/agência/conta ou chave PIX). |
| **Forma de lançamento** | Crédito em conta, DOC/TED, PIX-transferência, etc. (define o segmento e os campos). |

---

## 2. Invariantes (Rules-as-Code)

- **I-1 — Registro de 240 posições.** Todo registro gerado é validado em **exatamente 240** caracteres;
  divergência lança `RemessaCnabInvalidaException`.
- **I-2 — Remessa não-vazia.** Remessa sem favorecidos é inválida.
- **I-3 — Totais coerentes.** O trailer de lote soma os valores e conta os registros (header + detalhes +
  trailer); o trailer de arquivo conta lotes e registros.
- **I-4 — Campos formatados de forma estável.** Numéricos e datas usam formatação invariante de cultura
  (sem dependência de locale), preservando o alinhamento posicional do layout.

---

## 3. Cenários BDD (resumo)

1. **Remessa com 1 favorecido** → 6 registros (header arquivo, header lote, A, B, trailer lote, trailer
   arquivo), todos com 240 posições.
2. **Remessa vazia** → falha (I-2).
3. **PIX** → a chave PIX é escrita no Segmento B (campo de endereço/identificação de uso bancário).

> Cobertura em `tests/Tensorroot.Gov.Modules.Financas.Tests/`.
