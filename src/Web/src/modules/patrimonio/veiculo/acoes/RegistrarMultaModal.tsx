// RegistrarMulta — registra multa de trânsito (CTB). WIRED a useRegistrarMulta.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Button, FormField, Input, Modal, useToast } from '../../../../components/ui';
import { hojeIso } from '../veiculo.helpers';
import { useRegistrarMulta } from '../veiculo.api';
import { aplicarErrosBackend } from './acaoModais.shared';
import type { AcaoModalBaseProps } from './acaoModais.shared';

export function RegistrarMultaModal({
  veiculoId,
  open,
  onClose,
}: Pick<AcaoModalBaseProps, 'veiculoId' | 'open' | 'onClose'>) {
  const toast = useToast();
  const mutation = useRegistrarMulta(veiculoId);

  const [codigoInfracaoCtb, setCodigoInfracaoCtb] = useState('');
  const [valor, setValor] = useState('');
  const [dataInfracao, setDataInfracao] = useState(hojeIso());
  const [motoristaId, setMotoristaId] = useState('');
  const [erros, setErros] = useState<Record<string, string>>({});

  function fechar(): void {
    setErros({});
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const next: Record<string, string> = {};
    if (codigoInfracaoCtb.trim() === '') next.codigoInfracaoCtb = 'Código de infração (CTB) obrigatório.';
    if (valor.trim() === '' || Number(valor) <= 0) next.valor = 'Valor da multa deve ser positivo.';
    if (dataInfracao === '') next.dataInfracao = 'Informe a data da infração.';
    setErros(next);
    if (Object.keys(next).length > 0) return;

    mutation.mutate(
      {
        codigoInfracaoCtb: codigoInfracaoCtb.trim(),
        valor: Number(valor),
        dataInfracao,
        motoristaId: motoristaId.trim() || null,
      },
      {
        onSuccess: () => {
          toast.success('Multa registrada.', 'Sucesso');
          fechar();
        },
        onError: (error) => toast.error(aplicarErrosBackend(error, ['codigoInfracaoCtb', 'valor', 'dataInfracao', 'motoristaId'], setErros)),
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Registrar multa"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-multa" loading={mutation.isPending}>
            Registrar
          </Button>
        </>
      }
    >
      <form id="form-multa" className="br-form" onSubmit={submeter} noValidate>
        <div className="row">
          <div className="col-sm-6">
            <FormField label="Código de infração (CTB)" required error={erros.codigoInfracaoCtb}>
              {({ id, describedBy, invalid }) => (
                <Input id={id} aria-describedby={describedBy} invalid={invalid} value={codigoInfracaoCtb} onChange={(e) => setCodigoInfracaoCtb(e.target.value)} placeholder="74550" />
              )}
            </FormField>
          </div>
          <div className="col-sm-6">
            <FormField label="Valor (R$)" required error={erros.valor}>
              {({ id, describedBy, invalid }) => (
                <Input id={id} type="number" min="0" step="0.01" inputMode="decimal" aria-describedby={describedBy} invalid={invalid} value={valor} onChange={(e) => setValor(e.target.value)} />
              )}
            </FormField>
          </div>
        </div>
        <div className="row">
          <div className="col-sm-6">
            <FormField label="Data da infração" required error={erros.dataInfracao}>
              {({ id, describedBy, invalid }) => (
                <Input id={id} type="date" aria-describedby={describedBy} invalid={invalid} value={dataInfracao} onChange={(e) => setDataInfracao(e.target.value)} />
              )}
            </FormField>
          </div>
          <div className="col-sm-6">
            <FormField label="Motorista (id)" help="Opcional — condutor responsável.">
              {({ id, describedBy }) => (
                <Input id={id} aria-describedby={describedBy} value={motoristaId} onChange={(e) => setMotoristaId(e.target.value)} placeholder="00000000-0000-0000-0000-000000000000" />
              )}
            </FormField>
          </div>
        </div>
      </form>
    </Modal>
  );
}
