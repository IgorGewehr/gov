using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Tensorroot.Gov.ApiHost.ErrorHandling;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Identidade.Application.Autenticacao;
using Tensorroot.Gov.Modules.Identidade.Application.Usuarios;
using Tensorroot.Gov.Modules.Identidade.Domain.Usuarios;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.SharedKernel;
using Xunit;

namespace Tensorroot.Gov.ArchitectureTests;

/// <summary>
/// Testes do MAPEADOR PURO exceção→status (RFC 7807) do tratamento global de erros do ApiHost.
/// Exercitam a taxonomia REAL de exceções do código (não mocks de nome), provando que o deny-by-
/// default sobe como 403 (não 500), validação como 400 com erros de campo, regra de negócio como 422
/// e que o fallback é 500 GENÉRICO sem detalhe sensível.
/// </summary>
public sealed class TratamentoGlobalErrosTests
{
    [Fact] // O bug original: UsuarioSemVinculoServidorException (deny) subia como 500 → agora 403.
    public void DenySemVinculo_deveMapearPara_403()
    {
        var resultado = MapaExcecaoStatus.Mapear(new UsuarioSemVinculoServidorException());

        resultado.StatusCode.Should().Be(403);
        resultado.Titulo.Should().Be("Acesso negado.");
        resultado.ErrosPorCampo.Should().BeNull();
    }

    [Fact]
    public void ConcessaoNaoAutorizada_deveMapearPara_403()
    {
        var negado = ResultadoConcessao.Negar(MotivoConcessaoNegada.SemPoderAdministrativo);

        var resultado = MapaExcecaoStatus.Mapear(new ConcessaoNaoAutorizadaException(negado));

        resultado.StatusCode.Should().Be(403);
    }

    [Fact]
    public void BaseLegalLgpdNaoAplicavel_deveMapearPara_403()
    {
        var resultado = MapaExcecaoStatus.Mapear(
            new BaseLegalLgpdNaoAplicavelException("Prontuario", BaseLegalLgpd.ObrigacaoLegal));

        resultado.StatusCode.Should().Be(403);
    }

    [Fact]
    public void AutenticacaoFalhou_deveMapearPara_403()
    {
        var resultado = MapaExcecaoStatus.Mapear(new AutenticacaoFalhouException());

        resultado.StatusCode.Should().Be(403);
    }

    [Fact] // Cross-tenant é NEGAÇÃO de segurança (não regra de negócio) mesmo sendo InvalidOperationException.
    public void GravacaoCrossTenant_deveMapearPara_403()
    {
        var resultado = MapaExcecaoStatus.Mapear(
            new InvalidOperationException("Gravação cross-tenant bloqueada na entidade 'Empenho'."));

        resultado.StatusCode.Should().Be(403);
    }

    [Fact]
    public void ValidationException_deveMapearPara_400_comErrosPorCampo()
    {
        var falhas = new[]
        {
            new ValidationFailure("Email", "Email é obrigatório."),
            new ValidationFailure("Senha", "Senha é obrigatória."),
        };

        var resultado = MapaExcecaoStatus.Mapear(new ValidationException(falhas));

        resultado.StatusCode.Should().Be(400);
        resultado.ErrosPorCampo.Should().NotBeNull();
        resultado.ErrosPorCampo!.Should().ContainKey("Email");
        resultado.ErrosPorCampo!.Should().ContainKey("Senha");
    }

    [Fact] // InvalidOperationException de domínio (regra/invariante) → 422, não 500.
    public void InvalidOperationDeDominio_deveMapearPara_422()
    {
        var resultado = MapaExcecaoStatus.Mapear(
            new InvalidOperationException("Saldo orçamentário insuficiente para o empenho."));

        resultado.StatusCode.Should().Be(422);
        resultado.Titulo.Should().Be("Regra de negócio violada.");
    }

    [Fact] // Nome com "Inexistente" → 404.
    public void NomeInexistente_deveMapearPara_404()
    {
        var resultado = MapaExcecaoStatus.Mapear(new RecursoInexistenteFake());

        resultado.StatusCode.Should().Be(404);
    }

    [Fact] // Fallback: exceção genérica NÃO mapeada → 500 GENÉRICO, sem vazar a mensagem original.
    public void ExcecaoNaoMapeada_deveMapearPara_500_semDetalheSensivel()
    {
        var segredo = "stack trace / connection string / dado sensível";

        var resultado = MapaExcecaoStatus.Mapear(new NotSupportedException(segredo));

        resultado.StatusCode.Should().Be(500);
        resultado.Titulo.Should().Be("Erro interno.");
        resultado.Detalhe.Should().NotContain(segredo);
        resultado.ErrosPorCampo.Should().BeNull();
    }

    // Exceção-fantoche só para exercitar a regra de nome "*Inexistente*" sem depender de um módulo.
#pragma warning disable CA1064, CA1032, S3871 // exceção de teste, propositalmente mínima.
    private sealed class RecursoInexistenteFake : Exception
    {
    }
#pragma warning restore CA1064, CA1032, S3871
}
