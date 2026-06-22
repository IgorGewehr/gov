// HabilitarLicitante (Aberta | EmJulgamento) — registra habilitação (I-7).
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Button, FormField, Input, Modal, Select, Textarea, useToast } from '../../../../components/ui';
import type { SelectOption } from '../../../../components/ui';
import { ApiError } from '../../../../api/problemDetails';
import { useHabilitarLicitante } from '../licitacao.api';
import type { ResultadoHabilitacao } from '../licitacao.api';
import { RESULTADO_HABILITACAO_OPTIONS } from '../licitacao.helpers';
import { GUID_REGEX, primeiraMensagem } from './licitacaoModais.shared';
import type { AcaoModalProps } from './licitacaoModais.shared';

export function HabilitarLicitanteModal({
  open,
  onClose,
  licitacaoId,
  fornecedores,
}: AcaoModalProps & { fornecedores: string[] }) {
  const toast = useToast();
  const mutation = useHabilitarLicitante(licitacaoId);
  const [fornecedorId, setFornecedorId] = useState('');
  const [resultado, setResultado] = useState<ResultadoHabilitacao | ''>('');
  const [motivo, setMotivo] = useState('');
  const [errors, setErrors] = useState<{ fornecedorId?: string; resultado?: string }>({});

  const fornecedorOptions: SelectOption[] = fornecedores.map((f) => ({
    value: f,
    label: `Fornecedor ${f.slice(0, 8)}…`,
  }));

  function fechar(): void {
    setFornecedorId('');
    setResultado('');
    setMotivo('');
    setErrors({});
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const next: { fornecedorId?: string; resultado?: string } = {};
    if (fornecedorId.trim() === '' || !GUID_REGEX.test(fornecedorId.trim()))
      next.fornecedorId = 'Informe um fornecedor válido.';
    if (resultado === '') next.resultado = 'Selecione o resultado da habilitação.';
    setErrors(next);
    if (Object.keys(next).length > 0) return;

    mutation.mutate(
      { fornecedorId: fornecedorId.trim(), resultado: resultado as ResultadoHabilitacao, motivo: motivo.trim() || null },
      {
        onSuccess: () => {
          toast.success('Habilitação registrada.', 'Sucesso');
          fechar();
        },
        onError: (error) => {
          if (error instanceof ApiError) {
            setErrors({
              fornecedorId: primeiraMensagem(error, 'fornecedorId'),
              resultado: primeiraMensagem(error, 'resultado'),
            });
          }
          toast.error(error instanceof ApiError ? error.userMessage : 'Não foi possível registrar a habilitação.');
        },
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Habilitar licitante"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-habilitar" loading={mutation.isPending}>
            Registrar habilitação
          </Button>
        </>
      }
    >
      <form id="form-habilitar" className="br-form" onSubmit={submeter} noValidate>
        <p className="text-down-01 text-gray-60">
          Fornecedor com sanção de impedimento/inidoneidade vigente deve resultar Inabilitado (art. 7º da NLLC).
        </p>
        <FormField
          label="Fornecedor"
          required
          error={errors.fornecedorId}
          help="Selecione um proponente do certame ou informe o identificador."
        >
          {({ id, describedBy, invalid }) =>
            fornecedorOptions.length > 0 ? (
              <Select
                id={id}
                aria-describedby={describedBy}
                invalid={invalid}
                placeholder="Selecione o fornecedor…"
                options={fornecedorOptions}
                value={fornecedorId}
                onChange={(e) => setFornecedorId(e.target.value)}
              />
            ) : (
              <Input
                id={id}
                aria-describedby={describedBy}
                invalid={invalid}
                value={fornecedorId}
                onChange={(e) => setFornecedorId(e.target.value)}
                placeholder="00000000-0000-0000-0000-000000000000"
              />
            )
          }
        </FormField>

        <FormField label="Resultado" required error={errors.resultado}>
          {({ id, describedBy, invalid }) => (
            <Select
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              placeholder="Selecione…"
              options={RESULTADO_HABILITACAO_OPTIONS}
              value={resultado}
              onChange={(e) => setResultado(e.target.value as ResultadoHabilitacao)}
            />
          )}
        </FormField>

        <FormField label="Motivo" help="Opcional — obrigatório na prática quando inabilitado.">
          {({ id, describedBy }) => (
            <Textarea
              id={id}
              aria-describedby={describedBy}
              value={motivo}
              onChange={(e) => setMotivo(e.target.value)}
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}
