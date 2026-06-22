using Tensorroot.Gov.Modules.RecursosHumanos.Domain.Servidores;
using Tensorroot.Gov.SharedKernel.ValueObjects;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Servidores;

/// <summary>Projecoes de leitura do agregado <see cref="Servidor"/> (com mascaramento de CPF — LGPD).</summary>
internal static class ProjetarServidor
{
    /// <summary>Projeta um servidor para o resumo de leitura, mascarando o CPF (LGPD).</summary>
    /// <param name="servidor">Servidor a projetar.</param>
    /// <returns>Resumo do servidor com CPF mascarado.</returns>
    public static ServidorResumo ParaResumo(Servidor servidor)
    {
        ArgumentNullException.ThrowIfNull(servidor);
        return new ServidorResumo(
            servidor.Id.Value,
            MascararCpf(servidor.Cpf),
            servidor.Matricula.Valor,
            servidor.DadosPessoais.Nome,
            servidor.CargoId.Value,
            servidor.Regime.ToString(),
            servidor.Situacao.ToString(),
            servidor.DataNomeacao,
            servidor.DataExercicio);
    }

    /// <summary>Mascara o CPF preservando apenas os tres digitos centrais (LGPD): <c>***.NNN.***-**</c>.</summary>
    /// <param name="cpf">CPF a mascarar.</param>
    /// <returns>CPF mascarado.</returns>
    private static string MascararCpf(Cpf cpf)
    {
        ArgumentNullException.ThrowIfNull(cpf);
        var digitos = cpf.Digitos;
        return $"***.{digitos[3..6]}.***-**";
    }
}
