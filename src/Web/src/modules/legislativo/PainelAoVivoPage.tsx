// PAINEL AO VIVO — destaque do PoC. Informa-se a sessao; lista-se as votacoes
// (abertas primeiro) e, ao abrir uma, exibe-se o PLACAR nominal com auto-refresh
// (polling via TanStack Query refetchInterval) enquanto a votacao estiver aberta.
import { useMemo, useState } from 'react';
import type { FormEvent } from 'react';
import {
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
} from '../../components/ui';
import type { Column } from '../../components/ui';
import { errorMessage } from '../../components/ui';
import { usePainelVotacao, useVotacoesPorSessao } from './votacao.api';
import type { PainelVotacao, VotacaoResumo } from './votacao.api';
import { resultadoVotacaoTagVariant, situacaoVotacaoTagVariant } from './legislativo.helpers';
import { LegislativoSecoesNav } from './LegislativoSecoesNav';
import { PainelPlacar } from './PainelPlacar';

/** Abertas primeiro; depois ordem estavel pelo id (nao muta o array de origem). */
function ordenarAbertasPrimeiro(votacoes: VotacaoResumo[]): VotacaoResumo[] {
  return [...votacoes].sort((a, b) => {
    const pa = a.situacao === 'Aberta' ? 0 : 1;
    const pb = b.situacao === 'Aberta' ? 0 : 1;
    return pa !== pb ? pa - pb : a.id.localeCompare(b.id);
  });
}

export function PainelAoVivoPage() {
  const [sessaoInput, setSessaoInput] = useState('');
  const [sessaoId, setSessaoId] = useState('');
  const [votacaoId, setVotacaoId] = useState('');

  const votacoes = useVotacoesPorSessao(sessaoId);
  const painel = usePainelVotacao(votacaoId);

  const linhas = useMemo(
    () => (votacoes.data ? ordenarAbertasPrimeiro(votacoes.data) : votacoes.data),
    [votacoes.data],
  );

  function buscar(event: FormEvent): void {
    event.preventDefault();
    const id = sessaoInput.trim();
    setSessaoId(id);
    setVotacaoId('');
  }

  const columns: Column<VotacaoResumo>[] = [
    { key: 'tipo', header: 'Tipo', sortAccessor: (v) => v.tipo, render: (v) => v.tipo },
    {
      key: 'situacao',
      header: 'Situação',
      sortAccessor: (v) => v.situacao,
      render: (v) => <Tag variant={situacaoVotacaoTagVariant(v.situacao)}>{v.situacao}</Tag>,
    },
    {
      key: 'resultado',
      header: 'Resultado',
      render: (v) => (
        <Tag variant={resultadoVotacaoTagVariant(v.resultado)}>{v.resultado ?? '—'}</Tag>
      ),
    },
    {
      key: 'acoes',
      header: 'Ações',
      render: (v) => (
        <Button
          variant={v.id === votacaoId ? 'primary' : 'ghost'}
          size="sm"
          onClick={() => setVotacaoId(v.id)}
        >
          {v.id === votacaoId ? 'Abrindo' : 'Abrir painel'}
        </Button>
      ),
    },
  ];

  return (
    <>
      <PageHeader
        eyebrow="Legislativo"
        title="Painel ao vivo"
        description="Acompanhe o placar nominal das votações de uma sessão em tempo real."
      />

      <LegislativoSecoesNav />

      <Card className="mb-4">
        <form className="br-form" onSubmit={buscar}>
          <FormRow
            acao={
              <Button variant="primary" type="submit" disabled={sessaoInput.trim() === ''}>
                Carregar votações
              </Button>
            }
          >
            <FormField label="Identificador da sessão" required>
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  aria-describedby={describedBy}
                  invalid={invalid}
                  value={sessaoInput}
                  onChange={(e) => setSessaoInput(e.target.value)}
                  placeholder="00000000-0000-0000-0000-000000000000"
                />
              )}
            </FormField>
          </FormRow>
        </form>
      </Card>

      {sessaoId !== '' && (
        <DataTable
          caption={`Votações da sessão ${sessaoId} (abertas primeiro)`}
          columns={columns}
          rows={linhas}
          rowKey={(v) => v.id}
          loading={votacoes.isLoading}
          error={votacoes.isError ? errorMessage(votacoes.error) : null}
          empty={
            <EmptyState
              icon="fas fa-square-poll-vertical"
              title="Nenhuma votação nesta sessão"
              description="Não há votações registradas para o identificador informado."
            />
          }
        />
      )}

      {votacaoId !== '' && (
        <div className="mt-4">
          <QueryState<PainelVotacao>
            isLoading={painel.isLoading}
            isError={painel.isError}
            error={painel.error}
            data={painel.data}
          >
            {(dados) => <PainelPlacar painel={dados} atualizando={painel.isFetching} />}
          </QueryState>
        </div>
      )}

      {sessaoId === '' && (
        <EmptyState
          icon="fas fa-tower-broadcast"
          title="Informe uma sessão"
          description="Digite o identificador da sessão e carregue as votações para acompanhar o painel ao vivo."
        />
      )}
    </>
  );
}
