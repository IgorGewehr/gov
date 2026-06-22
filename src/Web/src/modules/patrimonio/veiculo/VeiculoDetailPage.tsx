// Tela de DETALHE do veículo (ObterVeiculo) + consulta de abastecimentos por período
// (ListarAbastecimentosDoVeiculo) e TODAS as ações de frota como botões que abrem
// Modal+form (RegistrarAbastecimento, AbrirOrdemServico, ConcluirManutencao,
// RegistrarMulta, RegistrarLicenciamento, DesignarMotorista). As ações de frota só
// ficam habilitadas para veículo ativo no acervo (I-5).
import { useState } from 'react';
import type { FormEvent, ReactNode } from 'react';
import { Link, useParams } from 'react-router-dom';
import {
  Alert,
  Button,
  Card,
  DataTable,
  EmptyState,
  errorMessage,
  FormField,
  Input,
  PageHeader,
  QueryState,
  Tag,
} from '../../../components/ui';
import type { Column } from '../../../components/ui';
import { Can } from '../../../auth/Can';
import { formatarData, formatarMoeda } from '../../../i18n/format';
import { useAbastecimentosDoVeiculo, useVeiculo } from './veiculo.api';
import type { AbastecimentoResumo, VeiculoDetalhe } from './veiculo.api';
import { situacaoVeiculoLabel, situacaoVeiculoTagVariant, veiculoAtivoNoAcervo } from './veiculo.helpers';
import {
  AbrirOrdemServicoModal,
  ConcluirManutencaoModal,
  DesignarMotoristaModal,
  RegistrarAbastecimentoModal,
  RegistrarLicenciamentoModal,
  RegistrarMultaModal,
} from './VeiculoAcaoModais';

type AcaoFrota = 'abastecimento' | 'os' | 'concluir-os' | 'multa' | 'licenciamento' | 'motorista' | null;

function Campo({ rotulo, children }: { rotulo: string; children: ReactNode }) {
  return (
    <div className="col-sm-6 mb-3">
      <dt className="text-gray-60 text-down-01">{rotulo}</dt>
      <dd className="mb-0 text-semi-bold">{children}</dd>
    </div>
  );
}

function inicioDoAnoIso(): string {
  return `${new Date().getFullYear()}-01-01`;
}

function PainelAbastecimentos({ veiculoId }: { veiculoId: string }) {
  const [de, setDe] = useState(inicioDoAnoIso());
  const [ate, setAte] = useState(new Date().toISOString().slice(0, 10));
  const [periodo, setPeriodo] = useState<{ de: string; ate: string } | null>(null);

  const query = useAbastecimentosDoVeiculo(
    veiculoId,
    periodo?.de ?? '',
    periodo?.ate ?? '',
    periodo !== null,
  );

  function consultar(event: FormEvent): void {
    event.preventDefault();
    setPeriodo({ de, ate });
  }

  const columns: Column<AbastecimentoResumo>[] = [
    { key: 'data', header: 'Data', sortAccessor: (a) => a.data, render: (a) => formatarData(a.data) },
    { key: 'litros', header: 'Litros', align: 'end', sortAccessor: (a) => a.litros, render: (a) => a.litros.toLocaleString('pt-BR') },
    { key: 'valor', header: 'Valor', align: 'end', sortAccessor: (a) => a.valor, render: (a) => formatarMoeda(a.valor) },
    { key: 'odometro', header: 'Odômetro (km)', align: 'end', sortAccessor: (a) => a.odometro, render: (a) => a.odometro.toLocaleString('pt-BR') },
  ];

  return (
    <Card className="mt-4" header={<strong>Abastecimentos</strong>}>
      <form className="br-form" onSubmit={consultar}>
        <div className="row align-items-end">
          <div className="col-sm">
            <FormField label="De">
              {({ id }) => <Input id={id} type="date" value={de} onChange={(e) => setDe(e.target.value)} />}
            </FormField>
          </div>
          <div className="col-sm">
            <FormField label="Até">
              {({ id }) => <Input id={id} type="date" value={ate} onChange={(e) => setAte(e.target.value)} />}
            </FormField>
          </div>
          <div className="col-auto mb-3">
            <Button variant="primary" type="submit" loading={query.isFetching} disabled={de === '' || ate === ''}>
              Consultar
            </Button>
          </div>
        </div>
      </form>

      {periodo === null ? (
        <EmptyState
          icon="fas fa-gas-pump"
          title="Consulte os abastecimentos"
          description="Informe o período e clique em Consultar."
        />
      ) : (
        <DataTable
          caption={`Abastecimentos de ${formatarData(periodo.de)} a ${formatarData(periodo.ate)}`}
          columns={columns}
          rows={query.data}
          rowKey={(a) => a.id}
          loading={query.isLoading}
          error={query.isError ? errorMessage(query.error) : null}
          empty={
            <EmptyState icon="fas fa-gas-pump" title="Nenhum abastecimento no período." />
          }
        />
      )}
    </Card>
  );
}

export function VeiculoDetailPage() {
  const { id = '' } = useParams<{ id: string }>();
  const query = useVeiculo(id);
  const [acao, setAcao] = useState<AcaoFrota>(null);

  function fechar(): void {
    setAcao(null);
  }

  return (
    <>
      <PageHeader
        title="Detalhe do veículo"
        actions={
          <Link className="br-button secondary" to="/patrimonio/frota">
            <i className="fas fa-arrow-left" aria-hidden="true" /> Voltar
          </Link>
        }
      />

      <QueryState<VeiculoDetalhe>
        isLoading={query.isLoading}
        isError={query.isError}
        error={query.error}
        data={query.data}
      >
        {(veiculo) => {
          const ativo = veiculoAtivoNoAcervo(veiculo.situacao);
          return (
            <>
              <Card
                header={
                  <div className="d-flex justify-content-between align-items-center">
                    <strong>
                      {veiculo.placa} — RENAVAM {veiculo.renavam}
                    </strong>
                    <Tag variant={situacaoVeiculoTagVariant(veiculo.situacao)}>
                      {situacaoVeiculoLabel(veiculo.situacao)}
                    </Tag>
                  </div>
                }
              >
                <dl className="row">
                  <Campo rotulo="Número de tombamento">{veiculo.numeroTombamento ?? '—'}</Campo>
                  <Campo rotulo="Situação">{situacaoVeiculoLabel(veiculo.situacao)}</Campo>
                  <Campo rotulo="Odômetro">{veiculo.odometro.toLocaleString('pt-BR')} km</Campo>
                  <Campo rotulo="Horímetro">{veiculo.horimetro.toLocaleString('pt-BR')} h</Campo>
                  <Campo rotulo="Valor contábil">{formatarMoeda(veiculo.valorContabil)}</Campo>
                  <Campo rotulo="Motorista atual">{veiculo.motoristaAtualId ?? '—'}</Campo>
                </dl>
              </Card>

              <Card className="mt-4" header={<strong>Operações de frota</strong>}>
                {!ativo && (
                  <Alert variant="warning">
                    Operações de frota só são permitidas para veículo ativo no acervo (Tombado ou Cedido). Situação
                    atual: {situacaoVeiculoLabel(veiculo.situacao)} (I-5).
                  </Alert>
                )}
                <Can permission="patrimonio.gerenciar">
                <div className="d-flex flex-wrap gap-2">
                  <Button variant="primary" disabled={!ativo} onClick={() => setAcao('abastecimento')}>
                    <i className="fas fa-gas-pump" aria-hidden="true" /> Registrar abastecimento
                  </Button>
                  <Button variant="secondary" disabled={!ativo} onClick={() => setAcao('os')}>
                    <i className="fas fa-wrench" aria-hidden="true" /> Abrir OS
                  </Button>
                  <Button variant="secondary" onClick={() => setAcao('concluir-os')}>
                    <i className="fas fa-check" aria-hidden="true" /> Concluir manutenção
                  </Button>
                  <Button variant="secondary" disabled={!ativo} onClick={() => setAcao('multa')}>
                    <i className="fas fa-triangle-exclamation" aria-hidden="true" /> Registrar multa
                  </Button>
                  <Button variant="secondary" disabled={!ativo} onClick={() => setAcao('licenciamento')}>
                    <i className="fas fa-file-contract" aria-hidden="true" /> Registrar licenciamento
                  </Button>
                  <Button variant="secondary" disabled={!ativo} onClick={() => setAcao('motorista')}>
                    <i className="fas fa-id-card" aria-hidden="true" /> Designar motorista
                  </Button>
                </div>
                </Can>
              </Card>

              <PainelAbastecimentos veiculoId={veiculo.id} />

              <RegistrarAbastecimentoModal
                veiculoId={veiculo.id}
                open={acao === 'abastecimento'}
                onClose={fechar}
                odometroAtual={veiculo.odometro}
                horimetroAtual={veiculo.horimetro}
              />
              <AbrirOrdemServicoModal
                veiculoId={veiculo.id}
                open={acao === 'os'}
                onClose={fechar}
                odometroAtual={veiculo.odometro}
                horimetroAtual={veiculo.horimetro}
              />
              <ConcluirManutencaoModal veiculoId={veiculo.id} open={acao === 'concluir-os'} onClose={fechar} />
              <RegistrarMultaModal veiculoId={veiculo.id} open={acao === 'multa'} onClose={fechar} />
              <RegistrarLicenciamentoModal veiculoId={veiculo.id} open={acao === 'licenciamento'} onClose={fechar} />
              <DesignarMotoristaModal veiculoId={veiculo.id} open={acao === 'motorista'} onClose={fechar} />
            </>
          );
        }}
      </QueryState>
    </>
  );
}
