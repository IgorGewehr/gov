// Tela APURAR COSIP — submódulo COSIP/CIP (CF art. 149-A). Configura a TABELA de
// faixas (modal) e APURA a COSIP por LANÇAMENTO PRÓPRIO (POST /cosip/lancar) para
// consumidores NÃO faturados pela distribuidora: a partir da classe e do consumo
// (kWh) a tabela vigente apura o valor e gera lançamento + guia (DAM). Toda ação é
// gated por "tributos.gerenciar".
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
  Select,
  Toolbar,
  useToast,
} from '../../components/ui';
import { Can } from '../../auth/Can';
import { ApiError } from '../../api/problemDetails';
import { formatarMoeda } from '../../i18n/format';
import { CLASSE_CONSUMIDOR_LABEL, CLASSE_CONSUMIDOR_VALOR, useLancarCosip } from './cosip.api';
import type { ClasseConsumidorCosip, ResultadoLancamentoCosip } from './cosip.api';
import { TributosSubNav } from './TributosSubNav';
import { TabelaCosipFormModal } from './TabelaCosipFormModal';

const PERM_GERENCIAR = 'tributos.gerenciar';

const CLASSE_OPCOES = (Object.keys(CLASSE_CONSUMIDOR_VALOR) as ClasseConsumidorCosip[]).map((k) => ({
  value: k,
  label: CLASSE_CONSUMIDOR_LABEL[k],
}));

function competenciaAtual(): string {
  const agora = new Date();
  return `${agora.getFullYear()}-${String(agora.getMonth() + 1).padStart(2, '0')}`;
}

export function ApurarCosipPage() {
  const toast = useToast();
  const lancar = useLancarCosip();

  const [contribuinteId, setContribuinteId] = useState('');
  const [classe, setClasse] = useState<ClasseConsumidorCosip>('Residencial');
  const [consumoKwh, setConsumoKwh] = useState('');
  const [competencia, setCompetencia] = useState(competenciaAtual());
  const [vencimento, setVencimento] = useState('');
  const [imovelId, setImovelId] = useState('');
  const [tabelaAberta, setTabelaAberta] = useState(false);
  const [resultado, setResultado] = useState<ResultadoLancamentoCosip | null>(null);

  function submeter(event: FormEvent): void {
    event.preventDefault();
    if (contribuinteId.trim() === '') {
      toast.error('Informe o identificador do contribuinte.');
      return;
    }
    if (!/^\d{4}-\d{2}$/.test(competencia)) {
      toast.error('Informe a competência no formato AAAA-MM.');
      return;
    }
    if (vencimento.trim() === '') {
      toast.error('Informe o vencimento da guia.');
      return;
    }
    const [ano, mes] = competencia.split('-').map(Number);
    lancar.mutate(
      {
        contribuinteId: contribuinteId.trim(),
        classe: CLASSE_CONSUMIDOR_VALOR[classe],
        consumoKwh: Number((consumoKwh || '0').replace(',', '.')),
        ano,
        mes,
        vencimento: vencimento.trim(),
        imovelId: imovelId.trim() === '' ? null : imovelId.trim(),
      },
      {
        onSuccess: (r) => {
          setResultado(r);
          toast.success(`COSIP apurada: ${formatarMoeda(r.valorCosip)}.`, 'Sucesso');
        },
        onError: (error) =>
          toast.error(error instanceof ApiError ? error.userMessage : 'Não foi possível apurar a COSIP.'),
      },
    );
  }

  return (
    <>
      <PageHeader
        title="Apurar COSIP"
        description="Apure a COSIP/CIP por lançamento próprio (consumidores não faturados pela distribuidora)."
        actions={
          <Can permission={PERM_GERENCIAR}>
            <Toolbar>
              <Button variant="secondary" onClick={() => setTabelaAberta(true)}>
                <i className="fas fa-table-list" aria-hidden="true" /> Configurar tabela
              </Button>
            </Toolbar>
          </Can>
        }
      />

      <TributosSubNav />

      <Card className="mb-4">
        <form className="br-form" onSubmit={submeter} noValidate>
          <div className="row">
            <div className="col-md-6">
              <FormField label="Identificador do contribuinte" required>
                {({ id, describedBy, invalid }) => (
                  <Input id={id} aria-describedby={describedBy} invalid={invalid} value={contribuinteId} onChange={(e) => setContribuinteId(e.target.value)} placeholder="00000000-0000-0000-0000-000000000000" />
                )}
              </FormField>
            </div>
            <div className="col-md-3">
              <FormField label="Classe de consumidor" required>
                {({ id }) => (
                  <Select id={id} options={CLASSE_OPCOES} value={classe} onChange={(e) => setClasse(e.target.value as ClasseConsumidorCosip)} />
                )}
              </FormField>
            </div>
            <div className="col-md-3">
              <FormField label="Consumo (kWh)">
                {({ id, describedBy, invalid }) => (
                  <Input id={id} type="number" min="0" step="0.01" inputMode="decimal" aria-describedby={describedBy} invalid={invalid} value={consumoKwh} onChange={(e) => setConsumoKwh(e.target.value)} placeholder="0,00" />
                )}
              </FormField>
            </div>
          </div>

          <div className="row align-items-end">
            <div className="col-md-3">
              <FormField label="Competência" required help="Mês de referência (AAAA-MM).">
                {({ id, describedBy, invalid }) => (
                  <Input id={id} type="month" aria-describedby={describedBy} invalid={invalid} value={competencia} onChange={(e) => setCompetencia(e.target.value)} />
                )}
              </FormField>
            </div>
            <div className="col-md-3">
              <FormField label="Vencimento" required>
                {({ id, describedBy, invalid }) => (
                  <Input id={id} type="date" aria-describedby={describedBy} invalid={invalid} value={vencimento} onChange={(e) => setVencimento(e.target.value)} />
                )}
              </FormField>
            </div>
            <div className="col-md-6">
              <FormField label="Imóvel vinculado (opcional)">
                {({ id }) => (
                  <Input id={id} value={imovelId} onChange={(e) => setImovelId(e.target.value)} placeholder="Identificador do imóvel" />
                )}
              </FormField>
            </div>
          </div>

          <Can permission={PERM_GERENCIAR}>
            <Toolbar>
              <Button variant="primary" type="submit" loading={lancar.isPending}>
                <i className="fas fa-calculator" aria-hidden="true" /> Apurar e lançar
              </Button>
            </Toolbar>
          </Can>
        </form>
      </Card>

      <Alert variant="info" title="Caminho de lançamento próprio">
        O caminho usual da COSIP é a cobrança na fatura de energia (convênio com a distribuidora,
        modelado à parte). Esta tela cobre o lançamento próprio dos consumidores não faturados.
      </Alert>

      {!resultado ? (
        <EmptyState
          icon="fas fa-lightbulb"
          title="Informe os dados e apure"
          description="Selecione o contribuinte, a classe e o consumo para apurar a COSIP pela tabela vigente."
        />
      ) : (
        <Alert variant="success" title="COSIP apurada">
          Valor apurado: <strong>{formatarMoeda(resultado.valorCosip)}</strong>. Lançamento{' '}
          <strong>{resultado.lancamentoId}</strong>, guia (DAM) {resultado.damId}.
        </Alert>
      )}

      <TabelaCosipFormModal open={tabelaAberta} onClose={() => setTabelaAberta(false)} />
    </>
  );
}
