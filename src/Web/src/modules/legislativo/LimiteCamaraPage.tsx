// DEMONSTRATIVO DO LIMITE DE DESPESA DA CAMARA (art. 29-A da CF/88) — visão do
// gestor legislativo. Para um exercício: abre a apuração (base informada ou de
// Finanças), exibe o TETO da despesa total (caput, por faixa populacional) e o
// SUBTETO da folha (§1, 70% do repasse), com realizado x limite e SEMÁFOROS
// (verde/amarelo/vermelho), e permite CONSOLIDAR. Deixa explícito que é estimativa
// local (prestação) — a transmissão oficial ao TCE-RS é etapa posterior (M10).
import { useState } from 'react';
import type { FormEvent } from 'react';
import {
  Alert,
  Button,
  Card,
  DataTable,
  EmptyState,
  FormField,
  FormRow,
  Input,
  PageHeader,
  QueryState,
  Tag,
  useToast,
} from '../../components/ui';
import type { Column } from '../../components/ui';
import { formatarMoeda } from '../../i18n/format';
import { Can } from '../../auth/Can';
import { LegislativoSecoesNav } from './LegislativoSecoesNav';
import { AbrirApuracaoModal } from './AbrirApuracaoModal';
import { NATUREZA_DESPESA_CAMARA_LABEL } from './legislativo.shared';
import { mensagemErro } from './legislativoAcao.shared';
import { semaforoArt29ALabel, semaforoArt29ATagVariant } from './legislativo.helpers';
import {
  useConsolidarApuracaoArt29A,
  useDemonstrativoArt29A,
  type DemonstrativoArt29A,
  type DespesaCamara,
} from './limiteCamara.api';

/** Formata uma fração 0..1 como percentual pt-BR com 2 casas. */
function formatarFracao(fracao: number): string {
  return `${(fracao * 100).toLocaleString('pt-BR', {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  })}%`;
}

function exercicioAtual(): number {
  return new Date().getFullYear();
}

function Indicador({ rotulo, children }: { rotulo: string; children: React.ReactNode }) {
  return (
    <div className="col-sm-6 col-lg-3 mb-3">
      <dt className="text-gray-60 text-down-01">{rotulo}</dt>
      <dd className="mb-0 text-semi-bold">{children}</dd>
    </div>
  );
}

/** Despesa com índice estável para a chave de linha da tabela. */
type DespesaLinha = DespesaCamara & { indice: number };

const COLUNAS_DESPESA: Column<DespesaLinha>[] = [
  {
    key: 'natureza',
    header: 'Natureza',
    render: (d) => NATUREZA_DESPESA_CAMARA_LABEL[d.natureza] ?? d.natureza,
  },
  { key: 'descricao', header: 'Descrição', render: (d) => d.descricao ?? '—' },
  { key: 'valor', header: 'Valor', render: (d) => formatarMoeda(d.valor) },
];

function acentoSemaforo(semaforo: string): 'success' | 'warning' | 'danger' {
  if (semaforo === 'Excedido') return 'danger';
  if (semaforo === 'Atencao') return 'warning';
  return 'success';
}

/** Card do TETO da despesa total (caput) por faixa populacional. */
function TetoCard({ d }: { d: DemonstrativoArt29A }) {
  return (
    <Card
      className="mb-4"
      accent={acentoSemaforo(d.semaforoTeto)}
      header={
        <div className="d-flex justify-content-between align-items-center">
          <strong>Teto da despesa total (art. 29-A, caput)</strong>
          <Tag variant={semaforoArt29ATagVariant(d.semaforoTeto)}>
            {semaforoArt29ALabel(d.semaforoTeto)}
          </Tag>
        </div>
      }
    >
      <dl className="row">
        <Indicador rotulo="Faixa populacional">
          {d.populacao.toLocaleString('pt-BR')} hab. — limite {formatarFracao(d.percentualFaixa)}
        </Indicador>
        <Indicador rotulo={`Base de receita (${d.exercicioBaseReceita})`}>
          {formatarMoeda(d.baseReceita)}
        </Indicador>
        <Indicador rotulo="Teto da despesa total">{formatarMoeda(d.tetoDespesaTotal)}</Indicador>
        <Indicador rotulo="Despesa total realizada">
          {formatarMoeda(d.despesaTotalRealizada)}
        </Indicador>
        <Indicador rotulo="Utilização do teto">
          <span className={d.semaforoTeto === 'Excedido' ? 'text-danger' : undefined}>
            {formatarFracao(d.utilizacaoTeto)}
          </span>
        </Indicador>
        <Indicador rotulo="Margem (folga)">
          <span className={d.margemTeto < 0 ? 'text-danger' : 'text-success'}>
            {formatarMoeda(d.margemTeto)}
          </span>
        </Indicador>
      </dl>
      <p className="mb-0 text-gray-60 text-down-01">
        Teto = base de receita do exercício anterior (tributária + transferências) ×{' '}
        {formatarFracao(d.percentualFaixa)} da faixa populacional.
        {d.inativosNoTeto
          ? ' Inativos e pensionistas integram o teto (EC 109/2021).'
          : ' Inativos e pensionistas ainda não integram o teto neste exercício.'}
      </p>
    </Card>
  );
}

/** Card do SUBTETO da folha (§1 — 70% do repasse). */
function SubtetoFolhaCard({ d }: { d: DemonstrativoArt29A }) {
  return (
    <Card
      className="mb-4"
      accent={acentoSemaforo(d.semaforoFolha)}
      header={
        <div className="d-flex justify-content-between align-items-center">
          <strong>Subteto da folha (art. 29-A, §1 — 70% do repasse)</strong>
          <Tag variant={semaforoArt29ATagVariant(d.semaforoFolha)}>
            {semaforoArt29ALabel(d.semaforoFolha)}
          </Tag>
        </div>
      }
    >
      <dl className="row">
        <Indicador rotulo="Repasse/duodecimo recebido">
          {formatarMoeda(d.repasseRecebido)}
        </Indicador>
        <Indicador rotulo="Subteto da folha (70%)">{formatarMoeda(d.subtetoFolha)}</Indicador>
        <Indicador rotulo="Folha realizada">{formatarMoeda(d.folhaRealizada)}</Indicador>
        <Indicador rotulo="Utilização do subteto">
          <span className={d.semaforoFolha === 'Excedido' ? 'text-danger' : undefined}>
            {formatarFracao(d.utilizacaoSubtetoFolha)}
          </span>
        </Indicador>
        <Indicador rotulo="Margem (folga)">
          <span className={d.margemSubtetoFolha < 0 ? 'text-danger' : 'text-success'}>
            {formatarMoeda(d.margemSubtetoFolha)}
          </span>
        </Indicador>
      </dl>
      <p className="mb-0 text-gray-60 text-down-01">
        Subteto = 70% do repasse recebido (art. 29-A, §1, CF/88). Estourar qualquer limite caracteriza
        infração (art. 29-A, §2).
      </p>
    </Card>
  );
}

export function LimiteCamaraPage() {
  const toast = useToast();
  const [exercicioInput, setExercicioInput] = useState(String(exercicioAtual()));
  const [exercicio, setExercicio] = useState(exercicioAtual());
  const [abrirAberto, setAbrirAberto] = useState(false);

  const demonstrativo = useDemonstrativoArt29A(exercicio);
  const consolidar = useConsolidarApuracaoArt29A();

  function consultar(event: FormEvent): void {
    event.preventDefault();
    const ano = Number(exercicioInput);
    if (!Number.isInteger(ano) || ano < 2000 || ano > 2100) return;
    setExercicio(ano);
  }

  function consolidarApuracao(d: DemonstrativoArt29A): void {
    consolidar.mutate(d.apuracaoId, {
      onSuccess: () => toast.success('Demonstrativo consolidado.', 'Sucesso'),
      onError: (error) =>
        toast.error(mensagemErro(error, 'Não foi possível consolidar o demonstrativo.')),
    });
  }

  return (
    <>
      <PageHeader
        eyebrow="Legislativo"
        title="Limite da Câmara (art. 29-A)"
        description="Teto de despesa da Câmara por faixa populacional e subteto da folha (§1), com semáforos de conformidade."
      />

      <LegislativoSecoesNav />

      <Card className="mb-4">
        <form className="br-form" onSubmit={consultar}>
          <FormRow
            acao={
              <Button variant="primary" type="submit" loading={demonstrativo.isFetching}>
                <i className="fas fa-magnifying-glass-chart" aria-hidden="true" /> Abrir exercício
              </Button>
            }
          >
            <div className="row">
              <div className="col-md-4">
                <FormField label="Exercício" required help="Ano sob teto (a base usa o ano anterior).">
                  {({ id, describedBy, invalid }) => (
                    <Input
                      id={id}
                      type="number"
                      min="2000"
                      max="2100"
                      step="1"
                      inputMode="numeric"
                      aria-describedby={describedBy}
                      invalid={invalid}
                      value={exercicioInput}
                      onChange={(e) => setExercicioInput(e.target.value)}
                    />
                  )}
                </FormField>
              </div>
            </div>
          </FormRow>
        </form>
      </Card>

      <QueryState<DemonstrativoArt29A>
        isLoading={demonstrativo.isLoading}
        isError={demonstrativo.isError}
        error={demonstrativo.error}
        data={demonstrativo.data ?? undefined}
        empty={
          <EmptyState
            icon="fas fa-scale-balanced"
            title={`Sem apuração para o exercício ${exercicio}`}
            description="Abra a apuração do exercício informando a base de receita, o repasse e as despesas."
            action={
              <Can permission="legislativo.limite-camara.gerenciar">
                <Button variant="primary" onClick={() => setAbrirAberto(true)}>
                  <i className="fas fa-plus" aria-hidden="true" /> Abrir apuração
                </Button>
              </Can>
            }
          />
        }
      >
        {(d) => (
          <>
            <div className="d-flex flex-wrap justify-content-between align-items-center mb-3" style={{ gap: '0.75rem' }}>
              <div className="d-flex align-items-center" style={{ gap: '0.5rem' }}>
                <Tag variant={d.situacao === 'Consolidada' ? 'success' : 'info'}>{d.situacao}</Tag>
                {d.irregular && <Tag variant="danger">Limite excedido</Tag>}
              </div>
              <Can permission="legislativo.limite-camara.gerenciar">
                {d.situacao !== 'Consolidada' && (
                  <Button
                    variant="primary"
                    loading={consolidar.isPending}
                    onClick={() => consolidarApuracao(d)}
                  >
                    <i className="fas fa-lock" aria-hidden="true" /> Consolidar demonstrativo
                  </Button>
                )}
              </Can>
            </div>

            <TetoCard d={d} />
            <SubtetoFolhaCard d={d} />

            <h2 className="text-up-01 mb-3">Despesas discriminadas</h2>
            <DataTable
              caption="Despesas realizadas sujeitas ao teto, por natureza"
              columns={COLUNAS_DESPESA}
              rows={d.despesas.map((item, indice) => ({ ...item, indice }))}
              rowKey={(item) => `${item.natureza}-${item.indice}`}
              empty={
                <EmptyState
                  icon="fas fa-coins"
                  title="Sem despesas lançadas"
                  description="A apuração não tem parcelas de despesa discriminadas."
                />
              }
            />

            <Alert variant="info" title="Estimativa local x prestação oficial" className="mt-4">
              Os valores acima são uma estimativa local (prestação) a partir da base informada/obtida de
              Finanças e das despesas lançadas. A transmissão oficial ao TCE-RS é etapa posterior (M10).
            </Alert>
          </>
        )}
      </QueryState>

      <AbrirApuracaoModal
        open={abrirAberto}
        onClose={() => setAbrirAberto(false)}
        exercicio={exercicio}
      />
    </>
  );
}
