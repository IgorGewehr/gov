// Picker reutilizável de Credor (credor/fornecedor da despesa) para os formulários e
// consultas de Finanças (ex.: extrato/razão do credor, retenções). Em vez de exigir o
// GUID digitado, oferece busca textual por nome/documento (endpoint real GET
// /financas/credores?termo=) + seleção, expondo o id selecionado via onChange.
//
// Construído sobre o EntityPicker compartilhado (components/ui): busca search-as-you-type
// (debounced), navegação por teclado e estados (carregando/vazio/erro) padronizados.
import { EntityPicker } from '../../components/ui';
import type { EntityPickerQuery } from '../../components/ui';
import { useCredores } from './credor.api';
import type { CredorResumo } from './credor.api';

interface CredorPickerProps {
  /** Id do credor selecionado (ou ''). */
  value: string;
  onChange: (id: string) => void;
  label?: string;
  required?: boolean;
  error?: string;
  disabled?: boolean;
}

/** Adapta o hook de busca de credores à assinatura do EntityPicker. */
function useBuscarCredorPicker(termo: string, habilitado: boolean): EntityPickerQuery<CredorResumo> {
  const query = useCredores(termo, habilitado);
  return {
    data: query.data,
    isFetching: query.isFetching,
    isError: query.isError,
    error: query.error,
  };
}

/** Picker de credor: busca por nome/documento (CPF/CNPJ) e seleciona o credor. */
export function CredorPicker({
  value,
  onChange,
  label = 'Credor',
  required,
  error,
  disabled,
}: CredorPickerProps) {
  return (
    <EntityPicker<CredorResumo>
      value={value}
      onChange={onChange}
      label={label}
      required={required}
      error={error}
      disabled={disabled}
      placeholder="Buscar por nome ou CPF/CNPJ (mín. 2 caracteres)"
      help="Informe parte do nome ou do documento e selecione o credor."
      useBuscar={useBuscarCredorPicker}
      idDe={(c) => c.id}
      rotuloDe={(c) => c.nome}
      detalheDe={(c) => c.documento}
    />
  );
}
