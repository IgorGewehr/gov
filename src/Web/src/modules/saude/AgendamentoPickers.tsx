// Pickers reutilizáveis de Paciente e Profissional para os formulários de Agendamento.
// Em vez de exigir o GUID digitado à mão, oferecem busca textual (reuso dos endpoints
// reais de lista) + seleção, expondo o id selecionado via onChange. Acessíveis (rótulos
// associados, status com aria-live) e dentro do padrão gov.br DS.
import { useMemo, useState } from 'react';
import type { ReactNode } from 'react';
import { FormField, Input, Spinner, Tag } from '../../components/ui';
import { useBuscarPacientes, useBuscarProfissionais } from './api';
import type { PacienteItemLista, ProfissionalItemLista } from './api';

interface PickerBaseProps {
  /** Id do registro selecionado (ou ''). */
  value: string;
  onChange: (id: string) => void;
  label: string;
  error?: string;
  disabled?: boolean;
}

/** Linha de um resultado selecionável (botão acessível). */
function OpcaoSelecionavel({
  ativo,
  onSelecionar,
  children,
}: {
  ativo: boolean;
  onSelecionar: () => void;
  children: ReactNode;
}) {
  return (
    <button
      type="button"
      className={`br-item d-flex align-items-center justify-content-between w-100 text-left ${
        ativo ? 'selected' : ''
      }`}
      aria-pressed={ativo}
      onClick={onSelecionar}
    >
      {children}
    </button>
  );
}

/** Picker de paciente: busca por nome/CPF/CNS e seleciona um paciente ativo. */
export function PacientePicker({ value, onChange, label, error, disabled }: PickerBaseProps) {
  const [termo, setTermo] = useState('');
  const filtro = useMemo(
    () => ({ termo: termo.trim() || undefined, situacao: 'Ativo', pagina: 1, tamanho: 8 }),
    [termo],
  );
  const habilitado = !disabled && termo.trim().length >= 2 && value === '';
  const query = useBuscarPacientes(filtro, habilitado);
  const itens = query.data?.itens ?? [];

  const selecionado = (paciente: PacienteItemLista) => {
    onChange(paciente.id);
    setTermo(paciente.nomeSocial || paciente.nome);
  };

  return (
    <FormField label={label} required error={error}>
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
            placeholder="Buscar por nome, CPF ou CNS (mín. 2 caracteres)"
          />
          {value !== '' ? (
            <p className="mt-1 mb-0" aria-live="polite">
              <Tag variant="success">Paciente selecionado</Tag>
            </p>
          ) : (
            habilitado && (
              <div className="br-list mt-1" role="listbox" aria-label="Resultados de pacientes">
                {query.isFetching && (
                  <span className="d-inline-flex align-items-center p-2">
                    <Spinner /> <span className="ml-2">Buscando…</span>
                  </span>
                )}
                {!query.isFetching && itens.length === 0 && (
                  <span className="d-block p-2 text-secondary">Nenhum paciente ativo encontrado.</span>
                )}
                {itens.map((paciente) => (
                  <OpcaoSelecionavel
                    key={paciente.id}
                    ativo={false}
                    onSelecionar={() => selecionado(paciente)}
                  >
                    <span>{paciente.nomeSocial || paciente.nome}</span>
                    <span className="text-down-01 text-secondary">CNS {paciente.cns}</span>
                  </OpcaoSelecionavel>
                ))}
              </div>
            )
          )}
        </>
      )}
    </FormField>
  );
}

/** Picker de profissional: busca por nome/CBO e seleciona um profissional ativo. */
export function ProfissionalPicker({ value, onChange, label, error, disabled }: PickerBaseProps) {
  const [termo, setTermo] = useState('');
  const filtro = useMemo(
    () => ({ termo: termo.trim() || undefined, situacao: 'Ativo', pagina: 1, tamanho: 8 }),
    [termo],
  );
  const habilitado = !disabled && termo.trim().length >= 2 && value === '';
  const query = useBuscarProfissionais(filtro, habilitado);
  const itens = query.data?.itens ?? [];

  const selecionado = (profissional: ProfissionalItemLista) => {
    onChange(profissional.id);
    setTermo(profissional.nome);
  };

  return (
    <FormField label={label} required error={error}>
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
            placeholder="Buscar por nome ou registro (mín. 2 caracteres)"
          />
          {value !== '' ? (
            <p className="mt-1 mb-0" aria-live="polite">
              <Tag variant="success">Profissional selecionado</Tag>
            </p>
          ) : (
            habilitado && (
              <div className="br-list mt-1" role="listbox" aria-label="Resultados de profissionais">
                {query.isFetching && (
                  <span className="d-inline-flex align-items-center p-2">
                    <Spinner /> <span className="ml-2">Buscando…</span>
                  </span>
                )}
                {!query.isFetching && itens.length === 0 && (
                  <span className="d-block p-2 text-secondary">Nenhum profissional ativo encontrado.</span>
                )}
                {itens.map((profissional) => (
                  <OpcaoSelecionavel
                    key={profissional.id}
                    ativo={false}
                    onSelecionar={() => selecionado(profissional)}
                  >
                    <span>{profissional.nome}</span>
                    <span className="text-down-01 text-secondary">
                      {profissional.conselho || 'Sem conselho'}
                    </span>
                  </OpcaoSelecionavel>
                ))}
              </div>
            )
          )}
        </>
      )}
    </FormField>
  );
}
