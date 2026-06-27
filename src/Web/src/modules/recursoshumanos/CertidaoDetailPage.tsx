// DETALHE (documento) de uma Certidao de Tempo de Servico/Contribuicao: dados do servidor, periodos
// computados (efetivo exercicio + averbados, com fatores e abatimentos), total apurado e o codigo de
// autenticacao para validacao publica. Read-only — a emissao/anulacao vivem na pagina do servidor.
import { Link, useParams, useLocation } from 'react-router-dom';
import {
  Alert,
  CardSecao,
  DataTable,
  PageHeader,
  QueryState,
  Tag,
} from '../../components/ui';
import type { Column } from '../../components/ui';
import { formatarData } from '../../i18n/format';
import { FINALIDADES, REGIMES_ORIGEM, useCertidao } from './certidaoTempo.api';
import type {
  CertidaoDetalheView,
  FinalidadeCertidao,
  PeriodoTempoView,
  RegimeOrigemPeriodo,
} from './certidaoTempo.api';

function rotuloFinalidade(valor: FinalidadeCertidao): string {
  return FINALIDADES.find((f) => f.valor === valor)?.rotulo ?? valor;
}

function rotuloRegime(valor: RegimeOrigemPeriodo | null): string {
  if (!valor) return '—';
  return REGIMES_ORIGEM.find((r) => r.valor === valor)?.rotulo ?? valor;
}

export function CertidaoDetailPage() {
  const { certidaoId = '' } = useParams<{ certidaoId: string }>();
  const location = useLocation() as { state?: { nome?: string } };
  const query = useCertidao(certidaoId);

  const colunas: Column<PeriodoTempoView>[] = [
    {
      key: 'natureza',
      header: 'Natureza',
      render: (p) =>
        p.natureza === 'EfetivoExercicio' ? 'Efetivo exercício (próprio)' : 'Averbado',
    },
    { key: 'inicio', header: 'Início', render: (p) => formatarData(p.inicio) },
    { key: 'fim', header: 'Fim', render: (p) => formatarData(p.fim) },
    { key: 'origem', header: 'Origem', render: (p) => p.origem ?? '—' },
    { key: 'regime', header: 'Regime', render: (p) => rotuloRegime(p.regimeOrigem) },
    { key: 'brutos', header: 'Dias brutos', render: (p) => String(p.diasBrutos) },
    { key: 'naoComp', header: 'Não-computáveis', render: (p) => String(p.diasNaoComputaveis) },
    { key: 'liquidos', header: 'Dias líquidos', render: (p) => String(p.diasLiquidos) },
    { key: 'fator', header: 'Fator', render: (p) => p.fator.toFixed(2) },
    { key: 'equiv', header: 'Dias equivalentes', render: (p) => String(p.diasEquivalentes) },
  ];

  return (
    <>
      <PageHeader
        eyebrow="Recursos Humanos"
        title="Certidão de tempo de serviço/contribuição"
        description={location.state?.nome}
        actions={
          <Link className="br-button secondary" to="/recursoshumanos">
            <i className="fas fa-arrow-left" aria-hidden="true" /> Voltar
          </Link>
        }
      />

      <QueryState<CertidaoDetalheView>
        isLoading={query.isLoading}
        isError={query.isError}
        error={query.error}
        data={query.data}
      >
        {(certidao) => (
          <>
            {certidao.situacao === 'Anulada' && (
              <Alert variant="warning" title="Certidão anulada" className="mb-4">
                Esta certidão foi anulada (sem efeito).{' '}
                {certidao.motivoAnulacao && <>Motivo: {certidao.motivoAnulacao}.</>}
              </Alert>
            )}

            <CardSecao
              titulo={`Certidão nº ${certidao.numero}`}
              subtitulo={rotuloFinalidade(certidao.finalidade)}
              acao={
                certidao.situacao === 'Emitida' ? (
                  <Tag variant="success">Emitida</Tag>
                ) : (
                  <Tag variant="danger">Anulada</Tag>
                )
              }
            >
              <dl className="tg-descricao">
                <div>
                  <dt>Servidor</dt>
                  <dd>
                    {certidao.servidorNome} (mat. {certidao.matricula})
                  </dd>
                </div>
                <div>
                  <dt>CPF</dt>
                  <dd>{certidao.cpfMascarado}</dd>
                </div>
                <div>
                  <dt>Emissão</dt>
                  <dd>{formatarData(certidao.dataEmissao)}</dd>
                </div>
                <div>
                  <dt>Órgão emissor</dt>
                  <dd>{certidao.orgaoEmissor}</dd>
                </div>
                <div>
                  <dt>Tempo total certificado</dt>
                  <dd>
                    <strong>{certidao.tempoFormatado}</strong> ({certidao.totalDias} dias)
                  </dd>
                </div>
                <div>
                  <dt>Código de autenticação</dt>
                  <dd>
                    <code>{certidao.codigoAutenticacao}</code>
                  </dd>
                </div>
                {certidao.finalidadeDescrita && (
                  <div>
                    <dt>Finalidade descrita</dt>
                    <dd>{certidao.finalidadeDescrita}</dd>
                  </div>
                )}
                {certidao.observacao && (
                  <div>
                    <dt>Observação</dt>
                    <dd>{certidao.observacao}</dd>
                  </div>
                )}
              </dl>
            </CardSecao>

            <CardSecao
              titulo="Períodos computados"
              subtitulo="A soma dos dias equivalentes (após fatores e abatimentos) compõe o tempo total."
            >
              <DataTable
                caption="Períodos computados na certidão"
                columns={colunas}
                rows={certidao.periodos}
                rowKey={(p) => `${p.inicio}-${p.fim}-${p.natureza}`}
                empty="Nenhum período computado."
              />
            </CardSecao>
          </>
        )}
      </QueryState>
    </>
  );
}
