// CONSIGNAÇÕES do servidor (Onda 2 — Lei 14.131/2021). Mostra a MARGEM consignável por
// balde (geral 35% / cartão consignado / cartão benefício) numa competência, lista os
// contratos averbados (com averbar/suspender/cancelar/reativar) e o cadastro mestre de
// consignatárias (cadastrar/suspender/reativar). A averbação só passa se a parcela couber
// na margem disponível — a invariante é do backend; aqui há pré-checagem de UX.
import { useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import {
  Button,
  CardSecao,
  DataTable,
  EmptyState,
  FormField,
  Metrica,
  MetricaGrade,
  PageHeader,
  QueryState,
  Select,
  Tag,
  Toolbar,
} from '../../components/ui';
import type { Column } from '../../components/ui';
import { Can } from '../../auth/Can';
import { useHasPermission } from '../../auth/Can';
import { formatarData, formatarMoeda } from '../../i18n/format';
import { MESES } from './recursosHumanos.helpers';
import {
  useConsignacoesDoServidor,
  useConsignatarias,
  useMargemConsignavel,
  useReativarConsignataria,
  useSuspenderConsignataria,
} from './consignacao.api';
import type {
  BaldeMargem,
  ConsignatariaResumo,
  ContratoConsignacaoResumo,
} from './consignacao.api';
import {
  PERM_CONSIGNACAO_AVERBAR,
  PERM_CONSIGNACAO_GERENCIAR,
  formatarCategoria,
  formatarGrupoMargem,
  formatarTipoConsignataria,
  formatarCnpj,
  situacaoConsignacaoTagVariant,
  situacaoConsignatariaTagVariant,
} from './consignacao.helpers';
import {
  AcaoConsignacaoMotivoModal,
  AverbarConsignacaoModal,
  CadastrarConsignatariaModal,
  ReativarConsignacaoModal,
} from './ConsignacaoModais';

const ANO_ATUAL = new Date().getFullYear();
const ANOS: { value: string; label: string }[] = Array.from({ length: 5 }, (_, i) => {
  const ano = ANO_ATUAL - i;
  return { value: String(ano), label: String(ano) };
});

function tomBalde(b: BaldeMargem): 'sucesso' | 'alerta' | 'perigo' {
  if (b.limite <= 0) return 'alerta';
  const usado = b.comprometido / b.limite;
  if (usado >= 1) return 'perigo';
  if (usado >= 0.8) return 'alerta';
  return 'sucesso';
}

type AcaoContrato =
  | { kind: 'suspender' | 'cancelar' | 'reativar'; contrato: ContratoConsignacaoResumo }
  | null;

export function ConsignacoesServidorPage() {
  const { servidorId = '' } = useParams<{ servidorId: string }>();
  const podeAverbar = useHasPermission(PERM_CONSIGNACAO_AVERBAR);

  const [ano, setAno] = useState(String(ANO_ATUAL));
  const [mes, setMes] = useState(String(new Date().getMonth() + 1));

  const margemQuery = useMargemConsignavel(servidorId, Number(ano), Number(mes));
  const consignacoesQuery = useConsignacoesDoServidor(servidorId);
  const consignatariasQuery = useConsignatarias();

  const suspenderCons = useSuspenderConsignataria();
  const reativarCons = useReativarConsignataria();

  const [cadastrarAberto, setCadastrarAberto] = useState(false);
  const [averbarAberto, setAverbarAberto] = useState(false);
  const [acao, setAcao] = useState<AcaoContrato>(null);

  const baldes = margemQuery.data?.baldes ?? [];
  const competencia = margemQuery.data?.competencia ?? `${ano}-${String(mes).padStart(2, '0')}`;

  const colunasContrato: Column<ContratoConsignacaoResumo>[] = [
    { key: 'rubrica', header: 'Rubrica', render: (c) => c.codigoRubrica },
    { key: 'categoria', header: 'Categoria', render: (c) => formatarCategoria(c.categoria) },
    { key: 'balde', header: 'Balde', render: (c) => formatarGrupoMargem(c.grupoMargem) },
    {
      key: 'parcela',
      header: 'Parcela',
      align: 'end',
      sortAccessor: (c) => c.valorParcela,
      render: (c) => formatarMoeda(c.valorParcela),
    },
    {
      key: 'parcelas',
      header: 'Parcelas',
      align: 'end',
      render: (c) => `${c.parcelasPagas}/${c.quantidadeParcelas} (restam ${c.parcelasRestantes})`,
    },
    { key: 'averbacao', header: 'Averbação', render: (c) => formatarData(c.dataAverbacao) },
    {
      key: 'situacao',
      header: 'Situação',
      render: (c) => <Tag variant={situacaoConsignacaoTagVariant(c.situacao)}>{c.situacao}</Tag>,
    },
    {
      key: 'acoes',
      header: 'Ações',
      sticky: true,
      render: (c) => (
        <Can permission={PERM_CONSIGNACAO_GERENCIAR}>
          <div className="d-flex" style={{ gap: '0.5rem' }}>
            {c.situacao === 'Averbada' && (
              <>
                <Button variant="secondary" size="sm" onClick={() => setAcao({ kind: 'suspender', contrato: c })}>
                  Suspender
                </Button>
                <Button variant="ghost" size="sm" onClick={() => setAcao({ kind: 'cancelar', contrato: c })}>
                  Cancelar
                </Button>
              </>
            )}
            {c.situacao === 'Suspensa' && (
              <>
                <Button variant="secondary" size="sm" onClick={() => setAcao({ kind: 'reativar', contrato: c })}>
                  Reativar
                </Button>
                <Button variant="ghost" size="sm" onClick={() => setAcao({ kind: 'cancelar', contrato: c })}>
                  Cancelar
                </Button>
              </>
            )}
            {(c.situacao === 'Quitada' || c.situacao === 'Cancelada') && <span className="text-gray-60">—</span>}
          </div>
        </Can>
      ),
    },
  ];

  const colunasConsignataria: Column<ConsignatariaResumo>[] = [
    { key: 'razao', header: 'Razão social', render: (c) => c.razaoSocial },
    { key: 'cnpj', header: 'CNPJ', render: (c) => formatarCnpj(c.cnpj) },
    { key: 'tipo', header: 'Natureza', render: (c) => formatarTipoConsignataria(c.tipo) },
    {
      key: 'situacao',
      header: 'Situação',
      render: (c) => (
        <Tag variant={situacaoConsignatariaTagVariant(c.situacao)}>{c.situacao}</Tag>
      ),
    },
    {
      key: 'acoes',
      header: 'Ações',
      sticky: true,
      render: (c) => (
        <Can permission={PERM_CONSIGNACAO_GERENCIAR}>
          {c.situacao === 'Ativa' ? (
            <Button
              variant="ghost"
              size="sm"
              loading={suspenderCons.isPending && suspenderCons.variables === c.id}
              onClick={() => suspenderCons.mutate(c.id)}
            >
              Suspender
            </Button>
          ) : (
            <Button
              variant="secondary"
              size="sm"
              loading={reativarCons.isPending && reativarCons.variables === c.id}
              onClick={() => reativarCons.mutate(c.id)}
            >
              Reativar
            </Button>
          )}
        </Can>
      ),
    },
  ];

  return (
    <>
      <PageHeader
        eyebrow="Recursos Humanos"
        title="Consignações"
        actions={
          <Link className="br-button secondary" to={`/recursoshumanos/servidores/${servidorId}/ficha`}>
            <i className="fas fa-arrow-left" aria-hidden="true" /> Voltar à ficha
          </Link>
        }
      />

      <CardSecao
        titulo="Margem consignável"
        subtitulo="Reserva legal por balde na competência (Lei 14.131/2021): geral 35%, cartão consignado e cartão benefício."
        acao={
          <Toolbar>
            <FormField label="Ano">
              {({ id }) => (
                <Select id={id} options={ANOS} value={ano} onChange={(e) => setAno(e.target.value)} />
              )}
            </FormField>
            <FormField label="Mês">
              {({ id }) => (
                <Select id={id} options={MESES} value={mes} onChange={(e) => setMes(e.target.value)} />
              )}
            </FormField>
          </Toolbar>
        }
        className="mb-4"
      >
        <QueryState
          isLoading={margemQuery.isLoading}
          isError={margemQuery.isError}
          error={margemQuery.error}
          data={margemQuery.data ?? undefined}
        >
          {(margem) => (
            <>
              <p className="text-gray-60 text-down-01 mb-3">
                Competência {margem.competencia} · base consignável apurada{' '}
                <strong>{formatarMoeda(margem.baseDeCalculo)}</strong>.
              </p>
              <MetricaGrade>
                {margem.baldes.map((b) => (
                  <Metrica
                    key={b.grupo}
                    label={formatarGrupoMargem(b.grupo)}
                    valor={formatarMoeda(b.disponivel)}
                    secundario={`Disponível de ${formatarMoeda(b.limite)} · comprometido ${formatarMoeda(b.comprometido)}`}
                    tom={tomBalde(b)}
                  />
                ))}
              </MetricaGrade>
            </>
          )}
        </QueryState>
      </CardSecao>

      <CardSecao
        titulo="Contratos averbados"
        subtitulo="Consignações do servidor (mais recente primeiro). Averbar só é aceito se a parcela couber na margem."
        acao={
          <Can permission={PERM_CONSIGNACAO_AVERBAR}>
            <Button
              variant="primary"
              onClick={() => setAverbarAberto(true)}
              disabled={!margemQuery.isSuccess || consignatariasQuery.data?.some((c) => c.situacao === 'Ativa') !== true}
              title={
                margemQuery.isSuccess
                  ? 'Averbar um novo contrato de consignação'
                  : 'Selecione a competência (margem) antes de averbar.'
              }
            >
              <i className="fas fa-file-signature" aria-hidden="true" /> Averbar consignação
            </Button>
          </Can>
        }
        className="mb-4"
      >
        <QueryState<ContratoConsignacaoResumo[]>
          isLoading={consignacoesQuery.isLoading}
          isError={consignacoesQuery.isError}
          error={consignacoesQuery.error}
          data={consignacoesQuery.data}
        >
          {(contratos) => (
            <DataTable
              caption="Contratos de consignação do servidor"
              columns={colunasContrato}
              rows={contratos}
              rowKey={(c) => c.id}
              empty={
                <EmptyState
                  icon="fas fa-file-signature"
                  title="Sem consignações"
                  description="O servidor ainda não possui contratos de consignação averbados."
                />
              }
            />
          )}
        </QueryState>
      </CardSecao>

      <CardSecao
        titulo="Consignatárias"
        subtitulo="Cadastro mestre de bancos/entidades habilitadas. Apenas as ativas recebem novas averbações."
        acao={
          <Can permission={PERM_CONSIGNACAO_GERENCIAR}>
            <Button variant="secondary" onClick={() => setCadastrarAberto(true)}>
              <i className="fas fa-plus" aria-hidden="true" /> Cadastrar consignatária
            </Button>
          </Can>
        }
      >
        <QueryState<ConsignatariaResumo[]>
          isLoading={consignatariasQuery.isLoading}
          isError={consignatariasQuery.isError}
          error={consignatariasQuery.error}
          data={consignatariasQuery.data}
        >
          {(consignatarias) => (
            <DataTable
              caption="Consignatárias cadastradas no tenant"
              columns={colunasConsignataria}
              rows={consignatarias}
              rowKey={(c) => c.id}
              empty={
                <EmptyState
                  icon="fas fa-building-columns"
                  title="Nenhuma consignatária"
                  description="Cadastre um banco/entidade habilitada para averbar consignações."
                />
              }
            />
          )}
        </QueryState>
      </CardSecao>

      {podeAverbar && (
        <AverbarConsignacaoModal
          open={averbarAberto}
          onClose={() => setAverbarAberto(false)}
          servidorId={servidorId}
          consignatarias={consignatariasQuery.data ?? []}
          baldes={baldes}
          competencia={competencia}
        />
      )}

      <Can permission={PERM_CONSIGNACAO_GERENCIAR}>
        <CadastrarConsignatariaModal open={cadastrarAberto} onClose={() => setCadastrarAberto(false)} />
        <AcaoConsignacaoMotivoModal
          open={acao?.kind === 'suspender' || acao?.kind === 'cancelar'}
          onClose={() => setAcao(null)}
          servidorId={servidorId}
          contratoId={acao?.contrato.id ?? ''}
          acao={acao?.kind === 'cancelar' ? 'cancelar' : 'suspender'}
        />
        <ReativarConsignacaoModal
          open={acao?.kind === 'reativar'}
          onClose={() => setAcao(null)}
          servidorId={servidorId}
          contratoId={acao?.contrato.id ?? ''}
        />
      </Can>
    </>
  );
}
