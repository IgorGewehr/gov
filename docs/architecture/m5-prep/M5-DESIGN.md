# M5 — DESIGN (RH completo: folha + eSocial + ponto + remessa TCE-RS)

> **Arquiteto:** M5. **Status:** design pronto-para-implementar.
> **Piloto:** Maximiliano de Almeida/RS (administração pública direta, Grupo 4 do eSocial).
> **Regra de ouro (CLAUDE.md §16):** nenhuma alíquota/faixa/regra de leiaute é *hardcoded* nem inventada.
> Toda afirmação factual tem **FONTE (URL)** ou está marcada `[a confirmar — <doc oficial>]`.
> **Fontes desta base:** os 5 `pesquisa-*.md` + 3 `verificacao-*.md` deste diretório (verificação adversarial 2026-06-22) + código existente (`Modules/RecursosHumanos`, `Modules/Cofre`, `Modules/Transparencia`).

---

## Resumo executivo (≤15 linhas)

1. **Reaproveitar, não recriar.** O módulo `RecursosHumanos` já tem agregados ricos (`Servidor` ciclo nomeação→posse→exercício→estabilidade→desligamento; `Cargo`; `FolhaDePagamento` aberta→calculada→fechada→paga com abate-teto). A assinatura A1 server-side **já está pronta** no módulo `Cofre` (`IServicoAssinaturaDigital`: XMLDSig enveloped, C14N inclusiva, RSA-SHA256, KeyInfo EndCertOnly, `ReferenceUri=""` — exatamente o que o MOS Desenvolvedor v1.10/v1.15 exige). **NÃO reescrever o assinador.**
2. **4 frentes, ordem de risco:** (A) domínio de folha **Rules-as-Code** (rubricas com incidências S-1010, motor INSS/RPPS/IRRF/consignado por competência parametrizada) → (B) **eSocial** (modelo de evento + geração XML + assinatura via Cofre + transmissão SOAP com Outbox idempotente + Polly + ACL) → (C) **ponto AFD/AEJ** (Portaria MTP 671/2021, assinatura CAdES via Cofre) → (D) **remessa folha TCE-RS** (gerador de arquivo posicional, transmissão **humana** via app PAD).
3. **Princípio dominante:** o motor aplica *fórmulas*; os *números* (INSS/IRRF/RPPS/margem) são **dado versionado por `competencia`+`tenantId`** (fail-closed sem tabela municipal). eSocial e TCE-RS são **pipelines distintos**: eSocial = web service online (assina + transmite + polling de recibo); TCE-RS = exportador de arquivo offline (gera + valida + operador faz upload).
4. **Maior risco:** codar leiaute por leitura de HTML/busca em vez de **MOS S-1.3 + Anexo I + XSD congelados**. Mitigação: versionar pacote eSocial por NT, validar contra **Produção Restrita** antes do go-live.
5. **Bloqueios duros (dados do município):** lei do RPPS (existe? alíquota?), lei de consignações, estatuto dos servidores, situação cadastral no eSocial produção. Sem eles → default RGPS + motor recusa cálculo previdenciário municipal.

---

## Caminho de implementação (ordem)

**Fase 0 — Congelar fontes oficiais (BLOQUEIA todo o resto).** Baixar e versionar: MOS S-1.3 (PDF + Anexo I Leiautes) + pacote **XSD** na NT exata; **MOS Desenvolvedor eSocial v1.15**; **Portaria MTP 671/2021** (texto DOU + Anexos AFD/AEJ); **Manual Técnico SIAPC Vol. V** vigente; tabelas INSS/IRRF 2026 (Portarias já publicadas). Sem isso, nenhuma regra de leiaute vira código.

**Fase A — Domínio de folha (Rules-as-Code).** É a fundação; eSocial e TCE-RS consomem o resultado.
**Fase B — eSocial** (depende de A: precisa de rubricas+remuneração apuradas).
**Fase C — Ponto AFD/AEJ** (independente de A/B; pode correr em paralelo após Fase 0).
**Fase D — Remessa TCE-RS** (depende de A: consome folha fechada).

Detalhe de cada fase abaixo.

---

## (1) Domínio de folha — rubricas, motor de cálculo, competência/13º/férias

### 1.1 Confiança
- **ALTA** na arquitetura (Rules-as-Code, parametrização por competência/tenant) — confirmada pela verificação adversarial (`verificacao-folha-calculo.md` §8: "alinhada à §7 e §16").
- **ALTA** nos números INSS/IRRF 2025 e 2026 (verificados com Portarias oficiais).
- **BAIXA/BLOQUEADA** nos parâmetros municipais (RPPS, margem, estatuto) — dependem de lei de Maximiliano de Almeida ainda não obtida.
- **MÉDIA** nos códigos de incidência S-1010 (estrutura confirmada; números das tabelas 03/20/21/23 a extrair do XSD/MOS).

### 1.2 O que já existe e o que muda
- `FolhaDePagamento` (raiz, estados Aberta/Calculada/Fechada/Paga, abate-teto CF art. 37 XI) — **mantém**. `Calcular()` hoje só soma e abate-teto; vai **delegar a um serviço de domínio `MotorDeCalculoFolha`** os descontos legais (INSS/RPPS/IRRF/consignado).
- `Rubrica` (hoje só `Codigo`, VO raso) — **enriquecer** para carregar a classificação S-1010 (ver 1.3). É a mudança estrutural central.
- `EventoFolha`, `BaseCalculo`, `LiquidoAPagar`, `Competencia` — **mantêm**.
- `Servidor` — **acrescentar** atributos exigidos por eSocial/TCE que hoje faltam: `codCateg` (eSocial), `tpRegPrev`, dados de TCE_4820 (setor, carga horária, situação incl. pensionista). Já tem CPF, Matricula, Regime, Dependentes.
- Abstrações já scaffoldadas a implementar: `IRubricaS1010Consulta`, `IServidorRegimeConsulta`, `IParametrosFolhaProvider`.

### 1.3 Modelo de rubrica (S-1010) — o coração do motor
Somar bases **por código de incidência, nunca por nome de rubrica** (`pesquisa-folha-calculo.md` §4). `Rubrica` passa a ser entidade de tabela (não VO efêmero), vigente por competência:

```
Rubrica {
  tenantId, codigo (≤30, S-1010), descricao,
  tpRubr            // 1=provento, 2=desconto, 3/4=informativo
  natRubr           // natureza — eSocial Tabela 03 [a confirmar — tabela oficial]
  codIncCP          // incidência previdenciária — eSocial Tabela 20/incidência CP [a confirmar]
  codIncIRRF        // incidência IRRF — eSocial Tabela 21 [a confirmar]
  codIncFGTS        // incidência FGTS — eSocial Tabela 23 [a confirmar]
  codIncCPRP        // incidência RPPS (servidor) [a confirmar — leiaute S-1202/Tabela RPPS]
  contaPlanoFolhaTce, baseLegal   // exigidos pela remessa TCE_4960
  vigenciaInicio, vigenciaFim
}
```
- Flags de base derivadas dos códigos: `IncideINSS/RPPS/IRRF/FGTS` calculadas a partir de `codIncCP/codIncCPRP/codIncIRRF/codIncFGTS` por um mapa **carregado do MOS/XSD** (não hardcode).
- Regras conhecidas confirmadas: rubrica informativa (`tpRubr∈{3,4}`) ⇒ `codIncCP=00` e `codIncIRRF=9`; `codIncFGTS∈{21,93}` só em S-2299/S-2399 ou `{remunPerAnt}` do S-1200 (`verificacao-esocial-eventos.md` §3.1 — CONFIRMADO).

### 1.4 Tabelas parametrizadas (seed = valores oficiais; nunca embutir no código)
```
TabelaContribuicaoPrevidenciaria { regime(RGPS|RPPS), competencia, tenantId?, faixas[(limite,aliquota)], teto }
TabelaIRRF                       { competencia, faixas[(limite,aliquota,parcelaDeduzir)], valorDependente, descontoSimplificado }
ParametroMargemConsignavel       { tenantId, competencia, percentualGlobal, sublimites{}, baseCalculo }
```
Seeds CONFIRMADOS (semear, versionado por competência):
- **INSS 2025** (Port. Interm. MPS/MF nº 6/2025): faixas 7,5/9/12/14%, teto R$ 8.157,41. FONTE: gov.br/inss.
- **INSS 2026** (Port. Interm. MPS/MF nº 13, 09/01/2026): até 1.621,00→7,5%; 1.621,01–2.902,84→9%; 2.902,85–4.354,27→12%; 4.354,28–8.475,55→14%; teto R$ 8.475,55. FONTE: `verificacao-folha-calculo.md` §1b (gov.br/inss).
- **IRRF mai–dez/2025** (MP 1.294/2025 → **Lei 15.191/2025**, *não* 15.270): isenção 2.428,80; parcelas 182,16/394,16/675,49/908,73; dependente 189,59; simplificado 607,20. FONTE: `verificacao-folha-calculo.md` §3.
- **IRRF jan–abr/2025:** isenção 2.259,20; parcelas 169,44/381,44/662,77/896,00; simplificado 564,80.

### 1.5 Motor de cálculo (serviço de domínio `MotorDeCalculoFolha`)
Fórmulas (números vêm das tabelas acima por competência):
- **INSS (RGPS):** cumulativo por faixa (cada faixa só sobre a parcela dentro dela), respeitando teto. Base = Σ rubricas com `IncideINSS`.
- **RPPS:** alíquota da **lei municipal** (mín. 14% se déficit atuarial — EC 103/2019 art. 9º §4º; pode ser menor sem déficit, nunca < RGPS). Base própria (pode divergir do RGPS). **Fail-closed: recusa cálculo se não houver tabela RPPS municipal carregada.**
- **IRRF:** `base = proventosTributáveis − (INSS/RPPS) − (Σdependentes×valor) − pensão`; comparar com `proventosTributáveis − simplificado`; usar **menor base**; `IRRF = base×alíquota − parcelaDeduzir`. **Regra 2026 (redutor Lei 15.270/2025) NÃO acoplar sem IN da Receita** (`verificacao-folha-calculo.md` §3 — INCERTO).
- **Consignados/margem:** `margemDisponível = baseMargem×percentualMáx − consignadosAtivos`; bloquear desconto facultativo que exceda. % e base = **lei municipal** (não herdar 40% federal).
- **13º:** tributação **exclusiva** na fonte (Lei 7.713/1988 art. 12-A) — base **separada** da folha mensal; INSS/RPPS e IRRF em evento de competência "13º". `[a confirmar — Planalto art. 12-A + proporcionalidade 1/12]`.
- **Férias + 1/3:** IRRF sobre o terço (tributado junto, não exclusivo); **incidência de CP do segurado sobre o 1/3 = flag parametrizável por rubrica** (STF Tema 985/STJ REsp 1.559.926 — `[a confirmar — parecer jurídico do ente]`).

### 1.6 Docs oficiais a obter
- MOS S-1.3 (Anexo I) + XSD → tabelas de incidência 03/20/21/23 + regras S-1010.
- **Lei previdenciária do RPPS de Maximiliano de Almeida/RS** (existe? alíquota servidor/patronal + base). Default sem lei = RGPS.
- **Lei/decreto municipal de consignações** (% e base da margem).
- **Estatuto dos servidores municipais** (férias, abono pecuniário, 13º proporcional, gratificações = rubricas locais).
- Lei 7.713/1988 art. 12-A (Planalto); IN Receita IRRF 2026.

---

## (2) Eventos eSocial — modelo + geração + assinatura A1 + transmissão WS

### 2.1 Confiança
- **ALTA** no roster de eventos e na ordem de dependência (verificados em leiautes S-1.3 oficiais).
- **ALTA** na assinatura: o `AssinadorXmlDsig` do Cofre **já implementa** o perfil CONFIRMADO no MOS Desenvolvedor v1.10 (`verificacao-esocial-transmissao.md` §4): C14N `REC-xml-c14n-20010315` (inclusiva, *não* exc-c14n), `rsa-sha256`, digest `xmlenc#sha256`, transforms enveloped+C14N, **`Reference URI=""`**, KeyInfo só `<X509Certificate>`, Base64. **Reconfirmar na v1.15.**
- **ALTA** no transporte: SOAP 1.1, lote sempre (`EnviarLoteEventos`), validação 2 níveis (protocolo síncrono → recibo assíncrono via `ConsultarLoteEventos`), limite **750 KB/lote** (não "50 eventos"), URLs prod/restrita confirmadas.
- **MÉDIA** no binding WS-Security exato e WSDL sob S-1.3 (confirmar v1.15).

### 2.2 Eventos no escopo do piloto (Grupo 4, RPPS) — todos CONFIRMADOS
| Classe | Eventos | Origem no domínio |
|---|---|---|
| Tabela (1º) | **S-1000** (órgão), **S-1005** (estabelecimentos), **S-1010** (rubricas), **S-1020** (lotações), **S-1070** (processos) | Configuração do tenant + tabela de Rubricas |
| Não-periódico | **S-2200** (admissão), **S-2300/S-2399** (TSVE início/término), **S-2299** (desligamento) | `Servidor` (eventos de domínio Admitido/Desligado) |
| Periódico | **S-1200** (remun. RGPS), **S-1202** (remun. RPPS), **S-1207** (benefícios RPPS — se paga inativos), **S-1210** (pagamentos), **S-1299** (fechamento), **S-1298** (reabertura) | `FolhaDePagamento` (Calculada→Fechada) |
| Retorno | **S-5001, S-5002, S-5003, S-5011, S-5012, S-5013** | persistir recibos/totalizadores |

- **Roteador S-1200 ↔ S-1202** (CONFIRMADO, `verificacao-esocial-eventos.md` §3): S-1202 SOMENTE `codCateg∈[301,302,303,304,306,307,309,310,312,314]` com `tpRegPrev∈[2,4]`; S-1200 para RGPS e servidores 3XX com `tpRegPrev∈[1,3]`. **`tpRegPrev`: 1=RGPS, 2=RPPS, 3=Exterior, 4=SPSMFA militares** (correção confirmada). Validar lista exata no XSD antes de codar (erro aqui rejeita a folha inteira).
- **SST (S-2210/2220/2240/2241/1060)** fora do MVP (não obrigatório p/ RPPS); registrar dependência se houver celetistas/RGPS.
- Ordem obrigatória: **S-1000 → S-1005/1010/1020/1070 → S-2200/2300 → S-1200/1202/1207/1210 → S-1299**.

### 2.3 Modelo + geração
- **Geração XML a partir do XSD oficial** (gerar classes do schema; nunca montar XML por string). `Span<T>`/`Memory<T>` para lotes grandes (CLAUDE.md §7).
- Mapeamento: `FolhaDePagamento` fechada → S-1200/S-1202 por servidor (eventos agrupados por `codIncCP/CPRP`); `Servidor` admitido/desligado → S-2200/S-2299. ACL traduz domínio→leiaute (não vazar tipos do leiaute para o domínio).
- Atributo `Id` do evento = identificador de negócio (consulta/download BX), **não** alvo da assinatura.

### 2.4 Assinatura A1 (reusar Cofre)
- Chamar `IServicoAssinaturaDigital.AssinarXmlAsync(xml, new OpcoesAssinaturaXml(DestinoAssinatura.ESocial), ct)`. O destino `ESocial` já fixa o perfil correto e audita cada uso (sucesso/falha) sem material sensível. **Nenhum código novo de assinatura.**
- Certificado: A1 e-CNPJ do ente (mesmo A1 serve conexão mTLS + assinatura). **EFR obrigatório** para ente público (S-1000) — `[a confirmar — preencher CNPJ do Ente Federado Responsável]`.

### 2.5 Transmissão (cliente SOAP + Outbox idempotente + Polly + ACL)
Pipeline distinto do TCE-RS:
1. **Outbox por evento eSocial** (`EventoESocial` persistido com estado: Pendente→Assinado→Transmitido→AguardandoRecibo→Aceito/Rejeitado). Idempotência por chave de negócio (tipo+Id+competência) — não reenviar duplicado.
2. **Worker de transmissão** (`Workers` host, padrão `NfseSync`): monta lote ≤**750 KB**, assina via Cofre, `EnviarLoteEventos` (SOAP 1.1, mTLS A1), guarda **protocolo**.
3. **Polling de retorno** `ConsultarLoteEventos` pelo protocolo → persiste **recibo por evento** (prova de entrega; necessário p/ retificação/exclusão). **Respeitar teto BX: ≤10 acessos/dia** ao download/consulta de identificadores; backoff.
4. **ACL** `IESocialGateway` (Application) com impl. na Infrastructure; **Polly** (retry + circuit breaker, `Microsoft.Extensions.Http.Resilience`) em toda chamada; mapeamento explícito de erros (ex.: erro 612 = lote > 750 KB).
5. **Ambientes:** Produção Restrita (sem efeito jurídico, ≤1.000 vínculos) para homologação; Produção para go-live. URL por `IOptions`/Key Vault, nunca hardcode.

URLs (CONFIRMADAS, `pesquisa-esocial-transmissao.md` §1):
- Prod envio: `webservices.envio.esocial.gov.br/.../WsEnviarLoteEventos.svc` · consulta: `webservices.consulta.esocial.gov.br/.../WsConsultarLoteEventos.svc`
- Restrita: `webservices.producaorestrita.esocial.gov.br/...`

### 2.6 Docs oficiais a obter
- **MOS S-1.3 + Anexo I + XSD** (NT congelada) — schemas dos eventos.
- **MOS Desenvolvedor v1.15** — confirmar assinatura (vs v1.10), binding/WSDL/WS-Security, 750 KB, URLs BX, teto 10/dia.
- Categorias 3XX reais do município (S-2200 vs S-2300) — dado operacional + tabela de categorias do MOS.
- RPPS paga inativos/pensionistas? → necessidade de S-1207 (lei municipal do RPPS).
- Situação cadastral do município em **Produção** (passivo 2021–2022? cargas retroativas?).

---

## (3) Ponto — AFD / AEJ (Portaria MTP 671/2021)

### 3.1 Confiança
- **MÉDIA.** Tipos de REP (REP-C/A/P), AFD imutável, AEJ via PTRP, assinatura CAdES (AFD/AEJ) / PAdES (comprovante) confirmados em fonte oficial (P&R REP gov.br). **Leiautes/posições exatas dos Anexos NÃO obtidos** (só fontes secundárias).
- **CRÍTICO/INCERTO:** aplicabilidade ao **servidor estatutário**. A Portaria 671 regulamenta a **CLT** (celetistas); jornada do estatutário é regida por **lei municipal / RJU** e normas do TCE-RS. Para o piloto (predominantemente estatutário) o ponto 671 pode **não ser a norma aplicável** — confirmar antes de priorizar.

### 3.2 Desenho
- **REP-P (software)** é o caminho (marcação inclusive mobile; só registro INPI, sem homologação Ministério).
- **AFD** (Arquivo Fonte de Dados): registro **bruto, cronológico, imutável**; ASCII ISO-8859-1, linha por registro com CR+LF, **NSR** obrigatório, validação **CRC-16 (CCITT)**, leiaute c/ CPF (vigente desde 10/02/2022). Modelar como log append-only (sem update) — alinha com auditoria imutável (CLAUDE.md §4).
- **PTRP** (tratamento) gera **AEJ** (Anexo VI; substitui AFDT/ACJEF) + **espelho de ponto**, **sem alterar o AFD**. Banco de horas: 6 meses (acordo individual) / 1 ano (ACT/CCT).
- **Assinatura:** AFD/AEJ = **CAdES detached (.p7s)** → `IServicoAssinaturaDigital.AssinarCmsAsync(..., new OpcoesAssinaturaCms(Destino, Detached:true))` (reusa Cofre); comprovante = **PAdES** (PDF). `[a confirmar — Destino/perfil CAdES exato no Anexo da 671]`.
- **Integração com folha:** AEJ/banco de horas pode alimentar rubricas (horas extras/atrasos) → eventos de folha. `[a confirmar — dependência ponto↔S-1200 no ente público]`.

### 3.3 Docs oficiais a obter
- **Portaria MTP 671/2021 — texto integral DOU + Anexos** (AFD, Anexo VI AEJ, requisitos REP/INMETRO): leiautes/posições/tipos de registro exatos. As "tipos 1–5" de blogs são **não confirmadas**.
- **RJU/estatuto de Maximiliano de Almeida + normas TCE-RS de jornada/frequência** — definir se 671 se aplica ao estatutário.

---

## (4) Remessa de folha ao TCE-RS — artefato + transmissão humana

### 4.1 Confiança
- **MÉDIA-ALTA** na estrutura (3 arquivos posicionais, periodicidade mensal, geração conjunta com SIAPC/PAD, **sem API**, upload manual). Campos do TCE_4810 capturados com bom detalhe; TCE_4820/4960 **parciais** (`pesquisa-tce-folha-1099.md` §3).
- **Pipeline 100% distinto do eSocial:** gerador/exportador offline validável, **transmissão humana** via app PAD do TCE. Reusar o padrão do `Modules/Transparencia` (RemessaTce: Gerada→Validada→Enviada, hash de integridade) — **não reusar o transporte SOAP do eSocial**.

### 4.2 Desenho
- **3 arquivos texto posicionais** (largura fixa, ASCII, padrão Febraban p/ bancos, valores 17 bytes, datas ddmmaaaa):
  - **TCE_4810.TXT** — lançamentos (vantagem/desconto/totalizador), incidências S/N/X (IRRF/RPPS/INSS/Saúde), dados bancários entidade+funcionário, ID único de folha, matrícula com sufixo de vínculo, tipo de folha (1-Normal…9-Outros). **Atenção: não somar rubrica marcada como totalizador junto às vantagens** (dobra o total).
  - **TCE_4820.TXT** — cadastro de funcionários (CPF, datas nasc/admissão/demissão, cargo, setor, carga horária, situação incl. "03-Pensionista").
  - **TCE_4960.TXT** — tabela de rubricas + **base legal** + **conta do Plano de Contas da Folha (cód. TCE)** — amarração contábil com PCASP/SIAPC.
- **Mapeamento do domínio:** `Rubrica` (enriquecida 1.3) → TCE_4960; `Servidor`+`Cargo` → TCE_4820; `EventoFolha`+bancário+incidências → TCE_4810. Os campos novos (base legal, conta TCE, incidências S/N/X, percentuais 4 díg.) entram no modelo de rubrica/servidor da Fase A.
- **Gerador posicional** com `Span<T>` (CLAUDE.md §7); validação offline; hash de integridade; **sem assinatura A1 no arquivo** (a A1 é para eSocial). `[a confirmar — PAD exige assinatura de responsáveis no app via SISCAD?]`.
- **Transmissão:** humana — operador carrega os 3 TXT no app PAD junto com o SIAPC. Periodicidade **mensal, até 30 dias corridos** após o encerramento; acumulado de 1º/jan até o mês. Modelar como artefato baixável + estado de remessa, sem transmissão automática.

### 4.3 Docs oficiais a obter
- **Resolução TCE-RS nº 1099/2018** (texto integral + revisões 2026).
- **Manual Técnico SIAPC Volume V** vigente em 2026 (usamos Rev. 06 de 26/03/2024) — confirmar versão no portal TCE.
- Tabelas completas **TCE_4820 e TCE_4960** (campos restantes) + **Plano de Contas da Folha (codificação TCE)**.
- **IN nº 8/2025 TCE-RS** (impacto em prazos/RGF) + changelog do app PAD por exercício.

---

## Mapa de dependências e isolamento (CLAUDE.md §2)
- **Domínio de folha** (Fase A) vive em `RecursosHumanos.Domain` (motor = serviço de domínio puro, sem I/O).
- **eSocial gateway** e **TCE-RS exporter** ficam em `RecursosHumanos.Infrastructure` atrás de portas na `.Application` (`IESocialGateway`, `IRemessaFolhaTceExporter`); assinatura via `IServicoAssinaturaDigital` (BuildingBlocks) → Cofre. Integration events via **Outbox**.
- Cross-module só por `*.Contracts` (ex.: `FolhaFechadaIntegrationEvent` já existe → Finanças/empenho da folha).
- Multi-tenant: toda nova entidade `IMustHaveTenant`; tabelas de parâmetros versionadas por `tenantId`+`competencia`.

## Três maiores riscos (consolidado das verificações)
1. **Leiaute por suposição** (eSocial e TCE): mitigar congelando MOS+XSD+Anexos na Fase 0; gerar parsers do schema; validar em Produção Restrita.
2. **Regra federal aplicada onde é municipal** (RPPS, margem, férias/13º): fail-closed sem tabela municipal; default RGPS só onde juridicamente seguro.
3. **Assinatura/transmissão**: assinatura já correta no Cofre (reconfirmar v1.15); lote por 750 KB e polling BX ≤10/dia com backoff; nunca polling agressivo.
