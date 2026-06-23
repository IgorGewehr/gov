// PAINEL FISCAL DA EDUCAÇÃO — visão do GESTOR (conformidade constitucional).
//   (1) Indicador MDE: % aplicado vs mínimo de 25% (CF art. 212), aferição ANUAL
//       de conformidade, situação Atingido/Não com cor gov.br (verde/vermelho).
//   (2) FUNDEB 70%: piso de remuneração do magistério (EC 108/2020; Lei
//       14.113/2020), Atingido/Não + receita FUNDEB e remuneração; gestão da
//       distribuição (abrir/esperado/parcela/remuneração) gated.
// Leitura gated por "educacao.ver"; toda ação por "educacao.gerenciar".
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
import { EducacaoSubNav } from './EducacaoSubNav';
import { NATUREZA_MDE_LABEL, useApuracaoFundeb70, useApuracaoMde } from './fiscal.api';
import type { ApuracaoFundeb70, ApuracaoMde } from './fiscal.api';
import { FundebGestaoPainel } from './FundebGestaoPainel';

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

function MdeCard({ mde }: { mde: ApuracaoMde }) {
  const margem = mde.aplicadoMde - mde.receitaBase * mde.percentualMinimo;
  return (
    <Card
      className="mb-4"
      header={
        <div className="d-flex justify-content-between align-items-center">
          <strong>Indicador MDE — exercício {mde.exercicio}</strong>
          <Tag variant={mde.atingido ? 'success' : 'danger'}>
            {mde.atingido ? 'Mínimo atingido' : 'Mínimo NÃO atingido'}
          </Tag>
        </div>
      }
    >
      <dl className="row">
        <Indicador rotulo="Percentual aplicado">
          <span className={mde.atingido ? 'text-success' : 'text-danger'}>
            {formatarFracao(mde.percentualAplicado)}
          </span>
        </Indicador>
        <Indicador rotulo="Mínimo constitucional">{formatarFracao(mde.percentualMinimo)}</Indicador>
        <Indicador rotulo="Receita-base (impostos + transf.)">
          {formatarMoeda(mde.receitaBase)}
        </Indicador>
        <Indicador rotulo="Aplicado em MDE">{formatarMoeda(mde.aplicadoMde)}</Indicador>
        <Indicador rotulo="Margem sobre o mínimo">
          <span className={margem >= 0 ? 'text-success' : 'text-danger'}>
            {formatarMoeda(margem)}
          </span>
        </Indicador>
        <Indicador rotulo="Natureza da aferição">
          {NATUREZA_MDE_LABEL[mde.natureza] ?? '—'}
        </Indicador>
      </dl>
      <p className="mb-0 text-gray-60 text-down-01">
        MDE = Manutenção e Desenvolvimento do Ensino. Mínimo de{' '}
        {formatarFracao(mde.percentualMinimo)} da receita-base (CF art. 212), aferido no encerramento
        do exercício. O percentual mínimo é parametrizável por tenant/exercício.
      </p>
    </Card>
  );
}

function Fundeb70Card({ f }: { f: ApuracaoFundeb70 }) {
  const margem = f.remuneracaoProfissionais - f.receitaFundeb * f.pisoMinimo;
  return (
    <Card
      className="mb-4"
      header={
        <div className="d-flex justify-content-between align-items-center">
          <strong>FUNDEB — piso de 70% (magistério) — exercício {f.exercicio}</strong>
          <Tag variant={f.atingido ? 'success' : 'danger'}>
            {f.atingido ? 'Piso atingido' : 'Piso NÃO atingido'}
          </Tag>
        </div>
      }
    >
      <dl className="row">
        <Indicador rotulo="Percentual aplicado">
          <span className={f.atingido ? 'text-success' : 'text-danger'}>
            {formatarFracao(f.percentualAplicado)}
          </span>
        </Indicador>
        <Indicador rotulo="Piso mínimo">{formatarFracao(f.pisoMinimo)}</Indicador>
        <Indicador rotulo="Receita FUNDEB">{formatarMoeda(f.receitaFundeb)}</Indicador>
        <Indicador rotulo="Remuneração do magistério">
          {formatarMoeda(f.remuneracaoProfissionais)}
        </Indicador>
        <Indicador rotulo="Margem sobre o piso">
          <span className={margem >= 0 ? 'text-success' : 'text-danger'}>
            {formatarMoeda(margem)}
          </span>
        </Indicador>
      </dl>
      <p className="mb-0 text-gray-60 text-down-01">
        Piso de {formatarFracao(f.pisoMinimo)} da receita FUNDEB na remuneração dos profissionais da
        educação básica (EC 108/2020; Lei 14.113/2020, art. 26). A remuneração paga vem da folha do RH
        ou, até o cruzamento, do total informado (// parametrizável).
      </p>
    </Card>
  );
}

export function FiscalEducacaoPainelPage() {
  const [exercicioInput, setExercicioInput] = useState(String(exercicioAtual()));
  const [exercicio, setExercicio] = useState(exercicioAtual());

  const mde = useApuracaoMde(exercicio);
  const fundeb = useApuracaoFundeb70(exercicio);

  function apurar(event: FormEvent): void {
    event.preventDefault();
    const ano = Number(exercicioInput);
    if (!Number.isInteger(ano) || ano < 2000 || ano > 2100) return;
    setExercicio(ano);
  }

  return (
    <>
      <PageHeader
        title="Painel Fiscal — Educação"
        description="Conformidade constitucional: mínimo de 25% em MDE (CF art. 212) e piso de 70% do FUNDEB no magistério."
      />

      <EducacaoSubNav />

      <Card className="mb-4">
        <form className="br-form" onSubmit={apurar}>
          <div className="row align-items-end">
            <div className="col-md-3">
              <FormField label="Exercício" required help="Ano de apuração (MDE e FUNDEB).">
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
              <Button
                variant="primary"
                type="submit"
                loading={mde.isFetching || fundeb.isFetching}
              >
                <i className="fas fa-magnifying-glass-chart" aria-hidden="true" /> Apurar exercício
              </Button>
            </div>
          </div>
        </form>
      </Card>

      <QueryState<ApuracaoMde>
        isLoading={mde.isLoading}
        isError={mde.isError}
        error={mde.error}
        data={mde.data}
        empty={
          <EmptyState
            icon="fas fa-graduation-cap"
            title="Sem apuração MDE para o exercício"
            description="Informe um exercício e clique em Apurar."
          />
        }
      >
        {(dados) => <MdeCard mde={dados} />}
      </QueryState>

      <QueryState<ApuracaoFundeb70>
        isLoading={fundeb.isLoading}
        isError={fundeb.isError}
        error={fundeb.error}
        data={fundeb.data}
        empty={
          <EmptyState
            icon="fas fa-coins"
            title="Sem apuração FUNDEB para o exercício"
            description="Abra a distribuição e registre a remuneração para apurar o piso de 70%."
          />
        }
      >
        {(dados) => <Fundeb70Card f={dados} />}
      </QueryState>

      <Alert variant="info" title="Sobre os indicadores fiscais">
        A receita-base, o aplicado e os percentuais vêm da execução fiscal e da distribuição FUNDEB
        projetadas (alimentadas via lançamentos/parcelas). Os mínimos vigentes são parametrizáveis por
        tenant/exercício e são os que constam nos indicadores acima.
      </Alert>

      <FundebGestaoPainel exercicio={exercicio} />
    </>
  );
}
