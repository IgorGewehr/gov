using Tensorroot.Gov.Modules.Saude.Domain.Imunizacao;
using Tensorroot.Gov.SharedKernel;
using PacienteId = Tensorroot.Gov.Modules.Saude.Domain.Pacientes.PacienteId;

namespace Tensorroot.Gov.Modules.Saude.Domain.Events;

/// <summary>
/// Dose de imunobiologico aplicada e registrada na carteira do paciente. Base para indicadores de
/// cobertura vacinal e, no M10, transmissao ao SI-PNI/RNDS.
/// </summary>
/// <param name="CarteiraId">Identificador da carteira de vacinacao.</param>
/// <param name="PacienteId">Paciente vacinado.</param>
/// <param name="ImunobiologicoId">Imunobiologico aplicado.</param>
/// <param name="NumeroDose">Numero da dose no esquema.</param>
/// <param name="DataAplicacao">Data de aplicacao.</param>
public sealed record DoseAplicadaRegistrada(
    CarteiraVacinacaoId CarteiraId,
    PacienteId PacienteId,
    ImunobiologicoId ImunobiologicoId,
    int NumeroDose,
    DateOnly DataAplicacao) : IDomainEvent;
