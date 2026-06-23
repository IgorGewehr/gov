// Aba INSPEÇÕES da VISA: agenda/histórico por intervalo de datas + filtro de situação, abrir
// nova inspeção e conduzir (detalhe em modal: checklist, conclusão, auto, cancelamento).
// Endpoints reais sob /saude/vigilancia/inspecoes, gated em "saude.vigilancia.ver"; ações em
// "saude.vigilancia.inspecionar".
import { useState } from 'react';
import type { FormEvent } from 'react';
import {
  Button,
  Card,
  DataTable,
  EmptyState,
  FormField,
  FormRow,
  Input,
  Select,
  Tag,
  Toolbar,
  errorMessage,
} from '../../components/ui';
import type { Column } from '../../components/ui';
import { Can } from '../../auth/Can';
import { formatarData } from '../../i18n/format';
import { useAgendaInspecoes } from './vigilancia.api';
import type { InspecaoDto } from './vigilancia.api';
import {
  opcoesSituacaoInspecao,
  resultadoLabel,
  resultadoVariant,
  situacaoInspecaoLabel,
  situacaoInspecaoVariant,
} from './vigilancia.helpers';
import { AbrirInspecaoModal } from './VisaInspecaoModals';
import { VisaInspecaoDetalheModal } from './VisaInspecaoDetalheModal';

function hoje(): string {
  return new Date().toISOString().slice(0, 10);
}

function diasAtras(dias: number): string {
  const d = new Date();
  d.setDate(d.getDate() - dias);
  return d.toISOString().slice(0, 10);
}

export function VisaInspecoesTab() {
  const [deCampo, setDeCampo] = useState(diasAtras(30));
  const [ateCampo, setAteCampo] = useState(hoje());
  const [situacaoCampo, setSituacaoCampo] = useState('');
  const [de, setDe] = useState(diasAtras(30));
  const [ate, setAte] = useState(hoje());
  const [situacao, setSituacao] = useState('');

  const [abrirAberto, setAbrirAberto] = useState(false);
  const [inspecaoId, setInspecaoId] = useState<string | null>(null);

  const query = useAgendaInspecoes(de, ate, situacao || undefined);

  function aplicar(event: FormEvent): void {
    event.preventDefault();
    setDe(deCampo);
    setAte(ateCampo);
    setSituacao(situacaoCampo);
  }

  const columns: Column<InspecaoDto>[] = [
    { key: 'data', header: 'Data', sortAccessor: (i) => i.dataInspecao, render: (i) => formatarData(i.dataInspecao) },
    { key: 'roteiro', header: 'Roteiro', render: (i) => i.roteiro || '—' },
    {
      key: 'situacao',
      header: 'Situação',
      render: (i) => <Tag variant={situacaoInspecaoVariant(i.situacao)}>{situacaoInspecaoLabel[i.situacao]}</Tag>,
    },
    {
      key: 'resultado',
      header: 'Resultado',
      render: (i) =>
        i.resultado ? <Tag variant={resultadoVariant(i.resultado)}>{resultadoLabel[i.resultado]}</Tag> : '—',
    },
    { key: 'pendencias', header: 'Pendências', render: (i) => i.pendencias },
    {
      key: 'acoes',
      header: 'Ações',
      sticky: true,
      render: (i) => (
        <Button variant="secondary" className="small" onClick={() => setInspecaoId(i.id)}>
          <i className="fas fa-clipboard-check" aria-hidden="true" /> Conduzir
        </Button>
      ),
    },
  ];

  return (
    <>
      <Toolbar className="mb-3">
        <Can permission="saude.vigilancia.inspecionar">
          <Button variant="primary" onClick={() => setAbrirAberto(true)}>
            <i className="fas fa-plus" aria-hidden="true" /> Abrir inspeção
          </Button>
        </Can>
      </Toolbar>

      <Card className="mb-4">
        <form className="br-form" onSubmit={aplicar}>
          <FormRow
            acao={
              <Button variant="primary" type="submit" loading={query.isFetching}>
                Aplicar
              </Button>
            }
          >
            <div className="row">
              <div className="col-6 col-md-4">
                <FormField label="De">
                  {({ id }) => <Input id={id} type="date" value={deCampo} onChange={(e) => setDeCampo(e.target.value)} />}
                </FormField>
              </div>
              <div className="col-6 col-md-4">
                <FormField label="Até">
                  {({ id }) => <Input id={id} type="date" value={ateCampo} onChange={(e) => setAteCampo(e.target.value)} />}
                </FormField>
              </div>
              <div className="col-12 col-md-4">
                <FormField label="Situação">
                  {({ id }) => (
                    <Select
                      id={id}
                      options={opcoesSituacaoInspecao}
                      placeholder="Todas"
                      value={situacaoCampo}
                      onChange={(e) => setSituacaoCampo(e.target.value)}
                    />
                  )}
                </FormField>
              </div>
            </div>
          </FormRow>
        </form>
      </Card>

      <DataTable
        caption="Agenda de inspeções sanitárias"
        columns={columns}
        rows={query.data}
        rowKey={(i) => i.id}
        loading={query.isLoading}
        error={query.isError ? errorMessage(query.error) : null}
        empty={
          <EmptyState
            icon="fas fa-clipboard-list"
            title="Nenhuma inspeção no período"
            description="Ajuste o intervalo de datas ou abra uma nova inspeção."
          />
        }
      />

      <AbrirInspecaoModal open={abrirAberto} onClose={() => setAbrirAberto(false)} />
      {inspecaoId && (
        <VisaInspecaoDetalheModal
          open={inspecaoId !== null}
          onClose={() => setInspecaoId(null)}
          inspecaoId={inspecaoId}
        />
      )}
    </>
  );
}
