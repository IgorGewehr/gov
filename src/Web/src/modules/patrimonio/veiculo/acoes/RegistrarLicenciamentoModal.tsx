// RegistrarLicenciamento — registra licenciamento/IPVA por exercício.
// WIRED a useRegistrarLicenciamento (I-11: um Regular por exercício/veículo).
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Alert, Button, FormField, Input, Modal, useToast } from '../../../../components/ui';
import { hojeIso } from '../veiculo.helpers';
import { useRegistrarLicenciamento } from '../veiculo.api';
import { aplicarErrosBackend } from './acaoModais.shared';
import type { AcaoModalBaseProps } from './acaoModais.shared';

export function RegistrarLicenciamentoModal({
  veiculoId,
  open,
  onClose,
}: Pick<AcaoModalBaseProps, 'veiculoId' | 'open' | 'onClose'>) {
  const toast = useToast();
  const mutation = useRegistrarLicenciamento(veiculoId);

  const anoCorrente = new Date().getFullYear();
  const [exercicio, setExercicio] = useState(String(anoCorrente));
  const [valorIpva, setValorIpva] = useState('');
  const [valorTaxa, setValorTaxa] = useState('');
  const [data, setData] = useState(hojeIso());
  const [erros, setErros] = useState<Record<string, string>>({});

  function fechar(): void {
    setErros({});
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const next: Record<string, string> = {};
    const ex = Number(exercicio);
    if (!Number.isInteger(ex) || ex < 1900 || ex > anoCorrente + 1) next.exercicio = 'Exercício inválido.';
    if (valorIpva.trim() === '' || Number(valorIpva) < 0) next.valorIpva = 'Valor do IPVA inválido.';
    if (valorTaxa.trim() === '' || Number(valorTaxa) < 0) next.valorTaxa = 'Valor da taxa inválido.';
    if (data === '') next.data = 'Informe a data.';
    setErros(next);
    if (Object.keys(next).length > 0) return;

    mutation.mutate(
      { exercicio: ex, valorIpva: Number(valorIpva), valorTaxa: Number(valorTaxa), data },
      {
        onSuccess: () => {
          toast.success(`Licenciamento ${ex} registrado.`, 'Sucesso');
          fechar();
        },
        onError: (error) => toast.error(aplicarErrosBackend(error, ['exercicio', 'valorIpva', 'valorTaxa', 'data'], setErros)),
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Registrar licenciamento / IPVA"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-licenciamento" loading={mutation.isPending}>
            Registrar
          </Button>
        </>
      }
    >
      <form id="form-licenciamento" className="br-form" onSubmit={submeter} noValidate>
        <Alert variant="info">Não é permitido mais de um licenciamento Regular por exercício/veículo (I-11).</Alert>
        <div className="row">
          <div className="col-sm-4">
            <FormField label="Exercício" required error={erros.exercicio}>
              {({ id, describedBy, invalid }) => (
                <Input id={id} type="number" min="1900" step="1" inputMode="numeric" aria-describedby={describedBy} invalid={invalid} value={exercicio} onChange={(e) => setExercicio(e.target.value)} />
              )}
            </FormField>
          </div>
          <div className="col-sm-4">
            <FormField label="Valor IPVA (R$)" required error={erros.valorIpva}>
              {({ id, describedBy, invalid }) => (
                <Input id={id} type="number" min="0" step="0.01" inputMode="decimal" aria-describedby={describedBy} invalid={invalid} value={valorIpva} onChange={(e) => setValorIpva(e.target.value)} />
              )}
            </FormField>
          </div>
          <div className="col-sm-4">
            <FormField label="Valor taxa (R$)" required error={erros.valorTaxa}>
              {({ id, describedBy, invalid }) => (
                <Input id={id} type="number" min="0" step="0.01" inputMode="decimal" aria-describedby={describedBy} invalid={invalid} value={valorTaxa} onChange={(e) => setValorTaxa(e.target.value)} />
              )}
            </FormField>
          </div>
        </div>
        <FormField label="Data" required error={erros.data}>
          {({ id, describedBy, invalid }) => (
            <Input id={id} type="date" aria-describedby={describedBy} invalid={invalid} value={data} onChange={(e) => setData(e.target.value)} />
          )}
        </FormField>
      </form>
    </Modal>
  );
}
