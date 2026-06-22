// Tela de BUSCA de Normas (acervo legislativo consolidado). Filtros termo/tipo/ano
// com submissao explicita, DataTable paginada com estados, link para detalhe e
// abertura do formulario de cadastro (gated `legislativo.normas.gerenciar`).
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Link } from 'react-router-dom';
import {
  Button,
  Card,
  DataTable,
  EmptyState,
  FormField,
  Input,
  PageHeader,
  Select,
  Tag,
} from '../../components/ui';
import type { Column } from '../../components/ui';
import { errorMessage } from '../../components/ui';
import { formatarData } from '../../i18n/format';
import { Can } from '../../auth/Can';
import { TIPOS_NORMA, useBuscaNormas } from './api';
import type { BuscaNormasFiltro, NormaResumo } from './api';
import { situacaoNormaTagVariant } from './legislativo.helpers';
import { LegislativoSecoesNav } from './LegislativoSecoesNav';
import { NormaFormModal } from './NormaFormModal';

const FILTRO_INICIAL: BuscaNormasFiltro = { termo: '', tipo: '', ano: '', pagina: 1 };

export function NormaListPage() {
  const [termo, setTermo] = useState('');
  const [tipo, setTipo] = useState('');
  const [ano, setAno] = useState('');
  const [filtro, setFiltro] = useState<BuscaNormasFiltro>(FILTRO_INICIAL);
  const [formAberto, setFormAberto] = useState(false);

  const query = useBuscaNormas(filtro);
  const totalPaginas = query.data ? Math.max(1, Math.ceil(query.data.total / query.data.tamanhoPagina)) : 1;

  function buscar(event: FormEvent): void {
    event.preventDefault();
    setFiltro({ termo: termo.trim(), tipo, ano: ano.trim(), pagina: 1 });
  }

  function irParaPagina(pagina: number): void {
    setFiltro((atual) => ({ ...atual, pagina }));
  }

  const columns: Column<NormaResumo>[] = [
    { key: 'tipo', header: 'Tipo', sortAccessor: (n) => n.tipo, render: (n) => n.tipo },
    {
      key: 'numero',
      header: 'Número/Ano',
      sortAccessor: (n) => `${n.ano}-${n.numero}`,
      render: (n) => `${n.numero}/${n.ano}`,
    },
    { key: 'ementa', header: 'Ementa', render: (n) => n.ementa },
    {
      key: 'situacao',
      header: 'Situação',
      sortAccessor: (n) => n.situacao,
      render: (n) => <Tag variant={situacaoNormaTagVariant(n.situacao)}>{n.situacao}</Tag>,
    },
    {
      key: 'publicacao',
      header: 'Publicação',
      sortAccessor: (n) => n.dataPublicacao,
      render: (n) => formatarData(n.dataPublicacao),
    },
    {
      key: 'acoes',
      header: 'Ações',
      render: (n) => (
        <Link className="br-button tertiary small" to={`/legislativo/normas/${n.id}`}>
          Detalhes
        </Link>
      ),
    },
  ];

  return (
    <>
      <PageHeader
        title="Normas"
        description="Pesquise o acervo de leis, decretos e resoluções da Câmara."
        actions={
          <Can permission="legislativo.normas.gerenciar">
            <Button variant="primary" onClick={() => setFormAberto(true)}>
              <i className="fas fa-plus" aria-hidden="true" /> Cadastrar norma
            </Button>
          </Can>
        }
      />

      <LegislativoSecoesNav />

      <Card className="mb-4">
        <form className="br-form" onSubmit={buscar}>
          <div className="row align-items-end">
            <div className="col-sm-6 col-md-5">
              <FormField label="Termo">
                {({ id, describedBy }) => (
                  <Input
                    id={id}
                    aria-describedby={describedBy}
                    value={termo}
                    onChange={(e) => setTermo(e.target.value)}
                    placeholder="Palavra na ementa ou número"
                  />
                )}
              </FormField>
            </div>
            <div className="col-sm-6 col-md-3">
              <FormField label="Tipo">
                {({ id, describedBy }) => (
                  <Select
                    id={id}
                    aria-describedby={describedBy}
                    value={tipo}
                    onChange={(e) => setTipo(e.target.value)}
                    options={[
                      { value: '', label: 'Todos' },
                      ...TIPOS_NORMA.map((t) => ({ value: String(t.value), label: t.label })),
                    ]}
                  />
                )}
              </FormField>
            </div>
            <div className="col-sm-4 col-md-2">
              <FormField label="Ano">
                {({ id, describedBy }) => (
                  <Input
                    id={id}
                    aria-describedby={describedBy}
                    type="number"
                    value={ano}
                    onChange={(e) => setAno(e.target.value)}
                    placeholder="Ex.: 2026"
                  />
                )}
              </FormField>
            </div>
            <div className="col-auto mb-3">
              <Button variant="primary" type="submit">
                <i className="fas fa-search" aria-hidden="true" /> Buscar
              </Button>
            </div>
          </div>
        </form>
      </Card>

      <DataTable
        caption="Resultado da busca de normas"
        columns={columns}
        rows={query.data?.itens}
        rowKey={(n) => n.id}
        loading={query.isLoading}
        error={query.isError ? errorMessage(query.error) : null}
        empty={
          <EmptyState
            icon="fas fa-scale-balanced"
            title="Nenhuma norma encontrada"
            description="Ajuste os filtros de busca ou cadastre uma nova norma."
          />
        }
      />

      {query.data && query.data.total > 0 && (
        <nav className="d-flex align-items-center mt-3" aria-label="Paginação" style={{ gap: '0.75rem' }}>
          <Button
            variant="secondary"
            onClick={() => irParaPagina(filtro.pagina - 1)}
            disabled={filtro.pagina <= 1 || query.isFetching}
          >
            Anterior
          </Button>
          <span aria-live="polite">
            Página {filtro.pagina} de {totalPaginas} ({query.data.total} normas)
          </span>
          <Button
            variant="secondary"
            onClick={() => irParaPagina(filtro.pagina + 1)}
            disabled={filtro.pagina >= totalPaginas || query.isFetching}
          >
            Próxima
          </Button>
        </nav>
      )}

      <NormaFormModal open={formAberto} onClose={() => setFormAberto(false)} />
    </>
  );
}
