// Tela APURAR ISS — coração do submódulo ISS. Seleciona o contribuinte e a
// competência (AAAA-MM) e APURA o imposto mensal (POST .../apurar): mostra os
// totais por forma de recolhimento (PRÓPRIO/RETIDO/SUBSTITUIÇÃO), o ISS devido e
// o LIVRO FISCAL ELETRÔNICO (uma linha por NFS-e). Antes de apurar, é possível
// SINCRONIZAR sob demanda as NFS-e do ADN (ingestão PASSIVA). Toda ação é gated
// por "tributos.gerenciar".
import { useState } from 'react';
import type { FormEvent } from 'react';
import {
  Alert,
  Button,
  Card,
  DataTable,
  EmptyState,
  FormField,
  Input,
  PageHeader,
  Tag,
  useToast,
} from '../../components/ui';
import type { Column } from '../../components/ui';
import { Can } from '../../auth/Can';
import { ApiError } from '../../api/problemDetails';
import { formatarData, formatarMoeda } from '../../i18n/format';
import { useApurarIss, useSincronizarNfse } from './iss.api';
import type { ApuracaoIss, LivroEletronicoLinha } from './iss.api';
import { formatarPercentual } from './iptu.helpers';
import { FORMA_RECOLHIMENTO_LABEL, formaRecolhimentoTagVariant } from './iss.helpers';
import { TributosSubNav } from './TributosSubNav';
import { IssAliquotasFormModal } from './IssAliquotasFormModal';

const PERM_GERENCIAR = 'tributos.gerenciar';

function competenciaAtual(): string {
  const agora = new Date();
  return `${agora.getFullYear()}-${String(agora.getMonth() + 1).padStart(2, '0')}`;
}

function Totalizador({ rotulo, valor, destaque }: { rotulo: string; valor: number; destaque?: boolean }) {
  return (
    <div className="col-sm-6 col-lg-3 mb-3">
      <dt className="text-gray-60 text-down-01">{rotulo}</dt>
      <dd className={`mb-0 ${destaque ? 'text-up-02 text-semi-bold' : 'text-semi-bold'}`}>
        {formatarMoeda(valor)}
      </dd>
    </div>
  );
}

export function ApurarIssPage() {
  const toast = useToast();
  const [contribuinteId, setContribuinteId] = useState('');
  const [competencia, setCompetencia] = useState(competenciaAtual());
  const [resultado, setResultado] = useState<ApuracaoIss | null>(null);
  const [aliquotasAberto, setAliquotasAberto] = useState(false);

  const apurar = useApurarIss(contribuinteId.trim());
  const sincronizar = useSincronizarNfse();

  function validarFiltro(): boolean {
    if (contribuinteId.trim() === '') {
      toast.error('Informe o identificador do contribuinte.');
      return false;
    }
    if (!/^\d{4}-\d{2}$/.test(competencia)) {
      toast.error('Informe a competência no formato AAAA-MM.');
      return false;
    }
    return true;
  }

  function sincronizarNfse(): void {
    if (!validarFiltro()) return;
    sincronizar.mutate(
      { contribuinteId: contribuinteId.trim(), competencia },
      {
        onSuccess: (r) =>
          toast.success(
            `NFS-e sincronizadas: ${r.notasNovas} nova(s), ${r.notasDuplicadas} duplicada(s).`,
            'Ingestão concluída',
          ),
        onError: (error) =>
          toast.error(
            error instanceof ApiError ? error.userMessage : 'Não foi possível sincronizar as NFS-e.',
          ),
      },
    );
  }

  function apurarIss(event: FormEvent): void {
    event.preventDefault();
    if (!validarFiltro()) return;
    apurar.mutate(
      { competencia },
      {
        onSuccess: (r) => {
          setResultado(r);
          toast.success(`ISS da competência ${r.competencia} apurado.`, 'Sucesso');
        },
        onError: (error) =>
          toast.error(error instanceof ApiError ? error.userMessage : 'Não foi possível apurar o ISS.'),
      },
    );
  }

  const colunas: Column<LivroEletronicoLinha>[] = [
    { key: 'nfse', header: 'NFS-e', render: (l) => l.numeroNfse },
    { key: 'emissao', header: 'Emissão', render: (l) => formatarData(l.dataEmissao) },
    { key: 'item', header: 'Item LC 116', render: (l) => l.itemListaServico },
    { key: 'base', header: 'Base de cálculo', align: 'end', render: (l) => formatarMoeda(l.baseCalculo) },
    { key: 'aliquota', header: 'Alíquota', align: 'end', render: (l) => formatarPercentual(l.aliquotaPercentual) },
    {
      key: 'forma',
      header: 'Forma',
      render: (l) => (
        <Tag variant={formaRecolhimentoTagVariant(l.forma)}>{FORMA_RECOLHIMENTO_LABEL[l.forma]}</Tag>
      ),
    },
    { key: 'iss', header: 'ISS devido', align: 'end', render: (l) => formatarMoeda(l.issDevido) },
  ];

  return (
    <>
      <PageHeader
        title="Apurar ISS"
        description="Apure o ISS mensal de um contribuinte por competência e veja o livro fiscal eletrônico."
        actions={
          <Can permission={PERM_GERENCIAR}>
            <Button variant="secondary" onClick={() => setAliquotasAberto(true)}>
              <i className="fas fa-percent" aria-hidden="true" /> Configurar alíquotas
            </Button>
          </Can>
        }
      />

      <TributosSubNav />

      <Card className="mb-4">
        <form className="br-form" onSubmit={apurarIss}>
          <div className="row align-items-end">
            <div className="col-md-6">
              <FormField label="Identificador do contribuinte" required>
                {({ id, describedBy, invalid }) => (
                  <Input
                    id={id}
                    aria-describedby={describedBy}
                    invalid={invalid}
                    value={contribuinteId}
                    onChange={(e) => setContribuinteId(e.target.value)}
                    placeholder="00000000-0000-0000-0000-000000000000"
                  />
                )}
              </FormField>
            </div>
            <div className="col-md-3">
              <FormField label="Competência" required help="Mês de referência (AAAA-MM).">
                {({ id, describedBy, invalid }) => (
                  <Input
                    id={id}
                    type="month"
                    aria-describedby={describedBy}
                    invalid={invalid}
                    value={competencia}
                    onChange={(e) => setCompetencia(e.target.value)}
                  />
                )}
              </FormField>
            </div>
            <div className="col-md-3 mb-3 d-flex" style={{ gap: '0.5rem' }}>
              <Can permission={PERM_GERENCIAR}>
                <Button variant="secondary" onClick={sincronizarNfse} loading={sincronizar.isPending}>
                  <i className="fas fa-cloud-arrow-down" aria-hidden="true" /> Sincronizar NFS-e
                </Button>
              </Can>
              <Can permission={PERM_GERENCIAR}>
                <Button variant="primary" type="submit" loading={apurar.isPending}>
                  <i className="fas fa-calculator" aria-hidden="true" /> Apurar
                </Button>
              </Can>
            </div>
          </div>
        </form>
      </Card>

      <Alert variant="info" title="Ingestão passiva de NFS-e">
        As NFS-e são recebidas do Ambiente de Dados Nacional (ADN). Use Sincronizar NFS-e para forçar a
        ingestão sob demanda antes da apuração. Nós não emitimos nem assinamos notas.
      </Alert>

      {!resultado ? (
        <EmptyState
          icon="fas fa-file-invoice"
          title="Informe os dados e apure"
          description="Selecione o contribuinte e a competência e clique em Apurar para gerar o livro fiscal."
        />
      ) : (
        <>
          <Card
            className="mb-4"
            header={<strong>Apuração do ISS — competência {resultado.competencia}</strong>}
          >
            <dl className="row">
              <Totalizador rotulo="ISS próprio" valor={resultado.totais.issProprio} />
              <Totalizador rotulo="ISS retido na fonte" valor={resultado.totais.issRetido} />
              <Totalizador rotulo="ISS por substituição" valor={resultado.totais.issSubstituicao} />
              <Totalizador rotulo="ISS total devido" valor={resultado.issTotalDevido} destaque />
            </dl>
            <p className="mb-0 text-gray-60">
              {resultado.quantidadeNotas} nota(s) processada(s); base de cálculo total{' '}
              <strong>{formatarMoeda(resultado.baseCalculoTotal)}</strong>.
            </p>
            {resultado.lancamentoId && (
              <Alert variant="success" title="Lançamento do ISS próprio">
                Lançamento <strong>{resultado.lancamentoId}</strong> constituído para a competência{' '}
                {resultado.competencia}.
              </Alert>
            )}
          </Card>

          <h2 className="text-up-01 mb-2">Livro fiscal eletrônico</h2>
          <DataTable
            caption={`Livro fiscal eletrônico do ISS — competência ${resultado.competencia}`}
            columns={colunas}
            rows={resultado.livro}
            rowKey={(l) => l.chaveAcesso}
            empty={
              <EmptyState
                icon="fas fa-file-circle-question"
                title="Sem NFS-e na competência"
                description="Não há notas ingeridas para este contribuinte nesta competência. Sincronize as NFS-e e apure novamente."
              />
            }
          />
        </>
      )}

      <IssAliquotasFormModal open={aliquotasAberto} onClose={() => setAliquotasAberto(false)} />
    </>
  );
}
