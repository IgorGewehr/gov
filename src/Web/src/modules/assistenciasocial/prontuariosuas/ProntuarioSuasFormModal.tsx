// Formulário de ABERTURA de Prontuário SUAS em Modal (AbrirProntuarioCommand).
// Padrão-ouro: mutation + validação por campo (FormField/aria-describedby) +
// mapeamento de ProblemDetails.errors por campo + Toast de sucesso/erro.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Button, FormField, Input, Modal, useToast } from '../../../components/ui';
import { ApiError } from '../../../api/problemDetails';
import { useAbrirProntuario } from './prontuariosuas.api';
import type { AbrirProntuarioInput } from './prontuariosuas.api';

export interface ProntuarioSuasFormModalProps {
  open: boolean;
  onClose: () => void;
  /** Pré-preenche a família quando aberto a partir de uma consulta. */
  familiaIdInicial?: string;
  /** Notifica o id do prontuário recém-aberto (para navegação opcional). */
  onAberto?: (prontuarioId: string) => void;
}

interface FormErrors {
  familiaId?: string;
  unidadeAtendimentoId?: string;
}

export function ProntuarioSuasFormModal({
  open,
  onClose,
  familiaIdInicial = '',
  onAberto,
}: ProntuarioSuasFormModalProps) {
  const toast = useToast();
  const mutation = useAbrirProntuario();

  const [familiaId, setFamiliaId] = useState(familiaIdInicial);
  const [unidadeAtendimentoId, setUnidadeAtendimentoId] = useState('');
  const [errors, setErrors] = useState<FormErrors>({});

  function validar(): FormErrors {
    const next: FormErrors = {};
    if (familiaId.trim() === '') next.familiaId = 'Família é obrigatória.';
    if (unidadeAtendimentoId.trim() === '')
      next.unidadeAtendimentoId = 'Unidade de atendimento é obrigatória.';
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

    const input: AbrirProntuarioInput = {
      familiaId: familiaId.trim(),
      unidadeAtendimentoId: unidadeAtendimentoId.trim(),
    };

    mutation.mutate(input, {
      onSuccess: (resultado) => {
        toast.success('Prontuário aberto com sucesso.', 'Sucesso');
        onAberto?.(resultado.id);
        fechar();
      },
      onError: (error) => {
        if (error instanceof ApiError && Object.keys(error.fieldErrors).length > 0) {
          const mapped: FormErrors = {};
          const aceitos: Record<string, number> = { familiaId: 1, unidadeAtendimentoId: 1 };
          for (const [field, messages] of Object.entries(error.fieldErrors)) {
            const key = field.charAt(0).toLowerCase() + field.slice(1);
            if (key in aceitos) (mapped as Record<string, string>)[key] = messages[0];
          }
          setErrors(mapped);
        }
        toast.error(
          error instanceof ApiError ? error.userMessage : 'Não foi possível abrir o prontuário.',
        );
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Abrir prontuário SUAS"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-abrir-prontuario" loading={mutation.isPending}>
            Abrir prontuário
          </Button>
        </>
      }
    >
      <form id="form-abrir-prontuario" className="br-form" onSubmit={submeter} noValidate>
        <FormField
          label="Identificador da família"
          required
          error={errors.familiaId}
          help="Família referenciada no CadÚnico (tenant atual)."
        >
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={familiaId}
              onChange={(e) => setFamiliaId(e.target.value)}
              placeholder="00000000-0000-0000-0000-000000000000"
            />
          )}
        </FormField>

        <FormField
          label="Unidade de atendimento (CRAS/CREAS)"
          required
          error={errors.unidadeAtendimentoId}
          help="Define os serviços permitidos: PAIF só em CRAS; PAEFI só em CREAS."
        >
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={unidadeAtendimentoId}
              onChange={(e) => setUnidadeAtendimentoId(e.target.value)}
              placeholder="00000000-0000-0000-0000-000000000000"
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}
