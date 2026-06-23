// Formulário de GERAÇÃO de verbas rescisórias (rescisão/desligamento) em Modal. Gera a folha
// Tipo=Rescisao da competência (mês/ano da data de desligamento) e leva ao demonstrativo
// (detalhe da folha resultante). As verbas devidas são compostas pela matriz parametrizável
// (tipo de desligamento × regime) no backend — aviso prévio e multa de 40% só fazem sentido
// para o celetista (informados pelo operador). Padrão-ouro: mutation + validação + Toast.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Button,
  FormField,
  Modal,
  Select,
  useToast,
} from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import { CampoControlado } from './CampoControlado';
import { useGerarRescisao, useServidoresAtivos } from './api';
import type { GerarRescisaoInput } from './api';
import {
  permiteVerbasCeletistas,
  REGIMES_VINCULO,
  TIPOS_DESLIGAMENTO,
} from './recursosHumanos.helpers';

export interface GerarRescisaoFormModalProps {
  open: boolean;
  onClose: () => void;
}

interface FormErrors {
  servidorId?: string;
  dataDesligamento?: string;
  tipoDesligamento?: string;
  regime?: string;
  vencimento?: string;
  diasTrabalhadosNoMes?: string;
  admissao?: string;
  inicioPeriodoAquisitivoFerias?: string;
  diasFeriasVencidas?: string;
  valorAvisoPrevio?: string;
  valorMultaFgts?: string;
}

const CAMPOS: Record<keyof FormErrors, true> = {
  servidorId: true,
  dataDesligamento: true,
  tipoDesligamento: true,
  regime: true,
  vencimento: true,
  diasTrabalhadosNoMes: true,
  admissao: true,
  inicioPeriodoAquisitivoFerias: true,
  diasFeriasVencidas: true,
  valorAvisoPrevio: true,
  valorMultaFgts: true,
};

function competenciaDaData(dataIso: string): { ano: number; mes: number } | null {
  const [ano, mes] = dataIso.split('-').map(Number);
  if (!Number.isInteger(ano) || !Number.isInteger(mes)) return null;
  return { ano, mes };
}

export function GerarRescisaoFormModal({ open, onClose }: GerarRescisaoFormModalProps) {
  const toast = useToast();
  const navigate = useNavigate();
  const mutation = useGerarRescisao();
  const servidoresQuery = useServidoresAtivos();

  const [servidorId, setServidorId] = useState('');
  const [dataDesligamento, setDataDesligamento] = useState('');
  const [tipoDesligamento, setTipoDesligamento] = useState('');
  const [regime, setRegime] = useState('1');
  const [vencimento, setVencimento] = useState('');
  const [diasTrabalhadosNoMes, setDiasTrabalhadosNoMes] = useState('');
  const [admissao, setAdmissao] = useState('');
  const [inicioPeriodoAquisitivoFerias, setInicioPeriodoAquisitivoFerias] = useState('');
  const [diasFeriasVencidas, setDiasFeriasVencidas] = useState('0');
  const [valorAvisoPrevio, setValorAvisoPrevio] = useState('0');
  const [valorMultaFgts, setValorMultaFgts] = useState('0');
  const [errors, setErrors] = useState<FormErrors>({});

  const celetista = permiteVerbasCeletistas(regime);

  const servidorOptions = (servidoresQuery.data ?? []).map((s) => ({
    value: s.id,
    label: `${s.matricula} — ${s.nomeServidor}`,
  }));

  function validar(): FormErrors {
    const next: FormErrors = {};
    if (servidorId.trim() === '') next.servidorId = 'Selecione o servidor.';
    if (dataDesligamento.trim() === '')
      next.dataDesligamento = 'Informe a data de desligamento.';
    if (tipoDesligamento.trim() === '')
      next.tipoDesligamento = 'Selecione o tipo de desligamento.';
    if (regime !== '1' && regime !== '2') next.regime = 'Selecione o regime do vínculo.';
    const venc = Number(vencimento);
    if (vencimento.trim() === '' || Number.isNaN(venc) || venc < 0)
      next.vencimento = 'Informe o vencimento (não negativo).';
    const dias = Number(diasTrabalhadosNoMes);
    if (!Number.isInteger(dias) || dias < 0 || dias > 31)
      next.diasTrabalhadosNoMes = 'Dias trabalhados entre 0 e 31.';
    if (admissao.trim() === '') next.admissao = 'Informe a admissão/exercício.';
    if (inicioPeriodoAquisitivoFerias.trim() === '')
      next.inicioPeriodoAquisitivoFerias = 'Informe o início do período aquisitivo.';
    const feriasVencidas = Number(diasFeriasVencidas);
    if (!Number.isInteger(feriasVencidas) || feriasVencidas < 0)
      next.diasFeriasVencidas = 'Informe os dias de férias vencidas (≥ 0).';
    const aviso = Number(valorAvisoPrevio);
    if (Number.isNaN(aviso) || aviso < 0)
      next.valorAvisoPrevio = 'Valor do aviso prévio não pode ser negativo.';
    const multa = Number(valorMultaFgts);
    if (Number.isNaN(multa) || multa < 0)
      next.valorMultaFgts = 'Valor da multa do FGTS não pode ser negativo.';
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

    // Aviso e multa de FGTS só existem para o celetista — zera para o estatutário.
    const input: GerarRescisaoInput = {
      servidorId,
      dataDesligamento,
      tipoDesligamento: Number(tipoDesligamento),
      regime: Number(regime),
      vencimento: Number(vencimento),
      diasTrabalhadosNoMes: Number(diasTrabalhadosNoMes),
      admissao,
      inicioPeriodoAquisitivoFerias,
      diasFeriasVencidas: Number(diasFeriasVencidas),
      valorAvisoPrevio: celetista ? Number(valorAvisoPrevio) : 0,
      valorMultaFgts: celetista ? Number(valorMultaFgts) : 0,
    };

    mutation.mutate(input, {
      onSuccess: ({ folhaId }) => {
        const competencia = competenciaDaData(input.dataDesligamento);
        toast.success('Verbas rescisórias geradas.', 'Sucesso');
        fechar();
        navigate(`/recursoshumanos/folhas/${folhaId}`, {
          state: competencia ?? {},
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
          error instanceof ApiError
            ? error.userMessage
            : 'Não foi possível gerar as verbas rescisórias.',
        );
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Gerar rescisão"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button
            variant="primary"
            type="submit"
            form="form-gerar-rescisao"
            loading={mutation.isPending}
          >
            Gerar rescisão
          </Button>
        </>
      }
    >
      <form id="form-gerar-rescisao" className="br-form" onSubmit={submeter} noValidate>
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
            <FormField label="Tipo de desligamento" required error={errors.tipoDesligamento}>
              {({ id, describedBy, invalid }) => (
                <Select
                  id={id}
                  aria-describedby={describedBy}
                  invalid={invalid}
                  value={tipoDesligamento}
                  onChange={(e) => setTipoDesligamento(e.target.value)}
                  placeholder="Selecione o tipo"
                  options={TIPOS_DESLIGAMENTO}
                />
              )}
            </FormField>
          </div>
          <div className="col-sm-6">
            <FormField label="Regime do vínculo" required error={errors.regime}>
              {({ id, describedBy, invalid }) => (
                <Select
                  id={id}
                  aria-describedby={describedBy}
                  invalid={invalid}
                  value={regime}
                  onChange={(e) => setRegime(e.target.value)}
                  options={REGIMES_VINCULO}
                />
              )}
            </FormField>
          </div>
        </div>

        <CampoControlado
          label="Data de desligamento"
          required
          type="date"
          help="Define a competência (mês/ano) da folha de rescisão."
          value={dataDesligamento}
          onChange={setDataDesligamento}
          error={errors.dataDesligamento}
        />

        <div className="row">
          <div className="col-sm-6">
            <CampoControlado
              label="Vencimento mensal-base (R$)"
              required
              type="number"
              min="0"
              step="0.01"
              inputMode="decimal"
              value={vencimento}
              onChange={setVencimento}
              error={errors.vencimento}
            />
          </div>
          <div className="col-sm-6">
            <CampoControlado
              label="Dias trabalhados no mês"
              required
              type="number"
              min="0"
              max="31"
              step="1"
              inputMode="numeric"
              help="Saldo de salário (0 a 31)."
              value={diasTrabalhadosNoMes}
              onChange={setDiasTrabalhadosNoMes}
              error={errors.diasTrabalhadosNoMes}
            />
          </div>
        </div>

        <div className="row">
          <div className="col-sm-6">
            <CampoControlado
              label="Admissão / exercício"
              required
              type="date"
              help="Início da contagem de avos do 13º."
              value={admissao}
              onChange={setAdmissao}
              error={errors.admissao}
            />
          </div>
          <div className="col-sm-6">
            <CampoControlado
              label="Início do período aquisitivo"
              required
              type="date"
              help="Férias proporcionais em curso."
              value={inicioPeriodoAquisitivoFerias}
              onChange={setInicioPeriodoAquisitivoFerias}
              error={errors.inicioPeriodoAquisitivoFerias}
            />
          </div>
        </div>

        <CampoControlado
          label="Dias de férias vencidas"
          type="number"
          min="0"
          step="1"
          inputMode="numeric"
          help="Períodos completos não gozados."
          value={diasFeriasVencidas}
          onChange={setDiasFeriasVencidas}
          error={errors.diasFeriasVencidas}
        />

        {celetista && (
          <div className="row">
            <div className="col-sm-6">
              <CampoControlado
                label="Aviso prévio (R$)"
                type="number"
                min="0"
                step="0.01"
                inputMode="decimal"
                help="Só celetista — informado pelo operador."
                value={valorAvisoPrevio}
                onChange={setValorAvisoPrevio}
                error={errors.valorAvisoPrevio}
              />
            </div>
            <div className="col-sm-6">
              <CampoControlado
                label="Multa 40% FGTS (R$)"
                type="number"
                min="0"
                step="0.01"
                inputMode="decimal"
                help="Só celetista — informado pelo operador."
                value={valorMultaFgts}
                onChange={setValorMultaFgts}
                error={errors.valorMultaFgts}
              />
            </div>
          </div>
        )}
      </form>
    </Modal>
  );
}
