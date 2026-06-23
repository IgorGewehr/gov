// Picker reutilizável de Licitação: em vez do GUID cru digitado, lista as licitações
// HOMOLOGADAS (origem lícita de um contrato — art. 90 da NLLC) via o endpoint real
// ListarLicitacoesPorSituacao, com filtro textual por objeto + seleção. Expõe o id
// selecionado via onChange. Acessível (rótulo associado, listbox, status aria-live).
import { useMemo, useState } from 'react';
import { FormField, Input, Spinner, Tag, errorMessage } from '../../../components/ui';
import { useLicitacoesPorSituacao } from './licitacao.api';
import type { LicitacaoResumo } from './licitacao.api';
import { MODALIDADE_LABEL } from './licitacao.helpers';

interface LicitacaoPickerProps {
  /** Id da licitação selecionada (ou ''). */
  value: string;
  onChange: (id: string) => void;
  label: string;
  required?: boolean;
  error?: string;
  disabled?: boolean;
}

/** Picker de licitação homologada: filtra por objeto e seleciona o certame de origem. */
export function LicitacaoPicker({
  value,
  onChange,
  label,
  required,
  error,
  disabled,
}: LicitacaoPickerProps) {
  const [termo, setTermo] = useState('');
  // Origem de contrato decorrente de licitação => certame já homologado (rules / art. 90).
  const query = useLicitacoesPorSituacao('Homologada', !disabled);
  const itens = useMemo(() => query.data ?? [], [query.data]);

  const filtradas = useMemo(() => {
    const t = termo.trim().toLowerCase();
    if (t === '') return itens.slice(0, 8);
    return itens.filter((l) => l.objeto.toLowerCase().includes(t)).slice(0, 8);
  }, [itens, termo]);

  const selecionada = itens.find((l) => l.id === value);
  const listaVisivel = !disabled && value === '';

  const selecionar = (licitacao: LicitacaoResumo) => {
    onChange(licitacao.id);
    setTermo(licitacao.objeto);
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
            value={selecionada ? selecionada.objeto : termo}
            placeholder="Buscar pelo objeto da licitação homologada"
            onChange={(e) => {
              setTermo(e.target.value);
              if (value !== '') onChange('');
            }}
          />
          {value !== '' && selecionada ? (
            <p className="mt-1 mb-0" aria-live="polite">
              <Tag variant="success">Licitação selecionada</Tag>{' '}
              <span className="text-down-01 text-secondary">
                {MODALIDADE_LABEL[selecionada.modalidade]}
                {selecionada.numeroEditalPncp ? ` — Edital PNCP ${selecionada.numeroEditalPncp}` : ''}
              </span>
            </p>
          ) : (
            listaVisivel && (
              <div
                className="br-list mt-1"
                role="listbox"
                aria-label="Licitações homologadas"
              >
                {query.isFetching && (
                  <span className="d-inline-flex align-items-center p-2">
                    <Spinner /> <span className="ml-2">Carregando…</span>
                  </span>
                )}
                {query.isError && (
                  <span className="d-block p-2 text-danger">{errorMessage(query.error)}</span>
                )}
                {!query.isFetching && !query.isError && filtradas.length === 0 && (
                  <span className="d-block p-2 text-secondary">
                    Nenhuma licitação homologada encontrada.
                  </span>
                )}
                {filtradas.map((l) => (
                  <button
                    key={l.id}
                    type="button"
                    className="br-item d-flex align-items-center justify-content-between w-100 text-left"
                    onClick={() => selecionar(l)}
                  >
                    <span>{l.objeto}</span>
                    <span className="text-down-01 text-secondary">{MODALIDADE_LABEL[l.modalidade]}</span>
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
