// PRONTUÁRIO do paciente. Padrão-ouro: param de rota -> useQuery, QueryState para o
// histórico clínico (dado sensível LGPD art. 11), DataTable da linha do tempo de
// atendimentos e ações de registrar atendimento / solicitar regulação (mutations).
import { useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import {
  Button,
  Card,
  DataTable,
  EmptyState,
  PageHeader,
  QueryState,
  Tag,
} from '../../components/ui';
import type { Column } from '../../components/ui';
import { errorMessage } from '../../components/ui';
import { Can } from '../../auth/Can';
import { formatarData } from '../../i18n/format';
import {
  useAtendimentosDoPaciente,
  useHistoricoClinico,
} from './api';
import type { AtendimentoResumo, HistoricoClinico } from './api';
import { situacaoAtendimentoVariant } from './saude.helpers';
import { AtendimentoFormModal } from './AtendimentoFormModal';
import { RegulacaoFormModal } from './RegulacaoFormModal';
import { PacienteEditModal } from './PacienteEditModal';
import { ConfirmarCadsusModal, InativarPacienteModal } from './PacienteAcaoModals';
import { RegistrarAlergiaModal, RegistrarCondicaoModal } from './PacienteHistoricoModals';

type AcaoPaciente =
  | 'editar'
  | 'cadsus'
  | 'condicao'
  | 'alergia'
  | 'inativar'
  | null;

export function PacienteDetailPage() {
  const { pacienteId = '' } = useParams<{ pacienteId: string }>();
  const historicoQuery = useHistoricoClinico(pacienteId);
  const atendimentosQuery = useAtendimentosDoPaciente(pacienteId);

  const [atendimentoAberto, setAtendimentoAberto] = useState(false);
  const [regulacaoAberta, setRegulacaoAberta] = useState(false);
  const [acaoPaciente, setAcaoPaciente] = useState<AcaoPaciente>(null);

  const colunasAtendimentos: Column<AtendimentoResumo>[] = [
    {
      key: 'dataHora',
      header: 'Data',
      sortAccessor: (a) => a.dataHora,
      render: (a) => formatarData(a.dataHora),
    },
    { key: 'modalidade', header: 'Modalidade', render: (a) => a.modalidade },
    {
      key: 'situacao',
      header: 'Situação',
      sortAccessor: (a) => a.situacao,
      render: (a) => <Tag variant={situacaoAtendimentoVariant(a.situacao)}>{a.situacao}</Tag>,
    },
    { key: 'cid', header: 'CID-10', render: (a) => a.cid ?? '—' },
    { key: 'ciap', header: 'CIAP-2', render: (a) => a.ciap ?? '—' },
    {
      key: 'acoes',
      header: 'Ações',
      render: (a) => (
        <Link className="br-button tertiary small" to={`/saude/atendimentos/${a.id}`}>
          Detalhes
        </Link>
      ),
    },
  ];

  return (
    <>
      <PageHeader
        title="Prontuário do paciente"
        description="Histórico clínico e linha do tempo de atendimentos. Acesso registrado em trilha de auditoria (LGPD)."
        actions={
          <div className="d-flex flex-wrap" style={{ gap: '0.5rem' }}>
            <Can permission="saude.gerenciar">
              <Button variant="secondary" onClick={() => setRegulacaoAberta(true)}>
                <i className="fas fa-share-from-square" aria-hidden="true" /> Solicitar regulação
              </Button>
              <Button variant="primary" onClick={() => setAtendimentoAberto(true)}>
                <i className="fas fa-notes-medical" aria-hidden="true" /> Registrar atendimento
              </Button>
            </Can>
            <Link className="br-button secondary" to="/saude">
              <i className="fas fa-arrow-left" aria-hidden="true" /> Voltar
            </Link>
          </div>
        }
      />

      <Can permission="saude.gerenciar">
        <Card className="mb-4" header={<strong>Ações do cadastro</strong>}>
          <div className="d-flex gap-2 flex-wrap">
            <Button variant="secondary" onClick={() => setAcaoPaciente('editar')}>
              <i className="fas fa-user-pen" aria-hidden="true" /> Atualizar cadastro
            </Button>
            <Button variant="secondary" onClick={() => setAcaoPaciente('cadsus')}>
              <i className="fas fa-id-card" aria-hidden="true" /> Confirmar CADSUS
            </Button>
            <Button variant="secondary" onClick={() => setAcaoPaciente('condicao')}>
              <i className="fas fa-stethoscope" aria-hidden="true" /> Registrar condição
            </Button>
            <Button variant="secondary" onClick={() => setAcaoPaciente('alergia')}>
              <i className="fas fa-triangle-exclamation" aria-hidden="true" /> Registrar alergia
            </Button>
            <Button variant="danger" onClick={() => setAcaoPaciente('inativar')}>
              <i className="fas fa-user-slash" aria-hidden="true" /> Inativar paciente
            </Button>
          </div>
        </Card>
      </Can>

      <Card className="mb-4" header={<strong>Histórico clínico</strong>}>
        <QueryState<HistoricoClinico>
          isLoading={historicoQuery.isLoading}
          isError={historicoQuery.isError}
          error={historicoQuery.error}
          data={historicoQuery.data}
        >
          {(historico) => (
            <div className="row">
              <div className="col-md-6">
                <h3 className="text-up-01">Condições de saúde</h3>
                {historico.condicoes.length === 0 ? (
                  <p className="text-gray-60">Nenhuma condição registrada.</p>
                ) : (
                  <ul className="br-list">
                    {historico.condicoes.map((c) => (
                      <li key={`${c.codigo}-${c.dataRegistro}`} className="br-item">
                        <strong>{c.codigo}</strong> — {c.descricao}{' '}
                        <Tag variant={c.ativa ? 'warning' : 'default'}>
                          {c.ativa ? 'Ativa' : 'Inativa'}
                        </Tag>
                        <span className="d-block text-down-01 text-gray-60">
                          Registrada em {formatarData(c.dataRegistro)}
                        </span>
                      </li>
                    ))}
                  </ul>
                )}
              </div>
              <div className="col-md-6">
                <h3 className="text-up-01">Alergias</h3>
                {historico.alergias.length === 0 ? (
                  <p className="text-gray-60">Nenhuma alergia registrada.</p>
                ) : (
                  <ul className="br-list">
                    {historico.alergias.map((a) => (
                      <li key={`${a.substancia}-${a.dataRegistro}`} className="br-item">
                        <strong>{a.substancia}</strong> — gravidade {a.gravidade}
                        <span className="d-block text-down-01 text-gray-60">
                          Registrada em {formatarData(a.dataRegistro)}
                        </span>
                      </li>
                    ))}
                  </ul>
                )}
              </div>
            </div>
          )}
        </QueryState>
      </Card>

      <Card header={<strong>Atendimentos</strong>}>
        <DataTable
          caption="Linha do tempo de atendimentos do paciente"
          columns={colunasAtendimentos}
          rows={atendimentosQuery.data}
          rowKey={(a) => a.id}
          loading={atendimentosQuery.isLoading}
          error={atendimentosQuery.isError ? errorMessage(atendimentosQuery.error) : null}
          empty={
            <EmptyState
              icon="fas fa-notes-medical"
              title="Nenhum atendimento registrado"
              description="Este paciente ainda não possui atendimentos. Registre o primeiro encontro assistencial."
            />
          }
        />
      </Card>

      <AtendimentoFormModal
        open={atendimentoAberto}
        onClose={() => setAtendimentoAberto(false)}
        pacienteId={pacienteId}
      />
      <RegulacaoFormModal
        open={regulacaoAberta}
        onClose={() => setRegulacaoAberta(false)}
        pacienteId={pacienteId}
      />

      <PacienteEditModal
        open={acaoPaciente === 'editar'}
        onClose={() => setAcaoPaciente(null)}
        pacienteId={pacienteId}
      />
      <ConfirmarCadsusModal
        open={acaoPaciente === 'cadsus'}
        onClose={() => setAcaoPaciente(null)}
        pacienteId={pacienteId}
      />
      <RegistrarCondicaoModal
        open={acaoPaciente === 'condicao'}
        onClose={() => setAcaoPaciente(null)}
        pacienteId={pacienteId}
      />
      <RegistrarAlergiaModal
        open={acaoPaciente === 'alergia'}
        onClose={() => setAcaoPaciente(null)}
        pacienteId={pacienteId}
      />
      <InativarPacienteModal
        open={acaoPaciente === 'inativar'}
        onClose={() => setAcaoPaciente(null)}
        pacienteId={pacienteId}
      />
    </>
  );
}
