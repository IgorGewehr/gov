// Ficha (detalhe) do Profissional de saúde. Param de rota -> useQuery + QueryState.
// Mostra identificação (CPF mascarado — LGPD), conselho, CRM ativo e a tabela de vínculos
// CNES/CBO. Ações gated por "saude.gerenciar": abrir vínculo, encerrar vínculo e inativar.
import { useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import {
  Button,
  Card,
  CardSecao,
  DataTable,
  EmptyState,
  PageHeader,
  QueryState,
  Tag,
  useToast,
} from '../../components/ui';
import type { Column } from '../../components/ui';
import { Can } from '../../auth/Can';
import { ApiError } from '../../api/problemDetails';
import { formatarData } from '../../i18n/format';
import { useInativarProfissional, useProfissional } from './api';
import type { ProfissionalDetalhe, VinculoCnes } from './api';
import { situacaoCadastroVariant } from './saude.helpers';
import {
  EncerrarVinculoModal,
  VincularProfissionalModal,
} from './ProfissionalVinculoModals';

export function ProfissionalDetailPage() {
  const { profissionalId = '' } = useParams<{ profissionalId: string }>();
  const toast = useToast();
  const query = useProfissional(profissionalId);
  const inativar = useInativarProfissional(profissionalId);

  const [vincularAberto, setVincularAberto] = useState(false);
  const [encerrarDe, setEncerrarDe] = useState<string | null>(null);

  function inativarProfissional(): void {
    inativar.mutate(undefined, {
      onSuccess: () => toast.success('Profissional inativado.', 'Sucesso'),
      onError: (error) =>
        toast.error(
          error instanceof ApiError ? error.userMessage : 'Não foi possível inativar o profissional.',
        ),
    });
  }

  const colunasVinculos: Column<VinculoCnes>[] = [
    {
      key: 'estabelecimento',
      header: 'Estabelecimento',
      render: (v) => (
        <Link to={`/saude/estabelecimentos/${v.estabelecimentoId}`}>{v.estabelecimentoId}</Link>
      ),
    },
    { key: 'cbo', header: 'CBO', sortAccessor: (v) => v.cbo, render: (v) => v.cbo },
    {
      key: 'inicio',
      header: 'Início',
      sortAccessor: (v) => v.dataInicio,
      render: (v) => formatarData(v.dataInicio),
    },
    {
      key: 'situacao',
      header: 'Situação',
      render: (v) =>
        v.dataFim ? (
          <Tag variant="default">Encerrado em {formatarData(v.dataFim)}</Tag>
        ) : (
          <Tag variant="success">Vigente</Tag>
        ),
    },
  ];

  return (
    <>
      <PageHeader
        eyebrow="Saúde · Profissionais"
        title="Ficha do profissional"
        description="Identificação, registro de conselho e vínculos CNES/CBO. CPF exibido com máscara (LGPD)."
        actions={
          <Link className="br-button secondary" to="/saude/profissionais">
            <i className="fas fa-arrow-left" aria-hidden="true" /> Voltar
          </Link>
        }
      />

      <QueryState<ProfissionalDetalhe>
        isLoading={query.isLoading}
        isError={query.isError}
        error={query.error}
        data={query.data}
      >
        {(profissional) => {
          const ativo = profissional.situacao === 'Ativo';
          return (
            <>
              <Can permission="saude.gerenciar">
                <Card className="mb-4" header={<strong>Ações</strong>}>
                  <div className="d-flex gap-2 flex-wrap">
                    <Button
                      variant="primary"
                      disabled={!ativo}
                      onClick={() => setVincularAberto(true)}
                    >
                      <i className="fas fa-link" aria-hidden="true" /> Abrir vínculo
                    </Button>
                    <Button
                      variant="danger"
                      disabled={!ativo}
                      loading={inativar.isPending}
                      onClick={inativarProfissional}
                    >
                      <i className="fas fa-user-slash" aria-hidden="true" /> Inativar
                    </Button>
                  </div>
                </Card>
              </Can>

              <CardSecao
                className="mb-4"
                titulo={profissional.nome}
                subtitulo={`CPF ${profissional.cpfMascarado}`}
                acao={
                  <Tag variant={situacaoCadastroVariant(profissional.situacao)}>
                    {profissional.situacao}
                  </Tag>
                }
              >
                <dl className="row mb-0">
                  <dt className="col-sm-3 text-semi-bold">CNS</dt>
                  <dd className="col-sm-9">{profissional.cns ?? '—'}</dd>
                  <dt className="col-sm-3 text-semi-bold">Conselho</dt>
                  <dd className="col-sm-9">{profissional.conselho ?? '—'}</dd>
                  <dt className="col-sm-3 text-semi-bold">CRM ativo</dt>
                  <dd className="col-sm-9">
                    <Tag variant={profissional.temCrmAtivo ? 'success' : 'default'}>
                      {profissional.temCrmAtivo ? 'Sim (habilita teleconsulta)' : 'Não'}
                    </Tag>
                  </dd>
                </dl>
              </CardSecao>

              <Card header={<strong>Vínculos CNES/CBO</strong>}>
                <DataTable
                  caption="Vínculos do profissional"
                  columns={
                    ativo
                      ? [
                          ...colunasVinculos,
                          {
                            key: 'acoes',
                            header: 'Ações',
                            render: (v: VinculoCnes) =>
                              v.dataFim ? (
                                <span className="text-down-01 text-secondary">—</span>
                              ) : (
                                <Can permission="saude.gerenciar">
                                  <Button
                                    variant="secondary"
                                    className="small"
                                    onClick={() => setEncerrarDe(v.estabelecimentoId)}
                                  >
                                    Encerrar
                                  </Button>
                                </Can>
                              ),
                          },
                        ]
                      : colunasVinculos
                  }
                  rows={profissional.vinculos}
                  rowKey={(v) => `${v.estabelecimentoId}-${v.dataInicio}`}
                  empty={
                    <EmptyState
                      icon="fas fa-link-slash"
                      title="Nenhum vínculo registrado"
                      description="Abra um vínculo CNES/CBO para lotar o profissional num estabelecimento."
                    />
                  }
                />
              </Card>

              <VincularProfissionalModal
                open={vincularAberto}
                onClose={() => setVincularAberto(false)}
                profissionalId={profissionalId}
              />
              <EncerrarVinculoModal
                open={encerrarDe !== null}
                onClose={() => setEncerrarDe(null)}
                profissionalId={profissionalId}
                estabelecimentoId={encerrarDe ?? ''}
              />
            </>
          );
        }}
      </QueryState>
    </>
  );
}
