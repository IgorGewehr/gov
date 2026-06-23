// PAINEL FISCAL DA SAÚDE — visão do GESTOR (conformidade constitucional).
//   (1) Indicador ASPS: % aplicado vs mínimo de 15% (LC 141/2012), situação
//       Atingido/Não Atingido com cor gov.br (verde/vermelho), base/aplicado/margem.
//   (2) Saldos do FMS por bloco (Custeio/Investimento — Port. GM/MS 3.992/2017),
//       com gestão (abrir fundo / receber parcela / executar despesa) gated.
// Leitura gated por "saude.ver"; toda ação por "saude.gerenciar".
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
  QueryState,
  Tag,
} from '../../components/ui';
import { formatarMoeda } from '../../i18n/format';
import { SaudeSubNav } from './SaudeSubNav';
import { useApuracaoAsps } from './fiscal.api';
import type { ApuracaoAsps } from './fiscal.api';
import { FmsPainel } from './FmsPainel';

const PERCENTUAL_MINIMO_ASPS_PADRAO = 0.15; // CF/LC 141 — exibido só como referência informativa.

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

function AspsCard({ asps }: { asps: ApuracaoAsps }) {
  const margem = asps.aplicadoAsps - asps.receitaBase * asps.percentualMinimo;
  return (
    <Card
      className="mb-4"
      header={
        <div className="d-flex justify-content-between align-items-center">
          <strong>Indicador ASPS — exercício {asps.exercicio}</strong>
          <Tag variant={asps.atingido ? 'success' : 'danger'}>
            {asps.atingido ? 'Mínimo atingido' : 'Mínimo NÃO atingido'}
          </Tag>
        </div>
      }
    >
      <dl className="row">
        <Indicador rotulo="Percentual aplicado">
          <span className={asps.atingido ? 'text-success' : 'text-danger'}>
            {formatarFracao(asps.percentualAplicado)}
          </span>
        </Indicador>
        <Indicador rotulo="Mínimo constitucional">{formatarFracao(asps.percentualMinimo)}</Indicador>
        <Indicador rotulo="Receita-base (impostos + transf.)">
          {formatarMoeda(asps.receitaBase)}
        </Indicador>
        <Indicador rotulo="Aplicado em ASPS">{formatarMoeda(asps.aplicadoAsps)}</Indicador>
        <Indicador rotulo="Margem sobre o mínimo">
          <span className={margem >= 0 ? 'text-success' : 'text-danger'}>
            {formatarMoeda(margem)}
          </span>
        </Indicador>
      </dl>
      <p className="mb-0 text-gray-60 text-down-01">
        ASPS = Ações e Serviços Públicos de Saúde. Mínimo de {formatarFracao(asps.percentualMinimo)}{' '}
        da receita-base (LC 141/2012, art. 6º). O percentual mínimo é parametrizável por tenant/exercício.
      </p>
    </Card>
  );
}

export function FiscalSaudePainelPage() {
  const [exercicioInput, setExercicioInput] = useState(String(exercicioAtual()));
  const [exercicioApurado, setExercicioApurado] = useState(exercicioAtual());

  const query = useApuracaoAsps(exercicioApurado);

  function apurar(event: FormEvent): void {
    event.preventDefault();
    const ano = Number(exercicioInput);
    if (!Number.isInteger(ano) || ano < 2000 || ano > 2100) return;
    setExercicioApurado(ano);
  }

  return (
    <>
      <PageHeader
        title="Painel Fiscal — Saúde"
        description="Conformidade constitucional: mínimo de 15% em ASPS (LC 141/2012) e saldos do Fundo Municipal de Saúde."
      />

      <SaudeSubNav />

      <Card className="mb-4">
        <form className="br-form" onSubmit={apurar}>
          <div className="row align-items-end">
            <div className="col-md-3">
              <FormField label="Exercício" required help="Ano de apuração do mínimo ASPS.">
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
            <div className="col-md-3 mb-3">
              <Button variant="primary" type="submit" loading={query.isFetching}>
                <i className="fas fa-magnifying-glass-chart" aria-hidden="true" /> Apurar exercício
              </Button>
            </div>
          </div>
        </form>
      </Card>

      <QueryState<ApuracaoAsps>
        isLoading={query.isLoading}
        isError={query.isError}
        error={query.error}
        data={query.data}
        empty={
          <EmptyState
            icon="fas fa-heart-pulse"
            title="Sem apuração para o exercício"
            description="Informe um exercício e clique em Apurar para ver o indicador ASPS."
          />
        }
      >
        {(asps) => <AspsCard asps={asps} />}
      </QueryState>

      <Alert variant="info" title="Sobre os indicadores fiscais">
        A receita-base, o aplicado e o percentual vêm da execução fiscal projetada no read model
        (alimentada via lançamentos). O mínimo padrão de referência é{' '}
        {formatarFracao(PERCENTUAL_MINIMO_ASPS_PADRAO)}, mas o valor vigente é parametrizável por
        tenant/exercício e é o que consta no indicador acima.
      </Alert>

      <FmsPainel />
    </>
  );
}
