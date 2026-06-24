// Livro DIÁRIO (GET /financas/contabilidade/diario?exercicio=&de=&ate=). Lançamentos em ordem
// cronológica, cada um com suas partidas (débitos e créditos) — exigência do TCE.
import { useState } from 'react';
import type { FormEvent } from 'react';
import {
  Button,
  Card,
  EmptyState,
  FormField,
  FormRow,
  Input,
  PageHeader,
  QueryState,
} from '../../../components/ui';
import { formatarData, formatarMoeda } from '../../../i18n/format';
import { exercicioCorrente } from '../financas.helpers';
import { FinancasSubNav } from '../FinancasSubNav';
import { useDiario } from './contabilidade.api';

export function DiarioPage() {
  const [exercicio, setExercicio] = useState(String(exercicioCorrente()));
  const [de, setDe] = useState('');
  const [ate, setAte] = useState('');
  const [consulta, setConsulta] = useState<{ exercicio: number; de?: string; ate?: string }>({
    exercicio: exercicioCorrente(),
  });

  const query = useDiario(consulta.exercicio, consulta.de, consulta.ate);

  function consultar(event: FormEvent): void {
    event.preventDefault();
    const ex = Number(exercicio);
    if (Number.isInteger(ex) && ex >= 2000) {
      setConsulta({ exercicio: ex, de: de || undefined, ate: ate || undefined });
    }
  }

  return (
    <>
      <PageHeader
        title="Livro Diário"
        description="Lançamentos contábeis em ordem cronológica (exigência TCE)."
      />

      <FinancasSubNav />

      <Card className="mb-4">
        <form className="br-form" onSubmit={consultar}>
          <FormRow
            acao={
              <Button variant="primary" type="submit" loading={query.isFetching}>
                Consultar
              </Button>
            }
          >
            <FormField label="Exercício" required>
              {({ id, describedBy, invalid }) => (
                <Input id={id} type="number" min="2000" step="1" inputMode="numeric"
                  aria-describedby={describedBy} invalid={invalid}
                  value={exercicio} onChange={(e) => setExercicio(e.target.value)} />
              )}
            </FormField>
            <FormField label="De">
              {({ id, describedBy }) => (
                <Input id={id} type="date" aria-describedby={describedBy} value={de}
                  onChange={(e) => setDe(e.target.value)} />
              )}
            </FormField>
            <FormField label="Até">
              {({ id, describedBy }) => (
                <Input id={id} type="date" aria-describedby={describedBy} value={ate}
                  onChange={(e) => setAte(e.target.value)} />
              )}
            </FormField>
          </FormRow>
        </form>
      </Card>

      <QueryState
        isLoading={query.isLoading}
        isError={query.isError}
        error={query.error}
        data={query.data}
        empty={
          <EmptyState
            icon="fas fa-book"
            title="Sem lançamentos"
            description="Não há lançamentos no exercício/intervalo informado."
          />
        }
      >
        {(dados) =>
          dados.length === 0 ? (
            <EmptyState
              icon="fas fa-book"
              title="Sem lançamentos"
              description="Não há lançamentos no exercício/intervalo informado."
            />
          ) : (
            dados.map((l) => (
          <Card key={l.lancamentoId} className="mb-3">
            <div className="d-flex justify-content-between mb-2">
              <strong>{formatarData(l.data)}</strong>
              <span className="text-secondary">{l.origem}</span>
            </div>
            <p className="mb-2">{l.historico}</p>
            <table className="br-table">
              <thead>
                <tr>
                  <th scope="col">Conta</th>
                  <th scope="col">Lado</th>
                  <th scope="col" className="text-right">Valor</th>
                </tr>
              </thead>
              <tbody>
                {l.partidas.map((p, i) => (
                  <tr key={`${l.lancamentoId}-${i}`}>
                    <td>{p.codigoConta}</td>
                    <td>{p.lado === 'Debito' ? 'Débito' : 'Crédito'}</td>
                    <td className="text-right">{formatarMoeda(p.valor)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
              </Card>
            ))
          )
        }
      </QueryState>
    </>
  );
}
