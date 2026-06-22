namespace Tensorroot.Gov.Modules.AssistenciaSocial.Application.Familias;

/// <summary>Dado de entrada de um membro do nucleo familiar (composicao + renda).</summary>
/// <param name="Cpf">CPF do membro (com ou sem mascara).</param>
/// <param name="Parentesco">Parentesco com o responsavel familiar (1=RF, 2=Conjuge, 3=Filho, 9=Outro).</param>
/// <param name="DataNascimento">Data de nascimento.</param>
/// <param name="RendaIndividual">Renda individual declarada (nao-negativa).</param>
/// <param name="EhPcd">Indicador de pessoa com deficiencia (dado sensivel — art. 11 LGPD).</param>
public sealed record MembroFamiliarDto(
    string Cpf,
    int Parentesco,
    DateOnly DataNascimento,
    decimal RendaIndividual,
    bool EhPcd);

/// <summary>Dado de entrada de um endereco territorializado (vinculado ao territorio do CRAS).</summary>
/// <param name="Logradouro">Logradouro.</param>
/// <param name="Municipio">Municipio.</param>
/// <param name="Cep">CEP (com ou sem mascara).</param>
/// <param name="Territorio">Territorio de cobertura do CRAS.</param>
public sealed record EnderecoTerritorializadoDto(
    string Logradouro,
    string Municipio,
    string Cep,
    string Territorio);
