# Tensorroot.Gov — Estado Atual e Pendências

> Documento de handoff **ultra-honesto, end-to-end e auto-contido**. Tudo aqui foi **medido do código** em 2026-06-27 (build + suíte completa + leitura de arquivos, READ-ONLY) — nada foi lido de `progresso.json` ou de docs de processo.
>
> **Referências duráveis** (as únicas que este documento assume que sobrevivem): o próprio **código** em `src/` e `infra/`, a **revisão humana** em `docs/REVISAO-HUMANA/` (contabil, tributos, rh-folha, licitacoes, tce-rs-integracao, outros-modulos) e as **fontes normativas** em `docs/normas/FONTES-NORMATIVAS.md`. Nenhuma afirmação aqui depende de documento de progresso/estudo intermediário.
>
> Convenção de citação: `arquivo:linha`. "Cred-gated" = só funciona de verdade com credencial/certificado oficial do ente. "Simulado/Placeholder" = a estrutura existe mas falta grade oficial, XSD ou transmissão real — **e isso está honestamente marcado no próprio código** com `TODO(prod:...)` / `[validar-*]`.

---

## 1. Sumário executivo (honesto, sem inflar)

**Veredito de um parágrafo:** a fundação técnica é **real, robusta e acima da média GovTech** — não é skeleton. Build limpo (0 erros), **1.767 testes verdes** em 20 projetos (incluindo E2E de isolamento cross-tenant, idempotência inbox/outbox e o ciclo da despesa via HTTP), domínio rico genuíno (0 setters públicos no domínio), contabilidade de partida dobrada e cálculos de folha/tributos testados com **números concretos** (INSS=501,51 / líquido=4.498,49; ITBI Tema 1.113/STJ). O que **ainda não está pronto** são as **integrações externas oficiais** (eSocial, SIAPC/PAD, NFS-e ADN, SICONFI/PNCP/Receita/CadÚnico — gateways simulados ou cred-gated), **algumas regras jurídicas finas** (decadência do ISS por homologação, prescrição intercorrente LEF art. 40) e o **hardening de produção** (sem HSTS/headers, Azure SQL aberto, login sem lockout por conta). Coerente com **"pronto para PoC, não para go-live com transmissão oficial"**. **Nenhum furo é estrutural** — tudo é completar dados/grades, trocar gateways e endurecer infra, sem reescrita.

### O que é FORTE e REAL (confirmado no código)

- **Cadeia orçamentária ponta-a-ponta, encadeada e testada por E2E:** PPA → LDO → LOA → `DotacaoOrcamentaria` → `Empenho` (FK `DotacaoId`) → `Liquidacao` (FK `EmpenhoId`) → `OrdemDePagamento`/`ItemPagamento` (FK `LiquidacaoId`) → `RestosAPagar`. Teste: `CicloDaDespesaE2ETests`.
- **Contabilidade de partida dobrada real**, com invariantes testadas (balanceamento + homogeneidade): `LancamentoContabil.cs:180-287`.
- **Folha com cálculos legais corretos e testados:** `ApurarDescontosLegaisTests.cs:195` (`INSS == 501.51m`), `CicloAnualDominioTests.cs:85`.
- **ITBI Tema 1.113/STJ** (base = valor declarado + processo de arbitramento CTN 148): `IssItbiEndpoints.cs:61,78`; migration `ItbiTema1113Arbitramento`.
- **Auditoria hash-chain SHA-256** encadeada por tenant: `Auditing/AuditHashChain.cs:8-12`, `AuditSaveChangesInterceptor.cs:59`.
- **CDA com requisitos LEF art. 2º §5º / CTN 202** (recusa se faltar requisito): `DividaAtiva.cs:246-289`.
- **Segurança de base acima da média:** envelope encryption AES-256-GCM da connection string com AAD cross-tenant (`ProtetorConexaoTenant.cs:46-158`), KEK com fail-fast de produção (`CofreModule.cs:76-83`), BCrypt work factor 12 (`SenhaHasher.cs`), deny-by-default global de autorização (`Program.cs` `FallbackPolicy`), login antienumeração (timing estável).

### Os 5 maiores gaps (priorizados)

| # | Gap | Impacto | Cred-gated? |
|---|-----|---------|-------------|
| 1 | **eSocial gateway SIMULADO + 0 XSD no repo** | Obrigação federal mensal não transmite em produção | Sim |
| 2 | **SIAPC/PAD: emissor real, grade placeholder** (`TODO(validar-leiaute-MT-2026)`) | Remessa obrigatória ao TCE-RS não passa no e-Validador oficial | Não (grade é JSON) |
| 3 | **Picker de Tributos ausente** (`/contribuintes?termo=`, `/imoveis?termo=`) | Balcão trava — só busca por GUID | Não |
| 4 | **Jurídico fino:** decadência ISS usa CTN 173 I (deveria 150 §4) + sem prescrição intercorrente (LEF art. 40 / Súmula 314) | Risco de cobrança/baixa indevida de créditos | Não |
| 5 | **Hardening de produção aberto:** Azure SQL público, sem HSTS/headers, login sem lockout, control-plane sem auditoria | Superfície de ataque / compliance | Parcial |

---

## 2. Estado MEDIDO — alegado vs medido

Compilado com SDK .NET 10.0.301 (`DOTNET_ROLL_FORWARD=LatestMajor`), suíte inteira executada.

| Item | Alegado (baseline) | **Medido (código)** | Veredito |
|---|---|---|---|
| Build | 0 err / 27 warn | **0 erros, 27 avisos** (`Compilação com êxito`, ~24s) | CONFIRMADO |
| Testes | "verde" | **1.767 testes, 0 falhas, 0 ignorados, 20 projetos — todos Aprovado!** | VERDE TOTAL |
| CVEs | 3 transitivos | **Confirmados exatos:** SQLitePCLRaw.lib.e_sqlite3 2.1.6 (NU1903 alta), System.Linq.Dynamic.Core 1.3.12 (NU1903 alta), OpenTelemetry.Api 1.9.0 (NU1902 moderada) | CONFIRMADO |
| Tamanho | — | **2.666 arquivos `.cs`, ~221.874 linhas C#** (incl. migrations), **91 `.csproj`**, **16 módulos**, SPA React **247 páginas** | — |
| `NotImplementedException` em `src/` | — | **0** | LIMPO |
| Comentários `// TODO` | — | **418** (maioria `TODO(prod:...)` / `[validar-oficial]` — documentam o que falta p/ produção, não bugs) | HONESTO |
| `simulado/placeholder/stub` | — | **56 ocorrências** | — |

**Os 27 avisos** = CA1305 (28 ocorrências de `DateOnly.ToString` sem `IFormatProvider`, **só em IntegrationTests**) + CA1711 (2, nome `ApiHostCollection`) + NU1608 (6, downgrade Microsoft.CodeAnalysis 4.8→4.5) + NU1902/NU1903 (6, os 3 CVEs). **Nenhum aviso em código de produção de domínio** — são testes e transitivos.

**Maiores módulos por LoC** (excl. migrations): RecursosHumanos 30.281, Financas 18.856, Saude 17.757, Tributos 16.388, Patrimonio 15.804, Administracao 13.910. Menores: Cidadao 1.581, Cofre 1.790, PainelGestor 1.893.

---

## 3. Scorecard por dimensão

| Dimensão | Nota | Uma linha |
|---|---|---|
| **Arquitetura modular** | 9,0 | 16 módulos, domínio rico (0 public setters), camadas puras validadas por NetArchTest (cego só em Cofre/Identidade na regra de referência cruzada). |
| **Fundação contábil** | 9,0 | Partida dobrada testada, cadeia da despesa encadeada e E2E, MSC com SI+mov=SF. |
| **Tributos (motor jurídico)** | 8,0 | ITBI Tema 1.113, CDA/LEF, decadência/prescrição por datas-do-fato; gaps finos no ISS 150 §4 e LEF art. 40. |
| **RH / Folha** | 8,5 | Cálculos legais testados, SST, ponto, 13º; eSocial não transmite (gateway simulado, 0 XSD). |
| **Leiautes oficiais de saída** | 5,0 | Mecânica de emissão sólida e honestamente marcada, mas **nenhuma saída validada no validador oficial**; Folha-TCE 1099 é a mais pronta. |
| **Integrações externas** | 4,0 | Quase todos os gateways em modo Simulado por default (PNCP, Receita, SICONFI, CadÚnico, Transferegov, NFS-e); reais existem atrás de flag. |
| **Segurança (design)** | 8,5 | Envelope encryption, KEK fail-fast, BCrypt 12, deny-by-default — acima da média. |
| **Hardening de produção** | 5,0 | Sem HSTS/headers, Azure SQL público, login sem lockout, control-plane sem auditoria, 3 CVEs transitivos. |
| **Cobertura de testes** | 9,0 | 1.767 testes verdes incl. E2E de isolamento cross-tenant e ciclo da despesa. |
| **Front-end (operação)** | 6,0 | 247 páginas SPA; pickers existem em RH/Saúde/Patrimônio mas **faltam em Tributos** (balcão); SPA cidadão rasa; PainelGestor/BI quase vazio. |

---

## 4. PENDÊNCIAS PARA A PoC — caminho mínimo p/ ganhar o piloto + passar no TCE-RS

Priorizadas. "Esforço" é ordem de grandeza de engenharia (P=pequeno ≤2d, M=médio ~1sem, G=grande >1sem). Caminho crítico p/ PoC: **(1) Folha-1099 + (2) SIAPC grade 2026 + (3) picker Tributos + (4) LicitaCon completar arquivos**.

| # | Pendência | Arquivo:linha | Esforço | Cred-gated |
|---|-----------|---------------|---------|------------|
| P1 | **Picker de Tributos (balcão).** Criar `GET /contribuintes?termo=` e `/imoveis?termo=` (busca por nome/CPF/CNPJ). Hoje `ImovelListPage.tsx` exige digitar o GUID do contribuinte → **bloqueador de atendimento**. RH/Saúde/Patrimônio já têm `?termo=` como referência. | `ImovelListPage.tsx`; padrão em `RecursosHumanos ...:306` | M | Não |
| P2 | **SIAPC/PAD — grade oficial 2026.** Emissor posicional já é robusto (Latin-1, largura fixa, centavos, overflow-safe). Falta a **grade completa MT SIAPC/PAD Vol. V 2026 (BP/BF/DFC)** em JSON versionado por exercício + rodar no e-Validador oficial. | Emissor: `EmissorRegistroSiapc.cs:99-207`; grade placeholder: `LeiauteSiapcSeed.cs:49-69` (`// TODO(validar-leiaute-MT-2026)`) | G | Não (grade é JSON); validação cred-gated |
| P3 | **Folha-TCE Res. 1099 — confirmar no e-Validador.** É o leiaute **mais pronto**: grade oficial dos 3 arquivos (TCE_4810/4820/4960) com posições/tamanhos exatos + contrato RH→Transparência cheio. Falta só confirmar vigência no e-Validador real. | Grade: `LeiauteFolhaTceSeed.cs:70-147`; contrato: `FolhaResumoRemessaTceIntegrationEvent.cs:28-100` | P | Sim (validador) |
| P4 | **LicitaCon 1.4 — completar 8/14 arquivos.** Cabeçalhos oficiais OK e 6 arquivos preenchidos (Licitacao/Licitante/Lote/Proposta). Saem **vazios**: Pessoas, MembroConsorcio, Comissao, MembroComissao, Dotacao, Evento, Item, LoteProposta, ItemProposta, Documento. Docs de fornecedor saem como GUID interno, não CNPJ/CPF. | `GeradorRemessaLicitaCon.cs:29-43` (10× `Vazio(...)`), `TODO(M10)` em :86,145,174 | G | Transmissão cred-gated |
| P5 | **NFS-e ADN — apontar para endpoint real.** `AdnNfseGateway` (HttpClient/Polly) já existe e é selecionável por `Nfse:Provider=Adn`. Falta URL oficial ADN + contrato XML nacional (hoje é DTO JSON ad-hoc) + certificado do ente. Default = `SimuladoNfseGateway`. | `AdnNfseGateway.cs:12-28`; toggle `TributosModule.cs:104,108` (`adn.invalido.local` é só fallback) | M | Sim |
| P6 | **eSocial — transporte real.** Pipeline interno (gerar→assinar→empacotar XML) é real (`EscritorXmlEvento`, `EmpacotadorLoteESocial`), inclui correções P0 (S-1202 usa `ideEstab`). Falta: (a) baixar **XSD S-1.3** e validar XML antes de assinar; (b) confirmar códigos das Tabelas; (c) implementar `ESocialGatewaySoap` (SOAP 1.2 + mTLS + A1 do Cofre). Gateway atual devolve protocolo/recibo FAKE. | `ESocialGatewaySimulado.cs:21,50`; wireup `RecursosHumanosModule.cs:167` (`AddSingleton<IESocialGateway, ESocialGatewaySimulado>`); `find -iname *.xsd` = vazio | G | Sim |
| P7 | **Login com lockout por conta.** Há rate-limit global (100 req/min) mas **não** lockout dedicado. Adicionar `AccessFailedCount` + `LockoutEnd` no agregado `Usuario` (ex.: 5 falhas → cooldown), auditado. + policy de rate-limit `"login"` curta no endpoint. | `Autenticar.cs:99-103` (sem incremento); `IdentidadeEndpoints.cs:63` (login sem `RequireRateLimiting`); grep `AccessFailedCount` = 0 | M | Não |
| P8 | **Security headers + HSTS.** Adicionar `app.UseHsts()` (não-Dev) + `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy`, CSP (portal público é HTML servido a cidadão). | Ausente em `Program.cs`; grep `UseHsts`/headers = 0 | P | Não |
| P9 | **Fechar os 3 CVEs transitivos.** Pinar `<PackageVersion>` explícito p/ SQLitePCLRaw, System.Linq.Dynamic.Core, OpenTelemetry.Api em `Directory.Packages.props` (pinning central já força a sobrescrita transitiva). Rodar `dotnet list package --vulnerable --include-transitive` p/ versões-alvo. | `Directory.Packages.props` (sem override dos 3) | P | Não |
| P10 | **Azure SQL — fechar superfície de rede.** `publicNetworkAccess: 'Disabled'` + Private Endpoint na VNet do App Service; se inviável agora, remover `AllowAllAzureIps` e restringir ao outbound IP do App Service. | `infra/main.bicep:77` (`Enabled`), `:91-96` (`AllowAllAzureIps 0.0.0.0`) | M | Não |

---

## 5. PENDÊNCIAS PARA CUIDAR DO MUNICÍPIO INTEIRO (além da PoC)

Completude, compliance fina e operação diária. A PoC é demonstrável; para uma prefeitura **operar de verdade** estes itens precisam fechar.

### 5.1 Completude funcional por módulo

| Módulo | Falta p/ operar o dia-a-dia |
|---|---|
| **Finanças** | PCASP completo 2026 (`[validar-plano-oficial]`, hoje ~62/70 contas "catálogo mínimo"); DCASP fina; conciliação bancária OFX/retorno CNAB; relatórios LRF operacionais (RREO/RGF no formato MDF — **não localizados**). |
| **Tributos** | **Picker (P1)**; NFS-e default Simulado; EFD-Reinf R-4000 (substitui DIRF — não localizado); migração/geração legada SAPI. |
| **RecursosHumanos** | eSocial transporte real (P6). |
| **Saúde** | Gateways externos SIMULADOS (`SimuladoGateways.cs`); e-SUS/CNES/SIA-SUS reais; regulação/agendamento. |
| **Educação** | Censo Escolar/Educacenso export; integração SIOPE; relatórios fiscais finos. |
| **Patrimônio** | Integração DETRAN; reavaliação/inventário em massa. |
| **AssistênciaSocial** | Gateway CadÚnico real (`SimuladoCadUnicoGateway.cs`); integração CRAS/CREAS oficial. |
| **Administração/Suprimentos** | Gateways PNCP/Receita reais (`PncpGatewaySimulado.cs`, `SimuladoReceitaCnpjGateway.cs`). |
| **Transparência** | SICONFI gateway real (`SimuladoSiconfiGateway.cs`); leiaute SIAPC (P2); PAD validação oficial. |
| **Legislativo** | Assinatura/publicação DOM real; integração TCE pessoal Câmara. |
| **Convênios** | Gateway Transferegov real (`SimuladoTransferegovGateway.cs`). |
| **Protocolo** | gov.br/peticionamento externo; assinatura cidadão. |
| **Cidadão/Portal** | SPA cidadão rasa (4 páginas); login gov.br; emissão de guias/2ª via; carnê IPTU online. |
| **PainelGestor/BI** | Praticamente vazio (1 endpoint, 1 página) — BI/indicadores reais, drill-down, dashboards executivos. |
| **Cofre / Identidade** | Cobertura de testes fina; MFA; NetArchTest cego na regra de referência cruzada. |

### 5.2 Compliance jurídica fina (corrigir antes de cobrar/baixar créditos do município)

| Regra | Norma | Estado | Gap |
|---|---|---|---|
| **Decadência ISS por homologação** | CTN 150 §4 vs 173 I | `Lancamento.Lancar()` **sempre** aplica 173 I; ISS chama o mesmo método (`ApurarIssMensal.cs:112`) | **REAL e ERRADO p/ ISS** — decai do fato gerador, não do exercício seguinte; falta `TipoLancamento` (ofício vs homologação) e a regra dupla. |
| **Prescrição intercorrente** | LEF art. 40 §§; Súmula 314-STJ | Só interrupção modelada (`DividaAtiva.cs:398-429`); `EstaPrescrita` só exclui Parcelada/Quitada/Cancelada | **AUSENTE** — sem estado `Suspensa` nem clock intercorrente quinquenal; risco de declarar prescrita dívida suspensa ou perder a intercorrente. |
| **Demais causas CTN 151** | moratória, depósito, reclamação/recurso, liminar | Só parcelamento suspende (`SituacaoDividaAtiva.cs:18`) | **PARCIAL** — faltam 4 das 5 hipóteses; impacta CND/CPEN e prescrição. |
| **Limite de pessoal LRF** | arts. 18-23 (54%/60% RCL) | Só **indicador/alerta** no Painel (`PainelGestorDtos.cs:56-65`); RCL é input manual | **PARCIAL** — mostra %, não impede ato que viole o limite. |
| **Mínimos Saúde/Educação/FUNDEB** | CF 198/212; LC 141; EC 108 | **Apuração real** (`ApurarAsps.cs`, `ApurarMde.cs`, `ApurarFundeb70.cs`) | Calcula/classifica mas **não trava** execução abaixo do piso — falta gate de alerta. |
| **PCASP completo** | Portaria STN/MF 3.133/2025 | Catálogo mínimo (~62/70 contas, `[validar-plano-oficial]`) | Importar elenco oficial 2026 p/ MSC/RREO/RGF reais. |

### 5.3 Operação diária / control-plane

- **Control-plane `PlatformDbContext` sem auditoria na hash-chain:** registro de tenants, licenças de módulo e índice email→tenant (pivô de login) fica **fora** da trilha. Adicionar interceptor de auditoria próprio (cadeia separada) ou log WORM. Citação: `PlatformDbContext.cs` herda `DbContext` puro; `AddPlatform` sem `.AddInterceptors`.

---

## 6. RISCOS de segurança / infra / jurídicos (priorizados + fix)

| # | Achado | Sev. | Arquivo:linha | Fix |
|---|--------|------|---------------|-----|
| R1 | **Login sem lockout por conta** — só rate-limit global; vetor mais explorável (brute-force/stuffing) | ALTA | `Autenticar.cs:99-103`; grep `AccessFailedCount` = 0 | Contador de falhas por usuário + cooldown progressivo, auditado (P7). |
| R2 | **Azure SQL exposto à internet** — `publicNetworkAccess: Enabled` + `AllowAllAzureIps` (qualquer recurso de qualquer tenant Azure) | ALTA | `infra/main.bicep:77,91-96` | Private Endpoint / `Disabled` (P10). |
| R3 | **Control-plane sem auditoria** — mutações de tenant/licença/índice de login fora da hash-chain | ALTA | `PlatformDbContext.cs` (`DbContext` puro, sem interceptors) | Interceptor de auditoria dedicado (§5.3). |
| R4 | **Decadência ISS errada (173 I em vez de 150 §4)** | ALTA (jurídico) | `Lancamento.cs:97-101,124-128`; `ApurarIssMensal.cs:112` | Ramo de homologação + `TipoLancamento` (§5.2). |
| R5 | **Prescrição intercorrente ausente (LEF art. 40)** | ALTA (jurídico) | `DividaAtiva.cs:450-452` | Estado `Suspensa` + janela 1 ano + clock quinquenal (§5.2). |
| R6 | **3 CVEs transitivos** (2 altas) — não pinados | MÉDIA | `Directory.Packages.props` | Pin central explícito (P9). |
| R7 | **Sem HSTS / security headers / HTTPS redirect na app** (borda Azure força httpsOnly+TLS1.2, mas app não emite headers) | MÉDIA | ausente em `Program.cs`; grep = 0 | `UseHsts` + middleware de headers (P8). |
| R8 | **Filtro de tenant sem `IModelCacheKeyFactory`** — Global Query Filter fecha sobre `Expression.Constant(context)`, EF cacheia modelo por tipo | MÉDIA (alta se 2 tenants compartilharem banco) | `ModelBuilderExtensions.cs:30,78` | **Não vaza hoje** porque o isolamento real é **database-per-tenant** (provado em E2E); o filtro é defesa-em-profundidade. Registrar `IModelCacheKeyFactory` ou ler propriedades vivas do contexto; documentar a separação física como invariante. |
| R9 | **NetArchTest cego em Cofre/Identidade** na regra de referência cruzada (as 2 mais sensíveis: KEK/A1, RBAC/senha). Pureza de camada **cobre** ambas. | MÉDIA | `FitnessFunctions.cs:15-20` | Incluir `"Cofre","Identidade"` no array `Modulos`. |
| R10 | **JWT HS256 simétrico, 60 min, sem refresh/revogação** — segredo único assina e valida; sem jti-blacklist. (Segredo vem do Key Vault com fail-fast em prod.) | MÉDIA | `EmissorToken.cs:76-77`; `JwtOptions.cs:22` | RS256/EdDSA (chave no Key Vault) + refresh + revogação por `jti`. |
| R11 | **Rate-limit de login fraco** — login anônimo cai no global (100/min/IP), sem partição por conta-alvo | MÉDIA | `Program.cs` global; `IdentidadeEndpoints.cs:63` (sem `RequireRateLimiting`) | Policy `"login"` (5-10/min por IP+e-mail). |
| R12 | **`AllowedHosts: "*"`** — sem validação de Host header | BAIXA | `src/ApiHost/appsettings.json` | Fixar host(s) de produção em `appsettings.Production.json`. |

---

## 7. Roteiro para "impecável end-to-end" (sequenciado, SEM reescrita)

**Premissa fundamental:** nenhum dos furos acima é estrutural. A arquitetura modular, o domínio rico, a contabilidade, a folha, os tributos e a segurança de base **já são produção**. O trabalho restante é **completar dados/grades, trocar gateways simulados por reais (cred-gated) e endurecer infra** — incremental, sem refatoração profunda.

### Fase A — Desbloquear o piloto (1-2 semanas, sem credenciais)
1. **Picker de Tributos** (P1) — desbloqueia o balcão.
2. **Security headers + HSTS** (P8) e **fechar os 3 CVEs** (P9) — hardening barato e visível.
3. **Login com lockout por conta** (P7) e **policy de rate-limit `"login"`** (R11).
4. **Corrigir jurídico fino:** decadência ISS 150 §4 (R4) + prescrição intercorrente LEF art. 40 (R5) — antes de qualquer cobrança/baixa real.

### Fase B — Passar no TCE-RS e nas obrigações (depende de grade/credencial)
5. **Folha-TCE 1099** (P3) — confirmar no e-Validador (já é o mais pronto).
6. **SIAPC/PAD grade 2026** (P2) — JSON versionado + e-Validador oficial.
7. **LicitaCon** (P4) — completar os 8 arquivos vazios + mapear CNPJ real do fornecedor.
8. **eSocial transporte real** (P6) — XSD + `ESocialGatewaySoap` + mTLS + A1 do Cofre.
9. **NFS-e ADN** (P5) — endpoint oficial + contrato XML nacional.

### Fase C — Endurecer infra de produção
10. **Azure SQL** (P10) — Private Endpoint / `publicNetworkAccess: Disabled`.
11. **Control-plane com auditoria** (R3) — interceptor dedicado no `PlatformDbContext`.
12. **`IModelCacheKeyFactory`** (R8) e **NetArchTest em Cofre/Identidade** (R9) — fechar a defesa-em-profundidade.
13. **JWT RS256 + refresh/revogação** (R10).

### Fase D — Cuidar do município inteiro (operação diária)
14. **Trocar gateways simulados por reais** (cred-gated): SICONFI, PNCP, Receita/CNPJ, CadÚnico, Transferegov, e-SUS/CNES, DETRAN, SIOPE/Educacenso.
15. **PCASP completo 2026** + RREO/RGF (MDF) + EFD-Reinf R-4000.
16. **PainelGestor/BI** e **SPA cidadão** (guias/2ª via, carnê IPTU, login gov.br).
17. **MSC 3 espécies** (saldo_inicial/movimento/saldo_final — hoje hardcoded `saldo_final` em `GeradorMscCsv.cs:106`).

---

> **Para a nova equipe** — arquivos-chave: `src/ApiHost/Program.cs` (rate-limit/middleware, falta HSTS), `infra/main.bicep:77,91` (SQL aberto), `src/Modules/Identidade/.../Autenticacao/Autenticar.cs` (sem lockout), `src/Modules/RecursosHumanos/.../ESocial/ESocialGatewaySimulado.cs` (transporte fake), `src/Modules/Transparencia/.../Leiautes/LeiauteSiapc.cs` (TODO leiaute), `src/Modules/Tributos/.../Nfse/AdnNfseGateway.cs` + `TributosModule.cs:104` (toggle ADN), `src/Modules/Financas/.../Seed/PlanoDeContasCatalogo.cs` (catálogo mínimo), `src/Platform/.../PlatformDbContext.cs` (control-plane sem auditoria). Revisão humana detalhada por domínio em `docs/REVISAO-HUMANA/`; rastreabilidade normativa em `docs/normas/FONTES-NORMATIVAS.md`.
