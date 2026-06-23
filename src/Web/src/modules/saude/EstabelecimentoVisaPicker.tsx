// Picker reutilizável de Estabelecimento sujeito à VISA — busca textual (reuso do endpoint
// real de lista) + seleção, expondo o id via onChange. Usado nos formulários de abertura de
// inspeção e emissão de licença. Acessível (rótulo associado, listbox, aria-live).
import { useMemo, useState } from 'react';
import { FormField, Input, Spinner, Tag } from '../../components/ui';
import { useBuscarEstabelecimentosVisa } from './vigilancia.api';
import type { EstabelecimentoVisaDto } from './vigilancia.api';

interface EstabelecimentoVisaPickerProps {
  value: string;
  onChange: (id: string) => void;
  label: string;
  required?: boolean;
  error?: string;
  disabled?: boolean;
}

export function EstabelecimentoVisaPicker({
  value,
  onChange,
  label,
  required,
  error,
  disabled,
}: EstabelecimentoVisaPickerProps) {
  const [termo, setTermo] = useState('');
  const filtro = useMemo(
    () => ({ termo: termo.trim() || undefined, situacao: 'Ativo', pagina: 1, tamanho: 8 }),
    [termo],
  );
  const habilitado = !disabled && termo.trim().length >= 2 && value === '';
  const query = useBuscarEstabelecimentosVisa(filtro, habilitado);
  const itens = query.data?.itens ?? [];

  const selecionar = (estab: EstabelecimentoVisaDto) => {
    onChange(estab.id);
    setTermo(estab.razaoSocial);
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
            placeholder="Buscar por razão social ou documento (mín. 2 caracteres)"
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
                    <span>{estab.razaoSocial}</span>
                    <span className="text-down-01 text-secondary">{estab.documento}</span>
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
