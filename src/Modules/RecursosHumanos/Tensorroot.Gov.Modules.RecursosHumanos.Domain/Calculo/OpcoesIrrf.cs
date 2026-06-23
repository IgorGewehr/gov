namespace Tensorroot.Gov.Modules.RecursosHumanos.Domain.Calculo;

/// <summary>
/// Opcoes de apuracao do IRRF para o motor de calculo. Permite desligar o desconto simplificado no
/// calculo de BASE SEPARADA do 13o salario (Lei 7.713/88 art. 12-A; IN RFB 1.500/2014 — design §2.2).
/// Mantem o comportamento mensal por default (<see cref="AplicarSimplificado"/> = true). Imutavel.
/// </summary>
/// <param name="AplicarSimplificado">
/// <c>true</c> (default) aplica a regra do mais vantajoso com desconto simplificado (folha mensal/ferias);
/// <c>false</c> veda o simplificado (IRRF do 13o, exclusivo na fonte).
/// </param>
public sealed record OpcoesIrrf(bool AplicarSimplificado = true)
{
    /// <summary>Opcoes padrao (comportamento mensal: aplica o desconto simplificado).</summary>
    public static OpcoesIrrf Mensal { get; } = new(AplicarSimplificado: true);

    /// <summary>Opcoes do 13o salario: desconto simplificado VEDADO (art. 12-A).</summary>
    public static OpcoesIrrf DecimoTerceiro { get; } = new(AplicarSimplificado: false);
}
