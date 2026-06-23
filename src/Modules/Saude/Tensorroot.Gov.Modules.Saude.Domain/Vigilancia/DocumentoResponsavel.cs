using Tensorroot.Gov.SharedKernel.ValueObjects;

namespace Tensorroot.Gov.Modules.Saude.Domain.Vigilancia;

/// <summary>
/// Documento do responsavel pelo estabelecimento sujeito a VISA: um CNPJ (pessoa juridica) OU um CPF
/// (pessoa fisica / MEI sem CNPJ / autonomo). Objeto de Valor imutavel, armazenado SEM mascara como a
/// concatenacao "tipo:digitos" para persistir num unico campo, validado pelos VOs do SharedKernel
/// (digitos verificadores conferidos). A VISA fiscaliza tanto PJ quanto PF (Lei 6.437/1977).
/// </summary>
public readonly record struct DocumentoResponsavel
{
    private DocumentoResponsavel(bool ehPessoaJuridica, string digitos)
    {
        EhPessoaJuridica = ehPessoaJuridica;
        Digitos = digitos;
    }

    /// <summary>Indica se o documento e um CNPJ (pessoa juridica); caso contrario e CPF (pessoa fisica).</summary>
    public bool EhPessoaJuridica { get; }

    /// <summary>Os digitos do documento (14 = CNPJ; 11 = CPF), sem mascara.</summary>
    public string Digitos { get; }

    /// <summary>Cria um documento a partir de um CNPJ valido.</summary>
    /// <param name="valor">CNPJ com ou sem mascara.</param>
    /// <returns>Documento do tipo pessoa juridica.</returns>
    /// <exception cref="ArgumentException">Se o CNPJ for invalido.</exception>
    public static DocumentoResponsavel DeCnpj(string valor)
    {
        var cnpj = Cnpj.Create(valor);
        return new DocumentoResponsavel(ehPessoaJuridica: true, cnpj.Digitos);
    }

    /// <summary>Cria um documento a partir de um CPF valido.</summary>
    /// <param name="valor">CPF com ou sem mascara.</param>
    /// <returns>Documento do tipo pessoa fisica.</returns>
    /// <exception cref="ArgumentException">Se o CPF for invalido.</exception>
    public static DocumentoResponsavel DeCpf(string valor)
    {
        var cpf = Cpf.Create(valor);
        return new DocumentoResponsavel(ehPessoaJuridica: false, cpf.Digitos);
    }

    /// <summary>
    /// Cria um documento inferindo o tipo pelo comprimento dos digitos (14 = CNPJ; 11 = CPF). Conveniencia
    /// para a borda HTTP, onde o operador informa um unico campo de documento.
    /// </summary>
    /// <param name="valor">Documento com ou sem mascara.</param>
    /// <returns>Documento valido (PJ ou PF).</returns>
    /// <exception cref="ArgumentException">Se nao for um CNPJ nem um CPF valido.</exception>
    public static DocumentoResponsavel Criar(string valor)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(valor);
        var digitos = new string([.. valor.Where(char.IsAsciiDigit)]);
        return digitos.Length switch
        {
            14 => DeCnpj(valor),
            11 => DeCpf(valor),
            _ => throw new ArgumentException($"Documento invalido (esperado CNPJ ou CPF): '{valor}'.", nameof(valor)),
        };
    }

    /// <summary>Forma persistida ("J:digitos" para CNPJ, "F:digitos" para CPF) — campo unico no banco.</summary>
    /// <returns>Representacao para persistencia.</returns>
    public string ParaPersistencia() => (EhPessoaJuridica ? "J:" : "F:") + Digitos;

    /// <summary>Reconstroi o documento a partir da forma persistida (materializacao EF).</summary>
    /// <param name="persistido">Valor no formato "J:digitos" ou "F:digitos".</param>
    /// <returns>Documento reconstruido.</returns>
    /// <exception cref="ArgumentException">Se o formato persistido for invalido.</exception>
    public static DocumentoResponsavel DePersistencia(string persistido)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(persistido);
        if (persistido.Length < 3 || persistido[1] != ':')
        {
            throw new ArgumentException($"Documento persistido invalido: '{persistido}'.", nameof(persistido));
        }

        var digitos = persistido[2..];
        return persistido[0] switch
        {
            'J' => new DocumentoResponsavel(ehPessoaJuridica: true, digitos),
            'F' => new DocumentoResponsavel(ehPessoaJuridica: false, digitos),
            _ => throw new ArgumentException($"Documento persistido invalido: '{persistido}'.", nameof(persistido)),
        };
    }

    /// <inheritdoc />
    public override string ToString() => Digitos;
}
