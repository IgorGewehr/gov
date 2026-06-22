// Demonstrações Contábeis (DCASP) — abas das 4 demonstrações derivadas do balancete:
// Balanço Orçamentário (BO), Financeiro (BF), Patrimonial (BP) e Variações Patrimoniais
// (DVP). Seletor exercício/mês + abas; cada quadro vira uma tabela com destaque do
// total/equilíbrio ao pé. Todas as consultas exigem financas.ver (gating server-side).
import { useState } from 'react';
import type { FormEvent } from 'react';
import {
  Button,
  Card,
  EmptyState,
  FormField,
  Input,
  PageHeader,
  QueryState,
  Select,
} from '../../../components/ui';
import { formatarMoeda } from '../../../i18n/format';
import { exercicioCorrente } from '../financas.helpers';
import { FinancasSubNav } from '../FinancasSubNav';
import { MESES } from './contabilidade.helpers';
import {
  useBalancoFinanceiro,
  useBalancoOrcamentario,
  useBalancoPatrimonial,
  useVariacoesPatrimoniais,
} from './demonstracoes.api';
import type { Demonstrativo } from './demonstracoes.api';
import {
  ABAS_DEMONSTRATIVO,
  quadrosBalancoFinanceiro,
  quadrosBalancoOrcamentario,
  quadrosBalancoPatrimonial,
  quadrosVariacoesPatrimoniais,
} from './demonstracoes.helpers';
import type { QuadroExibivel } from './demonstracoes.helpers';

interface Consulta {
  exercicio: number;
  mes: number;
}

/** Tabela de um quadro do demonstrativo, com o total/equilíbrio destacado no rodapé. */
function QuadroTabela({ quadro }: { quadro: QuadroExibivel }) {
  return (
    <Card className="mb-3">
      <div className="br-table">
        <table>
          <caption>{quadro.titulo}</caption>
          <thead>
            <tr>
              <th scope="col">Discriminação</th>
              {quadro.colunas.map((c) => (
                <th key={c} scope="col" className="text-end">
                  {c}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {quadro.linhas.length === 0 ? (
              <tr>
                <td colSpan={quadro.colunas.length + 1} className="text-gray-60">
                  Sem itens neste quadro.
                </td>
              </tr>
            ) : (
              quadro.linhas.map((l) => (
                <tr key={l.linha}>
                  <td>{l.linha}</td>
                  {l.valores.map((v, i) => (
                    <td key={i} className="text-end">
                      {formatarMoeda(v)}
                    </td>
                  ))}
                </tr>
              ))
            )}
          </tbody>
          {quadro.total && (
            <tfoot>
              <tr className="text-semi-bold bg-gray-2">
                <th scope="row">{quadro.total.linha}</th>
                {quadro.total.valores.map((v, i) => (
                  <td key={i} className="text-end">
                    {formatarMoeda(v)}
                  </td>
                ))}
              </tr>
            </tfoot>
          )}
        </table>
      </div>
    </Card>
  );
}

/** Renderiza o demonstrativo ativo (cada hook só busca quando é a aba selecionada). */
function Demonstrativos({
  ativo,
  exercicio,
  mes,
}: {
  ativo: Demonstrativo;
  exercicio: number;
  mes: number;
}) {
  const bo = useBalancoOrcamentario(exercicio, mes, ativo === 'balanco-orcamentario');
  const bf = useBalancoFinanceiro(exercicio, mes, ativo === 'balanco-financeiro');
  const bp = useBalancoPatrimonial(exercicio, mes, ativo === 'balanco-patrimonial');
  const dvp = useVariacoesPatrimoniais(exercicio, mes, ativo === 'variacoes-patrimoniais');

  function quadros(): QuadroExibivel[] | null {
    if (ativo === 'balanco-orcamentario') return bo.data ? quadrosBalancoOrcamentario(bo.data) : null;
    if (ativo === 'balanco-financeiro') return bf.data ? quadrosBalancoFinanceiro(bf.data) : null;
    if (ativo === 'balanco-patrimonial') return bp.data ? quadrosBalancoPatrimonial(bp.data) : null;
    return dvp.data ? quadrosVariacoesPatrimoniais(dvp.data) : null;
  }

  const query =
    ativo === 'balanco-orcamentario'
      ? bo
      : ativo === 'balanco-financeiro'
        ? bf
        : ativo === 'balanco-patrimonial'
          ? bp
          : dvp;

  return (
    <QueryState
      isLoading={query.isLoading}
      isError={query.isError}
      error={query.error}
      data={quadros() ?? undefined}
    >
      {(qs) => (
        <>
          {qs.map((q) => (
            <QuadroTabela key={q.titulo} quadro={q} />
          ))}
        </>
      )}
    </QueryState>
  );
}

export function DemonstracoesPage() {
  const [exercicio, setExercicio] = useState(String(exercicioCorrente()));
  const [mes, setMes] = useState(String(new Date().getMonth() + 1));
  const [consulta, setConsulta] = useState<Consulta | null>(null);
  const [ativo, setAtivo] = useState<Demonstrativo>(ABAS_DEMONSTRATIVO[0].slug);

  function consultar(event: FormEvent): void {
    event.preventDefault();
    const ex = Number(exercicio);
    const m = Number(mes);
    if (Number.isInteger(ex) && ex >= 2000 && Number.isInteger(m) && m >= 1 && m <= 12) {
      setConsulta({ exercicio: ex, mes: m });
    }
  }

  return (
    <>
      <PageHeader
        title="Demonstrações Contábeis (DCASP)"
        description="Balanços Orçamentário, Financeiro e Patrimonial e as Variações Patrimoniais, derivados do balancete do período."
      />

      <FinancasSubNav />

      <Card className="mb-4">
        <form className="br-form" onSubmit={consultar}>
          <div className="row align-items-end">
            <div className="col-12 col-md-auto">
              <FormField label="Exercício" required>
                {({ id, describedBy, invalid }) => (
                  <Input
                    id={id}
                    type="number"
                    min="2000"
                    step="1"
                    inputMode="numeric"
                    aria-describedby={describedBy}
                    invalid={invalid}
                    value={exercicio}
                    onChange={(e) => setExercicio(e.target.value)}
                  />
                )}
              </FormField>
            </div>
            <div className="col-12 col-md-auto">
              <FormField label="Mês" required>
                {({ id, describedBy, invalid }) => (
                  <Select
                    id={id}
                    options={MESES}
                    aria-describedby={describedBy}
                    invalid={invalid}
                    value={mes}
                    onChange={(e) => setMes(e.target.value)}
                  />
                )}
              </FormField>
            </div>
            <div className="col-auto mb-3">
              <Button variant="primary" type="submit">
                Consultar
              </Button>
            </div>
          </div>
        </form>
      </Card>

      {consulta === null ? (
        <EmptyState
          icon="fas fa-magnifying-glass"
          title="Faça uma consulta"
          description="Informe o exercício e o mês e clique em Consultar para ver as demonstrações."
        />
      ) : (
        <>
          <nav className="mb-4" aria-label="Demonstrações">
            <ul className="d-flex flex-wrap list-style-none p-0 m-0" style={{ gap: '0.5rem' }}>
              {ABAS_DEMONSTRATIVO.map((aba) => (
                <li key={aba.slug}>
                  <button
                    type="button"
                    className={`br-button small ${ativo === aba.slug ? 'primary' : 'secondary'}`}
                    aria-current={ativo === aba.slug ? 'true' : undefined}
                    onClick={() => setAtivo(aba.slug)}
                  >
                    {aba.sigla} — {aba.rotulo}
                  </button>
                </li>
              ))}
            </ul>
          </nav>

          <Demonstrativos ativo={ativo} exercicio={consulta.exercicio} mes={consulta.mes} />
        </>
      )}
    </>
  );
}
