// Aba AUTOS da VISA: fila de autos (intimação/infração/penalidade) filtrável por situação,
// com ações do rito do processo administrativo sanitário — apresentar defesa, julgar
// (deferir/indeferir) e reconhecer regularização de intimação. Endpoints reais sob
// /saude/vigilancia/autos, gated em "saude.vigilancia.ver"; ações em "saude.vigilancia.autuar".
import { useState } from 'react';
import type { ReactNode } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import {
  Button,
  Card,
  DataTable,
  EmptyState,
  FormField,
  FormRow,
  Select,
  Tag,
  errorMessage,
  useToast,
} from '../../components/ui';
import type { Column } from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import { http } from '../../api/http';
import { useHasPermission } from '../../auth/Can';
import { formatarData } from '../../i18n/format';
import { useApresentarDefesa, useAutos } from './vigilancia.api';
import type { AutoVisaDto } from './vigilancia.api';
import { visaKeys } from './vigilancia.keys';
import {
  formatarMoeda,
  opcoesSituacaoAuto,
  situacaoAutoLabel,
  situacaoAutoVariant,
  tipoAutoLabel,
} from './vigilancia.helpers';
import { VisaMotivoModal } from './VisaMotivoModal';

// Mutações que recebem o id no mutate (TanStack hooks fixam o id no closure; aqui é por linha).
function useJulgarAutoPorId() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, deferir }: { id: string; deferir: boolean }) =>
      http.post<void>(`/saude/vigilancia/autos/${id}/julgamento`, { deferir }),
    onSuccess: () => qc.invalidateQueries({ queryKey: visaKeys.autos() }),
  });
}

function useRegularizarPorId() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => http.post<void>(`/saude/vigilancia/autos/${id}/regularizacao`),
    onSuccess: () => qc.invalidateQueries({ queryKey: visaKeys.autos() }),
  });
}

export function VisaAutosTab() {
  const podeAutuar = useHasPermission('saude.vigilancia.autuar');
  const toast = useToast();
  const [statusCampo, setStatusCampo] = useState('');
  const [status, setStatus] = useState('');
  const query = useAutos(status || undefined);

  const [defesaAuto, setDefesaAuto] = useState<AutoVisaDto | null>(null);
  const defesaMut = useApresentarDefesa(defesaAuto?.id ?? '');
  const julgarMut = useJulgarAutoPorId();
  const regularizarMut = useRegularizarPorId();

  function julgar(id: string, deferir: boolean): void {
    julgarMut.mutate(
      { id, deferir },
      {
        onSuccess: () => toast.success(deferir ? 'Auto deferido.' : 'Auto indeferido.', 'Sucesso'),
        onError: (e: unknown) =>
          toast.error(e instanceof ApiError ? e.userMessage : 'Não foi possível julgar o auto.'),
      },
    );
  }

  function regularizar(id: string): void {
    regularizarMut.mutate(id, {
      onSuccess: () => toast.success('Intimação regularizada.', 'Sucesso'),
      onError: (e: unknown) =>
        toast.error(e instanceof ApiError ? e.userMessage : 'Não foi possível regularizar.'),
    });
  }

  const columns: Column<AutoVisaDto>[] = [
    {
      key: 'numero',
      header: 'Número',
      render: (a) => (
        <>
          <strong>{a.numero}</strong>
          <span className="d-block text-down-01 text-secondary">{tipoAutoLabel[a.tipo]}</span>
        </>
      ),
    },
    { key: 'lavratura', header: 'Lavratura', render: (a) => formatarData(a.dataLavratura) },
    { key: 'prazo', header: 'Prazo final', render: (a) => formatarData(a.prazoFinal) },
    { key: 'multa', header: 'Multa', render: (a) => formatarMoeda(a.valorMulta) },
    {
      key: 'situacao',
      header: 'Situação',
      render: (a) => <Tag variant={situacaoAutoVariant(a.situacao)}>{situacaoAutoLabel[a.situacao]}</Tag>,
    },
    {
      key: 'acoes',
      header: 'Ações',
      sticky: true,
      render: (a) => {
        if (!podeAutuar) return <span className="text-secondary">—</span>;
        const acoes: ReactNode[] = [];
        if (a.tipo !== 'Intimacao' && a.situacao === 'Lavrado') {
          acoes.push(
            <Button key="def" variant="secondary" className="small" onClick={() => setDefesaAuto(a)}>
              Defesa
            </Button>,
          );
        }
        if (a.situacao === 'DefesaApresentada') {
          acoes.push(
            <Button key="def-ok" variant="secondary" className="small" onClick={() => julgar(a.id, true)}>
              Deferir
            </Button>,
            <Button key="def-no" variant="secondary" className="small" onClick={() => julgar(a.id, false)}>
              Indeferir
            </Button>,
          );
        }
        if (a.tipo === 'Intimacao' && a.situacao === 'Lavrado') {
          acoes.push(
            <Button key="reg" variant="secondary" className="small" onClick={() => regularizar(a.id)}>
              Regularizar
            </Button>,
          );
        }
        return acoes.length > 0 ? (
          <div className="d-flex" style={{ gap: '0.5rem' }}>
            {acoes}
          </div>
        ) : (
          <span className="text-secondary">—</span>
        );
      },
    },
  ];

  return (
    <>
      <Card className="mb-4">
        <form
          className="br-form"
          onSubmit={(e) => {
            e.preventDefault();
            setStatus(statusCampo);
          }}
        >
          <FormRow
            acao={
              <Button variant="primary" type="submit" loading={query.isFetching}>
                Filtrar
              </Button>
            }
          >
            <div className="row">
              <div className="col-12 col-md-4">
                <FormField label="Situação do processo">
                  {({ id }) => (
                    <Select
                      id={id}
                      options={opcoesSituacaoAuto}
                      placeholder="Todas"
                      value={statusCampo}
                      onChange={(e) => setStatusCampo(e.target.value)}
                    />
                  )}
                </FormField>
              </div>
            </div>
          </FormRow>
        </form>
      </Card>

      <DataTable
        caption="Autos do processo administrativo sanitário"
        columns={columns}
        rows={query.data}
        rowKey={(a) => a.id}
        loading={query.isLoading}
        error={query.isError ? errorMessage(query.error) : null}
        empty={
          <EmptyState
            icon="fas fa-gavel"
            title="Nenhum auto encontrado"
            description="Autos são lavrados a partir de inspeções concluídas (aba Inspeções)."
          />
        }
      />

      <VisaMotivoModal
        open={defesaAuto !== null}
        onClose={() => setDefesaAuto(null)}
        title={`Defesa do auto ${defesaAuto?.numero ?? ''}`}
        label="Razões da defesa"
        acaoLabel="Apresentar defesa"
        sucessoMensagem="Defesa apresentada."
        pendente={defesaMut.isPending}
        executar={(texto) => defesaMut.mutateAsync(texto)}
      />
    </>
  );
}
