// Tela de LISTA/CONSULTA dos beneficios de uma familia (query ObterBeneficiosDaFamilia).
// Busca sob demanda (enabled), DataTable com colunas + ordenacao + estados loading/vazio/erro,
// link para o detalhe (que carrega a partir da mesma familia) e abertura do formulario de avaliacao.
import { useState } from 'react';
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
  Tag,
  Toolbar,
  errorMessage,
} from '../../../components/ui';
import type { Column } from '../../../components/ui';
import { formatarData, formatarMoeda } from '../../../i18n/format';
import { useBeneficiosDaFamilia } from './beneficio.api';
import type { BeneficioResumo } from './beneficio.api';
import { situacaoLabel, situacaoTagVariant, tipoLabel } from './beneficio.helpers';
import { BeneficioFormModal } from './BeneficioFormModal';
import { AssistenciaSocialSubNav } from '../AssistenciaSocialSubNav';
import { Can } from '../../../auth/Can';

export function BeneficioListPage() {
  const [familiaId, setFamiliaId] = useState('');
  const [consultaAtiva, setConsultaAtiva] = useState('');
  const [formAberto, setFormAberto] = useState(false);

  const query = useBeneficiosDaFamilia(consultaAtiva, consultaAtiva.length > 0);

  function consultar(event: FormEvent): void {
    event.preventDefault();
    setConsultaAtiva(familiaId.trim());
  }

  const columns: Column<BeneficioResumo>[] = [
    {
      key: 'tipo',
      header: 'Tipo',
      sortAccessor: (b) => b.tipo,
      render: (b) => tipoLabel(b.tipo),
    },
    {
      key: 'situacao',
      header: 'Situação',
      sortAccessor: (b) => b.situacao,
      render: (b) => <Tag variant={situacaoTagVariant(b.situacao)}>{situacaoLabel(b.situacao)}</Tag>,
    },
    {
      key: 'competencia',
      header: 'Competência',
      sortAccessor: (b) => b.competencia,
      render: (b) => b.competencia,
    },
    {
      key: 'valor',
      header: 'Valor',
      align: 'end',
      sortAccessor: (b) => b.valor ?? -1,
      render: (b) => (b.valor != null ? formatarMoeda(b.valor) : '—'),
    },
    {
      key: 'decisao',
      header: 'Data da decisão',
      sortAccessor: (b) => b.dataDecisao ?? '',
      render: (b) => formatarData(b.dataDecisao),
    },
    {
      key: 'motivo',
      header: 'Motivo (indeferimento)',
      render: (b) => b.motivoIndeferimento ?? '—',
    },
    {
      key: 'acoes',
      header: 'Ações',
      render: (b) => (
        <Link
          className="br-button tertiary small"
          to={`/assistenciasocial/familias/${b.familiaId}/beneficios/${b.id}`}
        >
          Detalhes
        </Link>
      ),
    },
  ];

  return (
    <>
      <AssistenciaSocialSubNav />
      <PageHeader
        eyebrow="Assistência Social"
        title="Benefícios da família"
        description="Consulte os benefícios socioassistenciais (BPC/PBF/eventuais) de uma família."
        actions={
          <Can permission="assistenciasocial.gerenciar">
            <Toolbar>
              <Button variant="primary" onClick={() => setFormAberto(true)}>
                <i className="fas fa-plus" aria-hidden="true" /> Avaliar elegibilidade
              </Button>
            </Toolbar>
          </Can>
        }
      />

      <Card className="mb-4">
        <form className="br-form" onSubmit={consultar}>
          <FormRow
            acao={
              <Button
                variant="primary"
                type="submit"
                disabled={familiaId.trim() === ''}
                loading={query.isFetching}
              >
                Consultar
              </Button>
            }
          >
            <FormField label="Identificador da família" required>
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  aria-describedby={describedBy}
                  invalid={invalid}
                  value={familiaId}
                  onChange={(e) => setFamiliaId(e.target.value)}
                  placeholder="00000000-0000-0000-0000-000000000000"
                />
              )}
            </FormField>
          </FormRow>
        </form>
      </Card>

      {consultaAtiva === '' ? (
        <EmptyState
          icon="fas fa-magnifying-glass"
          title="Faça uma consulta"
          description="Informe o identificador da família e clique em Consultar."
        />
      ) : (
        <DataTable
          caption={`Benefícios da família ${consultaAtiva}`}
          columns={columns}
          rows={query.data}
          rowKey={(b) => b.id}
          loading={query.isLoading}
          error={query.isError ? errorMessage(query.error) : null}
          empty={
            <EmptyState
              icon="fas fa-folder-open"
              title="Nenhum benefício encontrado"
              description="Esta família ainda não possui benefícios avaliados."
            />
          }
        />
      )}

      <BeneficioFormModal
        open={formAberto}
        onClose={() => setFormAberto(false)}
        familiaIdInicial={consultaAtiva}
      />
    </>
  );
}
