// Tela de LISTA (consulta) de Dívida Ativa por contribuinte (query
// ObterDividasAtivasDoContribuinte). Busca sob demanda (enabled), DataTable com
// estados loading/vazio/erro + ordenação, e ações por linha (Emitir CDA) e de
// cabeçalho (cadastrar contribuinte, lançar crédito) — todas gated por permissão.
import { useState } from 'react';
import type { FormEvent } from 'react';
import {
  Button,
  Card,
  DataTable,
  EmptyState,
  FormField,
  Input,
  PageHeader,
  Tag,
} from '../../components/ui';
import type { Column } from '../../components/ui';
import { formatarData, formatarMoeda } from '../../i18n/format';
import { errorMessage } from '../../components/ui';
import { Can } from '../../auth/Can';
import { useDividasPorContribuinte } from './api';
import type { DividaAtivaResumo } from './api';
import { SITUACAO_DIVIDA_LABEL, podeEmitirCda, situacaoTagVariant } from './dividaAtiva.helpers';
import { ContribuinteFormModal } from './ContribuinteFormModal';
import { LancamentoFormModal } from './LancamentoFormModal';
import { EmitirCdaModal } from './EmitirCdaModal';

const PERM_GERENCIAR = 'tributos.gerenciar';

export function DividaAtivaListPage() {
  const [contribuinteId, setContribuinteId] = useState('');
  const [consultaAtiva, setConsultaAtiva] = useState('');
  const [contribAberto, setContribAberto] = useState(false);
  const [lancamentoAberto, setLancamentoAberto] = useState(false);
  const [cdaDe, setCdaDe] = useState<string | null>(null);

  const query = useDividasPorContribuinte(consultaAtiva, consultaAtiva.length > 0);

  function consultar(event: FormEvent): void {
    event.preventDefault();
    setConsultaAtiva(contribuinteId.trim());
  }

  const columns: Column<DividaAtivaResumo>[] = [
    {
      key: 'situacao',
      header: 'Situação',
      sortAccessor: (d) => d.situacao,
      render: (d) => <Tag variant={situacaoTagVariant(d.situacao)}>{SITUACAO_DIVIDA_LABEL[d.situacao]}</Tag>,
    },
    {
      key: 'valor',
      header: 'Valor inscrito',
      align: 'end',
      sortAccessor: (d) => d.valorInscrito,
      render: (d) => formatarMoeda(d.valorInscrito),
    },
    {
      key: 'inscricao',
      header: 'Inscrição',
      sortAccessor: (d) => d.dataInscricao,
      render: (d) => formatarData(d.dataInscricao),
    },
    {
      key: 'prescricao',
      header: 'Prescrição',
      sortAccessor: (d) => d.dataPrescricao,
      render: (d) => formatarData(d.dataPrescricao),
    },
    { key: 'cda', header: 'CDA', render: (d) => d.numeroCda ?? '—' },
    {
      key: 'acoes',
      header: 'Ações',
      render: (d) => (
        <Can permission={PERM_GERENCIAR}>
          {podeEmitirCda(d.situacao) ? (
            <Button variant="tertiary" onClick={() => setCdaDe(d.id)}>
              <i className="fas fa-stamp" aria-hidden="true" /> Emitir CDA
            </Button>
          ) : (
            '—'
          )}
        </Can>
      ),
    },
  ];

  return (
    <>
      <PageHeader
        title="Dívida Ativa"
        description="Consulte os débitos inscritos em Dívida Ativa de um contribuinte e emita a CDA."
        actions={
          <Can permission={PERM_GERENCIAR}>
            <Button variant="secondary" onClick={() => setContribAberto(true)}>
              <i className="fas fa-user-plus" aria-hidden="true" /> Cadastrar contribuinte
            </Button>{' '}
            <Button variant="primary" onClick={() => setLancamentoAberto(true)}>
              <i className="fas fa-plus" aria-hidden="true" /> Lançar crédito
            </Button>
          </Can>
        }
      />

      <Card className="mb-4">
        <form className="br-form" onSubmit={consultar}>
          <div className="row align-items-end">
            <div className="col">
              <FormField label="Identificador do contribuinte" required>
                {({ id, describedBy, invalid }) => (
                  <Input
                    id={id}
                    aria-describedby={describedBy}
                    invalid={invalid}
                    value={contribuinteId}
                    onChange={(e) => setContribuinteId(e.target.value)}
                    placeholder="00000000-0000-0000-0000-000000000000"
                  />
                )}
              </FormField>
            </div>
            <div className="col-auto mb-3">
              <Button variant="primary" type="submit" disabled={contribuinteId.trim() === ''} loading={query.isFetching}>
                Consultar
              </Button>
            </div>
          </div>
        </form>
      </Card>

      {consultaAtiva === '' ? (
        <EmptyState
          icon="fas fa-magnifying-glass"
          title="Faça uma consulta"
          description="Informe o identificador do contribuinte e clique em Consultar."
        />
      ) : (
        <DataTable
          caption={`Dívidas ativas do contribuinte ${consultaAtiva}`}
          columns={columns}
          rows={query.data}
          rowKey={(d) => d.id}
          loading={query.isLoading}
          error={query.isError ? errorMessage(query.error) : null}
          empty={
            <EmptyState
              icon="fas fa-circle-check"
              title="Nenhuma dívida ativa encontrada"
              description="Este contribuinte não possui débitos inscritos em Dívida Ativa."
            />
          }
        />
      )}

      <ContribuinteFormModal open={contribAberto} onClose={() => setContribAberto(false)} />
      <LancamentoFormModal
        open={lancamentoAberto}
        onClose={() => setLancamentoAberto(false)}
        contribuinteIdInicial={consultaAtiva}
      />
      <EmitirCdaModal
        open={cdaDe !== null}
        onClose={() => setCdaDe(null)}
        dividaAtivaId={cdaDe ?? ''}
        contribuinteIdParaInvalidar={consultaAtiva}
      />
    </>
  );
}
