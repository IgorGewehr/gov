// Tela de Configuração de Módulos licenciados do tenant (só-admin).
// LISTA os módulos do tenant (GET /api/admin/tenants/{tenantId}/modulos) em uma
// DataTable acessível, com um SWITCH (br-switch, role="switch") por linha que
// ativa/desativa o módulo (PUT .../modulos/{modulo} { ativo }).
//
// Gating: a área inteira exige a permissão "admin.modulos.configurar"
// (PERM_MODULOS_CONFIGURAR). O tenantId vem SEMPRE da sessão (user.tenantId),
// nunca da UI — preservando o isolamento multi-tenant (CLAUDE.md §5).
import { useState } from 'react';
import {
  Alert,
  Card,
  DataTable,
  EmptyState,
  PageHeader,
  Tag,
} from '../../../components/ui';
import type { Column } from '../../../components/ui';
import { errorMessage } from '../../../components/ui';
import { useToast } from '../../../components/ui';
import { Can } from '../../../auth/Can';
import { useAuth } from '../../../auth/useAuth';
import { PERM_MODULOS_CONFIGURAR } from '../admin.permissoes';
import { useDefinirModulo, useModulosDoTenant } from './modulos.api';
import type { TenantModulo } from './modulos.api';
import {
  moduloIcone,
  moduloLabel,
  ordenarPorRotulo,
  statusLabel,
  statusTagVariant,
} from './modulos.helpers';

export function ModulosConfigPage() {
  const { user } = useAuth();
  const tenantId = user?.tenantId ?? '';

  return (
    <Can
      permission={PERM_MODULOS_CONFIGURAR}
      fallback={
        <>
          <PageHeader
            title="Módulos do órgão"
            description="Ative ou desative os módulos licenciados para o seu órgão."
          />
          <Alert variant="warning" title="Acesso restrito">
            Você não possui a permissão necessária para configurar os módulos do órgão.
          </Alert>
        </>
      }
    >
      <ModulosConfigConteudo tenantId={tenantId} tenantNome={user?.tenantNome ?? null} />
    </Can>
  );
}

interface ConteudoProps {
  tenantId: string;
  tenantNome: string | null;
}

function ModulosConfigConteudo({ tenantId, tenantNome }: ConteudoProps) {
  const toast = useToast();
  const query = useModulosDoTenant(tenantId, tenantId.length > 0);
  const definir = useDefinirModulo(tenantId);
  // Guarda o módulo cuja mutação está em curso para desabilitar só o seu switch.
  const [moduloEmAlteracao, setModuloEmAlteracao] = useState<string | null>(null);

  function alternar(modulo: TenantModulo): void {
    const proximoAtivo = !modulo.ativo;
    setModuloEmAlteracao(modulo.modulo);
    definir.mutate(
      { modulo: modulo.modulo, ativo: proximoAtivo },
      {
        onSuccess: () => {
          toast.success(
            `Módulo ${moduloLabel(modulo.modulo)} ${proximoAtivo ? 'ativado' : 'desativado'}.`,
          );
        },
        onError: (error) => {
          toast.error(errorMessage(error), 'Falha ao atualizar o módulo');
        },
        onSettled: () => {
          setModuloEmAlteracao(null);
        },
      },
    );
  }

  const linhas = query.data ? ordenarPorRotulo(query.data) : query.data;

  const columns: Column<TenantModulo>[] = [
    {
      key: 'modulo',
      header: 'Módulo',
      sortAccessor: (m) => moduloLabel(m.modulo),
      render: (m) => (
        <span>
          <i className={moduloIcone(m.modulo)} aria-hidden="true" /> {moduloLabel(m.modulo)}
        </span>
      ),
    },
    {
      key: 'status',
      header: 'Status',
      sortAccessor: (m) => statusLabel(m.ativo),
      render: (m) => <Tag variant={statusTagVariant(m.ativo)}>{statusLabel(m.ativo)}</Tag>,
    },
    {
      key: 'acoes',
      header: 'Ativação',
      align: 'end',
      render: (m) => {
        const ocupado = definir.isPending && moduloEmAlteracao === m.modulo;
        const inputId = `switch-modulo-${m.modulo}`;
        return (
          <div className="br-switch" aria-busy={ocupado || undefined}>
            <input
              id={inputId}
              type="checkbox"
              role="switch"
              checked={m.ativo}
              disabled={ocupado || definir.isPending}
              onChange={() => alternar(m)}
            />
            <label htmlFor={inputId}>
              {moduloLabel(m.modulo)} — {m.ativo ? 'desativar' : 'ativar'}
            </label>
          </div>
        );
      },
    },
  ];

  return (
    <>
      <PageHeader
        title="Módulos do órgão"
        description={
          tenantNome
            ? `Ative ou desative os módulos licenciados para ${tenantNome}.`
            : 'Ative ou desative os módulos licenciados para o seu órgão.'
        }
      />

      {tenantId === '' ? (
        <Alert variant="danger" title="Sessão sem órgão">
          Não foi possível identificar o órgão (tenant) da sua sessão. Faça login novamente.
        </Alert>
      ) : (
        <Card>
          <DataTable
            caption="Módulos licenciados do órgão"
            columns={columns}
            rows={linhas}
            rowKey={(m) => m.modulo}
            loading={query.isLoading}
            error={query.isError ? errorMessage(query.error) : null}
            empty={
              <EmptyState
                icon="fas fa-toggle-on"
                title="Nenhum módulo disponível"
                description="Não há módulos licenciados para configurar neste órgão."
              />
            }
          />
        </Card>
      )}
    </>
  );
}
