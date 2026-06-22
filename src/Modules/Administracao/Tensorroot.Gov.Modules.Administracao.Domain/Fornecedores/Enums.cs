namespace Tensorroot.Gov.Modules.Administracao.Domain.Fornecedores;

/// <summary>
/// Nivel de cadastramento/regularidade do fornecedor no SICAF (Lei 14.133/2021, art. 87).
/// </summary>
public enum NivelCadastralSICAF
{
    /// <summary>Sem cadastro no SICAF.</summary>
    NaoCadastrado = 0,

    /// <summary>Credenciamento (nivel I).</summary>
    CredenciamentoNivel1 = 1,

    /// <summary>Habilitacao juridica (nivel II).</summary>
    HabilitacaoJuridicaNivel2 = 2,

    /// <summary>Regularidade fiscal e trabalhista (nivel III).</summary>
    RegularidadeFiscalNivel3 = 3,

    /// <summary>Qualificacao economico-financeira (nivel IV).</summary>
    QualificacaoEconomicaNivel4 = 4,

    /// <summary>Qualificacao tecnica (nivel V).</summary>
    QualificacaoTecnicaNivel5 = 5,
}

/// <summary>
/// Tipo de sancao administrativa aplicavel ao fornecedor (Lei 14.133/2021, art. 156).
/// </summary>
public enum TipoSancao
{
    /// <summary>Advertencia (art. 156, I). Nao impeditiva.</summary>
    Advertencia = 1,

    /// <summary>Multa (art. 156, II). Nao impeditiva.</summary>
    Multa = 2,

    /// <summary>Impedimento de licitar e contratar no ambito do ente/esfera (art. 156, III). Impeditiva.</summary>
    Impedimento = 3,

    /// <summary>Declaracao de inidoneidade para toda a Administracao Publica (art. 156, IV). Impeditiva.</summary>
    Inidoneidade = 4,
}

/// <summary>Situacao (estado cadastral) do fornecedor.</summary>
public enum SituacaoFornecedor
{
    /// <summary>Apto a participar/contratar (estado inicial).</summary>
    Ativo = 1,

    /// <summary>Com sancao impeditiva vigente (impedimento/inidoneidade).</summary>
    Sancionado = 2,

    /// <summary>Cadastro inativo (terminal administrativo).</summary>
    Inativo = 3,
}
