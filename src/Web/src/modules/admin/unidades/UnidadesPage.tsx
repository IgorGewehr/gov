// Tela de ESTRUTURA ORGANIZACIONAL (Identidade / Administração do Sistema).
// Exibe a árvore hierárquica de Unidades Organizacionais (UOs) achatada em uma
// tabela, com indentação por nível; ações de criar raiz/subunidade, renomear,
// mover e ativar/desativar — TODAS escondidas pelo gating de permissão
// "identidade.usuarios.gerenciar" (<Can>). Segue o PADRÃO-OURO de UsuarioListPage
// (estados loading/erro/vazio, modais controlados).
import { useState } from 'react';
import {
  Button,
  DataTable,
  EmptyState,
  PageHeader,
  Tag,
} from '../../../components/ui';
import type { Column } from '../../../components/ui';
import { errorMessage } from '../../../components/ui';
import { Can } from '../../../auth/Can';
import { PERM_USUARIOS_GERENCIAR } from '../admin.permissoes';
import {
  useArvoreUnidades,
  useAtivarUnidade,
  useDesativarUnidade,
} from './unidades.api';
import type { NoUnidade } from './unidades.api';
import {
  achatarArvore,
  ativaLabel,
  ativaTagVariant,
  tipoLabel,
} from './unidades.helpers';
import type { UnidadeLinha } from './unidades.helpers';
import { UnidadeFormModal } from './UnidadeFormModal';
import { UnidadeMoverModal } from './UnidadeMoverModal';

export function UnidadesPage() {
  const query = useArvoreUnidades();
  const ativar = useAtivarUnidade();
  const desativar = useDesativarUnidade();

  const [formAberto, setFormAberto] = useState(false);
  const [emEdicao, setEmEdicao] = useState<NoUnidade | null>(null);
  const [paiDeNova, setPaiDeNova] = useState<NoUnidade | null>(null);
  const [moverAlvo, setMoverAlvo] = useState<NoUnidade | null>(null);

  const arvore = query.data ?? [];
  const linhas = achatarArvore(arvore);

  function abrirCriacaoRaiz(): void {
    setEmEdicao(null);
    setPaiDeNova(null);
    setFormAberto(true);
  }

  function abrirCriacaoFilha(pai: NoUnidade): void {
    setEmEdicao(null);
    setPaiDeNova(pai);
    setFormAberto(true);
  }

  function abrirEdicao(unidade: NoUnidade): void {
    setPaiDeNova(null);
    setEmEdicao(unidade);
    setFormAberto(true);
  }

  const columns: Column<UnidadeLinha>[] = [
    {
      key: 'nome',
      header: 'Unidade',
      render: ({ unidade, nivel }) => (
        <span style={{ paddingLeft: `${nivel * 1.5}rem` }}>
          {nivel > 0 && (
            <i className="fas fa-level-up-alt fa-rotate-90 mr-2 text-down-01" aria-hidden="true" />
          )}
          <span className="text-weight-semi-bold">{unidade.nome}</span>
        </span>
      ),
    },
    {
      key: 'codigo',
      header: 'Código',
      render: ({ unidade }) => <span className="text-mono">{unidade.codigo}</span>,
    },
    {
      key: 'tipo',
      header: 'Tipo',
      render: ({ unidade }) => tipoLabel(unidade.tipo),
    },
    {
      key: 'ativa',
      header: 'Status',
      render: ({ unidade }) => (
        <Tag variant={ativaTagVariant(unidade.ativa)}>{ativaLabel(unidade.ativa)}</Tag>
      ),
    },
    {
      key: 'acoes',
      header: 'Ações',
      render: ({ unidade }) => (
        <Can permission={PERM_USUARIOS_GERENCIAR}>
          <div className="d-flex" style={{ gap: '0.5rem', flexWrap: 'wrap' }}>
            <Button
              variant="tertiary"
              className="small"
              onClick={() => abrirCriacaoFilha(unidade)}
            >
              Subunidade
            </Button>
            <Button variant="tertiary" className="small" onClick={() => abrirEdicao(unidade)}>
              Editar
            </Button>
            <Button variant="tertiary" className="small" onClick={() => setMoverAlvo(unidade)}>
              Mover
            </Button>
            {unidade.ativa ? (
              <Button
                variant="tertiary"
                className="small"
                loading={desativar.isPending && desativar.variables === unidade.id}
                onClick={() => desativar.mutate(unidade.id)}
              >
                Desativar
              </Button>
            ) : (
              <Button
                variant="tertiary"
                className="small"
                loading={ativar.isPending && ativar.variables === unidade.id}
                onClick={() => ativar.mutate(unidade.id)}
              >
                Ativar
              </Button>
            )}
          </div>
        </Can>
      ),
    },
  ];

  return (
    <>
      <PageHeader
        title="Estrutura Organizacional"
        description="Hierarquia de unidades organizacionais (secretarias, departamentos, setores) do órgão."
        actions={
          <Can permission={PERM_USUARIOS_GERENCIAR}>
            <Button variant="primary" onClick={abrirCriacaoRaiz}>
              <i className="fas fa-plus" aria-hidden="true" /> Nova unidade raiz
            </Button>
          </Can>
        }
      />

      <DataTable
        caption="Unidades organizacionais do órgão"
        columns={columns}
        rows={linhas}
        rowKey={(linha) => linha.unidade.id}
        loading={query.isLoading}
        error={query.isError ? errorMessage(query.error) : null}
        empty={
          <EmptyState
            icon="fas fa-sitemap"
            title="Nenhuma unidade cadastrada"
            description="Cadastre a primeira unidade raiz (ex.: uma secretaria) para montar a estrutura do órgão."
          />
        }
      />

      <UnidadeFormModal
        open={formAberto}
        onClose={() => setFormAberto(false)}
        unidade={emEdicao}
        pai={paiDeNova}
      />
      <UnidadeMoverModal
        open={moverAlvo !== null}
        onClose={() => setMoverAlvo(null)}
        unidade={moverAlvo}
        arvore={arvore}
      />
    </>
  );
}
