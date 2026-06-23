// Modal de MARCAÇÃO de consulta/exame. Fluxo: selecionar paciente (picker) → filtrar
// vagas livres (profissional/estabelecimento/tipo/janela de datas) → escolher um slot →
// definir prioridade → marcar. Espelha MarcarAgendamentoCommand(PacienteId, VagaId, Prioridade).
import { useMemo, useState } from 'react';
import type { FormEvent } from 'react';
import {
  Button,
  EmptyState,
  FormField,
  Input,
  Modal,
  Select,
  Spinner,
  Tag,
  useToast,
} from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import { formatarDataHora } from '../../i18n/format';
import { useMarcarAgendamento, useVagasLivres } from './api';
import type { MarcarAgendamentoInput, TipoAtendimentoAgenda, VagaLivreItem } from './api';
import {
  opcoesPrioridadeAgendamento,
  opcoesTipoAgenda,
  paraPrioridadeAgendamento,
  paraTipoAgenda,
} from './saude.helpers';
import { PacientePicker, ProfissionalPicker } from './AgendamentoPickers';

export interface MarcarAgendamentoModalProps {
  open: boolean;
  onClose: () => void;
}

interface FormErrors {
  pacienteId?: string;
  vagaId?: string;
}

export function MarcarAgendamentoModal({ open, onClose }: MarcarAgendamentoModalProps) {
  const toast = useToast();
  const mutation = useMarcarAgendamento();

  const [pacienteId, setPacienteId] = useState('');
  const [profissionalId, setProfissionalId] = useState('');
  const [estabelecimentoId, setEstabelecimentoId] = useState('');
  const [tipo, setTipo] = useState('');
  const [de, setDe] = useState('');
  const [ate, setAte] = useState('');
  const [vagaId, setVagaId] = useState('');
  const [prioridade, setPrioridade] = useState('1');
  const [errors, setErrors] = useState<FormErrors>({});

  const tipoFiltro: TipoAtendimentoAgenda | undefined = tipo ? paraTipoAgenda(tipo) : undefined;
  const filtroVagas = useMemo(
    () => ({
      profissional: profissionalId || undefined,
      estabelecimento: estabelecimentoId || undefined,
      de: de || undefined,
      ate: ate || undefined,
      tipo: tipoFiltro,
    }),
    [profissionalId, estabelecimentoId, de, ate, tipoFiltro],
  );
  const vagas = useVagasLivres(filtroVagas, open);
  const itens = vagas.data ?? [];

  function limpar(): void {
    setPacienteId('');
    setProfissionalId('');
    setEstabelecimentoId('');
    setTipo('');
    setDe('');
    setAte('');
    setVagaId('');
    setPrioridade('1');
    setErrors({});
  }

  function fechar(): void {
    limpar();
    onClose();
  }

  function selecionarVaga(vaga: VagaLivreItem): void {
    setVagaId(vaga.vagaId);
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const next: FormErrors = {};
    if (pacienteId.trim() === '') next.pacienteId = 'Selecione o paciente.';
    if (vagaId.trim() === '') next.vagaId = 'Selecione uma vaga disponível.';
    setErrors(next);
    if (Object.keys(next).length > 0) return;

    const input: MarcarAgendamentoInput = {
      pacienteId,
      vagaId,
      prioridade: paraPrioridadeAgendamento(prioridade),
    };

    mutation.mutate(input, {
      onSuccess: () => {
        toast.success('Consulta marcada com sucesso.', 'Sucesso');
        fechar();
      },
      onError: (error) => {
        toast.error(
          error instanceof ApiError ? error.userMessage : 'Não foi possível marcar a consulta.',
        );
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Marcar consulta / exame"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button
            variant="primary"
            type="submit"
            form="form-marcar-agendamento"
            loading={mutation.isPending}
          >
            Marcar
          </Button>
        </>
      }
    >
      <form id="form-marcar-agendamento" className="br-form" onSubmit={submeter} noValidate>
        <PacientePicker
          label="Paciente"
          value={pacienteId}
          onChange={setPacienteId}
          error={errors.pacienteId}
        />

        <fieldset className="mt-3">
          <legend className="text-up-01 text-secondary mb-2">Disponibilidade (vagas livres)</legend>
          <ProfissionalPicker
            label="Profissional (opcional)"
            value={profissionalId}
            onChange={setProfissionalId}
          />
          <FormField label="Estabelecimento (CNES) — opcional">
            {({ id, describedBy }) => (
              <Input
                id={id}
                aria-describedby={describedBy}
                value={estabelecimentoId}
                onChange={(e) => setEstabelecimentoId(e.target.value)}
                placeholder="Identificador do estabelecimento"
              />
            )}
          </FormField>
          <div className="row">
            <div className="col-12 col-md-4">
              <FormField label="Natureza">
                {({ id }) => (
                  <Select
                    id={id}
                    options={opcoesTipoAgenda}
                    placeholder="Todas"
                    value={tipo}
                    onChange={(e) => setTipo(e.target.value)}
                  />
                )}
              </FormField>
            </div>
            <div className="col-6 col-md-4">
              <FormField label="De">
                {({ id }) => (
                  <Input id={id} type="date" value={de} onChange={(e) => setDe(e.target.value)} />
                )}
              </FormField>
            </div>
            <div className="col-6 col-md-4">
              <FormField label="Até">
                {({ id }) => (
                  <Input id={id} type="date" value={ate} onChange={(e) => setAte(e.target.value)} />
                )}
              </FormField>
            </div>
          </div>
        </fieldset>

        <FormField label="Vaga selecionada" required error={errors.vagaId}>
          {({ describedBy }) => (
            <div aria-describedby={describedBy}>
              {vagas.isLoading ? (
                <span className="d-inline-flex align-items-center">
                  <Spinner /> <span className="ml-2">Buscando vagas…</span>
                </span>
              ) : itens.length === 0 ? (
                <EmptyState
                  icon="fas fa-calendar-xmark"
                  title="Sem vagas livres"
                  description="Ajuste os filtros de disponibilidade ou inclua o paciente na fila de espera."
                />
              ) : (
                <div
                  className="br-list"
                  role="radiogroup"
                  aria-label="Vagas disponíveis"
                  style={{ maxHeight: '14rem', overflowY: 'auto' }}
                >
                  {itens.map((vaga) => (
                    <button
                      key={vaga.vagaId}
                      type="button"
                      role="radio"
                      aria-checked={vagaId === vaga.vagaId}
                      className={`br-item d-flex align-items-center justify-content-between w-100 text-left ${
                        vagaId === vaga.vagaId ? 'selected' : ''
                      }`}
                      onClick={() => selecionarVaga(vaga)}
                    >
                      <span>{formatarDataHora(vaga.dataHora)}</span>
                      <Tag variant="info">{vaga.tipo}</Tag>
                    </button>
                  ))}
                </div>
              )}
            </div>
          )}
        </FormField>

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
      </form>
    </Modal>
  );
}
