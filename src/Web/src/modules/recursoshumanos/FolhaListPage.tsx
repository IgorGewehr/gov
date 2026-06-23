// Tela de CONSULTA da folha de pagamento por competência (página de entrada do agregado
// Folha). Padrão-ouro: consulta sob demanda (enabled), QueryState com estados, abertura
// do formulário de abertura de folha (mutation). Quando há folha, resume e linka o detalhe.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Link } from 'react-router-dom';
import {
  Button,
  Card,
  EmptyState,
  FormField,
  FormRow,
  Input,
  PageHeader,
  QueryState,
  Select,
  Tag,
  Toolbar,
} from '../../components/ui';
import { Can } from '../../auth/Can';
import { formatarMoeda } from '../../i18n/format';
import { useFolhaPorCompetencia } from './api';
import type { FolhaResumo } from './api';
import { MESES, PERM_RH_GERENCIAR, situacaoFolhaTagVariant } from './recursosHumanos.helpers';
import { AbrirFolhaFormModal } from './AbrirFolhaFormModal';
import { competenciaParaState } from './FolhaDetailPage';
import { RhSubNav } from './RhSubNav';

const ANO_ATUAL = new Date().getFullYear();

export function FolhaListPage() {
  const [ano, setAno] = useState(String(ANO_ATUAL));
  const [mes, setMes] = useState(String(new Date().getMonth() + 1));
  const [consulta, setConsulta] = useState<{ ano: number; mes: number } | null>(null);
  const [formAberto, setFormAberto] = useState(false);

  const query = useFolhaPorCompetencia(
    consulta?.ano ?? 0,
    consulta?.mes ?? 0,
    consulta !== null,
  );

  function consultar(event: FormEvent): void {
    event.preventDefault();
    const anoNum = Number(ano);
    const mesNum = Number(mes);
    if (!Number.isInteger(anoNum) || !Number.isInteger(mesNum)) return;
    setConsulta({ ano: anoNum, mes: mesNum });
  }

  return (
    <>
      <RhSubNav />
      <PageHeader
        eyebrow="Recursos Humanos"
        title="Folha de Pagamento"
        description="Consulte a folha de uma competência (uma folha por competência)."
        actions={
          <Can permission={PERM_RH_GERENCIAR}>
            <Toolbar>
              <Button variant="primary" onClick={() => setFormAberto(true)}>
                <i className="fas fa-plus" aria-hidden="true" /> Abrir folha
              </Button>
            </Toolbar>
          </Can>
        }
      />

      <Card className="mb-4">
        <form className="br-form" onSubmit={consultar}>
          <FormRow
            acao={
              <Button variant="primary" type="submit" loading={query.isFetching}>
                Consultar
              </Button>
            }
          >
            <div className="row">
              <div className="col-sm-6">
                <FormField label="Mês" required>
                  {({ id, describedBy }) => (
                    <Select
                      id={id}
                      aria-describedby={describedBy}
                      value={mes}
                      onChange={(e) => setMes(e.target.value)}
                      options={MESES}
                    />
                  )}
                </FormField>
              </div>
              <div className="col-sm-6">
                <FormField label="Ano" required>
                  {({ id, describedBy }) => (
                    <Input
                      id={id}
                      type="number"
                      min="2000"
                      max="2100"
                      step="1"
                      inputMode="numeric"
                      aria-describedby={describedBy}
                      value={ano}
                      onChange={(e) => setAno(e.target.value)}
                    />
                  )}
                </FormField>
              </div>
            </div>
          </FormRow>
        </form>
      </Card>

      {consulta === null ? (
        <EmptyState
          icon="fas fa-magnifying-glass"
          title="Selecione uma competência"
          description="Escolha mês e ano e clique em Consultar."
        />
      ) : (
        <QueryState<FolhaResumo>
          isLoading={query.isLoading}
          isError={query.isError}
          error={query.error}
          data={query.data ?? undefined}
          empty={
            <EmptyState
              icon="fas fa-file-circle-question"
              title="Nenhuma folha para a competência"
              description="Não há folha aberta para essa competência. Use Abrir folha."
            />
          }
        >
          {(folha) => (
            <Card
              header={<strong>Competência {folha.competencia}</strong>}
              footer={
                <Link
                  className="br-button primary"
                  to={`/recursoshumanos/folhas/${folha.id}`}
                  state={competenciaParaState(folha)}
                >
                  Abrir detalhe da folha
                </Link>
              }
            >
              <dl className="row">
                <div className="col-sm-6 mb-3">
                  <dt className="text-gray-60 text-down-01">Situação</dt>
                  <dd className="mb-0">
                    <Tag variant={situacaoFolhaTagVariant(folha.situacao)}>{folha.situacao}</Tag>
                  </dd>
                </div>
                <div className="col-sm-6 mb-3">
                  <dt className="text-gray-60 text-down-01">Total líquido</dt>
                  <dd className="mb-0 text-semi-bold">{formatarMoeda(folha.totalLiquido)}</dd>
                </div>
                <div className="col-sm-6 mb-3">
                  <dt className="text-gray-60 text-down-01">Total de proventos</dt>
                  <dd className="mb-0 text-semi-bold">{formatarMoeda(folha.totalProventos)}</dd>
                </div>
                <div className="col-sm-6 mb-3">
                  <dt className="text-gray-60 text-down-01">Total de descontos</dt>
                  <dd className="mb-0 text-semi-bold">{formatarMoeda(folha.totalDescontos)}</dd>
                </div>
              </dl>
            </Card>
          )}
        </QueryState>
      )}

      <AbrirFolhaFormModal
        open={formAberto}
        onClose={() => setFormAberto(false)}
        anoInicial={Number(ano)}
        mesInicial={Number(mes)}
        onAberta={(c) => setConsulta(c)}
      />
    </>
  );
}
