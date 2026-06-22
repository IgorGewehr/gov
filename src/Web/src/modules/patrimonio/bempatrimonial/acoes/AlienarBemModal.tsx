// AlienarBem — POST /bens/{id}/alienacao (DESTRUTIVO: exige avaliação prévia)
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Alert, Button, FormField, Input, Modal, Select, useToast } from '../../../../components/ui';
import { ApiError } from '../../../../api/problemDetails';
import { useAlienarBem } from '../bempatrimonial.api';
import type { AlienarBemInput } from '../bempatrimonial.api';
import { guidInvalido } from '../bemPatrimonial.helpers';
import { mapearFieldErrors } from './acoesModais.shared';
import type { AcaoModalProps } from './acoesModais.shared';

export function AlienarBemModal({ bemId, open, onClose }: AcaoModalProps) {
  const toast = useToast();
  const mutation = useAlienarBem(bemId);
  const [avaliacaoPreviaId, setAvaliacaoPreviaId] = useState('');
  const [porLeilao, setPorLeilao] = useState('true');
  const [valorAlienacao, setValorAlienacao] = useState('');
  const [errors, setErrors] = useState<{ avaliacaoPreviaId?: string; valorAlienacao?: string }>({});

  function fechar(): void {
    setErrors({});
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const next: typeof errors = {};
    if (guidInvalido(avaliacaoPreviaId)) next.avaliacaoPreviaId = 'Avaliação prévia é obrigatória para alienação.';
    const valor = Number(valorAlienacao);
    if (valorAlienacao.trim() === '' || Number.isNaN(valor) || valor <= 0)
      next.valorAlienacao = 'Informe o valor de alienação (maior que zero).';
    setErrors(next);
    if (Object.keys(next).length > 0) return;

    const input: AlienarBemInput = {
      avaliacaoPreviaId: avaliacaoPreviaId.trim(),
      porLeilao: porLeilao === 'true',
      valorAlienacao: valor,
    };
    mutation.mutate(input, {
      onSuccess: () => {
        toast.success('Bem alienado.', 'Sucesso');
        fechar();
      },
      onError: (error) => {
        const mapped = mapearFieldErrors<typeof errors & Record<string, string>>(error, {
          avaliacaoPreviaId: true,
          valorAlienacao: true,
        });
        if (mapped) setErrors((prev) => ({ ...prev, ...mapped }));
        toast.error(error instanceof ApiError ? error.userMessage : 'Não foi possível alienar o bem.');
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Alienar bem"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="danger" type="submit" form="form-alienar" loading={mutation.isPending}>
            Confirmar alienação
          </Button>
        </>
      }
    >
      <form id="form-alienar" className="br-form" onSubmit={submeter} noValidate>
        <Alert variant="warning" title="Ação irreversível.">
          A alienação é um estado terminal (transferência onerosa de domínio). Em regra por leilão e com
          avaliação prévia registrada (Lei 14.133, art. 31 e 76).
        </Alert>
        <FormField label="Avaliação prévia (identificador)" required error={errors.avaliacaoPreviaId}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={avaliacaoPreviaId}
              onChange={(e) => setAvaliacaoPreviaId(e.target.value)}
              placeholder="00000000-0000-0000-0000-000000000000"
            />
          )}
        </FormField>
        <FormField label="Modalidade">
          {({ id, describedBy }) => (
            <Select
              id={id}
              aria-describedby={describedBy}
              options={[
                { value: 'true', label: 'Leilão' },
                { value: 'false', label: 'Outra modalidade' },
              ]}
              value={porLeilao}
              onChange={(e) => setPorLeilao(e.target.value)}
            />
          )}
        </FormField>
        <FormField label="Valor de alienação (R$)" required error={errors.valorAlienacao}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              type="number"
              min="0"
              step="0.01"
              inputMode="decimal"
              aria-describedby={describedBy}
              invalid={invalid}
              value={valorAlienacao}
              onChange={(e) => setValorAlienacao(e.target.value)}
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}
