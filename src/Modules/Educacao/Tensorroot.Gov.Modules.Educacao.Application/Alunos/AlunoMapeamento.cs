using Tensorroot.Gov.Modules.Educacao.Domain.Alunos;
using Tensorroot.Gov.SharedKernel.ValueObjects;

namespace Tensorroot.Gov.Modules.Educacao.Application.Alunos;

/// <summary>
/// Mapeamentos entre os payloads de entrada/DTOs de saida e o agregado <see cref="Aluno"/>.
/// Concentra a criacao dos objetos de valor (validados no dominio) e a MASCARA de CPF na projecao
/// (LGPD): o CPF nunca e devolvido em claro nas fichas/listas.
/// </summary>
internal static class AlunoMapeamento
{
    /// <summary>Cria o CPF de dominio a partir do texto, ou nulo quando vazio.</summary>
    /// <param name="cpf">CPF com ou sem mascara (opcional).</param>
    /// <returns>CPF valido, ou <c>null</c> se vazio.</returns>
    /// <exception cref="ArgumentException">Se o CPF informado for invalido.</exception>
    public static Cpf? CriarCpfOuNulo(string? cpf)
        => string.IsNullOrWhiteSpace(cpf) ? null : Cpf.Create(cpf);

    /// <summary>Cria os dados civis a partir do payload.</summary>
    /// <param name="payload">Payload de entrada.</param>
    /// <param name="hoje">Data corrente (guarda de nascimento nao futuro).</param>
    /// <returns>Objeto de valor <see cref="DadosCivis"/>.</returns>
    public static DadosCivis CriarDadosCivis(DadosCivisPayload payload, DateOnly hoje)
    {
        ArgumentNullException.ThrowIfNull(payload);
        return new DadosCivis(
            payload.Nome,
            payload.DataNascimento,
            payload.Sexo,
            payload.NomeMae,
            payload.NomePai,
            payload.NomeSocial,
            hoje);
    }

    /// <summary>Cria o endereco residencial a partir do payload.</summary>
    /// <param name="payload">Payload de entrada.</param>
    /// <returns>Objeto de valor <see cref="EnderecoAluno"/>.</returns>
    public static EnderecoAluno CriarEndereco(EnderecoAlunoPayload payload)
    {
        ArgumentNullException.ThrowIfNull(payload);
        return new EnderecoAluno(
            payload.Logradouro,
            payload.Numero,
            payload.Bairro,
            payload.Municipio,
            payload.Uf,
            payload.Cep);
    }

    /// <summary>Cria um responsavel a partir do payload.</summary>
    /// <param name="payload">Payload de entrada.</param>
    /// <returns>Entidade-filha <see cref="Responsavel"/>.</returns>
    public static Responsavel CriarResponsavel(ResponsavelPayload payload)
    {
        ArgumentNullException.ThrowIfNull(payload);
        return Responsavel.Criar(
            payload.Nome,
            CriarCpfOuNulo(payload.Cpf),
            payload.Parentesco,
            payload.Telefone,
            payload.ResponsavelFinanceiro,
            payload.AutorizadoBuscar);
    }

    /// <summary>Projeta o aluno no item de lista (sem CPF — minimizacao LGPD).</summary>
    /// <param name="aluno">Aluno de origem.</param>
    /// <returns>Item de lista.</returns>
    public static AlunoItemLista ParaItemLista(Aluno aluno)
    {
        ArgumentNullException.ThrowIfNull(aluno);
        return new AlunoItemLista(
            aluno.Id.Value,
            aluno.DadosCivis.Nome,
            aluno.DadosCivis.NomeSocial,
            aluno.DadosCivis.DataNascimento,
            aluno.DadosCivis.Sexo.ToString(),
            aluno.CodigoInepAluno,
            aluno.Situacao.ToString());
    }

    /// <summary>Projeta a ficha completa do aluno (CPF mascarado — LGPD).</summary>
    /// <param name="aluno">Aluno de origem.</param>
    /// <returns>Ficha completa.</returns>
    public static AlunoFicha ParaFicha(Aluno aluno)
    {
        ArgumentNullException.ThrowIfNull(aluno);
        var dados = aluno.DadosCivis;
        var endereco = aluno.Endereco;
        return new AlunoFicha(
            aluno.Id.Value,
            dados.Nome,
            dados.NomeSocial,
            dados.DataNascimento,
            dados.Sexo.ToString(),
            dados.NomeMae,
            dados.NomePai,
            Mascarar(aluno.Cpf),
            aluno.CodigoInepAluno,
            aluno.Situacao.ToString(),
            endereco.Logradouro,
            endereco.Numero,
            endereco.Bairro,
            endereco.Municipio,
            endereco.Uf,
            endereco.Cep,
            aluno.Responsaveis.Select(ParaResponsavelDto).ToList());
    }

    private static ResponsavelDto ParaResponsavelDto(Responsavel responsavel)
        => new(
            responsavel.Id.Value,
            responsavel.Nome,
            Mascarar(responsavel.Cpf),
            responsavel.Parentesco.ToString(),
            responsavel.Telefone,
            responsavel.ResponsavelFinanceiro,
            responsavel.AutorizadoBuscar);

    // Mascara LGPD: revela apenas os 3 digitos centrais (***.NNN.***-**), suficiente para conferencia
    // visual sem expor o documento completo na ficha/lista.
    private static string? Mascarar(Cpf? cpf)
    {
        if (cpf is null)
        {
            return null;
        }

        var digitos = cpf.Digitos;
        return $"***.{digitos[3..6]}.***-**";
    }
}
