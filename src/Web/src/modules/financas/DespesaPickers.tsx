// Pickers reais do ciclo da despesa (Lei 4.320/64) — substituem o GUID digitado por
// SELEÇÃO a partir dos endpoints de lista reais. Mesmo padrão dos demais módulos
// (busca/filtro + lista + seleção, com estado selecionado e botão "Trocar").
//
//   DotacaoPicker:  lista as dotações de um exercício (GET /financas/dotacoes?exercicio=)
//                   e seleciona pela classificação + saldo disponível.
//   EmpenhoPicker:  primeiro escolhe a dotação (DotacaoPicker), depois lista os empenhos
//                   dela (GET /financas/dotacoes/{dotacaoId}/empenhos) e seleciona.
import { useMemo, useState } from 'react';
import { FormField, Input, Select, Spinner, Tag } from '../../components/ui';
import type { SelectOption } from '../../components/ui';
import { formatarMoeda } from '../../i18n/format';
import { exercicioCorrente } from './financas.helpers';
import { useDotacoesPorExercicio } from './dotacao.api';
import { useEmpenhosPorDotacao } from './empenho.api';
import type { EmpenhoResumo } from './empenho.api';

// --- Dotação picker ---

export interface DotacaoPickerProps {
  /** Id da dotação selecionada (ou ''). */
  value: string;
  onChange: (id: string) => void;
  label?: string;
  required?: boolean;
  error?: string;
  disabled?: boolean;
}

/** Picker de dotação orçamentária: filtra por exercício e seleciona pela classificação. */
export function DotacaoPicker({
  value,
  onChange,
  label = 'Dotação orçamentária',
  required,
  error,
  disabled,
}: DotacaoPickerProps) {
  const [exercicio, setExercicio] = useState(String(exercicioCorrente()));
  const exercicioNum = Number(exercicio);
  const query = useDotacoesPorExercicio(exercicioNum, !disabled);
  const itens = useMemo(() => query.data ?? [], [query.data]);

  const opcoes = useMemo<SelectOption[]>(
    () =>
      itens.map((d) => ({
        value: d.id,
        label: `${d.classificacao} — saldo ${formatarMoeda(d.saldoDisponivel)}`,
        disabled: d.saldoDisponivel <= 0,
      })),
    [itens],
  );

  const selecionada = itens.find((d) => d.id === value);

  return (
    <FormField
      label={label}
      required={required}
      error={error}
      help={
        query.isFetching
          ? 'Carregando dotações do exercício…'
          : 'Filtre pelo exercício e selecione a dotação; sem saldo aparece desabilitada.'
      }
    >
      {({ id, describedBy, invalid }) => (
        <>
          <div className="d-flex align-items-end">
            <div className="mr-2" style={{ maxWidth: '8rem' }}>
              <Input
                aria-label="Exercício da dotação"
                type="number"
                min="2000"
                step="1"
                inputMode="numeric"
                disabled={disabled}
                value={exercicio}
                onChange={(e) => {
                  setExercicio(e.target.value);
                  if (value !== '') onChange('');
                }}
              />
            </div>
            <div className="flex-fill">
              <Select
                id={id}
                aria-describedby={describedBy}
                invalid={invalid}
                disabled={disabled || itens.length === 0}
                options={opcoes}
                placeholder="Selecione a dotação"
                value={value}
                onChange={(e) => onChange(e.target.value)}
              />
            </div>
          </div>
          {query.isFetching && <Spinner label="Buscando dotações…" />}
          {!query.isFetching && Number.isInteger(exercicioNum) && itens.length === 0 && (
            <p className="text-secondary text-down-01 mt-2" aria-live="polite">
              Nenhuma dotação encontrada para {exercicio}.
            </p>
          )}
          {selecionada && (
            <p className="mt-1 mb-0" aria-live="polite">
              <Tag variant="info">
                {selecionada.classificacao} · disponível {formatarMoeda(selecionada.saldoDisponivel)}
              </Tag>
            </p>
          )}
        </>
      )}
    </FormField>
  );
}

// --- Empenho picker ---

export interface EmpenhoPickerProps {
  /** Id do empenho selecionado (ou ''). */
  value: string;
  onChange: (id: string) => void;
  label?: string;
  required?: boolean;
  error?: string;
  disabled?: boolean;
}

/** Picker de empenho: escolhe a dotação e depois o empenho (com saldo a liquidar). */
export function EmpenhoPicker({
  value,
  onChange,
  label = 'Empenho',
  required,
  error,
  disabled,
}: EmpenhoPickerProps) {
  const [dotacaoId, setDotacaoId] = useState('');
  const query = useEmpenhosPorDotacao(dotacaoId, !disabled && dotacaoId.trim().length > 0);
  const itens = useMemo(() => query.data ?? [], [query.data]);

  const opcoes = useMemo<SelectOption[]>(
    () =>
      itens.map((e: EmpenhoResumo) => ({
        value: e.id,
        label: `${e.numero} — ${e.credorNome} · a liquidar ${formatarMoeda(e.saldoALiquidar)}`,
        disabled: e.saldoALiquidar <= 0,
      })),
    [itens],
  );

  const selecionado = itens.find((e) => e.id === value);

  return (
    <FormField
      label={label}
      required={required}
      error={error}
      help="Escolha a dotação e depois o empenho; empenho sem saldo a liquidar aparece desabilitado."
    >
      {({ id, describedBy, invalid }) => (
        <>
          <DotacaoPicker
            label="Dotação do empenho"
            value={dotacaoId}
            disabled={disabled}
            onChange={(novaDotacao) => {
              setDotacaoId(novaDotacao);
              if (value !== '') onChange('');
            }}
          />
          {dotacaoId !== '' && (
            <>
              <Select
                id={id}
                aria-describedby={describedBy}
                invalid={invalid}
                disabled={disabled || itens.length === 0}
                options={opcoes}
                placeholder="Selecione o empenho"
                value={value}
                onChange={(e) => onChange(e.target.value)}
              />
              {query.isFetching && <Spinner label="Buscando empenhos…" />}
              {!query.isFetching && itens.length === 0 && (
                <p className="text-secondary text-down-01 mt-2" aria-live="polite">
                  Nenhum empenho nesta dotação.
                </p>
              )}
              {selecionado && (
                <p className="mt-1 mb-0" aria-live="polite">
                  <Tag variant="info">
                    {selecionado.numero} · {selecionado.credorNome} · a liquidar{' '}
                    {formatarMoeda(selecionado.saldoALiquidar)}
                  </Tag>
                </p>
              )}
            </>
          )}
        </>
      )}
    </FormField>
  );
}
