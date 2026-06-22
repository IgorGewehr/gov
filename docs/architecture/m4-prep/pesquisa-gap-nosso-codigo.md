# M4 — Gap entre NOSSO CÓDIGO e a transmissão REAL ao TCE-RS (SIAPC/PAD)

> Análise do módulo `Transparencia` (estado em 2026-06) contra as specs oficiais já levantadas
> (`docs/architecture/specs-oficiais/tce-rs-siapc-pad.md`) e fontes oficiais do TCE-RS.
> REGRA DE OURO (CLAUDE.md §16): nada de leiaute/protocolo inventado. Toda afirmação factual tem
> FONTE; o que não foi extraído byte-a-byte está marcado `[a confirmar — obter doc oficial]`.

---

## 1. O QUE JÁ EXISTE NO CÓDIGO (achados)

### 1.1 Domínio (`...Domain/RemessasTce/`)
- **`RemessaTce`** (raiz de agregado, `IMustHaveTenant`): ciclo `Gerada → Validada → Enviada → Homologada/Rejeitada` com invariantes I-1..I-12. Factory `GerarRemessa` calcula `HashIntegridade` (SHA-256) na geração; `RegistrarResultadoValidacao` (aplica RDI), `EnviarTce` (exige `Validada` + hash íntegro), `Homologar`, `VencerPrazo` (alerta LRF art. 23 §3º, idempotente).
- **`ArquivoRemessa`** (entidade-filha): `NomeArquivo`, `Conteudo` (`ReadOnlyMemory<byte>`), `Hash` próprio, coleção de `RegistroLeiaute`.
- **`RegistroLeiaute`** (entidade-filha): apenas dois campos genéricos — `Tipo` (string) e `Conteudo` (string livre). **Não há tipagem de campos/posições/larguras.**
- **`Leiaute`** (VO): `Codigo` + `Versao` (strings livres). Sem grade de campos.
- **`HashIntegridade`** (VO): SHA-256 sobre `ReadOnlySpan<byte>`, `Calcular`/`Confere`. Sólido e reaproveitável.
- **`ResultadoValidacao`** (RDI) + **`OcorrenciaValidacao`** (arquivo/linha/severidade/mensagem; Erro bloqueia, Aviso não).
- VOs de período: `Periodo`, `Competencia`, `Quadrimestre`, `Bimestre`, `ValorMonetario`.
- Eventos de domínio: `RemessaGerada/Validada/Rejeitada/EnviadaTce/Homologada/PrazoRemessaVencido`.

### 1.2 Aplicação (`...Application/RemessasTce/`)
Handlers CQRS completos: `GerarRemessaTce`, `ValidarRemessaTce`, `EnviarRemessaTce`, `HomologarRemessaTce`, `VencerPrazoRemessaTce`, queries `ListarRemessasTcePorPeriodo`/`ObterRemessaTcePorId`.
Portas (ACL): `ISiapcPadGateway`, `IEValidadorTce`, `ILeiauteCatalogo`, `ISiconfiGateway`, `ICalendarioFiscal`, repositórios.

### 1.3 `EnviarRemessaTce` — STUB confirmado
- Handler chama `siapcPad.TransmitirAsync(remessa, ct)` e depois `remessa.EnviarTce(...)`. A transmissão real **não existe**.
- `ConsolidarConteudo` assume **pacote de UM único arquivo** (`Arquivos.FirstOrDefault()`) — divergente do leiaute real (vários `.TXT` num único ZIP).
- `MontarPacote` (em `GerarRemessaTce`) gera **texto fake** `"{Tipo}|{Conteudo}"` em **UTF-8**, nome `*.txt` — não é o leiaute SIAPC.

### 1.4 Portas e-Validador / SIAPC-PAD — SIMULADAS
- **`SimuladoEValidadorTce`** (`IEValidadorTce`): retorna RDI sempre sem erro se houver ≥1 arquivo. Não executa validação real.
- **`SimuladoSiapcPadGateway`** (`ISiapcPadGateway`): `Task.CompletedTask`, sem efeito externo. Doc da porta diz que a impl. real "assina com A1 do tenant (Key Vault) + Polly".
- Demais simulados: `SimuladoSiconfiGateway`, `SimuladoCalendarioFiscal`, `SimuladoLeiauteCatalogo`, `SimuladoPublic...`.

---

## 2. GAP PARA A TRANSMISSÃO REAL (o que falta)

### 2.1 LEIAUTE campo-a-campo (gap MAIOR)
O código trata registro como string opaca. O leiaute SIAPC é **posicional, byte-a-byte**. Falta:
- **Codificação/forma:** texto **ASCII ISO-8859-1 (Latin-1)**, registros de **largura fixa**, terminador **CR/LF (0D0A)** — hoje geramos UTF-8 com `\n`. **FONTE:** MT SIAPC Vol. V §2 (specs-oficiais §2).
- **Tipos de campo:** Caractere (esq., espaços à dir.), Numérico (dir., zeros à esq.), **Valor** (centavos, sinal `+`=2B/`-`=2D à esq.), **Data** `ddmmaaaa`. Não modelados. **FONTE:** Vol. V §2.1.
- **Cabeçalho obrigatório (1ª linha, 130 bytes):** CNPJ(14)+DataIni(8)+DataFim(8)+DataGer(8)+Nome(80)+CodRemessa(12). Não existe. **FONTE:** Vol. V §2.2.
- **Finalizador:** `FINALIZADOR` + qtd registros (10). Não existe. **FONTE:** Vol. V §2.2.
- **Nome do ZIP (60 bytes):** `CNPJ.DataIni.DataFim.DataGer.Tipo.CodRemessa.zip` (Tipo: P/C/A/F/E/S/O). Não existe — empacotamento ZIP ausente. **FONTE:** Vol. V §2.3.
- **Grade completa dos arquivos (.TXT):** orçamento/balancetes/empenho/receita-despesa (Vol. I–IV) + complementares (4810 Folha, 4820 Cadastro, 4960 rubricas, 4010/4011 receita, etc.). Apenas 4810/4820 têm início transcrito. `[a confirmar — obter doc oficial: MT SIAPC Vol. I–V exercício 2026 + Tabela do PAD]`
- **Catálogo de leiaute por exercício:** `Leiaute.Versao` é string livre; falta mapear código→grade real por exercício (a Revisão muda anualmente). `[a confirmar — versão do PAD 2026]`

### 2.2 VALIDAÇÃO
- `SimuladoEValidadorTce` não valida. Validação real ocorre no **PAD/SICOE local** do ente, que produz o **RVE** (Relatório de Validação e Envio). **Decisão arquitetural pendente:** não há API/SDK público do PAD — o validador é desktop instalado no ente. Opções: (a) replicar regras de consistência lógico-contábil internamente (alto custo, risco de divergência) ou (b) tratar geração de arquivos + import do RVE como artefato externo. `[a confirmar — existência de validador/CLI integrável; obter Manual SICOE + Tabela do PAD]`
- Falta consistência contábil real (bater com PCASP/MCASP do módulo Financas) — hoje `OcorrenciaValidacao` só carrega mensagens, sem motor de regras.

### 2.3 ASSINATURA (divergência arquitetural CRÍTICA)
- Código/porta assumem: gateway **assina o pacote com certificado A1 do tenant (Azure Key Vault)**.
- Realidade SICOE/e-Protocolo: o **RVE é assinado pelo certificado ICP-Brasil PESSOAL** dos responsáveis (Responsável/Gestor + Contabilista; na Complementar, + Resp. Controle Interno + Resp. Folha), via **Sistema e-Protocolo do TCE-RS**. Não é assinatura de pacote por A1 institucional do tenant. **FONTE:** Manual SICOE; FAQ SIAPC §4 (specs-oficiais §4). `[a confirmar — formato de assinatura aceito: CAdES/PAdES/XML e se há assinatura server-side ou só interativa no e-Protocolo]`
- Implicação: o modelo "A1 no Key Vault assina automaticamente" pode **não atender**. Confirmar se SICOE aceita assinatura programática ou se exige ato interativo do responsável. **Decisão pendente.**

### 2.4 TRANSMISSÃO
- `ISiapcPadGateway` não tem protocolo real. O canal oficial é **SICOE** (Res. TCE-RS 1.074/2017; IN 08/2017), aplicação instalada no ente que gera/valida/transmite por Internet ao Processo Eletrônico. **FONTE:** specs-oficiais §1/§6.
- Faltam: endpoints/WSDL ou protocolo de transmissão, idempotência por código de remessa real, retorno de protocolo/recibo, mapeamento de erros do SICOE. `[a confirmar — obter Manual SICOE byte-a-byte; verificar se há API ou só client desktop]`
- `ILeiauteCatalogo.ObterDataLimiteAsync` precisa dos prazos reais (mensal até 30 dias corridos — Res. 1099/2018; RGF/MCI semestral p/ Maximiliano de Almeida, ≤50 mil hab.). **FONTE:** specs-oficiais §4.

---

## 3. PENDÊNCIAS `[a confirmar — obter doc oficial]`
1. **MT SIAPC Vol. I–V exercício 2026** (grade campo-a-campo de todos os `.TXT`) — portal TCE-RS / Manuais.
2. **Versão do PAD 2026** (sucessora da 20.0.0.0) e **Tabela do PAD** (contas→RREO/RGF).
3. **Manual SICOE / Res. 1.074/2017 / IN 08/2017** byte-a-byte — protocolo de transmissão e se há API server-side ou só client desktop.
4. **Formato de assinatura** aceito pelo e-Protocolo (CAdES/PAdES/XML) e viabilidade de assinatura programática com A1 vs. ato interativo do responsável com cert. pessoal.
5. **IN vigente RREO/RGF** (IN 4/2021 vs. posterior) e Resolução base do Vol. V no exercício.

## 4. FONTES (URLs oficiais)
- MT SIAPC Vol. V (transcrito): http://www.tce.rs.gov.br/sistemas_controle/SIAPC/pdf/MT_Vol_V_Arq_DispTCE_4320.pdf · espelho https://mpc.rs.gov.br/repo/SIAPC/MANUAL/MT-ASCE-0105-06-MT-Volume-V.pdf
- MT SIAPC Vol. II (PAD): https://tcers.tc.br/repo/SIAPC/MANUAL/MT_Vol_II_SiapcPAD_6404.pdf
- Manual SICOE: https://tcers.tc.br/repo/cex/sicoe/manual-sicoe.pdf
- FAQ SIAPC (periodicidade/assinaturas/Res.1099): http://www.tce.rs.gov.br/sistemas_controle/SIAPC/pdf/perguntas_frequentes.pdf
- Portal Sistemas de Controle Externo (Manuais/PAD por exercício): https://portalnovo.tce.rs.gov.br/sistemas-de-controle-externo/
- Validador de assinaturas ITI/ICP-Brasil: https://validar.iti.gov.br/
