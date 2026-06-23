// FILA INTERNA do e-SIC (LAI Lei 12.527/2011) — gestão dos pedidos de informação
// do ente. Filtros (ano/situação) -> DataTable com solicitante, situação (semáforo)
// e o PRAZO LEGAL visível em dias úteis restantes (atraso destacado). Apenas leitura
// aqui; as transições (atender/responder/...) vivem no detalhe. Gated transparencia.esic.ver.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Link } from 'react-router-dom';
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
  Select,
  Tag,
  errorMessage,
} from '../../components/ui';
import type { Column } from '../../components/ui';
import { formatarData } from '../../i18n/format';
import { usePedidosSic } from './esic.api';
import type { ListarPedidosParams, PedidoSicResumo, SituacaoPedidoSic } from './esic.api';
import { exercicioCorrente } from './transparencia.helpers';
import { TransparenciaSubNav } from './TransparenciaSubNav';
import {
  calcularPrazoLegal,
  prazoLegalTagVariant,
  situacaoPedidoLabel,
  situacaoPedidoOptions,
  situacaoPedidoTagVariant,
} from './esic.helpers';

export function EsicListPage() {
  const [ano, setAno] = useState(String(exercicioCorrente()));
  const [situacao, setSituacao] = useState<SituacaoPedidoSic | ''>('');
  const [filtros, setFiltros] = useState<ListarPedidosParams>({});

  const query = usePedidosSic(filtros);

  function consultar(event: FormEvent): void {
    event.preventDefault();
    const numeroAno = Number(ano);
    setFiltros({
      ano: ano.trim() === '' || !Number.isInteger(numeroAno) ? null : numeroAno,
      situacao: situacao || null,
    });
  }

  const columns: Column<PedidoSicResumo>[] = [
    {
      key: 'protocolo',
      header: 'Protocolo',
      sortAccessor: (p) => p.protocolo,
      render: (p) => p.protocolo,
    },
    {
      key: 'solicitante',
      header: 'Solicitante',
      sortAccessor: (p) => p.solicitante,
      render: (p) => p.solicitante,
    },
    {
      key: 'situacao',
      header: 'Situação',
      sortAccessor: (p) => p.situacao,
      render: (p) => (
        <Tag variant={situacaoPedidoTagVariant(p.situacao)}>{situacaoPedidoLabel[p.situacao]}</Tag>
      ),
    },
    {
      key: 'dataAbertura',
      header: 'Abertura',
      sortAccessor: (p) => p.dataAbertura,
      render: (p) => formatarData(p.dataAbertura),
    },
    {
      key: 'prazoVigente',
      header: 'Prazo legal',
      sortAccessor: (p) => p.prazoVigente,
      render: (p) => {
        const prazo = calcularPrazoLegal(p.situacao, p.prazoVigente);
        return (
          <div className="d-flex flex-column">
            <span>{formatarData(p.prazoVigente)}</span>
            {prazo ? (
              <Tag variant={prazoLegalTagVariant(prazo)}>{prazo.rotulo}</Tag>
            ) : (
              <span className="text-muted">—</span>
            )}
          </div>
        );
      },
    },
    {
      key: 'acoes',
      header: 'Ações',
      render: (p) => (
        <Link className="br-button tertiary small" to={`/transparencia/esic/${p.pedidoId}`}>
          Atender
        </Link>
      ),
    },
  ];

  return (
    <>
      <PageHeader
        eyebrow="Transparência · e-SIC interno"
        title="Pedidos de acesso à informação (LAI)"
        description="Fila interna de atendimento dos pedidos e-SIC do ente. O prazo legal (20 dias úteis, prorrogável +10 — LAI art. 11) é exibido em dias úteis restantes."
      />

      <TransparenciaSubNav />

      <Alert variant="info" title="Superfície INTERNA (autenticada)">
        Esta é a gestão interna do e-SIC e exibe dados pessoais do solicitante (cada abertura de
        detalhe gera trilha de acesso — LGPD). O cidadão abre, acompanha e recorre pelo portal
        PÚBLICO em <code>/publico/transparencia/&#123;slug&#125;/esic</code>.
      </Alert>

      <Card className="mb-4">
        <form className="br-form" onSubmit={consultar}>
          <FormRow
            acao={
              <Button variant="primary" type="submit" loading={query.isFetching}>
                Consultar
              </Button>
            }
          >
            <div className="row">
              <div className="col-12 col-sm-4">
                <FormField label="Ano" help="Em branco lista todos os anos.">
                  {({ id, describedBy }) => (
                    <Input
                      id={id}
                      type="number"
                      min="1900"
                      inputMode="numeric"
                      aria-describedby={describedBy}
                      value={ano}
                      onChange={(e) => setAno(e.target.value)}
                    />
                  )}
                </FormField>
              </div>
              <div className="col-12 col-sm-4">
                <FormField label="Situação">
                  {({ id, describedBy }) => (
                    <Select
                      id={id}
                      aria-describedby={describedBy}
                      placeholder="Todas"
                      options={situacaoPedidoOptions}
                      value={situacao}
                      onChange={(e) => setSituacao(e.target.value as SituacaoPedidoSic | '')}
                    />
                  )}
                </FormField>
              </div>
            </div>
          </FormRow>
        </form>
      </Card>

      <DataTable
        caption="Pedidos de acesso à informação (e-SIC) do ente"
        columns={columns}
        rows={query.data}
        rowKey={(p) => p.pedidoId}
        loading={query.isLoading}
        error={query.isError ? errorMessage(query.error) : null}
        empty={
          <EmptyState
            icon="fas fa-folder-open"
            title="Nenhum pedido encontrado"
            description="Não há pedidos e-SIC para o ano e a situação informados."
          />
        }
      />
    </>
  );
}
