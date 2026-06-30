// Picker reutilizável de Contribuinte para o balcão de Tributos (P1). Em vez de exigir o
// GUID digitado, oferece busca textual por nome ou CPF/CNPJ (endpoint real
// GET /tributos/contribuintes?termo=) + seleção, expondo o id selecionado via onChange.
//
// Construído sobre o EntityPicker compartilhado (components/ui): busca search-as-you-type
// (debounced), navegação por teclado e estados (carregando/vazio/erro) padronizados.
import { EntityPicker } from '../../components/ui';
import type { EntityPickerQuery } from '../../components/ui';
import { useContribuintes } from './contribuinte.api';
import type { ContribuinteResumo } from './contribuinte.api';

interface ContribuintePickerProps {
  /** Id do contribuinte selecionado (ou ''). */
  value: string;
  onChange: (id: string) => void;
  label?: string;
  required?: boolean;
  error?: string;
  disabled?: boolean;
}

/** Adapta o hook de busca de contribuintes à assinatura do EntityPicker. */
function useBuscarContribuintePicker(
  termo: string,
  habilitado: boolean,
): EntityPickerQuery<ContribuinteResumo> {
  const query = useContribuintes(termo, habilitado);
  return {
    data: query.data,
    isFetching: query.isFetching,
    isError: query.isError,
    error: query.error,
  };
}

/** Picker de contribuinte: busca por nome/documento (CPF/CNPJ) e seleciona o contribuinte. */
export function ContribuintePicker({
  value,
  onChange,
  label = 'Contribuinte',
  required,
  error,
  disabled,
}: ContribuintePickerProps) {
  return (
    <EntityPicker<ContribuinteResumo>
      value={value}
      onChange={onChange}
      label={label}
      required={required}
      error={error}
      disabled={disabled}
      placeholder="Buscar por nome ou CPF/CNPJ (mín. 2 caracteres)"
      help="Informe parte do nome ou do documento e selecione o contribuinte."
      useBuscar={useBuscarContribuintePicker}
      idDe={(c) => c.id}
      rotuloDe={(c) => c.nome}
      detalheDe={(c) => c.documentoMascarado}
    />
  );
}
