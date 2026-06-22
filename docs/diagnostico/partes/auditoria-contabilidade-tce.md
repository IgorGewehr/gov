# Auditoria — Contabilidade (PCASP) e Prestação de Contas (TCE-RS / SICONFI)

> Auditoria EMPÍRICA do código real em `src/Modules/Financas` e `src/Modules/Transparencia`.
> Data: 2026-06-22. Método: leitura direta dos `.cs`, contagem de arquivos/ocorrências, rastreio de publishers/consumers.
> Spec de referência: `docs/architecture/contabilidade-pcasp-tce.md`.

## Veredito em uma linha

**A contabilidade PCASP NÃO EXISTE no código** (zero linhas), e toda a prestação de contas ao TCE-RS é **fachada**: leiaute genérico, e-Validador/transmissão/SICONFI/SIAPC-PAD são gateways `Simulado` sem efeito externo, sem assinatura A1, alimentados por um read model fixo e por um evento de integração que **ninguém publica**.

---

## 1. O que EXISTE

### Financas (módulo que deveria conter a Contabilidade)
Existe apenas o **ciclo orçamentário da despesa (Lei 4.320/64)** — não a contabilidade:

| Agregado | Arquivo | Papel |
|---|---|---|
| `DotacaoOrcamentaria` | `Domain/Dotacoes/DotacaoOrcamentaria.cs` | Crédito orçamentário |
| `Empenho` | `Domain/Empenhos/Empenho.cs` | 1º estágio |
| `Liquidacao` | `Domain/Liquidacoes/Liquidacao.cs` | 2º estágio |
| `OrdemDePagamento` | `Domain/Pagamentos/OrdemDePagamento.cs` | 3º estágio |
| `RestoAPagar` | `Domain/RestosAPagar/RestoAPagar.cs` | Inscrição de RAP |
| `ReceitaArrecadada` | `Domain/Receitas/ReceitaArrecadada.cs` | Receita |

- Domain Events (`Domain/Events/FinancasDomainEvents.cs`): `EmpenhoEmitido`, `DespesaLiquidada`, `PagamentoEfetuado`, `ReceitaArrecadada`, etc. — todos **orçamentários**, nenhum contábil/patrimonial.
- Contracts (2 eventos): `DespesaEmpenhadaIntegrationEvent`, `PagamentoEfetuadoIntegrationEvent`. **Não há** evento de MSC nem contábil.

### Transparencia (consumidor / prestação de contas)
Existe o **esqueleto do ciclo de remessa** com domínio rico de estados, mas sem conteúdo regulatório real:

- `RemessaTce` (raiz) + `ArquivoRemessa`, `RegistroLeiaute`, `Leiaute`, `Periodo`, `HashIntegridade`, `ResultadoValidacao` — máquina de estados `Gerada → Validada → Enviada` (+ rejeição/vencimento).
- `DeclaracaoFiscal` (raiz MSC/SICONFI) + `MatrizSaldos`, `LinhaContabil` — read model com invariante `EstaBalanceada` (ΣD=ΣC).
- Handlers: `GerarRemessaTce`, `ValidarRemessaTce`, `EnviarRemessaTce`, `HomologarRemessaTce`, `VencerPrazoRemessaTce`; `ConsolidarDeclaracaoFiscal`, `TransmitirDeclaracaoFiscal`, etc.
- Outbox configurado (`ConvertDomainEventsToOutboxInterceptor`, `DrenarOutboxAsync`); auditoria e tenant interceptors presentes.
- **Testes:** Transparencia tem cobertura razoável de fluxo de estado (`RemessaTceFluxoTests` 23 casos, `DeclaracaoFiscalFluxoTests` 22). Financas: `EmpenhoFluxoTests` (2) + `IntegracaoCrossModuleTests` (1) — nenhum teste contábil.

---

## 2. Profundidade real (o que é raso/inexistente)

### PCASP — INEXISTENTE (contagem = 0)
Busca por `PlanoDeContas | ContaContabil | LancamentoContabil | PartidaContabil | EventoContabil | Balancete` em `src/Modules/Financas`: **0 arquivos, 0 entidades**. As 2 ocorrências da string "PCASP" são apenas comentários (`ClassificacaoOrcamentaria.cs`, e o `// TODO(revisao-contabil)` em `EncerrarExercicio.cs`).

Não existe:
- Plano de Contas / `ContaContabil` (código, natureza da informação, natureza do saldo, nível, conta-mãe).
- `LancamentoContabil` / `PartidaContabil` — **não há partida dobrada em lugar nenhum**.
- `EventoContabil` (roteiro fato→partidas) parametrizável.
- Lançamento automático a partir dos Domain Events do ciclo (o "coração da integração" da spec §2 não foi escrito).
- `Balancete` (read model de saldos).
- **Geração da MSC a partir do PCASP** — a "FONTE" definida na spec não existe.

`EncerrarExercicio.cs` documenta explicitamente o buraco:
`// TODO(revisao-contabil): operacao contabil sensivel (apuracao de superavit financeiro, RAP por fonte, lancamentos PCASP) — aqui apenas a inscricao de RAP por empenho`.

### MSC — read model órfão (origem fantasma)
- `MatrizSaldos` em Transparencia existe e calcula ΣD/ΣC, **mas é alimentado por dados externos**, não derivado de lançamentos.
- O consumidor `ReceberMSCGeradaHandler` escuta `MSCGeradaIntegrationEvent` — porém esse evento está definido **localmente dentro de `Transparencia.Application/Integracoes/MSCGeradaIntegrationEvent.cs`** como placeholder ACL ("enquanto o modulo Financas... ainda nao foi gerado").
- Rastreio `Publish(MSCGerada...)` / `new MSCGeradaIntegrationEvent(...)` em todo `src`: **0 ocorrências**. **Ninguém publica o evento** → o pipeline contabilidade→MSC nunca dispara. É um consumidor ligado a um produtor inexistente.

### Remessa TCE-RS (SIAPC/PAD) — leiaute GENÉRICO, não real
`GerarRemessaTceHandler.MontarPacote` monta registros como string livre:
```csharp
var conteudoTexto = string.Join('\n', itens.Select(item => $"{item.Tipo}|{item.Conteudo}"));
var nomeArquivo = $"{leiaute.Codigo}_{periodo}.txt";
```
`RegistroLeiaute` é só `(string Tipo, string Conteudo)` — **sem posição/tamanho/formato campo-a-campo** do SIAPC/PAD. Os itens vêm de `SimuladoPublicacaoTransparenciaRepository`, que retorna 3 linhas fixas hardcoded (`CABECALHO|... / BALANCO_ORCAMENTARIO|... / RODAPE|...`). Não é o leiaute oficial do TCE-RS; é um TXT pipe-delimited de demonstração. (Nota: a spec promete marcadores `// TODO(validar-leiaute-oficial)` — busca no código real: **0 ocorrências**; o débito não está sequer sinalizado no código.)

### e-Validador / RDI — STUB
`SimuladoEValidadorTce`: retorna RDI sem erro desde que haja ≥1 arquivo; única "ocorrência" possível é "pacote sem arquivos". **Nenhuma regra do RDI vigente** é implementada.

### Transmissão (SOAP/REST SICOE) — STUB total
`SimuladoSiapcPadGateway.TransmitirAsync` → `return Task.CompletedTask;` (corpo vazio, sem efeito externo). `SimuladoSiconfiGateway` retorna protocolo determinístico fake. Busca por `HttpClient | System.ServiceModel | SOAP real | WebRequest` em Transparencia: **0**. Não há canal real, nem Polly aplicado a chamada externa (não há chamada externa). O comentário em `EnviarRemessaTce.cs` ("ACL + Polly... assinatura A1") é só documentação sobre o stub.

### Assinatura A1 — INEXISTENTE
Busca por `SignedXml | X509Certificate | .pfx | KeyVault | assinatura` (fora de comentários) em Transparencia: **0 implementações**. Todas as 3 menções a "A1/certificado" são comentários XML-doc descrevendo o que a implementação real *faria*. Nenhum acesso a Azure Key Vault, nenhum carregamento de certificado, nenhuma assinatura de pacote.

---

## 3. Real × Stub (tabela-resumo)

| Item | Esperado (spec/constituição) | No código | Classificação |
|---|---|---|---|
| Plano de Contas PCASP / `ContaContabil` | Agregado rico | inexistente | **AUSENTE** |
| `LancamentoContabil` partida dobrada (ΣD=ΣC) | Invariante forte | inexistente | **AUSENTE** |
| `EventoContabil` (roteiro parametrizável) | Mapa fato→partidas | inexistente | **AUSENTE** |
| Lançamento contábil automático (handlers dos eventos) | Coração da integração | inexistente | **AUSENTE** |
| `Balancete` (saldos por conta/período) | Read model | inexistente | **AUSENTE** |
| Geração da MSC em Financas + `MSCGeradaIntegrationEvent` | Publica via Outbox | evento definido como placeholder em Transparencia; **0 publishers** | **FACHADA (órfão)** |
| `MatrizSaldos` (consumo MSC) | Read model balanceado | existe, mas sem fonte | parcial / órfão |
| Leiaute SIAPC/PAD campo-a-campo | Posição/tamanho/formato oficial | `Tipo|Conteudo` string genérica; itens hardcoded | **FACHADA (genérico)** |
| e-Validador / RDI | Regras do RDI | `SimuladoEValidadorTce` (sempre passa) | **STUB** |
| Transmissão SOAP/REST SICOE + Polly | Canal real idempotente | `Task.CompletedTask` | **STUB** |
| Assinatura A1 (Key Vault) | Assinatura do pacote | 0 implementações (só comentários) | **AUSENTE** |
| SICONFI (declaração MSC) | Transmissão real | `SimuladoSiconfiGateway` protocolo fake | **STUB** |
| Máquina de estados da Remessa/Declaração | Transições válidas + testes | existe, com 45 testes de fluxo | **REAL (mas opera sobre dados fake)** |

---

## 4. Lacunas críticas (ordem de risco para o dono — Contabilidade e prestação de contas)

1. **Contabilidade PCASP do zero** — é a FONTE de tudo (spec §2/§5 Fase 2.2) e está 100% ausente. Sem ela, MSC, balancete, balanços e remessas "nascem ocos". Maior bloqueio.
2. **MSC sem origem** — o evento de integração que liga Financas→Transparencia não tem publisher; corrigir exige primeiro a contabilidade, depois mover o `MSCGeradaIntegrationEvent` para `Financas.Contracts` e publicá-lo via Outbox.
3. **Leiaute SIAPC/PAD oficial** — substituir o TXT genérico pelo leiaute campo-a-campo do TCE-RS vigente (registros, posições, tamanhos, versão). Hoje não passaria no e-Validador real.
4. **e-Validador/RDI real** — implementar regras; o stub aprova tudo, mascarando erros.
5. **Assinatura A1 + transmissão SICOE** — implementar assinatura com certificado do tenant (Key Vault) e canal SOAP/REST real com Polly/ACL/idempotência; ambos inexistentes.
6. **Read model de itens consolidados** — `SimuladoPublicacaoTransparenciaRepository` (3 linhas fixas) precisa ser materializado a partir de Integration Events reais.
7. **Sinalização de débito ausente** — a spec exige `// TODO(validar-leiaute-oficial)` (§16); o código tem **0** desses marcadores, então os pontos de leiaute oficial não estão rastreáveis no código.

## 5. Conclusão

O que está pronto é o **andaime de processo** (estados, Outbox, auditoria, testes de transição) — bem-feito, porém vazio de substância contábil e regulatória. **Nada nesta cadeia geraria uma prestação de contas válida ao TCE-RS hoje.** A Fase 2.2 (Contabilidade PCASP) e 2.3 (TCE real) descritas na spec **não foram iniciadas** no código; somente o esqueleto de Transparencia e o ciclo orçamentário de Financas existem.
