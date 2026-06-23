// Formulário de GERAÇÃO do 13º salário (gratificação natalina) em Modal. Gera a folha
// Tipo=DecimoTerceiro da competência e leva ao demonstrativo (detalhe da folha resultante,
// reusando o contracheque existente). Padrão-ouro: mutation + validação por campo + Toast.
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
import { useGerarDecimoTerceiro, useServidoresAtivos } from './api';
import type { GerarDecimoTerceiroInput } from './api';
import { MESES, PARCELAS_13 } from './recursosHumanos.helpers';

export interface GerarDecimoTerceiroFormModalProps {
  open: boolean;
  onClose: () => void;
  anoInicial: number;
}

interface FormErrors {
  servidorId?: string;
  ano?: string;
  mesCompetencia?: string;
  parcela?: string;
  remuneracaoBase?: string;
  admissao?: string;
}

const CAMPOS: Record<keyof FormErrors, true> = {
  servidorId: true,
  ano: true,
  mesCompetencia: true,
  parcela: true,
  remuneracaoBase: true,
  admissao: true,
};

export function GerarDecimoTerceiroFormModal({
  open,
  onClose,
  anoInicial,
}: GerarDecimoTerceiroFormModalProps) {
  const toast = useToast();
  const navigate = useNavigate();
  const mutation = useGerarDecimoTerceiro();
  const servidoresQuery = useServidoresAtivos();

  const [servidorId, setServidorId] = useState('');
  const [ano, setAno] = useState(String(anoInicial));
  const [mesCompetencia, setMesCompetencia] = useState('12');
  const [parcela, setParcela] = useState('2');
  const [remuneracaoBase, setRemuneracaoBase] = useState('');
  const [admissao, setAdmissao] = useState('');
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
    const mesNum = Number(mesCompetencia);
    if (!Number.isInteger(mesNum) || mesNum < 1 || mesNum > 12)
      next.mesCompetencia = 'Selecione o mês da competência.';
    if (parcela !== '1' && parcela !== '2') next.parcela = 'Selecione a parcela (1 ou 2).';
    const rem = Number(remuneracaoBase);
    if (remuneracaoBase.trim() === '' || Number.isNaN(rem) || rem <= 0)
      next.remuneracaoBase = 'Informe a remuneração-base (maior que zero).';
    if (admissao.trim() === '') next.admissao = 'Informe a data de admissão/exercício.';
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

    const input: GerarDecimoTerceiroInput = {
      servidorId,
      ano: Number(ano),
      mesCompetencia: Number(mesCompetencia),
      parcela: Number(parcela),
      remuneracaoBase: Number(remuneracaoBase),
      admissao,
    };

    mutation.mutate(input, {
      onSuccess: ({ folhaId }) => {
        toast.success(
          `13º (${input.parcela}ª parcela) gerado para ${String(input.mesCompetencia).padStart(2, '0')}/${input.ano}.`,
          'Sucesso',
        );
        fechar();
        navigate(`/recursoshumanos/folhas/${folhaId}`, {
          state: { ano: input.ano, mes: input.mesCompetencia },
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
          error instanceof ApiError ? error.userMessage : 'Não foi possível gerar o 13º salário.',
        );
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Gerar 13º salário"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button
            variant="primary"
            type="submit"
            form="form-gerar-13"
            loading={mutation.isPending}
          >
            Gerar 13º
          </Button>
        </>
      }
    >
      <form id="form-gerar-13" className="br-form" onSubmit={submeter} noValidate>
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

        <FormField label="Parcela" required error={errors.parcela}>
          {({ id, describedBy, invalid }) => (
            <Select
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={parcela}
              onChange={(e) => setParcela(e.target.value)}
              options={PARCELAS_13}
            />
          )}
        </FormField>

        <div className="row">
          <div className="col-sm-6">
            <FormField label="Mês da competência" required error={errors.mesCompetencia}>
              {({ id, describedBy, invalid }) => (
                <Select
                  id={id}
                  aria-describedby={describedBy}
                  invalid={invalid}
                  value={mesCompetencia}
                  onChange={(e) => setMesCompetencia(e.target.value)}
                  options={MESES}
                />
              )}
            </FormField>
          </div>
          <div className="col-sm-6">
            <FormField label="Ano-calendário" required error={errors.ano}>
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
          label="Remuneração-base do 13º (R$)"
          required
          error={errors.remuneracaoBase}
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
              value={remuneracaoBase}
              onChange={(e) => setRemuneracaoBase(e.target.value)}
            />
          )}
        </FormField>

        <FormField
          label="Admissão / exercício"
          required
          error={errors.admissao}
          help="Início da contagem de avos no ano."
        >
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              type="date"
              aria-describedby={describedBy}
              invalid={invalid}
              value={admissao}
              onChange={(e) => setAdmissao(e.target.value)}
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}
