// Formulário de INCORPORAÇÃO de veículo (IncorporarVeiculo) em Modal acessível.
// Padrão-ouro: useMutation + validação por campo (FormField/aria) + Toast +
// mapeamento de ApiError.fieldErrors. Reflete o validator IncorporarVeiculoValidator
// e as guardas da incorporação (placa/renavam, residual <= inicial, vida útil > 0).
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Button, FormField, Input, Modal, useToast } from '../../../components/ui';
import { ApiError } from '../../../api/problemDetails';
import { hojeIso } from './veiculo.helpers';
import { useIncorporarVeiculo } from './veiculo.api';
import type { IncorporarVeiculoInput } from './veiculo.api';

export interface VeiculoFormModalProps {
  open: boolean;
  onClose: () => void;
  /** Chamado após a incorporação com o id do veículo criado. */
  onCriado?: (veiculoId: string) => void;
}

interface FormErrors {
  descricao?: string;
  origem?: string;
  placa?: string;
  renavam?: string;
  valorInicial?: string;
  valorResidual?: string;
  vidaUtilMeses?: string;
  dataIncorporacao?: string;
  odometroInicial?: string;
  horimetroInicial?: string;
}

const CAMPOS: Record<keyof FormErrors, true> = {
  descricao: true,
  origem: true,
  placa: true,
  renavam: true,
  valorInicial: true,
  valorResidual: true,
  vidaUtilMeses: true,
  dataIncorporacao: true,
  odometroInicial: true,
  horimetroInicial: true,
};

export function VeiculoFormModal({ open, onClose, onCriado }: VeiculoFormModalProps) {
  const toast = useToast();
  const mutation = useIncorporarVeiculo();

  const [descricao, setDescricao] = useState('');
  const [origem, setOrigem] = useState('');
  const [placa, setPlaca] = useState('');
  const [renavam, setRenavam] = useState('');
  const [valorInicial, setValorInicial] = useState('');
  const [valorResidual, setValorResidual] = useState('');
  const [vidaUtilMeses, setVidaUtilMeses] = useState('');
  const [dataIncorporacao, setDataIncorporacao] = useState(hojeIso());
  const [odometroInicial, setOdometroInicial] = useState('0');
  const [horimetroInicial, setHorimetroInicial] = useState('0');
  const [errors, setErrors] = useState<FormErrors>({});

  function validar(): FormErrors {
    const next: FormErrors = {};
    if (descricao.trim() === '') next.descricao = 'Descrição obrigatória.';
    else if (descricao.trim().length > 200) next.descricao = 'Máximo de 200 caracteres.';
    if (origem.trim() === '') next.origem = 'Informe a origem do ingresso.';
    if (placa.trim() === '') next.placa = 'Placa obrigatória.';
    if (renavam.trim() === '') next.renavam = 'RENAVAM obrigatório.';

    const inicial = Number(valorInicial);
    if (valorInicial.trim() === '' || Number.isNaN(inicial) || inicial <= 0)
      next.valorInicial = 'Valor inicial deve ser positivo.';

    const residual = Number(valorResidual);
    if (valorResidual.trim() === '' || Number.isNaN(residual) || residual < 0)
      next.valorResidual = 'Valor residual inválido.';
    else if (!Number.isNaN(inicial) && residual > inicial)
      next.valorResidual = 'Valor residual não pode exceder o inicial.';

    const vida = Number(vidaUtilMeses);
    if (vidaUtilMeses.trim() === '' || !Number.isInteger(vida) || vida <= 0)
      next.vidaUtilMeses = 'Vida útil deve ser positiva (meses).';

    if (dataIncorporacao === '') next.dataIncorporacao = 'Informe a data de incorporação.';

    const odo = Number(odometroInicial);
    if (Number.isNaN(odo) || odo < 0) next.odometroInicial = 'Odômetro inválido.';

    const hor = Number(horimetroInicial);
    if (Number.isNaN(hor) || hor < 0) next.horimetroInicial = 'Horímetro inválido.';

    return next;
  }

  function limpar(): void {
    setDescricao('');
    setOrigem('');
    setPlaca('');
    setRenavam('');
    setValorInicial('');
    setValorResidual('');
    setVidaUtilMeses('');
    setDataIncorporacao(hojeIso());
    setOdometroInicial('0');
    setHorimetroInicial('0');
    setErrors({});
  }

  function fechar(): void {
    setErrors({});
    onClose();
  }

  function aplicarErrosBackend(error: ApiError): void {
    const mapped: FormErrors = {};
    for (const [field, messages] of Object.entries(error.fieldErrors)) {
      const key = field.charAt(0).toLowerCase() + field.slice(1);
      if (key in CAMPOS && messages.length > 0) {
        (mapped as Record<string, string>)[key] = messages[0];
      }
    }
    if (Object.keys(mapped).length > 0) setErrors(mapped);
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const validacao = validar();
    setErrors(validacao);
    if (Object.keys(validacao).length > 0) return;

    const input: IncorporarVeiculoInput = {
      descricao: descricao.trim(),
      valorInicial: Number(valorInicial),
      valorResidual: Number(valorResidual),
      vidaUtilMeses: Number(vidaUtilMeses),
      dataIncorporacao,
      origem: origem.trim(),
      placa: placa.trim().toUpperCase(),
      renavam: renavam.trim(),
      odometroInicial: Number(odometroInicial),
      horimetroInicial: Number(horimetroInicial),
    };

    mutation.mutate(input, {
      onSuccess: (resposta) => {
        toast.success('Veículo incorporado à frota.', 'Sucesso');
        limpar();
        onClose();
        onCriado?.(resposta.id);
      },
      onError: (error) => {
        if (error instanceof ApiError) aplicarErrosBackend(error);
        toast.error(
          error instanceof ApiError ? error.userMessage : 'Não foi possível incorporar o veículo.',
        );
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Incorporar veículo"
      size="large"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-incorporar-veiculo" loading={mutation.isPending}>
            Incorporar
          </Button>
        </>
      }
    >
      <form id="form-incorporar-veiculo" className="br-form" onSubmit={submeter} noValidate>
        <FormField label="Descrição" required error={errors.descricao}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              maxLength={200}
              value={descricao}
              onChange={(e) => setDescricao(e.target.value)}
              placeholder="Ex.: Caminhão basculante MB Atego"
            />
          )}
        </FormField>

        <div className="row">
          <div className="col-sm-6">
            <FormField label="Placa" required error={errors.placa}>
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  aria-describedby={describedBy}
                  invalid={invalid}
                  maxLength={8}
                  value={placa}
                  onChange={(e) => setPlaca(e.target.value.toUpperCase())}
                  placeholder="ABC1D23"
                />
              )}
            </FormField>
          </div>
          <div className="col-sm-6">
            <FormField label="RENAVAM" required error={errors.renavam}>
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  aria-describedby={describedBy}
                  invalid={invalid}
                  inputMode="numeric"
                  maxLength={11}
                  value={renavam}
                  onChange={(e) => setRenavam(e.target.value)}
                  placeholder="00000000000"
                />
              )}
            </FormField>
          </div>
        </div>

        <FormField label="Origem do ingresso" required error={errors.origem}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={origem}
              onChange={(e) => setOrigem(e.target.value)}
              placeholder="Aquisição, doação, produção própria…"
            />
          )}
        </FormField>

        <div className="row">
          <div className="col-sm-6">
            <FormField label="Valor inicial (R$)" required error={errors.valorInicial}>
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  type="number"
                  min="0"
                  step="0.01"
                  inputMode="decimal"
                  aria-describedby={describedBy}
                  invalid={invalid}
                  value={valorInicial}
                  onChange={(e) => setValorInicial(e.target.value)}
                />
              )}
            </FormField>
          </div>
          <div className="col-sm-6">
            <FormField
              label="Valor residual (R$)"
              required
              error={errors.valorResidual}
              help="Não pode exceder o valor inicial."
            >
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  type="number"
                  min="0"
                  step="0.01"
                  inputMode="decimal"
                  aria-describedby={describedBy}
                  invalid={invalid}
                  value={valorResidual}
                  onChange={(e) => setValorResidual(e.target.value)}
                />
              )}
            </FormField>
          </div>
        </div>

        <div className="row">
          <div className="col-sm-6">
            <FormField label="Vida útil (meses)" required error={errors.vidaUtilMeses}>
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  type="number"
                  min="1"
                  step="1"
                  inputMode="numeric"
                  aria-describedby={describedBy}
                  invalid={invalid}
                  value={vidaUtilMeses}
                  onChange={(e) => setVidaUtilMeses(e.target.value)}
                />
              )}
            </FormField>
          </div>
          <div className="col-sm-6">
            <FormField label="Data de incorporação" required error={errors.dataIncorporacao}>
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  type="date"
                  aria-describedby={describedBy}
                  invalid={invalid}
                  value={dataIncorporacao}
                  onChange={(e) => setDataIncorporacao(e.target.value)}
                />
              )}
            </FormField>
          </div>
        </div>

        <div className="row">
          <div className="col-sm-6">
            <FormField label="Odômetro inicial (km)" error={errors.odometroInicial}>
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  type="number"
                  min="0"
                  step="1"
                  inputMode="numeric"
                  aria-describedby={describedBy}
                  invalid={invalid}
                  value={odometroInicial}
                  onChange={(e) => setOdometroInicial(e.target.value)}
                />
              )}
            </FormField>
          </div>
          <div className="col-sm-6">
            <FormField label="Horímetro inicial (h)" error={errors.horimetroInicial}>
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  type="number"
                  min="0"
                  step="0.1"
                  inputMode="decimal"
                  aria-describedby={describedBy}
                  invalid={invalid}
                  value={horimetroInicial}
                  onChange={(e) => setHorimetroInicial(e.target.value)}
                />
              )}
            </FormField>
          </div>
        </div>
      </form>
    </Modal>
  );
}
