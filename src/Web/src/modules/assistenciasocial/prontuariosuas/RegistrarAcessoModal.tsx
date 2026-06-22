// Modal de acao: RegistrarAcessoProntuario -> RegistrarAcessoProntuarioCommand.
// Registro manual (append-only) na trilha imutavel de acessos (quem/quando/por que — I-7).
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Alert, Button, FormField, Input, Modal, Textarea, useToast } from '../../../components/ui';
import { ApiError } from '../../../api/problemDetails';
import { useRegistrarAcesso } from './prontuariosuas.api';
import type { RegistrarAcessoInput } from './prontuariosuas.api';
import { mapearErros } from './acaoModalShared';
import type { ModalAcaoBaseProps } from './acaoModalShared';

interface AcessoErrors {
  usuarioId?: string;
  motivoAcesso?: string;
}

export function RegistrarAcessoModal({ open, onClose, prontuarioId }: ModalAcaoBaseProps) {
  const toast = useToast();
  const mutation = useRegistrarAcesso(prontuarioId);

  const [usuarioId, setUsuarioId] = useState('');
  const [motivoAcesso, setMotivoAcesso] = useState('');
  const [errors, setErrors] = useState<AcessoErrors>({});

  function validar(): AcessoErrors {
    const next: AcessoErrors = {};
    if (usuarioId.trim() === '') next.usuarioId = 'Usuário do acesso é obrigatório.';
    if (motivoAcesso.trim() === '') next.motivoAcesso = 'Motivo de acesso ao prontuário é obrigatório.';
    else if (motivoAcesso.length > 400) next.motivoAcesso = 'O motivo deve ter no máximo 400 caracteres.';
    return next;
  }

  function fechar(): void {
    setErrors({});
    setUsuarioId('');
    setMotivoAcesso('');
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const validacao = validar();
    setErrors(validacao);
    if (Object.keys(validacao).length > 0) return;

    const input: RegistrarAcessoInput = {
      usuarioId: usuarioId.trim(),
      motivoAcesso: motivoAcesso.trim(),
    };

    mutation.mutate(input, {
      onSuccess: () => {
        toast.success('Acesso registrado na trilha.', 'Sucesso');
        fechar();
      },
      onError: (error) => {
        setErrors(mapearErros(error, { usuarioId: 1, motivoAcesso: 1 }));
        toast.error(
          error instanceof ApiError ? error.userMessage : 'Não foi possível registrar o acesso.',
        );
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Registrar acesso na trilha"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-registrar-acesso" loading={mutation.isPending}>
            Registrar acesso
          </Button>
        </>
      }
    >
      <div className="mb-3">
        <Alert variant="info">
          A trilha é <strong>imutável</strong> (append-only): registra quem leu, quando e por quê,
          exigível pelo controle social/Tribunal de Contas.
        </Alert>
      </div>
      <form id="form-registrar-acesso" className="br-form" onSubmit={submeter} noValidate>
        <FormField label="Usuário do acesso" required error={errors.usuarioId}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={usuarioId}
              onChange={(e) => setUsuarioId(e.target.value)}
              placeholder="00000000-0000-0000-0000-000000000000"
            />
          )}
        </FormField>

        <FormField
          label="Motivo do acesso"
          required
          error={errors.motivoAcesso}
          help="Máximo de 400 caracteres. Justificativa obrigatória da leitura sigilosa."
        >
          {({ id, describedBy, invalid }) => (
            <Textarea
              id={id}
              rows={3}
              maxLength={400}
              aria-describedby={describedBy}
              aria-invalid={invalid || undefined}
              value={motivoAcesso}
              onChange={(e) => setMotivoAcesso(e.target.value)}
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}
