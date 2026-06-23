// Modal de INCLUSÃO na fila de espera (paciente + profissional OU especialidade/CBO).
// WIRED a useEntrarNaFila + Toast. Espelha EntrarNaFilaDeEsperaCommand. Separado dos
// demais modais da fila para manter cada arquivo < 300 linhas.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Alert, Button, FormField, Input, Modal, Select, useToast } from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import { useEntrarNaFila } from './api';
import type { EntrarNaFilaInput } from './api';
import {
  opcoesPrioridadeAgendamento,
  opcoesTipoAgenda,
  paraPrioridadeAgendamento,
  paraTipoAgenda,
} from './saude.helpers';
import { PacientePicker, ProfissionalPicker } from './AgendamentoPickers';

export interface EntrarNaFilaModalProps {
  open: boolean;
  onClose: () => void;
}

interface FormErrors {
  pacienteId?: string;
  estabelecimentoId?: string;
  destino?: string;
}

export function EntrarNaFilaModal({ open, onClose }: EntrarNaFilaModalProps) {
  const toast = useToast();
  const mutation = useEntrarNaFila();

  const [pacienteId, setPacienteId] = useState('');
  const [estabelecimentoId, setEstabelecimentoId] = useState('');
  const [profissionalId, setProfissionalId] = useState('');
  const [especialidade, setEspecialidade] = useState('');
  const [tipo, setTipo] = useState('1');
  const [prioridade, setPrioridade] = useState('1');
  const [errors, setErrors] = useState<FormErrors>({});

  function limpar(): void {
    setPacienteId('');
    setEstabelecimentoId('');
    setProfissionalId('');
    setEspecialidade('');
    setTipo('1');
    setPrioridade('1');
    setErrors({});
  }

  function fechar(): void {
    limpar();
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const next: FormErrors = {};
    if (pacienteId.trim() === '') next.pacienteId = 'Selecione o paciente.';
    if (estabelecimentoId.trim() === '') next.estabelecimentoId = 'Informe o estabelecimento.';
    if (profissionalId.trim() === '' && especialidade.trim() === '')
      next.destino = 'Informe um profissional desejado OU uma especialidade (CBO).';
    setErrors(next);
    if (Object.keys(next).length > 0) return;

    const input: EntrarNaFilaInput = {
      pacienteId,
      estabelecimentoId: estabelecimentoId.trim(),
      profissionalId: profissionalId.trim() || null,
      especialidade: especialidade.trim() || null,
      tipo: paraTipoAgenda(tipo),
      prioridade: paraPrioridadeAgendamento(prioridade),
    };

    mutation.mutate(input, {
      onSuccess: () => {
        toast.success('Paciente incluído na fila de espera.', 'Sucesso');
        fechar();
      },
      onError: (error) => {
        toast.error(
          error instanceof ApiError ? error.userMessage : 'Não foi possível incluir na fila.',
        );
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Incluir na fila de espera"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-entrar-fila" loading={mutation.isPending}>
            Incluir na fila
          </Button>
        </>
      }
    >
      <form id="form-entrar-fila" className="br-form" onSubmit={submeter} noValidate>
        <PacientePicker
          label="Paciente"
          value={pacienteId}
          onChange={setPacienteId}
          error={errors.pacienteId}
        />

        <FormField label="Estabelecimento (CNES)" required error={errors.estabelecimentoId}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={estabelecimentoId}
              onChange={(e) => setEstabelecimentoId(e.target.value)}
              placeholder="Identificador do estabelecimento"
            />
          )}
        </FormField>

        {errors.destino && (
          <Alert variant="warning" title="Atenção:">
            {errors.destino}
          </Alert>
        )}

        <ProfissionalPicker
          label="Profissional desejado (opcional)"
          value={profissionalId}
          onChange={setProfissionalId}
        />

        <FormField label="Especialidade / CBO (opcional)">
          {({ id, describedBy }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              value={especialidade}
              onChange={(e) => setEspecialidade(e.target.value)}
              placeholder="Ex.: 225125 (Cardiologia)"
            />
          )}
        </FormField>

        <div className="row">
          <div className="col-12 col-md-6">
            <FormField label="Natureza" required>
              {({ id }) => (
                <Select
                  id={id}
                  options={opcoesTipoAgenda}
                  value={tipo}
                  onChange={(e) => setTipo(e.target.value)}
                />
              )}
            </FormField>
          </div>
          <div className="col-12 col-md-6">
            <FormField label="Prioridade" required>
              {({ id }) => (
                <Select
                  id={id}
                  options={opcoesPrioridadeAgendamento}
                  value={prioridade}
                  onChange={(e) => setPrioridade(e.target.value)}
                />
              )}
            </FormField>
          </div>
        </div>
      </form>
    </Modal>
  );
}
