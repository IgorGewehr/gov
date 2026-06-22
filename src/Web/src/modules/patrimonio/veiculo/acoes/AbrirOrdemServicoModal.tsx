// AbrirOrdemServico — abre OS de manutenção. WIRED a useAbrirOrdemServico.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Button, FormField, Input, Modal, useToast } from '../../../../components/ui';
import { useAbrirOrdemServico } from '../veiculo.api';
import { aplicarErrosBackend } from './acaoModais.shared';
import type { AcaoModalBaseProps } from './acaoModais.shared';

export function AbrirOrdemServicoModal({ veiculoId, open, onClose, odometroAtual }: AcaoModalBaseProps) {
  const toast = useToast();
  const mutation = useAbrirOrdemServico(veiculoId);

  const [descricao, setDescricao] = useState('');
  const [custoEstimado, setCustoEstimado] = useState('');
  const [odometro, setOdometro] = useState(String(odometroAtual));
  const [erros, setErros] = useState<Record<string, string>>({});

  function fechar(): void {
    setErros({});
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const next: Record<string, string> = {};
    if (descricao.trim() === '') next.descricao = 'Descreva a manutenção.';
    if (custoEstimado.trim() === '' || Number(custoEstimado) < 0) next.custoEstimado = 'Custo estimado inválido.';
    if (odometro.trim() === '' || Number(odometro) < odometroAtual)
      next.odometro = `Odômetro deve ser ≥ ${odometroAtual} km.`;
    setErros(next);
    if (Object.keys(next).length > 0) return;

    mutation.mutate(
      { descricao: descricao.trim(), custoEstimado: Number(custoEstimado), odometro: Number(odometro) },
      {
        onSuccess: () => {
          toast.success('Ordem de serviço aberta.', 'Sucesso');
          fechar();
        },
        onError: (error) => toast.error(aplicarErrosBackend(error, ['descricao', 'custoEstimado', 'odometro'], setErros)),
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Abrir ordem de serviço"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-os" loading={mutation.isPending}>
            Abrir OS
          </Button>
        </>
      }
    >
      <form id="form-os" className="br-form" onSubmit={submeter} noValidate>
        <FormField label="Descrição da manutenção" required error={erros.descricao}>
          {({ id, describedBy, invalid }) => (
            <Input id={id} aria-describedby={describedBy} invalid={invalid} value={descricao} onChange={(e) => setDescricao(e.target.value)} placeholder="Troca de óleo e filtros" />
          )}
        </FormField>
        <div className="row">
          <div className="col-sm-6">
            <FormField label="Custo estimado (R$)" required error={erros.custoEstimado}>
              {({ id, describedBy, invalid }) => (
                <Input id={id} type="number" min="0" step="0.01" inputMode="decimal" aria-describedby={describedBy} invalid={invalid} value={custoEstimado} onChange={(e) => setCustoEstimado(e.target.value)} />
              )}
            </FormField>
          </div>
          <div className="col-sm-6">
            <FormField label="Odômetro (km)" required error={erros.odometro} help={`Atual: ${odometroAtual} km`}>
              {({ id, describedBy, invalid }) => (
                <Input id={id} type="number" min={odometroAtual} step="1" inputMode="numeric" aria-describedby={describedBy} invalid={invalid} value={odometro} onChange={(e) => setOdometro(e.target.value)} />
              )}
            </FormField>
          </div>
        </div>
      </form>
    </Modal>
  );
}
