// MEUS DEBITOS: lancamentos tributarios EM ABERTO do PROPRIO cidadao (dado-proprio
// resolvido server-side pelo token). Endpoint: GET /api/cidadao/meus-debitos.
//
// 2a VIA DE DAM: o contrato expoe GET /api/cidadao/dams/{damId}/segunda-via — chaveado
// pelo ID do DAM, que NAO consta no DTO de lancamento (MeuLancamentoDto nao traz damId).
// Para nao INVENTAR um vinculo inexistente, a 2a via e um lookup pelo codigo do DAM (que
// consta no documento/aviso impresso do cidadao); o backend revalida a titularidade
// server-side (anti-IDOR) e retorna 404 se o DAM nao for do cidadao.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { useQuery } from '@tanstack/react-query';
import {
  Alert,
  Button,
  Card,
  DataTable,
  EmptyState,
  FormField,
  Input,
  Modal,
  PageHeader,
  QueryState,
  Tag,
} from '../../components/ui';
import type { Column } from '../../components/ui';
import { errorMessage } from '../../components/ui';
import { cidadaoApi } from './cidadaoApi';
import type { MeuLancamento } from './cidadaoApi';
import { formatarCompetencia, formatarData, formatarMoeda, situacaoTagVariant } from './cidadao.helpers';

export function MeusDebitosPage() {
  const [damCodigo, setDamCodigo] = useState('');
  const [damBuscado, setDamBuscado] = useState<string | null>(null);

  const debitos = useQuery({
    queryKey: ['cidadao', 'meus-debitos'],
    queryFn: () => cidadaoApi.meusDebitos(),
  });

  const colunas: Column<MeuLancamento>[] = [
    { key: 'tributo', header: 'Tributo', render: (l) => l.tributo, sortAccessor: (l) => l.tributo },
    { key: 'competencia', header: 'Competencia', render: (l) => formatarCompetencia(l.competencia) },
    {
      key: 'vencimento',
      header: 'Vencimento',
      render: (l) => formatarData(l.vencimento),
      sortAccessor: (l) => l.vencimento,
    },
    {
      key: 'valor',
      header: 'Valor',
      align: 'end',
      render: (l) => formatarMoeda(l.valorPrincipal),
      sortAccessor: (l) => l.valorPrincipal,
    },
    {
      key: 'situacao',
      header: 'Situacao',
      render: (l) => <Tag variant={situacaoTagVariant(l.situacao)}>{l.situacao}</Tag>,
    },
  ];

  function buscarDam(event: FormEvent): void {
    event.preventDefault();
    const codigo = damCodigo.trim();
    if (codigo !== '') setDamBuscado(codigo);
  }

  return (
    <>
      <PageHeader
        title="Meus debitos"
        description="Lancamentos tributarios em aberto vinculados ao seu CPF/CNPJ neste municipio."
      />

      <Card className="mb-4">
        <DataTable
          caption="Lancamentos tributarios em aberto"
          columns={colunas}
          rows={debitos.data}
          rowKey={(l) => l.lancamentoId}
          loading={debitos.isLoading}
          error={debitos.isError ? errorMessage(debitos.error) : null}
          empty={
            <EmptyState
              icon="fas fa-check-circle"
              title="Voce nao tem debitos em aberto."
              description="Nenhum lancamento tributario pendente foi encontrado."
            />
          }
        />
      </Card>

      <Card header={<h2 className="mb-0 text-up-01">Segunda via de DAM</h2>}>
        <form className="br-form" onSubmit={buscarDam} noValidate>
          <div className="d-flex" style={{ gap: '0.75rem', alignItems: 'flex-end', flexWrap: 'wrap' }}>
            <div style={{ flex: '1 1 16rem' }}>
              <FormField
                label="Codigo do DAM"
                help="Esta no seu documento de arrecadacao ou aviso de vencimento."
              >
                {({ id, describedBy, invalid }) => (
                  <Input
                    id={id}
                    aria-describedby={describedBy}
                    invalid={invalid}
                    value={damCodigo}
                    onChange={(e) => setDamCodigo(e.target.value)}
                    placeholder="Ex.: 0000-0000-0000-0000"
                  />
                )}
              </FormField>
            </div>
            <Button variant="primary" type="submit" disabled={damCodigo.trim() === ''}>
              <i className="fas fa-search" aria-hidden="true" /> Buscar 2a via
            </Button>
          </div>
        </form>
      </Card>

      <DamModal codigo={damBuscado} onClose={() => setDamBuscado(null)} />
    </>
  );
}

/** Modal da 2a via de DAM: busca pelo codigo informado; 404 vira mensagem amigavel. */
function DamModal({ codigo, onClose }: { codigo: string | null; onClose: () => void }) {
  const dam = useQuery({
    queryKey: ['cidadao', 'dam', codigo],
    queryFn: () => cidadaoApi.segundaViaDam(codigo as string),
    enabled: codigo !== null,
    retry: false,
  });

  return (
    <Modal
      open={codigo !== null}
      onClose={onClose}
      title="Segunda via do DAM"
      size="medium"
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            Fechar
          </Button>
          <Button variant="primary" onClick={() => window.print()} disabled={!dam.data}>
            <i className="fas fa-print" aria-hidden="true" /> Imprimir
          </Button>
        </>
      }
    >
      <QueryState
        isLoading={dam.isLoading}
        isError={dam.isError}
        error={dam.error}
        data={dam.data}
        empty={<Alert variant="info">DAM nao encontrado para o codigo informado.</Alert>}
      >
        {(d) => (
          <div className="stack">
            <p className="mb-1">
              <strong>Valor total:</strong> {formatarMoeda(d.valorTotal)}
            </p>
            <p className="mb-2">
              <strong>Situacao:</strong>{' '}
              <Tag variant={d.quitado ? 'success' : 'warning'}>{d.quitado ? 'Quitado' : 'Em aberto'}</Tag>
            </p>
            <DataTable
              caption="Parcelas do documento de arrecadacao"
              columns={[
                { key: 'numero', header: 'Parcela', render: (p) => p.numero },
                { key: 'venc', header: 'Vencimento', render: (p) => formatarData(p.vencimento) },
                { key: 'valor', header: 'Valor', align: 'end', render: (p) => formatarMoeda(p.valor) },
                {
                  key: 'paga',
                  header: 'Situacao',
                  render: (p) => (
                    <Tag variant={p.paga ? 'success' : 'warning'}>{p.paga ? 'Paga' : 'Em aberto'}</Tag>
                  ),
                },
              ]}
              rows={d.parcelas}
              rowKey={(p) => String(p.numero)}
            />
          </div>
        )}
      </QueryState>
    </Modal>
  );
}
