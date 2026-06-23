// PAINEL DE MÍNIMOS CONSTITUCIONAIS (Saúde 15% ASPS · Educação 25% MDE).
// Onda 0 — navegabilidade: consulta read-only que projeta o SEMÁFORO de conformidade de
// um exercício a partir dos endpoints fiscais JÁ existentes:
//   GET /api/transparencia/fiscal/minimos?exercicio=  (semáforo Saúde/Educação)
//   GET /api/saude/fiscal/asps/{exercicio}            (complemento ASPS, módulo Saúde)
// Reusa os componentes de dataviz/cards do design system (CardSecao + Metrica/MetricaGrade
// + Tag de semáforo). Nenhuma mutação aqui. O painel PÚBLICO de mínimos é Onda 2.
import { useState } from 'react';
import type { FormEvent } from 'react';
import {
  Alert,
  Button,
  Card,
  CardSecao,
  EmptyState,
  FormField,
  FormRow,
  Input,
  Metrica,
  MetricaGrade,
  PageHeader,
  QueryState,
  Tag,
} from '../../components/ui';
import { formatarMoeda } from '../../i18n/format';
import { useAspsComplemento, useMinimosConstitucionais } from './api';
import type { ApuracaoMinimoResultado } from './api';
import {
  exercicioCorrente,
  formatarMargemPontos,
  formatarPercentual,
  setorMinimoFundamento,
  setorMinimoIcone,
  setorMinimoLabel,
  situacaoMinimoLabel,
  situacaoMinimoTagVariant,
  situacaoMinimoTom,
} from './transparencia.helpers';
import { TransparenciaSubNav } from './TransparenciaSubNav';

/** Card de semáforo de UM mínimo constitucional (dataviz reutilizada do DS). */
function MinimoCard({ resultado }: { resultado: ApuracaoMinimoResultado }) {
  const tom = situacaoMinimoTom(resultado.situacao);
  return (
    <CardSecao
      titulo={
        <>
          <i className={setorMinimoIcone[resultado.setor]} aria-hidden="true" />{' '}
          {setorMinimoLabel[resultado.setor]}
        </>
      }
      subtitulo={`Mínimo exigido: ${formatarPercentual(resultado.percentualMinimo)}`}
      acao={
        <Tag variant={situacaoMinimoTagVariant(resultado.situacao)}>
          {situacaoMinimoLabel[resultado.situacao]}
        </Tag>
      }
      nota={setorMinimoFundamento[resultado.setor]}
    >
      <MetricaGrade>
        <Metrica
          label="Aplicado"
          valor={formatarPercentual(resultado.percentualAplicado)}
          secundario={formatarMoeda(resultado.aplicado)}
          tom={tom}
        />
        <Metrica
          label="Margem sobre o mínimo"
          valor={formatarMargemPontos(resultado.percentualAplicado, resultado.percentualMinimo)}
          secundario={`Mínimo ${formatarPercentual(resultado.percentualMinimo)}`}
          tom={tom}
        />
        <Metrica
          label="Receita-base"
          valor={formatarMoeda(resultado.receitaBase)}
          secundario="Impostos + transferências constitucionais"
        />
      </MetricaGrade>
    </CardSecao>
  );
}

/**
 * Complemento ASPS (Saúde 15%). Vem do módulo Saúde (perm `saude.ver`) — pode não estar
 * licenciado/autorizado; nesse caso o bloco simplesmente NÃO aparece (degrade gracioso).
 */
function AspsComplemento({ exercicio }: { exercicio: number }) {
  const asps = useAspsComplemento(exercicio);
  if (!asps.data) return null;
  const tom = asps.data.atingido ? 'sucesso' : 'perigo';
  return (
    <CardSecao
      className="mt-4"
      titulo="Detalhe ASPS (Saúde)"
      subtitulo="Ações e Serviços Públicos de Saúde — LC 141/2012"
      acao={
        <Tag variant={asps.data.atingido ? 'success' : 'danger'}>
          {asps.data.atingido ? 'Atingido' : 'Não atingido'}
        </Tag>
      }
      nota="Espírito do SIOPS: aplicado em ASPS sobre a receita-base de impostos e transferências."
    >
      <MetricaGrade>
        <Metrica
          label="Aplicado em ASPS"
          valor={formatarPercentual(asps.data.percentualAplicado)}
          secundario={formatarMoeda(asps.data.aplicadoAsps)}
          tom={tom}
        />
        <Metrica
          label="Margem sobre o mínimo"
          valor={formatarMargemPontos(asps.data.percentualAplicado, asps.data.percentualMinimo)}
          secundario={`Mínimo ${formatarPercentual(asps.data.percentualMinimo)}`}
          tom={tom}
        />
        <Metrica
          label="Receita-base"
          valor={formatarMoeda(asps.data.receitaBase)}
          secundario="Impostos + transferências constitucionais"
        />
      </MetricaGrade>
    </CardSecao>
  );
}

export function PainelMinimosPage() {
  const [exercicioInput, setExercicioInput] = useState(String(exercicioCorrente()));
  const [exercicio, setExercicio] = useState<number | null>(exercicioCorrente());

  const minimos = useMinimosConstitucionais(exercicio ?? 0, exercicio !== null);

  function consultar(event: FormEvent): void {
    event.preventDefault();
    const ano = Number(exercicioInput);
    if (!Number.isInteger(ano) || ano <= 0) return;
    setExercicio(ano);
  }

  return (
    <>
      <PageHeader
        eyebrow="Transparência"
        title="Mínimos constitucionais"
        description="Semáforo de conformidade dos mínimos de aplicação em Saúde (15% ASPS) e Educação (25% MDE) por exercício."
      />

      <TransparenciaSubNav />

      <Card className="mb-4">
        <form className="br-form" onSubmit={consultar}>
          <FormRow
            acao={
              <Button variant="primary" type="submit" loading={minimos.isFetching}>
                Consultar
              </Button>
            }
          >
            <div className="row">
              <div className="col-12 col-sm-4">
                <FormField label="Exercício" required>
                  {({ id, describedBy, invalid }) => (
                    <Input
                      id={id}
                      type="number"
                      min="1900"
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

      {exercicio === null ? (
        <EmptyState
          icon="fas fa-scale-balanced"
          title="Informe um exercício"
          description="Digite o ano e clique em Consultar para ver o semáforo de conformidade."
        />
      ) : (
        <QueryState
          isLoading={minimos.isLoading}
          isError={minimos.isError}
          error={minimos.error}
          data={minimos.data}
          empty={
            <EmptyState
              icon="fas fa-folder-open"
              title="Sem apuração para o exercício"
              description="Não há execução fiscal classificada para apurar os mínimos deste exercício."
            />
          }
        >
          {(resultados) =>
            resultados.length === 0 ? (
              <EmptyState
                icon="fas fa-folder-open"
                title="Sem apuração para o exercício"
                description="Não há execução fiscal classificada para apurar os mínimos deste exercício."
              />
            ) : (
              <>
                <p className="tg-caption">
                  Apuração do exercício {exercicio}. Percentuais sobre a receita-base (impostos +
                  transferências constitucionais).
                </p>
                {resultados.some((r) => r.situacao === 'NaoAtingido') ? (
                  <Alert variant="danger" title="Atenção:">
                    Há mínimo constitucional NÃO atingido no exercício {exercicio}. Verifique a
                    execução do(s) setor(es) sinalizado(s) em vermelho.
                  </Alert>
                ) : (
                  <Alert variant="success" title="Conformidade:">
                    Todos os mínimos constitucionais foram atingidos no exercício {exercicio}.
                  </Alert>
                )}
                <div className="row">
                  {resultados.map((resultado) => (
                    <div className="col-12 col-lg-6 mb-4" key={resultado.setor}>
                      <MinimoCard resultado={resultado} />
                    </div>
                  ))}
                </div>
                <AspsComplemento exercicio={exercicio} />
              </>
            )
          }
        </QueryState>
      )}
    </>
  );
}
