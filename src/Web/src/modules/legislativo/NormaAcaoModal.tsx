// Modal de acao de Norma (revogacao/alteracao). Captura a norma afetada (a que
// esta norma revoga/altera) e a justificativa. Mutation + validacao + Toast.
import { useEffect, useState } from 'react';
import type { FormEvent } from 'react';
import { Button, FormField, Input, Modal, Textarea, useToast } from '../../components/ui';
import { tratarErroCampos, mensagemErro } from './legislativoAcao.shared';
import { useAlterarNorma, useRevogarNorma, type NormaAcaoInput } from './normas.api';

export interface NormaAcaoModalProps {
  open: boolean;
  onClose: () => void;
  /** Identificador da norma que revoga/altera. */
  id: string;
  tipo: 'revogar' | 'alterar';
}

interface FormErrors {
  normaAfetadaId?: string;
  justificativa?: string;
}

const CAMPOS: Record<string, number> = { normaAfetadaId: 1, justificativa: 1 };

const TEXTOS = {
  revogar: {
    title: 'Revogar norma',
    rotulo: 'Revogar',
    sucesso: 'Revogação registrada.',
    erro: 'Não foi possível registrar a revogação.',
  },
  alterar: {
    title: 'Registrar alteração',
    rotulo: 'Registrar',
    sucesso: 'Alteração registrada.',
    erro: 'Não foi possível registrar a alteração.',
  },
} as const;

export function NormaAcaoModal({ open, onClose, id, tipo }: NormaAcaoModalProps) {
  const toast = useToast();
  const revogar = useRevogarNorma(id);
  const alterar = useAlterarNorma(id);
  const mutation = tipo === 'revogar' ? revogar : alterar;
  const textos = TEXTOS[tipo];

  const [normaAfetadaId, setNormaAfetadaId] = useState('');
  const [justificativa, setJustificativa] = useState('');
  const [errors, setErrors] = useState<FormErrors>({});

  useEffect(() => {
    if (!open) return;
    setNormaAfetadaId('');
    setJustificativa('');
    setErrors({});
  }, [open]);

  function validar(): FormErrors {
    const next: FormErrors = {};
    if (normaAfetadaId.trim() === '') next.normaAfetadaId = 'Informe a norma afetada.';
    if (justificativa.trim() === '') next.justificativa = 'Informe a justificativa.';
    return next;
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const validacao = validar();
    setErrors(validacao);
    if (Object.keys(validacao).length > 0) return;

    const input: NormaAcaoInput = {
      normaAfetadaId: normaAfetadaId.trim(),
      justificativa: justificativa.trim(),
    };

    mutation.mutate(input, {
      onSuccess: () => {
        toast.success(textos.sucesso, 'Sucesso');
        onClose();
      },
      onError: (error) => {
        setErrors(tratarErroCampos(error, CAMPOS));
        toast.error(mensagemErro(error, textos.erro));
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={onClose}
      title={textos.title}
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-norma-acao" loading={mutation.isPending}>
            {textos.rotulo}
          </Button>
        </>
      }
    >
      <form id="form-norma-acao" className="br-form" onSubmit={submeter} noValidate>
        <FormField
          label="Norma afetada"
          required
          error={errors.normaAfetadaId}
          help="Identificador da norma que será revogada/alterada por esta."
        >
          {({ id: fieldId, describedBy, invalid }) => (
            <Input
              id={fieldId}
              aria-describedby={describedBy}
              invalid={invalid}
              value={normaAfetadaId}
              onChange={(e) => setNormaAfetadaId(e.target.value)}
              placeholder="00000000-0000-0000-0000-000000000000"
            />
          )}
        </FormField>

        <FormField label="Justificativa" required error={errors.justificativa}>
          {({ id: fieldId, describedBy, invalid }) => (
            <Textarea
              id={fieldId}
              aria-describedby={describedBy}
              invalid={invalid}
              value={justificativa}
              onChange={(e) => setJustificativa(e.target.value)}
              rows={4}
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}
