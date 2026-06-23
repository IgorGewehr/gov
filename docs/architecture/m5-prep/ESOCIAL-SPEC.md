# ESOCIAL-SPEC — Spec de implementação do eSocial para a FOLHA PÚBLICA (M5 / RecursosHumanos)

> **Status:** pronto-para-implementar. **Piloto:** Maximiliano de Almeida/RS (Grupo 4, RPPS).
> **Premissa:** a folha (motor INSS/IRRF/RPPS provado) e o ponto já existem; FALTA o eSocial.
> **Regra de ouro (CLAUDE.md §16):** nada de alíquota/faixa/regra de leiaute *hardcoded* ou inventado.
> Toda afirmação factual tem **FONTE** ou está marcada `[a confirmar — <doc oficial>]`.
> **CONFIANÇA** declarada por bloco (ALTA / MÉDIA / BAIXA).
>
> **Fontes primárias usadas nesta spec (baixadas e lidas, não só busca):**
> - **MOS Desenvolvedor eSocial v1.15 (abr/2025)** — PDF oficial, texto extraído e citado por linha/seção:
>   <https://www.gov.br/esocial/pt-br/documentacao-tecnica/manuais/manualorientacaodesenvolvedoresocialv1-15.pdf>
> - **Leiautes S-1.3 consolidados até NT 06/2026 (rev. 09/04/2026)** + Anexo I (Tabelas) + Anexo II (Regras) + pacote XSD:
>   <https://www.gov.br/esocial/pt-br/documentacao-tecnica/leiautes-esocial-versao-s-1-3-nt-06-2026-rev-09-04-2026/index.html>
> - Os 3 `pesquisa-/verificacao-esocial-*.md` + `M5-DESIGN.md` deste diretório.
> - Código existente: `Modules/Cofre` (`IServicoAssinaturaDigital`, `IAssinaturaEmEscopoDedicado`,
>   `DestinoAssinatura.ESocial`, `AssinadorXmlDsig`), `Modules/RecursosHumanos`
>   (`Servidor`, `RubricaFolha`, `FolhaDePagamento`, `MotorDeCalculoFolha`, `ResultadoCalculoServidor`),
>   `Modules/Transparencia` (`RemessaTce` estado `Gerada→Validada→Enviada`), `SharedKernel.OutboxMessage`
>   (já tem `AttemptCount`/`NextAttemptUtc`/`DeadLetteredOnUtc`/`MaxAttempts=5`).

---

## CORREÇÕES FACTUAIS desta spec sobre os `pesquisa-*.md` (verificadas no v1.15 oficial)

Quatro afirmações de `pesquisa-esocial-transmissao.md` / `M5-DESIGN.md` foram **refutadas** ao ler o PDF v1.15:

| # | O que o research dizia | O que o v1.15 OFICIAL diz | Fonte (v1.15) |
|---|---|---|---|
| C1 | "limite **750 KB/lote**" / "não '50 eventos'" / "erro 612 = lote > 750 KB" | Lote = **no máximo 50 eventos** (§7.5.x, "pode ser repetido até 50 vezes... no máximo 50 eventos") **E** tamanho limite da **mensagem SOAP = 5 megabytes** (erro **612**). As duas restrições coexistem. | linhas 1353-1355; 1648-1653 |
| C2 | "SOAP **1.1**, Document/Literal" | **SOAP 1.2** — envelope `xmlns:soap="http://www.w3.org/2003/05/soap-envelope"` no exemplo oficial de mensagem | linha 702 |
| C3 | binding "`[a confirmar]` transport x message security / WS-Security" | **HTTPS (TLS) com autenticação MÚTUA via certificado** (mTLS) — **transport security**, TLS **1.2** com cifras fixas; **não** há WS-Security message-level | linhas 677, 725-728, 939 |
| C4 | assinatura "`[a confirmar — v1.15]`" | **CONFIRMADO verbatim no v1.15 §6.7** (ver bloco 2.4). O perfil que o Cofre já implementa está correto. | linhas 793-869 |

`tpRegPrev` (1=RGPS, 2=RPPS, 3=Exterior, 4=SPSMFA militares) e a omissão do **S-5012** já foram corrigidos
em `verificacao-esocial-eventos.md` — incorporados aqui.

---

## 1) EVENTOS NÚCLEO — ordem, dependências e ORIGEM no nosso domínio

**CONFIANÇA: ALTA** no roster e na ordem (lidos na página oficial de leiautes S-1.3 NT 06/2026 e em
`verificacao-esocial-eventos.md` §2-3). **MÉDIA** nos campos internos exatos de cada grupo
(`[a confirmar — XSD do evento na versão travada]`).

### 1.0 Ordem obrigatória de transmissão (grafo de dependência)

```
S-1000 (empregador/órgão)
   └─ S-1005 (estabelecimentos) · S-1010 (rubricas) · S-1020 (lotações) · S-1070 (processos)
         └─ S-2200 (admissão) · S-2300 (TSVE início)        [não-periódicos, por servidor]
               └─ S-1200 (remun. RGPS) / S-1202 (remun. RPPS) · S-1207 (benefícios RPPS) · S-1210 (pagamentos)
                     └─ S-1299 (fechamento da competência)        [S-1298 reabre]
```
Regra dura (`verificacao-esocial-eventos.md` §2.2, CONFIRMADO): S-2200/S-2300 **antes** de qualquer
remuneração do vínculo; S-1299 só depois de todos os periódicos da competência. **Esta ordem é a
própria ordem de implementação dos sub-workflows** (ver §4.6).

### 1.1 S-1000 — Informações do Empregador/Órgão Público (evento de tabela, 1º de tudo)
- **Quando emitir:** uma vez por tenant (e a cada alteração cadastral), **antes de tudo**. Vigência aberta.
- **Campos-chave:** `tpInsc`/`nrInsc` (CNPJ do ente), `classTrib`, `indCoop`/`indConstr`, **`infoEFR`/`ideEFR`
  (Ente Federado Responsável — obrigatório p/ ente público)**, `dadosIsencao` (n/a), contato.
- **Origem no domínio:** **configuração do tenant** (não há agregado de "empregador" no RH hoje).
  → criar provider `IEmpregadorESocialProvider` (Application) lido de config/Key Vault por tenant.
- `[a confirmar — XSD S-1000 + preencher CNPJ do EFR de Maximiliano de Almeida]` (M5-DESIGN §2.4).

### 1.2 S-1005 — Tabela de Estabelecimentos/Unidades
- **Quando:** após S-1000; um registro por estabelecimento/unidade (CNPJ ou CAEPF/CNO).
- **Campos-chave:** `ideEstab` (`tpInsc`/`nrInsc`), `dadosEstab` (`cnaePrep`, `aliqGilrat` — para ente público
  tipicamente isento/`[a confirmar]`), `infoEntFed` (`indSubstPatr`, `percRedDopz`, etc.).
- **Origem:** configuração do tenant (unidades administrativas) → `IEmpregadorESocialProvider`.
- `[a confirmar — XSD S-1005 + quais campos GILRAT/FAP se aplicam a órgão público RPPS]`.

### 1.3 S-1010 — Tabela de Rubricas (mapeada das NOSSAS rubricas) — **ponto de maior trabalho**
- **Quando:** após S-1000; um S-1010 por rubrica vigente; nova versão a cada mudança de incidência/natureza.
- **Campos-chave (do leiaute):** `codRubr`, `ideTabRubr`, `dscRubr`, **`natRubr`** (Tabela 03),
  **`tpRubr`** (1=provento/2=desconto/3,4=informativa), **`codIncCP`** (incidência previdenciária — Tabela 20),
  **`codIncIRRF`** (Tabela 21), **`codIncFGTS`** (Tabela 23), `codIncCPRP` (incidência RPPS), `tetoRemun`.
- **GAP estrutural identificado no código atual (importante):** a entidade `RubricaFolha`
  (`Domain/Rubricas/RubricaFolha.cs`) hoje carrega **flags booleanas** (`IncideInss/IncideRpps/IncideIrrf/IncideFgts`),
  **não** os códigos eSocial. Para gerar S-1010 fiel é preciso **enriquecer `RubricaFolha`** com
  `natRubr/tpRubr/codIncCP/codIncIRRF/codIncFGTS/codIncCPRP` (vindos das Tabelas 03/20/21/23 do MOS, **carregadas**,
  nunca hardcoded). As flags booleanas atuais passam a ser **derivadas** desses códigos por um mapa de incidência
  carregado do MOS — preservando o motor de cálculo já provado.
- **Regra CONFIRMADA (`verificacao-esocial-eventos.md` §3.1):** rubrica informativa (`tpRubr∈{3,4}`) ⇒
  `codIncCP=00` e `codIncIRRF=9`; `codIncFGTS∈{21,93}` só em S-2299/S-2399 ou no grupo `{remunPerAnt}` do S-1200.
  A invariante "informativa não integra base" **já existe** em `RubricaFolha.Criar/AjustarIncidencias` — mantê-la
  e estendê-la para validar contra os códigos.
- **Origem:** agregado `RubricaFolha` (enriquecido) → ACL `MapeadorS1010`.

### 1.4 S-2200 (admissão) / S-2300 (TSVE)
- **Quando:** S-2200 ao admitir vínculo efetivo/empregado; S-2300 para sem-vínculo (TSVE: comissionados sem
  cargo efetivo, agentes políticos, conselheiros tutelares, estagiários, contratados temporários). Ambos **antes**
  da 1ª remuneração do vínculo.
- **Roteamento S-2200 vs S-2300:** `[a confirmar — MOS tabela de categorias + vínculos reais do município]`
  (M5-DESIGN §2.6). Categorias de servidor são **3XX**; S-2200 admite [101..111, 301,302,303,306,307,309,310,312,314].
- **Campos-chave S-2200:** `cpfTrab`, `dtAdm`, `matricula`, `codCateg`, `infoRegimeTrab/infoEstatutario`
  (`tpProv`, `dtNomeacao`, `dtPosse`, `dtExercicio`), `infoContrato` (`codCargo`, `codCarreira`, `remuneracao`,
  `tpRegPrev`), `localTrabalho`.
- **Origem no domínio (mapeamento direto, alta fidelidade):** agregado **`Servidor`** já tem TODO o ciclo
  nomeação→posse→exercício (`DataNomeacao/DataPosse/DataExercicio`, `Situacao`, `CargoId`, `Regime`, `Cpf`,
  `Matricula`, `Dependentes`). O evento de domínio **`ServidorAdmitido`** (já emitido no construtor) é o gatilho.
- **GAP:** `Servidor` ainda **não** tem `codCateg` (eSocial) nem `tpRegPrev` explícito (hoje só `RegimePrevidenciario`
  RPPS/RGPS). Acrescentar `codCateg` e derivar/armazenar `tpRegPrev` (1/2/4) — exigência já registrada em M5-DESIGN §1.2.

### 1.5 S-2299 (desligamento) / S-2399 (término TSVE)
- **Quando:** ao desligar. **Origem:** evento de domínio **`ServidorDesligado`** (já emitido por `Servidor.Desligar`).
- **Campos-chave:** `dtDeslig`, `mtvDeslig`, `verbasResc` (rescisórias), `quarentena`, `consigFGTS`.
- **Regra de exclusão (CONFIRMADO):** S-2299/S-2399 não pode ser excluído se houver S-1210 vinculado;
  após S-1299, exclusão só após reabertura S-1298.

### 1.6 S-1200 (remun. RGPS) / S-1202 (remun. RPPS) — derivados da NOSSA folha
- **Quando:** após a folha fechar; **um evento por servidor por competência**, roteado por regime.
- **ROTEADOR S-1200 ↔ S-1202 (CONFIRMADO, `verificacao` §3.2-3.3 — erro aqui rejeita a folha inteira):**
  - **S-1202** SOMENTE `codCateg∈[301,302,303,304,306,307,309,310,312,314]` **com `tpRegPrev∈[2,4]`** (RPPS/militar).
  - **S-1200** para RGPS (1XX/2XX/5XX/7XX/9XX) e servidores 3XX **com `tpRegPrev∈[1,3]`**.
  - **`tpRegPrev`: 1=RGPS, 2=RPPS, 3=Exterior, 4=SPSMFA** (correção confirmada; o "3=RPPS exterior" do research estava errado).
- **Campos-chave:** `ideEvento/perApur`, `ideTrabalhador/cpfTrab`, `dmDev` (demonstrativos), `ideEstabLot`,
  **`detVerbas`** (`codRubr`, `qtdRubr`, `vrRubr`, `indApurIR`) somado **por rubrica/incidência**, `infoPerApur`,
  `infoComplem`. S-1202 tem o grupo de **base RPPS** própria.
- **Origem (mapeamento de alta fidelidade):** `FolhaDePagamento` **Fechada** + `ResultadoCalculoServidor`
  (já traz `BaseInss/DescontoInss/BaseRpps/DescontoRpps/BaseIrrf/DescontoIrrf/TotalProventos/Liquido`) +
  `EventoFolha` (lançamentos por `Rubrica`) → ACL `MapeadorS1200/S1202`. **A folha É a fonte de verdade do `detVerbas`.**
- `[a confirmar — XSD S-1200/S-1202: nomes exatos de `dmDev`/`detVerbas` e grupos RPPS na versão travada]`.

### 1.7 S-1207 — Benefícios RPPS (aposentadorias/pensões) — **condicional**
- **Quando:** **somente se** o RPPS do município paga inativos/pensionistas. `[a confirmar — lei municipal do RPPS]`.
- **Origem:** fora do MVP até confirmar; se necessário, exige modelar benefício/pensionista (não há agregado hoje).

### 1.8 S-1210 — Pagamentos de Rendimentos do Trabalho
- **Quando:** após S-1200/S-1202, ao efetivar o pagamento da folha. Carrega `indGuia` herdado do evento de origem.
- **Campos-chave:** `ideBenef/cpfBenef`, `infoPgto` (`dtPgto`, `tpPgto`, `perRef`, `vrLiq`).
- **Origem:** evento de domínio **`PagamentoEfetuado`** (já emitido por `FolhaDePagamento.EfetuarPagamento`) +
  `TotalLiquido`/`LiquidoAPagar` por servidor → ACL `MapeadorS1210`.

### 1.9 S-1299 (fechamento) / S-1298 (reabertura)
- **Quando:** S-1299 fecha a competência depois de transmitidos todos os periódicos; S-1298 reabre p/ retificação.
- **Campos-chave:** `ideEvento/indApuracao`, `evtRemun`/`evtPgtos` (flags "houve movimento"), `compSemMovto`.
- **Origem:** orquestração do **gerador** (não há lançamento por servidor) — emitido quando a `FolhaDePagamento`
  está `Fechada` e todos os S-1200/1202/1210 dela estão `Aceito`.

### 1.10 Eventos de RETORNO (persistir, não gerar) — CONFIRMADOS
- **S-5001** (CP por trabalhador), **S-5002** (IRRF por trabalhador), **S-5003** (FGTS),
  **S-5011** (CP consolidada por contribuinte), **S-5012** (IRRF consolidado — **não esquecer; faltava no research**),
  **S-5013** (FGTS consolidado). São devolvidos pelo eSocial após processar S-1200/1202/1210/1299.
- **Origem:** persistir como **comprovação/totalizador** vinculado ao `EventoESocial` de origem (§4).

---

## 2) TRANSMISSÃO — Lote SOAP, XSD, assinatura A1 (reusa Cofre), retorno

**CONFIANÇA: ALTA** (lido no v1.15 oficial, citado por linha).

### 2.1 Ambientes e URLs (CONFIRMADO v1.15)
| Serviço | Host (Produção) | Host (Produção Restrita) |
|---|---|---|
| **Enviar lote** | `webservices.envio.esocial.gov.br/.../WsEnviarLoteEventos.svc` | `webservices.producaorestrita.esocial.gov.br/...WsEnviarLoteEventos.svc` |
| **Consultar lote** | `webservices.consulta.esocial.gov.br/.../WsConsultarLoteEventos.svc` | `webservices.producaorestrita.esocial.gov.br/...WsConsultarLoteEventos.svc` |
| **Download (BX)** | `webservices.download.esocial.gov.br/.../dwlcirurgico/...` | `webservices.producaorestrita.esocial.gov.br/...` |

(v1.15 linhas 1172-1179, 1716-1726, 2287-2295). **URLs por `IOptions`/Key Vault, nunca hardcode** (CLAUDE.md §7).

### 2.2 Protocolo de transporte (CONFIRMADO v1.15 — corrige o research)
- **SOAP 1.2** (`xmlns:soap="http://www.w3.org/2003/05/soap-envelope"`, linha 702). **NÃO SOAP 1.1.**
- **HTTPS / mTLS** com **autenticação mútua por certificado** (transport security), **TLS 1.2**, cifras fixas
  desde 24/06/2024: `TLS_RSA_WITH_AES_128_GCM_SHA256`, `..._256_GCM_SHA384`, `TLS_ECDHE_RSA_WITH_AES_128/256_...`
  (linhas 725-728). **Não há WS-Security message-level** — a autenticação do canal é o **certificado de conexão**.
- O certificado de conexão **pode ser o mesmo A1** usado na assinatura (mas o `nrInsc` do transmissor deve ser
  igual ao CNPJ/CPF do certificado — erro **607** se divergir, linha ~1660).

### 2.3 Estrutura do LOTE (CONFIRMADO v1.15, schema `EnvioLoteEventos-vx_x_x.xsd`)
```
<eSocial xmlns="http://www.esocial.gov.br/schema/lote/eventos/envio/vx_x_x">   (linha 627)
  <envioLoteEventos grupo="...">                                              (linha 1239)
    <ideEmpregador> <tpInsc/> <nrInsc/> </ideEmpregador>                       (linha 1250)
    <ideTransmissor> <tpInsc/> <nrInsc/> </ideTransmissor>                     (linha 1281)
    <eventos>
      <evento Id="..."> <!-- XML do evento JÁ ASSINADO --> </evento>          (até 50x)
    </eventos>
  </envioLoteEventos>
</eSocial>
```
- **Limites (CONFIRMADO):** **máx. 50 eventos/lote** (linha 1355) **E** **mensagem SOAP ≤ 5 MB** (erro **612**, linha 1653).
  O empacotador deve respeitar **as duas** — fechar lote ao atingir 50 eventos **ou** ~5 MB, o que vier primeiro.
- Erro **613** = erro estrutural do lote; **11** = revogação de certificado não verificável; **608** = versão do lote
  não é a mais recente; **607** = nrInsc transmissor ≠ certificado.
- Cada **evento** tem seu próprio namespace `http://www.esocial.gov.br/schema/evt/...` e **uma única declaração de
  namespace no elemento raiz** (linhas 623-638) — uso de namespace fora do padrão é **vetado**.

### 2.4 ASSINATURA A1 — reusa o Cofre (perfil CONFIRMADO verbatim no v1.15 §6.7)
**A assinatura do XML do evento já está implementada e correta.** O v1.15 (linhas 793-869) confirma **exatamente**
o perfil que `DestinoAssinatura.ESocial` documenta e que `AssinadorXmlDsig` produz:
- Formato **Enveloped**; **CanonicalizationMethod** `http://www.w3.org/TR/2001/REC-xml-c14n-20010315`
  (**C14N inclusiva**, *não* exc-c14n — exatamente a guarda anti-default-do-.NET de A1-DESIGN §7);
- **SignatureMethod** `http://www.w3.org/2001/04/xmldsig-more#rsa-sha256`;
- **DigestMethod** `http://www.w3.org/2001/04/xmlenc#sha256`;
- **Transforms**: `#enveloped-signature` + `REC-xml-c14n-20010315`;
- **`<Reference URI="">`** (documento inteiro — linha 853);
- **KeyInfo** contém **apenas `<X509Certificate>`** (linhas 837, 865-869);
- Chave **2048 bits** (A1/A3), compatível com o A1 e-CNPJ do ente.

**Como chamar (zero código novo de assinatura):** do gerador/worker, via a porta de escopo dedicado
(`IAssinaturaEmEscopoDedicado` — necessária porque o handler do RH já resolveu o `RecursosHumanosDbContext`
no escopo, e a guarda H5 recusa dois `ModuleDbContext` no mesmo escopo):
```csharp
ResultadoAssinatura r = await assinaturaEscopoDedicado.AssinarXmlAsync(
    xmlEventoUtf8, new OpcoesAssinaturaXml(DestinoAssinatura.ESocial), ct);
```
A trilha de auditoria de cada uso (sucesso/falha) já é registrada pelo Cofre. **`ReferenceUri=""`** (default) está correto.

### 2.5 Processamento do RETORNO (assíncrono, 2 níveis — CONFIRMADO v1.15)
1. **Nível 1 (síncrono):** `EnviarLoteEventos` valida certificado + estrutura do lote → retorna **`protocoloEnvio`**
   (número sequencial único — linha 1601). Persistir o protocolo no agregado.
2. **Nível 2 (assíncrono):** o eSocial processa cada evento e gera **recibo (`nrRecibo`) por evento**.
3. **Polling:** `ConsultarLoteEventos(consulta)` por `protocoloEnvio` (linhas 1696-1781) → retorna status do lote +
   **recibos e/ou erros por evento**. Persistir `nrRecibo` por `EventoESocial` (prova de entrega; necessário p/
   retificação/exclusão). Backoff entre polls (a consulta deve respeitar "data limite até uma hora a menos", §retorno).
4. **BX / recuperação:** `ConsultarIdentificadoresEventos*` + `SolicitarDownloadEventosPorNrRecibo/PorId` —
   **teto de 10 solicitações/dia** (soma; erro **405**, linha 2280). Usar só para reconciliação/recuperação, com backoff.

### 2.6 Validação contra XSD oficial (antes de assinar e antes de transmitir)
- **Gerar classes a partir do XSD** da versão travada (nunca montar XML por string — M5-DESIGN §2.3); serializar com
  `XmlSerializer`/`XmlWriter` e **validar contra o `.xsd`** localmente antes da assinatura (assinar XML inválido =
  recibo rejeitado e desperdício de cota). `Span<T>`/`Memory<T>` para lotes grandes (CLAUDE.md §7).
- `[a confirmar — baixar e congelar o pacote XSD da versão exata (S-1.3 NT 06/2026 rev. 09/04/2026) no repositório]`.

---

## 3) HOMOLOGAÇÃO — Produção Restrita como validação REAL automatizável

**CONFIANÇA: ALTA** (ambiente e regras no v1.15 e na página oficial de Produção Restrita).

- **O que é:** ambiente de webservice **idêntico ao de produção em estrutura/validação**, **sem efeito jurídico**,
  limitado a **1.000 vínculos/empregador**. Mesmos schemas, mesma assinatura, mesmas validações de lote/evento.
  É a validação **real** do pipeline ponta-a-ponta antes do go-live.
- **O que precisamos:**
  1. **Certificado A1 e-CNPJ** do ente (ou de teste) carregado no Cofre, com cadeia ICP-Brasil válida (a validação
     de cadeia já existe em `Cofre/Cripto/ValidacaoCadeiaIcpBrasil`).
  2. **Cadastro/situação do ente** na Produção Restrita `[a confirmar — situação cadastral do município]`.
  3. URLs de **Produção Restrita** apontadas por config (`webservices.producaorestrita.esocial.gov.br`).
- **Como testar (automatizável, é o critério de pronto do M5-eSocial):**
  1. Suite de **testes de integração** (xUnit) que: gera S-1000→S-1005→S-1010→S-2200→S-1200/1202→S-1210→S-1299 a
     partir de fixtures de domínio (Servidor + Folha fechada), **valida contra XSD**, assina via Cofre, monta lote,
     **transmite à Produção Restrita** e faz **polling do recibo**, asserindo `nrRecibo` sem erro.
  2. Testes negativos: lote > 5 MB (erro 612), > 50 eventos, nrInsc ≠ certificado (607), rubrica informativa com
     incidência (rejeição S-1010) — provando que o ACL e o empacotador barram antes de gastar cota.
  3. Gate de CI: nenhum go-live de produção sem a suite de Produção Restrita verde na versão de leiaute travada.

---

## 4) ARQUITETURA NO NOSSO SISTEMA

**CONFIANÇA: ALTA** — reaproveita padrões já em produção (Outbox com backoff/dead-letter; estado
`Gerada→Validada→Enviada` do `RemessaTce`; assinatura em escopo dedicado; Polly).

### 4.1 Camadas (CLAUDE.md §2 — isolamento)
- **`RecursosHumanos.Domain`** — agregado novo **`EventoESocial`** (raiz, `IMustHaveTenant`) com a **máquina de
  estados** e invariantes; ACLs de mapeamento como serviços de domínio puros (sem I/O).
- **`RecursosHumanos.Application`** — porta **`IESocialGateway`** (Enviar/Consultar) + handlers (gerar/transmitir/consultar).
- **`RecursosHumanos.Infrastructure`** — impl. `ESocialGateway` (cliente SOAP 1.2 + mTLS), `MapeadorSxxxx`,
  repositório de `EventoESocial`, **Polly** (`Microsoft.Extensions.Http.Resilience`) em toda chamada.
- **Assinatura** via `IAssinaturaEmEscopoDedicado` → Cofre (BuildingBlocks/ApiHost). **Nada de assinatura nova.**
- **Cross-module** só por `*.Contracts` (consumimos `FolhaFechada`/`ServidorAdmitido`/`ServidorDesligado`/
  `PagamentoEfetuado` que **já existem** em `RecursosHumanos.Contracts`).

### 4.2 Gerador de eventos (a partir da folha/servidor) — ACL domínio→leiaute
- Gatilhos por **evento de domínio já existente**: `ServidorAdmitido`→S-2200/S-2300; `ServidorDesligado`→S-2299/2399;
  `FolhaFechada`→S-1200/S-1202 (por servidor) + S-1299; `PagamentoEfetuado`→S-1210.
- Cada `MapeadorSxxxx` traduz agregado→DTO-de-leiaute (gerado do XSD); **tipos do leiaute não vazam para o domínio**.
- Produz o XML, **valida contra XSD**, cria o `EventoESocial` em estado `Gerado`.

### 4.3 Outbox + idempotência (reusa `SharedKernel.OutboxMessage`)
- `EventoESocial` persistido na **mesma transação** do estado que o originou (Outbox transacional).
- **Idempotência por chave de negócio:** `(tenantId, tipoEvento, idNegocio, competência)` — ex.: um único S-1200 por
  `(servidor, competência)`. Reprocessar nunca duplica (não reenvia evento já `Aceito`). O `OutboxMessage` já traz
  `AttemptCount`/`NextAttemptUtc`/`DeadLetteredOnUtc`/`MaxAttempts=5` — reusar para retry/backoff/dead-letter.

### 4.4 Máquina de estados do `EventoESocial`
```
Gerado ──assinar──▶ Assinado ──empacotar+enviar──▶ Transmitido(protocolo)
   │                                                      │
   │                                            consultarLote (polling)
   ▼                                                      ▼
RejeitadoLocal (XSD inválido)              Processado/Aceito(nrRecibo)  ──┐
                                           Rejeitado(erros por evento) ◀──┘
```
- `Gerado→Assinado`: assinatura via Cofre (`DestinoAssinatura.ESocial`).
- `Assinado→Transmitido`: empacota lote (≤50 e ≤5 MB), `EnviarLoteEventos`, guarda `protocoloEnvio`.
- `Transmitido→Aceito/Rejeitado`: `ConsultarLoteEventos`, persiste `nrRecibo` por evento ou erros.
- `RejeitadoLocal`: falha de validação XSD antes de transmitir (não gasta cota; exige correção).
- Estados terminais: `Aceito`, dead-letter (`DeadLetteredOnUtc`). `Rejeitado` é re-gerável após correção.

### 4.5 Worker de transmissão + Polly (padrão `NfseSync`)
- Host em `Workers` (padrão do `NfseSyncWorker`): drena `EventoESocial` pendentes por tenant, monta lotes,
  transmite, faz polling. **Polly** (retry + circuit breaker) em toda chamada SOAP; **respeitar teto BX ≤10/dia**;
  backoff exponencial (reusa `NextAttemptUtc`). Mapeamento explícito de erros (612/613/607/608/11) → ação.

### 4.6 Ordem de implementação (sub-workflows) — espelha o grafo de dependência do §1.0
1. **B0 — Congelar XSD + leiaute** (BLOQUEIA tudo): baixar/versionar pacote XSD S-1.3 NT 06/2026 rev. + Anexo I
   (Tabelas 03/20/21/23/categorias/tpRegPrev) + Anexo II (Regras). Gerar classes do schema.
2. **B1 — `EventoESocial` + máquina de estados + Outbox/idempotência** (Domain + persistência).
3. **B2 — Gateway SOAP 1.2 + mTLS + Polly** (Infra) validado em **Produção Restrita** com um S-1000 dummy.
4. **B3 — S-1000/S-1005** (tabela base; `IEmpregadorESocialProvider`).
5. **B4 — S-1010** (enriquecer `RubricaFolha` com `natRubr/codInc*`; `MapeadorS1010`).
6. **B5 — S-2200/S-2300 + S-2299/S-2399** (do `Servidor`; acrescentar `codCateg`/`tpRegPrev`).
7. **B6 — Roteador S-1200↔S-1202 + S-1210** (da folha fechada / `ResultadoCalculoServidor`).
8. **B7 — S-1299/S-1298 + persistência dos retornos S-5xxx** (incl. **S-5012**).
9. **B8 — Suite de homologação automatizada em Produção Restrita** (gate de CI; §3).
10. **B9 (condicional) — S-1207** se o RPPS pagar inativos/pensionistas `[a confirmar]`.

---

## PENDÊNCIAS `[a confirmar — doc/dado oficial]` (consolidadas)
1. **Pacote XSD da versão exata** (S-1.3 NT 06/2026 rev. 09/04/2026) — baixar e congelar no repo. *(bloqueia B0)*
2. **Campos internos exatos** de cada evento (`dmDev`/`detVerbas` do S-1200/1202; grupos RPPS; S-1000 EFR) — Anexo I + XSD.
3. **Tabelas 03/20/21/23** (natureza/incidências) e tabela de **categorias** + **tpRegPrev** — Anexo I do MOS.
4. **codCateg reais** do município (S-2200 vs S-2300/TSVE) — dado operacional + tabela de categorias.
5. **RPPS paga inativos/pensionistas?** → necessidade de **S-1207** — lei municipal do RPPS.
6. **Situação cadastral do ente** em Produção e Produção Restrita (passivo 2021-2022? cargas retroativas?) — operacional.
7. **CNPJ do EFR** de Maximiliano de Almeida (obrigatório no S-1000) — dado do ente.

## Três maiores riscos (e mitigação)
1. **Leiaute/incidência por suposição** → rejeição em massa da folha. *Mitigar:* B0 congela XSD; gerar classes do
   schema; validar local antes de transmitir; gate de Produção Restrita.
2. **Roteador S-1200↔S-1202 / tpRegPrev errado** → folha inteira rejeitada. *Mitigar:* transcrever codCateg/tpRegPrev
   do XSD travado; testes negativos no gate.
3. **Cota e duplicidade na transmissão** → bloqueio/retrabalho. *Mitigar:* idempotência por chave de negócio; lote
   ≤50 e ≤5 MB; BX ≤10/dia com backoff (reusa Outbox `NextAttemptUtc`/dead-letter).

---

> **Disclaimer (CLAUDE.md §16):** a assinatura, o transporte (SOAP 1.2/mTLS/limites de lote) e a ordem/roster de
> eventos estão **CONFIRMADOS** no MOS Desenvolvedor v1.15 e nos leiautes S-1.3 (citados por linha/URL). Os
> **campos internos de cada evento e as tabelas de incidência** dependem do **pacote XSD + Anexo I da versão travada**
> (pendência 1-3) e **não** devem virar código antes de congelados. Esta spec reduz incerteza ao máximo permitido
> sem o XSD em mãos; não autoriza implementar regra de leiaute por suposição.
