// RegistrarImpairment — POST /bens/{id}/impairment
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Button, FormField, Input, Modal, useToast } from '../../../../components/ui';
import { ApiError } from '../../../../api/problemDetails';
import { useRegistrarImpairment } from '../bempatrimonial.api';
import type { RegistrarImpairmentInput } from '../bempatrimonial.api';
import { hoje, mapearFieldErrors } from './acoesModais.shared';
import type { AcaoModalProps } from './acoesModais.shared';

export function RegistrarImpairmentModal({ bemId, open, onClose }: AcaoModalProps) {
  const toast = useToast();
  const mutation = useRegistrarImpairment(bemId);
  const [valorRecuperavel, setValorRecuperavel] = useState('');
  const [laudoUri, setLaudoUri] = useState('');
  const [dataTeste, setDataTeste] = useState(hoje());
  const [errors, setErrors] = useState<{ valorRecuperavel?: string; laudoUri?: string; dataTeste?: string }>({});

  function fechar(): void {
    setErrors({});
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const next: typeof errors = {};
    const valor = Number(valorRecuperavel);
    if (valorRecuperavel.trim() === '' || Number.isNaN(valor) || valor < 0)
      next.valorRecuperavel = 'Informe o valor recuperável (≥ 0).';
    if (laudoUri.trim() === '') next.laudoUri = 'Laudo/teste de recuperabilidade é obrigatório.';
    if (dataTeste.trim() === '') next.dataTeste = 'Informe a data do teste.';
    setErrors(next);
    if (Object.keys(next).length > 0) return;

    const input: RegistrarImpairmentInput = {
      valorRecuperavel: valor,
      laudoUri: laudoUri.trim(),
      dataTeste,
    };
    mutation.mutate(input, {
      onSuccess: () => {
        toast.success('Perda por impairment reconhecida.', 'Sucesso');
        fechar();
      },
      onError: (error) => {
        const mapped = mapearFieldErrors<typeof errors & Record<string, string>>(error, {
          valorRecuperavel: true,
          laudoUri: true,
          dataTeste: true,
        });
        if (mapped) setErrors((prev) => ({ ...prev, ...mapped }));
        toast.error(error instanceof ApiError ? error.userMessage : 'Não foi possível registrar o impairment.');
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Registrar impairment"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-impairment" loading={mutation.isPending}>
            Registrar
          </Button>
        </>
      }
    >
      <form id="form-impairment" className="br-form" onSubmit={submeter} noValidate>
        <p className="text-down-01 text-gray-70">
          A perda é reconhecida quando o valor recuperável é inferior ao valor contábil. Se for
          maior ou igual, nada é reconhecido. Laudo/teste obrigatório.
        </p>
        <FormField label="Valor recuperável (R$)" required error={errors.valorRecuperavel}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              type="number"
              min="0"
              step="0.01"
              inputMode="decimal"
              aria-describedby={describedBy}
              invalid={invalid}
              value={valorRecuperavel}
              onChange={(e) => setValorRecuperavel(e.target.value)}
            />
          )}
        </FormField>
        <FormField label="Laudo / teste de recuperabilidade (URI)" required error={errors.laudoUri}>
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
        <FormField label="Data do teste" required error={errors.dataTeste}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              type="date"
              aria-describedby={describedBy}
              invalid={invalid}
              value={dataTeste}
              onChange={(e) => setDataTeste(e.target.value)}
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}
