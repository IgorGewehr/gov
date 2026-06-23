// MSC — Matriz de Saldos Contábeis (SICONFI/STN). Gera a matriz de um período a partir
// do balancete (POST /financas/contabilidade/msc/gerar, gated por financas.gerenciar) e
// mostra a quantidade de linhas e se já existia (idempotência). A TRANSMISSÃO ao
// SICONFI/TCE é etapa SEPARADA — esta tela apenas materializa a matriz.
import { useState } from 'react';
import type { FormEvent } from 'react';
import {
  Alert,
  Button,
  Card,
  EmptyState,
  FormField,
  Input,
  PageHeader,
  Select,
  useToast,
} from '../../../components/ui';
import { Can } from '../../../auth/Can';
import { exercicioCorrente, mensagemErro } from '../financas.helpers';
import { FinancasSubNav } from '../FinancasSubNav';
import { MESES } from './contabilidade.helpers';
import { useGerarMsc } from './msc.api';
import type { GerarMscResultado } from './msc.api';

export function MscPage() {
  const toast = useToast();
  const gerar = useGerarMsc();
  const [exercicio, setExercicio] = useState(String(exercicioCorrente()));
  const [mes, setMes] = useState(String(new Date().getMonth() + 1));
  const [resultado, setResultado] = useState<GerarMscResultado | null>(null);

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const ex = Number(exercicio);
    const m = Number(mes);
    if (!(Number.isInteger(ex) && ex >= 2000 && Number.isInteger(m) && m >= 1 && m <= 12)) {
      return;
    }
    gerar.mutate(
      { exercicio: ex, mes: m },
      {
        onSuccess: (data) => {
          setResultado(data);
          toast.success(
            data.jaExistia
              ? 'A MSC deste período já havia sido gerada.'
              : `MSC gerada com ${data.quantidadeLinhas} linha(s).`,
            'MSC',
          );
        },
        onError: (error) => toast.error(mensagemErro(error, 'Não foi possível gerar a MSC.')),
      },
    );
  }

  return (
    <>
      <PageHeader
        title="MSC — Matriz de Saldos Contábeis"
        description="Matriz mensal de saldos (SICONFI/STN) derivada do balancete do período. A geração é idempotente."
      />

      <FinancasSubNav />

      <Alert variant="info" title="A transmissão é uma etapa à parte">
        Gerar a MSC aqui apenas materializa a matriz do período no sistema. O <strong>envio ao
        SICONFI/STN</strong> e a <strong>remessa ao TCE</strong> são realizados em etapa
        separada (prestação de contas), com o certificado digital do ente.
      </Alert>

      <Card className="mb-4">
        <Can
          permission="financas.gerenciar"
          fallback={
            <p className="mb-0 text-gray-60">
              Você não possui permissão para gerar a MSC (<code>financas.gerenciar</code>).
            </p>
          }
        >
          <form className="br-form" onSubmit={submeter}>
            <div className="row align-items-end">
              <div className="col-12 col-md-auto">
                <FormField label="Exercício" required>
                  {({ id, describedBy, invalid }) => (
                    <Input
                      id={id}
                      type="number"
                      min="2000"
                      step="1"
                      inputMode="numeric"
                      aria-describedby={describedBy}
                      invalid={invalid}
                      value={exercicio}
                      onChange={(e) => setExercicio(e.target.value)}
                    />
                  )}
                </FormField>
              </div>
              <div className="col-12 col-md-auto">
                <FormField label="Mês" required>
                  {({ id, describedBy, invalid }) => (
                    <Select
                      id={id}
                      options={MESES}
                      aria-describedby={describedBy}
                      invalid={invalid}
                      value={mes}
                      onChange={(e) => setMes(e.target.value)}
                    />
                  )}
                </FormField>
              </div>
              <div className="col-auto">
                <div className="tg-form-row-acao">
                  <Button variant="primary" type="submit" loading={gerar.isPending}>
                    <i className="fas fa-table-cells" aria-hidden="true" /> Gerar MSC
                  </Button>
                </div>
              </div>
            </div>
          </form>
        </Can>
      </Card>

      {resultado === null ? (
        <EmptyState
          icon="fas fa-table-cells-large"
          title="Nenhuma MSC gerada nesta sessão"
          description="Informe o exercício e o mês e clique em Gerar MSC."
        />
      ) : (
        <Alert
          variant={resultado.jaExistia ? 'warning' : 'success'}
          title={resultado.jaExistia ? 'MSC já existente (idempotência)' : 'MSC gerada'}
        >
          <dl className="row mb-0">
            <dt className="col-sm-4 text-gray-60">Linhas da matriz</dt>
            <dd className="col-sm-8 text-semi-bold mb-2">{resultado.quantidadeLinhas}</dd>
            <dt className="col-sm-4 text-gray-60">Identificador do evento</dt>
            <dd className="col-sm-8 mb-2">
              <span className="text-mono">{resultado.eventId}</span>
            </dd>
            <dt className="col-sm-4 text-gray-60">Situação</dt>
            <dd className="col-sm-8 mb-0">
              {resultado.jaExistia
                ? 'A matriz deste período já havia sido gerada anteriormente.'
                : 'Matriz materializada e evento de integração publicado.'}
            </dd>
          </dl>
        </Alert>
      )}
    </>
  );
}
