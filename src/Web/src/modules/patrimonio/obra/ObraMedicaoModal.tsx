// [Command RegistrarMedicao] Registra um boletim de medição (rascunho) com o avanço
// POR ETAPA no período (% físico + valor). Reforça a regra do TETO (I-1): o medido
// acumulado + o valor deste boletim NÃO pode exceder o valor contratado — checagem
// de tela espelhando a invariante do domínio (a validação OFICIAL é do backend).
import { useMemo, useState } from 'react';
import type { FormEvent } from 'react';
import { Alert, Button, FormField, Input, Modal, useToast } from '../../../components/ui';
import { ApiError } from '../../../api/problemDetails';
import { formatarMoeda } from '../../../i18n/format';
import { useRegistrarMedicao } from './obra.api';
import type { AvancoEtapaInput, EtapaCronogramaDto } from './obra.api';
import { hojeIso } from './obra.helpers';

export interface ObraMedicaoModalProps {
  open: boolean;
  onClose: () => void;
  obraId: string;
  etapas: EtapaCronogramaDto[];
  valorContratado: number;
  valorMedidoAcumulado: number;
}

interface LinhaAvanco {
  percentual: string;
  valor: string;
}

const hojeMes = (): number => new Date().getMonth() + 1;
const hojeAno = (): number => new Date().getFullYear();

export function ObraMedicaoModal({
  open,
  onClose,
  obraId,
  etapas,
  valorContratado,
  valorMedidoAcumulado,
}: ObraMedicaoModalProps) {
  const toast = useToast();
  const mutation = useRegistrarMedicao(obraId);

  const [ano, setAno] = useState(String(hojeAno()));
  const [mes, setMes] = useState(String(hojeMes()));
  const [periodoInicio, setPeriodoInicio] = useState(hojeIso());
  const [periodoFim, setPeriodoFim] = useState(hojeIso());
  const [linhas, setLinhas] = useState<Record<string, LinhaAvanco>>({});
  const [erro, setErro] = useState<string | null>(null);

  function valorLinha(etapaId: string): number {
    return Number((linhas[etapaId]?.valor ?? '').replace(',', '.')) || 0;
  }

  const totalBoletim = useMemo(
    () => etapas.reduce((soma, etapa) => soma + valorLinha(etapa.id), 0),
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [linhas, etapas],
  );

  const saldoContrato = valorContratado - valorMedidoAcumulado;
  const excedeTeto = totalBoletim > saldoContrato + 0.005;

  function alterar(etapaId: string, campo: keyof LinhaAvanco, valor: string): void {
    setLinhas((atuais) => {
      const atual = atuais[etapaId] ?? { percentual: '', valor: '' };
      return { ...atuais, [etapaId]: { ...atual, [campo]: valor } };
    });
  }

  function fechar(): void {
    setErro(null);
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const anoNum = Number(ano);
    const mesNum = Number(mes);
    if (!Number.isInteger(anoNum) || !Number.isInteger(mesNum) || mesNum < 1 || mesNum > 12) {
      setErro('Competência inválida (ano/mês).');
      return;
    }
    const avancos: AvancoEtapaInput[] = etapas
      .map((etapa) => ({
        etapaId: etapa.id,
        percentualFisicoNoPeriodo:
          Number((linhas[etapa.id]?.percentual ?? '').replace(',', '.')) || 0,
        valorNoPeriodo: valorLinha(etapa.id),
      }))
      .filter((a) => a.percentualFisicoNoPeriodo > 0 || a.valorNoPeriodo > 0);

    if (avancos.length === 0) {
      setErro('Informe o avanço (% físico/valor) de ao menos uma etapa.');
      return;
    }
    if (excedeTeto) {
      setErro('A medição acumulada não pode exceder o valor contratado (teto — I-1).');
      return;
    }
    setErro(null);

    mutation.mutate(
      {
        competenciaAno: anoNum,
        competenciaMes: mesNum,
        periodoInicio,
        periodoFim,
        avancos,
      },
      {
        onSuccess: () => {
          toast.success('Boletim de medição registrado (rascunho).', 'Sucesso');
          fechar();
        },
        onError: (error) => {
          toast.error(
            error instanceof ApiError
              ? error.userMessage
              : 'Não foi possível registrar a medição.',
          );
        },
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Registrar medição (boletim)"
      size="large"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button
            variant="primary"
            type="submit"
            form="form-medicao"
            loading={mutation.isPending}
            disabled={excedeTeto}
          >
            Registrar boletim
          </Button>
        </>
      }
    >
      <form id="form-medicao" className="br-form" onSubmit={submeter} noValidate>
        {erro && (
          <p className="text-danger text-down-01 mb-3" role="alert">
            {erro}
          </p>
        )}
        <div className="row">
          <div className="col-sm-3">
            <FormField label="Ano" required>
              {({ id, describedBy }) => (
                <Input id={id} type="number" aria-describedby={describedBy} value={ano} onChange={(e) => setAno(e.target.value)} />
              )}
            </FormField>
          </div>
          <div className="col-sm-3">
            <FormField label="Mês" required>
              {({ id, describedBy }) => (
                <Input id={id} type="number" min={1} max={12} aria-describedby={describedBy} value={mes} onChange={(e) => setMes(e.target.value)} />
              )}
            </FormField>
          </div>
          <div className="col-sm-3">
            <FormField label="Período (início)" required>
              {({ id, describedBy }) => (
                <Input id={id} type="date" aria-describedby={describedBy} value={periodoInicio} onChange={(e) => setPeriodoInicio(e.target.value)} />
              )}
            </FormField>
          </div>
          <div className="col-sm-3">
            <FormField label="Período (fim)" required>
              {({ id, describedBy }) => (
                <Input id={id} type="date" aria-describedby={describedBy} value={periodoFim} onChange={(e) => setPeriodoFim(e.target.value)} />
              )}
            </FormField>
          </div>
        </div>

        <fieldset className="mb-3">
          <legend className="text-down-01 text-semi-bold">Avanço por etapa no período</legend>
          {etapas.length === 0 ? (
            <p className="text-down-01 text-gray-60">
              Defina o cronograma físico-financeiro antes de medir.
            </p>
          ) : (
            etapas.map((etapa) => (
              <div className="row align-items-end mb-2" key={etapa.id}>
                <div className="col-sm-6">
                  <span className="text-down-01 text-semi-bold">
                    {etapa.ordem}. {etapa.descricao}
                  </span>
                  <br />
                  <span className="text-down-02 text-gray-60">
                    Previsto {formatarMoeda(etapa.valorPrevisto)} · medido {formatarMoeda(etapa.valorMedido)}
                  </span>
                </div>
                <div className="col-sm-3">
                  <FormField label="% físico">
                    {({ id, describedBy }) => (
                      <Input
                        id={id}
                        inputMode="decimal"
                        aria-describedby={describedBy}
                        value={linhas[etapa.id]?.percentual ?? ''}
                        onChange={(e) => alterar(etapa.id, 'percentual', e.target.value)}
                        placeholder="0,00"
                      />
                    )}
                  </FormField>
                </div>
                <div className="col-sm-3">
                  <FormField label="Valor (R$)">
                    {({ id, describedBy }) => (
                      <Input
                        id={id}
                        inputMode="decimal"
                        aria-describedby={describedBy}
                        value={linhas[etapa.id]?.valor ?? ''}
                        onChange={(e) => alterar(etapa.id, 'valor', e.target.value)}
                        placeholder="0,00"
                      />
                    )}
                  </FormField>
                </div>
              </div>
            ))
          )}
        </fieldset>

        <Alert variant={excedeTeto ? 'danger' : 'info'} title="Teto contratual (I-1)">
          Saldo do contrato: <strong>{formatarMoeda(saldoContrato)}</strong> · total deste boletim:{' '}
          <strong>{formatarMoeda(totalBoletim)}</strong>.{' '}
          {excedeTeto
            ? 'O boletim excede o saldo — a medição acumulada não pode ultrapassar o valor contratado.'
            : 'Dentro do limite contratado.'}
        </Alert>
      </form>
    </Modal>
  );
}
