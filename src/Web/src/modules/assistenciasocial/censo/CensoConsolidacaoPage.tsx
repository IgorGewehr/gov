// Consolidacao do Censo SUAS por exercicio (query ObterCenso). Lista os formularios
// consolidados (volume derivado do RMA), permite (re)consolidar uma unidade e fechar
// (selar) o formulario para envio ao SAGI/MDS. Gating gerenciar nas acoes.
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
  useToast,
} from '../../../components/ui';
import type { Column } from '../../../components/ui';
import { ApiError } from '../../../api/problemDetails';
import { Can } from '../../../auth/Can';
import { AssistenciaSocialSubNav } from '../AssistenciaSocialSubNav';
import { useCenso, useFecharCenso } from './censo.api';
import type { CensoResultado } from './censo.api';
import { ConsolidarCensoModal } from './ConsolidarCensoModal';

const hoje = new Date();

export function CensoConsolidacaoPage() {
  const toast = useToast();
  const [exercicio, setExercicio] = useState(String(hoje.getFullYear()));
  const [consulta, setConsulta] = useState<number | null>(null);
  const [consolidarAberto, setConsolidarAberto] = useState(false);

  const query = useCenso(consulta ?? 0, consulta !== null);
  const fechar = useFecharCenso();

  function consultar(event: FormEvent): void {
    event.preventDefault();
    const ano = Number(exercicio);
    if (Number.isNaN(ano) || ano < 2000) return;
    setConsulta(ano);
  }

  function fecharCenso(row: CensoResultado): void {
    fechar.mutate(
      { unidadeId: row.unidadeId, exercicio: row.exercicio },
      {
        onSuccess: () => toast.success('Censo da unidade fechado (selado).', 'Fechamento'),
        onError: (error) =>
          toast.error(
            error instanceof ApiError ? error.userMessage : 'Não foi possível fechar o Censo.',
          ),
      },
    );
  }

  const columns: Column<CensoResultado>[] = [
    {
      key: 'unidade',
      header: 'Unidade',
      sortAccessor: (c) => c.unidadeId,
      render: (c) => c.unidadeId,
    },
    {
      key: 'situacao',
      header: 'Situação',
      sortAccessor: (c) => (c.fechado ? 1 : 0),
      render: (c) =>
        c.fechado ? <Tag variant="success">Fechado</Tag> : <Tag variant="warning">Em consolidação</Tag>,
    },
    {
      key: 'equipe',
      header: 'Equipe',
      align: 'end',
      sortAccessor: (c) => c.quantidadeProfissionais,
      render: (c) => c.quantidadeProfissionais,
    },
    {
      key: 'servicos',
      header: 'Serviços',
      align: 'end',
      sortAccessor: (c) => c.quantidadeServicosOfertados,
      render: (c) => c.quantidadeServicosOfertados,
    },
    {
      key: 'familias',
      header: 'Famílias referenciadas',
      align: 'end',
      sortAccessor: (c) => c.familiasReferenciadas,
      render: (c) => c.familiasReferenciadas,
    },
    {
      key: 'volume',
      header: 'Volume/ano (RMA)',
      align: 'end',
      sortAccessor: (c) => c.volumeAtendimentosAno,
      render: (c) => c.volumeAtendimentosAno,
    },
    {
      key: 'acoes',
      header: 'Ações',
      render: (c) =>
        c.fechado ? (
          '—'
        ) : (
          <Can permission="assistenciasocial.gerenciar">
            <Button
              variant="tertiary"
              size="sm"
              onClick={() => fecharCenso(c)}
              loading={fechar.isPending}
            >
              Fechar
            </Button>
          </Can>
        ),
    },
  ];

  return (
    <>
      <AssistenciaSocialSubNav />
      <PageHeader
        eyebrow="Assistência Social · Censo SUAS"
        title="Consolidação do Censo"
        description="Formulários consolidados por exercício — o volume anual de atendimentos deriva do RMA (sem dupla digitação). Consolide e feche (sele) para envio ao SAGI/MDS."
        actions={
          <Can permission="assistenciasocial.gerenciar">
            <Toolbar>
              <Button variant="primary" onClick={() => setConsolidarAberto(true)}>
                <i className="fas fa-rotate" aria-hidden="true" /> Consolidar unidade
              </Button>
            </Toolbar>
          </Can>
        }
      />

      <Card className="mb-4">
        <form className="br-form" onSubmit={consultar}>
          <FormRow
            acao={
              <Button variant="primary" type="submit" loading={query.isFetching}>
                Consultar
              </Button>
            }
          >
            <FormField label="Exercício (ano)" required>
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  type="number"
                  min="2000"
                  max="9999"
                  step="1"
                  inputMode="numeric"
                  aria-describedby={describedBy}
                  invalid={invalid}
                  value={exercicio}
                  onChange={(e) => setExercicio(e.target.value)}
                />
              )}
            </FormField>
          </FormRow>
        </form>
      </Card>

      {consulta === null ? (
        <EmptyState
          icon="fas fa-calendar"
          title="Selecione um exercício"
          description="Informe o ano e clique em Consultar para ver os formulários do Censo."
        />
      ) : (
        <DataTable
          caption={`Formulários do Censo SUAS — exercício ${consulta}`}
          columns={columns}
          rows={query.data}
          rowKey={(c) => c.formularioId}
          loading={query.isLoading}
          error={query.isError ? errorMessage(query.error) : null}
          empty={
            <EmptyState
              icon="fas fa-folder-open"
              title="Nenhum formulário consolidado"
              description="Consolide uma unidade para gerar o formulário do Censo deste exercício."
            />
          }
        />
      )}

      <ConsolidarCensoModal
        open={consolidarAberto}
        onClose={() => setConsolidarAberto(false)}
        exercicio={consulta ?? hoje.getFullYear()}
      />
    </>
  );
}
