// Editor reutilizavel da composicao familiar (lista de MembroFamiliar) usado pelo
// formulario de referenciamento e pelo modal de atualizacao de renda. Acessivel
// (fieldset/legend, FormField com aria-describedby) e gov.br DS (classes br-*).
import { Button, FormField, Input, Select } from '../../../components/ui';
import { formatarMoeda } from '../../../i18n/format';
import { PARENTESCO_OPCOES } from './familia.api';
import type { MembroFamiliarInput, ParentescoValor } from './familia.api';

/** Linha editavel: campos como string para controle de input; convertida no submit. */
export interface MembroLinha {
  cpf: string;
  parentesco: string;
  dataNascimento: string;
  rendaIndividual: string;
  ehPcd: boolean;
}

/** Cria uma linha de membro vazia. */
export function membroLinhaVazia(): MembroLinha {
  return { cpf: '', parentesco: '3', dataNascimento: '', rendaIndividual: '', ehPcd: false };
}

/** Converte as linhas do formulario em DTOs de membro para o backend. */
export function linhasParaMembros(linhas: MembroLinha[]): MembroFamiliarInput[] {
  return linhas.map((l) => ({
    cpf: l.cpf.trim(),
    parentesco: (Number(l.parentesco) || 9) as ParentescoValor,
    dataNascimento: l.dataNascimento,
    rendaIndividual: Number(l.rendaIndividual) || 0,
    ehPcd: l.ehPcd,
  }));
}

/** Soma a renda familiar total das linhas (para previa da renda per capita). */
export function rendaFamiliarTotal(linhas: MembroLinha[]): number {
  return linhas.reduce((acc, l) => acc + (Number(l.rendaIndividual) || 0), 0);
}

export interface MembrosFieldsetProps {
  linhas: MembroLinha[];
  onChange: (linhas: MembroLinha[]) => void;
  /** Mensagens de erro por indice (campo "cpf" e/ou "geral"). */
  errosPorLinha?: Record<number, { cpf?: string }>;
  /** Mensagem de erro agregada (ex.: ao menos um membro). */
  erroGeral?: string;
  disabled?: boolean;
}

export function MembrosFieldset({
  linhas,
  onChange,
  errosPorLinha = {},
  erroGeral,
  disabled = false,
}: MembrosFieldsetProps) {
  function atualizar(indice: number, patch: Partial<MembroLinha>): void {
    onChange(linhas.map((l, i) => (i === indice ? { ...l, ...patch } : l)));
  }

  function adicionar(): void {
    onChange([...linhas, membroLinhaVazia()]);
  }

  function remover(indice: number): void {
    onChange(linhas.filter((_, i) => i !== indice));
  }

  const total = rendaFamiliarTotal(linhas);
  const perCapita = linhas.length > 0 ? total / linhas.length : 0;

  return (
    <fieldset className="mb-3">
      <legend className="text-up-01 text-semi-bold mb-2">Composição familiar</legend>

      {erroGeral && (
        <p className="feedback danger mb-2" role="alert">
          <i className="fas fa-times-circle" aria-hidden="true" /> {erroGeral}
        </p>
      )}

      {linhas.map((linha, indice) => (
        <div className="br-card p-3 mb-3" key={indice}>
          <div className="d-flex justify-content-between align-items-center mb-2">
            <span className="text-semi-bold">Membro {indice + 1}</span>
            <Button
              variant="tertiary"
              className="small"
              onClick={() => remover(indice)}
              disabled={disabled || linhas.length <= 1}
              aria-label={`Remover membro ${indice + 1}`}
            >
              <i className="fas fa-trash" aria-hidden="true" /> Remover
            </Button>
          </div>

          <div className="row">
            <div className="col-sm-6">
              <FormField label="CPF" required error={errosPorLinha[indice]?.cpf}>
                {({ id, describedBy, invalid }) => (
                  <Input
                    id={id}
                    aria-describedby={describedBy}
                    invalid={invalid}
                    inputMode="numeric"
                    value={linha.cpf}
                    onChange={(e) => atualizar(indice, { cpf: e.target.value })}
                    disabled={disabled}
                    placeholder="000.000.000-00"
                  />
                )}
              </FormField>
            </div>

            <div className="col-sm-6">
              <FormField label="Parentesco" required>
                {({ id, describedBy }) => (
                  <Select
                    id={id}
                    aria-describedby={describedBy}
                    options={PARENTESCO_OPCOES}
                    value={linha.parentesco}
                    onChange={(e) => atualizar(indice, { parentesco: e.target.value })}
                    disabled={disabled}
                  />
                )}
              </FormField>
            </div>

            <div className="col-sm-6">
              <FormField label="Data de nascimento" required>
                {({ id, describedBy }) => (
                  <Input
                    id={id}
                    type="date"
                    aria-describedby={describedBy}
                    value={linha.dataNascimento}
                    onChange={(e) => atualizar(indice, { dataNascimento: e.target.value })}
                    disabled={disabled}
                  />
                )}
              </FormField>
            </div>

            <div className="col-sm-6">
              <FormField label="Renda individual (R$)">
                {({ id, describedBy }) => (
                  <Input
                    id={id}
                    type="number"
                    min="0"
                    step="0.01"
                    inputMode="decimal"
                    aria-describedby={describedBy}
                    value={linha.rendaIndividual}
                    onChange={(e) => atualizar(indice, { rendaIndividual: e.target.value })}
                    disabled={disabled}
                  />
                )}
              </FormField>
            </div>

            <div className="col-12">
              <div className="br-checkbox">
                <input
                  id={`membro-${indice}-pcd`}
                  type="checkbox"
                  checked={linha.ehPcd}
                  onChange={(e) => atualizar(indice, { ehPcd: e.target.checked })}
                  disabled={disabled}
                />
                <label htmlFor={`membro-${indice}-pcd`}>Pessoa com deficiência (PcD)</label>
              </div>
            </div>
          </div>
        </div>
      ))}

      <Button variant="secondary" className="small" onClick={adicionar} disabled={disabled}>
        <i className="fas fa-plus" aria-hidden="true" /> Adicionar membro
      </Button>

      <p className="text-down-01 text-gray-60 mt-3 mb-0" aria-live="polite">
        Renda familiar total: <strong>{formatarMoeda(total)}</strong> — Renda per capita prevista:{' '}
        <strong>{formatarMoeda(perCapita)}</strong> ({linhas.length}{' '}
        {linhas.length === 1 ? 'membro' : 'membros'})
      </p>
    </fieldset>
  );
}
