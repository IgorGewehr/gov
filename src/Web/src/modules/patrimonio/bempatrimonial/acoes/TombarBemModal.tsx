// TombarBem — POST /bens/{id}/tombamento
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Button, FormField, Input, Modal, useToast } from '../../../../components/ui';
import { ApiError } from '../../../../api/problemDetails';
import { useTombarBem } from '../bempatrimonial.api';
import type { TombarBemInput } from '../bempatrimonial.api';
import { mapearFieldErrors } from './acoesModais.shared';
import type { AcaoModalProps } from './acoesModais.shared';

export function TombarBemModal({ bemId, open, onClose }: AcaoModalProps) {
  const toast = useToast();
  const mutation = useTombarBem(bemId);
  const [numeroTombamento, setNumeroTombamento] = useState('');
  const [erro, setErro] = useState<string | undefined>();

  function fechar(): void {
    setNumeroTombamento('');
    setErro(undefined);
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    if (numeroTombamento.trim() === '') {
      setErro('Informe o número de tombo.');
      return;
    }
    if (numeroTombamento.trim().length > 40) {
      setErro('Máximo de 40 caracteres.');
      return;
    }
    const input: TombarBemInput = { numeroTombamento: numeroTombamento.trim() };
    mutation.mutate(input, {
      onSuccess: () => {
        toast.success('Bem tombado.', 'Sucesso');
        fechar();
      },
      onError: (error) => {
        const mapped = mapearFieldErrors<{ numeroTombamento: string }>(error, { numeroTombamento: true });
        if (mapped?.numeroTombamento) setErro(mapped.numeroTombamento);
        toast.error(error instanceof ApiError ? error.userMessage : 'Não foi possível tombar o bem.');
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Tombar bem"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-tombar" loading={mutation.isPending}>
            Tombar
          </Button>
        </>
      }
    >
      <form id="form-tombar" className="br-form" onSubmit={submeter} noValidate>
        <p className="text-down-01 text-gray-70">
          O tombamento atribui um número de tombo único e move o bem de
          {' '}<strong>Em incorporação</strong> para <strong>Tombado</strong>.
        </p>
        <FormField label="Número de tombo" required error={erro} help="Único por ente. Máximo de 40 caracteres.">
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              maxLength={40}
              value={numeroTombamento}
              onChange={(e) => setNumeroTombamento(e.target.value)}
              placeholder="TOMBO-2026-0001"
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}
