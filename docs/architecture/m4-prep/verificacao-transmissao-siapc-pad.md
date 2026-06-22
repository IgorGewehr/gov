# Verificação adversarial — `pesquisa-transmissao-siapc-pad.md` (M4)

> Papel: cético/auditor de fatos (CLAUDE.md §16). Tentei REFUTAR cada afirmação factual do documento `pesquisa-transmissao-siapc-pad.md` contra fonte oficial.
> Método: download direto via `curl` + `pdftotext -layout` dos PDFs oficiais do TCE-RS (o domínio `tcers.tc.br` está atrás de **Cloudflare/challenge** e bloqueia fetch automatizado; `www.tce.rs.gov.br` responde). Buscas web complementares.
> Data da verificação: 2026-06-22.
>
> Classificação usada: **CONFIRMADO** (transcrito de fonte oficial primária, com URL) · **PLAUSÍVEL-SEM-FONTE** (coerente mas não extraído byte-a-byte) · **INCERTO/CONTRADITÓRIO** (sem fonte vigente OU contradito pela fonte).

---

## Placar

- **CONFIRMADOS: 12**
- **PLAUSÍVEL-SEM-FONTE: 3**
- **INCERTO/CONTRADITÓRIO: 4** (sendo **1 ERRO FACTUAL** detectado no documento)

Fontes primárias re-baixadas e transcritas nesta verificação:
- FAQ SIAPC/PAD — `http://www.tce.rs.gov.br/sistemas_controle/SIAPC/pdf/perguntas_frequentes.pdf` (8 pág., baixado OK, transcrito)
- Manual SICOE (TCERS WIKI) — `https://tcers.tc.br/repo/cex/sicoe/manual-sicoe.pdf` (baixado via curl OK, transcrito)
- e-Validador — Novo processo de autenticação (ago/2022) — `https://tcers.tc.br/repo/cex/licitacon/eValidador_NovaAutenticacao.pdf` (baixado via curl OK, transcrito) — **doc-chave que o documento original marcava `[a confirmar]`; agora obtido**.

---

## Tabela de afirmações

| # | Afirmação no documento | Classificação | Evidência (fonte oficial) |
|---|---|---|---|
| A1 | Há DOIS canais distintos: SIAPC/PAD+MCI (direta) vs SICOE/e-Validador (indireta — Companhias e Entidades) | **CONFIRMADO** | e-Validador auth doc, Apresentação: *"SICOE (Sistema de Informações para Companhias e Entidades da Administração Indireta)"*. Manual SICOE FAQ 6: *"SICOE – Sistema de Informações para Companhias e Entidades"*. |
| A2 | O piloto (Prefeitura) usa fluxo PAD/MCI, NÃO o SICOE | **CONFIRMADO** | Decorre de A1: SICOE/e-Validador é só para indireta. Manual SICOE e e-Validador falam em "Companhia"/"Administrador Responsável", nunca em Prefeitura. |
| A3 | **NÃO há API/web service público REST/SOAP** para o ente integrar a transmissão diretamente | **CONFIRMADO** (reforçado) | e-Validador auth: transmissão exige autenticação **externa via navegador** ("Autorizar Envio" ou **QR-code** → redireciona ao "Autenticador e-Validador" com **TCE-login** / **CPF+Senha do operador**). É fluxo interativo humano dentro do app desktop, sem endpoint programável. PAD 25.0.0.0 (teste 2025) *"não permite o envio de remessas"*. |
| A4 | A transmissão é feita por app cliente oficial (PAD/e-Validador) que roda na máquina do ente, lê os `.TXT`, valida e transmite por canal próprio | **CONFIRMADO** | Manual SICOE, "Transmitir Remessa": *"Esta tela permite selecionar a forma de envio da remessa e transmitir os dados para o TCE-RS... pressionar o botão 'Iniciar'"*. FAQ 2 (SICOE): instalar app → gerar TXT → configurar certificado → validar → gerar remessa → transmitir → assinar. |
| A5 | Quote do Manual SICOE "Transmitir Remessa" + URL do eValidador_NovaAutenticacao.pdf | **CONFIRMADO (verbatim)** | Transcrito idêntico: *"A transmissão só será iniciada após realizar o processo de autenticação descrito em https://tcers.tc.br/repo/cex/licitacon/eValidador_NovaAutenticacao.pdf"*. |
| A6 | Quote do FAQ SIAPC item sobre assinatura digital / Processo Eletrônico / certificado conectado | **CONFIRMADO (verbatim)** | FAQ item 3: *"É necessário realizar a assinatura digital. Para tal, acesse Portal > Jurisdicionado > Processo Eletrônico > Acesso ao Sistema (nesse momento é necessário estar com o certificado digital conectado à máquina)..."*. |
| A7 | Assinatura final do RVE com Certificado particular ICP-Brasil; direciona ao e-Protocolo | **CONFIRMADO (verbatim)** | Manual SICOE, "Assinar Remessa": *"o Administrador Responsável deverá assinar digitalmente o RVE... utilizando seu Certificado particular ICP-Brasil... será direcionado para o Sistema e-Protocolo do TCE-RS"*. |
| A8 | SISCAD integrado ao SIAPC (PAD e MCI) desde 2014; cadastro único; PAD busca vínculos automaticamente | **CONFIRMADO (verbatim)** | FAQ item 1: *"Desde 2014 o SIAPC (PAD E MCI) está integrado com o Sistema de Cadastro Único – SISCAD... todas as informações cadastrais devem ser registradas no SISCAD."* FAQ 7: *"O PAD busca de forma automática o responsável atual."* |
| A9 | "É necessário fazer o envio do e-protocolo para completar a entrega" | **CONFIRMADO (verbatim)** | FAQ item 5: *"Sim. É necessário fazer o envio do e-protocolo para completar a entrega."* |
| A10 | Recibo consultado em Portal > Jurisdicionado > Sistema de Controle Externo > Relatórios e Recibos de envio; status `pendente / concluída / carregada` | **CONFIRMADO (verbatim)** | FAQ item 6: *"Acesse o site do TCE em Portal > Jurisdicionado > Sistema de Controle Externo > Relatórios e Recibos de envio, e verifique se a remessa está pendente, concluída ou carregada."* |
| A11 | Folha de Pagamento: mensal, até **30 (trinta) dias corridos** após o fim do período, a partir de jan/2019, Res. 1099/2018 | **CONFIRMADO (verbatim)** | FAQ item 9: *"O envio das informações da folha de pagamento está previsto na Resolução Nº 1099/2018... a partir de janeiro de 2019 a remessa é enviada mensalmente, em até 30 (trinta) dias corridos após o encerramento do período a que corresponder."* |
| A12 | Livro Diário Geral: mensal, Res. 1099/2018, obrigatório desde 2017/2018 | **CONFIRMADO (verbatim)** | FAQ item 10: *"Sim. Desde 2018 as informações, referente 2017, devem ser enviadas... a partir de 2019 a entrega está prevista na Resolução Nº 1099/2018 com periodicidade mensal."* |
| A13 | Reenvio do PAD: (a) dentro do prazo, livre; (b) fora do prazo e não-RGF, justificar na tela; (c) fora do prazo E mês de RGF, contatar TCE (SAG) | **CONFIRMADO (verbatim)** | FAQ item 11: as três situações descritas literalmente; mês de Gestão Fiscal fora do prazo → *"entrar em contato com o Serviço de Acompanhamento de Gestão – SAG"*. |
| A14 | RGF/MCI: quadrimestral >50 mil hab.; **semestral ≤50 mil hab.** → Maximiliano = semestral | **CONFIRMADO** (regra) | FAQ item 11: *"quadrimestral para municípios com mais de 50 mil habitantes, e semestral para municípios com até 50 mil habitantes"*. (Mas ver C1 sobre os meses.) |
| A15 | Formato do pacote: 1 ZIP, nome de 60 bytes `CNPJ.DataIni.DataFim.DataGer.Tipo.CodRemessa.zip`; tipos P/C/A/F/E/S/O; exemplo `99999999000199.01012007.31052007.15062007.P.000000000010.zip` | **PLAUSÍVEL-SEM-FONTE** (re-confirmar exercício 2026) | Já transcrito em `specs-oficiais/tce-rs-siapc-pad.md` §2.3 do MT Vol. V (Rev. 6, mar/2024). NÃO re-extraí o byte-a-byte do MT 2026 nesta rodada. A própria Rev. muda anualmente. |
| A16 | Cabeçalho/Corpo/Finalizador, ASCII ISO-8859-1, CR/LF, larguras fixas | **PLAUSÍVEL-SEM-FONTE** (exercício corrente) | Transcrito do MT Vol. V Rev.6 em `specs-oficiais`. Estrutura geral estável historicamente, mas leiaute do exercício-alvo deve ser re-baixado (novidade 2026: BP + DFC). |
| A17 | Certificado A1/A3 ICP-Brasil para identificação do ente; signatários resolvidos do SISCAD | **PLAUSÍVEL-SEM-FONTE / parcial** | Manual SICOE FAQ 2c confirma config do certificado da entidade. **PORÉM** o e-Validador auth (ago/2022) **REMOVEU** o certificado TCE-net da identificação do órgão, trocando por **seleção do órgão em lista + CPF/Senha do operador**. Para a Prefeitura (PAD) o quadro pode diferir — ver C4. |
| A18 | Prazo padrão mensal do PAD "regular" (orçamentário/contábil, fora Folha/Diário) = "30 dias corridos" | **INCERTO/CONTRADITÓRIO** | O FAQ confirma "30 dias corridos" **apenas para Folha (item 9) e Diário (item 10)**. Para a remessa regular orçamentária NÃO há, no FAQ, um prazo em dias; o FAQ remete a Ofício Circular anual. O documento já marca isto como `[a confirmar]` — correto. Notícia oficial sobre prazos (tcers.tc.br) bloqueada por Cloudflare; não consegui extrair data exata do calendário 2026. |
| A19 | "1º semestre na entrega de junho; 2º na de janeiro" (Maximiliano, semestral) | **INCERTO/CONTRADITÓRIO — ERRO FACTUAL** | Ver **C1** abaixo. O quadro do FAQ mostra **RGF/MCI - 1º Semestre na coluna "Junho (entrega julho)"** e **RGF/MCI - 3º Quad./2º Semestre na coluna "Dezembro (entrega janeiro)"**. Ou seja, o 1º semestre é entregue em **JULHO** (competência junho), não em junho. O documento está com 1 mês de defasagem na ponta de junho/julho. |
| A20 | Nome `EnviarRemessaTce`/`SICOE` no domínio está ambíguo; rever/renomear | **CONFIRMADO** (recomendação válida) | A1/A2/A3 sustentam: "SICOE" no código induz a erro (é canal de indireta). Para Prefeitura o canal correto é PAD/e-Protocolo. |

---

## Os 3 maiores RISCOS (o que NÃO implementar sem o documento oficial vigente)

### Risco 1 — Modelar `EnviarRemessaTce` como integração ativa (POST a um web service do TCE-RS)
**Evidência forte de que NÃO existe esse endpoint.** A transmissão real do PAD/e-Validador é interativa: roda no app desktop oficial **operado por um servidor humano da prefeitura**, com autenticação externa via navegador (TCE-login / CPF+Senha) e **QR-code**, e a conclusão depende de **assinatura no e-Protocolo** com certificado conectado à máquina. A versão de teste do PAD 2025 (25.0.0.0) **explicitamente não transmite**, só valida.
**Implicação:** `EnviarRemessaTce` NÃO deve fazer POST a uma API. O valor real e automatizável do ERP termina em **gerar os `.TXT` no leiaute + montar o ZIP nomeado + registrar protocolo/recibo manualmente** (artefato de auditoria, CLAUDE.md §6). Tratar o "envio" como entrega de artefato para o app oficial, não como chamada HTTP. NÃO inventar handshake/endpoint.

### Risco 2 — Hardcodar prazos e a cadência de meses do RGF (erro de off-by-one já presente no doc)
O prazo mensal do PAD **regular** (orçamentário) NÃO está fixado em dias no FAQ — depende de **Ofício Circular anual**, que o TCE-RS historicamente **prorroga** (2022/2023/abr-2024). E o doc tem **erro factual** na ponta do RGF semestral (C1): o 1º semestre é assinado/entregue em **julho** (competência junho), não em junho.
**Implicação:** prazos e calendário de assinaturas DEVEM ser **parametrizáveis por tenant/exercício** (CLAUDE.md §7 — proibido hardcode de regra fiscal/prazo), alimentados pelo calendário oficial do exercício. NÃO codar "junho/janeiro" nem "30 dias" como constante para a remessa regular. Confirmar o calendário 2026 com o Ofício Circular/IN vigente (fonte primária bloqueada por Cloudflare nesta rodada).

### Risco 3 — Congelar o leiaute do ZIP/arquivos a partir de manual de exercício anterior
O leiaute (MT SIAPC) muda **a cada exercício** e o **PAD muda de versão todo ano**. Para **2026** foram anunciados **Balanço Patrimonial + Demonstração dos Fluxos de Caixa** novos (26ª edição do evento SIAPC, nov/2025). O nome do ZIP/cabeçalho transcrito (A15/A16) vem do MT Vol. V **Rev. 6 (mar/2024)** — não re-extraído do exercício 2026.
**Implicação:** o gerador de remessa deve ser dirigido pelo **leiaute do exercício-alvo** (versionado), não por constantes do código. Antes de implementar o gerador 2026, **baixar o MT Vol. I–V do exercício 2026 + Tabela do PAD 2026** e re-validar campos byte-a-byte.

---

## Pendências que continuam ABERTAS (precisam de doc oficial vigente antes de implementar)

1. **Prazo da remessa regular mensal (orçamentário) do exercício 2026** — Ofício Circular/calendário anual. Fonte primária (`tcers.tc.br/noticia/...prazos...`) **bloqueada por Cloudflare** nesta verificação; obter por download autenticado/manual. `[a confirmar — Ofício Circular SIAPC 2026]`
2. **Conteúdo byte-a-byte da Res. 1.074/2017 e IN 08/2017** — só citados pelo Manual SICOE; não extraídos. (Relevante apenas se o escopo M4 incluir indireta/SICOE — confirmar escopo com o dono.)
3. **Leiaute MT SIAPC Vol. I–V do exercício 2026 + versão do PAD 2026** (sucessora da 25.0.0.0) — incluindo os novos BP + DFC. `[obter do portal TCE-RS]`
4. **Fluxo de identificação do ente no PAD (direta)** — o e-Validador (indireta) trocou certificado TCE-net por CPF/Senha+seleção de órgão (ago/2022); **confirmar se o PAD da direta seguiu o mesmo modelo** ou mantém certificado. `[a confirmar — manual/versão PAD 2026]`

---

## Correção factual a aplicar no documento de pesquisa

**C1 (corrigir `pesquisa-transmissao-siapc-pad.md` §3, linha da RGF, e o §4):** trocar
"*1º sem. na entrega de junho; 2º na de janeiro*" por
"**1º semestre: RGF/MCI assinados na entrega de JULHO (competência junho); 2º semestre (3º Quad./2º Sem.): na entrega de JANEIRO (competência dezembro)**".
Fonte: quadro "Assinaturas necessárias para o Executivo e Legislativo" do FAQ SIAPC/PAD — coluna *Junho (entrega julho)* contém "RGF - 1º Semestre" e coluna *Dezembro (entrega janeiro)* contém "RGF - 3º Quad./2º Semestre".

---

## Fontes (re-verificadas nesta rodada)

- FAQ SIAPC/PAD (baixado + transcrito): http://www.tce.rs.gov.br/sistemas_controle/SIAPC/pdf/perguntas_frequentes.pdf
- Manual SICOE (baixado via curl + transcrito): https://tcers.tc.br/repo/cex/sicoe/manual-sicoe.pdf
- e-Validador — Novo processo de autenticação, ago/2022 (baixado via curl + transcrito): https://tcers.tc.br/repo/cex/licitacon/eValidador_NovaAutenticacao.pdf
- Notícia PAD 2025 (25.0.0.0 valida, não transmite): https://tcers.tc.br/noticia/tce-disponibiliza-versao-de-teste-do-programa-autenticador-de-dados-para-2025/
- 26ª edição evento SIAPC (BP + DFC para 2026): https://tcers.tc.br/noticia/tce-rs-realiza-26a-edicao-do-evento-do-siapc-com-orientacoes-tecnicas-para-gestores-municipais/
- MT SIAPC Vol. V (Rev.6 mar/2024 — já transcrito em specs-oficiais): http://www.tce.rs.gov.br/sistemas_controle/SIAPC/pdf/MT_Vol_V_Arq_DispTCE_4320.pdf

> Nota de método: `tcers.tc.br` está atrás de **Cloudflare challenge** — páginas HTML de notícia retornam 403/desafio JS a curl e WebFetch; porém os **PDFs em `/repo/...` baixam direto via curl**. `www.tce.rs.gov.br` (PDFs do FAQ/MT) responde normalmente.
