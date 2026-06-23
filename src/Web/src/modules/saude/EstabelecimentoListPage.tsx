// Tela de LISTA + BUSCA de estabelecimentos de saúde (CNES — UBS/UPA/Hospital/...).
// Espelha o endpoint REAL GET /saude/estabelecimentos?termo&tipo&situacao&pagina&tamanho
// (ResultadoPaginado<EstabelecimentoItemLista>), gated em "saude.ver"; o cadastro é
// gated em "saude.gerenciar". Padrão-ouro: form de busca em Card, DataTable com estados
// loading/vazio/erro, paginação 1-based (keepPreviousData).
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
import { useBuscarEstabelecimentos } from './api';
import type { EstabelecimentoBuscaFiltro, EstabelecimentoItemLista } from './api';
import {
  opcoesSituacaoCadastro,
  opcoesTipoEstabelecimento,
  situacaoCadastroVariant,
} from './saude.helpers';
import { EstabelecimentoFormModal } from './EstabelecimentoFormModal';
import { SaudeSubNav } from './SaudeSubNav';

const TAMANHO_PAGINA = 20;

export function EstabelecimentoListPage() {
  const [termoCampo, setTermoCampo] = useState('');
  const [tipoCampo, setTipoCampo] = useState('');
  const [situacaoCampo, setSituacaoCampo] = useState('');
  const [termo, setTermo] = useState('');
  const [tipo, setTipo] = useState('');
  const [situacao, setSituacao] = useState('');
  const [pagina, setPagina] = useState(1);
  const [formAberto, setFormAberto] = useState(false);

  const filtro = useMemo<EstabelecimentoBuscaFiltro>(
    () => ({
      termo: termo || undefined,
      tipo: tipo || undefined,
      situacao: situacao || undefined,
      pagina,
      tamanho: TAMANHO_PAGINA,
    }),
    [termo, tipo, situacao, pagina],
  );

  const query = useBuscarEstabelecimentos(filtro);

  function aplicarBusca(event: FormEvent): void {
    event.preventDefault();
    setPagina(1);
    setTermo(termoCampo.trim());
    setTipo(tipoCampo);
    setSituacao(situacaoCampo);
  }

  const total = query.data?.total ?? 0;
  const totalPaginas = total > 0 ? Math.ceil(total / TAMANHO_PAGINA) : 0;

  const columns: Column<EstabelecimentoItemLista>[] = [
    {
      key: 'cnes',
      header: 'CNES',
      sortAccessor: (e) => e.cnes,
      render: (e) => e.cnes,
    },
    {
      key: 'nome',
      header: 'Nome',
      sortAccessor: (e) => e.nome.toLowerCase(),
      render: (e) => e.nome,
    },
    { key: 'tipo', header: 'Tipo', sortAccessor: (e) => e.tipo, render: (e) => e.tipo },
    {
      key: 'municipio',
      header: 'Município/UF',
      sortAccessor: (e) => e.municipio,
      render: (e) => `${e.municipio || '—'}/${e.uf || '—'}`,
    },
    {
      key: 'situacao',
      header: 'Situação',
      sortAccessor: (e) => e.situacao,
      render: (e) => <Tag variant={situacaoCadastroVariant(e.situacao)}>{e.situacao}</Tag>,
    },
    {
      key: 'acoes',
      header: 'Ações',
      sticky: true,
      render: (e) => (
        <Link className="br-button secondary small" to={`/saude/estabelecimentos/${e.id}`}>
          <i className="fas fa-hospital" aria-hidden="true" /> Detalhes
        </Link>
      ),
    },
  ];

  return (
    <>
      <SaudeSubNav />
      <PageHeader
        eyebrow="Saúde"
        title="Estabelecimentos (CNES)"
        description="Unidades de saúde do município — UBS, UPA, hospitais, CAPS, farmácias e demais tipos do CNES."
        actions={
          <Can permission="saude.gerenciar">
            <Toolbar>
              <Button variant="primary" onClick={() => setFormAberto(true)}>
                <i className="fas fa-plus" aria-hidden="true" /> Cadastrar estabelecimento
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
                <FormField label="Buscar estabelecimento" help="Nome (trecho) ou CNES.">
                  {({ id, describedBy }) => (
                    <Input
                      id={id}
                      aria-describedby={describedBy}
                      value={termoCampo}
                      onChange={(e) => setTermoCampo(e.target.value)}
                      placeholder="Ex.: UBS Central ou 0000000"
                    />
                  )}
                </FormField>
              </div>
              <div className="col-12 col-md-3">
                <FormField label="Tipo">
                  {({ id }) => (
                    <Select
                      id={id}
                      options={opcoesTipoEstabelecimento}
                      placeholder="Todos"
                      value={tipoCampo}
                      onChange={(e) => setTipoCampo(e.target.value)}
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
        caption="Estabelecimentos de saúde (CNES)"
        columns={columns}
        rows={query.data?.itens}
        rowKey={(e) => e.id}
        loading={query.isLoading}
        error={query.isError ? errorMessage(query.error) : null}
        empty={
          <EmptyState
            icon="fas fa-hospital"
            title="Nenhum estabelecimento encontrado"
            description="Ajuste os termos da busca ou cadastre um novo estabelecimento."
          />
        }
      />

      {totalPaginas > 1 && (
        <nav
          className="d-flex align-items-center justify-content-between mt-3"
          aria-label="Paginação de estabelecimentos"
        >
          <span className="text-down-01 text-secondary">
            Página {pagina} de {totalPaginas} · {total} estabelecimento(s)
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

      <EstabelecimentoFormModal open={formAberto} onClose={() => setFormAberto(false)} />
    </>
  );
}
