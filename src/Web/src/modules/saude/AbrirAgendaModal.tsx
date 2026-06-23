// Modal de ABERTURA de grade de disponibilidade (agenda do profissional/UBS). Define o
// profissional, o estabelecimento (CNES), a natureza, a data e o expediente, gerando as
// vagas (slots). Espelha AbrirAgendaCommand(ProfissionalId, EstabelecimentoId, Tipo, Data,
// HoraInicio, HoraFim, DuracaoSlotMinutos, CapacidadeVagas).
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Alert, Button, FormField, Input, Modal, Select, useToast } from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import { useAbrirAgenda } from './api';
import type { AbrirAgendaInput } from './api';
import { opcoesTipoAgenda, paraTipoAgenda } from './saude.helpers';
import { ProfissionalPicker } from './AgendamentoPickers';

export interface AbrirAgendaModalProps {
  open: boolean;
  onClose: () => void;
}

interface FormErrors {
  profissionalId?: string;
  estabelecimentoId?: string;
  data?: string;
  horario?: string;
  duracao?: string;
  capacidade?: string;
}

export function AbrirAgendaModal({ open, onClose }: AbrirAgendaModalProps) {
  const toast = useToast();
  const mutation = useAbrirAgenda();

  const [profissionalId, setProfissionalId] = useState('');
  const [estabelecimentoId, setEstabelecimentoId] = useState('');
  const [tipo, setTipo] = useState('1');
  const [data, setData] = useState('');
  const [horaInicio, setHoraInicio] = useState('08:00');
  const [horaFim, setHoraFim] = useState('12:00');
  const [duracao, setDuracao] = useState('20');
  const [capacidade, setCapacidade] = useState('1');
  const [errors, setErrors] = useState<FormErrors>({});

  function limpar(): void {
    setProfissionalId('');
    setEstabelecimentoId('');
    setTipo('1');
    setData('');
    setHoraInicio('08:00');
    setHoraFim('12:00');
    setDuracao('20');
    setCapacidade('1');
    setErrors({});
  }

  function fechar(): void {
    limpar();
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const next: FormErrors = {};
    if (profissionalId.trim() === '') next.profissionalId = 'Selecione o profissional.';
    if (estabelecimentoId.trim() === '') next.estabelecimentoId = 'Informe o estabelecimento.';
    if (data === '') next.data = 'Informe a data da grade.';
    if (horaInicio >= horaFim) next.horario = 'A hora fim deve ser posterior à hora início.';
    if (Number(duracao) <= 0) next.duracao = 'Duração inválida.';
    if (Number(capacidade) <= 0) next.capacidade = 'Capacidade inválida.';
    setErrors(next);
    if (Object.keys(next).length > 0) return;

    const input: AbrirAgendaInput = {
      profissionalId,
      estabelecimentoId: estabelecimentoId.trim(),
      tipo: paraTipoAgenda(tipo),
      data,
      horaInicio,
      horaFim,
      duracaoSlotMinutos: Number(duracao),
      capacidadeVagas: Number(capacidade),
    };

    mutation.mutate(input, {
      onSuccess: () => {
        toast.success('Agenda aberta (em rascunho). Publique para liberar as vagas.', 'Sucesso');
        fechar();
      },
      onError: (error) => {
        toast.error(
          error instanceof ApiError ? error.userMessage : 'Não foi possível abrir a agenda.',
        );
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Abrir agenda (grade de disponibilidade)"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-abrir-agenda" loading={mutation.isPending}>
            Abrir agenda
          </Button>
        </>
      }
    >
      <form id="form-abrir-agenda" className="br-form" onSubmit={submeter} noValidate>
        <Alert variant="info" title="Rascunho:">
          A agenda nasce em rascunho. Publique-a depois para liberar as vagas para marcação.
        </Alert>

        <ProfissionalPicker
          label="Profissional"
          value={profissionalId}
          onChange={setProfissionalId}
          error={errors.profissionalId}
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
            <FormField label="Data" required error={errors.data}>
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  aria-describedby={describedBy}
                  invalid={invalid}
                  type="date"
                  value={data}
                  onChange={(e) => setData(e.target.value)}
                />
              )}
            </FormField>
          </div>
        </div>

        <div className="row">
          <div className="col-6">
            <FormField label="Hora início" required error={errors.horario}>
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  aria-describedby={describedBy}
                  invalid={invalid}
                  type="time"
                  value={horaInicio}
                  onChange={(e) => setHoraInicio(e.target.value)}
                />
              )}
            </FormField>
          </div>
          <div className="col-6">
            <FormField label="Hora fim" required>
              {({ id }) => (
                <Input id={id} type="time" value={horaFim} onChange={(e) => setHoraFim(e.target.value)} />
              )}
            </FormField>
          </div>
        </div>

        <div className="row">
          <div className="col-6">
            <FormField label="Duração do slot (min)" required error={errors.duracao}>
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  aria-describedby={describedBy}
                  invalid={invalid}
                  type="number"
                  min={1}
                  value={duracao}
                  onChange={(e) => setDuracao(e.target.value)}
                />
              )}
            </FormField>
          </div>
          <div className="col-6">
            <FormField label="Vagas por slot" required error={errors.capacidade}>
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  aria-describedby={describedBy}
                  invalid={invalid}
                  type="number"
                  min={1}
                  value={capacidade}
                  onChange={(e) => setCapacidade(e.target.value)}
                />
              )}
            </FormField>
          </div>
        </div>
      </form>
    </Modal>
  );
}
