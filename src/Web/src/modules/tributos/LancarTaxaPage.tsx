// Tela LANÇAR TAXA — submódulo Taxas. Configura a TABELA de uma taxa (modal) e
// LANÇA a taxa (poder de polícia/serviço) a um contribuinte (POST /taxas/lancar):
// calcula o valor pela tabela vigente (código + exercício) sobre a quantidade-base
// e gera lançamento + guia (DAM). Toda ação é gated por "tributos.gerenciar".
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
import { useLancarTaxa } from './taxas.api';
import type { ResultadoLancamentoTaxa } from './taxas.api';
import { TributosSubNav } from './TributosSubNav';
import { TabelaTaxaFormModal } from './TabelaTaxaFormModal';

const PERM_GERENCIAR = 'tributos.gerenciar';

export function LancarTaxaPage() {
  const toast = useToast();
  const lancar = useLancarTaxa();

  const [contribuinteId, setContribuinteId] = useState('');
  const [codigoTaxa, setCodigoTaxa] = useState('');
  const [exercicio, setExercicio] = useState(String(new Date().getFullYear()));
  const [quantidadeBase, setQuantidadeBase] = useState('');
  const [vencimento, setVencimento] = useState('');
  const [imovelId, setImovelId] = useState('');
  const [numeroParcelas, setNumeroParcelas] = useState('1');
  const [tabelaAberta, setTabelaAberta] = useState(false);
  const [resultado, setResultado] = useState<ResultadoLancamentoTaxa | null>(null);

  function submeter(event: FormEvent): void {
    event.preventDefault();
    if (contribuinteId.trim() === '' || codigoTaxa.trim() === '') {
      toast.error('Informe o contribuinte e o código da taxa.');
      return;
    }
    if (vencimento.trim() === '') {
      toast.error('Informe o vencimento da guia.');
      return;
    }
    const ano = Number(exercicio);
    if (!Number.isInteger(ano) || ano < 1900) {
      toast.error('Informe um exercício válido.');
      return;
    }
    lancar.mutate(
      {
        contribuinteId: contribuinteId.trim(),
        codigoTaxa: codigoTaxa.trim(),
        exercicio: ano,
        quantidadeBase: Number((quantidadeBase || '0').replace(',', '.')),
        vencimento: vencimento.trim(),
        imovelId: imovelId.trim() === '' ? null : imovelId.trim(),
        numeroParcelas: Math.max(1, Number(numeroParcelas) || 1),
      },
      {
        onSuccess: (r) => {
          setResultado(r);
          toast.success(`Taxa lançada: ${formatarMoeda(r.valorTaxa)}.`, 'Sucesso');
        },
        onError: (error) =>
          toast.error(error instanceof ApiError ? error.userMessage : 'Não foi possível lançar a taxa.'),
      },
    );
  }

  return (
    <>
      <PageHeader
        title="Lançar taxa"
        description="Lance uma taxa (poder de polícia ou serviço) a um contribuinte a partir da tabela vigente."
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
              <FormField label="Código da taxa (CTM)" required>
                {({ id, describedBy, invalid }) => (
                  <Input id={id} aria-describedby={describedBy} invalid={invalid} value={codigoTaxa} onChange={(e) => setCodigoTaxa(e.target.value)} placeholder="T-001" />
                )}
              </FormField>
            </div>
            <div className="col-md-3">
              <FormField label="Exercício" required>
                {({ id, describedBy, invalid }) => (
                  <Input id={id} type="number" min={1900} inputMode="numeric" aria-describedby={describedBy} invalid={invalid} value={exercicio} onChange={(e) => setExercicio(e.target.value)} />
                )}
              </FormField>
            </div>
          </div>

          <div className="row align-items-end">
            <div className="col-md-3">
              <FormField label="Quantidade-base" help="m²/unidades; ignorada no modo Valor fixo.">
                {({ id, describedBy, invalid }) => (
                  <Input id={id} type="number" min="0" step="0.01" inputMode="decimal" aria-describedby={describedBy} invalid={invalid} value={quantidadeBase} onChange={(e) => setQuantidadeBase(e.target.value)} placeholder="0,00" />
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
            <div className="col-md-2">
              <FormField label="Parcelas">
                {({ id }) => (
                  <Input id={id} type="number" min={1} inputMode="numeric" value={numeroParcelas} onChange={(e) => setNumeroParcelas(e.target.value)} />
                )}
              </FormField>
            </div>
            <div className="col-md-4">
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
                <i className="fas fa-calculator" aria-hidden="true" /> Lançar taxa
              </Button>
            </Toolbar>
          </Can>
        </form>
      </Card>

      {!resultado ? (
        <EmptyState
          icon="fas fa-file-invoice-dollar"
          title="Informe os dados e lance"
          description="Selecione o contribuinte, o código da taxa e a quantidade-base para gerar o lançamento e a guia."
        />
      ) : (
        <Alert variant="success" title="Taxa lançada">
          Valor apurado: <strong>{formatarMoeda(resultado.valorTaxa)}</strong>. Lançamento{' '}
          <strong>{resultado.lancamentoId}</strong>, guia (DAM) {resultado.damId}.
        </Alert>
      )}

      <TabelaTaxaFormModal open={tabelaAberta} onClose={() => setTabelaAberta(false)} />
    </>
  );
}
