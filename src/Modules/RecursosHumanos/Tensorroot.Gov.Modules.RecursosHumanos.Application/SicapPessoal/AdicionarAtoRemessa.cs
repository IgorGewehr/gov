using FluentValidation;
using Tensorroot.Gov.BuildingBlocks.Application.Abstractions;
using Tensorroot.Gov.BuildingBlocks.Application.Messaging;
using Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;
using Tensorroot.Gov.Modules.RecursosHumanos.Domain.SicapPessoal;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.SicapPessoal;

/// <summary>
/// Acrescenta um ato de admissao a uma remessa SICAP-AP/SIAPES aberta, preenchido AUTOMATICAMENTE a
/// partir de um servidor existente (CPF/nome/nascimento/cargo/datas). O titulo e o regime juridico sao
/// derivados do TIPO DO CARGO (atributo proprio do vinculo, nao do regime previdenciario), podendo ser
/// sobrescritos quando o caso concreto exigir (ex.: admissao por decisao judicial, emprego celetista).
/// O identificador do ato (IDENTIFICADOR_ATO) usa a matricula do servidor.
/// </summary>
/// <param name="RemessaId">Remessa (aberta) destino.</param>
/// <param name="ServidorId">Servidor de origem dos dados do ato.</param>
/// <param name="TituloOverride">Titulo de admissao a forcar (opcional; default derivado do cargo).</param>
/// <param name="RegimeOverride">Regime juridico a forcar (opcional; default derivado do regime previdenciario).</param>
/// <param name="ClassificacaoConcurso">Classificacao no concurso (obrigatoria no titulo concurso publico).</param>
public sealed record AdicionarAtoDeServidorCommand(
    Guid RemessaId,
    Guid ServidorId,
    TipoAtoAdmissao? TituloOverride,
    RegimeJuridicoSiapes? RegimeOverride,
    int? ClassificacaoConcurso) : ICommand<Guid>;

/// <summary>Regras de validacao da adicao de ato a partir de servidor.</summary>
public sealed class AdicionarAtoDeServidorValidator : AbstractValidator<AdicionarAtoDeServidorCommand>
{
    /// <summary>Define as regras.</summary>
    public AdicionarAtoDeServidorValidator()
    {
        RuleFor(comando => comando.RemessaId).NotEmpty();
        RuleFor(comando => comando.ServidorId).NotEmpty();
        RuleFor(comando => comando.TituloOverride)
            .IsInEnum()
            .When(comando => comando.TituloOverride is not null);
        RuleFor(comando => comando.RegimeOverride)
            .IsInEnum()
            .When(comando => comando.RegimeOverride is not null);
    }
}

/// <summary>Handler da adicao de ato a partir de servidor.</summary>
public sealed class AdicionarAtoDeServidorHandler(
    IRemessaSicapPessoalRepository remessas,
    IServidorRepository servidores,
    ICargoRepository cargos,
    IUnitOfWork unitOfWork)
    : ICommandHandler<AdicionarAtoDeServidorCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Guid> Handle(AdicionarAtoDeServidorCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var remessa = await remessas.ObterPorIdAsync(new RemessaSicapPessoalId(request.RemessaId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Remessa de pessoal nao encontrada.");
        var servidor = await servidores.ObterPorIdAsync(new ServidorId(request.ServidorId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Servidor nao encontrado.");
        var cargo = await cargos.ObterPorIdAsync(servidor.CargoId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Cargo do servidor nao encontrado.");

        var titulo = request.TituloOverride ?? MapeamentoSiapes.TituloPadraoDe(cargo.Tipo);
        // Regime JURIDICO deriva do TIPO DO CARGO (atributo do vinculo), nao do regime previdenciario (P1-6).
        var regime = request.RegimeOverride ?? MapeamentoSiapes.RegimePadraoDe(cargo.Tipo);

        // DATA_ATO = admissao/exercicio; DATA_HISTORICA = nomeacao (titulo concurso); extincao quando desligado.
        var dataAto = servidor.DataExercicio ?? servidor.DataNomeacao;
        var dataHistorica = titulo == TipoAtoAdmissao.ConcursoPublico ? servidor.DataNomeacao : (DateOnly?)null;
        var dataTermino = servidor.DataDesligamento;
        var motivoExtincao = dataTermino is not null ? MotivoExtincaoVinculo.ExoneracaoDemissaoRescisao : (MotivoExtincaoVinculo?)null;

        var atoId = remessa.AdicionarAto(
            servidor.Id,
            servidor.Matricula.Valor,
            titulo,
            regime,
            servidor.Cpf.Digitos,
            servidor.DadosPessoais.Nome,
            servidor.DadosPessoais.DataNascimento,
            cargo.Denominacao,
            CargaHorariaSemanalPadrao,
            request.ClassificacaoConcurso,
            dataAto,
            dataHistorica,
            dataTermino,
            motivoExtincao);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return atoId.Value;
    }

    // Jornada padrao semanal quando o cadastro do cargo nao guarda carga horaria propria (40h — jornada
    // administrativa tipica). // TODO(M10): puxar a carga horaria efetiva da jornada de ponto/cargo.
    private const int CargaHorariaSemanalPadrao = 40;
}
