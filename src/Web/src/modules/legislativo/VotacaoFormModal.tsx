// Formulario de inicio (abertura) de Votacao em Modal. Mutation + validacao por
// campo + Toast. Ao iniciar, navega para o detalhe da votacao criada (placar).
import { useState } from 'react';
import type { FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import { Button, FormField, Input, Modal, Select, useToast } from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import { MAIORIAS_EXIGIDAS, TIPOS_VOTACAO, useIniciarVotacao } from './api';
import type { IniciarVotacaoInput } from './api';

export interface VotacaoFormModalProps {
  open: boolean;
  onClose: () => void;
}

interface FormErrors {
  sessaoId?: string;
  proposicaoId?: string;
  tipo?: string;
  maioriaExigida?: string;
  totalMembros?: string;
  presentes?: string;
  turno?: string;
}

const CAMPOS: ReadonlyArray<keyof FormErrors> = [
  'sessaoId',
  'proposicaoId',
  'tipo',
  'maioriaExigida',
  'totalMembros',
  'presentes',
  'turno',
];

export function VotacaoFormModal({ open, onClose }: VotacaoFormModalProps) {
  const toast = useToast();
  const navigate = useNavigate();
  const mutation = useIniciarVotacao();

  const [sessaoId, setSessaoId] = useState('');
  const [proposicaoId, setProposicaoId] = useState('');
  const [tipo, setTipo] = useState<string>(String(TIPOS_VOTACAO[0].value));
  const [maioriaExigida, setMaioriaExigida] = useState<string>(String(MAIORIAS_EXIGIDAS[0].value));
  const [totalMembros, setTotalMembros] = useState('');
  const [presentes, setPresentes] = useState('');
  const [turno, setTurno] = useState('1');
  const [errors, setErrors] = useState<FormErrors>({});

  function inteiroPositivo(valor: string): boolean {
    const n = Number(valor);
    return valor.trim() !== '' && Number.isInteger(n) && n > 0;
  }

  function validar(): FormErrors {
    const next: FormErrors = {};
    if (sessaoId.trim() === '') next.sessaoId = 'Informe a sessão da votação.';
    if (proposicaoId.trim() === '') next.proposicaoId = 'Informe a proposição votada.';
    if (!inteiroPositivo(totalMembros)) next.totalMembros = 'Informe o total de membros (maior que zero).';
    if (!inteiroPositivo(presentes)) next.presentes = 'Informe o número de presentes (maior que zero).';
    const t = Number(turno);
    if (t !== 1 && t !== 2) next.turno = 'O turno deve ser 1 ou 2.';
    return next;
  }

  function fechar(): void {
    setErrors({});
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const validacao = validar();
    setErrors(validacao);
    if (Object.keys(validacao).length > 0) return;

    const input: IniciarVotacaoInput = {
      sessaoId: sessaoId.trim(),
      proposicaoId: proposicaoId.trim(),
      tipo: Number(tipo),
      maioriaExigida: Number(maioriaExigida),
      totalMembros: Number(totalMembros),
      presentes: Number(presentes),
      turno: Number(turno),
    };

    mutation.mutate(input, {
      onSuccess: (id) => {
        toast.success('Votação iniciada.', 'Sucesso');
        fechar();
        navigate(`/legislativo/votacoes/${id}`);
      },
      onError: (error) => {
        if (error instanceof ApiError && Object.keys(error.fieldErrors).length > 0) {
          const mapped: FormErrors = {};
          for (const [field, messages] of Object.entries(error.fieldErrors)) {
            const key = (field.charAt(0).toLowerCase() + field.slice(1)) as keyof FormErrors;
            if (CAMPOS.includes(key)) mapped[key] = messages[0];
          }
          setErrors(mapped);
        }
        toast.error(error instanceof ApiError ? error.userMessage : 'Não foi possível iniciar a votação.');
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Iniciar votação"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-iniciar-votacao" loading={mutation.isPending}>
            Iniciar
          </Button>
        </>
      }
    >
      <form id="form-iniciar-votacao" className="br-form" onSubmit={submeter} noValidate>
        <FormField label="Sessão" required error={errors.sessaoId}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={sessaoId}
              onChange={(e) => setSessaoId(e.target.value)}
              placeholder="Identificador da sessão"
            />
          )}
        </FormField>

        <FormField label="Proposição" required error={errors.proposicaoId}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={proposicaoId}
              onChange={(e) => setProposicaoId(e.target.value)}
              placeholder="Identificador da proposição"
            />
          )}
        </FormField>

        <FormField label="Modalidade de apuração" required error={errors.tipo}>
          {({ id, describedBy, invalid }) => (
            <Select
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={tipo}
              onChange={(e) => setTipo(e.target.value)}
              options={TIPOS_VOTACAO.map((t) => ({ value: String(t.value), label: t.label }))}
            />
          )}
        </FormField>

        <FormField label="Maioria exigida" required error={errors.maioriaExigida}>
          {({ id, describedBy, invalid }) => (
            <Select
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={maioriaExigida}
              onChange={(e) => setMaioriaExigida(e.target.value)}
              options={MAIORIAS_EXIGIDAS.map((m) => ({ value: String(m.value), label: m.label }))}
            />
          )}
        </FormField>

        <div className="row">
          <div className="col-sm-6">
            <FormField label="Total de membros" required error={errors.totalMembros}>
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  type="number"
                  min="1"
                  step="1"
                  inputMode="numeric"
                  aria-describedby={describedBy}
                  invalid={invalid}
                  value={totalMembros}
                  onChange={(e) => setTotalMembros(e.target.value)}
                />
              )}
            </FormField>
          </div>
          <div className="col-sm-6">
            <FormField label="Presentes" required error={errors.presentes}>
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  type="number"
                  min="1"
                  step="1"
                  inputMode="numeric"
                  aria-describedby={describedBy}
                  invalid={invalid}
                  value={presentes}
                  onChange={(e) => setPresentes(e.target.value)}
                />
              )}
            </FormField>
          </div>
        </div>

        <FormField label="Turno" required error={errors.turno} help="1 ou 2 (a maioria qualificada exige 2 turnos).">
          {({ id, describedBy, invalid }) => (
            <Select
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={turno}
              onChange={(e) => setTurno(e.target.value)}
              options={[
                { value: '1', label: '1º turno' },
                { value: '2', label: '2º turno' },
              ]}
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}
