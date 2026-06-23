// Formulário de GERAÇÃO de férias (remuneração do período + 1/3 constitucional + abono
// pecuniário opcional) em Modal. Gera a folha Tipo=Ferias da competência e leva ao
// demonstrativo (detalhe da folha resultante). Padrão-ouro: mutation + validação + Toast.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Button,
  FormField,
  Input,
  Modal,
  Select,
  useToast,
} from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import { useGerarFerias, useServidoresAtivos } from './api';
import type { GerarFeriasInput } from './api';
import { MESES } from './recursosHumanos.helpers';

export interface GerarFeriasFormModalProps {
  open: boolean;
  onClose: () => void;
  anoInicial: number;
  mesInicial: number;
}

interface FormErrors {
  servidorId?: string;
  ano?: string;
  mes?: string;
  remuneracaoMensal?: string;
  diasGozados?: string;
  diasVendidos?: string;
}

const CAMPOS: Record<keyof FormErrors, true> = {
  servidorId: true,
  ano: true,
  mes: true,
  remuneracaoMensal: true,
  diasGozados: true,
  diasVendidos: true,
};

export function GerarFeriasFormModal({
  open,
  onClose,
  anoInicial,
  mesInicial,
}: GerarFeriasFormModalProps) {
  const toast = useToast();
  const navigate = useNavigate();
  const mutation = useGerarFerias();
  const servidoresQuery = useServidoresAtivos();

  const [servidorId, setServidorId] = useState('');
  const [ano, setAno] = useState(String(anoInicial));
  const [mes, setMes] = useState(String(mesInicial));
  const [remuneracaoMensal, setRemuneracaoMensal] = useState('');
  const [diasGozados, setDiasGozados] = useState('30');
  const [diasVendidos, setDiasVendidos] = useState('0');
  const [errors, setErrors] = useState<FormErrors>({});

  const servidorOptions = (servidoresQuery.data ?? []).map((s) => ({
    value: s.id,
    label: `${s.matricula} — ${s.nomeServidor}`,
  }));

  function validar(): FormErrors {
    const next: FormErrors = {};
    if (servidorId.trim() === '') next.servidorId = 'Selecione o servidor.';
    const anoNum = Number(ano);
    if (!Number.isInteger(anoNum) || anoNum < 2000 || anoNum > 2100)
      next.ano = 'Informe um ano entre 2000 e 2100.';
    const mesNum = Number(mes);
    if (!Number.isInteger(mesNum) || mesNum < 1 || mesNum > 12)
      next.mes = 'Selecione o mês da competência.';
    const rem = Number(remuneracaoMensal);
    if (remuneracaoMensal.trim() === '' || Number.isNaN(rem) || rem <= 0)
      next.remuneracaoMensal = 'Informe a remuneração mensal (maior que zero).';
    const gozados = Number(diasGozados);
    if (!Number.isInteger(gozados) || gozados < 0 || gozados > 30)
      next.diasGozados = 'Dias gozados entre 0 e 30.';
    const vendidos = Number(diasVendidos);
    if (!Number.isInteger(vendidos) || vendidos < 0 || vendidos > 10)
      next.diasVendidos = 'Abono pecuniário: venda de até 1/3 (máx. 10 dias).';
    if (
      Number.isInteger(gozados) &&
      Number.isInteger(vendidos) &&
      gozados + vendidos > 30
    )
      next.diasVendidos = 'Soma de dias gozados e vendidos não pode exceder 30.';
    return next;
  }

  function fechar(): void {
    setErrors({});
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const validacao = validar();
    setErrors(validacao);
    if (Object.keys(validacao).length > 0) return;

    const input: GerarFeriasInput = {
      servidorId,
      ano: Number(ano),
      mes: Number(mes),
      remuneracaoMensal: Number(remuneracaoMensal),
      diasGozados: Number(diasGozados),
      diasVendidos: Number(diasVendidos),
    };

    mutation.mutate(input, {
      onSuccess: ({ folhaId }) => {
        toast.success(
          `Férias geradas para ${String(input.mes).padStart(2, '0')}/${input.ano}.`,
          'Sucesso',
        );
        fechar();
        navigate(`/recursoshumanos/folhas/${folhaId}`, {
          state: { ano: input.ano, mes: input.mes },
        });
      },
      onError: (error) => {
        if (error instanceof ApiError && Object.keys(error.fieldErrors).length > 0) {
          const mapped: FormErrors = {};
          for (const [field, messages] of Object.entries(error.fieldErrors)) {
            const key = (field.charAt(0).toLowerCase() + field.slice(1)) as keyof FormErrors;
            if (key in CAMPOS) mapped[key] = messages[0];
          }
          setErrors(mapped);
        }
        toast.error(
          error instanceof ApiError ? error.userMessage : 'Não foi possível gerar as férias.',
        );
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Gerar férias"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button
            variant="primary"
            type="submit"
            form="form-gerar-ferias"
            loading={mutation.isPending}
          >
            Gerar férias
          </Button>
        </>
      }
    >
      <form id="form-gerar-ferias" className="br-form" onSubmit={submeter} noValidate>
        <FormField
          label="Servidor"
          required
          error={errors.servidorId}
          help={servidoresQuery.isLoading ? 'Carregando servidores…' : undefined}
        >
          {({ id, describedBy, invalid }) => (
            <Select
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={servidorId}
              onChange={(e) => setServidorId(e.target.value)}
              placeholder="Selecione o servidor"
              options={servidorOptions}
              disabled={servidoresQuery.isLoading}
            />
          )}
        </FormField>

        <div className="row">
          <div className="col-sm-6">
            <FormField label="Mês da competência" required error={errors.mes}>
              {({ id, describedBy, invalid }) => (
                <Select
                  id={id}
                  aria-describedby={describedBy}
                  invalid={invalid}
                  value={mes}
                  onChange={(e) => setMes(e.target.value)}
                  options={MESES}
                />
              )}
            </FormField>
          </div>
          <div className="col-sm-6">
            <FormField label="Ano" required error={errors.ano}>
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  type="number"
                  min="2000"
                  max="2100"
                  step="1"
                  inputMode="numeric"
                  aria-describedby={describedBy}
                  invalid={invalid}
                  value={ano}
                  onChange={(e) => setAno(e.target.value)}
                />
              )}
            </FormField>
          </div>
        </div>

        <FormField
          label="Remuneração mensal-base (R$)"
          required
          error={errors.remuneracaoMensal}
          help="Salário + médias habituais."
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
              value={remuneracaoMensal}
              onChange={(e) => setRemuneracaoMensal(e.target.value)}
            />
          )}
        </FormField>

        <div className="row">
          <div className="col-sm-6">
            <FormField
              label="Dias gozados"
              required
              error={errors.diasGozados}
              help="0 a 30 dias."
            >
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  type="number"
                  min="0"
                  max="30"
                  step="1"
                  inputMode="numeric"
                  aria-describedby={describedBy}
                  invalid={invalid}
                  value={diasGozados}
                  onChange={(e) => setDiasGozados(e.target.value)}
                />
              )}
            </FormField>
          </div>
          <div className="col-sm-6">
            <FormField
              label="Dias vendidos (abono)"
              error={errors.diasVendidos}
              help="Abono pecuniário: até 10 dias."
            >
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  type="number"
                  min="0"
                  max="10"
                  step="1"
                  inputMode="numeric"
                  aria-describedby={describedBy}
                  invalid={invalid}
                  value={diasVendidos}
                  onChange={(e) => setDiasVendidos(e.target.value)}
                />
              )}
            </FormField>
          </div>
        </div>
      </form>
    </Modal>
  );
}
