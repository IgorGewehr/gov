// Tela de LISTA/CONSULTA de Fornecedores (modulo Administracao). Cobre:
//   - ListarFornecedoresImpedidosQuery -> tabela com filtro por data de referencia,
//     colunas + ordenacao (DataTable) e estados loading/vazio/erro;
//   - ObterFornecedorPorCnpjQuery -> consulta pontual por CNPJ que navega ao detalhe;
//   - CadastrarFornecedorCommand -> botao que abre o FornecedorFormModal.
import { useEffect, useState } from 'react';
import type { FormEvent } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import {
  Alert,
  Button,
  Card,
  DataTable,
  EmptyState,
  errorMessage,
  FormField,
  FormRow,
  Input,
  PageHeader,
  Tag,
  Toolbar,
  useToast,
} from '../../../components/ui';
import type { Column } from '../../../components/ui';
import { Can } from '../../../auth/Can';
import { formatarData } from '../../../i18n/format';
import { useFornecedoresImpedidos, useFornecedorPorCnpj } from './fornecedor.api';
import type { FornecedorResumo } from './fornecedor.api';
import { NIVEL_SICAF_LABEL, SITUACAO_LABEL } from './fornecedor.api';
import { situacaoTagVariant, hojeIso } from './fornecedor.helpers';
import { FornecedorFormModal } from './FornecedorFormModal';
import { AdministracaoSubNav } from '../AdministracaoSubNav';

export function FornecedorListPage() {
  const navigate = useNavigate();
  const toast = useToast();

  const [referencia, setReferencia] = useState(hojeIso());
  const [refConsulta, setRefConsulta] = useState(hojeIso());
  const impedidos = useFornecedoresImpedidos(refConsulta, refConsulta.length > 0);

  const [cnpjBusca, setCnpjBusca] = useState('');
  const [cnpjConsulta, setCnpjConsulta] = useState('');
  const porCnpj = useFornecedorPorCnpj(cnpjConsulta, cnpjConsulta.length > 0);

  const [formAberto, setFormAberto] = useState(false);

  function consultarImpedidos(event: FormEvent): void {
    event.preventDefault();
    setRefConsulta(referencia);
  }

  function consultarPorCnpj(event: FormEvent): void {
    event.preventDefault();
    setCnpjConsulta(cnpjBusca.trim());
  }

  // Navega ao detalhe quando a consulta por CNPJ retorna; trata erro/404.
  useEffect(() => {
    if (cnpjConsulta === '') return;
    if (porCnpj.isSuccess && porCnpj.data) {
      const destino = porCnpj.data.id;
      setCnpjConsulta('');
      navigate(`/administracao/fornecedores/${destino}`);
    } else if (porCnpj.isError) {
      setCnpjConsulta('');
      toast.error(errorMessage(porCnpj.error));
    }
  }, [cnpjConsulta, porCnpj.isSuccess, porCnpj.isError, porCnpj.data, porCnpj.error, navigate, toast]);

  const columns: Column<FornecedorResumo>[] = [
    {
      key: 'razaoSocial',
      header: 'Razão social',
      sortAccessor: (f) => f.razaoSocial,
      render: (f) => f.razaoSocial,
    },
    { key: 'cnpj', header: 'CNPJ', render: (f) => f.cnpj },
    {
      key: 'situacao',
      header: 'Situação',
      sortAccessor: (f) => f.situacao,
      render: (f) => <Tag variant={situacaoTagVariant(f.situacao)}>{SITUACAO_LABEL[f.situacao]}</Tag>,
    },
    {
      key: 'nivel',
      header: 'Nível SICAF',
      sortAccessor: (f) => f.nivelCadastralSICAF,
      render: (f) => NIVEL_SICAF_LABEL[f.nivelCadastralSICAF],
    },
    {
      key: 'acoes',
      header: 'Ações',
      sticky: true,
      render: (f) => (
        <Link className="br-button secondary small" to={`/administracao/fornecedores/${f.id}`}>
          Detalhes
        </Link>
      ),
    },
  ];

  return (
    <>
      <AdministracaoSubNav />
      <PageHeader
        eyebrow="Compras e Licitações"
        title="Fornecedores"
        description="Cadastro e sanção de fornecedores (Lei 14.133/2021 — registro cadastral/SICAF e sanções)."
        actions={
          <Can permission="administracao.gerenciar">
            <Toolbar>
              <Button variant="primary" onClick={() => setFormAberto(true)}>
                <i className="fas fa-plus" aria-hidden="true" /> Cadastrar fornecedor
              </Button>
            </Toolbar>
          </Can>
        }
      />

      <Card className="mb-4" header={<strong>Consultar fornecedor por CNPJ</strong>}>
        <form className="br-form" onSubmit={consultarPorCnpj}>
          <FormRow
            acao={
              <Button
                variant="primary"
                type="submit"
                disabled={cnpjBusca.trim() === ''}
                loading={porCnpj.isFetching}
              >
                Consultar CNPJ
              </Button>
            }
          >
            <FormField label="CNPJ do fornecedor" help="Com ou sem máscara.">
              {({ id, describedBy }) => (
                <Input
                  id={id}
                  aria-describedby={describedBy}
                  value={cnpjBusca}
                  onChange={(e) => setCnpjBusca(e.target.value)}
                  placeholder="00.000.000/0000-00"
                  inputMode="numeric"
                />
              )}
            </FormField>
          </FormRow>
        </form>
      </Card>

      <Card header={<strong>Fornecedores impedidos (sanção impeditiva vigente)</strong>}>
        <form className="br-form mb-3" onSubmit={consultarImpedidos}>
          <FormRow
            acao={
              <Button
                variant="secondary"
                type="submit"
                disabled={referencia.trim() === ''}
                loading={impedidos.isFetching}
              >
                Atualizar lista
              </Button>
            }
          >
            <FormField label="Data de referência" required help="Apura a vigência impeditiva na data informada.">
              {({ id, describedBy }) => (
                <Input
                  id={id}
                  type="date"
                  aria-describedby={describedBy}
                  value={referencia}
                  onChange={(e) => setReferencia(e.target.value)}
                />
              )}
            </FormField>
          </FormRow>
        </form>

        <Alert variant="info" title="Referência">
          Listando fornecedores impedidos em {formatarData(refConsulta)} (art. 156, III/IV).
        </Alert>

        <DataTable
          caption={`Fornecedores impedidos em ${formatarData(refConsulta)}`}
          columns={columns}
          rows={impedidos.data}
          rowKey={(f) => f.id}
          loading={impedidos.isLoading}
          error={impedidos.isError ? errorMessage(impedidos.error) : null}
          onRowClick={(f) => navigate(`/administracao/fornecedores/${f.id}`)}
          empty={
            <EmptyState
              icon="fas fa-circle-check"
              title="Nenhum fornecedor impedido"
              description="Nao ha fornecedores com sancao impeditiva vigente na data de referencia."
            />
          }
        />
      </Card>

      <FornecedorFormModal
        open={formAberto}
        onClose={() => setFormAberto(false)}
        onCadastrado={(id) => navigate(`/administracao/fornecedores/${id}`)}
      />
    </>
  );
}
