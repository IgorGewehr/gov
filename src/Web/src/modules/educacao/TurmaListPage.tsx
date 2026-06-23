// Tela de LISTA + BUSCA de turmas (módulo Educação). Espelha o endpoint REAL
// GET /educacao/turmas?escolaId&anoLetivo&turno&etapa&situacao&pagina&tamanho
// (ResultadoPaginado<TurmaItemLista>) — exibe VAGAS DISPONÍVEIS. Ações de ciclo de
// vida (abrir/encerrar) gated em "educacao.gerenciar"; leitura em "educacao.ver".
import { useMemo, useState } from 'react';
import type { FormEvent } from 'react';
import {
  Button,
  Card,
  DataTable,
  EmptyState,
  FormField,
  FormRow,
  PageHeader,
  Select,
  Tag,
  Toolbar,
  useToast,
} from '../../components/ui';
import type { Column, SelectOption } from '../../components/ui';
import { errorMessage } from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import { Can } from '../../auth/Can';
import { useHasPermission } from '../../auth/Can';
import { useBuscarTurmas, useAbrirTurma, useEncerrarTurma } from './turma.api';
import type { TurmaBuscaFiltro, TurmaItemLista } from './turma.api';
import { useEscolasDaRede } from './escola.api';
import { opcoesSituacaoTurma, situacaoTurmaTagVariant } from './educacao.helpers';
import { TurmaFormModal } from './TurmaFormModal';
import { EducacaoSubNav } from './EducacaoSubNav';

const TAMANHO_PAGINA = 20;

export function TurmaListPage() {
  const toast = useToast();
  const podeGerenciar = useHasPermission('educacao.gerenciar');

  const [escolaCampo, setEscolaCampo] = useState('');
  const [situacaoCampo, setSituacaoCampo] = useState('');
  const [escolaId, setEscolaId] = useState('');
  const [situacao, setSituacao] = useState('');
  const [pagina, setPagina] = useState(1);
  const [formAberto, setFormAberto] = useState(false);

  const escolas = useEscolasDaRede();
  const abrir = useAbrirTurma();
  const encerrar = useEncerrarTurma();

  const opcoesEscola = useMemo<SelectOption[]>(
    () => (escolas.data ?? []).map((e) => ({ value: e.id, label: e.nome })),
    [escolas.data],
  );

  const filtro = useMemo<TurmaBuscaFiltro>(
    () => ({
      escolaId: escolaId || undefined,
      situacao: situacao ? Number(situacao) : undefined,
      pagina,
      tamanho: TAMANHO_PAGINA,
    }),
    [escolaId, situacao, pagina],
  );

  const query = useBuscarTurmas(filtro);

  function aplicarBusca(event: FormEvent): void {
    event.preventDefault();
    setPagina(1);
    setEscolaId(escolaCampo);
    setSituacao(situacaoCampo);
  }

  function onAbrir(turmaId: string): void {
    abrir.mutate(turmaId, {
      onSuccess: () => toast.success('Turma aberta para enturmação.', 'Sucesso'),
      onError: (error) =>
        toast.error(error instanceof ApiError ? error.userMessage : 'Não foi possível abrir a turma.'),
    });
  }

  function onEncerrar(turmaId: string): void {
    encerrar.mutate(
      { turmaId, encerramentoAnoLetivo: false },
      {
        onSuccess: () => toast.success('Turma encerrada.', 'Sucesso'),
        onError: (error) =>
          toast.error(error instanceof ApiError ? error.userMessage : 'Não foi possível encerrar a turma.'),
      },
    );
  }

  const total = query.data?.total ?? 0;
  const totalPaginas = total > 0 ? Math.ceil(total / TAMANHO_PAGINA) : 0;
  const ocupada = abrir.isPending || encerrar.isPending;

  const columns: Column<TurmaItemLista>[] = [
    { key: 'serie', header: 'Série/ano', sortAccessor: (t) => t.serie, render: (t) => t.serie },
    { key: 'etapa', header: 'Etapa', render: (t) => t.etapa },
    { key: 'turno', header: 'Turno', render: (t) => t.turno },
    { key: 'ano', header: 'Ano letivo', sortAccessor: (t) => t.anoLetivo, render: (t) => t.anoLetivo },
    {
      key: 'vagas',
      header: 'Vagas (disp./total)',
      sortAccessor: (t) => t.vagasDisponiveis,
      render: (t) => (
        <Tag variant={t.vagasDisponiveis > 0 ? 'success' : 'warning'}>
          {t.vagasDisponiveis} / {t.vagas}
        </Tag>
      ),
    },
    {
      key: 'situacao',
      header: 'Situação',
      sortAccessor: (t) => t.situacao,
      render: (t) => <Tag variant={situacaoTurmaTagVariant(t.situacao)}>{t.situacao}</Tag>,
    },
    ...(podeGerenciar
      ? [
          {
            key: 'acoes',
            header: 'Ações',
            sticky: true,
            render: (t: TurmaItemLista) => (
              <div className="d-flex">
                {t.situacao === 'Planejada' && (
                  <Button variant="secondary" className="small" disabled={ocupada} onClick={() => onAbrir(t.id)}>
                    <i className="fas fa-lock-open" aria-hidden="true" /> Abrir
                  </Button>
                )}
                {t.situacao !== 'Encerrada' && (
                  <Button variant="secondary" className="small ml-2" disabled={ocupada} onClick={() => onEncerrar(t.id)}>
                    <i className="fas fa-flag-checkered" aria-hidden="true" /> Encerrar
                  </Button>
                )}
              </div>
            ),
          } as Column<TurmaItemLista>,
        ]
      : []),
  ];

  return (
    <>
      <EducacaoSubNav />
      <PageHeader
        eyebrow="Educação"
        title="Turmas"
        description="Crie e gerencie turmas, controle vagas e o ciclo de vida (planejada → aberta → encerrada)."
        actions={
          <Can permission="educacao.gerenciar">
            <Toolbar>
              <Button variant="primary" onClick={() => setFormAberto(true)}>
                <i className="fas fa-plus" aria-hidden="true" /> Cadastrar turma
              </Button>
            </Toolbar>
          </Can>
        }
      />

      <Card className="mb-4">
        <form className="br-form" onSubmit={aplicarBusca}>
          <FormRow
            acao={
              <Button variant="primary" type="submit" loading={query.isFetching}>
                Buscar
              </Button>
            }
          >
            <div className="row">
              <div className="col-12 col-md-8">
                <FormField label="Escola">
                  {({ id }) => (
                    <Select
                      id={id}
                      options={opcoesEscola}
                      placeholder="Todas as escolas"
                      value={escolaCampo}
                      onChange={(e) => setEscolaCampo(e.target.value)}
                    />
                  )}
                </FormField>
              </div>
              <div className="col-12 col-md-4">
                <FormField label="Situação">
                  {({ id }) => (
                    <Select
                      id={id}
                      options={opcoesSituacaoTurma}
                      placeholder="Todas"
                      value={situacaoCampo}
                      onChange={(e) => setSituacaoCampo(e.target.value)}
                    />
                  )}
                </FormField>
              </div>
            </div>
          </FormRow>
        </form>
      </Card>

      <DataTable
        caption="Turmas da rede"
        columns={columns}
        rows={query.data?.itens}
        rowKey={(t) => t.id}
        loading={query.isLoading}
        error={query.isError ? errorMessage(query.error) : null}
        empty={
          <EmptyState
            icon="fas fa-chalkboard"
            title="Nenhuma turma encontrada"
            description="Ajuste os filtros ou cadastre uma nova turma."
          />
        }
      />

      {totalPaginas > 1 && (
        <nav
          className="d-flex align-items-center justify-content-between mt-3"
          aria-label="Paginação de turmas"
        >
          <span className="text-down-01 text-secondary">
            Página {pagina} de {totalPaginas} · {total} turma(s)
          </span>
          <div className="d-flex">
            <Button
              variant="secondary"
              className="small"
              disabled={pagina <= 1 || query.isFetching}
              onClick={() => setPagina((p) => Math.max(1, p - 1))}
            >
              <i className="fas fa-chevron-left" aria-hidden="true" /> Anterior
            </Button>
            <Button
              variant="secondary"
              className="small ml-2"
              disabled={pagina >= totalPaginas || query.isFetching}
              onClick={() => setPagina((p) => Math.min(totalPaginas, p + 1))}
            >
              Próxima <i className="fas fa-chevron-right" aria-hidden="true" />
            </Button>
          </div>
        </nav>
      )}

      <TurmaFormModal open={formAberto} onClose={() => setFormAberto(false)} />
    </>
  );
}
