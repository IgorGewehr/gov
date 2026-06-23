// Card da estimativa LOCAL do IGD (query EstimarIgd). Exibe o indice 0-1 e os tres
// fatores que o compoem, SEMPRE com o rotulo CLARO de que e uma estimativa gerencial
// local (derivada de Familia/condicionalidades/beneficios) — NAO o IGD oficial do MDS/SAGI.
import { useState } from 'react';
import type { FormEvent } from 'react';
import {
  Alert,
  Button,
  Card,
  CardSecao,
  FormField,
  FormRow,
  Input,
  Metrica,
  MetricaGrade,
  PageHeader,
  QueryState,
} from '../../../components/ui';
import { AssistenciaSocialSubNav } from '../AssistenciaSocialSubNav';
import { useEstimativaIgd } from './igd.api';

const hoje = new Date();

/** Formata um indice [0,1] como percentual com 1 casa (pt-BR). */
function formatarIndice(valor: number): string {
  return `${(valor * 100).toLocaleString('pt-BR', { minimumFractionDigits: 1, maximumFractionDigits: 1 })}%`;
}

/** Tom semantico (semaforo) do indice: >=0,7 bom; >=0,5 alerta; abaixo, perigo. */
function tomDoIndice(valor: number): 'sucesso' | 'alerta' | 'perigo' {
  if (valor >= 0.7) return 'sucesso';
  if (valor >= 0.5) return 'alerta';
  return 'perigo';
}

export function IgdEstimativaPage() {
  const [exercicio, setExercicio] = useState(String(hoje.getFullYear()));
  const [consulta, setConsulta] = useState<number | null>(null);

  const query = useEstimativaIgd(consulta ?? 0, consulta !== null);

  function consultar(event: FormEvent): void {
    event.preventDefault();
    const ano = Number(exercicio);
    if (Number.isNaN(ano) || ano < 2000) return;
    setConsulta(ano);
  }

  return (
    <>
      <AssistenciaSocialSubNav />
      <PageHeader
        eyebrow="Assistência Social · IGD"
        title="Estimativa local do IGD"
        description="Índice gerencial (0 a 1) estimado a partir de indicadores locais — atualização cadastral, condicionalidades e gestão de benefícios."
      />

      <Alert variant="warning" title="Estimativa gerencial — não é o IGD oficial">
        Este índice é uma <strong>estimativa gerencial local</strong>, derivada das suas próprias
        bases (CadÚnico, condicionalidades e benefícios). <strong>NÃO</strong> é o IGD-PBF/IGD-SUAS
        oficial: o índice oficial é apurado e divulgado pelo MDS/SAGI. Use-o apenas como termômetro
        interno de gestão.
      </Alert>

      <Card className="mb-4 mt-3">
        <form className="br-form" onSubmit={consultar}>
          <FormRow
            acao={
              <Button variant="primary" type="submit" loading={query.isFetching}>
                Consultar
              </Button>
            }
          >
            <FormField label="Exercício (ano)" required>
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  type="number"
                  min="2000"
                  max="9999"
                  step="1"
                  inputMode="numeric"
                  aria-describedby={describedBy}
                  invalid={invalid}
                  value={exercicio}
                  onChange={(e) => setExercicio(e.target.value)}
                />
              )}
            </FormField>
          </FormRow>
        </form>
      </Card>

      {consulta === null ? null : (
        <QueryState
          isLoading={query.isLoading}
          isError={query.isError}
          error={query.error}
          data={query.data}
        >
          {(estimativa) => (
            <CardSecao
              titulo={`Estimativa do exercício ${estimativa.exercicio}`}
              subtitulo={estimativa.rotulo}
              nota="Índice composto pelos fatores de atualização cadastral, cumprimento de condicionalidades e gestão de benefícios (cada um em 0 a 1). Estimativa gerencial local — não substitui o IGD oficial (MDS/SAGI)."
            >
              <MetricaGrade>
                <Metrica
                  label="Índice estimado (IGD local)"
                  valor={formatarIndice(estimativa.indice)}
                  secundario={`Escala 0 a 1 · ${estimativa.indice.toLocaleString('pt-BR', {
                    minimumFractionDigits: 3,
                    maximumFractionDigits: 3,
                  })}`}
                  tom={tomDoIndice(estimativa.indice)}
                />
                <Metrica
                  label="Atualização cadastral"
                  valor={formatarIndice(estimativa.fatorAtualizacaoCadastral)}
                  tom={tomDoIndice(estimativa.fatorAtualizacaoCadastral)}
                />
                <Metrica
                  label="Condicionalidades"
                  valor={formatarIndice(estimativa.fatorCondicionalidades)}
                  tom={tomDoIndice(estimativa.fatorCondicionalidades)}
                />
                <Metrica
                  label="Gestão de benefícios"
                  valor={formatarIndice(estimativa.fatorGestaoBeneficios)}
                  tom={tomDoIndice(estimativa.fatorGestaoBeneficios)}
                />
              </MetricaGrade>
            </CardSecao>
          )}
        </QueryState>
      )}
    </>
  );
}
