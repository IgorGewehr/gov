using Tensorroot.Gov.Modules.Saude.Domain.Pacientes;
using Tensorroot.Gov.SharedKernel.ValueObjects;

namespace Tensorroot.Gov.Modules.Saude.Application.Pacientes;

/// <summary>Conversores entre DTOs de entrada e os Value Objects do dominio do <see cref="Paciente"/>.</summary>
internal static class PacienteMapeamento
{
    /// <summary>Converte um <see cref="IdentificacaoDto"/> no VO <see cref="Identificacao"/>.</summary>
    /// <param name="dto">DTO de identificacao.</param>
    /// <param name="hoje">Data corrente (guarda de nascimento nao futuro).</param>
    /// <returns>VO de identificacao validado.</returns>
    public static Identificacao ParaDominio(this IdentificacaoDto dto, DateOnly hoje)
    {
        ArgumentNullException.ThrowIfNull(dto);
        Cpf? cpf = null;
        if (!string.IsNullOrWhiteSpace(dto.Cpf))
        {
            cpf = Cpf.Create(dto.Cpf);
        }

        return new Identificacao(dto.Nome, dto.DataNascimento, (Sexo)dto.Sexo, dto.NomeSocial, cpf, hoje);
    }

    /// <summary>Converte um <see cref="EnderecoDto"/> no VO <see cref="Endereco"/>.</summary>
    /// <param name="dto">DTO de endereco.</param>
    /// <returns>VO de endereco validado.</returns>
    public static Endereco ParaDominio(this EnderecoDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        return new Endereco(dto.Logradouro, dto.Numero, dto.Bairro, dto.Municipio, dto.Uf, dto.Cep);
    }
}
