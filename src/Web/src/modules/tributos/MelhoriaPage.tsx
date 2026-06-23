// Tela CONTRIBUIÇÃO DE MELHORIA — submódulo Melhoria (CTN arts. 81–82). Conduz o
// fluxo legal completo da obra em etapas: (1) PUBLICAR EDITAL (prazo de impugnação
// ≥ 30 dias) -> guarda o id da obra; (2) ADICIONAR IMÓVEIS beneficiados (modal);
// (3) ENCERRAR PRAZO de impugnação; (4) RATEAR (gera lançamento + guia por imóvel,
// proporcional à valorização). Toda ação é gated por "tributos.gerenciar".
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
  Textarea,
  Toolbar,
  useToast,
} from '../../components/ui';
import type { Column } from '../../components/ui';
import { Can } from '../../auth/Can';
import { ApiError } from '../../api/problemDetails';
import { formatarMoeda } from '../../i18n/format';
import {
  PRAZO_MINIMO_IMPUGNACAO_DIAS,
  useEncerrarImpugnacao,
  usePublicarEditalMelhoria,
  useRatearMelhoria,
} from './melhoria.api';
import type { LancamentoMelhoriaPorImovel } from './melhoria.api';
import { TributosSubNav } from './TributosSubNav';
import { AdicionarImovelMelhoriaModal } from './AdicionarImovelMelhoriaModal';

const PERM_GERENCIAR = 'tributos.gerenciar';

type Etapa = 'edital' | 'imoveis' | 'rateada';

const COLUNAS: Column<LancamentoMelhoriaPorImovel>[] = [
  { key: 'imovel', header: 'Imóvel', render: (l) => l.imovelId },
  { key: 'lancamento', header: 'Lançamento', render: (l) => l.lancamentoId },
  { key: 'dam', header: 'Guia (DAM)', render: (l) => l.damId },
  {
    key: 'contribuicao',
    header: 'Contribuição',
    render: (l) => formatarMoeda(l.contribuicaoRateada),
    align: 'end',
  },
];

export function MelhoriaPage() {
  const toast = useToast();
  const publicar = usePublicarEditalMelhoria();
  const encerrar = useEncerrarImpugnacao();
  const ratear = useRatearMelhoria();

  const [etapa, setEtapa] = useState<Etapa>('edital');
  const [obraId, setObraId] = useState('');
  const [imovelAberto, setImovelAberto] = useState(false);
  const [rateio, setRateio] = useState<LancamentoMelhoriaPorImovel[]>([]);

  // Formulário do edital.
  const [identificacaoObra, setIdentificacaoObra] = useState('');
  const [memorial, setMemorial] = useState('');
  const [custoTotal, setCustoTotal] = useState('');
  const [parcelaFinanciada, setParcelaFinanciada] = useState('');
  const [zona, setZona] = useState('');
  const [fatorAbsorcao, setFatorAbsorcao] = useState('');
  const [dataPublicacao, setDataPublicacao] = useState('');
  const [fimPrazo, setFimPrazo] = useState('');
  const [fundamentoLegal, setFundamentoLegal] = useState('');

  // Ações sobre a obra.
  const [dataReferencia, setDataReferencia] = useState('');
  const [vencimento, setVencimento] = useState('');
  const [numeroParcelas, setNumeroParcelas] = useState('1');

  function publicarEdital(event: FormEvent): void {
    event.preventDefault();
    if (identificacaoObra.trim() === '' || memorial.trim() === '' || fundamentoLegal.trim() === '') {
      toast.error('Informe a identificação, o memorial e o fundamento legal da obra.');
      return;
    }
    if (dataPublicacao.trim() === '' || fimPrazo.trim() === '') {
      toast.error('Informe as datas de publicação e fim do prazo de impugnação.');
      return;
    }
    const dias = (new Date(fimPrazo).getTime() - new Date(dataPublicacao).getTime()) / 86_400_000;
    if (dias < PRAZO_MINIMO_IMPUGNACAO_DIAS) {
      toast.error(`O prazo de impugnação deve ser de no mínimo ${PRAZO_MINIMO_IMPUGNACAO_DIAS} dias (CTN art. 82, II).`);
      return;
    }
    publicar.mutate(
      {
        identificacaoObra: identificacaoObra.trim(),
        memorialDescritivo: memorial.trim(),
        custoTotalObra: Number((custoTotal || '0').replace(',', '.')),
        parcelaCustoFinanciadaPercentual: Number((parcelaFinanciada || '0').replace(',', '.')),
        zonaBeneficiada: zona.trim(),
        fatorAbsorcaoPercentual: Number((fatorAbsorcao || '0').replace(',', '.')),
        dataPublicacaoEdital: dataPublicacao.trim(),
        fimPrazoImpugnacao: fimPrazo.trim(),
        fundamentoLegal: fundamentoLegal.trim(),
      },
      {
        onSuccess: (r) => {
          setObraId(r.id);
          setEtapa('imoveis');
          toast.success(`Edital publicado. Obra ${r.id}.`, 'Sucesso');
        },
        onError: (error) =>
          toast.error(error instanceof ApiError ? error.userMessage : 'Não foi possível publicar o edital.'),
      },
    );
  }

  function encerrarPrazo(): void {
    if (dataReferencia.trim() === '') {
      toast.error('Informe a data de referência (≥ fim do prazo).');
      return;
    }
    encerrar.mutate(
      { obraId, input: { dataReferencia: dataReferencia.trim() } },
      {
        onSuccess: () => toast.success('Prazo de impugnação encerrado. Rateio habilitado.', 'Sucesso'),
        onError: (error) =>
          toast.error(error instanceof ApiError ? error.userMessage : 'Não foi possível encerrar o prazo.'),
      },
    );
  }

  function ratearObra(): void {
    if (vencimento.trim() === '') {
      toast.error('Informe o vencimento das guias.');
      return;
    }
    ratear.mutate(
      { obraId, input: { vencimento: vencimento.trim(), numeroParcelas: Math.max(1, Number(numeroParcelas) || 1) } },
      {
        onSuccess: (r) => {
          setRateio(r.lancamentos);
          setEtapa('rateada');
          toast.success(`Rateio concluído: ${formatarMoeda(r.totalRateado)} em ${r.lancamentos.length} imóvel(is).`, 'Sucesso');
        },
        onError: (error) =>
          toast.error(error instanceof ApiError ? error.userMessage : 'Não foi possível ratear a obra.'),
      },
    );
  }

  function reiniciar(): void {
    setEtapa('edital');
    setObraId('');
    setRateio([]);
    setIdentificacaoObra('');
    setMemorial('');
    setCustoTotal('');
    setParcelaFinanciada('');
    setZona('');
    setFatorAbsorcao('');
    setDataPublicacao('');
    setFimPrazo('');
    setFundamentoLegal('');
    setDataReferencia('');
    setVencimento('');
    setNumeroParcelas('1');
  }

  return (
    <>
      <PageHeader
        title="Contribuição de Melhoria"
        description="Edital → imóveis beneficiados → encerramento da impugnação → rateio (CTN arts. 81–82)."
        actions={
          etapa !== 'edital' ? (
            <Toolbar>
              <Button variant="secondary" onClick={reiniciar}>
                <i className="fas fa-plus" aria-hidden="true" /> Nova obra
              </Button>
            </Toolbar>
          ) : undefined
        }
      />

      <TributosSubNav />

      {obraId !== '' && (
        <Alert variant="info" title="Obra em andamento">
          Obra <strong>{obraId}</strong> —{' '}
          <Tag variant={etapa === 'rateada' ? 'success' : 'info'}>
            {etapa === 'imoveis' ? 'Edital publicado' : etapa === 'rateada' ? 'Rateada' : 'Em edital'}
          </Tag>
        </Alert>
      )}

      {etapa === 'edital' && (
        <Card className="mb-4" header={<strong>1. Publicar edital da obra</strong>}>
          <form className="br-form" onSubmit={publicarEdital} noValidate>
            <div className="row">
              <div className="col-md-6">
                <FormField label="Identificação da obra" required>
                  {({ id, describedBy, invalid }) => (
                    <Input id={id} aria-describedby={describedBy} invalid={invalid} value={identificacaoObra} onChange={(e) => setIdentificacaoObra(e.target.value)} placeholder="Pavimentação da Rua X" />
                  )}
                </FormField>
              </div>
              <div className="col-md-6">
                <FormField label="Zona beneficiada" required>
                  {({ id, describedBy, invalid }) => (
                    <Input id={id} aria-describedby={describedBy} invalid={invalid} value={zona} onChange={(e) => setZona(e.target.value)} placeholder="Bairro Centro" />
                  )}
                </FormField>
              </div>
            </div>

            <FormField label="Memorial descritivo" required>
              {({ id, describedBy, invalid }) => (
                <Textarea id={id} aria-describedby={describedBy} invalid={invalid} rows={3} value={memorial} onChange={(e) => setMemorial(e.target.value)} placeholder="Descrição do projeto e do benefício gerado." />
              )}
            </FormField>

            <div className="row">
              <div className="col-md-4">
                <FormField label="Custo total da obra (R$)" required>
                  {({ id, describedBy, invalid }) => (
                    <Input id={id} type="number" min="0" step="0.01" inputMode="decimal" aria-describedby={describedBy} invalid={invalid} value={custoTotal} onChange={(e) => setCustoTotal(e.target.value)} placeholder="0,00" />
                  )}
                </FormField>
              </div>
              <div className="col-md-4">
                <FormField label="Parcela financiada (%)" required help="Percentual do custo a financiar.">
                  {({ id, describedBy, invalid }) => (
                    <Input id={id} type="number" min="0.01" max="100" step="0.01" inputMode="decimal" aria-describedby={describedBy} invalid={invalid} value={parcelaFinanciada} onChange={(e) => setParcelaFinanciada(e.target.value)} placeholder="100" />
                  )}
                </FormField>
              </div>
              <div className="col-md-4">
                <FormField label="Fator de absorção (%)" required>
                  {({ id, describedBy, invalid }) => (
                    <Input id={id} type="number" min="0.01" max="100" step="0.01" inputMode="decimal" aria-describedby={describedBy} invalid={invalid} value={fatorAbsorcao} onChange={(e) => setFatorAbsorcao(e.target.value)} placeholder="100" />
                  )}
                </FormField>
              </div>
            </div>

            <div className="row">
              <div className="col-md-4">
                <FormField label="Publicação do edital" required>
                  {({ id, describedBy, invalid }) => (
                    <Input id={id} type="date" aria-describedby={describedBy} invalid={invalid} value={dataPublicacao} onChange={(e) => setDataPublicacao(e.target.value)} />
                  )}
                </FormField>
              </div>
              <div className="col-md-4">
                <FormField label="Fim do prazo de impugnação" required help="≥ 30 dias após a publicação (CTN art. 82).">
                  {({ id, describedBy, invalid }) => (
                    <Input id={id} type="date" aria-describedby={describedBy} invalid={invalid} value={fimPrazo} onChange={(e) => setFimPrazo(e.target.value)} />
                  )}
                </FormField>
              </div>
              <div className="col-md-4">
                <FormField label="Fundamento legal" required>
                  {({ id, describedBy, invalid }) => (
                    <Input id={id} aria-describedby={describedBy} invalid={invalid} value={fundamentoLegal} onChange={(e) => setFundamentoLegal(e.target.value)} placeholder="Lei da obra + CTM" />
                  )}
                </FormField>
              </div>
            </div>

            <Can permission={PERM_GERENCIAR}>
              <Toolbar>
                <Button variant="primary" type="submit" loading={publicar.isPending}>
                  <i className="fas fa-bullhorn" aria-hidden="true" /> Publicar edital
                </Button>
              </Toolbar>
            </Can>
          </form>
        </Card>
      )}

      {etapa === 'imoveis' && (
        <Card className="mb-4" header={<strong>2. Imóveis, encerramento e rateio</strong>}>
          <p className="text-gray-60">
            Adicione os imóveis beneficiados com a valorização individual apurada. Depois encerre o prazo
            de impugnação (a partir do fim do prazo) e rateie a contribuição.
          </p>
          <Can permission={PERM_GERENCIAR}>
            <Button variant="secondary" onClick={() => setImovelAberto(true)}>
              <i className="fas fa-house-circle-check" aria-hidden="true" /> Adicionar imóvel beneficiado
            </Button>
          </Can>

          <fieldset className="mt-3 mb-3">
            <legend className="text-up-01 text-semi-bold">Encerrar prazo de impugnação</legend>
            <div className="row align-items-end">
              <div className="col-md-6">
                <FormField label="Data de referência" required help="Deve ser ≥ fim do prazo.">
                  {({ id, describedBy, invalid }) => (
                    <Input id={id} type="date" aria-describedby={describedBy} invalid={invalid} value={dataReferencia} onChange={(e) => setDataReferencia(e.target.value)} />
                  )}
                </FormField>
              </div>
              <div className="col-md-6">
                <div className="tg-form-row-acao">
                  <Can permission={PERM_GERENCIAR}>
                    <Button variant="secondary" onClick={encerrarPrazo} loading={encerrar.isPending}>
                      <i className="fas fa-gavel" aria-hidden="true" /> Encerrar prazo
                    </Button>
                  </Can>
                </div>
              </div>
            </div>
          </fieldset>

          <fieldset className="mb-3">
            <legend className="text-up-01 text-semi-bold">Ratear contribuição</legend>
            <div className="row align-items-end">
              <div className="col-md-4">
                <FormField label="Vencimento das guias" required>
                  {({ id, describedBy, invalid }) => (
                    <Input id={id} type="date" aria-describedby={describedBy} invalid={invalid} value={vencimento} onChange={(e) => setVencimento(e.target.value)} />
                  )}
                </FormField>
              </div>
              <div className="col-md-4">
                <FormField label="Parcelas por imóvel">
                  {({ id }) => (
                    <Input id={id} type="number" min={1} inputMode="numeric" value={numeroParcelas} onChange={(e) => setNumeroParcelas(e.target.value)} />
                  )}
                </FormField>
              </div>
              <div className="col-md-4">
                <div className="tg-form-row-acao">
                  <Can permission={PERM_GERENCIAR}>
                    <Button variant="primary" onClick={ratearObra} loading={ratear.isPending}>
                      <i className="fas fa-scale-balanced" aria-hidden="true" /> Ratear
                    </Button>
                  </Can>
                </div>
              </div>
            </div>
          </fieldset>
        </Card>
      )}

      {etapa === 'rateada' && (
        rateio.length === 0 ? (
          <EmptyState
            icon="fas fa-scale-balanced"
            title="Rateio sem cobranças"
            description="Nenhum imóvel teve parcela positiva (valorização absorvida ou limite atingido)."
          />
        ) : (
          <Card className="mb-4" header={<strong>Lançamentos gerados pelo rateio</strong>}>
            <DataTable<LancamentoMelhoriaPorImovel>
              columns={COLUNAS}
              rows={rateio}
              rowKey={(l) => l.lancamentoId}
              caption="Contribuição de Melhoria rateada por imóvel"
            />
          </Card>
        )
      )}

      <AdicionarImovelMelhoriaModal
        open={imovelAberto}
        obraId={obraId}
        onClose={() => setImovelAberto(false)}
      />
    </>
  );
}
