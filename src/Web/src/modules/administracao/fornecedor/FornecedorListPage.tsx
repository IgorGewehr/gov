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
  Input,
  PageHeader,
  Tag,
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
      header: 'Razao social',
      sortAccessor: (f) => f.razaoSocial,
      render: (f) => f.razaoSocial,
    },
    { key: 'cnpj', header: 'CNPJ', render: (f) => f.cnpj },
    {
      key: 'situacao',
      header: 'Situacao',
      sortAccessor: (f) => f.situacao,
      render: (f) => <Tag variant={situacaoTagVariant(f.situacao)}>{SITUACAO_LABEL[f.situacao]}</Tag>,
    },
    {
      key: 'nivel',
      header: 'Nivel SICAF',
      sortAccessor: (f) => f.nivelCadastralSICAF,
      render: (f) => NIVEL_SICAF_LABEL[f.nivelCadastralSICAF],
    },
    {
      key: 'acoes',
      header: 'Acoes',
      render: (f) => (
        <Link className="br-button tertiary small" to={`/administracao/fornecedores/${f.id}`}>
          Detalhes
        </Link>
      ),
    },
  ];

  return (
    <>
      <PageHeader
        title="Fornecedores"
        description="Cadastro e sancao de fornecedores (Lei 14.133/2021 — registro cadastral/SICAF e sancoes)."
        actions={
          <Can permission="administracao.gerenciar">
            <Button variant="primary" onClick={() => setFormAberto(true)}>
              <i className="fas fa-plus" aria-hidden="true" /> Cadastrar fornecedor
            </Button>
          </Can>
        }
      />

      <Card className="mb-4" header={<strong>Consultar fornecedor por CNPJ</strong>}>
        <form className="br-form" onSubmit={consultarPorCnpj}>
          <div className="row align-items-end">
            <div className="col">
              <FormField label="CNPJ do fornecedor" help="Com ou sem mascara.">
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
            </div>
            <div className="col-auto mb-3">
              <Button
                variant="primary"
                type="submit"
                disabled={cnpjBusca.trim() === ''}
                loading={porCnpj.isFetching}
              >
                Consultar CNPJ
              </Button>
            </div>
          </div>
        </form>
      </Card>

      <Card header={<strong>Fornecedores impedidos (sancao impeditiva vigente)</strong>}>
        <form className="br-form mb-3" onSubmit={consultarImpedidos}>
          <div className="row align-items-end">
            <div className="col-sm-6">
              <FormField label="Data de referencia" required help="Apura a vigencia impeditiva na data informada.">
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
            </div>
            <div className="col-auto mb-3">
              <Button
                variant="secondary"
                type="submit"
                disabled={referencia.trim() === ''}
                loading={impedidos.isFetching}
              >
                Atualizar lista
              </Button>
            </div>
          </div>
        </form>

        <Alert variant="info" title="Referencia">
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
