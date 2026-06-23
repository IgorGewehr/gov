// Tela de LISTA + BUSCA de profissionais de saúde (CBO/CRM + vínculos CNES).
// Espelha o endpoint REAL GET /saude/profissionais?termo&cbo&estabelecimentoId&situacao&pagina&tamanho
// (ResultadoPaginado<ProfissionalItemLista>), gated em "saude.ver"; cadastro em "saude.gerenciar".
// LGPD: a lista NÃO traz CPF (minimização). Padrão-ouro: form de busca em Card, DataTable
// com estados loading/vazio/erro, paginação 1-based (keepPreviousData).
import { useMemo, useState } from 'react';
import type { FormEvent } from 'react';
import { Link } from 'react-router-dom';
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
import { useBuscarProfissionais } from './api';
import type { ProfissionalBuscaFiltro, ProfissionalItemLista } from './api';
import { opcoesSituacaoCadastro, situacaoCadastroVariant } from './saude.helpers';
import { ProfissionalFormModal } from './ProfissionalFormModal';
import { SaudeSubNav } from './SaudeSubNav';

const TAMANHO_PAGINA = 20;

export function ProfissionalListPage() {
  const [termoCampo, setTermoCampo] = useState('');
  const [cboCampo, setCboCampo] = useState('');
  const [situacaoCampo, setSituacaoCampo] = useState('');
  const [termo, setTermo] = useState('');
  const [cbo, setCbo] = useState('');
  const [situacao, setSituacao] = useState('');
  const [pagina, setPagina] = useState(1);
  const [formAberto, setFormAberto] = useState(false);

  const filtro = useMemo<ProfissionalBuscaFiltro>(
    () => ({
      termo: termo || undefined,
      cbo: cbo || undefined,
      situacao: situacao || undefined,
      pagina,
      tamanho: TAMANHO_PAGINA,
    }),
    [termo, cbo, situacao, pagina],
  );

  const query = useBuscarProfissionais(filtro);

  function aplicarBusca(event: FormEvent): void {
    event.preventDefault();
    setPagina(1);
    setTermo(termoCampo.trim());
    setCbo(cboCampo.replace(/\D/g, ''));
    setSituacao(situacaoCampo);
  }

  const total = query.data?.total ?? 0;
  const totalPaginas = total > 0 ? Math.ceil(total / TAMANHO_PAGINA) : 0;

  const columns: Column<ProfissionalItemLista>[] = [
    {
      key: 'nome',
      header: 'Nome',
      sortAccessor: (p) => p.nome.toLowerCase(),
      render: (p) => p.nome,
    },
    {
      key: 'conselho',
      header: 'Conselho',
      sortAccessor: (p) => p.conselho ?? '',
      render: (p) => p.conselho ?? '—',
    },
    {
      key: 'vinculos',
      header: 'Vínculos ativos',
      sortAccessor: (p) => p.qtdVinculosAtivos,
      render: (p) => p.qtdVinculosAtivos,
    },
    {
      key: 'situacao',
      header: 'Situação',
      sortAccessor: (p) => p.situacao,
      render: (p) => <Tag variant={situacaoCadastroVariant(p.situacao)}>{p.situacao}</Tag>,
    },
    {
      key: 'acoes',
      header: 'Ações',
      sticky: true,
      render: (p) => (
        <Link className="br-button secondary small" to={`/saude/profissionais/${p.id}`}>
          <i className="fas fa-user-doctor" aria-hidden="true" /> Ficha
        </Link>
      ),
    },
  ];

  return (
    <>
      <SaudeSubNav />
      <PageHeader
        eyebrow="Saúde"
        title="Profissionais"
        description="Profissionais de saúde, registros de conselho (CRM/COREN/...) e vínculos CNES/CBO."
        actions={
          <Can permission="saude.gerenciar">
            <Toolbar>
              <Button variant="primary" onClick={() => setFormAberto(true)}>
                <i className="fas fa-plus" aria-hidden="true" /> Cadastrar profissional
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
              <div className="col-12 col-md-6">
                <FormField label="Buscar profissional" help="Nome (trecho) ou CPF (dígitos).">
                  {({ id, describedBy }) => (
                    <Input
                      id={id}
                      aria-describedby={describedBy}
                      value={termoCampo}
                      onChange={(e) => setTermoCampo(e.target.value)}
                      placeholder="Ex.: Dr. João ou 000.000.000-00"
                    />
                  )}
                </FormField>
              </div>
              <div className="col-12 col-md-3">
                <FormField label="CBO" help="6 dígitos.">
                  {({ id, describedBy }) => (
                    <Input
                      id={id}
                      aria-describedby={describedBy}
                      inputMode="numeric"
                      maxLength={6}
                      value={cboCampo}
                      onChange={(e) => setCboCampo(e.target.value.replace(/\D/g, ''))}
                      placeholder="225125"
                    />
                  )}
                </FormField>
              </div>
              <div className="col-12 col-md-3">
                <FormField label="Situação">
                  {({ id }) => (
                    <Select
                      id={id}
                      options={opcoesSituacaoCadastro}
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
        caption="Profissionais de saúde"
        columns={columns}
        rows={query.data?.itens}
        rowKey={(p) => p.id}
        loading={query.isLoading}
        error={query.isError ? errorMessage(query.error) : null}
        empty={
          <EmptyState
            icon="fas fa-user-doctor"
            title="Nenhum profissional encontrado"
            description="Ajuste os termos da busca ou cadastre um novo profissional."
          />
        }
      />

      {totalPaginas > 1 && (
        <nav
          className="d-flex align-items-center justify-content-between mt-3"
          aria-label="Paginação de profissionais"
        >
          <span className="text-down-01 text-secondary">
            Página {pagina} de {totalPaginas} · {total} profissional(is)
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

      <ProfissionalFormModal open={formAberto} onClose={() => setFormAberto(false)} />
    </>
  );
}
