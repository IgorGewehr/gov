// Tela de CONSULTA de Paciente por CNS. Padrão-ouro: busca sob demanda (enabled),
// estados loading/vazio/erro, link para o prontuário e abertura do formulário de
// cadastro (mutation). Dado sensível — LGPD: consulta minimizada e auditada.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Link } from 'react-router-dom';
import {
  Alert,
  Button,
  Card,
  EmptyState,
  FormField,
  Input,
  PageHeader,
  Spinner,
  Tag,
} from '../../components/ui';
import { errorMessage } from '../../components/ui';
import { Can } from '../../auth/Can';
import { formatarData } from '../../i18n/format';
import { usePacientePorCns } from './api';
import { PacienteFormModal } from './PacienteFormModal';

function Campo({ rotulo, children }: { rotulo: string; children: React.ReactNode }) {
  return (
    <div className="col-sm-6 col-lg-4 mb-3">
      <dt className="text-gray-60 text-down-01">{rotulo}</dt>
      <dd className="mb-0 text-semi-bold">{children}</dd>
    </div>
  );
}

export function PacienteListPage() {
  const [cns, setCns] = useState('');
  const [consultaAtiva, setConsultaAtiva] = useState('');
  const [formAberto, setFormAberto] = useState(false);

  const query = usePacientePorCns(consultaAtiva, consultaAtiva.length > 0);
  const paciente = query.data ?? null;

  function consultar(event: FormEvent): void {
    event.preventDefault();
    setConsultaAtiva(cns.trim());
  }

  return (
    <>
      <PageHeader
        title="Pacientes"
        description="Localize um paciente pelo Cartão Nacional de Saúde (CNS) para acessar o prontuário."
        actions={
          <Can permission="saude.gerenciar">
            <Button variant="primary" onClick={() => setFormAberto(true)}>
              <i className="fas fa-plus" aria-hidden="true" /> Cadastrar paciente
            </Button>
          </Can>
        }
      />

      <Card className="mb-4">
        <form className="br-form" onSubmit={consultar}>
          <div className="row align-items-end">
            <div className="col">
              <FormField label="Cartão Nacional de Saúde (CNS)" required>
                {({ id, describedBy, invalid }) => (
                  <Input
                    id={id}
                    aria-describedby={describedBy}
                    invalid={invalid}
                    inputMode="numeric"
                    maxLength={15}
                    value={cns}
                    onChange={(e) => setCns(e.target.value.replace(/\D/g, ''))}
                    placeholder="000000000000000"
                  />
                )}
              </FormField>
            </div>
            <div className="col-auto mb-3">
              <Button
                variant="primary"
                type="submit"
                disabled={cns.trim() === ''}
                loading={query.isFetching}
              >
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
          description="Informe o CNS do paciente e clique em Consultar."
        />
      ) : query.isLoading ? (
        <div className="app-center">
          <Spinner label="Consultando paciente…" />
        </div>
      ) : query.isError ? (
        <Alert variant="danger">{errorMessage(query.error)}</Alert>
      ) : paciente === null ? (
        <EmptyState
          icon="fas fa-user-slash"
          title="Paciente não encontrado"
          description="Nenhum paciente cadastrado para este CNS. Verifique o número ou cadastre um novo paciente."
        />
      ) : (
        <Card
          header={
            <div className="d-flex justify-content-between align-items-center">
              <strong>{paciente.nomeSocial || paciente.nome}</strong>
              <Link className="br-button secondary small" to={`/saude/pacientes/${paciente.id}`}>
                <i className="fas fa-folder-open" aria-hidden="true" /> Abrir prontuário
              </Link>
            </div>
          }
        >
          <dl className="row">
            <Campo rotulo="Nome civil">{paciente.nome}</Campo>
            <Campo rotulo="Nome social">{paciente.nomeSocial ?? '—'}</Campo>
            <Campo rotulo="CNS">{paciente.cns}</Campo>
            <Campo rotulo="Data de nascimento">{formatarData(paciente.dataNascimento)}</Campo>
            <Campo rotulo="Sexo">{paciente.sexo}</Campo>
            <Campo rotulo="Situação">
              <Tag variant={paciente.situacao === 'Ativo' ? 'success' : 'danger'}>
                {paciente.situacao}
              </Tag>
            </Campo>
            <Campo rotulo="CADSUS">
              <Tag variant={paciente.cnsConfirmado ? 'success' : 'warning'}>
                {paciente.cnsConfirmado ? 'CNS confirmado' : 'Não confirmado'}
              </Tag>
            </Campo>
          </dl>
        </Card>
      )}

      <PacienteFormModal open={formAberto} onClose={() => setFormAberto(false)} />
    </>
  );
}
