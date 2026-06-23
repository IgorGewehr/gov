// [Command AbrirPedido] Abre uma requisição de almoxarifado multi-item (nasce Solicitada).
// Identificação do pedido (UO, setor, solicitante, data, justificativa) + linhas montadas
// pelo ItemEstoquePicker (reuso de useItensLista). Cada linha referencia um item de estoque
// ativo × quantidade (> 0). Validação espelha o backend (UnidadeId/SolicitanteId, setor
// obrigatório, ao menos uma linha, quantidades positivas).
import { useState } from 'react';
import type { FormEvent } from 'react';
import {
  Button,
  Card,
  DataTable,
  EmptyState,
  FormField,
  Input,
  Modal,
  Textarea,
  useToast,
} from '../../../components/ui';
import type { Column } from '../../../components/ui';
import { ApiError } from '../../../api/problemDetails';
import type { ItemEstoqueItemLista } from '../itemestoque/itemestoque.api';
import { ItemEstoquePicker } from './ItemEstoquePicker';
import { useAbrirPedido } from './requisicao.api';
import type { AbrirPedidoInput, ItemPedidoEntrada } from './requisicao.api';
import { hojeIso, mapearFieldErrors } from './requisicao.helpers';

export interface RequisicaoAbrirModalProps {
  open: boolean;
  onClose: () => void;
  /** Chamado com o Id do pedido criado (navega ao detalhe). */
  onAberto: (id: string) => void;
}

const CAMPOS = ['unidadeId', 'setorSolicitante', 'solicitanteId', 'data', 'itens'] as const;
type Campo = (typeof CAMPOS)[number];
type FormErrors = Partial<Record<Campo, string>>;

/** Linha em construção: item escolhido + descrição (para exibição) + quantidade (texto do input). */
interface LinhaSelecionada {
  itemEstoqueId: string;
  codigo: string;
  descricao: string;
  quantidade: string;
}

export function RequisicaoAbrirModal({ open, onClose, onAberto }: RequisicaoAbrirModalProps) {
  const toast = useToast();
  const mutation = useAbrirPedido();

  const [unidadeId, setUnidadeId] = useState('');
  const [setorSolicitante, setSetorSolicitante] = useState('');
  const [solicitanteId, setSolicitanteId] = useState('');
  const [data, setData] = useState(hojeIso());
  const [justificativa, setJustificativa] = useState('');
  const [linhas, setLinhas] = useState<LinhaSelecionada[]>([]);
  const [errors, setErrors] = useState<FormErrors>({});

  function adicionarItem(item: ItemEstoqueItemLista): void {
    setLinhas((atuais) =>
      atuais.some((l) => l.itemEstoqueId === item.id)
        ? atuais
        : [
            ...atuais,
            { itemEstoqueId: item.id, codigo: item.codigo, descricao: item.descricao, quantidade: '' },
          ],
    );
  }

  function removerLinha(itemEstoqueId: string): void {
    setLinhas((atuais) => atuais.filter((l) => l.itemEstoqueId !== itemEstoqueId));
  }

  function alterarQuantidade(itemEstoqueId: string, valor: string): void {
    setLinhas((atuais) =>
      atuais.map((l) => (l.itemEstoqueId === itemEstoqueId ? { ...l, quantidade: valor } : l)),
    );
  }

  function resetar(): void {
    setUnidadeId('');
    setSetorSolicitante('');
    setSolicitanteId('');
    setData(hojeIso());
    setJustificativa('');
    setLinhas([]);
    setErrors({});
  }

  function fechar(): void {
    resetar();
    onClose();
  }

  function validar(): FormErrors {
    const next: FormErrors = {};
    if (unidadeId.trim() === '') next.unidadeId = 'Informe a UO consumidora.';
    if (setorSolicitante.trim() === '') next.setorSolicitante = 'Informe o setor solicitante.';
    if (solicitanteId.trim() === '') next.solicitanteId = 'Informe o servidor solicitante.';
    if (data.trim() === '') next.data = 'Informe a data do pedido.';
    if (linhas.length === 0) {
      next.itens = 'Adicione ao menos um item ao pedido.';
    } else if (linhas.some((l) => !(Number(l.quantidade) > 0))) {
      next.itens = 'Todas as quantidades devem ser maiores que zero.';
    }
    return next;
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const validacao = validar();
    setErrors(validacao);
    if (Object.keys(validacao).length > 0) return;

    const itens: ItemPedidoEntrada[] = linhas.map((l) => ({
      itemEstoqueId: l.itemEstoqueId,
      quantidade: Number(l.quantidade),
    }));

    const input: AbrirPedidoInput = {
      unidadeId: unidadeId.trim(),
      setorSolicitante: setorSolicitante.trim(),
      solicitanteId: solicitanteId.trim(),
      data,
      justificativa: justificativa.trim() || null,
      itens,
    };

    mutation.mutate(input, {
      onSuccess: (resposta) => {
        toast.success('Requisição aberta.', 'Sucesso');
        const novoId = resposta.id;
        resetar();
        onClose();
        onAberto(novoId);
      },
      onError: (error) => {
        if (error instanceof ApiError && Object.keys(error.fieldErrors).length > 0) {
          setErrors(mapearFieldErrors(error.fieldErrors, CAMPOS));
        }
        toast.error(
          error instanceof ApiError ? error.userMessage : 'Não foi possível abrir a requisição.',
        );
      },
    });
  }

  const colunasLinhas: Column<LinhaSelecionada>[] = [
    { key: 'codigo', header: 'Código', render: (l) => l.codigo },
    { key: 'descricao', header: 'Descrição', render: (l) => l.descricao },
    {
      key: 'quantidade',
      header: 'Quantidade',
      align: 'end',
      render: (l) => (
        <Input
          type="number"
          min="0"
          step="0.01"
          inputMode="decimal"
          aria-label={`Quantidade de ${l.codigo}`}
          value={l.quantidade}
          onChange={(e) => alterarQuantidade(l.itemEstoqueId, e.target.value)}
        />
      ),
    },
    {
      key: 'acoes',
      header: 'Ações',
      sticky: true,
      render: (l) => (
        <Button
          variant="tertiary"
          size="sm"
          onClick={() => removerLinha(l.itemEstoqueId)}
          aria-label={`Remover ${l.codigo}`}
        >
          <i className="fas fa-trash" aria-hidden="true" /> Remover
        </Button>
      ),
    },
  ];

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Abrir requisição de almoxarifado"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-abrir-requisicao" loading={mutation.isPending}>
            Abrir requisição
          </Button>
        </>
      }
    >
      <form id="form-abrir-requisicao" className="br-form" onSubmit={submeter} noValidate>
        <div className="row">
          <div className="col-sm-6">
            <FormField label="UO consumidora (identificador)" required error={errors.unidadeId}>
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  aria-describedby={describedBy}
                  invalid={invalid}
                  value={unidadeId}
                  onChange={(e) => setUnidadeId(e.target.value)}
                  placeholder="00000000-0000-0000-0000-000000000000"
                />
              )}
            </FormField>
          </div>
          <div className="col-sm-6">
            <FormField label="Servidor solicitante (identificador)" required error={errors.solicitanteId}>
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  aria-describedby={describedBy}
                  invalid={invalid}
                  value={solicitanteId}
                  onChange={(e) => setSolicitanteId(e.target.value)}
                  placeholder="00000000-0000-0000-0000-000000000000"
                />
              )}
            </FormField>
          </div>
        </div>

        <div className="row">
          <div className="col-sm-8">
            <FormField label="Setor solicitante" required error={errors.setorSolicitante}>
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  aria-describedby={describedBy}
                  invalid={invalid}
                  value={setorSolicitante}
                  onChange={(e) => setSetorSolicitante(e.target.value)}
                  placeholder="Ex.: Secretaria de Obras"
                />
              )}
            </FormField>
          </div>
          <div className="col-sm-4">
            <FormField label="Data do pedido" required error={errors.data}>
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  type="date"
                  aria-describedby={describedBy}
                  invalid={invalid}
                  value={data}
                  onChange={(e) => setData(e.target.value)}
                />
              )}
            </FormField>
          </div>
        </div>

        <FormField label="Justificativa" help="Opcional.">
          {({ id, describedBy }) => (
            <Textarea
              id={id}
              aria-describedby={describedBy}
              rows={2}
              value={justificativa}
              onChange={(e) => setJustificativa(e.target.value)}
            />
          )}
        </FormField>

        <Card className="mb-3" header={<strong>Itens da requisição</strong>}>
          {errors.itens && (
            <p className="text-danger text-down-01 mb-2" role="alert">
              {errors.itens}
            </p>
          )}
          <DataTable
            caption="Itens selecionados para a requisição"
            columns={colunasLinhas}
            rows={linhas}
            rowKey={(l) => l.itemEstoqueId}
            empty={
              <EmptyState
                icon="fas fa-boxes-stacked"
                title="Nenhum item adicionado"
                description="Busque e adicione itens do almoxarifado abaixo."
              />
            }
          />
        </Card>

        <ItemEstoquePicker selecionados={linhas.map((l) => l.itemEstoqueId)} onAdicionar={adicionarItem} />
      </form>
    </Modal>
  );
}
