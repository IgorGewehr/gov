# Credenciais & Onboarding para Go-Live (Insumos de Produção)

**Guia acionável para o responsável pelo produto/município.** Esta é a parte do go-live que **não depende de horas de engenharia**, e sim de **processos burocráticos externos** (cadastros, certificados, adesões, convênios). O código está pronto e marcado no `src/` com `// TODO(M10)` / `// TODO(M10-creds)` / `// TODO(validar-leiaute-*)` atrás de ACL (Anti-Corruption Layer) e de gateways `Simulado*`/`Stub` — a troca por integração real só "acende" quando a credencial chega.

- **Piloto:** Município de Maximiliano de Almeida/RS — Tribunal de Contas do Estado do RS (TCE-RS).
- **Âncora:** estado do código (módulos completos, Cofre A1 em modo DEV pronto p/ PROD, gateways simulados nas integrações oficiais).

> ⚠️ **REGRA DE OURO DESTE GUIA:** prazos, links e nomes de portais/sistemas oficiais **mudam**. Todo prazo aqui é uma **estimativa de planejamento**, não um SLA. **CONFIRMAR na hora** no portal/órgão oficial e com o contador/controlador do município antes de agir. Onde escrevo "confirmar link/prazo oficial", é porque o valor exato pode ter mudado desde a redação.

> 🔑 **Para QUEM é cada solicitação:** a maioria exige assinatura do **Gestor/Chefe do Executivo (Prefeito)** ou de **procurador com poderes**, operada na ponta pelo **Contador/Controlador Interno**, **RH/folha**, **TI** ou **pregoeiro**, conforme o insumo. Indico em cada item.

---

## ÍNDICE DOS 12 INSUMOS

1. Certificado A1 e-CNPJ do município (assinatura — base de tudo)
2. eSocial: Produção Restrita → Produção (RH/folha)
3. TCE-RS: e-Validador/PAD + leiaute MT vigente + cadastro do responsável
4. SICONFI/STN (MSC / DCASP)
5. Azure (assinatura, tenant, infra de produção)
6. Adesão à Rede gov.br + Relying Party (login do cidadão)
7. PNCP (Lei 14.133 — publicação de contratos/editais)
8. Banco arrecadador / convênio de arrecadação (DAM/tributos)
9. Saúde: RNDS / SI-PNI / CNES / HÓRUS (DATASUS)
10. Educação: Educacenso/INEP + SIOPE + PNAE/PNATE (FNDE)
11. Assistência: CadÚnico / SAGI / Transferegov (MDS)
12. ACT credenciada ICP-Brasil (carimbo de tempo RFC 3161 — Protocolo)

---

## 1. Certificado A1 e-CNPJ do município

**(i) O que é + por que o sistema precisa.**
Certificado digital ICP-Brasil **e-CNPJ tipo A1** (arquivo `.pfx`/PKCS#12, validade tipicamente 1 ano, fica em arquivo — diferente do A3 em token/cartão), emitido para o **CNPJ do ente**. É a **chave de assinatura digital** de tudo: eSocial, remessas TCE-RS, documentos do Protocolo (Lei 14.063/2020), e (em alguns fluxos) autenticação em webservices gov.
No nosso sistema o A1 entra pelo **módulo Cofre** (`CertificadoA1Cofre.cs`): nunca persistimos o `.pfx` em claro — a chave privada é embrulhada por envelope encryption (DEK/KEK AES-256-GCM). Hoje a KEK roda em **modo DEV (Config)**; em produção vira **Azure Key Vault** (item 5). **Sem A1 não há assinatura → nada de eSocial, remessa TCE ou documento assinado.**

**(ii) Como obter.**
- Órgão: **qualquer Autoridade Certificadora (AC) credenciada ICP-Brasil** + sua AR (Autoridade de Registro). Ex.: Serpro, Serasa, Certisign, Soluti, Valid, AC do SERPRO/Receita, AC Caixa para entes públicos. *Confirmar AC com melhor condição para órgão público.*
- Quem solicita: **representante legal do ente (Prefeito)** ou **procurador** com procuração registrada.
- Passos: (1) escolher AC/AR; (2) emitir e pagar; (3) **validação presencial ou por videoconferência** do titular/representante com documentos; (4) baixar o `.pfx` e definir a senha do certificado.
- Documentos: CNPJ/cartão CNPJ do ente, ato de nomeação/posse do Prefeito (ou procuração com firma), documentos pessoais do representante (RG/CPF), comprovantes do ente.

**(iii) Lead time + custo.**
~**3 a 10 dias úteis** (depende da agenda da validação). Custo do e-CNPJ A1: aproximadamente **R$ 200–450** (confirmar tabela da AC). **Renovação anual** obrigatória — agendar lembrete.

**(iv) Dependências.** Nenhuma anterior — **é a fundação**. Para uso em produção depende do Key Vault (item 5) estar pronto para guardar a KEK; em DEV/PoC já roda.

**(v) O que destrava no sistema.** Cofre (guarda do A1) → assinatura de **eSocial** (item 2), **remessas TCE-RS** (item 3), **documentos do Protocolo**. É **pré-requisito de 2, 3, 4 e 12**. **Começar JÁ.**

---

## 2. eSocial: Produção Restrita → Produção

**(i) O que é + por que.**
O eSocial tem dois ambientes: **Produção Restrita** (homologação — dados de teste, sem valor jurídico) e **Produção** (valor legal). Nosso RH gera os eventos (`S-1000` empregador, `S-1200`/`S-1202` remuneração RGPS/RPPS) e transmite via `TransmitirEventosESocial`. O serviço `GeradorEventoApplicationService` **já nasce apontando para Produção Restrita por padrão** (parâmetro `Ambiente`) — exatamente para homologar leiaute antes do go-live. A virada para Produção é um **parâmetro do tenant**, mas só deve acontecer **após validar leiaute na restrita**.

**(ii) Como obter / habilitar.**
- Órgão: **eSocial / Receita Federal**. Sem "cadastro" comercial: o acesso ao webservice é por **certificado e-CNPJ A1** (item 1). Quem prepara cargas e confere retornos: **RH/folha** + **contador**.
- Passos: (1) com o A1, transmitir lotes em **Produção Restrita** e corrigir até zerar erros de schema/regra; (2) confirmar enquadramento do ente e dos **eventos de tabela (S-1000/S-1005/S-1010...)** antes dos periódicos; (3) virar o parâmetro `Ambiente` do tenant para **Produção** e transmitir os eventos com valor legal, respeitando o **calendário de obrigatoriedade** do ente público.
- Documentos: A1 e-CNPJ; dados cadastrais do empregador (FAP/alíquotas RAT, lotações, rubricas).

**(iii) Lead time + custo.**
A habilitação técnica é imediata (depende só do A1). O esforço real é o **ciclo de homologação na Produção Restrita: ~2 a 6 semanas** de idas e voltas até zerar erros e bater a folha. **Sem custo de licença** (é obrigação federal). **CONFIRMAR o calendário/fase de obrigatoriedade vigente para entes públicos** — pode ter mudado.

**(iv) Dependências.** **Item 1 (A1)** obrigatório. Folha do RH fechada (`FecharFolha`) para gerar os periódicos.

**(v) O que destrava.** Transmissão real de eventos de RH/folha ao eSocial (trabalhista/previdenciário). É um dos **dois grandes ciclos de homologação** do projeto (o outro é o TCE-RS) — **começar a restrita cedo** porque o lead-time é de semanas.

---

## 3. TCE-RS: e-Validador/PAD + leiaute MT vigente + cadastro do responsável

**(i) O que é + por que.**
O TCE-RS recebe as prestações de contas em **arquivos de remessa de leiaute fixo** (largura fixa, ISO-8859-1), validados pelo **PAD (Programa Autenticador de Dados) / e-Validador** antes do envio. Abrange **SIAPC** (contábil/orçamentário, inclui o **Anexo/relatório de gestão fiscal** e os mínimos), **SICOE** e a **Folha (Resolução TCE-RS 1099/2018)**. Nosso módulo Transparência **já monta as remessas** (`GerarRemessaFolhaTce`, `EmissorRegistroSiapc`, `ILeiauteCatalogo`) com a **versão do leiaute parametrizável por catálogo** — por isso "leiaute MT vigente" é um insumo de configuração, não de código.

**(ii) Como obter.**
- Órgão: **TCE-RS** (portal do TCE-RS / SIAPC-PAD). Operação: **Contador/Controlador Interno** do município; cadastro do responsável assinado pelo **Gestor**.
- Passos: (1) **cadastrar o(s) responsável(is)** pela prestação no sistema do TCE-RS (geralmente vinculado ao contador e ao gestor, com certificado); (2) **baixar a versão vigente do e-Validador/PAD e o leiaute MT** (Manual Técnico) **do exercício corrente** — é publicado/atualizado periodicamente pelo TCE; (3) **carregar a versão do leiaute no nosso catálogo** (`ILeiauteCatalogo`) e rodar uma remessa de teste; (4) validar no PAD/e-Validador até passar limpo; (5) transmitir oficialmente nos prazos do calendário do TCE.
- Documentos: cadastro do ente no TCE-RS, A1 do ente (item 1) para assinatura/envio, dados do responsável (contador — registro CRC).

**(iii) Lead time + custo.**
**Sem custo** (obrigação). Lead-time burocrático do cadastro: **~1 a 3 semanas**. **Ciclo de homologação de leiaute + bater os números: ~2 a 6 semanas** (este é o **maior risco da PoC oficial** — o layout exato SIAPC/MSC tem 51 marcadores `// TODO(validar-leiaute-*)` no código). **CONFIRMAR a versão do leiaute do exercício e o calendário de entregas no portal do TCE-RS** — muda todo ano.

**(iv) Dependências.** **Item 1 (A1)**. Dados contábeis reais em Finanças (empenho→liquidação→pagamento) e folha do RH para as remessas correspondentes.

**(v) O que destrava.** Prestação de contas oficial SIAPC/SICOE/Folha ao TCE-RS — **a meta nº1 do produto**. É o ciclo de homologação **mais crítico**; **começar cedo**.

---

## 4. SICONFI/STN (MSC / DCASP)

**(i) O que é + por que.**
**SICONFI** (Sistema de Informações Contábeis e Fiscais do Setor Público, da **STN/Tesouro Nacional**) recebe a **MSC (Matriz de Saldos Contábeis)**, o **DCA (Declaração de Contas Anuais)**, **RREO** e **RGF**. Nosso Finanças/Transparência já produz **MSC e DCASP** parametrizados. É **federal** (vale para qualquer ente, não só RS) e condiciona transferências/CAUC.

**(ii) Como obter.**
- Órgão: **STN / SICONFI** (portal SICONFI). Operação: **Contador**; cadastro autorizado pelo **Gestor**.
- Passos: (1) **cadastrar o ente e os usuários/responsáveis** no SICONFI (perfil declarante), com certificado digital; (2) homologar o **leiaute da MSC vigente**; (3) gerar a MSC pelo nosso sistema e enviar (via webservice/upload) nos prazos; (4) validar DCASP/RREO/RGF.
- Documentos: A1 (item 1), dados do ente e do contador responsável.

**(iii) Lead time + custo.**
**Sem custo.** Cadastro: **~1 a 2 semanas**. Homologação de leiaute MSC: **~1 a 3 semanas**. **CONFIRMAR layout MSC vigente e prazos no portal SICONFI** (mudam por exercício).

**(iv) Dependências.** **Item 1 (A1)**; contabilidade PCASP/MCASP populada em Finanças.

**(v) O que destrava.** Envio de MSC/DCASP/RREO/RGF à STN (cumprimento LRF federal + CAUC). Roda em paralelo ao TCE-RS.

---

## 5. Azure (assinatura, tenant, infra de produção)

**(i) O que é + por que.**
A infraestrutura de produção: **assinatura Azure**, **tenant (Entra ID)** e os recursos (App Service/Container Apps, SQL, **Azure Key Vault**). O Key Vault é onde a **KEK do Cofre** (item 1) vive em produção (`ProvedorKekKeyVault`) — hoje o código roda em **modo DEV (Config)** e a virada para PROD é trocar o provedor por `KeyVault` (já implementado, falta a infra real). Sem Azure provisionado **não há onde guardar a KEK em produção nem onde hospedar**.

**(ii) Como obter.**
- Órgão/fornecedor: **Microsoft Azure** (cartão corporativo, parceiro CSP, ou — se aplicável — **convênio de compra pública/ata**). Operação: **TI / dono do produto**.
- Passos: (1) criar/abrir a **assinatura** (pagamento ou CSP); (2) criar o **tenant Entra ID** e o resource group de produção; (3) provisionar **Key Vault, SQL, App/Container host, observabilidade**; (4) o Bicep/infra do repo (`infra/`) já descreve os recursos — aplicar; (5) configurar `Cofre:ProvedorKek=KeyVault` + `KeyVaultUri` + nome da KEK; criar a KEK no Key Vault.
- Documentos: dados de faturamento; se compra pública, processo licitatório/ata/convênio.

**(iii) Lead time + custo.**
Abertura de assinatura via cartão: **horas a 1 dia**. Via **compra pública/convênio: semanas** (depende do processo do município — pode virar o gargalo se exigir licitação). Custo: **OPEX mensal** conforme consumo (estimar dimensionamento). **CONFIRMAR a via de contratação aceita pelo município** (cartão corporativo vs. licitação/ata).

**(iv) Dependências.** Nenhuma técnica anterior, mas se a contratação for por compra pública, depende do rito licitatório. **Item 1 (A1)** é guardado **dentro** do Key Vault, então 5 viabiliza 1 em produção.

**(v) O que destrava.** Cofre A1 em **modo PROD** (KEK no Key Vault), hospedagem de produção, segredos fora do repo. **Pré-requisito de go-live.** Se a via for licitação, **tem lead-time longo → começar JÁ.**

---

## 6. Adesão à Rede gov.br + Relying Party (login do cidadão)

**(i) O que é + por que.**
Para o cidadão logar no Portal com a conta **gov.br** (login único, OIDC), o município precisa **aderir à Rede gov.br** e registrar o sistema como **Relying Party** (obter `client_id`/`client_secret` e os endpoints de authorization/token/userinfo + JWKS). No código, a porta `IProvedorIdentidadeGovBr` está **stub** (`ProvedorIdentidadeGovBrStub`); o `// TODO(M10-creds)` em `IProvedorIdentidadeGovBr.cs` registra explicitamente: **"a adesão do município à Rede gov.br e o client_id/secret são BLOQUEADORES EXTERNOS"**. Até lá, o **login local (CPF/CNPJ + senha) cobre 100% do portal** — o gov.br é incremento, não bloqueio do PoC.

**(ii) Como obter.**
- Órgão: **gov.br / Ministério da Gestão (SGD)** — processo de adesão de órgão à Rede gov.br e solicitação de integração como provedor de serviço (Relying Party). Operação: **dono do produto + TI**, com **anuência do gestor** do município (é o município que adere).
- Passos: (1) o **município solicita adesão** à Rede gov.br (formulário/processo oficial do SGD); (2) cadastrar o serviço/sistema e solicitar credenciais OIDC; (3) configurar URLs de redirect (callback) homologadas; (4) receber `client_id`/`client_secret`, endpoints e JWKS; (5) trocar o stub pelo provedor real e validar o `id_token` (CPF + selo bronze/prata/ouro).
- Documentos: identificação do órgão aderente, responsável técnico, URLs do serviço.

**(iii) Lead time + custo.**
**Sem custo.** Lead-time **burocrático longo e variável: ~3 a 8+ semanas** (processo de adesão de órgão + aprovação). Por ser **dependente de aprovação de terceiro (SGD)**, é dos **primeiros a iniciar**. **CONFIRMAR o processo de adesão e os endpoints OIDC vigentes** no portal de integração gov.br.

**(iv) Dependências.** Anuência formal do município (é o ente quem adere). URLs de produção (relaciona-se com item 5).

**(v) O que destrava.** Login do cidadão via gov.br no Portal (e selos de confiabilidade para atos "prata"/"ouro"). Não bloqueia o PoC (login local existe), mas tem **lead-time alto → iniciar cedo**.

---

## 7. PNCP (Lei 14.133 — publicação de contratos/editais)

**(i) O que é + por que.**
O **Portal Nacional de Contratações Públicas (PNCP)** é **condição de eficácia** dos contratos/editais sob a Lei 14.133/2021 (art. 174 e divulgação obrigatória). Nosso Administração tem `PublicarContratoNoPncp` e `PublicarEditalNoPncp` (e o evento `ContratoPublicadoPncpIntegrationEvent`) prontos; hoje recebem o número PNCP manualmente / via gateway a integrar.

**(ii) Como obter.**
- Órgão: **PNCP** (gerido pelo Governo Federal / Compras.gov). Operação: **pregoeiro/setor de contratos**, com cadastro do ente.
- Passos: (1) **cadastrar o ente e os usuários** no PNCP (perfil de ente público); (2) obter credenciais de **API/integração** do PNCP; (3) configurar no sistema; (4) publicar editais/contratos e capturar o número PNCP retornado.
- Documentos: cadastro do ente, responsável pelas contratações, A1 (item 1) se exigido para autenticação na API.

**(iii) Lead time + custo.**
**Sem custo.** Cadastro: **~1 a 2 semanas**; integração de API: dias após credenciais. **CONFIRMAR o manual/versão de API do PNCP vigente** (a API evolui).

**(iv) Dependências.** Cadastro do ente no PNCP; possivelmente A1 para a API. Dados de contratos/licitações em Administração.

**(v) O que destrava.** Publicação automática de contratos/editais no PNCP (eficácia legal 14.133). Lead-time **médio**.

---

## 8. Banco arrecadador / convênio de arrecadação (DAM/tributos)

**(i) O que é + por que.**
Para que o cidadão **pague tributos** (IPTU/ISS/ITBI/taxas/Dívida Ativa) via DAM (Documento de Arrecadação Municipal) com **código de barras/linha digitável (padrão FEBRABAN) e/ou Pix**, e o município **reconcilie os pagamentos**, é preciso um **convênio de arrecadação** com um **banco arrecadador** (e o **retorno bancário CNAB** para baixa automática). Nosso Tributos emite os lançamentos e a guia; a numeração do convênio (cedente/carteira) e o arquivo-retorno entram aqui.

**(ii) Como obter.**
- Órgão: **banco** (Banrisul é o usual no RS para entes; também BB, Caixa). Operação: **Tesouraria/Finanças** + **gestor** (assina o convênio).
- Passos: (1) abrir/usar **conta de arrecadação** do ente; (2) firmar **convênio de arrecadação** (define número do convênio/cedente, carteira, faixa de nosso-número, layout do código de barras e Pix); (3) homologar a **geração da linha digitável/QR Pix** e o **retorno CNAB** (240/400) com o banco; (4) configurar no sistema e conciliar.
- Documentos: dados bancários do ente, ato do gestor, CNPJ, convênio assinado.

**(iii) Lead time + custo.**
Convênio: **~2 a 4 semanas** (negociação + homologação de layout com o banco). Pode haver **tarifa por documento arrecadado** (confirmar). **CONFIRMAR layout de código de barras/Pix e CNAB com o banco escolhido.**

**(iv) Dependências.** Conta de arrecadação do ente; relação com o banco. Independe dos demais insumos técnicos.

**(v) O que destrava.** Emissão de DAM pagável + baixa automática (conciliação) em Tributos/Dívida Ativa. Lead-time **médio**.

---

## 9. Saúde: RNDS / SI-PNI / CNES / HÓRUS (DATASUS)

**(i) O que é + por que.**
Integrações federais de Saúde (DATASUS/Ministério da Saúde): **CNES** (cadastro de estabelecimentos/profissionais — base dos demais), **SI-PNI/RNDS** (imunização → registro de doses na Rede Nacional de Dados em Saúde), **HÓRUS/SNGPC** (assistência farmacêutica e medicamentos controlados). Os marcadores no código: `RegistrarDose.cs` → "transmissão ao SI-PNI/RNDS via Outbox **após A1**"; `DispensarMedicamento.cs` → "escrituração SNGPC e integração HÓRUS para itens controlados"; `SaudeEndpointsVigilancia.cs` → "SINAVISA/e-SUS VS **após credencial estadual/DATASUS**". **Dados sensíveis (LGPD)** — só transmitir com base legal e trilha.

**(ii) Como obter.**
- Órgão: **DATASUS / Ministério da Saúde** (RNDS exige credenciamento e **certificado**), **CNES** (gestão municipal de saúde), nível **estadual (SES-RS)** para vigilância. Operação: **Secretaria Municipal de Saúde** + TI.
- Passos: (1) garantir o ente/estabelecimentos no **CNES**; (2) **credenciar na RNDS** (solicitação de acesso + certificado/token de produção do DATASUS); (3) habilitar SI-PNI/HÓRUS conforme o módulo; (4) homologar e ligar a transmissão (Outbox) atrás do ACL.
- Documentos: dados do gestor de saúde, CNES dos estabelecimentos, A1/certificado conforme exigência da RNDS.

**(iii) Lead time + custo.**
**Sem custo.** Credenciamento RNDS/DATASUS: **~3 a 8 semanas** (varia muito; envolve aprovação federal/estadual). **CONFIRMAR o processo de credenciamento RNDS e os requisitos de certificado vigentes.**

**(iv) Dependências.** CNES em dia; **item 1 (A1)** ou certificado específico DATASUS; base legal LGPD. Não é caminho crítico do PoC fiscal (Saúde roda local).

**(v) O que destrava.** Transmissão real de imunização (RNDS/SI-PNI), farmácia controlada (HÓRUS/SNGPC), vigilância. **Onda fora do núcleo fiscal — priorizar conforme o módulo Saúde estiver no escopo do tenant.**

---

## 10. Educação: Educacenso/INEP + SIOPE + PNAE/PNATE (FNDE)

**(i) O que é + por que.**
**Educacenso/INEP** (Censo Escolar — base de repasses), **SIOPE** (gastos em educação — FNDE/MEC), **PNAE/PNATE** (merenda e transporte escolar — prestação ao **FNDE**). Nosso Educação já modela Aluno/Turma/Matrícula e PNAE/PNATE.

**(ii) Como obter.**
- Órgão: **INEP** (Educacenso), **FNDE/MEC** (SIOPE, PNAE, PNATE). Operação: **Secretaria Municipal de Educação**.
- Passos: (1) acesso de declarante do município no **Educacenso** (período anual definido pelo INEP); (2) credenciais **SIOPE** (FNDE); (3) cadastro nos sistemas de prestação **PNAE/PNATE** do FNDE; (4) gerar/enviar conforme o calendário de cada um.
- Documentos: dados da Secretaria de Educação, responsáveis, CNPJ.

**(iii) Lead time + custo.**
**Sem custo.** Acesso aos sistemas: **~1 a 3 semanas**. **Atenção: Educacenso tem janela anual fixa** — confirmar o período. **CONFIRMAR calendários INEP/FNDE vigentes.**

**(iv) Dependências.** Dados escolares populados; sem dependência dos insumos fiscais.

**(v) O que destrava.** Envio Educacenso/SIOPE e prestação PNAE/PNATE. **Fora do núcleo fiscal — priorizar conforme escopo do tenant.**

---

## 11. Assistência: CadÚnico / SAGI / Transferegov (MDS)

**(i) O que é + por que.**
**CadÚnico** (base de famílias — elegibilidade de benefícios), **SAGI/Censo SUAS** (questionário anual das unidades socioassistenciais), **SICON/CECAD** (condicionalidades do PBF e IGD), **Transferegov** (transferências federais). Os `// TODO(M10)` no código: `UnidadeSocioassistencial.cs` → "envio do Censo SUAS ao SAGI/MDS — **requer credencial MDS**"; `AcompanhamentoCondicionalidade.cs` → "sincronizar com SICON/CECAD do MDS"; `CalculadoraIgd.cs` → "substituir fatores locais pelos componentes oficiais do IGD".

**(ii) Como obter.**
- Órgão: **MDS** (Ministério do Desenvolvimento e Assistência Social). Operação: **Secretaria Municipal de Assistência Social** (gestor do SUAS municipal).
- Passos: (1) habilitar o gestor municipal nos sistemas do MDS (CadÚnico/CECAD, SAGI, Censo SUAS, Transferegov); (2) obter credenciais de acesso/consulta; (3) homologar importação/sincronização atrás do ACL.
- Documentos: dados do gestor SUAS, NIS/CRAS/CREAS cadastrados, CNPJ.

**(iii) Lead time + custo.**
**Sem custo.** Habilitação de acesso MDS: **~2 a 4 semanas**. Censo SUAS tem **janela anual**. **CONFIRMAR os sistemas e o questionário SUAS vigentes.**

**(iv) Dependências.** Unidades/famílias cadastradas; sem dependência dos insumos fiscais.

**(v) O que destrava.** Censo SUAS (SAGI), condicionalidades PBF (SICON/CECAD), IGD oficial. **Fora do núcleo fiscal — priorizar conforme escopo.**

---

## 12. ACT credenciada ICP-Brasil (carimbo de tempo RFC 3161 — Protocolo)

**(i) O que é + por que.**
**Carimbo de tempo** (timestamp confiável RFC 3161) emitido por uma **ACT — Autoridade de Carimbo do Tempo credenciada ICP-Brasil** dá **data/hora com fé pública** aos documentos assinados no Protocolo (Lei 14.063/2020) — prova de existência num instante, à prova de adulteração. No código, `ProtocoloModule.cs` registra hoje `CarimboDeTempoLocalService` (carimbo local, para DEV/PoC); o `CarimboDeTempo` (ValueObject) já guarda `InstanteUtc` + `Autoridade`. Em produção, trocar pelo cliente que chama a **ACT real**.

**(ii) Como obter.**
- Órgão: **ACT credenciada ICP-Brasil** (ex.: Serpro, Bry, Valid e outras — confirmar lista de ACTs credenciadas no ITI). Operação: **TI / dono do produto**.
- Passos: (1) contratar serviço de carimbo de tempo de uma ACT; (2) receber credenciais/endpoint TSA (RFC 3161); (3) configurar o cliente no Protocolo e validar a emissão/verificação do token de tempo.
- Documentos: contrato com a ACT, dados do ente.

**(iii) Lead time + custo.**
Contratação: **~1 a 3 semanas**. Custo: **por carimbo** ou pacote/assinatura (confirmar com a ACT — costuma ser barato por unidade). **CONFIRMAR a lista de ACTs credenciadas e o protocolo TSA vigente.**

**(iv) Dependências.** Independe do A1 para a chamada TSA, mas anda junto com a assinatura de documentos (item 1). Não bloqueia o PoC (carimbo local existe).

**(v) O que destrava.** Carimbo de tempo com fé pública nos documentos do Protocolo. Lead-time **baixo-médio**; **não é caminho crítico do PoC fiscal.**

---

## TABELA DE PRIORIDADE / ORDEM

Coluna "Lead-time": estimativa burocrática total até estar **utilizável em produção** (cadastro + homologação). "Criticidade PoC fiscal": peso para a meta nº1 (prestação ao TCE-RS / go-live). Todos os prazos são **estimativas — CONFIRMAR oficialmente.**

| Ordem | Insumo | Lead-time estimado | Custo | Depende de | Criticidade p/ PoC fiscal | Quem assina/opera |
|---|---|---|---|---|---|---|
| **1º** | **6. Adesão gov.br + Relying Party** | **3–8+ sem** (aprovação SGD) | — | Anuência do município | Média (não bloqueia PoC) | Dono + TI / Gestor |
| **2º** | **2. eSocial Restrita → Produção** | **2–6 sem** (homologação) | — | A1 (item 1) | Alta | RH + Contador |
| **3º** | **3. TCE-RS (e-Validador/PAD + leiaute MT + responsável)** | **3–9 sem** (cadastro + bater números) | — | A1; dados Finanças/RH | **CRÍTICA (meta nº1)** | Contador/Controlador / Gestor |
| **4º** | **1. Certificado A1 e-CNPJ** | **3–10 dias úteis** | ~R$ 200–450/ano | — (fundação) | **CRÍTICA (pré-req de 2,3,4,12)** | Prefeito / procurador |
| **5º** | **5. Azure (assinatura + Key Vault + infra)** | **horas (cartão)** a **semanas (licitação)** | OPEX mensal | — (licitação se aplicável) | Alta (go-live PROD) | TI / Dono / Gestor |
| **6º** | **9. Saúde RNDS/DATASUS** | **3–8 sem** | — | A1/cert DATASUS, CNES | Baixa (fora do núcleo) | Sec. Saúde + TI |
| **7º** | **8. Banco arrecadador (DAM)** | **2–4 sem** | tarifa/doc | conta do ente | Média (receita própria) | Tesouraria / Gestor |
| **8º** | **11. Assistência MDS (CadÚnico/SAGI)** | **2–4 sem** | — | acesso MDS | Baixa (fora do núcleo) | Sec. Assistência |
| **9º** | **10. Educação INEP/FNDE** | **1–3 sem** (+ janela anual) | — | dados escolares | Baixa (fora do núcleo) | Sec. Educação |
| **10º** | **4. SICONFI/STN (MSC/DCASP)** | **1–3 sem** (cadastro+leiaute) | — | A1; PCASP | Alta (LRF federal) | Contador |
| **11º** | **7. PNCP (14.133)** | **1–2 sem** | — | cadastro ente; A1 | Média (eficácia contratos) | Pregoeiro |
| **12º** | **12. ACT carimbo de tempo (RFC 3161)** | **1–3 sem** | por carimbo | — | Baixa (carimbo local cobre PoC) | TI / Dono |

> A "Ordem" mistura **lead-time** e **dependência**: o A1 (item 1) tem lead-time curto, mas é **pré-requisito de quase tudo**, então deve ser **emitido em paralelo, imediatamente**, mesmo aparecendo na 4ª linha. Itens 6, 2 e 3 lideram por terem **lead-time longo dependente de terceiros**.

---

## RESPOSTA — LISTA PRIORIZADA + OS 3 DE MAIOR LEAD-TIME

**Sequência recomendada de início (do que travar JÁ ao que é rápido):**

1. **Adesão à Rede gov.br + Relying Party (item 6)** — aprovação do SGD, 3–8+ semanas, terceiro.
2. **eSocial Produção Restrita (item 2)** — começar a homologar leiaute já (2–6 semanas de ciclo).
3. **TCE-RS: cadastro do responsável + e-Validador/PAD + leiaute MT (item 3)** — meta nº1, 3–9 semanas.
4. **Certificado A1 e-CNPJ (item 1)** — emitir EM PARALELO desde o dia 1 (pré-requisito de 2, 3, 4 e 12).
5. **Azure + Key Vault (item 5)** — iniciar já se a via for compra pública/licitação (lead-time imprevisível).
6. **SICONFI/STN MSC/DCASP (item 4)** e **PNCP (item 7)** — cadastros federais, médio prazo.
7. **Banco arrecadador / DAM (item 8)** — convênio, 2–4 semanas.
8. **Saúde DATASUS/RNDS (9)**, **Assistência MDS (11)**, **Educação INEP/FNDE (10)** — conforme os módulos no escopo do tenant (fora do núcleo fiscal).
9. **ACT carimbo de tempo (item 12)** — rápido e não bloqueia PoC (carimbo local cobre).

**OS 3 DE MAIOR LEAD-TIME PARA O DONO INICIAR PRIMEIRO (gargalos por dependerem de aprovação de terceiros):**

1. **Adesão à Rede gov.br + Relying Party** — 3–8+ semanas, depende da aprovação do SGD; é bloqueador externo do login do cidadão (já marcado assim no código). **Protocolar o pedido de adesão do município agora.**
2. **TCE-RS (cadastro do responsável + homologação do leiaute MT vigente)** — 3–9 semanas; é a **meta nº1** e o **maior risco da PoC oficial** (layout exato SIAPC/MSC). **Iniciar cadastro e baixar o e-Validador/leiaute do exercício imediatamente.**
3. **eSocial Produção Restrita** — 2–6 semanas de ciclo de homologação até zerar erros e bater a folha. **Começar a transmitir na restrita assim que o A1 sair.**

> E, transversalmente: **emitir o Certificado A1 e-CNPJ no dia 1** — lead-time curto (3–10 dias), mas é a **fundação que destrava 2, 3, 4 e 12**; sem ele, os três gargalos acima não avançam.
