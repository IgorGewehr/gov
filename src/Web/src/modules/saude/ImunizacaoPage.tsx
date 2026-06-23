// Tela da IMUNIZAÇÃO (SI-PNI): catálogo de imunobiológicos (PNI), carteira de vacinação do
// paciente (situação Completo/EmDia/Atrasado + aprazamento) e busca ativa de aprazamentos
// vencidos. Catálogo gated em "saude.imunizacao.ver"; carteira e busca ativa são SENSÍVEIS
// (LGPD) gated em "saude.prontuario.ler"; registro de dose em "saude.imunizacao.aplicar".
import { useMemo, useState } from 'react';
import {
  Button,
  Card,
  DataTable,
  EmptyState,
  PageHeader,
  Tag,
  Toolbar,
} from '../../components/ui';
import type { Column } from '../../components/ui';
import { errorMessage } from '../../components/ui';
import { Can, useHasPermission } from '../../auth/Can';
import { formatarData } from '../../i18n/format';
import {
  useAprazamentosVencidos,
  useCarteiraVacinacao,
  useImunobiologicos,
} from './api';
import type { AprazamentoVencidoDto, DoseAplicadaDto, ImunobiologicoItemLista } from './api';
import { situacaoVacinalDeAprazamento, situacaoVacinalVariant } from './saude.helpers';
import { SaudeSubNav } from './SaudeSubNav';
import { PacientePicker } from './AgendamentoPickers';
import { CadastrarImunobiologicoModal, AplicarDoseModal } from './ImunizacaoModals';

export function ImunizacaoPage() {
  const podeLerProntuario = useHasPermission('saude.prontuario.ler');
  const hojeIso = new Date().toISOString().slice(0, 10);

  const [cadastrarAberto, setCadastrarAberto] = useState(false);
  const [aplicarAberto, setAplicarAberto] = useState(false);
  const [pacienteId, setPacienteId] = useState('');

  const catalogo = useImunobiologicos();
  const carteira = useCarteiraVacinacao(pacienteId, podeLerProntuario);
  const aprazamentos = useAprazamentosVencidos(podeLerProntuario);

  const colunasCatalogo: Column<ImunobiologicoItemLista>[] = [
    { key: 'sigla', header: 'Sigla', sortAccessor: (i) => i.sigla, render: (i) => <Tag variant="info">{i.sigla}</Tag> },
    { key: 'nome', header: 'Imunobiológico', render: (i) => i.nome },
    {
      key: 'esquema',
      header: 'Esquema',
      render: (i) => (i.doseUnica ? 'Dose única' : `${i.totalDoses} doses`),
    },
    {
      key: 'intervalo',
      header: 'Intervalo (dias)',
      render: (i) => (i.intervaloDiasProximaDose > 0 ? i.intervaloDiasProximaDose : '—'),
    },
  ];

  const colunasCarteira: Column<DoseAplicadaDto>[] = [
    { key: 'sigla', header: 'Vacina', render: (d) => <Tag variant="info">{d.sigla}</Tag> },
    { key: 'tipoDose', header: 'Dose', render: (d) => `${d.tipoDose} (${d.numeroDose})` },
    { key: 'lote', header: 'Lote', render: (d) => d.lote },
    { key: 'dataAplicacao', header: 'Aplicada em', sortAccessor: (d) => d.dataAplicacao, render: (d) => formatarData(d.dataAplicacao) },
    {
      key: 'situacao',
      header: 'Situação',
      render: (d) => {
        const situacao = situacaoVacinalDeAprazamento(d.proximaDoseAprazada, hojeIso);
        const detalhe = d.proximaDoseAprazada ? ` · próx. ${formatarData(d.proximaDoseAprazada)}` : '';
        return <Tag variant={situacaoVacinalVariant(situacao)}>{situacao}{detalhe}</Tag>;
      },
    },
  ];

  const colunasAprazamentos: Column<AprazamentoVencidoDto>[] = [
    { key: 'sigla', header: 'Vacina', render: (a) => <Tag variant="info">{a.sigla}</Tag> },
    { key: 'ultimaDose', header: 'Última dose', render: (a) => a.ultimaDose },
    {
      key: 'dataAprazada',
      header: 'Aprazada para',
      sortAccessor: (a) => a.dataAprazada,
      render: (a) => <Tag variant="danger">{formatarData(a.dataAprazada)}</Tag>,
    },
    { key: 'paciente', header: 'Paciente (id)', render: (a) => <span className="text-down-01">{a.pacienteId}</span> },
  ];

  const dosesOrdenadas = useMemo(() => carteira.data?.doses ?? [], [carteira.data]);

  return (
    <>
      <SaudeSubNav />
      <PageHeader
        eyebrow="Saúde"
        title="Imunização"
        description="Catálogo (PNI), carteira de vacinação do paciente e busca ativa de aprazamentos vencidos."
        actions={
          <Toolbar>
            <Can permission="saude.imunizacao.aplicar">
              <Button variant="secondary" onClick={() => setAplicarAberto(true)} disabled={pacienteId === ''}>
                <i className="fas fa-syringe" aria-hidden="true" /> Registrar dose
              </Button>
            </Can>
            <Can permission="saude.imunizacao.gerenciar">
              <Button variant="primary" onClick={() => setCadastrarAberto(true)}>
                <i className="fas fa-plus" aria-hidden="true" /> Novo imunobiológico
              </Button>
            </Can>
          </Toolbar>
        }
      />

      <Card className="mb-4">
        <h2 className="text-up-01 mb-3">Catálogo de imunobiológicos</h2>
        <DataTable
          caption="Imunobiológicos do PNI"
          columns={colunasCatalogo}
          rows={catalogo.data}
          rowKey={(i) => i.id}
          loading={catalogo.isLoading}
          error={catalogo.isError ? errorMessage(catalogo.error) : null}
          empty={<EmptyState icon="fas fa-vials" title="Sem imunobiológicos" description="Cadastre o primeiro item do PNI." />}
        />
      </Card>

      <Can
        permission="saude.prontuario.ler"
        fallback={
          <Card className="mb-4">
            <EmptyState
              icon="fas fa-lock"
              title="Carteira de vacinação restrita"
              description="Requer a permissão de leitura de prontuário (dado sensível — LGPD)."
            />
          </Card>
        }
      >
        <Card className="mb-4">
          <h2 className="text-up-01 mb-3">Carteira de vacinação do paciente</h2>
          <PacientePicker label="Paciente" value={pacienteId} onChange={setPacienteId} />
          {pacienteId !== '' && (
            <div className="mt-3">
              <DataTable
                caption="Doses aplicadas"
                columns={colunasCarteira}
                rows={dosesOrdenadas}
                rowKey={(d) => `${d.imunobiologicoId}-${d.numeroDose}-${d.dataAplicacao}`}
                loading={carteira.isLoading}
                error={carteira.isError ? errorMessage(carteira.error) : null}
                empty={<EmptyState icon="fas fa-id-card" title="Carteira vazia" description="Nenhuma dose registrada para o paciente." />}
              />
            </div>
          )}
        </Card>

        <Card className="mb-4" accent="danger">
          <h2 className="text-up-01 mb-3">Busca ativa — aprazamentos vencidos</h2>
          <DataTable
            caption="Aprazamentos vencidos"
            columns={colunasAprazamentos}
            rows={aprazamentos.data}
            rowKey={(a) => `${a.pacienteId}-${a.imunobiologicoId}`}
            loading={aprazamentos.isLoading}
            error={aprazamentos.isError ? errorMessage(aprazamentos.error) : null}
            empty={<EmptyState icon="fas fa-circle-check" title="Sem pendências" description="Nenhum aprazamento vencido." />}
          />
        </Card>
      </Can>

      <CadastrarImunobiologicoModal open={cadastrarAberto} onClose={() => setCadastrarAberto(false)} />
      <AplicarDoseModal open={aplicarAberto} pacienteId={pacienteId} onClose={() => setAplicarAberto(false)} />
    </>
  );
}
