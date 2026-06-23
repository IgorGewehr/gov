// Tela de LISTA de servidores ativos do tenant (página de entrada do módulo RH).
// Padrão-ouro (espelha DividaAtivaListPage): DataTable com estados loading/vazio/erro
// + ordenação, link para detalhe e abertura do formulário de admissão (mutation).
import { useState } from 'react';
import { Link } from 'react-router-dom';
import {
  Button,
  DataTable,
  EmptyState,
  PageHeader,
  Tag,
  Toolbar,
} from '../../components/ui';
import type { Column } from '../../components/ui';
import { errorMessage } from '../../components/ui';
import { Can } from '../../auth/Can';
import { formatarData } from '../../i18n/format';
import { useServidoresAtivos } from './api';
import type { ServidorResumo } from './api';
import {
  formatarRegimePrev,
  PERM_RH_GERENCIAR,
  situacaoServidorTagVariant,
} from './recursosHumanos.helpers';
import { AdmitirServidorFormModal } from './AdmitirServidorFormModal';
import { RhSubNav } from './RhSubNav';

export function ServidoresListPage() {
  const [formAberto, setFormAberto] = useState(false);
  const query = useServidoresAtivos();

  const columns: Column<ServidorResumo>[] = [
    {
      key: 'matricula',
      header: 'Matrícula',
      sortAccessor: (s) => s.matricula,
      render: (s) => <span className="text-semi-bold">{s.matricula}</span>,
    },
    {
      key: 'nome',
      header: 'Servidor',
      sortAccessor: (s) => s.nomeServidor,
      render: (s) => s.nomeServidor,
    },
    { key: 'cpf', header: 'CPF', render: (s) => s.cpf },
    {
      key: 'regime',
      header: 'Regime',
      sortAccessor: (s) => s.regime,
      render: (s) => formatarRegimePrev(s.regime),
    },
    {
      key: 'situacao',
      header: 'Situação',
      sortAccessor: (s) => s.situacao,
      render: (s) => (
        <Tag variant={situacaoServidorTagVariant(s.situacao)}>{s.situacao}</Tag>
      ),
    },
    {
      key: 'nomeacao',
      header: 'Nomeação',
      align: 'end',
      sortAccessor: (s) => s.dataNomeacao,
      render: (s) => formatarData(s.dataNomeacao),
    },
    {
      key: 'acoes',
      header: 'Ações',
      sticky: true,
      render: (s) => (
        <Link
          className="br-button secondary small"
          to={`/recursoshumanos/servidores/${encodeURIComponent(s.matricula)}`}
        >
          Detalhes
        </Link>
      ),
    },
  ];

  return (
    <>
      <RhSubNav />
      <PageHeader
        eyebrow="Recursos Humanos"
        title="Servidores"
        description="Quadro de pessoal ativo do órgão (situação diferente de Desligado)."
        actions={
          <Can permission={PERM_RH_GERENCIAR}>
            <Toolbar>
              <Button variant="primary" onClick={() => setFormAberto(true)}>
                <i className="fas fa-user-plus" aria-hidden="true" /> Admitir servidor
              </Button>
            </Toolbar>
          </Can>
        }
      />

      <DataTable
        caption="Servidores ativos do órgão"
        columns={columns}
        rows={query.data}
        rowKey={(s) => s.id}
        loading={query.isLoading}
        error={query.isError ? errorMessage(query.error) : null}
        empty={
          <EmptyState
            icon="fas fa-users"
            title="Nenhum servidor ativo"
            description="Cadastre um servidor com o botão Admitir servidor."
          />
        }
      />

      <AdmitirServidorFormModal open={formAberto} onClose={() => setFormAberto(false)} />
    </>
  );
}
