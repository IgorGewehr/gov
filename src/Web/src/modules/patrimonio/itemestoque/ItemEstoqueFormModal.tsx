// [Command CadastrarItemEstoque §5.1] Formulário de cadastro de item de almoxarifado
// em Modal (foco preso). Validação por campo (I-10: código, descrição, unidade e método
// obrigatórios; PontoPedido >= 0), mapeamento de ApiError.fieldErrors e Toast.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Button, FormField, Input, Modal, Select, useToast } from '../../../components/ui';
import { ApiError } from '../../../api/problemDetails';
import { useCadastrarItemEstoque } from './itemestoque.api';
import type { CadastrarItemEstoqueInput } from './itemestoque.api';
import { METODO_CUSTEIO, CLASSIFICACAO_ABC } from './itemestoque.api';
import { OPCOES_CLASSIFICACAO_ABC, OPCOES_METODO_CUSTEIO, mapearFieldErrors } from './itemEstoque.helpers';

export interface ItemEstoqueFormModalProps {
  open: boolean;
  onClose: () => void;
  /** Callback opcional após cadastro (ex.: navegar ao detalhe). */
  onCadastrado?: (id: string) => void;
}

const CAMPOS = ['codigo', 'descricao', 'unidadeMedida', 'metodoCusteio', 'pontoPedido', 'classificacaoAbc'] as const;
type Campo = (typeof CAMPOS)[number];
type FormErrors = Partial<Record<Campo, string>>;

export function ItemEstoqueFormModal({ open, onClose, onCadastrado }: ItemEstoqueFormModalProps) {
  const toast = useToast();
  const mutation = useCadastrarItemEstoque();

  const [codigo, setCodigo] = useState('');
  const [descricao, setDescricao] = useState('');
  const [unidadeMedida, setUnidadeMedida] = useState('');
  const [metodoCusteio, setMetodoCusteio] = useState(String(METODO_CUSTEIO.Peps));
  const [pontoPedido, setPontoPedido] = useState('');
  const [classificacaoAbc, setClassificacaoAbc] = useState(String(CLASSIFICACAO_ABC.C));
  const [errors, setErrors] = useState<FormErrors>({});

  function validar(): FormErrors {
    const next: FormErrors = {};
    if (codigo.trim() === '') next.codigo = 'Informe o código do item.';
    else if (codigo.trim().length > 40) next.codigo = 'Código deve ter no máximo 40 caracteres.';
    if (descricao.trim() === '') next.descricao = 'Informe a descrição do item.';
    else if (descricao.trim().length > 200) next.descricao = 'Descrição deve ter no máximo 200 caracteres.';
    if (unidadeMedida.trim() === '') next.unidadeMedida = 'Informe a unidade de medida.';
    else if (unidadeMedida.trim().length > 10) next.unidadeMedida = 'Unidade deve ter no máximo 10 caracteres.';
    const ponto = Number(pontoPedido);
    if (pontoPedido.trim() === '' || Number.isNaN(ponto) || ponto < 0)
      next.pontoPedido = 'Ponto de pedido não pode ser negativo.';
    return next;
  }

  function fechar(): void {
    setErrors({});
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const validacao = validar();
    setErrors(validacao);
    if (Object.keys(validacao).length > 0) return;

    const input: CadastrarItemEstoqueInput = {
      codigo: codigo.trim(),
      descricao: descricao.trim(),
      unidadeMedida: unidadeMedida.trim(),
      metodoCusteio: Number(metodoCusteio),
      pontoPedido: Number(pontoPedido),
      classificacaoAbc: Number(classificacaoAbc),
    };

    mutation.mutate(input, {
      onSuccess: (resposta) => {
        toast.success(`Item ${input.codigo} cadastrado no almoxarifado.`, 'Sucesso');
        onCadastrado?.(resposta.id);
        fechar();
      },
      onError: (error) => {
        if (error instanceof ApiError && Object.keys(error.fieldErrors).length > 0) {
          setErrors(mapearFieldErrors(error.fieldErrors, CAMPOS));
        }
        toast.error(
          error instanceof ApiError ? error.userMessage : 'Não foi possível cadastrar o item.',
        );
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Cadastrar item de almoxarifado"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-cadastrar-item" loading={mutation.isPending}>
            Cadastrar
          </Button>
        </>
      }
    >
      <form id="form-cadastrar-item" className="br-form" onSubmit={submeter} noValidate>
        <FormField label="Código" required error={errors.codigo} help="Código no catálogo de almoxarifado.">
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              maxLength={40}
              value={codigo}
              onChange={(e) => setCodigo(e.target.value)}
              placeholder="ALM-0001"
            />
          )}
        </FormField>

        <FormField label="Descrição" required error={errors.descricao}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              maxLength={200}
              value={descricao}
              onChange={(e) => setDescricao(e.target.value)}
              placeholder="Papel A4 75g, resma 500 folhas"
            />
          )}
        </FormField>

        <FormField label="Unidade de medida" required error={errors.unidadeMedida} help="Ex.: un, kg, cx, L.">
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              maxLength={10}
              value={unidadeMedida}
              onChange={(e) => setUnidadeMedida(e.target.value)}
              placeholder="un"
            />
          )}
        </FormField>

        <FormField label="Método de custeio" required error={errors.metodoCusteio}>
          {({ id, describedBy, invalid }) => (
            <Select
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              options={OPCOES_METODO_CUSTEIO}
              value={metodoCusteio}
              onChange={(e) => setMetodoCusteio(e.target.value)}
            />
          )}
        </FormField>

        <FormField
          label="Ponto de pedido"
          required
          error={errors.pontoPedido}
          help="Saldo mínimo que dispara reposição."
        >
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              type="number"
              min="0"
              step="0.01"
              inputMode="decimal"
              aria-describedby={describedBy}
              invalid={invalid}
              value={pontoPedido}
              onChange={(e) => setPontoPedido(e.target.value)}
            />
          )}
        </FormField>

        <FormField label="Classificação ABC" required error={errors.classificacaoAbc}>
          {({ id, describedBy, invalid }) => (
            <Select
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              options={OPCOES_CLASSIFICACAO_ABC}
              value={classificacaoAbc}
              onChange={(e) => setClassificacaoAbc(e.target.value)}
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}
