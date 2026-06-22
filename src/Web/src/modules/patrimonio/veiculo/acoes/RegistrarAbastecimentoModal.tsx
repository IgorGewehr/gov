// RegistrarAbastecimento — registra abastecimento com validação de monotonia
// de odômetro/horímetro (I-3/I-4). WIRED a useRegistrarAbastecimento.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Button, FormField, Input, Modal, useToast } from '../../../../components/ui';
import { hojeIso } from '../veiculo.helpers';
import { useRegistrarAbastecimento } from '../veiculo.api';
import { aplicarErrosBackend } from './acaoModais.shared';
import type { AcaoModalBaseProps } from './acaoModais.shared';

export function RegistrarAbastecimentoModal({
  veiculoId,
  open,
  onClose,
  odometroAtual,
  horimetroAtual,
}: AcaoModalBaseProps) {
  const toast = useToast();
  const mutation = useRegistrarAbastecimento(veiculoId);

  const [data, setData] = useState(hojeIso());
  const [litros, setLitros] = useState('');
  const [valor, setValor] = useState('');
  const [odometro, setOdometro] = useState(String(odometroAtual));
  const [horimetro, setHorimetro] = useState(String(horimetroAtual));
  const [motoristaId, setMotoristaId] = useState('');
  const [erros, setErros] = useState<Record<string, string>>({});

  function fechar(): void {
    setErros({});
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const next: Record<string, string> = {};
    if (data === '') next.data = 'Informe a data.';
    if (litros.trim() === '' || Number(litros) <= 0) next.litros = 'Litros deve ser positivo.';
    if (valor.trim() === '' || Number(valor) <= 0) next.valor = 'Valor deve ser positivo.';
    if (odometro.trim() === '' || Number(odometro) < odometroAtual)
      next.odometro = `Odômetro deve ser ≥ ${odometroAtual} km (não pode retroceder).`;
    if (horimetro.trim() === '' || Number(horimetro) < horimetroAtual)
      next.horimetro = `Horímetro deve ser ≥ ${horimetroAtual} h (não pode retroceder).`;
    setErros(next);
    if (Object.keys(next).length > 0) return;

    mutation.mutate(
      {
        data,
        litros: Number(litros),
        valor: Number(valor),
        odometro: Number(odometro),
        horimetro: Number(horimetro),
        motoristaId: motoristaId.trim() || null,
      },
      {
        onSuccess: () => {
          toast.success('Abastecimento registrado.', 'Sucesso');
          fechar();
        },
        onError: (error) => {
          const msg = aplicarErrosBackend(
            error,
            ['data', 'litros', 'valor', 'odometro', 'horimetro', 'motoristaId'],
            setErros,
          );
          toast.error(msg);
        },
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Registrar abastecimento"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-abastecimento" loading={mutation.isPending}>
            Registrar
          </Button>
        </>
      }
    >
      <form id="form-abastecimento" className="br-form" onSubmit={submeter} noValidate>
        <FormField label="Data" required error={erros.data}>
          {({ id, describedBy, invalid }) => (
            <Input id={id} type="date" aria-describedby={describedBy} invalid={invalid} value={data} onChange={(e) => setData(e.target.value)} />
          )}
        </FormField>
        <div className="row">
          <div className="col-sm-6">
            <FormField label="Litros" required error={erros.litros}>
              {({ id, describedBy, invalid }) => (
                <Input id={id} type="number" min="0" step="0.01" inputMode="decimal" aria-describedby={describedBy} invalid={invalid} value={litros} onChange={(e) => setLitros(e.target.value)} />
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
            <FormField label="Odômetro (km)" required error={erros.odometro} help={`Atual: ${odometroAtual} km`}>
              {({ id, describedBy, invalid }) => (
                <Input id={id} type="number" min={odometroAtual} step="1" inputMode="numeric" aria-describedby={describedBy} invalid={invalid} value={odometro} onChange={(e) => setOdometro(e.target.value)} />
              )}
            </FormField>
          </div>
          <div className="col-sm-6">
            <FormField label="Horímetro (h)" required error={erros.horimetro} help={`Atual: ${horimetroAtual} h`}>
              {({ id, describedBy, invalid }) => (
                <Input id={id} type="number" min={horimetroAtual} step="0.1" inputMode="decimal" aria-describedby={describedBy} invalid={invalid} value={horimetro} onChange={(e) => setHorimetro(e.target.value)} />
              )}
            </FormField>
          </div>
        </div>
        <FormField label="Motorista (id)" help="Opcional — condutor no abastecimento.">
          {({ id, describedBy }) => (
            <Input id={id} aria-describedby={describedBy} value={motoristaId} onChange={(e) => setMotoristaId(e.target.value)} placeholder="00000000-0000-0000-0000-000000000000" />
          )}
        </FormField>
      </form>
    </Modal>
  );
}
