// Histórico de DISPENSAÇÃO do paciente (o que recebeu, quando, onde) — dado SENSÍVEL de
// saúde (LGPD art. 11): gera trilha de acesso no backend. Gated em "saude.prontuario.ler".
// Permite filtrar por janela de datas e estornar uma dispensação efetivada (saude.farmacia.dispensar).
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
  Tag,
} from '../../components/ui';
import type { Column } from '../../components/ui';
import { errorMessage } from '../../components/ui';
import { Can } from '../../auth/Can';
import { formatarDataHora } from '../../i18n/format';
import { useDispensacoesPaciente } from './api';
import type { DispensacaoDto } from './api';
import { dispensacaoEfetivada, situacaoDispensacaoVariant } from './saude.helpers';
import { PacientePicker } from './AgendamentoPickers';
import { EstornarDispensacaoModal } from './FarmaciaModals';

export function DispensacaoHistorico() {
  const [pacienteId, setPacienteId] = useState('');
  const [deCampo, setDeCampo] = useState('');
  const [ateCampo, setAteCampo] = useState('');
  const [de, setDe] = useState('');
  const [ate, setAte] = useState('');
  const [estornoId, setEstornoId] = useState('');

  const filtro = useMemo(() => ({ de: de || undefined, ate: ate || undefined }), [de, ate]);
  const query = useDispensacoesPaciente(pacienteId, filtro, pacienteId !== '');

  function aplicar(event: FormEvent): void {
    event.preventDefault();
    setDe(deCampo);
    setAte(ateCampo);
  }

  const colunas: Column<DispensacaoDto>[] = [
    { key: 'dataHora', header: 'Data/hora', sortAccessor: (d) => d.dataHora, render: (d) => formatarDataHora(d.dataHora) },
    {
      key: 'situacao',
      header: 'Situação',
      render: (d) => <Tag variant={situacaoDispensacaoVariant(d.situacao)}>{d.situacao}</Tag>,
    },
    {
      key: 'itens',
      header: 'Itens',
      render: (d) => (
        <ul className="mb-0 pl-3">
          {d.itens.map((i, idx) => (
            <li key={`${i.medicamentoId}-${idx}`}>
              {i.quantidade} · {i.posologia}
            </li>
          ))}
        </ul>
      ),
    },
    {
      key: 'acoes',
      header: 'Ações',
      sticky: true,
      render: (d) =>
        dispensacaoEfetivada(d.situacao) ? (
          <Can permission="saude.farmacia.dispensar">
            <Button variant="secondary" className="small" onClick={() => setEstornoId(d.id)}>
              <i className="fas fa-rotate-left" aria-hidden="true" /> Estornar
            </Button>
          </Can>
        ) : (
          <span className="text-secondary">—</span>
        ),
    },
  ];

  return (
    <Card className="mb-4">
      <h2 className="text-up-01 mb-3">Histórico de dispensação do paciente</h2>
      <p className="text-secondary text-down-01">Acesso registrado em trilha de auditoria (LGPD).</p>
      <PacientePicker label="Paciente" value={pacienteId} onChange={setPacienteId} />

      {pacienteId !== '' && (
        <>
          <form className="br-form mt-3" onSubmit={aplicar}>
            <FormRow
              acao={
                <Button variant="primary" type="submit" loading={query.isFetching}>
                  Filtrar
                </Button>
              }
            >
              <div className="row">
                <div className="col-6">
                  <FormField label="De">
                    {({ id }) => <Input id={id} type="date" value={deCampo} onChange={(e) => setDeCampo(e.target.value)} />}
                  </FormField>
                </div>
                <div className="col-6">
                  <FormField label="Até">
                    {({ id }) => <Input id={id} type="date" value={ateCampo} onChange={(e) => setAteCampo(e.target.value)} />}
                  </FormField>
                </div>
              </div>
            </FormRow>
          </form>

          <div className="mt-3">
            <DataTable
              caption="Dispensações do paciente"
              columns={colunas}
              rows={query.data}
              rowKey={(d) => d.id}
              loading={query.isLoading}
              error={query.isError ? errorMessage(query.error) : null}
              empty={<EmptyState icon="fas fa-prescription-bottle-medical" title="Sem dispensações" description="Nenhuma entrega registrada na janela." />}
            />
          </div>
        </>
      )}

      <EstornarDispensacaoModal open={estornoId !== ''} dispensacaoId={estornoId} onClose={() => setEstornoId('')} />
    </Card>
  );
}
