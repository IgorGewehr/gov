// ConcluirManutencao — encerra uma OS aberta com custo realizado.
// WIRED a useConcluirManutencao.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Button, FormField, Input, Modal, useToast } from '../../../../components/ui';
import { hojeIso } from '../veiculo.helpers';
import { useConcluirManutencao } from '../veiculo.api';
import { aplicarErrosBackend } from './acaoModais.shared';
import type { AcaoModalBaseProps } from './acaoModais.shared';

export function ConcluirManutencaoModal({
  veiculoId,
  open,
  onClose,
}: Pick<AcaoModalBaseProps, 'veiculoId' | 'open' | 'onClose'>) {
  const toast = useToast();
  const mutation = useConcluirManutencao(veiculoId);

  const [ordemServicoId, setOrdemServicoId] = useState('');
  const [custoRealizado, setCustoRealizado] = useState('');
  const [dataConclusao, setDataConclusao] = useState(hojeIso());
  const [erros, setErros] = useState<Record<string, string>>({});

  function fechar(): void {
    setErros({});
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const next: Record<string, string> = {};
    if (ordemServicoId.trim() === '') next.ordemServicoId = 'Informe a OS a concluir.';
    if (custoRealizado.trim() === '' || Number(custoRealizado) < 0) next.custoRealizado = 'Custo realizado inválido.';
    if (dataConclusao === '') next.dataConclusao = 'Informe a data de conclusão.';
    setErros(next);
    if (Object.keys(next).length > 0) return;

    mutation.mutate(
      { ordemServicoId: ordemServicoId.trim(), custoRealizado: Number(custoRealizado), dataConclusao },
      {
        onSuccess: () => {
          toast.success('Manutenção concluída.', 'Sucesso');
          fechar();
        },
        onError: (error) => toast.error(aplicarErrosBackend(error, ['ordemServicoId', 'custoRealizado', 'dataConclusao'], setErros)),
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Concluir manutenção"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-concluir-os" loading={mutation.isPending}>
            Concluir
          </Button>
        </>
      }
    >
      <form id="form-concluir-os" className="br-form" onSubmit={submeter} noValidate>
        <FormField label="Ordem de serviço (id)" required error={erros.ordemServicoId} help="Apenas OS em situação Aberta podem ser concluídas.">
          {({ id, describedBy, invalid }) => (
            <Input id={id} aria-describedby={describedBy} invalid={invalid} value={ordemServicoId} onChange={(e) => setOrdemServicoId(e.target.value)} placeholder="00000000-0000-0000-0000-000000000000" />
          )}
        </FormField>
        <div className="row">
          <div className="col-sm-6">
            <FormField label="Custo realizado (R$)" required error={erros.custoRealizado}>
              {({ id, describedBy, invalid }) => (
                <Input id={id} type="number" min="0" step="0.01" inputMode="decimal" aria-describedby={describedBy} invalid={invalid} value={custoRealizado} onChange={(e) => setCustoRealizado(e.target.value)} />
              )}
            </FormField>
          </div>
          <div className="col-sm-6">
            <FormField label="Data de conclusão" required error={erros.dataConclusao}>
              {({ id, describedBy, invalid }) => (
                <Input id={id} type="date" aria-describedby={describedBy} invalid={invalid} value={dataConclusao} onChange={(e) => setDataConclusao(e.target.value)} />
              )}
            </FormField>
          </div>
        </div>
      </form>
    </Modal>
  );
}
