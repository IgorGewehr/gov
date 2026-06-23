// PAINEL DO GESTOR — DASHBOARD EXECUTIVO (visão do PREFEITO/GESTOR). Abre com uma
// faixa-resumo (KPIs que saltam) e detalha em cards com DATAVIZ: (a) execução
// orçamentária (barras), (b) mínimos Saúde/Educação (barra c/ marca de mínimo), (c)
// arrecadação + dívida ativa, (d) pessoal/% RCL (medidor de faixas LRF c/ semáforo),
// (e) prontidão de prestação de contas (TCE-RS). Seletor de exercício, estados de
// loading/erro/vazio. Leitura gated por "painel.ver" (a rota repete o gating via
// PermissionRoute; o backend é a fonte da verdade).
import './painelgestor.css';
import { useState } from 'react';
import type { FormEvent } from 'react';
import {
  Alert,
  Button,
  Card,
  EmptyState,
  FormField,
  FormRow,
  Input,
  PageHeader,
  QueryState,
  Toolbar,
} from '../../components/ui';
import { usePainelGestor } from './api';
import type { PainelGestorDto } from './api';
import { exercicioAtual } from './painelgestor.helpers';
import {
  ArrecadacaoCard,
  ExecucaoCard,
  MinimosCard,
  PessoalLrfCard,
  PrestacaoContasCard,
} from './PainelGestorCards';
import { PainelGestorResumo } from './PainelGestorResumo';

export function PainelGestorPage() {
  const [exercicioInput, setExercicioInput] = useState(String(exercicioAtual()));
  const [exercicio, setExercicio] = useState(exercicioAtual());

  const painel = usePainelGestor(exercicio);

  function consultar(event: FormEvent): void {
    event.preventDefault();
    const ano = Number(exercicioInput);
    if (!Number.isInteger(ano) || ano < 2000 || ano > 2100) return;
    setExercicio(ano);
  }

  return (
    <>
      <PageHeader
        eyebrow="Visão executiva"
        title="Painel do Gestor"
        description="Consolidado do exercício: execução orçamentária, mínimos constitucionais, arrecadação, despesa de pessoal (LRF) e prestação de contas (TCE-RS)."
        actions={
          <Toolbar>
            <span className="pg-resumo-secundario">Exercício {exercicio}</span>
          </Toolbar>
        }
      />

      <Card className="mb-4">
        <form className="br-form" onSubmit={consultar}>
          <FormRow
            acao={
              <Button variant="primary" type="submit" loading={painel.isFetching}>
                <i className="fas fa-chart-line" aria-hidden="true" /> Carregar painel
              </Button>
            }
          >
            <FormField label="Exercício" required help="Ano dos indicadores consolidados.">
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
          </FormRow>
        </form>
      </Card>

      <QueryState<PainelGestorDto>
        isLoading={painel.isLoading}
        isError={painel.isError}
        error={painel.error}
        data={painel.data}
        empty={
          <EmptyState
            size="page"
            icon="fas fa-chart-pie"
            title="Sem indicadores para o exercício"
            description="Informe um exercício e clique em Carregar painel."
          />
        }
      >
        {(dados) => (
          <>
            <PainelGestorResumo dados={dados} />
            <ExecucaoCard d={dados.execucaoOrcamentaria} />
            <MinimosCard minimos={dados.minimos} />
            <ArrecadacaoCard d={dados.arrecadacao} />
            <PessoalLrfCard d={dados.pessoalLrf} />
            <PrestacaoContasCard d={dados.prestacaoContas} />
          </>
        )}
      </QueryState>

      <Alert variant="info" title="Sobre os indicadores">
        Os 5 KPIs são reprodutíveis a partir dos read models materializados (execução, mínimos,
        arrecadação/dívida, pessoal/RCL e remessas TCE). Cada indicador degrada graciosamente quando o
        dado de origem ainda não chegou (zeros ou Indeterminado) — nenhum número é inventado. Mínimos,
        limites LRF e prazos de remessa são parametrizáveis por tenant/exercício.
      </Alert>
    </>
  );
}
