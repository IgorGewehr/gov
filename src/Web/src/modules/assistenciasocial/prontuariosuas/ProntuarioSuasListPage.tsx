// Tela de CONSULTA do Prontuário SUAS de uma família (ObterProntuarioDaFamiliaQuery).
// É uma leitura SIGILOSA e AUDITADA (I-7): exige família + usuário + motivo de acesso.
// Busca sob demanda (enabled); ao obter o conteúdo, oferece navegação ao detalhe.
// Também abre o formulário de ABERTURA de prontuário (AbrirProntuarioCommand).
import { useState } from 'react';
import type { FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Alert,
  Button,
  Card,
  DataTable,
  EmptyState,
  errorMessage,
  FormField,
  Input,
  PageHeader,
  Tag,
  Textarea,
} from '../../../components/ui';
import type { Column } from '../../../components/ui';
import { formatarData } from '../../../i18n/format';
import {
  situacaoTagVariant,
  useProntuarioDaFamilia,
} from './prontuariosuas.api';
import type { ConsultaProntuarioParams, RegistroResumo } from './prontuariosuas.api';
import { ProntuarioSuasFormModal } from './ProntuarioSuasFormModal';
import { AssistenciaSocialSubNav } from '../AssistenciaSocialSubNav';
import { Can } from '../../../auth/Can';

const PARAMS_VAZIO: ConsultaProntuarioParams = { familiaId: '', usuarioId: '', motivoAcesso: '' };

export function ProntuarioSuasListPage() {
  const navigate = useNavigate();

  const [familiaId, setFamiliaId] = useState('');
  const [usuarioId, setUsuarioId] = useState('');
  const [motivoAcesso, setMotivoAcesso] = useState('');
  const [consulta, setConsulta] = useState<ConsultaProntuarioParams>(PARAMS_VAZIO);
  const [formAberto, setFormAberto] = useState(false);

  const consultaAtiva =
    consulta.familiaId !== '' && consulta.usuarioId !== '' && consulta.motivoAcesso !== '';

  const query = useProntuarioDaFamilia(consulta, consultaAtiva);

  function consultar(event: FormEvent): void {
    event.preventDefault();
    setConsulta({
      familiaId: familiaId.trim(),
      usuarioId: usuarioId.trim(),
      motivoAcesso: motivoAcesso.trim(),
    });
  }

  const formValido =
    familiaId.trim() !== '' && usuarioId.trim() !== '' && motivoAcesso.trim() !== '';

  const columns: Column<RegistroResumo>[] = [
    {
      key: 'servico',
      header: 'Serviço',
      sortAccessor: (r) => r.servico,
      render: (r) => <Tag variant="info">{r.servico}</Tag>,
    },
    {
      key: 'data',
      header: 'Data do atendimento',
      sortAccessor: (r) => r.dataAtendimento,
      render: (r) => formatarData(r.dataAtendimento),
    },
  ];

  return (
    <>
      <AssistenciaSocialSubNav />
      <PageHeader
        title="Prontuário SUAS"
        description="Acompanhamento familiar sigiloso (PAIF em CRAS / PAEFI em CREAS). Toda leitura é auditada."
        actions={
          <Can permission="assistenciasocial.gerenciar">
            <Button variant="primary" onClick={() => setFormAberto(true)}>
              <i className="fas fa-folder-plus" aria-hidden="true" /> Abrir prontuário
            </Button>
          </Can>
        }
      />

      <div className="mb-4">
        <Alert variant="warning" title="Leitura sigilosa e auditada">
          O acesso ao conteúdo do prontuário exige justificativa (motivo de acesso) e é registrado
          de forma imutável na trilha (quem leu, quando, por quê) — LGPD art. 11.
        </Alert>
      </div>

      <Card className="mb-4">
        <form className="br-form" onSubmit={consultar} noValidate>
          <div className="row">
            <div className="col-md-6">
              <FormField label="Identificador da família" required>
                {({ id, describedBy, invalid }) => (
                  <Input
                    id={id}
                    aria-describedby={describedBy}
                    invalid={invalid}
                    value={familiaId}
                    onChange={(e) => setFamiliaId(e.target.value)}
                    placeholder="00000000-0000-0000-0000-000000000000"
                  />
                )}
              </FormField>
            </div>
            <div className="col-md-6">
              <FormField label="Usuário do acesso" required>
                {({ id, describedBy, invalid }) => (
                  <Input
                    id={id}
                    aria-describedby={describedBy}
                    invalid={invalid}
                    value={usuarioId}
                    onChange={(e) => setUsuarioId(e.target.value)}
                    placeholder="00000000-0000-0000-0000-000000000000"
                  />
                )}
              </FormField>
            </div>
          </div>
          <FormField
            label="Motivo do acesso"
            required
            help="Justificativa obrigatória da leitura sigilosa (máx. 400 caracteres)."
          >
            {({ id, describedBy, invalid }) => (
              <Textarea
                id={id}
                rows={2}
                maxLength={400}
                aria-describedby={describedBy}
                aria-invalid={invalid || undefined}
                value={motivoAcesso}
                onChange={(e) => setMotivoAcesso(e.target.value)}
              />
            )}
          </FormField>
          <div className="d-flex justify-content-end">
            <Button variant="primary" type="submit" disabled={!formValido} loading={query.isFetching}>
              <i className="fas fa-magnifying-glass" aria-hidden="true" /> Consultar prontuário
            </Button>
          </div>
        </form>
      </Card>

      {!consultaAtiva ? (
        <EmptyState
          icon="fas fa-user-shield"
          title="Consulta sigilosa"
          description="Informe a família, o usuário e o motivo de acesso para consultar o prontuário."
        />
      ) : (
        <>
          {query.data && (
            <Card
              className="mb-4"
              header={<strong>Prontuário da família {query.data.familiaId}</strong>}
              footer={
                <Button variant="secondary" onClick={() => navigate(`/assistenciasocial/prontuarios/${query.data!.id}`)}>
                  <i className="fas fa-id-card" aria-hidden="true" /> Abrir prontuário completo
                </Button>
              }
            >
              <dl className="row">
                <div className="col-sm-4 mb-3">
                  <dt className="text-gray-60 text-down-01">Situação</dt>
                  <dd className="mb-0">
                    <Tag variant={situacaoTagVariant(query.data.situacao)}>{query.data.situacao}</Tag>
                  </dd>
                </div>
                <div className="col-sm-4 mb-3">
                  <dt className="text-gray-60 text-down-01">Data de abertura</dt>
                  <dd className="mb-0 text-semi-bold">{formatarData(query.data.dataAbertura)}</dd>
                </div>
                <div className="col-sm-4 mb-3">
                  <dt className="text-gray-60 text-down-01">Violação envolvendo criança/adolescente</dt>
                  <dd className="mb-0">
                    {query.data.possuiViolacaoCriancaAdolescente ? (
                      <Tag variant="danger">Sim — dado sensível reforçado</Tag>
                    ) : (
                      <Tag variant="default">Não</Tag>
                    )}
                  </dd>
                </div>
              </dl>
            </Card>
          )}

          <DataTable
            caption={`Atendimentos registrados no prontuário da família ${consulta.familiaId}`}
            columns={columns}
            rows={query.data?.registros}
            rowKey={(r) => `${r.servico}-${r.dataAtendimento}`}
            loading={query.isLoading}
            error={query.isError ? errorMessage(query.error) : null}
            empty={
              <EmptyState
                icon="fas fa-clipboard"
                title="Nenhum atendimento registrado"
                description="Este prontuário ainda não possui atendimentos registrados."
              />
            }
          />
        </>
      )}

      <ProntuarioSuasFormModal
        open={formAberto}
        onClose={() => setFormAberto(false)}
        familiaIdInicial={consulta.familiaId}
        onAberto={(id) => navigate(`/assistenciasocial/prontuarios/${id}`)}
      />
    </>
  );
}
