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
  EmptyState,
  FormField,
  Input,
  PageHeader,
  Toolbar,
  useToast,
} from '../../components/ui';
import { Can } from '../../auth/Can';
import { ApiError } from '../../api/problemDetails';
import { formatarMoeda } from '../../i18n/format';
import { useApurarIss, useSincronizarNfse } from './iss.api';
import type { ApuracaoIss } from './iss.api';
import { TributosSubNav } from './TributosSubNav';
import { IssAliquotasFormModal } from './IssAliquotasFormModal';

const PERM_GERENCIAR = 'tributos.gerenciar';

function competenciaAtual(): string {
  const agora = new Date();
  return `${agora.getFullYear()}-${String(agora.getMonth() + 1).padStart(2, '0')}`;
}

/** Converte "AAAA-MM" em { ano, mes }. */
function partesCompetencia(competencia: string): { ano: number; mes: number } {
  const [ano, mes] = competencia.split('-').map(Number);
  return { ano, mes };
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
  const [vencimento, setVencimento] = useState('');
  const [prestadores, setPrestadores] = useState('');
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
    const listaPrestadores = prestadores
      .split(/[\s,;]+/)
      .map((p) => p.trim())
      .filter((p) => p !== '');
    if (listaPrestadores.length === 0) {
      toast.error('Informe ao menos um CNPJ de prestador para sincronizar.');
      return;
    }
    // Janela "desde" = primeiro dia da competência selecionada.
    const desde = /^\d{4}-\d{2}$/.test(competencia) ? `${competencia}-01` : `${competenciaAtual()}-01`;
    sincronizar.mutate(
      { prestadores: listaPrestadores, desde },
      {
        onSuccess: (r) =>
          toast.success(`NFS-e sincronizadas: ${r.importadas} importada(s).`, 'Ingestão concluída'),
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
    if (vencimento.trim() === '') {
      toast.error('Informe o vencimento do ISS próprio.');
      return;
    }
    const { ano, mes } = partesCompetencia(competencia);
    apurar.mutate(
      { ano, mes, vencimentoIssProprio: vencimento.trim() },
      {
        onSuccess: (r) => {
          setResultado(r);
          toast.success(`ISS da competência ${competencia} apurado.`, 'Sucesso');
        },
        onError: (error) =>
          toast.error(error instanceof ApiError ? error.userMessage : 'Não foi possível apurar o ISS.'),
      },
    );
  }

  return (
    <>
      <PageHeader
        title="Apurar ISS"
        description="Apure o ISS mensal de um contribuinte por competência e veja o livro fiscal eletrônico."
        actions={
          <Can permission={PERM_GERENCIAR}>
            <Toolbar>
              <Button variant="secondary" onClick={() => setAliquotasAberto(true)}>
                <i className="fas fa-percent" aria-hidden="true" /> Configurar alíquotas
              </Button>
            </Toolbar>
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
            <div className="col-md-3">
              <FormField label="Vencimento do ISS próprio" required help="Para a apuração.">
                {({ id, describedBy, invalid }) => (
                  <Input
                    id={id}
                    type="date"
                    aria-describedby={describedBy}
                    invalid={invalid}
                    value={vencimento}
                    onChange={(e) => setVencimento(e.target.value)}
                  />
                )}
              </FormField>
            </div>
          </div>

          <div className="row">
            <div className="col-12">
              <FormField
                label="Prestadores (CNPJ) para sincronizar"
                help="Um ou mais CNPJs separados por vírgula/espaço; necessário só p/ a sincronização."
              >
                {({ id, describedBy, invalid }) => (
                  <Input
                    id={id}
                    aria-describedby={describedBy}
                    invalid={invalid}
                    value={prestadores}
                    onChange={(e) => setPrestadores(e.target.value)}
                    placeholder="00.000.000/0001-00, 11.111.111/0001-11"
                  />
                )}
              </FormField>
            </div>
          </div>

          <Can permission={PERM_GERENCIAR}>
            <Toolbar>
              <Button variant="secondary" onClick={sincronizarNfse} loading={sincronizar.isPending}>
                <i className="fas fa-cloud-arrow-down" aria-hidden="true" /> Sincronizar NFS-e
              </Button>
              <Button variant="primary" type="submit" loading={apurar.isPending}>
                <i className="fas fa-calculator" aria-hidden="true" /> Apurar
              </Button>
            </Toolbar>
          </Can>
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
        <Card className="mb-4" header={<strong>Apuração do ISS — competência {competencia}</strong>}>
          <dl className="row">
            <Totalizador rotulo="ISS próprio" valor={resultado.issProprio} destaque />
            <Totalizador rotulo="ISS retido na fonte" valor={resultado.issRetido} />
            <Totalizador rotulo="ISS por substituição" valor={resultado.issSubstituicao} />
          </dl>
          <p className="mb-0 text-gray-60">{resultado.quantidadeNotas} nota(s) processada(s).</p>
          {resultado.lancamentoId && (
            <Alert variant="success" title="Lançamento do ISS próprio">
              Lançamento <strong>{resultado.lancamentoId}</strong> constituído (apuração{' '}
              {resultado.apuracaoId}).
            </Alert>
          )}
        </Card>
      )}

      <IssAliquotasFormModal open={aliquotasAberto} onClose={() => setAliquotasAberto(false)} />
    </>
  );
}
