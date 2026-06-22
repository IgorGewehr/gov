// Formulario de inscricao de um orador na Tribuna: vereador + tempo concedido
// (minutos, convertido em segundos no payload). Mutation + validacao + Toast.
import { useEffect, useState } from 'react';
import type { FormEvent } from 'react';
import { Button, FormField, Input, Modal, Select, useToast } from '../../components/ui';
import { FASES_USO_PALAVRA } from './legislativo.shared';
import { tratarErroCampos, mensagemErro } from './legislativoAcao.shared';
import { useVereadores } from './vereadores.api';
import { useInscreverOrador, type InscricaoInput } from './tribuna.api';

export interface TribunaInscricaoModalProps {
  open: boolean;
  onClose: () => void;
  /** Identificador da sessao. */
  sessaoId: string;
  /** Identificador da tribuna (obrigatorio no payload do backend). */
  tribunaId: string;
}

interface FormErrors {
  vereadorId?: string;
  tempoConcedidoSegundos?: string;
}

const CAMPOS: Record<string, number> = { vereadorId: 1, tempoConcedidoSegundos: 1 };

export function TribunaInscricaoModal({ open, onClose, sessaoId, tribunaId }: TribunaInscricaoModalProps) {
  const toast = useToast();
  const vereadores = useVereadores();
  const inscrever = useInscreverOrador(sessaoId);

  const [vereadorId, setVereadorId] = useState('');
  const [fase, setFase] = useState<string>(String(FASES_USO_PALAVRA[0].value));
  const [minutos, setMinutos] = useState('5');
  const [errors, setErrors] = useState<FormErrors>({});

  useEffect(() => {
    if (!open) return;
    setVereadorId('');
    setFase(String(FASES_USO_PALAVRA[0].value));
    setMinutos('5');
    setErrors({});
  }, [open]);

  function validar(): FormErrors {
    const next: FormErrors = {};
    if (vereadorId === '') next.vereadorId = 'Selecione o orador.';
    if (minutos.trim() === '' || Number(minutos) <= 0)
      next.tempoConcedidoSegundos = 'Informe um tempo válido (minutos).';
    return next;
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const validacao = validar();
    setErrors(validacao);
    if (Object.keys(validacao).length > 0) return;

    const input: InscricaoInput = {
      tribunaId,
      vereadorId,
      fase: Number(fase),
      tempoConcedidoSegundos: Math.round(Number(minutos) * 60),
    };

    inscrever.mutate(input, {
      onSuccess: () => {
        toast.success('Orador inscrito.', 'Sucesso');
        onClose();
      },
      onError: (error) => {
        setErrors(tratarErroCampos(error, CAMPOS));
        toast.error(mensagemErro(error, 'Não foi possível inscrever o orador.'));
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={onClose}
      title="Inscrever orador"
      size="small"
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={inscrever.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-inscricao" loading={inscrever.isPending}>
            Inscrever
          </Button>
        </>
      }
    >
      <form id="form-inscricao" className="br-form" onSubmit={submeter} noValidate>
        <FormField label="Orador" required error={errors.vereadorId}>
          {({ id, describedBy, invalid }) => (
            <Select
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={vereadorId}
              onChange={(e) => setVereadorId(e.target.value)}
              placeholder="Selecione o vereador"
              options={(vereadores.data ?? []).map((v) => ({
                value: v.id,
                label: `${v.nomeParlamentar} (${v.partido})`,
              }))}
            />
          )}
        </FormField>

        <FormField label="Fase de uso da palavra" required>
          {({ id, describedBy, invalid }) => (
            <Select
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={fase}
              onChange={(e) => setFase(e.target.value)}
              options={FASES_USO_PALAVRA.map((f) => ({ value: String(f.value), label: f.label }))}
            />
          )}
        </FormField>

        <FormField label="Tempo concedido (minutos)" required error={errors.tempoConcedidoSegundos}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              type="number"
              min={1}
              value={minutos}
              onChange={(e) => setMinutos(e.target.value)}
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}
