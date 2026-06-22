// Card com as CRÍTICAS (RDI — Relatório de Diagnóstico de Inconsistências) emitidas
// pelo e-Validador do TCE-RS após a validação da remessa. Lista código/severidade/
// registro/mensagem; destaca quando há erros que bloqueiam o empacotamento.
import { Card, DataTable, EmptyState, Tag, errorMessage } from '../../components/ui';
import type { Column } from '../../components/ui';
import { useRemessaCriticas } from './api';
import type { RemessaCritica } from './api';
import { severidadeCriticaTagVariant } from './transparencia.helpers';

export interface RemessaCriticasCardProps {
  id: string;
  /** Só busca o RDI após a validação ter ocorrido. */
  habilitado: boolean;
}

export function RemessaCriticasCard({ id, habilitado }: RemessaCriticasCardProps) {
  const query = useRemessaCriticas(id, habilitado);

  if (!habilitado) return null;

  const columns: Column<RemessaCritica>[] = [
    { key: 'codigo', header: 'Código', sortAccessor: (c) => c.codigo, render: (c) => c.codigo },
    {
      key: 'severidade',
      header: 'Severidade',
      sortAccessor: (c) => c.severidade,
      render: (c) => <Tag variant={severidadeCriticaTagVariant(c.severidade)}>{c.severidade}</Tag>,
    },
    { key: 'registro', header: 'Registro', render: (c) => c.registro ?? '—' },
    { key: 'mensagem', header: 'Mensagem', render: (c) => c.mensagem },
  ];

  return (
    <Card className="mt-4" header={<strong>Críticas do e-Validador (RDI)</strong>}>
      <DataTable
        caption="Ocorrências apontadas pelo RDI do TCE-RS"
        columns={columns}
        rows={query.data}
        rowKey={(c) => `${c.codigo}-${c.registro ?? ''}-${c.mensagem}`}
        loading={query.isLoading}
        error={query.isError ? errorMessage(query.error) : null}
        empty={
          <EmptyState
            icon="fas fa-circle-check"
            title="Sem inconsistências"
            description="O RDI não apontou ocorrências para esta remessa."
          />
        }
      />
    </Card>
  );
}
