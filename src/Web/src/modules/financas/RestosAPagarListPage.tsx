// Lista de Restos a Pagar por exercício de inscrição (GET /restos-a-pagar). Inclui as
// ações de encerramento de exercício (inscreve restos) e, por linha, pagar/cancelar.
import { useState } from 'react';
import type { FormEvent } from 'react';
import {
  Button,
  Card,
  DataTable,
  EmptyState,
  FormField,
  FormRow,
  Input,
  PageHeader,
  Tag,
  Toolbar,
  errorMessage,
} from '../../components/ui';
import type { Column } from '../../components/ui';
import { Can } from '../../auth/Can';
import { formatarMoeda } from '../../i18n/format';
import { useCancelarResto, usePagarResto, useRestosPorExercicio } from './financas.api';
import type { RestoAPagarResumo } from './financas.api';
import { exercicioCorrente, situacaoTagVariant } from './financas.helpers';
import { FinancasSubNav } from './FinancasSubNav';
import { EncerrarExercicioModal } from './EncerrarExercicioModal';
import { ValorAcaoModal } from './ValorAcaoModal';

type AcaoLinha = { tipo: 'pagar' | 'cancelar'; resto: RestoAPagarResumo } | null;

export function RestosAPagarListPage() {
  const [exercicio, setExercicio] = useState(String(exercicioCorrente()));
  const [consultaAtiva, setConsultaAtiva] = useState<number | null>(null);
  const [encerrarAberto, setEncerrarAberto] = useState(false);
  const [acao, setAcao] = useState<AcaoLinha>(null);

  const query = useRestosPorExercicio(consultaAtiva ?? 0, consultaAtiva !== null);
  const pagar = usePagarResto();
  const cancelar = useCancelarResto();

  function consultar(event: FormEvent): void {
    event.preventDefault();
    const valor = Number(exercicio);
    if (Number.isInteger(valor) && valor >= 2000) setConsultaAtiva(valor);
  }

  const columns: Column<RestoAPagarResumo>[] = [
    { key: 'classificacao', header: 'Classificação', sortAccessor: (r) => r.classificacao, render: (r) => r.classificacao },
    {
      key: 'situacao',
      header: 'Situação',
      sortAccessor: (r) => r.situacao,
      render: (r) => <Tag variant={situacaoTagVariant(r.situacao)}>{r.situacao}</Tag>,
    },
    { key: 'inscrito', header: 'Inscrito', align: 'end', sortAccessor: (r) => r.valorInscrito, render: (r) => formatarMoeda(r.valorInscrito) },
    { key: 'saldo', header: 'Saldo a pagar', align: 'end', sortAccessor: (r) => r.saldoAPagar, render: (r) => formatarMoeda(r.saldoAPagar) },
    { key: 'origem', header: 'Exerc. origem', align: 'end', sortAccessor: (r) => r.exercicioOrigem, render: (r) => r.exercicioOrigem },
    {
      key: 'acoes',
      header: 'Ações',
      render: (r) => (
        <Can permission="financas.gerenciar">
          <Toolbar>
            <Button size="sm" variant="ghost" onClick={() => setAcao({ tipo: 'pagar', resto: r })}>
              Pagar
            </Button>
            <Button size="sm" variant="ghost" onClick={() => setAcao({ tipo: 'cancelar', resto: r })}>
              Cancelar
            </Button>
          </Toolbar>
        </Can>
      ),
    },
  ];

  return (
    <>
      <PageHeader
        title="Restos a Pagar"
        description="Despesas empenhadas e não pagas no encerramento do exercício (Lei 4.320/64, art. 36)."
        actions={
          <Can permission="financas.gerenciar">
            <Toolbar>
              <Button variant="primary" onClick={() => setEncerrarAberto(true)}>
                <i className="fas fa-flag-checkered" aria-hidden="true" /> Encerrar exercício
              </Button>
            </Toolbar>
          </Can>
        }
      />

      <FinancasSubNav />

      <Card className="mb-4">
        <form className="br-form" onSubmit={consultar}>
          <FormRow
            acao={
              <Button variant="primary" type="submit" loading={query.isFetching}>
                Consultar
              </Button>
            }
          >
            <FormField label="Exercício de inscrição" required>
              {({ id, describedBy, invalid }) => (
                <Input id={id} type="number" min="2000" step="1" inputMode="numeric"
                  aria-describedby={describedBy} invalid={invalid}
                  value={exercicio} onChange={(e) => setExercicio(e.target.value)} />
              )}
            </FormField>
          </FormRow>
        </form>
      </Card>

      {consultaAtiva === null ? (
        <EmptyState
          icon="fas fa-magnifying-glass"
          title="Faça uma consulta"
          description="Informe o exercício de inscrição e clique em Consultar."
        />
      ) : (
        <DataTable
          caption={`Restos a pagar inscritos em ${consultaAtiva}`}
          columns={columns}
          rows={query.data}
          rowKey={(r) => r.id}
          loading={query.isLoading}
          error={query.isError ? errorMessage(query.error) : null}
          empty={
            <EmptyState
              icon="fas fa-folder-open"
              title="Nenhum resto a pagar encontrado"
              description="Não há restos a pagar inscritos no exercício informado."
            />
          }
        />
      )}

      <EncerrarExercicioModal open={encerrarAberto} onClose={() => setEncerrarAberto(false)} />

      <ValorAcaoModal
        open={acao?.tipo === 'pagar'}
        onClose={() => setAcao(null)}
        title="Pagar resto a pagar"
        label="Valor a pagar (R$)"
        confirmLabel="Pagar"
        pending={pagar.isPending}
        onConfirm={(valor) => pagar.mutateAsync({ id: acao!.resto.id, valor: valor as number })}
        mensagemSucesso="Resto a pagar quitado (parcial/total)."
        mensagemErroPadrao="Não foi possível pagar o resto a pagar."
      />
      <ValorAcaoModal
        open={acao?.tipo === 'cancelar'}
        onClose={() => setAcao(null)}
        title="Cancelar resto a pagar"
        label="Valor a cancelar (R$)"
        confirmLabel="Cancelar resto"
        confirmVariant="danger"
        pending={cancelar.isPending}
        onConfirm={(valor) => cancelar.mutateAsync({ id: acao!.resto.id, valor: valor as number })}
        mensagemSucesso="Resto a pagar cancelado (parcial/total)."
        mensagemErroPadrao="Não foi possível cancelar o resto a pagar."
      />
    </>
  );
}
