using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Tributos.Application.Abstractions;
using Tensorroot.Gov.Modules.Tributos.Domain.Contribuintes;
using Tensorroot.Gov.Modules.Tributos.Domain.Imoveis;

namespace Tensorroot.Gov.Modules.Tributos.Application.Imoveis;

/// <summary>Cadastra um imóvel urbano no cadastro imobiliário (BCI), vinculado a um contribuinte.</summary>
/// <param name="ProprietarioId">Contribuinte proprietário/possuidor.</param>
/// <param name="InscricaoMunicipal">Inscrição municipal cadastral (obrigatória).</param>
/// <param name="CibCodigo">Código CIB opcional (formato AAAAAAA-D).</param>
/// <param name="MatriculaRgi">Matrícula do RGI opcional.</param>
/// <param name="Logradouro">Logradouro.</param>
/// <param name="Numero">Número predial opcional.</param>
/// <param name="Bairro">Bairro.</param>
/// <param name="Cep">CEP opcional.</param>
/// <param name="SetorQuadraLote">Setor/quadra/lote.</param>
/// <param name="FaceQuadra">Face de quadra opcional.</param>
/// <param name="ZonaFiscal">Zona fiscal da PGV.</param>
/// <param name="AreaTerreno">Área do terreno (m²).</param>
/// <param name="AreaConstruida">Área construída (m²).</param>
/// <param name="TipoUso">Tipo de uso.</param>
/// <param name="PadraoConstrutivo">Código do padrão construtivo.</param>
/// <param name="AnoConstrucao">Ano de construção opcional.</param>
/// <param name="FracaoIdeal">Fração ideal (0, 1]; padrão 1.</param>
public sealed record CadastrarImovelCommand(
    Guid ProprietarioId,
    string InscricaoMunicipal,
    string? CibCodigo,
    string? MatriculaRgi,
    string Logradouro,
    string? Numero,
    string Bairro,
    string? Cep,
    string SetorQuadraLote,
    string? FaceQuadra,
    string ZonaFiscal,
    decimal AreaTerreno,
    decimal AreaConstruida,
    TipoUsoImovel TipoUso,
    string PadraoConstrutivo,
    int? AnoConstrucao,
    decimal FracaoIdeal = 1m) : ICommand<Guid>;

/// <summary>Regras de validação do cadastro de imóvel.</summary>
public sealed class CadastrarImovelValidator : AbstractValidator<CadastrarImovelCommand>
{
    /// <summary>Define as regras.</summary>
    public CadastrarImovelValidator()
    {
        RuleFor(comando => comando.ProprietarioId).NotEmpty();
        RuleFor(comando => comando.InscricaoMunicipal).NotEmpty().MaximumLength(40);
        RuleFor(comando => comando.Logradouro).NotEmpty().MaximumLength(200);
        RuleFor(comando => comando.Bairro).NotEmpty().MaximumLength(100);
        RuleFor(comando => comando.SetorQuadraLote).NotEmpty().MaximumLength(60);
        RuleFor(comando => comando.ZonaFiscal).NotEmpty().MaximumLength(60);
        RuleFor(comando => comando.PadraoConstrutivo).NotEmpty().MaximumLength(30);
        RuleFor(comando => comando.AreaTerreno).GreaterThanOrEqualTo(0m);
        RuleFor(comando => comando.AreaConstruida).GreaterThanOrEqualTo(0m);
        RuleFor(comando => comando.TipoUso)
            .Must(tipo => Enum.IsDefined(tipo))
            .WithMessage("Tipo de uso inválido.");
        RuleFor(comando => comando.FracaoIdeal).GreaterThan(0m).LessThanOrEqualTo(1m);
    }
}

/// <summary>Handler do cadastro de imóvel.</summary>
public sealed class CadastrarImovelHandler(
    IImovelRepository imoveis,
    IContribuinteRepository contribuintes,
    IUnitOfWork unitOfWork,
    ITenantContext tenant)
    : ICommandHandler<CadastrarImovelCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(CadastrarImovelCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var proprietarioId = new ContribuinteId(request.ProprietarioId);
        _ = await contribuintes.ObterPorIdAsync(proprietarioId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Contribuinte (proprietário) não encontrado.");

        var identificacao = IdentificacaoImovel.Criar(request.InscricaoMunicipal, request.CibCodigo, request.MatriculaRgi);
        var endereco = EnderecoImovel.Criar(
            request.Logradouro,
            request.Bairro,
            request.SetorQuadraLote,
            request.ZonaFiscal,
            request.Numero,
            cep: request.Cep,
            faceQuadra: request.FaceQuadra);
        var caracteristicas = CaracteristicasImovel.Criar(
            request.AreaTerreno,
            request.AreaConstruida,
            request.TipoUso,
            request.PadraoConstrutivo,
            request.AnoConstrucao,
            request.FracaoIdeal);

        var imovel = Imovel.Cadastrar(tenant.TenantId, proprietarioId, identificacao, endereco, caracteristicas);

        imoveis.Adicionar(imovel);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return imovel.Id.Value;
    }
}
