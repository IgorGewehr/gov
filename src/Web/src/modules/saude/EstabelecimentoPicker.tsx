// Picker reutilizável de Estabelecimento (CNES/UBS) para os formulários de Farmácia e
// Imunização (entrada de estoque, dispensação, aplicação de dose). Em vez de exigir o
// GUID digitado, oferece busca textual (reuso do endpoint real de lista) + seleção,
// expondo o id selecionado via onChange.
//
// Construído sobre o EntityPicker compartilhado (components/ui): a busca/seleção, a
// navegação por teclado e os estados (carregando/vazio/erro) são padronizados; aqui
// só ligamos o endpoint real (busca por nome/CNES, unidades ATIVAS) e o rótulo.
import { EntityPicker } from '../../components/ui';
import type { EntityPickerQuery } from '../../components/ui';
import { useBuscarEstabelecimentos } from './api';
import type { EstabelecimentoItemLista } from './api';

interface EstabelecimentoPickerProps {
  /** Id do estabelecimento selecionado (ou ''). */
  value: string;
  onChange: (id: string) => void;
  label: string;
  required?: boolean;
  error?: string;
  disabled?: boolean;
}

/** Adapta o hook do módulo (filtro paginado, só ATIVOS) à assinatura do EntityPicker. */
function useBuscarEstabelecimentoPicker(
  termo: string,
  habilitado: boolean,
): EntityPickerQuery<EstabelecimentoItemLista> {
  const query = useBuscarEstabelecimentos(
    { termo: termo || undefined, situacao: 'Ativo', pagina: 1, tamanho: 8 },
    habilitado,
  );
  return {
    data: query.data?.itens,
    isFetching: query.isFetching,
    isError: query.isError,
    error: query.error,
  };
}

/** Picker de estabelecimento: busca por nome/CNES e seleciona uma unidade ativa. */
export function EstabelecimentoPicker({
  value,
  onChange,
  label,
  required,
  error,
  disabled,
}: EstabelecimentoPickerProps) {
  return (
    <EntityPicker<EstabelecimentoItemLista>
      value={value}
      onChange={onChange}
      label={label}
      required={required}
      error={error}
      disabled={disabled}
      placeholder="Buscar por nome ou CNES (mín. 2 caracteres)"
      useBuscar={useBuscarEstabelecimentoPicker}
      idDe={(e) => e.id}
      rotuloDe={(e) => e.nome}
      detalheDe={(e) => `CNES ${e.cnes}`}
    />
  );
}
