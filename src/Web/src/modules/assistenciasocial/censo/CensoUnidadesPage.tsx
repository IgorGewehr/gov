// Lista das unidades socioassistenciais (query ListarUnidadesSocioassistenciais).
// Cadastro de nova unidade e configuracao (servico + equipe) por unidade — gating gerenciar.
import { useState } from 'react';
import {
  Button,
  DataTable,
  EmptyState,
  PageHeader,
  Tag,
  Toolbar,
  errorMessage,
} from '../../../components/ui';
import type { Column } from '../../../components/ui';
import { Can } from '../../../auth/Can';
import { AssistenciaSocialSubNav } from '../AssistenciaSocialSubNav';
import { useUnidades } from './censo.api';
import type { UnidadeSocioassistencial } from './censo.api';
import { servicoLabel, tipoUnidadeSigla } from './censo.helpers';
import { CadastrarUnidadeModal } from './CadastrarUnidadeModal';
import { ConfigurarUnidadeModal } from './ConfigurarUnidadeModal';

export function CensoUnidadesPage() {
  const query = useUnidades();
  const [cadastrarAberto, setCadastrarAberto] = useState(false);
  const [configurar, setConfigurar] = useState<UnidadeSocioassistencial | null>(null);

  const columns: Column<UnidadeSocioassistencial>[] = [
    {
      key: 'nome',
      header: 'Unidade',
      sortAccessor: (u) => u.nome,
      render: (u) => u.nome,
    },
    {
      key: 'tipo',
      header: 'Tipo',
      sortAccessor: (u) => u.tipo,
      render: (u) => <Tag variant="info">{tipoUnidadeSigla(u.tipo)}</Tag>,
    },
    {
      key: 'territorio',
      header: 'Território',
      sortAccessor: (u) => u.territorioCobertura,
      render: (u) => u.territorioCobertura,
    },
    {
      key: 'equipe',
      header: 'Equipe',
      align: 'end',
      sortAccessor: (u) => u.quantidadeProfissionais,
      render: (u) => u.quantidadeProfissionais,
    },
    {
      key: 'servicos',
      header: 'Serviços ofertados',
      render: (u) =>
        u.servicos.length === 0
          ? '—'
          : u.servicos.map((s) => `${servicoLabel(s.servico)} (${s.capacidadeMensal})`).join(', '),
    },
    {
      key: 'acoes',
      header: 'Ações',
      render: (u) => (
        <Can permission="assistenciasocial.gerenciar">
          <Button variant="tertiary" size="sm" onClick={() => setConfigurar(u)}>
            Configurar
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
        title="Unidades socioassistenciais"
        description="Cadastro das unidades CRAS/CREAS/Centro POP, com serviços tipificados ofertados e equipe de referência — base do Censo SUAS."
        actions={
          <Can permission="assistenciasocial.gerenciar">
            <Toolbar>
              <Button variant="primary" onClick={() => setCadastrarAberto(true)}>
                <i className="fas fa-plus" aria-hidden="true" /> Cadastrar unidade
              </Button>
            </Toolbar>
          </Can>
        }
      />

      <DataTable
        caption="Unidades socioassistenciais cadastradas"
        columns={columns}
        rows={query.data}
        rowKey={(u) => u.id}
        loading={query.isLoading}
        error={query.isError ? errorMessage(query.error) : null}
        empty={
          <EmptyState
            icon="fas fa-building"
            title="Nenhuma unidade cadastrada"
            description="Cadastre a primeira unidade socioassistencial para iniciar o Censo SUAS."
          />
        }
      />

      <CadastrarUnidadeModal open={cadastrarAberto} onClose={() => setCadastrarAberto(false)} />

      {configurar && (
        <ConfigurarUnidadeModal open onClose={() => setConfigurar(null)} unidade={configurar} />
      )}
    </>
  );
}
