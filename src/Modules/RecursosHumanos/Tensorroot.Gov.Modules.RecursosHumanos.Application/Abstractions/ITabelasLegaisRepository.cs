using Tensorroot.Gov.Modules.RecursosHumanos.Domain.TabelasLegais;

namespace Tensorroot.Gov.Modules.RecursosHumanos.Application.Abstractions;

/// <summary>Repositorio de escrita das tabelas legais parametrizadas (INSS/IRRF/RPPS) por tenant/competencia.</summary>
public interface ITabelasLegaisRepository
{
    /// <summary>Marca uma nova tabela INSS para insercao.</summary>
    /// <param name="tabela">Tabela INSS a adicionar.</param>
    void Adicionar(TabelaInss tabela);

    /// <summary>Marca uma nova tabela IRRF para insercao.</summary>
    /// <param name="tabela">Tabela IRRF a adicionar.</param>
    void Adicionar(TabelaIrrf tabela);

    /// <summary>Marca uma nova tabela RPPS municipal para insercao.</summary>
    /// <param name="tabela">Tabela RPPS a adicionar.</param>
    void Adicionar(TabelaRpps tabela);
}
