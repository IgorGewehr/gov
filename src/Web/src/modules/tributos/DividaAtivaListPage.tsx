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
  FormRow,
  Input,
  PageHeader,
  Tag,
  Toolbar,
} from '../../components/ui';
import type { Column } from '../../components/ui';
import { formatarData, formatarMoeda } from '../../i18n/format';
import { errorMessage } from '../../components/ui';
import { Can } from '../../auth/Can';
import { useDividasPorContribuinte } from './api';
import type { DividaAtivaResumo } from './api';
import {
  SITUACAO_DIVIDA_LABEL,
  podeEmitirCda,
  podeExecutar,
  podeProtestar,
  situacaoTagVariant,
} from './dividaAtiva.helpers';
import { TributosSubNav } from './TributosSubNav';
import { ContribuinteFormModal } from './ContribuinteFormModal';
import { LancamentoFormModal } from './LancamentoFormModal';
import { InscreverDividaModal } from './InscreverDividaModal';
import { EmitirCdaModal } from './EmitirCdaModal';
import { ProtestoModal } from './ProtestoModal';
import { ExecucaoFiscalModal } from './ExecucaoFiscalModal';
import { PrescricaoModal } from './PrescricaoModal';

const PERM_GERENCIAR = 'tributos.gerenciar';

export function DividaAtivaListPage() {
  const [contribuinteId, setContribuinteId] = useState('');
  const [consultaAtiva, setConsultaAtiva] = useState('');
  const [contribAberto, setContribAberto] = useState(false);
  const [lancamentoAberto, setLancamentoAberto] = useState(false);
  const [inscreverAberto, setInscreverAberto] = useState(false);
  const [cdaDe, setCdaDe] = useState<string | null>(null);
  const [protestoDe, setProtestoDe] = useState<string | null>(null);
  const [execucaoDe, setExecucaoDe] = useState<string | null>(null);
  const [prescricaoDe, setPrescricaoDe] = useState<string | null>(null);

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
      key: 'numeroInscricao',
      header: 'Inscrição nº',
      align: 'end',
      sortAccessor: (d) => d.numeroInscricao,
      render: (d) => d.numeroInscricao,
    },
    {
      key: 'valor',
      header: 'Valor originário',
      align: 'end',
      sortAccessor: (d) => d.valorOriginario,
      render: (d) => formatarMoeda(d.valorOriginario),
    },
    {
      key: 'inscricao',
      header: 'Data inscrição',
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
        <Toolbar>
          <Button size="sm" variant="ghost" onClick={() => setPrescricaoDe(d.id)}>
            <i className="fas fa-hourglass-half" aria-hidden="true" /> Prescrição
          </Button>
          <Can permission={PERM_GERENCIAR}>
            {podeEmitirCda(d.situacao) && (
              <Button size="sm" variant="ghost" onClick={() => setCdaDe(d.id)}>
                <i className="fas fa-stamp" aria-hidden="true" /> Emitir CDA
              </Button>
            )}
            {podeProtestar(d.situacao) && (
              <Button size="sm" variant="ghost" onClick={() => setProtestoDe(d.id)}>
                <i className="fas fa-file-signature" aria-hidden="true" /> Protesto
              </Button>
            )}
            {podeExecutar(d.situacao) && (
              <Button size="sm" variant="ghost" onClick={() => setExecucaoDe(d.id)}>
                <i className="fas fa-gavel" aria-hidden="true" /> Execução fiscal
              </Button>
            )}
          </Can>
        </Toolbar>
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
            <Toolbar>
              <Button variant="secondary" onClick={() => setContribAberto(true)}>
                <i className="fas fa-user-plus" aria-hidden="true" /> Cadastrar contribuinte
              </Button>
              <Button variant="secondary" onClick={() => setLancamentoAberto(true)}>
                <i className="fas fa-plus" aria-hidden="true" /> Lançar crédito
              </Button>
              <Button variant="primary" onClick={() => setInscreverAberto(true)}>
                <i className="fas fa-file-import" aria-hidden="true" /> Inscrever em Dívida Ativa
              </Button>
            </Toolbar>
          </Can>
        }
      />

      <TributosSubNav />

      <Card className="mb-4">
        <form className="br-form" onSubmit={consultar}>
          <FormRow
            acao={
              <Button variant="primary" type="submit" disabled={contribuinteId.trim() === ''} loading={query.isFetching}>
                Consultar
              </Button>
            }
          >
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
          </FormRow>
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
      <InscreverDividaModal
        open={inscreverAberto}
        onClose={() => setInscreverAberto(false)}
        contribuinteIdParaInvalidar={consultaAtiva}
      />
      <EmitirCdaModal
        open={cdaDe !== null}
        onClose={() => setCdaDe(null)}
        dividaAtivaId={cdaDe ?? ''}
        contribuinteIdParaInvalidar={consultaAtiva}
      />
      <ProtestoModal
        open={protestoDe !== null}
        onClose={() => setProtestoDe(null)}
        dividaAtivaId={protestoDe ?? ''}
        contribuinteIdParaInvalidar={consultaAtiva}
      />
      <ExecucaoFiscalModal
        open={execucaoDe !== null}
        onClose={() => setExecucaoDe(null)}
        dividaAtivaId={execucaoDe ?? ''}
        contribuinteIdParaInvalidar={consultaAtiva}
      />
      <PrescricaoModal
        open={prescricaoDe !== null}
        onClose={() => setPrescricaoDe(null)}
        dividaAtivaId={prescricaoDe ?? ''}
      />
    </>
  );
}
