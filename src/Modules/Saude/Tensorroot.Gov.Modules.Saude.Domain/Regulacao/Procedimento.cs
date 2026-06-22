namespace Tensorroot.Gov.Modules.Saude.Domain.Regulacao;

/// <summary>
/// Procedimento da tabela unificada SIGTAP (codigo + descricao). Value Object imutavel.
/// O codigo SIGTAP e validado quanto ao formato aqui; a existencia e validada via ACL no handler.
/// </summary>
/// <param name="CodigoSigtap">Codigo do procedimento na tabela SIGTAP (nao vazio).</param>
/// <param name="Descricao">Descricao do procedimento.</param>
public readonly record struct Procedimento(string CodigoSigtap, string Descricao)
{
    /// <summary>Cria um procedimento SIGTAP validado (codigo nao vazio).</summary>
    /// <param name="codigoSigtap">Codigo SIGTAP.</param>
    /// <param name="descricao">Descricao do procedimento.</param>
    /// <returns>Instancia de <see cref="Procedimento"/> normalizada.</returns>
    /// <exception cref="ArgumentException">Se o codigo SIGTAP ou a descricao forem vazios.</exception>
    public static Procedimento Criar(string codigoSigtap, string descricao)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(codigoSigtap);
        ArgumentException.ThrowIfNullOrWhiteSpace(descricao);
        return new Procedimento(codigoSigtap.Trim(), descricao.Trim());
    }

    /// <inheritdoc />
    public override string ToString() => $"{CodigoSigtap} - {Descricao}";
}
