// TransferirBem — POST /bens/{id}/transferencia
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Button, FormField, Input, Modal, useToast } from '../../../../components/ui';
import { ApiError } from '../../../../api/problemDetails';
import { useTransferirBem } from '../bempatrimonial.api';
import type { TransferirBemInput } from '../bempatrimonial.api';
import { guidInvalido } from '../bemPatrimonial.helpers';
import { mapearFieldErrors } from './acoesModais.shared';
import type { AcaoModalProps } from './acoesModais.shared';

export function TransferirBemModal({ bemId, open, onClose }: AcaoModalProps) {
  const toast = useToast();
  const mutation = useTransferirBem(bemId);
  const [localizacaoDestino, setLocalizacaoDestino] = useState('');
  const [responsavelDestinoId, setResponsavelDestinoId] = useState('');
  const [errors, setErrors] = useState<{ localizacaoDestino?: string; responsavelDestinoId?: string }>({});

  function fechar(): void {
    setErrors({});
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const next: typeof errors = {};
    if (localizacaoDestino.trim() === '') next.localizacaoDestino = 'Informe a localização de destino.';
    if (guidInvalido(responsavelDestinoId)) next.responsavelDestinoId = 'Informe um identificador de responsável válido.';
    setErrors(next);
    if (Object.keys(next).length > 0) return;

    const input: TransferirBemInput = {
      localizacaoDestino: localizacaoDestino.trim(),
      responsavelDestinoId: responsavelDestinoId.trim(),
    };
    mutation.mutate(input, {
      onSuccess: () => {
        toast.success('Bem transferido.', 'Sucesso');
        fechar();
      },
      onError: (error) => {
        const mapped = mapearFieldErrors<typeof errors & Record<string, string>>(error, {
          localizacaoDestino: true,
          responsavelDestinoId: true,
        });
        if (mapped) setErrors((prev) => ({ ...prev, ...mapped }));
        toast.error(error instanceof ApiError ? error.userMessage : 'Não foi possível transferir o bem.');
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Transferir bem"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-transferir" loading={mutation.isPending}>
            Transferir
          </Button>
        </>
      }
    >
      <form id="form-transferir" className="br-form" onSubmit={submeter} noValidate>
        <p className="text-down-01 text-gray-70">
          Mudança de localização/responsável dentro do ente. A situação do bem permanece inalterada.
        </p>
        <FormField label="Localização de destino" required error={errors.localizacaoDestino}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={localizacaoDestino}
              onChange={(e) => setLocalizacaoDestino(e.target.value)}
              placeholder="Ex.: Secretaria de Saúde — Almoxarifado"
            />
          )}
        </FormField>
        <FormField label="Responsável de destino (identificador)" required error={errors.responsavelDestinoId}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={responsavelDestinoId}
              onChange={(e) => setResponsavelDestinoId(e.target.value)}
              placeholder="00000000-0000-0000-0000-000000000000"
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}
