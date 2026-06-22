// BaixarBem — POST /bens/{id}/baixa (DESTRUTIVO: exige laudo + autorização)
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Alert, Button, FormField, Input, Modal, Select, useToast } from '../../../../components/ui';
import { ApiError } from '../../../../api/problemDetails';
import { useBaixarBem } from '../bempatrimonial.api';
import type { BaixarBemInput } from '../bempatrimonial.api';
import { guidInvalido, MOTIVO_BAIXA_OPCOES } from '../bemPatrimonial.helpers';
import { mapearFieldErrors } from './acoesModais.shared';
import type { AcaoModalProps } from './acoesModais.shared';

export function BaixarBemModal({ bemId, open, onClose }: AcaoModalProps) {
  const toast = useToast();
  const mutation = useBaixarBem(bemId);
  const [motivoBaixa, setMotivoBaixa] = useState('');
  const [laudoUri, setLaudoUri] = useState('');
  const [autorizacaoId, setAutorizacaoId] = useState('');
  const [errors, setErrors] = useState<{ motivoBaixa?: string; laudoUri?: string; autorizacaoId?: string }>({});

  function fechar(): void {
    setErrors({});
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const next: typeof errors = {};
    if (motivoBaixa.trim() === '') next.motivoBaixa = 'Selecione o motivo da baixa.';
    if (laudoUri.trim() === '') next.laudoUri = 'Laudo/parecer é obrigatório para baixa.';
    if (guidInvalido(autorizacaoId)) next.autorizacaoId = 'Autorização é obrigatória para baixa.';
    setErrors(next);
    if (Object.keys(next).length > 0) return;

    const input: BaixarBemInput = {
      motivoBaixa: Number(motivoBaixa),
      laudoUri: laudoUri.trim(),
      autorizacaoId: autorizacaoId.trim(),
    };
    mutation.mutate(input, {
      onSuccess: () => {
        toast.success('Bem baixado do acervo.', 'Sucesso');
        fechar();
      },
      onError: (error) => {
        const mapped = mapearFieldErrors<typeof errors & Record<string, string>>(error, {
          motivoBaixa: true,
          laudoUri: true,
          autorizacaoId: true,
        });
        if (mapped) setErrors((prev) => ({ ...prev, ...mapped }));
        toast.error(error instanceof ApiError ? error.userMessage : 'Não foi possível baixar o bem.');
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Baixar bem do acervo"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="danger" type="submit" form="form-baixar" loading={mutation.isPending}>
            Confirmar baixa
          </Button>
        </>
      }
    >
      <form id="form-baixar" className="br-form" onSubmit={submeter} noValidate>
        <Alert variant="warning" title="Ação irreversível.">
          A baixa é um estado terminal: o bem deixa o acervo e um lançamento contábil é emitido. Exige laudo
          anexado e autorização.
        </Alert>
        <FormField label="Motivo da baixa" required error={errors.motivoBaixa}>
          {({ id, describedBy, invalid }) => (
            <Select
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              placeholder="Selecione…"
              options={MOTIVO_BAIXA_OPCOES}
              value={motivoBaixa}
              onChange={(e) => setMotivoBaixa(e.target.value)}
            />
          )}
        </FormField>
        <FormField label="Laudo / parecer (URI)" required error={errors.laudoUri}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={laudoUri}
              onChange={(e) => setLaudoUri(e.target.value)}
              placeholder="https://… ou nº do parecer"
            />
          )}
        </FormField>
        <FormField label="Autorização (identificador)" required error={errors.autorizacaoId}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={autorizacaoId}
              onChange={(e) => setAutorizacaoId(e.target.value)}
              placeholder="00000000-0000-0000-0000-000000000000"
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}
