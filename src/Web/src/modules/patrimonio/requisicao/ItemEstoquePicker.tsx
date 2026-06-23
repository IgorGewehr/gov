// Picker de ItemEstoque para a abertura de requisições: busca por código/descrição
// (reuso de useItensLista, somente itens ATIVOS) e adiciona o item escolhido ao pedido.
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
} from '../../../components/ui';
import type { Column } from '../../../components/ui';
import { useItensLista } from '../itemestoque/itemestoque.api';
import type { ItemEstoqueItemLista } from '../itemestoque/itemestoque.api';

export interface ItemEstoquePickerProps {
  /** Ids já adicionados ao pedido (desabilita o botão "Adicionar"). */
  selecionados: ReadonlyArray<string>;
  /** Callback ao adicionar um item ao pedido. */
  onAdicionar: (item: ItemEstoqueItemLista) => void;
}

export function ItemEstoquePicker({ selecionados, onAdicionar }: ItemEstoquePickerProps) {
  const [termoInput, setTermoInput] = useState('');
  const [termo, setTermo] = useState('');
  const picker = useItensLista({
    termo: termo || undefined,
    situacao: 'Ativo',
    pagina: 1,
    tamanho: 10,
  });

  function buscar(event: FormEvent): void {
    event.preventDefault();
    setTermo(termoInput.trim());
  }

  const colunas: Column<ItemEstoqueItemLista>[] = [
    { key: 'codigo', header: 'Código', render: (i) => i.codigo },
    { key: 'descricao', header: 'Descrição', render: (i) => i.descricao },
    { key: 'saldo', header: 'Saldo', align: 'end', render: (i) => `${i.saldo} ${i.unidadeMedida}` },
    {
      key: 'acoes',
      header: 'Ações',
      sticky: true,
      render: (i) => (
        <Button
          variant="secondary"
          size="sm"
          onClick={() => onAdicionar(i)}
          disabled={selecionados.includes(i.id)}
        >
          <i className="fas fa-plus" aria-hidden="true" /> Adicionar
        </Button>
      ),
    },
  ];

  return (
    <Card header={<strong>Buscar item de estoque</strong>}>
      <form className="br-form" onSubmit={buscar}>
        <FormRow
          acao={
            <Button variant="secondary" type="submit" loading={picker.isFetching}>
              <i className="fas fa-magnifying-glass" aria-hidden="true" /> Buscar
            </Button>
          }
        >
          <FormField label="Código ou descrição">
            {({ id, describedBy }) => (
              <Input
                id={id}
                aria-describedby={describedBy}
                value={termoInput}
                onChange={(e) => setTermoInput(e.target.value)}
                placeholder="Trecho do código ou da descrição"
              />
            )}
          </FormField>
        </FormRow>
      </form>

      <DataTable
        caption="Itens ativos do almoxarifado disponíveis para requisição"
        columns={colunas}
        rows={picker.data?.itens}
        rowKey={(i) => i.id}
        loading={picker.isLoading}
        empty={
          <EmptyState
            icon="fas fa-magnifying-glass"
            title="Nenhum item ativo encontrado"
            description="Ajuste o termo de busca para localizar itens do almoxarifado."
          />
        }
      />
    </Card>
  );
}
