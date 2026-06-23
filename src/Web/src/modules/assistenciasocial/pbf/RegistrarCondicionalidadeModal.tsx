// Acao "Registrar condicionalidade" (educacao/saude) de um membro num acompanhamento
// (command RegistrarCondicionalidade) — recalcula o efeito gradativo no backend.
import { useState } from 'react';
import type { FormEvent } from 'react';
import {
  Alert,
  Button,
  FormField,
  Input,
  Modal,
  Select,
  Textarea,
  useToast,
} from '../../../components/ui';
import { ApiError } from '../../../api/problemDetails';
import { useRegistrarCondicionalidade } from './pbf.api';
import type {
  AcompanhamentoResultado,
  StatusCondicionalidade,
  TipoCondicionalidade,
} from './pbf.api';
import { STATUS_OPTIONS, TIPO_CONDICIONALIDADE_OPTIONS } from './pbf.helpers';

export interface RegistrarCondicionalidadeModalProps {
  open: boolean;
  onClose: () => void;
  /** Acompanhamento alvo (define a competencia/familia). */
  acompanhamento: AcompanhamentoResultado;
}

export function RegistrarCondicionalidadeModal({
  open,
  onClose,
  acompanhamento,
}: RegistrarCondicionalidadeModalProps) {
  const toast = useToast();
  const mutation = useRegistrarCondicionalidade();

  const [tipo, setTipo] = useState<TipoCondicionalidade>('EducacaoFrequenciaEscolar');
  const [membroId, setMembroId] = useState('');
  const [status, setStatus] = useState<StatusCondicionalidade>('Pendente');
  const [observacao, setObservacao] = useState('');
  const [erroMembro, setErroMembro] = useState<string | undefined>();

  function fechar(): void {
    setTipo('EducacaoFrequenciaEscolar');
    setMembroId('');
    setStatus('Pendente');
    setObservacao('');
    setErroMembro(undefined);
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    if (membroId.trim() === '') {
      setErroMembro('Informe o identificador do membro.');
      return;
    }
    setErroMembro(undefined);

    mutation.mutate(
      {
        acompanhamentoId: acompanhamento.acompanhamentoId,
        tipo,
        membroId: membroId.trim(),
        status,
        observacao: observacao.trim() === '' ? null : observacao.trim(),
      },
      {
        onSuccess: () => {
          toast.success('Condicionalidade registrada.', 'Sucesso');
          fechar();
        },
        onError: (error) => {
          if (error instanceof ApiError && error.fieldErrors.MembroId) {
            setErroMembro(error.fieldErrors.MembroId[0]);
          }
          toast.error(
            error instanceof ApiError
              ? error.userMessage
              : 'Não foi possível registrar a condicionalidade.',
          );
        },
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Registrar condicionalidade"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-registrar-condicionalidade" loading={mutation.isPending}>
            Registrar
          </Button>
        </>
      }
    >
      <form id="form-registrar-condicionalidade" className="br-form" onSubmit={submeter} noValidate>
        <Alert variant="info">
          Acompanhamento da competência <strong>{acompanhamento.competencia}</strong>. Registrar um
          descumprimento recalcula o efeito gradativo (advertência/bloqueio/suspensão).
        </Alert>

        <FormField label="Eixo da condicionalidade" required>
          {({ id, describedBy, invalid }) => (
            <Select
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              options={TIPO_CONDICIONALIDADE_OPTIONS}
              value={tipo}
              onChange={(e) => setTipo(e.target.value as TipoCondicionalidade)}
            />
          )}
        </FormField>

        <FormField
          label="Identificador do membro"
          required
          error={erroMembro}
          help="Membro da família ao qual a condicionalidade se aplica."
        >
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={membroId}
              onChange={(e) => setMembroId(e.target.value)}
              placeholder="00000000-0000-0000-0000-000000000000"
            />
          )}
        </FormField>

        <FormField label="Status" required>
          {({ id, describedBy, invalid }) => (
            <Select
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              options={STATUS_OPTIONS}
              value={status}
              onChange={(e) => setStatus(e.target.value as StatusCondicionalidade)}
            />
          )}
        </FormField>

        <FormField label="Observação" help="Opcional. Contexto do registro (não substitui a justificativa formal).">
          {({ id, describedBy }) => (
            <Textarea
              id={id}
              aria-describedby={describedBy}
              rows={3}
              value={observacao}
              onChange={(e) => setObservacao(e.target.value)}
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}
