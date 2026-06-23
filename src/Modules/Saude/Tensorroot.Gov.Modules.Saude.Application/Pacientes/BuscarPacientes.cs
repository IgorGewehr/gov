using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Saude.Application.Abstractions;
using Tensorroot.Gov.Modules.Saude.Application.Common;
using Tensorroot.Gov.Modules.Saude.Domain.Pacientes;
using Tensorroot.Gov.SharedKernel;

namespace Tensorroot.Gov.Modules.Saude.Application.Pacientes;

/// <summary>
/// Item da lista de pacientes (navegabilidade — Onda 0): projecao MINIMIZADA (LGPD) para a tabela.
/// O CNS e devolvido pois e a chave de negocio operacional (abrir prontuario); CPF NAO e devolvido.
/// </summary>
/// <param name="Id">Identificador do paciente.</param>
/// <param name="Cns">Cartao Nacional de Saude.</param>
/// <param name="Nome">Nome civil.</param>
/// <param name="NomeSocial">Nome social, quando informado.</param>
/// <param name="DataNascimento">Data de nascimento.</param>
/// <param name="Sexo">Sexo (descricao).</param>
/// <param name="CnsConfirmado">Indica se o CNS foi confirmado no CADSUS.</param>
/// <param name="Situacao">Situacao atual do cadastro.</param>
public sealed record PacienteItemLista(
    Guid Id,
    string Cns,
    string Nome,
    string? NomeSocial,
    DateOnly DataNascimento,
    string Sexo,
    bool CnsConfirmado,
    string Situacao);

/// <summary>
/// Lista/busca paginada de pacientes por nome/CNS/CPF (navegabilidade — Onda 0). Tenant-scoped via
/// Global Query Filter; read-only. Dado pessoal SENSIVEL (LGPD art. 11): exige o verbo fino de leitura
/// de prontuario e, por implementar <see cref="ISensivelLgpd"/>, GERA TRILHA DE ACESSO (LG-2). Como e
/// uma busca (nao um registro especifico), <see cref="EntidadeId"/> e nulo. Base legal: tutela da saude.
/// </summary>
/// <param name="Termo">Termo livre (nome, CNS ou CPF); nulo lista tudo.</param>
/// <param name="Situacao">Filtro opcional por situacao do cadastro.</param>
/// <param name="Pagina">Pagina (base 1).</param>
/// <param name="Tamanho">Tamanho da pagina.</param>
public sealed record BuscarPacientesQuery(
    string? Termo,
    SituacaoPaciente? Situacao,
    int? Pagina,
    int? Tamanho) : IQuery<ResultadoPaginado<PacienteItemLista>>, ISensivelLgpd
{
    /// <inheritdoc />
    public string EntidadeSensivel => nameof(Paciente);

    /// <inheritdoc />
    public string? EntidadeId => null;

    /// <inheritdoc />
    public BaseLegalLgpd BaseLegal => BaseLegalLgpd.TutelaDaSaude;

    /// <inheritdoc />
    public IReadOnlySet<BaseLegalLgpd> BasesLegaisAplicaveis => BasesLegaisSaude.Aplicaveis;
}

/// <summary>Handler da busca paginada de pacientes.</summary>
public sealed class BuscarPacientesHandler(IPacienteRepository pacientes)
    : IQueryHandler<BuscarPacientesQuery, ResultadoPaginado<PacienteItemLista>>
{
    /// <inheritdoc />
    public async Task<ResultadoPaginado<PacienteItemLista>> Handle(
        BuscarPacientesQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (pagina, tamanho) = Paginacao.Sanear(request.Pagina, request.Tamanho);

        var (itens, total) = await pacientes
            .BuscarAsync(request.Termo, request.Situacao, pagina, tamanho, cancellationToken)
            .ConfigureAwait(false);

        var projetados = itens
            .Select(paciente => new PacienteItemLista(
                paciente.Id.Value,
                paciente.Cns.Valor,
                paciente.Identificacao.Nome,
                paciente.Identificacao.NomeSocial,
                paciente.Identificacao.DataNascimento,
                paciente.Identificacao.Sexo.ToString(),
                paciente.CnsConfirmado,
                paciente.Situacao.ToString()))
            .ToList();

        return new ResultadoPaginado<PacienteItemLista>(projetados, total, pagina, tamanho);
    }
}
