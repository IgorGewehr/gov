// Harness Rules-as-Code (v1) — gera/atualiza um módulo a partir dos *.rules.md.
// Uso (a partir do Sprint 3):
//   Workflow({ scriptPath: "<este arquivo>", args: { modulo, contexto, entidades:[...], referencias:[...] } })
// args.entidades = [{ entidade, rulesPath, dominioDir, aplicacaoDir, infraDir }]
// args.referencias = caminhos de arquivos do Tributos/Finanças usados como exemplo de estilo.
export const meta = {
  name: 'gerar-modulo-de-regras',
  description: 'Gera um módulo a partir dos *.rules.md: Gerador → Testador → Verificador → Corretor',
  phases: [
    { title: 'Gerar', detail: 'um agente por entidade escreve Domain/Application/Infrastructure' },
    { title: 'Testar', detail: 'um agente por entidade escreve testes (invariantes/estados/BDD)' },
    { title: 'Verificar', detail: 'build + testes + fitness + spec-code consistency' },
    { title: 'Corrigir', detail: 'corrige o CÓDIGO (nunca as regras) até ficar verde' },
  ],
}

const CONST = '/Users/igorgewehr/Development/Tensorroot.Gov/CLAUDE.md'
const a = args || {}
const entidades = a.entidades || []
const referencias = (a.referencias || []).join('\n')

if (entidades.length === 0) {
  log('args.entidades vazio — nada a gerar.')
  return { erro: 'sem entidades' }
}

// Gerar e Testar pipelinados por entidade (sem barreira entre as fases).
const produzidos = await pipeline(
  entidades,
  (e) => agent(
    `Você é o "Gerador" (Rules-as-Code) do Tensorroot.Gov.
Leia a constituição ${CONST} e o arquivo de regras ${e.rulesPath} (a ÚNICA fonte da verdade).
Use como referência de estilo/convenções:
${referencias}
Gere o código do agregado **${e.entidade}** (módulo ${a.modulo}) EXATAMENTE conforme as regras:
- Domain (agregado, VOs, enums, eventos de domínio) em ${e.dominioDir}
- Application (Commands/Queries/Handlers/Validators/ports) em ${e.aplicacaoDir}
- Infrastructure (DbContext quando 1º agregado, EF config com conversores, repositórios) em ${e.infraDir}
Regras de código: net8/C#12, NRT, sealed, construtor privado + factory, IMustHaveTenant, XML docs em tipos públicos, ConfigureAwait(false), Span<T> em parsing pesado, SEM acoplar a outros módulos (só *.Contracts). Os manifestos do rules.md devem refletir EXATAMENTE o que você gerar.
Escreva os arquivos com Write. Resposta final: "GERADO ${e.entidade}".`,
    { label: `gerar:${e.entidade}`, phase: 'Gerar' },
  ),
  (_prev, e) => agent(
    `Você é o "Testador". Leia ${e.rulesPath} e o código gerado do agregado ${e.entidade} (módulo ${a.modulo}).
Escreva um teste de integração (SQLite em memória + TenantSaveChangesInterceptor + AuditSaveChangesInterceptor, no padrão de tests/Tensorroot.Gov.Modules.Tributos.Tests) cobrindo: CADA invariante, CADA transição da máquina de estados e CADA cenário BDD do rules.md, além do isolamento por tenant.
Escreva os arquivos de teste com Write. Resposta final: "TESTADO ${e.entidade}".`,
    { label: `testar:${e.entidade}`, phase: 'Testar' },
  ),
)

// Barreira: verificação completa.
phase('Verificar')
const relatorio = await agent(
  `Você é o "Verificador/Checker". Execute (Bash, com DOTNET_ROLL_FORWARD=Major):
- dotnet build da solução Tensorroot.Gov.sln
- dotnet test do módulo ${a.modulo} e de tests/Tensorroot.Gov.ArchitectureTests (fitness functions + spec-code consistency)
Relate, de forma objetiva: erros de compilação (arquivo:linha), testes falhos e violações de arquitetura/consistência. Responda "OK" se tudo verde, ou a lista de problemas.`,
  { label: 'verificar', phase: 'Verificar', effort: 'high' },
)

// Correção iterativa (mexe só no CÓDIGO).
phase('Corrigir')
const correcao = await agent(
  `Você é o "Corretor". Relatório do Verificador:
${relatorio}
Corrija o CÓDIGO (NUNCA os *.rules.md) até build + testes + fitness functions + spec-code consistency ficarem 100% verdes. Itere com Bash/Edit/Write. Se algo no código for impossível sem mudar uma regra, NÃO mude a regra: relate a divergência. Resposta final: "VERDE" ou a lista do que falta.`,
  { label: 'corrigir', phase: 'Corrigir', effort: 'high' },
)

return { entidades: entidades.length, produzidos: produzidos.filter(Boolean).length, relatorio, correcao }
