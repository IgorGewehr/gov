namespace Tensorroot.Gov.Modules.Transparencia.Domain.RemessasTce.Leiautes;

/// <summary>
/// Um arquivo já montado da remessa (corpo posicional + as linhas/valores que o originaram), usado pela
/// pré-validação local e pelo empacotamento. Estrutura de transporte do domínio (não persistida).
/// </summary>
/// <param name="Definicao">Definição do registro/arquivo do leiaute.</param>
/// <param name="Linhas">Linhas (valores por nome de campo) que compõem o corpo.</param>
/// <param name="Conteudo">Bytes ISO-8859-1 do arquivo completo (cabeçalho + corpo + finalizador).</param>
public sealed record ArquivoMontado(
    RegistroLeiauteDef Definicao,
    IReadOnlyList<IReadOnlyDictionary<string, ValorCampo>> Linhas,
    ReadOnlyMemory<byte> Conteudo);
