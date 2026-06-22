# ADR-0003 — NFS-e Nacional (ADN): Integração Passiva via Worker

- **Status:** Aceito (substitui a abordagem inicial de middleware ABRASF)
- **Data:** 2026-06-20

## Contexto

O **padrão nacional da NFS-e** entrou em vigor em **2026** e o município piloto
(**Maximiliano de Almeida/RS**) já aderiu. Nesse modelo, o contribuinte **emite a nota
diretamente no portal gov.br / APIs da Receita Federal**, onde também ocorrem a **validação
e a assinatura oficial** do documento. A prefeitura permanece **titular do ISS** e o fiscal
precisa saber quem emitiu notas para cobrar quem deve.

A abordagem original (desenvolver Portal do Contribuinte para emissão, webservices de recepção
padrão **ABRASF** e **assinatura A1** das notas no nosso lado) tornou-se **obsoleta e desnecessária**.

## Decisão

1. **Remover** do escopo a emissão/recepção ABRASF e a **assinatura A1 de NFS-e**.
2. O módulo **Tributos** passa de *emissor* a **consumidor passivo** ("sugador de dados").
3. Criar o **Worker `Tensorroot.Gov.Workers.NfseSync`**: processo em segundo plano (Azure) que
   **diariamente** conecta na **API da Receita Federal / Ambiente de Dados Nacional (ADN)**,
   baixa os XMLs das NFS-e emitidas pelos **CNPJs do tenant**, deduplica por **chave de acesso**,
   e persiste — preenchendo o **painel fiscal** automaticamente.
4. **Concentrar o esforço** onde a prefeitura realmente precisa: **Dívida Ativa, CDA, protesto e
   cobrança (boleto/PIX)**.
5. **Certificado A1 permanece** no escopo — mas apenas para **eSocial**, **remessas TCE-RS** e
   **assinatura de documentos no Protocolo**.

## Consequências

- ➕ **Menos risco jurídico e de infraestrutura** (não somos fonte/assinante do documento fiscal).
- ➕ Reduz o esforço de Tributos em ~50%; foco no valor real (cobrança/Dívida Ativa).
- ➖ **Dependência da disponibilidade da API nacional** — mitigada por **Polly** (retry/circuit breaker),
  reprocessamento idempotente (chave de acesso) e janelas de sincronização.
- ➖ Requer **credenciamento/escopo de acesso** aos dados do tenant no ambiente nacional (config por tenant).
- 🔗 Implementação detalhada na **Fase 5**; spec no README do módulo **Tributos**.
