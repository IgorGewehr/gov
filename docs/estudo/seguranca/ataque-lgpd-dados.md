# Red-Team LGPD — Exposição de dados sensíveis (CPF, saúde, assistência, menores)

Escopo: análise estática do código em `src/`. Dimensão: LGPD — masking, trilha de
ACESSO (quem leu o quê), base legal nos módulos sensíveis, vazamento em respostas/logs/auditoria.
Alvos principais: `Saude`, `AssistenciaSocial`, `RecursosHumanos`, `Educacao` + cross-cutting
(`AuditSaveChangesInterceptor`, visualizador de auditoria, exception handling).

`[a confirmar]` = exige runtime para validação definitiva.

**Total de achados: 9** (3 CRÍTICOS, 3 ALTOS, 2 MÉDIOS, 1 BAIXO)

---

## CRÍTICO

### C1 — Trilha de acesso ao prontuário SUAS é FORJÁVEL: `UsuarioId` e `MotivoAcesso` vêm do cliente, não do principal autenticado

- Arquivo: `src/Modules/AssistenciaSocial/Tensorroot.Gov.Modules.AssistenciaSocial.Infrastructure/AssistenciaSocialEndpoints.cs:111-114`
- Arquivo: `src/Modules/AssistenciaSocial/Tensorroot.Gov.Modules.AssistenciaSocial.Application/Prontuarios/ObterProntuarioDaFamilia.cs:41-44,78`
- Arquivo: `src/Modules/AssistenciaSocial/Tensorroot.Gov.Modules.AssistenciaSocial.Application/Prontuarios/RegistrarAcessoProntuario.cs:16-19,49`

O endpoint da leitura sigilosa do prontuário:

```csharp
grupo.MapGet("/familias/{familiaId:guid}/prontuario", async (
    Guid familiaId, Guid usuarioId, string motivoAcesso, ISender sender, ...)
    => Results.Ok(await sender.Send(new ObterProntuarioDaFamiliaQuery(familiaId, usuarioId, motivoAcesso), ...)))
    .RequirePermission("assistenciasocial.ver");
```

`usuarioId` e `motivoAcesso` são **query string controlada pelo atacante**. O handler grava
exatamente esses valores na trilha imutável (`prontuario.RegistrarAcesso(request.UsuarioId, request.MotivoAcesso, ...)`),
**sem nunca comparar com o usuário autenticado** (`ICurrentUser`).

Cenário de exploração: assistente social mal-intencionado (ou conta comprometida) lê o prontuário
de uma família — incluindo violação envolvendo criança/adolescente (`PossuiViolacaoCriancaAdolescente`,
art. 11 LGPD + ECA) — passando `usuarioId=<id-de-um-colega>&motivoAcesso=auditoria de rotina`. A
trilha de acesso "à prova de adulteração" passa a **incriminar terceiro** e o atacante fica invisível.
Isto destrói o valor probatório da trilha (I-7/I-8) — exatamente a evidência exigida pela LGPD para
demonstrar quem acessou dado sensível.

O sistema **tem** `ICurrentUser` (`UserId`, `IpAddress`) e ele já é usado em outros módulos
(`Modules/Identidade/.../AtribuirPapelAoUsuario.cs`), então a correção é arquiteturalmente trivial.

Correção:
- Remover `usuarioId` da assinatura HTTP. Derivar o usuário **exclusivamente** de `ICurrentUser.UserId`
  no handler (injetar `ICurrentUser`). Manter `motivoAcesso` como entrada, mas validá-lo e registrar
  junto o `UserId`/`IpAddress` do principal.
- Idem para `POST /prontuarios/{id}/acessos` (`RegistrarAcessoProntuarioCommand.UsuarioId`).

---

### C2 — Leituras de dados de saúde NÃO geram trilha de acesso (doc mente; implementação ausente)

- Arquivo: `src/Modules/Saude/Tensorroot.Gov.Modules.Saude.Application/Pacientes/ObterPacientePorCns.cs:28,33-58`
- Arquivo: `src/Modules/Saude/Tensorroot.Gov.Modules.Saude.Application/Pacientes/ObterHistoricoClinicoDoPaciente.cs:31,40-57`
- Suporte: `src/SharedKernel/NivelSensibilidade.cs:5-6,27` (declara "SensivelLGPD sempre gera trilha de leitura" — só "uso futuro")
- Suporte: `src/BuildingBlocks/.../Auditing/AuditSaveChangesInterceptor.cs:25-49` (interceptor só dispara em `SavingChanges`/mutações)

Os docstrings afirmam: *"gera trilha de acesso ao prontuario"* (CNS) e *"Dado sensivel ... gera
trilha de acesso ao prontuario"* (histórico clínico). **Nenhum dos dois handlers grava qualquer
trilha** — apenas leem e projetam. Diferente do `AssistenciaSocial`, que ao menos tenta (vide C1),
em `Saude` **não há absolutamente nenhum registro de leitura**.

Como a única trilha existente (`AuditSaveChangesInterceptor`) é acionada apenas em `SaveChanges`
(operações de escrita), **toda leitura de CID-10/CIAP-2, alergias, condições crônicas e CNS é
invisível**. Isso viola o requisito de CLAUDE.md §6 ("trilha de acesso — quem leu o quê, quando,
por quê") e o princípio do art. 37 LGPD (registro de operações de tratamento, incluindo acesso a
dado sensível de saúde).

Cenário: funcionário com `saude.ver` varre `GET /api/saude/pacientes/{id}/historico-clinico` de
toda a população atendida e exporta o histórico clínico. **Não há rastro** — nem no banco, nem na
trilha de auditoria, nem nos logs (o `LoggingBehavior` só loga o nome do request, ver M2).

Correção:
- Introduzir trilha de leitura para recursos `NivelSensibilidade.SensivelLGPD` (a abstração já está
  declarada). Cada query sensível registra `{TenantId, UserId, Ip, EntidadeAcessada, EntityId,
  BaseLegal, Timestamp}` em tabela append-only encadeada (reusar `AuditHashChain`).
- Fazer isso de forma transversal (pipeline behavior MediatR para queries marcadas como sensíveis),
  não handler-a-handler, para não depender de cada autor lembrar.

---

### C3 — Trilha de auditoria de escrita armazena CPF, NIS e dados clínicos em TEXTO CLARO e o visualizador os devolve crus

- Arquivo: `src/BuildingBlocks/.../Auditing/AuditSaveChangesInterceptor.cs:104-160` (serializa `OldValues`/`NewValues` sem masking)
- Arquivo: `src/SharedKernel/IHasRedactedAuditFields.cs` (mecanismo de redação **opt-in**, só implementado pelo Cofre)
- Confirmação: só `Modules/Cofre/.../CertificadoA1Cofre.cs` implementa `IHasRedactedAuditFields` (grep em `src/Modules`)
- Arquivo: `src/Modules/RecursosHumanos/.../Persistence/Configurations/ServidorConfiguration.cs:23-25` (CPF persistido como `cpf.Digitos` — 11 dígitos crus)
- Arquivo: `src/ApiHost/Admin/AdminEndpoints.cs:121-124,171-172` (retorna `OldValues`/`NewValues` sem masking)

O interceptor de auditoria só redige (`[REDACTED]`) colunas listadas por entidades que implementam
`IHasRedactedAuditFields` — e a **única** entidade que o faz é o certificado A1 do Cofre. Para
todas as demais, ele serializa o `CurrentValue`/`OriginalValue` direto em JSON. Como o `Servidor`
persiste o CPF como `cpf.Digitos` (11 dígitos sem máscara), **cada inserção/alteração de servidor
grava o CPF completo em `NewValues`**. O mesmo vale para NIS, nome civil, dados do CadÚnico e
qualquer campo sensível dos módulos Saúde/Assistência/Educação que sofra mutação.

O visualizador admin (`GET /api/admin/auditoria`, permissão `admin.auditoria.ver`) devolve
`OldValues` e `NewValues` **na íntegra**, sem qualquer masking. Ou seja: o masking de CPF feito na
projeção de leitura do RH (`ProjetarServidor.MascararCpf`, `***.NNN.***-**`) é **contornável** —
basta um admin (ou conta com `admin.auditoria.ver`) ler a trilha para obter o CPF completo de todos
os servidores. A trilha de auditoria virou um banco-sombra de dados sensíveis em claro.

Cenário: conta com `admin.auditoria.ver` exporta `GET /api/admin/auditoria?entidade=Servidor` e
coleta CPF + nome de toda a folha; ou `entidade=Familia`/CadÚnico para NIS + renda. Nenhuma das
proteções de minimização aplicadas nas leituras "normais" se aplica aqui.

Correção:
- Mudar a redação de auditoria de opt-in para **deny-by-default em campos sensíveis**: marcar CPF,
  NIS, CNS, nome civil, dados clínicos como redigíveis por padrão (ex.: atributo/registro central de
  classificação por `NivelSensibilidade`), e o interceptor mascara/tokeniza antes de serializar.
- O visualizador deve mascarar `OldValues`/`NewValues` conforme a sensibilidade da coluna, e exigir
  permissão segregada (e trilha de leitura própria — recursão de C2) para qualquer "desmascarar".

---

## ALTO

### A1 — Sem exception handler global: mensagens de domínio e (em ambiente não-prod) stack traces vazam ao cliente

- Arquivo: `src/ApiHost/Program.cs` (nenhum `UseExceptionHandler`/`AddProblemDetails`/`IExceptionHandler` registrado)
- Gatilhos: `ObterHistoricoClinicoDoPaciente.cs:46` (`throw new InvalidOperationException("Paciente nao encontrado.")`), `ObterProntuarioDaFamilia.cs:74`, `ObterResumoCadUnico.cs:40`, etc.

Não há tratamento global de exceções. Exceções de domínio (`InvalidOperationException` com mensagens
PT-BR) sobem até o middleware default do ASP.NET. Em `IsDevelopment()` o developer exception page
expõe stack trace e detalhes internos; fora de dev, o 500 default ainda **diferencia
comportamento** (mensagem/types) e permite enumeração: "Paciente nao encontrado" vs. 200 revela se
um CNS/`pacienteId`/`familiaId` existe naquele tenant — vazamento por canal lateral de existência de
indivíduo em base de saúde/assistência (já é dado pessoal: "fulano é paciente/atendido aqui").
`[a confirmar]` o conteúdo exato do corpo em produção depende do ambiente efetivo.

Correção: registrar `IExceptionHandler`/`UseExceptionHandler` com `ProblemDetails` neutro (sem
mensagem de domínio nem stack), mapear "não encontrado" para 404 **genérico e idêntico** ao
"sem permissão de ver aquele recurso", e garantir `IsDevelopment()==false` em produção.

### A2 — Base legal (LGPD art. 7/11) não é aplicada nem registrada nos módulos sensíveis

- Arquivos: todos os handlers de leitura sensível (Saúde/Assistência/Educação) — nenhum captura/valida hipótese legal
- Suporte: `src/SharedKernel/NivelSensibilidade.cs:27` ("sempre exige base legal" — declarado só para uso futuro)

O `MotivoAcesso` do prontuário SUAS é texto livre não validado e **não é uma base legal** estruturada.
Em Saúde, Educação (menores) e RH não há sequer campo de finalidade/hipótese legal. A LGPD exige
hipótese de tratamento explícita para dado sensível (art. 11) e dado de criança/adolescente (art. 14).
O código não modela, não exige e não registra a base legal por operação de acesso/tratamento.

Cenário: em fiscalização ANPD/TCE, não é possível demonstrar a hipótese legal aplicada a cada acesso
ao histórico clínico ou ao diário de classe de um menor — ausência de accountability (art. 37/40).

Correção: modelar `BaseLegal` (enum por hipótese) como parâmetro obrigatório das operações sobre
recursos `SensivelLGPD`, validar coerência (ex.: "consentimento" exige referência a registro de
consentimento) e persistir junto à trilha de leitura (C2).

### A3 — `assistenciasocial.ver`/`saude.ver` é grosso demais: uma única permissão libera dado civil agregado E conteúdo sigiloso

- Arquivo: `src/Modules/AssistenciaSocial/.../AssistenciaSocialEndpoints.cs:46-54,71-79,111-119` (todos sob `assistenciasocial.ver`)
- Arquivo: `src/Modules/Saude/.../SaudeEndpoints.cs:34,38,84,88,143,147` (todos sob `saude.ver`)

A mesma permissão `*.ver` autoriza tanto listagens minimizadas (NIS mascarado, território) quanto a
leitura do **conteúdo sigiloso** (prontuário com violação contra menor; histórico clínico/CID).
Não há clearance por `NivelSensibilidade` (declarado, não aplicado — `NivelSensibilidade.cs:8-13`).
Viola o "acesso por necessidade de serviço" e o princípio da minimização: quem só precisa de
listagem territorial recebe, com a mesma claim, poder de abrir prontuários sigilosos.

Cenário: recepcionista com `saude.ver` (concedido para conferir CNS) acessa históricos clínicos
completos. Privilégio excessivo por design.

Correção: separar verbos (ex.: `saude.ver` vs. `saude.prontuario.ler`; `assistenciasocial.ver` vs.
`assistenciasocial.prontuario.sigilo`) e aplicar a checagem de clearance ABAC por
`NivelSensibilidade(recurso) <= clearance(sujeito)` já modelada.

---

## MÉDIO

### M1 — `EhPcd` (dado sensível, art. 11) e renda individual trafegam sem masking/minimização no payload de entrada e ecoam na trilha

- Arquivo: `src/Modules/AssistenciaSocial/.../Familias/MembroFamiliarDto.cs:9-14` (`Cpf`, `EhPcd`, `RendaIndividual`)
- Endpoint: `AssistenciaSocialEndpoints.cs:32-37` (`PUT /familias/{id}/renda` recebe lista de membros com CPF cru)

`EhPcd` (deficiência) é dado sensível LGPD. O DTO recebe CPF em claro + indicador PCD + renda por
membro; ao persistir/alterar, esses campos vão para a trilha de escrita em claro (encadeia com C3).
Não há masking nem segregação de quem pode ver indicador de deficiência por membro.

Correção: tratar `EhPcd` como `SensivelLGPD` (redação na trilha + clearance); minimizar retorno.

### M2 — `UseSerilogRequestLogging` + binding de querystring sensível arrisca vazar dado pessoal em logs

- Arquivo: `src/ApiHost/Program.cs:176` (`app.UseSerilogRequestLogging()` sem enriquecimento que omita querystring sensível)
- Arquivo: `AssistenciaSocialEndpoints.cs:111-114` (`usuarioId`, `motivoAcesso` em querystring); `SaudeEndpoints.cs:32` (CNS em path)

O request logging do Serilog registra `RequestPath`/`QueryString` por padrão. Caminhos como
`/api/saude/pacientes/por-cns/{cns}` colocam o **CNS (dado de saúde, identifica o paciente)** na URL,
e o prontuário recebe `motivoAcesso` (pode conter texto livre identificável) na querystring — tudo
potencialmente persistido no log estruturado (console/sink). `[a confirmar]` se o sink de produção
retém querystring/path; o template default do `UseSerilogRequestLogging` inclui `RequestPath`.
O `LoggingBehavior` (`Behaviors/LoggingBehavior.cs:25,30`) está correto (só nome do request) — o
risco está na camada HTTP.

Correção: mover identificadores sensíveis para o corpo/headers, não a URL; configurar
`GetMessageTemplateProperties`/enrichers para redigir path/querystring sensível; nunca logar
`MotivoAcesso`/CNS.

---

## BAIXO

### B1 — `Cpf.Formatar()`/`ToString()` expõem CPF completo e são fáceis de usar por engano em respostas

- Arquivo: `src/SharedKernel/ValueObjects/Cpf.cs:69,72` (`Formatar()` → `000.000.000-00`; `ToString()` → 11 dígitos)

O VO oferece `Formatar()` e `ToString()` que devolvem o CPF completo. O masking correto
(`ProjetarServidor.MascararCpf`) é uma função **isolada e privada** de um único módulo; qualquer
nova projeção que faça `cpf.Formatar()` ou interpole `cpf` vaza o documento sem aviso do compilador.
Risco latente de regressão de masking à medida que módulos sensíveis crescem (M5–M7).

Correção: expor `Mascarado` como membro canônico do próprio `Cpf` (centralizar a regra de masking no
VO, como já existe `Nis.Mascarado` no AssistenciaSocial) e desencorajar `Formatar()` para saída ao
cliente (documentar/`[Obsolete]` para borda de API ou analyzer).

---

## Resumo dos 3 piores

1. **C1 — Trilha de acesso ao prontuário SUAS forjável.** `usuarioId` e `motivoAcesso` vêm da
   querystring e são gravados crus na trilha imutável sem checar `ICurrentUser`; atacante lê dado
   sensível de menor (ECA/art. 11) e incrimina um colega — a evidência LGPD vira prova falsa.
   (`AssistenciaSocialEndpoints.cs:111-114`, `ObterProntuarioDaFamilia.cs:78`)

2. **C2 — Saúde sem nenhuma trilha de leitura.** Os handlers afirmam gerar trilha de acesso, mas não
   geram; o único interceptor só registra escrita. Histórico clínico/CID/alergias podem ser varridos
   sem deixar rastro — viola CLAUDE.md §6 e art. 37 LGPD.
   (`ObterHistoricoClinicoDoPaciente.cs:31,40-57`, `AuditSaveChangesInterceptor.cs:25-49`)

3. **C3 — Auditoria de escrita guarda CPF/NIS/clínico em claro e o visualizador devolve cru.** A
   redação é opt-in e só o Cofre a usa; CPF do servidor é persistido cru e cai em `NewValues`. O
   masking das leituras normais é contornável via `GET /api/admin/auditoria`.
   (`AuditSaveChangesInterceptor.cs:104-160`, `ServidorConfiguration.cs:23-25`, `AdminEndpoints.cs:121-124`)
