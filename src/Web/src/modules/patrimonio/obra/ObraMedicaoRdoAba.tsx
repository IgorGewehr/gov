// Aba "Medição / RDO" da ficha da obra: registrar RDO diário (I-8/I-9), registrar
// medição por etapa (boletim — rascunho) e APROVAR/rejeitar a medição (gated). A
// aprovação libera a liquidação em Finanças (Lei 4.320 art. 63) e a medição
// acumulada não excede o valor contratado (teto — I-1). Só com a obra em execução.
import { useState } from 'react';
import { Button, Card, DataTable, EmptyState, Tag, Toolbar } from '../../../components/ui';
import type { Column, TagVariant } from '../../../components/ui';
import { Can } from '../../../auth/Can';
import { formatarData, formatarMoeda } from '../../../i18n/format';
import type { MedicaoDto, ObraDetalhe, RdoDto } from './obra.api';
import { ObraRdoModal } from './ObraRdoModal';
import { ObraMedicaoModal } from './ObraMedicaoModal';
import { ObraMedicaoAcoesModal } from './ObraMedicaoAcoesModal';
import type { MedicaoAcao } from './ObraMedicaoAcoesModal';

function medicaoTagVariant(s: MedicaoDto['situacao']): TagVariant {
  switch (s) {
    case 'Aprovada':
      return 'success';
    case 'Rejeitada':
      return 'danger';
    default:
      return 'warning';
  }
}

const COLUNAS_RDO: Column<RdoDto>[] = [
  { key: 'data', header: 'Data', sortAccessor: (r) => r.data, render: (r) => formatarData(r.data) },
  { key: 'tempo', header: 'Tempo', render: (r) => r.condicaoTempo },
  { key: 'efetivo', header: 'Efetivo', align: 'end', render: (r) => r.efetivoMaoDeObra },
  { key: 'atividades', header: 'Atividades', render: (r) => r.atividadesExecutadas },
  { key: 'ocorrencias', header: 'Ocorrências', render: (r) => r.ocorrencias ?? '—' },
];

export function ObraMedicaoRdoAba({ obra }: { obra: ObraDetalhe }) {
  const [rdoAberto, setRdoAberto] = useState(false);
  const [medicaoAberta, setMedicaoAberta] = useState(false);
  const [acaoMedicao, setAcaoMedicao] = useState<{ id: string; acao: MedicaoAcao } | null>(null);

  const emExecucao = obra.situacao === 'EmExecucao';

  const colunasMedicao: Column<MedicaoDto>[] = [
    { key: 'numero', header: '#', align: 'end', render: (m) => m.numero },
    {
      key: 'competencia',
      header: 'Competência',
      render: (m) => `${String(m.competenciaMes).padStart(2, '0')}/${m.competenciaAno}`,
    },
    {
      key: 'periodo',
      header: 'Período',
      render: (m) => `${formatarData(m.periodoInicio)} – ${formatarData(m.periodoFim)}`,
    },
    { key: 'fisico', header: '% físico', align: 'end', render: (m) => `${m.percentualFisicoNoPeriodo.toFixed(2)}%` },
    { key: 'valor', header: 'Valor medido', align: 'end', render: (m) => formatarMoeda(m.valorMedido) },
    {
      key: 'situacao',
      header: 'Situação',
      render: (m) => <Tag variant={medicaoTagVariant(m.situacao)}>{m.situacao}</Tag>,
    },
    {
      key: 'acoes',
      header: 'Ações',
      sticky: true,
      render: (m) =>
        m.situacao === 'Rascunho' ? (
          <Can permission="patrimonio.gerenciar">
            <Toolbar>
              <Button variant="primary" size="sm" onClick={() => setAcaoMedicao({ id: m.id, acao: 'aprovar' })}>
                Aprovar
              </Button>
              <Button variant="secondary" size="sm" onClick={() => setAcaoMedicao({ id: m.id, acao: 'rejeitar' })}>
                Rejeitar
              </Button>
            </Toolbar>
          </Can>
        ) : (
          <span className="text-gray-60">
            {m.dataAprovacao ? `Aprovada ${formatarData(m.dataAprovacao)}` : '—'}
          </span>
        ),
    },
  ];

  return (
    <>
      <Card
        className="mb-4"
        header={
          <div className="d-flex justify-content-between align-items-center flex-wrap">
            <strong>Medições (boletins)</strong>
            {emExecucao && (
              <Can permission="patrimonio.gerenciar">
                <Toolbar>
                  <Button variant="primary" size="sm" onClick={() => setMedicaoAberta(true)}>
                    <i className="fas fa-ruler-combined" aria-hidden="true" /> Registrar medição
                  </Button>
                </Toolbar>
              </Can>
            )}
          </div>
        }
        footer={
          <p className="text-down-02 text-gray-60 mb-0">
            A aprovação do fiscal libera a liquidação em Finanças (Lei 4.320, art. 63). O medido
            acumulado nunca excede o valor contratado (teto — I-1).
          </p>
        }
      >
        <DataTable
          caption="Boletins de medição da obra"
          columns={colunasMedicao}
          rows={obra.medicoes}
          rowKey={(m) => m.id}
          empty={
            <EmptyState
              icon="fas fa-ruler-combined"
              title="Sem medições"
              description="Registre o primeiro boletim de medição por etapa."
            />
          }
        />
      </Card>

      <Card
        header={
          <div className="d-flex justify-content-between align-items-center flex-wrap">
            <strong>Registros diários de obra (RDO)</strong>
            {emExecucao && (
              <Can permission="patrimonio.gerenciar">
                <Toolbar>
                  <Button variant="primary" size="sm" onClick={() => setRdoAberto(true)}>
                    <i className="fas fa-clipboard-list" aria-hidden="true" /> Registrar RDO
                  </Button>
                </Toolbar>
              </Can>
            )}
          </div>
        }
      >
        <DataTable
          caption="Registros diários de obra"
          columns={COLUNAS_RDO}
          rows={obra.registrosDiarios}
          rowKey={(r) => r.id}
          empty={
            <EmptyState
              icon="fas fa-clipboard-list"
              title="Sem RDOs"
              description="Registre o diário de obra para a fiscalização contínua."
            />
          }
        />
      </Card>

      <ObraRdoModal open={rdoAberto} onClose={() => setRdoAberto(false)} obraId={obra.id} />
      <ObraMedicaoModal
        open={medicaoAberta}
        onClose={() => setMedicaoAberta(false)}
        obraId={obra.id}
        etapas={obra.etapas}
        valorContratado={obra.valorContratado}
        valorMedidoAcumulado={obra.valorMedidoAcumulado}
      />
      {acaoMedicao && (
        <ObraMedicaoAcoesModal
          open
          onClose={() => setAcaoMedicao(null)}
          obraId={obra.id}
          medicaoId={acaoMedicao.id}
          acao={acaoMedicao.acao}
        />
      )}
    </>
  );
}
