// [Command AtenderRequisicao §5.3] Atende uma requisição de material: gera saída, valora pelo
// custeio (I-3/I-8), reduz o saldo e reconhece despesa no consumo (I-4/I-7). Ao atingir o ponto
// de pedido, dispara reposição (I-6). Validações: Requisicao/Solicitante obrigatórios;
// Quantidade > 0 (I-11); alerta de saldo insuficiente (I-1) antes do envio.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Alert, Button, FormField, Input, Modal, useToast } from '../../../components/ui';
import { ApiError } from '../../../api/problemDetails';
import { useAtenderRequisicao } from './itemestoque.api';
import type { AtenderRequisicaoInput } from './itemestoque.api';
import { hojeIso, mapearFieldErrors } from './itemEstoque.helpers';

export interface ItemEstoqueRequisicaoModalProps {
  open: boolean;
  onClose: () => void;
  itemId: string;
  /** Saldo atual do item, para alerta preventivo de saldo insuficiente (I-1). */
  saldoAtual: number;
  pontoPedido: number;
  unidadeMedida: string;
}

const CAMPOS = ['requisicaoId', 'solicitanteId', 'quantidade', 'data'] as const;
type Campo = (typeof CAMPOS)[number];
type FormErrors = Partial<Record<Campo, string>>;

export function ItemEstoqueRequisicaoModal({
  open,
  onClose,
  itemId,
  saldoAtual,
  pontoPedido,
  unidadeMedida,
}: ItemEstoqueRequisicaoModalProps) {
  const toast = useToast();
  const mutation = useAtenderRequisicao(itemId);

  const [requisicaoId, setRequisicaoId] = useState('');
  const [solicitanteId, setSolicitanteId] = useState('');
  const [quantidade, setQuantidade] = useState('');
  const [data, setData] = useState(hojeIso());
  const [errors, setErrors] = useState<FormErrors>({});

  const qtdNum = Number(quantidade);
  const saldoInsuficiente = quantidade.trim() !== '' && !Number.isNaN(qtdNum) && qtdNum > saldoAtual;
  const atingiraPontoPedido =
    quantidade.trim() !== '' && !Number.isNaN(qtdNum) && qtdNum > 0 && qtdNum <= saldoAtual && saldoAtual - qtdNum <= pontoPedido;

  function validar(): FormErrors {
    const next: FormErrors = {};
    if (requisicaoId.trim() === '') next.requisicaoId = 'Informe o identificador da requisição.';
    if (solicitanteId.trim() === '') next.solicitanteId = 'Informe o solicitante.';
    if (quantidade.trim() === '' || Number.isNaN(qtdNum) || qtdNum <= 0)
      next.quantidade = 'Quantidade deve ser maior que zero.';
    else if (qtdNum > saldoAtual) next.quantidade = 'Saldo insuficiente para atender a requisição.';
    if (data.trim() === '') next.data = 'Informe a data do atendimento.';
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

    const input: AtenderRequisicaoInput = {
      requisicaoId: requisicaoId.trim(),
      solicitanteId: solicitanteId.trim(),
      quantidade: qtdNum,
      data,
    };

    mutation.mutate(input, {
      onSuccess: () => {
        toast.success('Requisição atendida e saldo baixado.', 'Sucesso');
        if (saldoAtual - qtdNum <= pontoPedido) {
          toast.warning('Ponto de pedido atingido — reposição acionada na Administração.', 'Reposição');
        }
        fechar();
      },
      onError: (error) => {
        if (error instanceof ApiError && Object.keys(error.fieldErrors).length > 0) {
          setErrors(mapearFieldErrors(error.fieldErrors, CAMPOS));
        }
        toast.error(
          error instanceof ApiError ? error.userMessage : 'Não foi possível atender a requisição.',
        );
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Atender requisição de material"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button
            variant="primary"
            type="submit"
            form="form-requisicao-item"
            loading={mutation.isPending}
            disabled={saldoInsuficiente}
          >
            Atender requisição
          </Button>
        </>
      }
    >
      <form id="form-requisicao-item" className="br-form" onSubmit={submeter} noValidate>
        <p className="text-gray-60">
          Saldo disponível: <strong>{saldoAtual}</strong> {unidadeMedida}.
        </p>

        {saldoInsuficiente && (
          <Alert variant="danger" title="Saldo insuficiente">
            A quantidade requisitada excede o saldo disponível ({saldoAtual} {unidadeMedida}). A baixa será
            rejeitada (I-1).
          </Alert>
        )}

        {atingiraPontoPedido && (
          <Alert variant="warning" title="Atenção">
            Após esta saída o saldo atingirá ou ficará abaixo do ponto de pedido ({pontoPedido} {unidadeMedida}),
            acionando reposição.
          </Alert>
        )}

        <FormField label="Identificador da requisição" required error={errors.requisicaoId}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={requisicaoId}
              onChange={(e) => setRequisicaoId(e.target.value)}
              placeholder="00000000-0000-0000-0000-000000000000"
            />
          )}
        </FormField>

        <FormField label="Solicitante" required error={errors.solicitanteId} help="Identificador de quem requisita.">
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

        <FormField label={`Quantidade (${unidadeMedida})`} required error={errors.quantidade}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              type="number"
              min="0"
              step="0.01"
              inputMode="decimal"
              aria-describedby={describedBy}
              invalid={invalid}
              value={quantidade}
              onChange={(e) => setQuantidade(e.target.value)}
            />
          )}
        </FormField>

        <FormField label="Data do atendimento" required error={errors.data}>
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
      </form>
    </Modal>
  );
}
