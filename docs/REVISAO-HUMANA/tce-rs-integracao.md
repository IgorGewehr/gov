# Revisão de Integração e Transmissão — para a Ligação TCE-RS

> **O que esta página é.** O mapa de **toda transmissão a sistema externo**: cada **gerador de
> leiaute** (com a norma/versão que ele segue) e **onde fica cada ponto de transmissão** que ainda
> espera credencial — os marcadores `// TODO(M10)` no código. É a página da pessoa que faz a ligação
> com o TCE-RS e cuida do envio.
>
> **Como o sistema funciona hoje (importante):** os geradores de leiaute **já produzem e validam
> localmente** os arquivos (estrutura, posições, hash, e-Validador/RDI). O que falta para "ligar" é a
> **credencial e o canal de transmissão de produção** (certificado A1 no Azure Key Vault, login JWT do
> PNCP, etc.). Esses pontos estão marcados com `// TODO(M10)` — são **os interruptores de transmissão**,
> não buracos na lógica.
>
> Base normativa: [`docs/normas/FONTES-NORMATIVAS.md`](../normas/FONTES-NORMATIVAS.md) (seções B = TCE-RS/
> SICONFI; D = eSocial/EFD-Reinf; F = PNCP; G = MROSC/Transferegov).

---

## Parte 1 — Inventário de geradores de leiaute

Cada gerador é uma **função pura** (sem I/O): recebe os dados, monta o arquivo no leiaute oficial. A
"O QUE CONFERIR" é: a versão/norma do leiaute está correta e atualizada?

| # | Gerador | Arquivo | Leiaute / Norma (versão) | O QUE CONFERIR |
|---|---|---|---|---|
| 1 | **MSC → SICONFI** | [`GeradorMscCsv.cs`](../../src/Modules/Transparencia/Tensorroot.Gov.Modules.Transparencia.Infrastructure/Integracoes/GeradorMscCsv.cs) | MSC 2026 — **Portaria STN 642/2019, Anexo I** (CSV, pipe-separated) | Confira separador/ordem/códigos das tabelas TIPO contra o e-Validador do SICONFI (o próprio arquivo tem `TODO(M10-validate)` nisso). |
| 2 | **SIAPC/PAD → TCE-RS** (estrutura agregada) | [`LeiauteSiapc.cs`](../../src/Modules/Transparencia/Tensorroot.Gov.Modules.Transparencia.Domain/RemessasTce/Leiautes/LeiauteSiapc.cs) | SIAPC/PAD (muda em 2026: novos BP/BF + DFC) — MT-ASCE Vol. V | **Conferir leiaute 2026:** o arquivo tem `TODO(validar-leiaute-MT-2026)` nos campos/posições exatos. |
| 3 | **SIAPC seed (grade estrutural)** | [`LeiauteSiapcSeed.cs`](../../src/Modules/Transparencia/Tensorroot.Gov.Modules.Transparencia.Infrastructure/Integracoes/Leiautes/LeiauteSiapcSeed.cs) | SIAPC 2026 para Prefeitura ('P') | Confira que a grade de produção (JSON/config versionada) substitui o esqueleto antes de produção. |
| 4 | **Folha → TCE-RS** (TCE_4810/4820/4960) | [`LeiauteFolhaTceSeed.cs`](../../src/Modules/Transparencia/Tensorroot.Gov.Modules.Transparencia.Infrastructure/Integracoes/Leiautes/LeiauteFolhaTceSeed.cs) | **Resolução TCE-RS 1099/2018** + MT-ASCE-0105 Vol. V (ISO-8859-1, posicional). 3 arquivos: TCE_4810 (lançamentos folha), TCE_4820 (cadastro servidores), TCE_4960 (rubricas/base legal) | **Conferir a grade posicional/tamanhos** do MT oficial (`TODO(validar-leiaute-MT-2026)`). |
| 5 | **LicitaCon → TCE-RS** | [`GeradorRemessaLicitaCon.cs`](../../src/Modules/Administracao/Tensorroot.Gov.Modules.Administracao.Application/LicitaCon/GeradorRemessaLicitaCon.cs) | **LicitaCon 1.4** (e-Validador TCE-RS) — 14 arquivos CSV (UTF-8/BOM, vírgula, CRLF) | Confira os 14 arquivos contra o dataset real do TCE-RS; transmissão ao Processo Eletrônico é `TODO(M10)`. |
| 6 | **eSocial (folha)** | [`GeradorEventosESocial.cs`](../../src/Modules/RecursosHumanos/Tensorroot.Gov.Modules.RecursosHumanos.Domain/ESocial/Mapeamento/GeradorEventosESocial.cs) | **eSocial S-1.3** (S-1000/1005/1010/2200/2299/1200/1202/1210/1299) | O XML deve ser validado contra o **XSD oficial** antes de assinar/transmitir (`TODO(validar-oficial)` em campos exatos). |
| 7 | **eSocial (SST)** | [`GeradorEventosSst.cs`](../../src/Modules/RecursosHumanos/Tensorroot.Gov.Modules.RecursosHumanos.Domain/ESocial/Mapeamento/GeradorEventosSst.cs) | eSocial S-1.3 — **S-2210 (CAT), S-2220 (ASO), S-2240 (agentes nocivos)** | Confira campos/XSD de SST (`TODO(validar-oficial)`); tabelas 13/23/25/26/27 vêm dos insumos. |
| 8 | **SIAPES → TCE-RS** (admissões) | [`LeiauteSiapes.cs`](../../src/Modules/RecursosHumanos/Tensorroot.Gov.Modules.RecursosHumanos.Domain/SicapPessoal/LeiauteSiapes.cs) | **SIAPES/SIAPESweb** (TCE-RS — auditoria de atos de pessoal; Resolução 682/2004) — posicional, datas DDMMAAAA | **Conferir campos 26..57** (concurso/fundamentação) — `TODO(M10)`; transmissão real ao SIAPESweb pendente. |
| 9 | **SIAPES — mapeamento** | [`MapeamentoSiapes.cs`](../../src/Modules/RecursosHumanos/Tensorroot.Gov.Modules.RecursosHumanos.Domain/SicapPessoal/MapeamentoSiapes.cs) | SIAPES — TipoCargo → CD_TIPO_ATO + CD_REGIME_JURIDICO (Tabela 5) | Confira o mapeamento de regime jurídico; operador pode sobrepor por decisão judicial. |

> **Convenção de marcadores no código:**
> - `// TODO(M10)` = ponto de transmissão real esperando credencial/canal de produção.
> - `// TODO(validar-oficial)` / `// TODO(validar-leiaute-MT-2026)` / `// TODO(M10-validate)` = ponto
>   onde a **versão/campos do leiaute** precisam de conferência contra o documento oficial vigente.

---

## Parte 2 — Quadro dos pontos de transmissão `// TODO(M10)`, por sistema externo

**Total: 64 marcadores `// TODO(M10)` em 47 arquivos.** Cada um é onde a credencial de produção entra.
Agrupados pelo sistema externo / credencial que aguardam:

| Sistema externo (credencial) | Pontos | Onde estão (exemplos arquivo:linha) | O QUE CONFERIR |
|---|---|---|---|
| **PNCP** (JWT ~1h + pré-cadastro + Key Vault) | ~11 | `Administracao/.../Pncp/PncpGatewaySimulado.cs:16` · `.../Licitacoes/PublicarEditalNoPncp.cs:54` · `.../Contratos/PublicarContratoNoPncp.cs:85` · `.../Pca/GerenciarPca.cs:170` | Trocar o gateway **simulado** pelo `PncpGatewayHttp` real com credenciais. Confirmar Manual PNCP 2.3.5 e ambiente (treina.pncp.gov.br × produção). |
| **LicitaCon / Processo Eletrônico TCE-RS** (certificado A1) | ~8 | `Administracao/.../LicitaCon/GeradorRemessaLicitaCon.cs:9` · `.../GerarRemessaLicitaConQuery.cs:12` · `.../RemessaLicitaCon.cs:44` · `AdministracaoEndpoints.cs:244` | Transmissão dos 14 CSV ao TCE-RS com credenciamento; mapear documentos reais (CNPJ/CPF dos fornecedores). |
| **eSocial / Receita Federal** (A1 no Key Vault, SOAP/mTLS) | ~5 | `RecursosHumanos/.../ESocial/Mapeamento/GeradorEventosESocial.cs:12` · `.../GeradorEventosSst.cs:10` · `.../Application/ESocial/TransmitirEventosESocial.cs` | Validar XML contra XSD oficial; trocar gateway simulado por SOAP real; lote ≤ 50 eventos / ≤ 5 MB. |
| **SIAPES / SIAPESweb TCE-RS** (A1) | ~6 | `RecursosHumanos/.../SicapPessoal/RemessaSicapPessoal.cs:78,243` · `LeiauteSiapes.cs:68` · `.../Application/SicapPessoal/GerarRemessaSicap.cs:45` · `RecursosHumanosEndpoints.SicapPessoal.cs:59` | Integrar a transmissão real ao SIAPESweb (registra protocolo); completar campos do leiaute. |
| **SICONFI / MSC** (A1 quando exigido) | ~2 | `Transparencia/.../Integracoes/GeradorMscCsv.cs:23` · `LeiauteSiapc.cs:14` | Confirmar separador/ordem/códigos contra o e-Validador do SICONFI. |
| **TCE-RS SIAPC/PAD + Folha-TCE** (A1) | ~3 | `Transparencia/.../Leiautes/LeiauteSiapcSeed.cs:13,46` · `LeiauteFolhaTceSeed.cs:13` | Confirmar grade posicional MT 2026 antes de produção. |
| **Transferegov.br** (MROSC — creds/cert) | ~6 | `Convenios/.../Integracoes/SimuladoTransferegovGateway.cs:11,24,33` · `ITransferegovGateway.cs:33` · handlers de empenho/liquidação/pagamento | Trocar gateway simulado por HTTP real (Lei 13.019/2014); correlacionar empenho/liquidação/pagamento com convênio/parceria. |
| **ACT — Carimbo de Tempo (Protocolo)** (cert ACT no Key Vault) | ~3 | `Protocolo/.../Carimbo/CarimbadorDeTempoAct.cs` (várias linhas) · `ProtocoloModule.cs:74` | Integrar ACT credenciada real (RFC 3161); montar/validar TimeStampReq ASN.1/DER (BouncyCastle). |
| **Saúde** (SNGPC/HÓRUS/SI-PNI/RNDS/SINAVISA — DATASUS/estadual) | ~4 | `Saude/.../Farmacia/DispensarMedicamento.cs:23` · `.../Imunizacao/RegistrarDose.cs:16` · `SaudeEndpointsVigilancia.cs:15` · `Vigilancia/AutoVisa.cs:13` | Integrar escrituração SNGPC/HÓRUS e transmissão SI-PNI/RNDS após A1 (dado sensível LGPD). |
| **Assistência Social** (MDS/SAGI/SICON/CECAD) | ~5 | `AssistenciaSocial/.../Censo/UnidadeSocioassistencial.cs:79` · `FormularioCensoSuas.cs:38` · `Igd/CalculadoraIgd.cs:31` · `Pbf/AcompanhamentoCondicionalidade.cs:33` | Envio do Censo SUAS ao SAGI/MDS; componentes oficiais do IGD; calendário PBF do SICON/CECAD. |
| **Legislativo** (LexML / Receita Federal) | ~4 | `Legislativo/.../Lexml/ExportarNormaLexml.cs:17` · `IConsultaReceitaEmEscopoDedicado.cs:21` · `LegislativoModule.cs:72` | Transmissão à base LexML; consulta CNPJ real à Receita. |
| **Infraestrutura / Azure** (OTLP → Azure Monitor; Key Vault) | ~4 | `ApiHost/Program.cs:46` · `Observabilidade/ContextoCorrelacaoMiddleware.cs:27` · `EnriquecedorTenantSpanProcessor.cs:17` · `Provisioning/TenantProvisioner.cs:67` | Exportador de telemetria para Azure Monitor; rotação de connection string via Key Vault. |
| **Patrimônio (SICOE)** (A1) | ~1 | `Patrimonio/.../Obras/ConcluirObra.cs:109` | Gerar artefato/leiaute SICOE (remessa de obras ao TCE-RS). |
| **RH — PASEP / extrato OFX** | ~3 | `RecursosHumanos/.../Pasep/ApuracaoPasep.cs:26,109` · `Financas/.../Tesouraria/MovimentoFinanceiro.cs:110` | Transmissão/recolhimento real do PASEP; importação de extrato OFX. |
| **Parametrização / metadados** (config → tabela) | ~3 | `Convenios/.../Parametros/ConveniosParametrosConfiguracao.cs:14,54` · `Protocolo/.../Arquivistica/MetadadosArquivisticos.cs:9` | Evoluir parâmetros de IConfiguration para tabela versionada por tenant+vigência (ex.: Selic mensal). |

> Os números por sistema são aproximados (alguns arquivos têm mais de um marcador); o **total verificado
> é 64 ocorrências em 47 arquivos**. Para reauditar a qualquer momento:
> `grep -rn 'TODO(M10)' src | grep '\.cs:'`

---

## Parte 3 — O que TODA transmissão tem em comum (padrão do sistema)

Antes de "ligar" qualquer integração, confira que ela respeita o padrão da casa (CLAUDE.md §8/§11):

- **Certificado A1 (.pfx) por tenant**, sempre do **Azure Key Vault** — nunca no repositório.
- **Idempotência:** reenviar a mesma remessa não duplica no destino (chave por ID da remessa/evento).
- **Resiliência (Polly):** timeout + retry + circuit breaker em toda chamada externa.
- **Anti-Corruption Layer:** o domínio nunca fala o "dialeto" do sistema externo direto; há um gateway
  que traduz e mapeia erros.
- **Outbox:** os eventos de integração saem transacionalmente com o estado (não se perde nem duplica).
- **Validação local antes de enviar:** SIAPC/MSC passam pelo **e-Validador (RDI)**; eSocial valida
  **XSD**; CNAB240 valida 240 posições. Remessa com erro tem **envio bloqueado**.

---

## Resumo para a ligação TCE-RS

- **9 geradores de leiaute** já produzem e validam os arquivos localmente: MSC (SICONFI), SIAPC/PAD
  (TCE-RS), Folha-TCE (TCE_4810/4820/4960), LicitaCon 1.4 (TCE-RS), eSocial folha + SST, SIAPES
  (TCE-RS).
- **64 pontos `// TODO(M10)`** (em 47 arquivos) são os **interruptores de transmissão** que esperam
  credencial de produção — concentrados em **PNCP (~11)**, **LicitaCon (~8)**, **SIAPES (~6)**,
  **Transferegov (~6)**, **eSocial (~5)**, e os demais sistemas federais/estaduais.
- **Prioridade de conferência:** as versões de leiaute marcadas com `TODO(validar-leiaute-MT-2026)` /
  `TODO(validar-oficial)` (SIAPC 2026, Folha-TCE MT, MSC SICONFI, eSocial XSD, classificação tributária)
  — são onde o documento oficial vigente precisa ser confrontado com o que está no código.
