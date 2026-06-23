// Picker reutilizável de Estabelecimento (CNES/UBS) para os formulários de Farmácia e
// Imunização (entrada de estoque, dispensação, aplicação de dose). Em vez de exigir o
// GUID digitado, oferece busca textual (reuso do endpoint real de lista) + seleção,
// expondo o id selecionado via onChange. Acessível (rótulo associado, status aria-live).
import { useMemo, useState } from 'react';
import { FormField, Input, Spinner, Tag } from '../../components/ui';
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

/** Picker de estabelecimento: busca por nome/CNES e seleciona uma unidade ativa. */
export function EstabelecimentoPicker({
  value,
  onChange,
  label,
  required,
  error,
  disabled,
}: EstabelecimentoPickerProps) {
  const [termo, setTermo] = useState('');
  const filtro = useMemo(
    () => ({ termo: termo.trim() || undefined, situacao: 'Ativo', pagina: 1, tamanho: 8 }),
    [termo],
  );
  const habilitado = !disabled && termo.trim().length >= 2 && value === '';
  const query = useBuscarEstabelecimentos(filtro, habilitado);
  const itens = query.data?.itens ?? [];

  const selecionar = (estab: EstabelecimentoItemLista) => {
    onChange(estab.id);
    setTermo(estab.nome);
  };

  return (
    <FormField label={label} required={required} error={error}>
      {({ id, describedBy, invalid }) => (
        <>
          <Input
            id={id}
            aria-describedby={describedBy}
            invalid={invalid}
            disabled={disabled}
            value={termo}
            onChange={(e) => {
              setTermo(e.target.value);
              if (value !== '') onChange('');
            }}
            placeholder="Buscar por nome ou CNES (mín. 2 caracteres)"
          />
          {value !== '' ? (
            <p className="mt-1 mb-0" aria-live="polite">
              <Tag variant="success">Estabelecimento selecionado</Tag>
            </p>
          ) : (
            habilitado && (
              <div className="br-list mt-1" role="listbox" aria-label="Resultados de estabelecimentos">
                {query.isFetching && (
                  <span className="d-inline-flex align-items-center p-2">
                    <Spinner /> <span className="ml-2">Buscando…</span>
                  </span>
                )}
                {!query.isFetching && itens.length === 0 && (
                  <span className="d-block p-2 text-secondary">
                    Nenhum estabelecimento ativo encontrado.
                  </span>
                )}
                {itens.map((estab) => (
                  <button
                    key={estab.id}
                    type="button"
                    className="br-item d-flex align-items-center justify-content-between w-100 text-left"
                    onClick={() => selecionar(estab)}
                  >
                    <span>{estab.nome}</span>
                    <span className="text-down-01 text-secondary">CNES {estab.cnes}</span>
                  </button>
                ))}
              </div>
            )
          )}
        </>
      )}
    </FormField>
  );
}
