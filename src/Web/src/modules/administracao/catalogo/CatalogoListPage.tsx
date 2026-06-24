// Tela de LISTA/CADASTRO do Catalogo de materiais e servicos (CATMAT/CATSER) — modulo Administracao.
// Lista itens (filtro por natureza/termo), cadastra novo item e permite (in)ativar.
import { useState } from 'react';
import type { FormEvent } from 'react';
import {
  Button,
  Card,
  DataTable,
  EmptyState,
  errorMessage,
  FormField,
  FormRow,
  Input,
  Modal,
  PageHeader,
  Select,
  Tag,
  Toolbar,
  useToast,
} from '../../../components/ui';
import type { Column } from '../../../components/ui';
import { Can } from '../../../auth/Can';
import { AdministracaoSubNav } from '../AdministracaoSubNav';
import {
  NATUREZA_LABEL,
  useCadastrarItemCatalogo,
  useInativarItemCatalogo,
  useItensCatalogo,
  useReativarItemCatalogo,
} from './catalogo.api';
import type { ItemCatalogoResumo, NaturezaItem } from './catalogo.api';

export function CatalogoListPage() {
  const toast = useToast();

  const [naturezaFiltro, setNaturezaFiltro] = useState<NaturezaItem | ''>('');
  const [termo, setTermo] = useState('');
  const [termoConsulta, setTermoConsulta] = useState('');

  const itens = useItensCatalogo({
    natureza: naturezaFiltro === '' ? undefined : naturezaFiltro,
    termo: termoConsulta,
    apenasAtivos: false,
  });

  const inativar = useInativarItemCatalogo();
  const reativar = useReativarItemCatalogo();

  const [formAberto, setFormAberto] = useState(false);

  function consultar(event: FormEvent): void {
    event.preventDefault();
    setTermoConsulta(termo.trim());
  }

  async function alternarSituacao(item: ItemCatalogoResumo): Promise<void> {
    try {
      if (item.situacao === 'Ativo') {
        await inativar.mutateAsync(item.id);
        toast.success('Item inativado.');
      } else {
        await reativar.mutateAsync(item.id);
        toast.success('Item reativado.');
      }
    } catch (erro) {
      toast.error(errorMessage(erro));
    }
  }

  const columns: Column<ItemCatalogoResumo>[] = [
    { key: 'codigo', header: 'Código', sortAccessor: (i) => i.codigo, render: (i) => i.codigo },
    {
      key: 'natureza',
      header: 'Natureza',
      sortAccessor: (i) => i.natureza,
      render: (i) => NATUREZA_LABEL[i.natureza],
    },
    { key: 'descricao', header: 'Descrição', render: (i) => i.descricao },
    { key: 'unidade', header: 'Unidade', render: (i) => i.unidadeFornecimento },
    { key: 'classe', header: 'Classe', render: (i) => i.classe ?? '—' },
    {
      key: 'situacao',
      header: 'Situação',
      sortAccessor: (i) => i.situacao,
      render: (i) => <Tag variant={i.situacao === 'Ativo' ? 'success' : 'default'}>{i.situacao}</Tag>,
    },
    {
      key: 'acoes',
      header: 'Ações',
      sticky: true,
      render: (i) => (
        <Can permission="administracao.gerenciar">
          <Button variant="secondary" size="sm" onClick={() => void alternarSituacao(i)}>
            {i.situacao === 'Ativo' ? 'Inativar' : 'Reativar'}
          </Button>
        </Can>
      ),
    },
  ];

  return (
    <>
      <AdministracaoSubNav />
      <PageHeader
        eyebrow="Compras e Licitações"
        title="Catálogo de materiais e serviços"
        description="Itens padronizados (CATMAT/CATSER) — base estruturada das compras e contratações (Lei 14.133/2021)."
        actions={
          <Can permission="administracao.gerenciar">
            <Toolbar>
              <Button variant="primary" onClick={() => setFormAberto(true)}>
                <i className="fas fa-plus" aria-hidden="true" /> Cadastrar item
              </Button>
            </Toolbar>
          </Can>
        }
      />

      <Card header={<strong>Itens do catálogo</strong>}>
        <form className="br-form mb-3" onSubmit={consultar}>
          <FormRow
            acao={
              <Button variant="secondary" type="submit" loading={itens.isFetching}>
                Buscar
              </Button>
            }
          >
            <FormField label="Natureza">
              {({ id }) => (
                <Select
                  id={id}
                  value={naturezaFiltro}
                  onChange={(e) => setNaturezaFiltro(e.target.value as NaturezaItem | '')}
                  placeholder="Todas"
                  options={[
                    { value: 'Material', label: 'Material' },
                    { value: 'Servico', label: 'Serviço' },
                  ]}
                />
              )}
            </FormField>
            <FormField label="Buscar" help="Código ou descrição.">
              {({ id }) => (
                <Input id={id} value={termo} onChange={(e) => setTermo(e.target.value)} placeholder="Termo de busca" />
              )}
            </FormField>
          </FormRow>
        </form>

        <DataTable
          caption="Itens do catálogo de materiais e serviços"
          columns={columns}
          rows={itens.data}
          rowKey={(i) => i.id}
          loading={itens.isLoading}
          error={itens.isError ? errorMessage(itens.error) : null}
          empty={
            <EmptyState
              icon="fas fa-boxes-stacked"
              title="Nenhum item cadastrado"
              description="Cadastre itens padronizados para usar nas atas, no PCA e nas contratações."
            />
          }
        />
      </Card>

      <CatalogoFormModal open={formAberto} onClose={() => setFormAberto(false)} />
    </>
  );
}

interface CatalogoFormModalProps {
  open: boolean;
  onClose: () => void;
}

function CatalogoFormModal({ open, onClose }: CatalogoFormModalProps) {
  const toast = useToast();
  const cadastrar = useCadastrarItemCatalogo();

  const [codigo, setCodigo] = useState('');
  const [natureza, setNatureza] = useState<NaturezaItem>('Material');
  const [descricao, setDescricao] = useState('');
  const [unidade, setUnidade] = useState('');
  const [classe, setClasse] = useState('');

  function limpar(): void {
    setCodigo('');
    setNatureza('Material');
    setDescricao('');
    setUnidade('');
    setClasse('');
  }

  async function submeter(event: FormEvent): Promise<void> {
    event.preventDefault();
    try {
      await cadastrar.mutateAsync({
        codigo: codigo.trim(),
        natureza,
        descricao: descricao.trim(),
        unidadeFornecimento: unidade.trim(),
        classe: classe.trim() === '' ? null : classe.trim(),
      });
      toast.success('Item cadastrado.');
      limpar();
      onClose();
    } catch (erro) {
      toast.error(errorMessage(erro));
    }
  }

  const valido = codigo.trim() !== '' && descricao.trim() !== '' && unidade.trim() !== '';

  return (
    <Modal
      open={open}
      onClose={onClose}
      title="Cadastrar item de catálogo"
      footer={
        <Toolbar>
          <Button variant="secondary" onClick={onClose}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="catalogo-form" disabled={!valido} loading={cadastrar.isPending}>
            Cadastrar
          </Button>
        </Toolbar>
      }
    >
      <form id="catalogo-form" className="br-form" onSubmit={(e) => void submeter(e)}>
        <FormField label="Código" required>
          {({ id }) => <Input id={id} value={codigo} onChange={(e) => setCodigo(e.target.value)} maxLength={40} />}
        </FormField>
        <FormField label="Natureza" required>
          {({ id }) => (
            <Select
              id={id}
              value={natureza}
              onChange={(e) => setNatureza(e.target.value as NaturezaItem)}
              options={[
                { value: 'Material', label: 'Material' },
                { value: 'Servico', label: 'Serviço' },
              ]}
            />
          )}
        </FormField>
        <FormField label="Descrição" required>
          {({ id }) => (
            <Input id={id} value={descricao} onChange={(e) => setDescricao(e.target.value)} maxLength={500} />
          )}
        </FormField>
        <FormField label="Unidade de fornecimento" required help="Ex.: UN, KG, M, HORA, MES.">
          {({ id }) => <Input id={id} value={unidade} onChange={(e) => setUnidade(e.target.value)} maxLength={20} />}
        </FormField>
        <FormField label="Classe/classificação">
          {({ id }) => <Input id={id} value={classe} onChange={(e) => setClasse(e.target.value)} maxLength={120} />}
        </FormField>
      </form>
    </Modal>
  );
}
