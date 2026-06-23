// Tela de LISTA + BUSCA de alunos (módulo Educação). Espelha o endpoint REAL
// GET /educacao/alunos?termo&situacao&pagina&tamanho (ResultadoPaginado<AlunoItemLista>),
// gated em "educacao.ver" (leitura) / "educacao.gerenciar" (cadastro).
// LGPD: a lista é MINIMIZADA (sem CPF). Padrão-ouro: form de busca em Card, DataTable
// com estados loading/vazio/erro e paginação 1-based (keepPreviousData).
import { useMemo, useState } from 'react';
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
  Select,
  Tag,
  Toolbar,
} from '../../components/ui';
import type { Column } from '../../components/ui';
import { errorMessage } from '../../components/ui';
import { Can } from '../../auth/Can';
import { formatarData } from '../../i18n/format';
import { useBuscarAlunos } from './aluno.api';
import type { AlunoBuscaFiltro, AlunoItemLista } from './aluno.api';
import { opcoesSituacaoAluno, situacaoAlunoTagVariant } from './educacao.helpers';
import { AlunoFormModal } from './AlunoFormModal';
import { EducacaoSubNav } from './EducacaoSubNav';

const TAMANHO_PAGINA = 20;

export function AlunoListPage() {
  const [termoCampo, setTermoCampo] = useState('');
  const [situacaoCampo, setSituacaoCampo] = useState('');
  const [termo, setTermo] = useState('');
  const [situacao, setSituacao] = useState('');
  const [pagina, setPagina] = useState(1);
  const [formAberto, setFormAberto] = useState(false);

  const filtro = useMemo<AlunoBuscaFiltro>(
    () => ({
      termo: termo || undefined,
      situacao: situacao ? Number(situacao) : undefined,
      pagina,
      tamanho: TAMANHO_PAGINA,
    }),
    [termo, situacao, pagina],
  );

  const query = useBuscarAlunos(filtro);

  function aplicarBusca(event: FormEvent): void {
    event.preventDefault();
    setPagina(1);
    setTermo(termoCampo.trim());
    setSituacao(situacaoCampo);
  }

  const total = query.data?.total ?? 0;
  const totalPaginas = total > 0 ? Math.ceil(total / TAMANHO_PAGINA) : 0;

  const columns: Column<AlunoItemLista>[] = [
    {
      key: 'nome',
      header: 'Nome',
      sortAccessor: (a) => (a.nomeSocial || a.nome).toLowerCase(),
      render: (a) => (
        <span>
          {a.nomeSocial || a.nome}
          {a.nomeSocial && (
            <span className="d-block text-down-01 text-secondary">Nome civil: {a.nome}</span>
          )}
        </span>
      ),
    },
    {
      key: 'nascimento',
      header: 'Nascimento',
      sortAccessor: (a) => a.dataNascimento,
      render: (a) => formatarData(a.dataNascimento),
    },
    { key: 'sexo', header: 'Sexo', sortAccessor: (a) => a.sexo, render: (a) => a.sexo },
    {
      key: 'inep',
      header: 'Código INEP',
      render: (a) => a.codigoInepAluno ?? '—',
    },
    {
      key: 'situacao',
      header: 'Situação',
      sortAccessor: (a) => a.situacao,
      render: (a) => <Tag variant={situacaoAlunoTagVariant(a.situacao)}>{a.situacao}</Tag>,
    },
  ];

  return (
    <>
      <EducacaoSubNav />
      <PageHeader
        eyebrow="Educação"
        title="Alunos"
        description="Localize alunos por nome ou CPF para matrícula e acompanhamento. Lista minimizada conforme a LGPD (sem CPF)."
        actions={
          <Can permission="educacao.gerenciar">
            <Toolbar>
              <Button variant="primary" onClick={() => setFormAberto(true)}>
                <i className="fas fa-plus" aria-hidden="true" /> Cadastrar aluno
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
                <FormField label="Buscar aluno" help="Nome (trecho) ou CPF (dígitos).">
                  {({ id, describedBy }) => (
                    <Input
                      id={id}
                      aria-describedby={describedBy}
                      value={termoCampo}
                      onChange={(e) => setTermoCampo(e.target.value)}
                      placeholder="Ex.: João da Silva ou 000.000.000-00"
                    />
                  )}
                </FormField>
              </div>
              <div className="col-12 col-md-4">
                <FormField label="Situação">
                  {({ id }) => (
                    <Select
                      id={id}
                      options={opcoesSituacaoAluno}
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
        caption="Alunos da rede"
        columns={columns}
        rows={query.data?.itens}
        rowKey={(a) => a.id}
        loading={query.isLoading}
        error={query.isError ? errorMessage(query.error) : null}
        empty={
          <EmptyState
            icon="fas fa-user-slash"
            title="Nenhum aluno encontrado"
            description="Ajuste os termos da busca ou cadastre um novo aluno."
          />
        }
      />

      {totalPaginas > 1 && (
        <nav
          className="d-flex align-items-center justify-content-between mt-3"
          aria-label="Paginação de alunos"
        >
          <span className="text-down-01 text-secondary">
            Página {pagina} de {totalPaginas} · {total} aluno(s)
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

      <AlunoFormModal open={formAberto} onClose={() => setFormAberto(false)} />
    </>
  );
}
