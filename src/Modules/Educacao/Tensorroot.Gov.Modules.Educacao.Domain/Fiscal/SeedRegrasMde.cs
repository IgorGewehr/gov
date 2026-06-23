namespace Tensorroot.Gov.Modules.Educacao.Domain.Fiscal;

/// <summary>
/// <b>E-1 — seed default das regras de classificação MDE</b> (CF art. 212; LDB Lei 9.394/1996 arts.
/// 70/71), versionado por vigência. NÃO é hardcode de regra de negócio: é o <b>conjunto inicial
/// parametrizável</b> que o tenant pode substituir/estender por vigência. Materializa o caso geral:
/// <list type="bullet">
///   <item><description>inclui a função 12 (Educação) como MDE — art. 70 (regra base, menos específica);</description></item>
///   <item><description>exclui, por subfunção, as despesas que o art. 71 veda: programas suplementares de
///   alimentação (merenda — 306), assistência médico-odontológica/farmacêutica/psicológica (301/302),
///   obras de infraestrutura urbana fora das escolas (451/452) e inativos/administração geral (122).</description></item>
/// </list>
/// As exclusões são mais específicas que a inclusão genérica, então vencem no <see cref="ClassificadorMde"/>.
/// <para>
/// // TODO(validar-oficial): os códigos de subfunção e o recorte exato das exclusões dependem do
/// <b>Manual SIOPE</b> e da MOG 42/1999 vigentes — confirmar antes de tratar como aferição oficial. As
/// subfunções abaixo seguem a Portaria MOG 42/1999 (122 Administração Geral, 301/302 Atenção Básica/
/// Assistência Hospitalar, 306 Alimentação e Nutrição, 451/452 Infraestrutura/Serviços Urbanos) como
/// ponto de partida.
/// </para>
/// </summary>
public static class SeedRegrasMde
{
    /// <summary>
    /// Gera o conjunto default de regras MDE para um tenant, a partir de uma data de vigência.
    /// </summary>
    /// <param name="tenantId">Tenant dono das regras.</param>
    /// <param name="vigenciaInicio">Início de vigência do conjunto (default: 1996-12-20, edição da LDB).</param>
    /// <returns>Regras default (a primeira é a inclusão base; as demais são exclusões finas).</returns>
    public static IReadOnlyList<RegraClassificacaoMde> Gerar(Guid tenantId, DateOnly? vigenciaInicio = null)
    {
        // Edição da LDB (Lei 9.394): 20/12/1996 — referência reprodutível de vigência do seed legal base.
        var vigencia = vigenciaInicio ?? new DateOnly(1996, 12, 20);

        return
        [
            // Regra base: toda a função 12 computa na MDE (LDB art. 70) — menos específica.
            RegraClassificacaoMde.Criar(
                tenantId, EfeitoMde.Inclui, "Função 12 (Educação) — MDE, LDB art. 70", vigencia),

            // Exclusões do art. 71 (mais específicas, por subfunção):
            RegraClassificacaoMde.Criar(
                tenantId, EfeitoMde.Exclui, "Merenda escolar / alimentação e nutrição — não MDE (LDB art. 71, IV)", vigencia, subfuncao: "306"),
            RegraClassificacaoMde.Criar(
                tenantId, EfeitoMde.Exclui, "Assistência médico-odontológica ao aluno — não MDE (LDB art. 71, IV)", vigencia, subfuncao: "301"),
            RegraClassificacaoMde.Criar(
                tenantId, EfeitoMde.Exclui, "Assistência hospitalar ao aluno — não MDE (LDB art. 71, IV)", vigencia, subfuncao: "302"),
            RegraClassificacaoMde.Criar(
                tenantId, EfeitoMde.Exclui, "Obras de infraestrutura urbana fora das escolas — não MDE (LDB art. 71, III)", vigencia, subfuncao: "451"),
            RegraClassificacaoMde.Criar(
                tenantId, EfeitoMde.Exclui, "Serviços urbanos fora das escolas — não MDE (LDB art. 71, III)", vigencia, subfuncao: "452"),
            RegraClassificacaoMde.Criar(
                tenantId, EfeitoMde.Exclui, "Inativos e pensionistas / administração geral — não MDE (LDB art. 71)", vigencia, subfuncao: "122"),
        ];
    }
}
