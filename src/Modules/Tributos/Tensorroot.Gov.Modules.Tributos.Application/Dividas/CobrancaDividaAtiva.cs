using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.Tributos.Application.Abstractions;
using Tensorroot.Gov.Modules.Tributos.Domain.Dividas;

namespace Tensorroot.Gov.Modules.Tributos.Application.Dividas;

// ===================================================================================================
// COBRANÇA — PROTESTO EXTRAJUDICIAL (Lei 9.492/97; STF ADI 5.135; STJ REsp 1.895.557).
// O ente gera a remessa ao CRA-RS via ACL (IProtestoCraGateway) e processa o retorno. A integração
// real é convênio à parte. Ver M6-DESIGN §4 e pesquisa-divida-protesto §4. // TODO(validar-oficial).
// ===================================================================================================

/// <summary>Resultado da geração de remessa de protesto.</summary>
/// <param name="RemessaProtestoId">Remessa gerada.</param>
/// <param name="IdentificadorCra">CRA estadual de destino.</param>
/// <param name="ConteudoRemessa">Conteúdo do arquivo de remessa (leiaute do CRA).</param>
public sealed record ResultadoRemessaProtesto(Guid RemessaProtestoId, string IdentificadorCra, string ConteudoRemessa);

/// <summary>Gera a remessa de protesto extrajudicial de uma CDA ao CRA estadual.</summary>
/// <param name="DividaAtivaId">Dívida ativa (com CDA emitida).</param>
/// <param name="DataGeracao">Data de geração da remessa (data do fato).</param>
public sealed record GerarRemessaProtestoCommand(Guid DividaAtivaId, DateOnly DataGeracao) : ICommand<ResultadoRemessaProtesto>;

/// <summary>Validação da geração de remessa de protesto.</summary>
public sealed class GerarRemessaProtestoValidator : AbstractValidator<GerarRemessaProtestoCommand>
{
    /// <summary>Define as regras.</summary>
    public GerarRemessaProtestoValidator() => RuleFor(c => c.DividaAtivaId).NotEmpty();
}

/// <summary>Handler da geração de remessa de protesto.</summary>
public sealed class GerarRemessaProtestoHandler(
    IDividaAtivaRepository dividas,
    IContribuinteRepository contribuintes,
    IProtestoCraGateway craGateway,
    IUnitOfWork unitOfWork)
    : ICommandHandler<GerarRemessaProtestoCommand, ResultadoRemessaProtesto>
{
    /// <inheritdoc />
    public async Task<ResultadoRemessaProtesto> Handle(GerarRemessaProtestoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var divida = await dividas.ObterPorIdAsync(new DividaAtivaId(request.DividaAtivaId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Dívida ativa não encontrada.");

        var contribuinte = await contribuintes.ObterPorIdAsync(divida.ContribuinteId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Contribuinte (devedor) da dívida não encontrado.");

        // Domínio: exige CDA emitida e dívida exigível; registra o ato (remessa) e muda o estado.
        var remessa = divida.GerarRemessaProtesto(craGateway.IdentificadorCra, request.DataGeracao);

        // ACL: o adapter (versionado por CRA) gera o arquivo no leiaute oficial. Determinístico: valor
        // atualizado apurado na data de geração (datas do fato).
        var encargos = divida.ApurarEncargos(request.DataGeracao);
        var titulo = new TituloProtesto(
            divida.NumeroCda!,
            contribuinte.Nome,
            contribuinte.Documento,
            encargos.ValorAtualizado.Valor,
            divida.DataInscricao);

        var arquivo = await craGateway.GerarRemessaAsync(titulo, cancellationToken).ConfigureAwait(false);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new ResultadoRemessaProtesto(remessa.Id.Value, arquivo.IdentificadorCra, arquivo.ConteudoRemessa);
    }
}

/// <summary>Processa o retorno do CRA/cartório de uma remessa de protesto (ocorrência + protocolo).</summary>
/// <param name="DividaAtivaId">Dívida ativa.</param>
/// <param name="RemessaProtestoId">Remessa cujo retorno chegou.</param>
/// <param name="Ocorrencia">Ocorrência de retorno.</param>
/// <param name="DataRetorno">Data do retorno.</param>
/// <param name="ProtocoloCartorio">Protocolo do cartório (opcional).</param>
public sealed record ProcessarRetornoProtestoCommand(
    Guid DividaAtivaId,
    Guid RemessaProtestoId,
    OcorrenciaProtesto Ocorrencia,
    DateOnly DataRetorno,
    string? ProtocoloCartorio = null) : ICommand;

/// <summary>Validação do processamento de retorno de protesto.</summary>
public sealed class ProcessarRetornoProtestoValidator : AbstractValidator<ProcessarRetornoProtestoCommand>
{
    /// <summary>Define as regras.</summary>
    public ProcessarRetornoProtestoValidator()
    {
        RuleFor(c => c.DividaAtivaId).NotEmpty();
        RuleFor(c => c.RemessaProtestoId).NotEmpty();
        RuleFor(c => c.Ocorrencia).IsInEnum().NotEqual(OcorrenciaProtesto.Pendente);
    }
}

/// <summary>Handler do processamento de retorno de protesto.</summary>
public sealed class ProcessarRetornoProtestoHandler(IDividaAtivaRepository dividas, IUnitOfWork unitOfWork)
    : ICommandHandler<ProcessarRetornoProtestoCommand>
{
    /// <inheritdoc />
    public async Task Handle(ProcessarRetornoProtestoCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var divida = await dividas.ObterPorIdAsync(new DividaAtivaId(request.DividaAtivaId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Dívida ativa não encontrada.");

        divida.ProcessarRetornoProtesto(
            new RemessaProtestoId(request.RemessaProtestoId),
            request.Ocorrencia,
            request.DataRetorno,
            request.ProtocoloCartorio);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}

// ===================================================================================================
// EXECUÇÃO FISCAL — gancho de saída (Lei 6.830/80). O ERP gera/exporta CDA + petição; o ajuizamento
// ocorre no PJe/eproc-RS (integração de saída = decisão de produto). Ver M6-DESIGN §4.
// ===================================================================================================

/// <summary>Ajuíza (gancho) a execução fiscal de uma CDA.</summary>
/// <param name="DividaAtivaId">Dívida ativa (com CDA emitida/protestada).</param>
/// <param name="DataAjuizamento">Data do ajuizamento (despacho de citação interrompe a prescrição).</param>
public sealed record AjuizarExecucaoFiscalCommand(Guid DividaAtivaId, DateOnly DataAjuizamento) : ICommand;

/// <summary>Validação do ajuizamento de execução fiscal.</summary>
public sealed class AjuizarExecucaoFiscalValidator : AbstractValidator<AjuizarExecucaoFiscalCommand>
{
    /// <summary>Define as regras.</summary>
    public AjuizarExecucaoFiscalValidator() => RuleFor(c => c.DividaAtivaId).NotEmpty();
}

/// <summary>Handler do ajuizamento de execução fiscal.</summary>
public sealed class AjuizarExecucaoFiscalHandler(IDividaAtivaRepository dividas, IUnitOfWork unitOfWork)
    : ICommandHandler<AjuizarExecucaoFiscalCommand>
{
    /// <inheritdoc />
    public async Task Handle(AjuizarExecucaoFiscalCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var divida = await dividas.ObterPorIdAsync(new DividaAtivaId(request.DividaAtivaId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Dívida ativa não encontrada.");

        // ORDEM IMPORTA (fail-closed): afere a prescrição na data do ajuizamento ANTES de interromper.
        // O despacho de citação só interrompe a prescrição (CTN art. 174 p.ú. I; LC 118/2005, retroage
        // ao ajuizamento) se a ação for tempestiva; dívida já prescrita no ajuizamento é barrada
        // (DividaAtivaPrescritaException). Se aprovado, marca a interrupção na data do ajuizamento.
        divida.AjuizarExecucaoFiscal(request.DataAjuizamento);
        divida.InterromperPrescricao(request.DataAjuizamento);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
