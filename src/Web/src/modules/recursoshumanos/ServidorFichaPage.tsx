// FICHA FUNCIONAL completa do servidor (navegabilidade — Onda 0). Busca por id via
// param de rota -> GET /api/recursoshumanos/servidores/{id}/ficha-funcional.
// Agrega dados pessoais + vínculo/cargo + timeline do ciclo de vida + dependentes +
// histórico de folhas e de ponto. CPF mascarado (LGPD). Somente leitura.
import { Link, useParams } from 'react-router-dom';
import {
  CardSecao,
  DataTable,
  EmptyState,
  PageHeader,
  QueryState,
  Tag,
} from '../../components/ui';
import type { Column } from '../../components/ui';
import { formatarData, formatarMoeda } from '../../i18n/format';
import { useFichaFuncional } from './api';
import type { FichaFolha, FichaFuncional, FichaPonto } from './api';
import {
  formatarMinutos,
  formatarRegimePrev,
  formatarTipoFolha,
  situacaoFolhaTagVariant,
  situacaoServidorTagVariant,
} from './recursosHumanos.helpers';

const MESES_CURTO = [
  '—',
  'Jan',
  'Fev',
  'Mar',
  'Abr',
  'Mai',
  'Jun',
  'Jul',
  'Ago',
  'Set',
  'Out',
  'Nov',
  'Dez',
];

function competencia(ano: number, mes: number): string {
  const rotuloMes = mes >= 1 && mes <= 12 ? MESES_CURTO[mes] : String(mes);
  return `${rotuloMes}/${ano}`;
}

function Campo({ rotulo, children }: { rotulo: string; children: React.ReactNode }) {
  return (
    <div className="col-sm-6 col-lg-4 mb-3">
      <dt className="text-gray-60 text-down-01">{rotulo}</dt>
      <dd className="mb-0 text-semi-bold">{children}</dd>
    </div>
  );
}

const COLUNAS_FOLHA: Column<FichaFolha>[] = [
  {
    key: 'competencia',
    header: 'Competência',
    sortAccessor: (f) => `${f.ano}-${String(f.mes).padStart(2, '0')}`,
    render: (f) => competencia(f.ano, f.mes),
  },
  { key: 'tipo', header: 'Tipo', render: (f) => formatarTipoFolha(f.tipo) },
  {
    key: 'situacao',
    header: 'Situação',
    render: (f) => <Tag variant={situacaoFolhaTagVariant(f.situacao)}>{f.situacao}</Tag>,
  },
  {
    key: 'proventos',
    header: 'Proventos',
    align: 'end',
    sortAccessor: (f) => f.totalProventos,
    render: (f) => formatarMoeda(f.totalProventos),
  },
  {
    key: 'descontos',
    header: 'Descontos',
    align: 'end',
    sortAccessor: (f) => f.totalDescontos,
    render: (f) => formatarMoeda(f.totalDescontos),
  },
  {
    key: 'liquido',
    header: 'Líquido',
    align: 'end',
    sortAccessor: (f) => f.liquido,
    render: (f) => <span className="text-semi-bold">{formatarMoeda(f.liquido)}</span>,
  },
  {
    key: 'acoes',
    header: 'Ações',
    sticky: true,
    render: (f) => (
      <Link className="br-button secondary small" to={`/recursoshumanos/folhas/${f.folhaId}`}>
        Abrir folha
      </Link>
    ),
  },
];

const COLUNAS_PONTO: Column<FichaPonto>[] = [
  {
    key: 'competencia',
    header: 'Competência',
    sortAccessor: (p) => `${p.ano}-${String(p.mes).padStart(2, '0')}`,
    render: (p) => competencia(p.ano, p.mes),
  },
  { key: 'situacao', header: 'Situação', render: (p) => p.situacao },
  {
    key: 'extras',
    header: 'Horas extras',
    align: 'end',
    sortAccessor: (p) => p.minutosExtras,
    render: (p) => formatarMinutos(p.minutosExtras),
  },
  {
    key: 'faltas',
    header: 'Faltas',
    align: 'end',
    sortAccessor: (p) => p.minutosFalta,
    render: (p) => formatarMinutos(p.minutosFalta),
  },
  {
    key: 'banco',
    header: 'Saldo banco de horas',
    align: 'end',
    sortAccessor: (p) => p.saldoBancoHorasMinutos,
    render: (p) => formatarMinutos(p.saldoBancoHorasMinutos),
  },
];

export function ServidorFichaPage() {
  const { servidorId = '' } = useParams<{ servidorId: string }>();
  const query = useFichaFuncional(servidorId);

  return (
    <>
      <PageHeader
        eyebrow="Recursos Humanos"
        title="Ficha funcional"
        actions={
          <>
            <Link
              className="br-button secondary"
              to={`/recursoshumanos/servidores/${servidorId}/afastamentos`}
            >
              <i className="fas fa-user-clock" aria-hidden="true" /> Afastamentos / licenças
            </Link>
            <Link
              className="br-button secondary"
              to={`/recursoshumanos/servidores/${servidorId}/consignacoes`}
            >
              <i className="fas fa-file-signature" aria-hidden="true" /> Consignações
            </Link>
            <Link
              className="br-button secondary"
              to={`/recursoshumanos/servidores/${servidorId}/sst`}
              state={{ nome: query.data?.dadosPessoais.nome }}
            >
              <i className="fas fa-notes-medical" aria-hidden="true" /> SST / Saúde Ocupacional
            </Link>
            <Link
              className="br-button secondary"
              to={`/recursoshumanos/servidores/${servidorId}/banco-de-horas`}
              state={{ nome: query.data?.dadosPessoais.nome }}
            >
              <i className="fas fa-clock" aria-hidden="true" /> Banco de horas
            </Link>
            <Link
              className="br-button secondary"
              to={`/recursoshumanos/servidores/${servidorId}/certidoes-tempo`}
              state={{ nome: query.data?.dadosPessoais.nome }}
            >
              <i className="fas fa-file-contract" aria-hidden="true" /> Certidões de tempo
            </Link>
            <Link className="br-button secondary" to="/recursoshumanos">
              <i className="fas fa-arrow-left" aria-hidden="true" /> Voltar à lista
            </Link>
          </>
        }
      />

      <QueryState<FichaFuncional>
        isLoading={query.isLoading}
        isError={query.isError}
        error={query.error}
        data={query.data ?? undefined}
        empty={
          <EmptyState
            icon="fas fa-user-slash"
            title="Servidor não encontrado"
            description="Não há ficha funcional para o servidor informado."
          />
        }
      >
        {(ficha) => (
          <>
            <CardSecao
              titulo={ficha.dadosPessoais.nome}
              subtitulo="Identificação e dados cadastrais (CPF mascarado — LGPD)."
              acao={
                <Tag variant={situacaoServidorTagVariant(ficha.vinculo.situacao)}>
                  {ficha.vinculo.situacao}
                </Tag>
              }
              className="mb-4"
            >
              <dl className="row mb-0">
                <Campo rotulo="Matrícula">{ficha.vinculo.matricula}</Campo>
                <Campo rotulo="CPF">{ficha.dadosPessoais.cpf}</Campo>
                <Campo rotulo="Data de nascimento">
                  {formatarData(ficha.dadosPessoais.dataNascimento)}
                </Campo>
                <Campo rotulo="Cargo">{ficha.vinculo.cargo ?? '—'}</Campo>
                <Campo rotulo="Tipo de cargo">{ficha.vinculo.tipoCargo ?? '—'}</Campo>
                <Campo rotulo="Lotação">{ficha.vinculo.lotacao ?? '—'}</Campo>
                <Campo rotulo="Regime previdenciário">
                  {formatarRegimePrev(ficha.vinculo.regime)}
                </Campo>
                <Campo rotulo="Vencimento-base">
                  {ficha.vinculo.vencimento != null
                    ? formatarMoeda(ficha.vinculo.vencimento)
                    : '—'}
                </Campo>
              </dl>
            </CardSecao>

            <CardSecao
              titulo="Linha do tempo do vínculo"
              subtitulo="Marcos do ciclo de vida funcional (ordem cronológica)."
              className="mb-4"
            >
              {ficha.timeline.length === 0 ? (
                <p className="text-gray-60 mb-0">Nenhum marco registrado.</p>
              ) : (
                <ol className="br-list">
                  {ficha.timeline.map((marco) => (
                    <li key={`${marco.evento}-${marco.data}`} className="br-item">
                      <span className="text-semi-bold">{marco.evento}</span>
                      <span className="text-gray-60"> — {formatarData(marco.data)}</span>
                    </li>
                  ))}
                </ol>
              )}
            </CardSecao>

            <CardSecao
              titulo="Dependentes"
              subtitulo="Dependentes registrados no vínculo."
              className="mb-4"
            >
              {ficha.dependentes.length === 0 ? (
                <p className="text-gray-60 mb-0">Nenhum dependente registrado.</p>
              ) : (
                <ul className="br-list">
                  {ficha.dependentes.map((dep) => (
                    <li key={`${dep.nome}-${dep.dataNascimento}`} className="br-item">
                      <span className="text-semi-bold">{dep.nome}</span>
                      <span className="text-gray-60">
                        {' '}
                        — {dep.parentesco}, nascimento {formatarData(dep.dataNascimento)}
                      </span>
                    </li>
                  ))}
                </ul>
              )}
            </CardSecao>

            <CardSecao
              titulo="Histórico de folhas"
              subtitulo="Folhas em que o servidor aparece (mais recente primeiro)."
              className="mb-4"
            >
              <DataTable
                caption="Histórico de folhas do servidor"
                columns={COLUNAS_FOLHA}
                rows={ficha.folhas}
                rowKey={(f) => f.folhaId}
                empty={
                  <EmptyState
                    icon="fas fa-file-invoice-dollar"
                    title="Sem folhas"
                    description="O servidor ainda não aparece em nenhuma folha de pagamento."
                  />
                }
              />
            </CardSecao>

            <CardSecao
              titulo="Histórico de ponto"
              subtitulo="Apurações de ponto do servidor (mais recente primeiro)."
            >
              <DataTable
                caption="Histórico de apurações de ponto do servidor"
                columns={COLUNAS_PONTO}
                rows={ficha.ponto}
                rowKey={(p) => p.apuracaoId}
                empty={
                  <EmptyState
                    icon="fas fa-clock"
                    title="Sem apurações"
                    description="O servidor ainda não tem apurações de ponto."
                  />
                }
              />
            </CardSecao>
          </>
        )}
      </QueryState>
    </>
  );
}
