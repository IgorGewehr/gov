// MEUS PROTOCOLOS: processos publicos de que o PROPRIO cidadao e o interessado (dado
// resolvido server-side; processos restritos/sigilosos NAO sao expostos no portal).
// Endpoint: GET /api/cidadao/meus-processos.
import { useQuery } from '@tanstack/react-query';
import { Card, DataTable, EmptyState, PageHeader, Tag } from '../../components/ui';
import type { Column } from '../../components/ui';
import { errorMessage } from '../../components/ui';
import { cidadaoApi } from './cidadaoApi';
import type { MeuProcesso } from './cidadaoApi';
import { formatarData } from './cidadao.helpers';

export function MeusProcessosPage() {
  const processos = useQuery({
    queryKey: ['cidadao', 'meus-processos'],
    queryFn: () => cidadaoApi.meusProcessos(),
  });

  const colunas: Column<MeuProcesso>[] = [
    { key: 'nup', header: 'Protocolo (NUP)', render: (p) => p.nup, sortAccessor: (p) => p.nup },
    {
      key: 'classificacao',
      header: 'Assunto',
      render: (p) => p.classificacao,
      sortAccessor: (p) => p.classificacao,
    },
    {
      key: 'autuacao',
      header: 'Aberto em',
      render: (p) => formatarData(p.dataAutuacao),
      sortAccessor: (p) => p.dataAutuacao,
    },
    { key: 'situacao', header: 'Situacao', render: (p) => <Tag variant="info">{p.situacao}</Tag> },
  ];

  return (
    <>
      <PageHeader
        title="Meus protocolos"
        description="Processos administrativos publicos em que voce consta como interessado neste municipio."
      />
      <Card>
        <DataTable
          caption="Meus processos administrativos"
          columns={colunas}
          rows={processos.data}
          rowKey={(p) => p.processoId}
          loading={processos.isLoading}
          error={processos.isError ? errorMessage(processos.error) : null}
          empty={
            <EmptyState
              icon="fas fa-folder-open"
              title="Voce nao tem protocolos."
              description="Nenhum processo publico foi encontrado no seu nome."
            />
          }
        />
      </Card>
    </>
  );
}
