// Picker reutilizável de Fornecedor: em vez de exigir o GUID cru digitado (péssima UX),
// o usuário informa o CNPJ e CONFIRMA o fornecedor real (razão social + situação) antes
// de vincular. Reuso do endpoint real ObterFornecedorPorCnpj (useFornecedorPorCnpj).
// Expõe o id selecionado via onChange. Acessível (rótulo associado, status aria-live).
import { useEffect, useState } from 'react';
import { Button, FormField, Input, Spinner, Tag, errorMessage } from '../../../components/ui';
import { useFornecedorPorCnpj, SITUACAO_LABEL } from './fornecedor.api';
import { situacaoTagVariant } from './fornecedor.helpers';

interface FornecedorPickerProps {
  /** Id do fornecedor selecionado (ou ''). */
  value: string;
  onChange: (id: string) => void;
  label: string;
  required?: boolean;
  error?: string;
  disabled?: boolean;
}

/** Picker de fornecedor: busca por CNPJ, confirma o cadastro real e seleciona o id. */
export function FornecedorPicker({
  value,
  onChange,
  label,
  required,
  error,
  disabled,
}: FornecedorPickerProps) {
  const [cnpj, setCnpj] = useState('');
  const [consulta, setConsulta] = useState('');
  const query = useFornecedorPorCnpj(consulta, consulta.length > 0);
  const fornecedor = query.data;

  // Quando a consulta resolve um fornecedor, fixa a seleção (expõe o id ao formulário).
  useEffect(() => {
    if (consulta !== '' && query.isSuccess && fornecedor && value !== fornecedor.id) {
      onChange(fornecedor.id);
    }
  }, [consulta, query.isSuccess, fornecedor, value, onChange]);

  function buscar(): void {
    const termo = cnpj.trim();
    if (termo === '') return;
    if (value !== '') onChange('');
    setConsulta(termo);
  }

  function limpar(): void {
    onChange('');
    setConsulta('');
    setCnpj('');
  }

  const selecionado = value !== '' && fornecedor?.id === value;

  return (
    <FormField label={label} required={required} error={error} help="Informe o CNPJ e confirme o fornecedor.">
      {({ id, describedBy, invalid }) => (
        <>
          <div className="d-flex" style={{ gap: '0.5rem' }}>
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              disabled={disabled || selecionado}
              value={cnpj}
              inputMode="numeric"
              placeholder="00.000.000/0000-00"
              onChange={(e) => {
                setCnpj(e.target.value);
                if (value !== '') onChange('');
                if (consulta !== '') setConsulta('');
              }}
              onKeyDown={(e) => {
                if (e.key === 'Enter') {
                  e.preventDefault();
                  buscar();
                }
              }}
            />
            {selecionado ? (
              <Button variant="secondary" type="button" disabled={disabled} onClick={limpar}>
                Trocar
              </Button>
            ) : (
              <Button
                variant="secondary"
                type="button"
                disabled={disabled || cnpj.trim() === ''}
                loading={query.isFetching}
                onClick={buscar}
              >
                Buscar
              </Button>
            )}
          </div>

          <div aria-live="polite" className="mt-1">
            {query.isFetching && (
              <span className="d-inline-flex align-items-center text-secondary">
                <Spinner /> <span className="ml-2">Buscando fornecedor…</span>
              </span>
            )}
            {!query.isFetching && selecionado && fornecedor && (
              <span className="d-inline-flex align-items-center" style={{ gap: '0.5rem' }}>
                <Tag variant="success">{fornecedor.razaoSocial}</Tag>
                <Tag variant={situacaoTagVariant(fornecedor.situacao)}>
                  {SITUACAO_LABEL[fornecedor.situacao]}
                </Tag>
                <span className="text-down-01 text-secondary">CNPJ {fornecedor.cnpj}</span>
              </span>
            )}
            {!query.isFetching && consulta !== '' && query.isError && (
              <span className="d-block text-danger">{errorMessage(query.error)}</span>
            )}
          </div>
        </>
      )}
    </FormField>
  );
}
