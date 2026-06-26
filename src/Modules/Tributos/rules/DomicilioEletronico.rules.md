# Regras — Domicílio Eletrônico do Contribuinte (DEC)

<!-- manifest
commands: AderirDomicilioEletronico, DisponibilizarMensagemFiscal, CancelarDomicilioEletronico
queries: 
domainEvents: DomicilioEletronicoAderido, DomicilioEletronicoCancelado, MensagemFiscalDisponibilizada, CienciaMensagemFiscalRegistrada
integrationEventsPublished: 
integrationEventsConsumed: 
-->

> Bounded Context: **Tributos** — caixa postal fiscal eletrônica (**DEC**) com **ciência** e **prazo**.
> Paridade com o incumbente **SAPI**. Modelo análogo ao **e-CAC/DTE**: após adesão, as comunicações têm
> efeito de **intimação pessoal**. Datas são do **fato** (informadas), nunca do relógio (CLAUDE.md §16).

---

## 1. Linguagem ubíqua

- **Domicílio (`DomicilioEletronicoContribuinte`)** — caixa do contribuinte; um domicílio **ativo** por
  contribuinte. Parametriza os **dias de ciência tácita** (padrão 15).
- **Mensagem fiscal (`MensagemFiscal`)** — intimação/notificação/aviso; guarda disponibilização, limite
  de ciência tácita, forma de ciência (pendente/expressa/tácita), data de ciência e limite de manifestação.
- **Ciência** — **expressa** (consulta do contribuinte) ou **tácita** (decurso do prazo de
  disponibilização sem consulta). A partir da ciência conta-se o **prazo de manifestação/pagamento**.

---

## 2. Comandos (escrita)

- **`AderirDomicilioEletronicoCommand`** → cria a caixa (efeito legal a partir da adesão). Idempotente
  por contribuinte (recusa segundo domicílio ativo).
- **`DisponibilizarMensagemFiscalCommand`** → envia a comunicação ao domicílio ativo; inicia a contagem
  da ciência tácita.
- **`CancelarDomicilioEletronicoCommand`** → desativa (novas mensagens vedadas).
- **Cidadão (via Contracts):** `ConsultarMinhaCaixaPostalFiscalAsync` — lê a caixa do PRÓPRIO cidadão e,
  na mesma operação, aplica a ciência tácita vencida e registra a **ciência expressa** das pendentes
  (a consulta vale como ciência).

---

## 3. Eventos

- **Domínio (in-process):** `DomicilioEletronicoAderido`, `DomicilioEletronicoCancelado`,
  `MensagemFiscalDisponibilizada`, `CienciaMensagemFiscalRegistrada(... DataCiencia, Tacita)`.
- **Integração:** nenhum publicado/consumido nesta versão.

---

## 4. Pendências `// TODO(validar-oficial)`

- **Prazo legal** de ciência tácita e prazos de manifestação conforme o CTM/PAT de Maximiliano de Almeida/RS.
- Notificação ativa (push/e-mail) da disponibilização e **comprovante de ciência** (PDF assinado) — defere.
- Vínculo automático das **intimações de lançamento/CDA** ao DEC do contribuinte (parametrização por tenant).
