// PublicarEditalNoPncp (Aberta) — grava NumeroEditalPncp (I-13).
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Button, FormField, Input, Modal, useToast } from '../../../../components/ui';
import { ApiError } from '../../../../api/problemDetails';
import { usePublicarEditalPncp } from '../licitacao.api';
import { primeiraMensagem } from './licitacaoModais.shared';
import type { AcaoModalProps } from './licitacaoModais.shared';

export function PublicarEditalPncpModal({ open, onClose, licitacaoId }: AcaoModalProps) {
  const toast = useToast();
  const mutation = usePublicarEditalPncp(licitacaoId);
  const [numero, setNumero] = useState('');
  const [erro, setErro] = useState<string | undefined>();

  function fechar(): void {
    setNumero('');
    setErro(undefined);
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    if (numero.trim() === '') {
      setErro('Número do edital no PNCP é obrigatório.');
      return;
    }
    if (numero.trim().length > 60) {
      setErro('Número do edital deve ter no máximo 60 caracteres.');
      return;
    }
    setErro(undefined);
    mutation.mutate(
      { numeroEditalPncp: numero.trim() },
      {
        onSuccess: () => {
          toast.success('Edital publicado no PNCP.', 'Sucesso');
          fechar();
        },
        onError: (error) => {
          if (error instanceof ApiError) setErro(primeiraMensagem(error, 'numeroEditalPncp'));
          toast.error(error instanceof ApiError ? error.userMessage : 'Não foi possível publicar no PNCP.');
        },
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Publicar edital no PNCP"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-pncp" loading={mutation.isPending}>
            Publicar
          </Button>
        </>
      }
    >
      <form id="form-pncp" className="br-form" onSubmit={submeter} noValidate>
        <p className="text-down-01 text-gray-60">
          A publicidade no Portal Nacional de Contratações Públicas é condição de regularidade do certame (art. 174 da
          NLLC).
        </p>
        <FormField label="Número do edital no PNCP" required error={erro}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              maxLength={60}
              value={numero}
              onChange={(e) => setNumero(e.target.value)}
              placeholder="PNCP-2026-0001"
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}
