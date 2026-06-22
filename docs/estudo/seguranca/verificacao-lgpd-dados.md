# Verificação cética — Red-Team LGPD (exposição de dados sensíveis)

Verificador cético do relatório `ataque-lgpd-dados.md`. Cada achado foi reaberto **no código**
para decidir: **VULNERABILIDADE REAL** (com evidência arquivo:linha) ou **FALSO-POSITIVO**
(já mitigado por filtro/interceptor/teste). Análise estática de `src/`. `[a confirmar]` = exige
runtime para fechar 100%.

## Veredito agregado

| ID | Severidade alegada | Veredito | Severidade ajustada |
|----|--------------------|----------|---------------------|
| C1 | CRÍTICO | **REAL — confirmado** | CRÍTICO |
| C2 | CRÍTICO | **REAL — confirmado** | CRÍTICO |
| C3 | CRÍTICO | **REAL — confirmado** | CRÍTICO |
| A1 | ALTO | **REAL — confirmado** (parte do impacto requer runtime) | ALTO |
| A2 | ALTO | **REAL — confirmado** | ALTO |
| A3 | ALTO | **REAL — confirmado** | ALTO |
| M1 | MÉDIO | **REAL — confirmado** | MÉDIO |
| M2 | MÉDIO | **REAL — risco confirmado**, exposição efetiva `[a confirmar]` | MÉDIO |
| B1 | BAIXO | **REAL — confirmado** (risco latente) | BAIXO |

**Resultado: 9 reais, 0 falsos-positivos.** Nenhum achado estava mitigado por filtro,
interceptor ou teste. Rebaixamentos: nenhum. Ressalvas de runtime: A1 (corpo do 500 em prod) e
M2 (retenção de querystring/path no sink).

---

## CRÍTICO

### C1 — Trilha de acesso ao prontuário SUAS é FORJÁVEL — **REAL (CRÍTICO)**

Evidência:
- `AssistenciaSocialEndpoints.cs:111-114` — assinatura HTTP recebe `Guid usuarioId, string motivoAcesso`
  da **query string**; nenhum `ICurrentUser` é injetado:
  ```csharp
  grupo.MapGet("/familias/{familiaId:guid}/prontuario", async (
      Guid familiaId, Guid usuarioId, string motivoAcesso, ISender sender, CancellationToken ct)
      => Results.Ok(await sender.Send(new ObterProntuarioDaFamiliaQuery(familiaId, usuarioId, motivoAcesso), ct)))
  ```
- `ObterProntuarioDaFamilia.cs:78` — handler grava o `usuarioId` do cliente cru na trilha:
  `prontuario.RegistrarAcesso(request.UsuarioId, request.MotivoAcesso, agoraUtc);` — **sem** comparar com `ICurrentUser`.
- `AcessoProntuario.cs:41` — `public Guid UsuarioId { get; private set; }` persiste exatamente o valor recebido (entidade aceita qualquer GUID).
- `RegistrarAcessoProntuario.cs:16-19,49` — o mesmo padrão no `POST /prontuarios/{id}/acessos` (`payload.UsuarioId`).
- Contraprova de viabilidade da correção: `CurrentUser.cs:13-22` expõe `UserId`/`IpAddress`; o `AuditSaveChangesInterceptor` já consome `currentUser` (`AuditSaveChangesInterceptor.cs:157-158`).

Por que NÃO é falso-positivo: o `ObterProntuarioDaFamiliaValidator` (linhas 50-55) só valida `NotEmpty`/
`MaximumLength` — não há nenhum cruzamento com o principal autenticado em lugar nenhum do caminho.
O atacante com `assistenciasocial.ver` lê prontuário com violação contra menor
(`ProntuarioDetalhe.PossuiViolacaoCriancaAdolescente`) e grava na trilha imutável o id de um colega.
A trilha "à prova de adulteração" (cadeia de hash) sela um valor **já adulterado na origem** —
selo íntegro sobre dado falso. Confirmado.

### C2 — Leituras de saúde NÃO geram trilha de acesso — **REAL (CRÍTICO)**

Evidência:
- `ObterPacientePorCns.cs:34-57` — handler injeta só `IPacienteRepository`, lê e projeta; **sem** `IUnitOfWork`, **sem** `RegistrarAcesso`. Docstring (linha 28) afirma "gera trilha de acesso ao prontuario" — **falso**.
- `ObterHistoricoClinicoDoPaciente.cs:41-57` — idem; docstring (linha 31) afirma "gera trilha de acesso ao prontuario" — **falso**. Lê CID-10/CIAP-2 e alergias e retorna, sem nenhum write.
- `grep` em `src/Modules/Saude`: as **únicas** ocorrências de "trilha de acesso" são os dois docstrings mentirosos; não há entidade/DbSet de acesso (`SaudeDbContext.cs` só tem `Pacientes`, `Atendimentos`, `SolicitacoesRegulacao`).
- `AuditSaveChangesInterceptor.cs:25-49` — só dispara em `SavingChanges`/`SavingChangesAsync` (escrita). Leitura pura não passa por ele.
- Pipeline MediatR só tem `ValidationBehavior` e `LoggingBehavior` (`Behaviors/`); nenhum behavior de trilha de leitura.
- `NivelSensibilidade.cs:9` — a abstração de trilha de leitura está marcada "DECLARADO EM M1 APENAS PARA USO FUTURO".

Por que NÃO é falso-positivo: não há nenhum mecanismo (handler, behavior, interceptor) que registre
leitura sensível em Saúde. Varredura de `GET /pacientes/{id}/historico-clinico` é invisível. Viola
CLAUDE.md §6 ("trilha de acesso — quem leu o quê") e art. 37 LGPD. Confirmado.

### C3 — Auditoria de escrita guarda CPF/NIS/clínico em claro e o visualizador devolve cru — **REAL (CRÍTICO)**

Evidência:
- `AuditSaveChangesInterceptor.cs:111-135` — redação é **opt-in**: só entidades `IHasRedactedAuditFields`
  têm colunas substituídas por `[REDACTED]`; todas as demais serializam `CurrentValue`/`OriginalValue` cru em JSON (`newValues[name] = ... property.CurrentValue`).
- `grep IHasRedactedAuditFields` em `src/Modules`: **único** implementador é `Cofre/CertificadoA1Cofre.cs`. `Servidor`, `Familia`, `Paciente` etc. **não** implementam → CPF/NIS/clínico vão crus.
- `ServidorConfiguration.cs:23-25` — `builder.Property(s => s.Cpf).HasConversion(cpf => cpf.Digitos, ...)` persiste os **11 dígitos sem máscara**; cada insert/update do servidor grava o CPF completo em `NewValues`.
- `AdminEndpoints.cs:121-124` — visualizador (`GET /api/admin/auditoria`, perm `admin.auditoria.ver`) projeta `item.OldValues, item.NewValues` **na íntegra**, sem masking.
- `ProjetarServidor.cs:30-35` — o masking de leitura (`***.NNN.***-**`) é método **`private static`** isolado no módulo RH → não cobre a trilha. Confirma a "bypassabilidade".

Por que NÃO é falso-positivo: o masking de CPF aplicado nas leituras "normais" é literalmente
contornável lendo `GET /api/admin/auditoria?entidade=Servidor` — devolve CPF cru de toda a folha.
A trilha virou banco-sombra em claro. Confirmado.

---

## ALTO

### A1 — Sem exception handler global — **REAL (ALTO)** — impacto-prod `[a confirmar]`

Evidência:
- `grep -rn "IExceptionHandler|UseExceptionHandler|AddProblemDetails"` em `src/` → **zero ocorrências**. Nada registrado em `Program.cs`.
- `Program.cs:178` ativa `UseSwagger`/`UseSwaggerUI` só em `IsDevelopment()`, mas **não há** `UseDeveloperExceptionPage` explícito nem handler de produção — fica no comportamento default do ASP.NET.
- Gatilhos de domínio que sobem como exceção: `ObterHistoricoClinicoDoPaciente.cs:46` (`throw new InvalidOperationException("Paciente nao encontrado.")`), `ObterProntuarioDaFamilia.cs:74`, `RegistrarAcessoProntuario.cs:46`.

Por que NÃO é falso-positivo: sem `ProblemDetails` neutro, "não encontrado" (throw) difere de
"encontrado" (200) → canal lateral de existência de indivíduo em base de saúde/assistência (o próprio
fato "fulano é paciente aqui" já é dado pessoal). `[a confirmar]` o corpo exato do 500 em produção
(developer page só em dev). Risco confirmado; severidade ALTO mantida.

### A2 — Base legal (art. 7/11) não aplicada nem registrada — **REAL (ALTO)**

Evidência:
- `MotivoAcesso` (`ObterProntuarioDaFamilia.cs:40,54`) é texto livre validado só por `NotEmpty`/`MaximumLength(400)` — **não** é base legal estruturada.
- Handlers de Saúde (`ObterPacientePorCns`, `ObterHistoricoClinico`) e os endpoints de RH/Educação não têm sequer campo de finalidade/hipótese legal.
- `NivelSensibilidade.cs:25-29` — "sempre exige base legal" declarado, mas o `<remarks>` (linha 9) diz que a aplicação é "USO FUTURO".

Por que NÃO é falso-positivo: não há modelagem de `BaseLegal` (enum/hipótese) exigida por operação
sensível. Em fiscalização ANPD/TCE não se demonstra a hipótese por acesso. Confirmado.

### A3 — Permissão `*.ver` grossa demais — **REAL (ALTO)**

Evidência:
- `SaudeEndpoints.cs:34,38,84,88,143,147` — `por-cns`, `historico-clinico` (CID/alergias), atendimentos e regulação **todos** sob a mesma `RequirePermission("saude.ver")`.
- `AssistenciaSocialEndpoints.cs:46-54,71-79,111-119` — listagem territorial minimizada e leitura do prontuário sigiloso (com violação contra menor) **ambos** sob `assistenciasocial.ver`.
- `NivelSensibilidade.cs` declarado mas não aplicado como clearance ABAC (ver A2/C2).

Por que NÃO é falso-positivo: uma única claim concede tanto listagem minimizada quanto conteúdo
sigiloso. Recepcionista com `saude.ver` abre históricos clínicos completos. Privilégio excessivo
por design. Confirmado.

---

## MÉDIO

### M1 — `EhPcd` (art. 11) e renda individual sem masking, ecoam na trilha — **REAL (MÉDIO)**

Evidência:
- `MembroFamiliarDto.cs:9-14` — `record(string Cpf, int Parentesco, DateOnly DataNascimento, decimal RendaIndividual, bool EhPcd)`: CPF cru + indicador de deficiência + renda por membro.
- `AssistenciaSocialEndpoints.cs:32-37` — `PUT /familias/{id}/renda` recebe `IReadOnlyList<MembroFamiliarDto>` e despacha `AtualizarRendaFamiliarCommand`; ao persistir, encadeia com C3 (trilha em claro).

Por que NÃO é falso-positivo: `EhPcd` é dado sensível LGPD e não recebe nenhum tratamento de
redação/clearance; a mutação cai na trilha sem masking (C3). Confirmado. Severidade MÉDIO adequada
(escopo menor que C3, mas é instância concreta dele para dado de deficiência).

### M2 — Serilog request logging pode vazar dado pessoal em log — **REAL (MÉDIO)**, exposição `[a confirmar]`

Evidência:
- `Program.cs:176` — `app.UseSerilogRequestLogging();` sem enricher que omita/redija querystring/path. O template default inclui `RequestPath`.
- `SaudeEndpoints.cs:32` — CNS no **path** (`/pacientes/por-cns/{cns}`): CNS identifica o paciente (dado de saúde).
- `AssistenciaSocialEndpoints.cs:111-114` — `usuarioId`/`motivoAcesso` na **querystring** (motivo pode conter texto livre identificável).
- Contraste correto: `LoggingBehavior.cs:24-25` loga só `typeof(TRequest).Name` — a camada MediatR está OK; o risco é HTTP.

Por que NÃO é falso-positivo: o caminho de exposição existe na configuração atual. `[a confirmar]`
se o sink de produção retém path/querystring (depende de `appsettings`/sink efetivo). Risco real
confirmado; magnitude depende de runtime.

### B1 — `Cpf.Formatar()`/`ToString()` expõem CPF completo — **REAL (BAIXO, latente)**

Evidência:
- `Cpf.cs:69` — `Formatar() => "000.000.000-00"` (completo); `Cpf.cs:72` — `ToString() => Digitos` (11 dígitos).
- Não há membro `Mascarado` no `Cpf`. Já existe o padrão certo no NIS: `Nis.cs:20` — `public string Mascarado => "********" + Digitos[8..]`.
- O masking de CPF está isolado em `ProjetarServidor.MascararCpf` (`private static`) de um único módulo — qualquer nova projeção que faça `cpf.Formatar()` vaza sem aviso do compilador.

Por que NÃO é falso-positivo: é risco latente de regressão de masking conforme os módulos sensíveis
crescem (M5–M7). O VO oferece a saída crua como caminho fácil e não oferece o mascarado canônico.
Confirmado como BAIXO.

---

## Conclusão

Dos 9 achados do relatório de ataque, **9 são vulnerabilidades reais** e **0 são falsos-positivos**.
Nenhum estava mitigado por filtro, interceptor ou teste existente. O único interceptor de auditoria
(`AuditSaveChangesInterceptor`) cobre apenas escrita e por padrão **não** redige dado sensível
(opt-in usado só pelo Cofre), o que **agrava** C3/M1 em vez de mitigar. Duas ressalvas exigem runtime
para fechar a magnitude (não a existência): A1 (corpo do 500 em produção) e M2 (retenção efetiva de
path/querystring no sink). Recomenda-se priorizar C1, C2 e C3 — todos atacam diretamente a trilha
LGPD/TCE que é o ativo probatório central do produto.
