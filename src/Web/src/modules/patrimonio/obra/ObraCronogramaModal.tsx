// [Command DefinirCronograma] Define o cronograma físico-financeiro (curva S — I-3):
// etapas com peso físico previsto (%) × valor previsto × janela prevista. A soma dos
// pesos físicos deve fechar 100% e a dos valores deve igualar o valor contratado
// (checagem de tela; a validação OFICIAL é do backend). Só na obra Planejada.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Alert, Button, FormField, Input, Modal, useToast } from '../../../components/ui';
import { ApiError } from '../../../api/problemDetails';
import { formatarMoeda } from '../../../i18n/format';
import { useDefinirCronograma } from './obra.api';
import type { EtapaCronogramaInput } from './obra.api';
import { hojeIso } from './obra.helpers';

export interface ObraCronogramaModalProps {
  open: boolean;
  onClose: () => void;
  obraId: string;
  valorContratado: number;
}

interface LinhaEtapa {
  descricao: string;
  percentual: string;
  valor: string;
  inicio: string;
  fim: string;
}

function linhaVazia(): LinhaEtapa {
  return { descricao: '', percentual: '', valor: '', inicio: hojeIso(), fim: hojeIso() };
}

export function ObraCronogramaModal({
  open,
  onClose,
  obraId,
  valorContratado,
}: ObraCronogramaModalProps) {
  const toast = useToast();
  const mutation = useDefinirCronograma(obraId);

  const [linhas, setLinhas] = useState<LinhaEtapa[]>([linhaVazia()]);
  const [erro, setErro] = useState<string | null>(null);

  const num = (v: string): number => Number(v.replace(',', '.')) || 0;
  const totalPercentual = linhas.reduce((s, l) => s + num(l.percentual), 0);
  const totalValor = linhas.reduce((s, l) => s + num(l.valor), 0);
  const percentualOk = Math.abs(totalPercentual - 100) < 0.01;
  const valorOk = Math.abs(totalValor - valorContratado) < 0.005;

  function alterar(idx: number, campo: keyof LinhaEtapa, valor: string): void {
    setLinhas((atuais) => atuais.map((l, i) => (i === idx ? { ...l, [campo]: valor } : l)));
  }

  function adicionar(): void {
    setLinhas((atuais) => [...atuais, linhaVazia()]);
  }

  function remover(idx: number): void {
    setLinhas((atuais) => (atuais.length > 1 ? atuais.filter((_, i) => i !== idx) : atuais));
  }

  function fechar(): void {
    setErro(null);
    setLinhas([linhaVazia()]);
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    if (linhas.some((l) => l.descricao.trim() === '')) {
      setErro('Toda etapa precisa de descrição.');
      return;
    }
    if (!percentualOk) {
      setErro('A soma dos pesos físicos previstos deve fechar 100%.');
      return;
    }
    if (!valorOk) {
      setErro('A soma dos valores previstos deve igualar o valor contratado.');
      return;
    }
    setErro(null);

    const etapas: EtapaCronogramaInput[] = linhas.map((l, i) => ({
      ordem: i + 1,
      descricao: l.descricao.trim(),
      percentualFisicoPrevisto: num(l.percentual),
      valorPrevisto: num(l.valor),
      dataPrevistaInicio: l.inicio,
      dataPrevistaFim: l.fim,
    }));

    mutation.mutate(etapas, {
      onSuccess: () => {
        toast.success('Cronograma físico-financeiro definido.', 'Sucesso');
        fechar();
      },
      onError: (error) =>
        toast.error(
          error instanceof ApiError ? error.userMessage : 'Não foi possível definir o cronograma.',
        ),
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Definir cronograma físico-financeiro"
      size="large"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-cronograma" loading={mutation.isPending}>
            Salvar cronograma
          </Button>
        </>
      }
    >
      <form id="form-cronograma" className="br-form" onSubmit={submeter} noValidate>
        {erro && (
          <p className="text-danger text-down-01 mb-3" role="alert">
            {erro}
          </p>
        )}
        {linhas.map((linha, idx) => (
          <div className="row align-items-end mb-2" key={idx}>
            <div className="col-sm-4">
              <FormField label={`Etapa ${idx + 1}`} required>
                {({ id, describedBy }) => (
                  <Input
                    id={id}
                    aria-describedby={describedBy}
                    value={linha.descricao}
                    onChange={(e) => alterar(idx, 'descricao', e.target.value)}
                    placeholder="Descrição"
                  />
                )}
              </FormField>
            </div>
            <div className="col-sm-2">
              <FormField label="% físico">
                {({ id, describedBy }) => (
                  <Input
                    id={id}
                    inputMode="decimal"
                    aria-describedby={describedBy}
                    value={linha.percentual}
                    onChange={(e) => alterar(idx, 'percentual', e.target.value)}
                    placeholder="0,00"
                  />
                )}
              </FormField>
            </div>
            <div className="col-sm-2">
              <FormField label="Valor (R$)">
                {({ id, describedBy }) => (
                  <Input
                    id={id}
                    inputMode="decimal"
                    aria-describedby={describedBy}
                    value={linha.valor}
                    onChange={(e) => alterar(idx, 'valor', e.target.value)}
                    placeholder="0,00"
                  />
                )}
              </FormField>
            </div>
            <div className="col-sm-2">
              <FormField label="Início">
                {({ id, describedBy }) => (
                  <Input
                    id={id}
                    type="date"
                    aria-describedby={describedBy}
                    value={linha.inicio}
                    onChange={(e) => alterar(idx, 'inicio', e.target.value)}
                  />
                )}
              </FormField>
            </div>
            <div className="col-sm-2 d-flex">
              <FormField label="Fim">
                {({ id, describedBy }) => (
                  <Input
                    id={id}
                    type="date"
                    aria-describedby={describedBy}
                    value={linha.fim}
                    onChange={(e) => alterar(idx, 'fim', e.target.value)}
                  />
                )}
              </FormField>
              <Button
                variant="secondary"
                size="sm"
                className="ml-1 mb-3"
                onClick={() => remover(idx)}
                aria-label={`Remover etapa ${idx + 1}`}
              >
                <i className="fas fa-trash" aria-hidden="true" />
              </Button>
            </div>
          </div>
        ))}

        <Button variant="secondary" size="sm" className="mb-3" onClick={adicionar}>
          <i className="fas fa-plus" aria-hidden="true" /> Adicionar etapa
        </Button>

        <Alert variant={percentualOk && valorOk ? 'info' : 'warning'} title="Fechamento da curva S">
          Pesos físicos: <strong>{totalPercentual.toFixed(2)}%</strong> (precisa 100%) · valores:{' '}
          <strong>{formatarMoeda(totalValor)}</strong> de {formatarMoeda(valorContratado)}{' '}
          contratado.
        </Alert>
      </form>
    </Modal>
  );
}
