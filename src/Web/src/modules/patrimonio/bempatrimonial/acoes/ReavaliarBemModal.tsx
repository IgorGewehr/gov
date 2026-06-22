// ReavaliarBem — POST /bens/{id}/reavaliacao
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Button, FormField, Input, Modal, useToast } from '../../../../components/ui';
import { ApiError } from '../../../../api/problemDetails';
import { useReavaliarBem } from '../bempatrimonial.api';
import type { ReavaliarBemInput } from '../bempatrimonial.api';
import { hoje, mapearFieldErrors } from './acoesModais.shared';
import type { AcaoModalProps } from './acoesModais.shared';

export function ReavaliarBemModal({ bemId, open, onClose }: AcaoModalProps) {
  const toast = useToast();
  const mutation = useReavaliarBem(bemId);
  const [novoValorJusto, setNovoValorJusto] = useState('');
  const [laudoUri, setLaudoUri] = useState('');
  const [dataReavaliacao, setDataReavaliacao] = useState(hoje());
  const [errors, setErrors] = useState<{ novoValorJusto?: string; laudoUri?: string; dataReavaliacao?: string }>({});

  function fechar(): void {
    setErrors({});
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const next: typeof errors = {};
    const valor = Number(novoValorJusto);
    if (novoValorJusto.trim() === '' || Number.isNaN(valor) || valor <= 0)
      next.novoValorJusto = 'Informe o novo valor justo (maior que zero).';
    if (laudoUri.trim() === '') next.laudoUri = 'Laudo é obrigatório para reavaliação.';
    if (dataReavaliacao.trim() === '') next.dataReavaliacao = 'Informe a data da reavaliação.';
    setErrors(next);
    if (Object.keys(next).length > 0) return;

    const input: ReavaliarBemInput = {
      novoValorJusto: valor,
      laudoUri: laudoUri.trim(),
      dataReavaliacao,
    };
    mutation.mutate(input, {
      onSuccess: () => {
        toast.success('Bem reavaliado a valor justo.', 'Sucesso');
        fechar();
      },
      onError: (error) => {
        const mapped = mapearFieldErrors<typeof errors & Record<string, string>>(error, {
          novoValorJusto: true,
          laudoUri: true,
          dataReavaliacao: true,
        });
        if (mapped) setErrors((prev) => ({ ...prev, ...mapped }));
        toast.error(error instanceof ApiError ? error.userMessage : 'Não foi possível reavaliar o bem.');
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Reavaliar a valor justo"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-reavaliar" loading={mutation.isPending}>
            Reavaliar
          </Button>
        </>
      }
    >
      <form id="form-reavaliar" className="br-form" onSubmit={submeter} noValidate>
        <p className="text-down-01 text-gray-70">
          Ajusta o valor contábil ao valor justo informado. Admitido apenas para bem ativo no acervo
          (Tombado ou Cedido). Laudo obrigatório.
        </p>
        <FormField label="Novo valor justo (R$)" required error={errors.novoValorJusto}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              type="number"
              min="0"
              step="0.01"
              inputMode="decimal"
              aria-describedby={describedBy}
              invalid={invalid}
              value={novoValorJusto}
              onChange={(e) => setNovoValorJusto(e.target.value)}
            />
          )}
        </FormField>
        <FormField label="Laudo (URI/identificador)" required error={errors.laudoUri}>
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
        <FormField label="Data da reavaliação" required error={errors.dataReavaliacao}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              type="date"
              aria-describedby={describedBy}
              invalid={invalid}
              value={dataReavaliacao}
              onChange={(e) => setDataReavaliacao(e.target.value)}
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}
