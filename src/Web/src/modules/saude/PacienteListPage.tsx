// Tela de LISTA + BUSCA de pacientes/munícipes (Onda 0 — Navegabilidade).
// Antes só era possível "achar" um paciente digitando o CNS de 15 dígitos; agora a
// entrada do módulo Saúde permite LISTAR, BUSCAR (por nome, CPF ou CNS) e navegar ao
// prontuário. Espelha o endpoint REAL GET /saude/pacientes?termo&situacao&pagina&tamanho
// (ResultadoPaginado<PacienteItemLista>), gated em "saude.prontuario.ler".
//
// LGPD: a consulta é SENSÍVEL — o backend grava trilha de acesso (quem leu o quê,
// quando) e MINIMIZA os dados (a lista NÃO traz CPF). Padrão-ouro: form de busca em
// Card, DataTable com estados loading/vazio/erro, paginação 1-based (keepPreviousData).
import { useMemo, useState } from 'react';
import type { FormEvent } from 'react';
import { Link } from 'react-router-dom';
import {
  Button,
  Card,
  DataTable,
  EmptyState,
  FormField,
  FormRow,
  Input,
  PageHeader,
  Select,
  Tag,
  Toolbar,
} from '../../components/ui';
import type { Column } from '../../components/ui';
import { errorMessage } from '../../components/ui';
import { Can } from '../../auth/Can';
import { formatarData } from '../../i18n/format';
import { useBuscarPacientes } from './api';
import type { PacienteBuscaFiltro, PacienteItemLista } from './api';
import { opcoesSituacaoPaciente, situacaoPacienteVariant } from './saude.helpers';
import { PacienteFormModal } from './PacienteFormModal';
import { SaudeSubNav } from './SaudeSubNav';

const TAMANHO_PAGINA = 20;

export function PacienteListPage() {
  // Campos do formulário (edição) × filtros aplicados (disparam a query).
  const [termoCampo, setTermoCampo] = useState('');
  const [situacaoCampo, setSituacaoCampo] = useState('');
  const [termo, setTermo] = useState('');
  const [situacao, setSituacao] = useState('');
  const [pagina, setPagina] = useState(1);

  const [formAberto, setFormAberto] = useState(false);

  const filtro = useMemo<PacienteBuscaFiltro>(
    () => ({
      termo: termo || undefined,
      situacao: situacao || undefined,
      pagina,
      tamanho: TAMANHO_PAGINA,
    }),
    [termo, situacao, pagina],
  );

  const query = useBuscarPacientes(filtro);

  function aplicarBusca(event: FormEvent): void {
    event.preventDefault();
    setPagina(1);
    setTermo(termoCampo.trim());
    setSituacao(situacaoCampo);
  }

  const total = query.data?.total ?? 0;
  const totalPaginas = total > 0 ? Math.ceil(total / TAMANHO_PAGINA) : 0;

  const columns: Column<PacienteItemLista>[] = [
    {
      key: 'nome',
      header: 'Nome',
      sortAccessor: (p) => (p.nomeSocial || p.nome).toLowerCase(),
      render: (p) => (
        <span>
          {p.nomeSocial || p.nome}
          {p.nomeSocial && (
            <span className="d-block text-down-01 text-secondary">Nome civil: {p.nome}</span>
          )}
        </span>
      ),
    },
    {
      key: 'cns',
      header: 'CNS',
      sortAccessor: (p) => p.cns,
      render: (p) => p.cns,
    },
    {
      key: 'nascimento',
      header: 'Nascimento',
      sortAccessor: (p) => p.dataNascimento,
      render: (p) => formatarData(p.dataNascimento),
    },
    { key: 'sexo', header: 'Sexo', sortAccessor: (p) => p.sexo, render: (p) => p.sexo },
    {
      key: 'cadsus',
      header: 'CADSUS',
      render: (p) => (
        <Tag variant={p.cnsConfirmado ? 'success' : 'warning'}>
          {p.cnsConfirmado ? 'Confirmado' : 'Não confirmado'}
        </Tag>
      ),
    },
    {
      key: 'situacao',
      header: 'Situação',
      sortAccessor: (p) => p.situacao,
      render: (p) => <Tag variant={situacaoPacienteVariant(p.situacao)}>{p.situacao}</Tag>,
    },
    {
      key: 'acoes',
      header: 'Ações',
      sticky: true,
      render: (p) => (
        <Link className="br-button secondary small" to={`/saude/pacientes/${p.id}`}>
          <i className="fas fa-folder-open" aria-hidden="true" /> Prontuário
        </Link>
      ),
    },
  ];

  return (
    <>
      <SaudeSubNav />
      <PageHeader
        eyebrow="Saúde"
        title="Pacientes"
        description="Localize munícipes por nome, CPF ou CNS para acessar o prontuário. Acesso registrado em trilha de auditoria (LGPD)."
        actions={
          <Can permission="saude.gerenciar">
            <Toolbar>
              <Button variant="primary" onClick={() => setFormAberto(true)}>
                <i className="fas fa-plus" aria-hidden="true" /> Cadastrar paciente
              </Button>
            </Toolbar>
          </Can>
        }
      />

      <Card className="mb-4">
        <form className="br-form" onSubmit={aplicarBusca}>
          <FormRow
            acao={
              <Button variant="primary" type="submit" loading={query.isFetching}>
                Buscar
              </Button>
            }
          >
            <div className="row">
              <div className="col-12 col-md-8">
                <FormField
                  label="Buscar paciente"
                  help="Nome (trecho), CPF (dígitos) ou CNS completo (15 dígitos)."
                >
                  {({ id, describedBy }) => (
                    <Input
                      id={id}
                      aria-describedby={describedBy}
                      value={termoCampo}
                      onChange={(e) => setTermoCampo(e.target.value)}
                      placeholder="Ex.: Maria da Silva, 000.000.000-00 ou CNS"
                    />
                  )}
                </FormField>
              </div>
              <div className="col-12 col-md-4">
                <FormField label="Situação">
                  {({ id }) => (
                    <Select
                      id={id}
                      options={opcoesSituacaoPaciente}
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
        caption="Pacientes/munícipes"
        columns={columns}
        rows={query.data?.itens}
        rowKey={(p) => p.id}
        loading={query.isLoading}
        error={query.isError ? errorMessage(query.error) : null}
        empty={
          <EmptyState
            icon="fas fa-user-slash"
            title="Nenhum paciente encontrado"
            description="Ajuste os termos da busca ou cadastre um novo paciente."
          />
        }
      />

      {totalPaginas > 1 && (
        <nav
          className="d-flex align-items-center justify-content-between mt-3"
          aria-label="Paginação de pacientes"
        >
          <span className="text-down-01 text-secondary">
            Página {pagina} de {totalPaginas} · {total} paciente(s)
          </span>
          <div className="d-flex">
            <Button
              variant="secondary"
              className="small"
              disabled={pagina <= 1 || query.isFetching}
              onClick={() => setPagina((p) => Math.max(1, p - 1))}
            >
              <i className="fas fa-chevron-left" aria-hidden="true" /> Anterior
            </Button>
            <Button
              variant="secondary"
              className="small ml-2"
              disabled={pagina >= totalPaginas || query.isFetching}
              onClick={() => setPagina((p) => Math.min(totalPaginas, p + 1))}
            >
              Próxima <i className="fas fa-chevron-right" aria-hidden="true" />
            </Button>
          </div>
        </nav>
      )}

      <PacienteFormModal open={formAberto} onClose={() => setFormAberto(false)} />
    </>
  );
}
