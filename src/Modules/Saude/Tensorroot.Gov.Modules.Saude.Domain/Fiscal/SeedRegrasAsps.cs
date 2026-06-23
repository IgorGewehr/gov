namespace Tensorroot.Gov.Modules.Saude.Domain.Fiscal;

/// <summary>
/// <b>S-1 — seed default das regras de classificação ASPS</b> (LC 141/2012), versionado por vigência.
/// NÃO é hardcode de regra de negócio: é o <b>conjunto inicial parametrizável</b> que o tenant pode
/// substituir/estender por vigência. Materializa o caso geral:
/// <list type="bullet">
///   <item><description>inclui a função 10 (Saúde) como ASPS — art. 3º (regra base, menos específica);</description></item>
///   <item><description>exclui, por subfunção, as despesas que o art. 4º veda: saneamento básico geral,
///   limpeza urbana, merenda; e, por convenção de fonte/subfunção, inativos e assistência ao servidor.</description></item>
/// </list>
/// As exclusões são mais específicas que a inclusão genérica, então vencem no <see cref="ClassificadorAsps"/>.
/// <para>
/// // TODO(validar-oficial): os códigos de subfunção e o recorte exato das exclusões dependem do
/// <b>Manual SIOPS</b> e da MOG 42/1999 vigentes — confirmar antes de tratar como aferição oficial.
/// As subfunções abaixo seguem a Portaria MOG 42/1999 (122 Administração Geral, 306 Alimentação e
/// Nutrição, 511/512 Saneamento Básico Rural/Urbano, 452 Serviços Urbanos) como ponto de partida.
/// </para>
/// </summary>
public static class SeedRegrasAsps
{
    /// <summary>
    /// Gera o conjunto default de regras ASPS para um tenant, a partir de uma data de vigência.
    /// </summary>
    /// <param name="tenantId">Tenant dono das regras.</param>
    /// <param name="vigenciaInicio">Início de vigência do conjunto (default: 2012-01-16, edição da LC 141).</param>
    /// <returns>Regras default (a primeira é a inclusão base; as demais são exclusões finas).</returns>
    public static IReadOnlyList<RegraClassificacaoAsps> Gerar(Guid tenantId, DateOnly? vigenciaInicio = null)
    {
        // Edição da LC 141/2012: 16/01/2012 — referência reprodutível de vigência do seed legal base.
        var vigencia = vigenciaInicio ?? new DateOnly(2012, 1, 16);

        return
        [
            // Regra base: toda a função 10 computa nas ASPS (art. 3º) — menos específica.
            RegraClassificacaoAsps.Criar(
                tenantId, EfeitoAsps.Inclui, "Função 10 (Saúde) — ASPS, LC 141/2012 art. 3º", vigencia),

            // Exclusões do art. 4º (mais específicas, por subfunção):
            RegraClassificacaoAsps.Criar(
                tenantId, EfeitoAsps.Exclui, "Saneamento básico rural — não ASPS (LC 141 art. 4º)", vigencia, subfuncao: "511"),
            RegraClassificacaoAsps.Criar(
                tenantId, EfeitoAsps.Exclui, "Saneamento básico urbano — não ASPS (LC 141 art. 4º)", vigencia, subfuncao: "512"),
            RegraClassificacaoAsps.Criar(
                tenantId, EfeitoAsps.Exclui, "Limpeza urbana / serviços urbanos — não ASPS (LC 141 art. 4º)", vigencia, subfuncao: "452"),
            RegraClassificacaoAsps.Criar(
                tenantId, EfeitoAsps.Exclui, "Merenda escolar / alimentação e nutrição — não ASPS (LC 141 art. 4º)", vigencia, subfuncao: "306"),
            RegraClassificacaoAsps.Criar(
                tenantId, EfeitoAsps.Exclui, "Inativos e pensionistas / administração geral — não ASPS (LC 141 art. 4º)", vigencia, subfuncao: "122"),
        ];
    }
}
