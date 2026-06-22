// Tela de DETALHE de um Fornecedor (ObterFornecedorPorIdQuery). Exibe dados cadastrais,
// historico de sancoes e as ACOES de transicao de estado (cada command vira um botao que
// abre o respectivo Modal): AtualizarNivelSicaf, AplicarSancao, Reabilitar, Inativar.
import { useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import {
  Alert,
  Button,
  Card,
  DataTable,
  EmptyState,
  PageHeader,
  QueryState,
  Tag,
} from '../../../components/ui';
import type { Column } from '../../../components/ui';
import { Can } from '../../../auth/Can';
import { formatarData, formatarMoeda } from '../../../i18n/format';
import { useFornecedor } from './fornecedor.api';
import type { FornecedorDetalhe, SancaoResumo } from './fornecedor.api';
import { NIVEL_SICAF_LABEL, SITUACAO_LABEL, TIPO_SANCAO_LABEL } from './fornecedor.api';
import { situacaoTagVariant, tipoSancaoTagVariant } from './fornecedor.helpers';
import {
  AplicarSancaoModal,
  AtualizarNivelSicafModal,
  InativarModal,
  ReabilitarModal,
} from './FornecedorAcoesModais';

type AcaoAberta = 'sancao' | 'nivel' | 'reabilitar' | 'inativar' | null;

function Campo({ rotulo, children }: { rotulo: string; children: React.ReactNode }) {
  return (
    <div className="col-sm-6 mb-3">
      <dt className="text-gray-60 text-down-01">{rotulo}</dt>
      <dd className="mb-0 text-semi-bold">{children}</dd>
    </div>
  );
}

export function FornecedorDetailPage() {
  const { id = '' } = useParams<{ id: string }>();
  const query = useFornecedor(id);
  const [acao, setAcao] = useState<AcaoAberta>(null);

  const sancaoColumns: Column<SancaoResumo>[] = [
    {
      key: 'tipo',
      header: 'Tipo',
      sortAccessor: (s) => s.tipo,
      render: (s) => <Tag variant={tipoSancaoTagVariant(s.tipo)}>{TIPO_SANCAO_LABEL[s.tipo]}</Tag>,
    },
    {
      key: 'inicio',
      header: 'Inicio',
      sortAccessor: (s) => s.dataInicio,
      render: (s) => formatarData(s.dataInicio),
    },
    {
      key: 'fim',
      header: 'Fim',
      sortAccessor: (s) => s.dataFim ?? '',
      render: (s) => (s.dataFim ? formatarData(s.dataFim) : 'Sem termo'),
    },
    { key: 'processo', header: 'Processo', render: (s) => s.processoAdministrativo },
    {
      key: 'multa',
      header: 'Valor da multa',
      align: 'end',
      sortAccessor: (s) => s.valorMulta ?? 0,
      render: (s) => (s.valorMulta != null ? formatarMoeda(s.valorMulta) : '—'),
    },
  ];

  return (
    <>
      <PageHeader
        title="Detalhe do fornecedor"
        actions={
          <Link className="br-button secondary" to="/administracao/fornecedores">
            <i className="fas fa-arrow-left" aria-hidden="true" /> Voltar
          </Link>
        }
      />

      <QueryState<FornecedorDetalhe>
        isLoading={query.isLoading}
        isError={query.isError}
        error={query.error}
        data={query.data}
        empty={
          <EmptyState
            icon="fas fa-circle-question"
            title="Fornecedor nao encontrado"
            description="O fornecedor informado nao existe ou nao esta acessivel neste ente."
          />
        }
      >
        {(fornecedor) => {
          const podeSancionar = fornecedor.situacao !== 'Inativo';
          const podeReabilitar = fornecedor.situacao === 'Sancionado' && !fornecedor.estaImpedido;
          const podeInativar = fornecedor.situacao !== 'Inativo';

          return (
            <>
              {fornecedor.estaImpedido && (
                <Alert variant="danger" title="Fornecedor impedido">
                  Este fornecedor possui sancao impeditiva vigente e nao pode ser habilitado nem contratado
                  (art. 14 / art. 156).
                </Alert>
              )}

              <Card
                className="mt-3"
                header={<strong>{fornecedor.razaoSocial}</strong>}
                footer={
                  <Can permission="administracao.gerenciar">
                    <div className="d-flex flex-wrap" style={{ gap: '0.5rem' }}>
                      <Button variant="secondary" onClick={() => setAcao('nivel')}>
                        <i className="fas fa-layer-group" aria-hidden="true" /> Atualizar nivel SICAF
                      </Button>
                      <Button variant="primary" onClick={() => setAcao('sancao')} disabled={!podeSancionar}>
                        <i className="fas fa-gavel" aria-hidden="true" /> Aplicar sancao
                      </Button>
                      <Button variant="secondary" onClick={() => setAcao('reabilitar')} disabled={!podeReabilitar}>
                        <i className="fas fa-rotate-left" aria-hidden="true" /> Reabilitar
                      </Button>
                      <Button variant="danger" onClick={() => setAcao('inativar')} disabled={!podeInativar}>
                        <i className="fas fa-ban" aria-hidden="true" /> Inativar
                      </Button>
                    </div>
                  </Can>
                }
              >
                <dl className="row">
                  <Campo rotulo="CNPJ">{fornecedor.cnpj}</Campo>
                  <Campo rotulo="Situacao">
                    <Tag variant={situacaoTagVariant(fornecedor.situacao)}>
                      {SITUACAO_LABEL[fornecedor.situacao]}
                    </Tag>
                  </Campo>
                  <Campo rotulo="Nivel cadastral SICAF">
                    {NIVEL_SICAF_LABEL[fornecedor.nivelCadastralSICAF]}
                  </Campo>
                  <Campo rotulo="Impedido de contratar">
                    <Tag variant={fornecedor.estaImpedido ? 'danger' : 'success'}>
                      {fornecedor.estaImpedido ? 'Sim' : 'Nao'}
                    </Tag>
                  </Campo>
                </dl>
              </Card>

              <Card className="mt-3" header={<strong>Historico de sancoes (art. 156)</strong>}>
                <DataTable
                  caption={`Sancoes do fornecedor ${fornecedor.razaoSocial}`}
                  columns={sancaoColumns}
                  rows={fornecedor.sancoes}
                  rowKey={(s) => s.id}
                  empty={
                    <EmptyState
                      icon="fas fa-circle-check"
                      title="Nenhuma sancao registrada"
                      description="Este fornecedor nao possui sancoes administrativas."
                    />
                  }
                />
              </Card>

              <AtualizarNivelSicafModal
                open={acao === 'nivel'}
                onClose={() => setAcao(null)}
                fornecedorId={fornecedor.id}
                nivelAtual={fornecedor.nivelCadastralSICAF}
              />
              <AplicarSancaoModal
                open={acao === 'sancao'}
                onClose={() => setAcao(null)}
                fornecedorId={fornecedor.id}
              />
              <ReabilitarModal
                open={acao === 'reabilitar'}
                onClose={() => setAcao(null)}
                fornecedorId={fornecedor.id}
              />
              <InativarModal
                open={acao === 'inativar'}
                onClose={() => setAcao(null)}
                fornecedorId={fornecedor.id}
              />
            </>
          );
        }}
      </QueryState>
    </>
  );
}
