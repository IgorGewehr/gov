// Modais de GESTÃO da distribuição do FUNDEB (E-3): abrir distribuição, definir
// esperado por origem, receber parcela por origem e registrar a remuneração do
// magistério. Todas exigem "educacao.gerenciar" (gated pelos botões que os abrem).
// Padrão mutation + validação por campo + Toast (espelha DiarioClasseFormModal).
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Button, FormField, Input, Modal, Select, useToast } from '../../components/ui';
import type { SelectOption } from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import {
  ORIGEM_FUNDEB_LABEL,
  ORIGEM_FUNDEB_VALOR,
  useAbrirDistribuicaoFundeb,
  useDefinirEsperadoFundeb,
  useReceberParcelaFundeb,
  useRegistrarRemuneracao,
} from './fiscal.api';
import type { OrigemRecursoFundeb } from './fiscal.api';

const ORIGEM_OPCOES: SelectOption[] = (
  Object.keys(ORIGEM_FUNDEB_VALOR) as OrigemRecursoFundeb[]
).map((k) => ({ value: k, label: ORIGEM_FUNDEB_LABEL[k] }));

function parseValor(texto: string): number {
  return Number((texto || '').replace(',', '.'));
}

// ---------------------------------------------------------------------------
// Abrir distribuição
// ---------------------------------------------------------------------------

export interface AbrirDistribuicaoFundebModalProps {
  open: boolean;
  exercicio: number;
  onClose: () => void;
  onCriado?: (id: string) => void;
}

export function AbrirDistribuicaoFundebModal({
  open,
  exercicio,
  onClose,
  onCriado,
}: AbrirDistribuicaoFundebModalProps) {
  const toast = useToast();
  const mutation = useAbrirDistribuicaoFundeb();

  function submeter(event: FormEvent): void {
    event.preventDefault();
    mutation.mutate(
      { exercicio },
      {
        onSuccess: (r) => {
          toast.success(`Distribuição do FUNDEB ${exercicio} aberta.`, 'Sucesso');
          onCriado?.(r.id);
          onClose();
        },
        onError: (error) =>
          toast.error(
            error instanceof ApiError ? error.userMessage : 'Não foi possível abrir a distribuição.',
          ),
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={onClose}
      title="Abrir distribuição do FUNDEB"
      size="small"
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-abrir-fundeb" loading={mutation.isPending}>
            Abrir distribuição
          </Button>
        </>
      }
    >
      <form id="form-abrir-fundeb" className="br-form" onSubmit={submeter} noValidate>
        <p className="mb-0">
          Abrir a distribuição do FUNDEB do exercício <strong>{exercicio}</strong>?
        </p>
      </form>
    </Modal>
  );
}

// ---------------------------------------------------------------------------
// Movimento por origem (esperado / parcela)
// ---------------------------------------------------------------------------

export interface MovimentoFundebModalProps {
  open: boolean;
  /** 'esperado' = valor divulgado; 'parcela' = recebimento efetivo. */
  tipo: 'esperado' | 'parcela';
  distribuicaoId: string;
  onClose: () => void;
}

export function MovimentoFundebModal({
  open,
  tipo,
  distribuicaoId,
  onClose,
}: MovimentoFundebModalProps) {
  const toast = useToast();
  const definir = useDefinirEsperadoFundeb(distribuicaoId);
  const receber = useReceberParcelaFundeb(distribuicaoId);
  const mutation = tipo === 'esperado' ? definir : receber;

  const [origem, setOrigem] = useState<OrigemRecursoFundeb>('CotaParteEstadual');
  const [valor, setValor] = useState('');
  const [erro, setErro] = useState<string | undefined>();

  const titulo = tipo === 'esperado' ? 'Definir valor esperado' : 'Receber parcela';
  const valorLabel = tipo === 'esperado' ? 'Valor esperado (R$)' : 'Valor recebido (R$)';

  function fechar(): void {
    setErro(undefined);
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const valorNum = parseValor(valor);
    // Parcela exige valor > 0 (back: GreaterThan(0)); esperado admite >= 0.
    const invalido =
      !Number.isFinite(valorNum) || valorNum < 0 || (tipo === 'parcela' && valorNum <= 0);
    if (invalido) {
      setErro(tipo === 'parcela' ? 'Informe um valor maior que zero.' : 'Informe um valor válido.');
      return;
    }
    const origemValor = ORIGEM_FUNDEB_VALOR[origem];
    const onResult = {
      onSuccess: () => {
        toast.success(`${titulo} registrado.`, 'Sucesso');
        setValor('');
        fechar();
      },
      onError: (error: unknown) =>
        toast.error(
          error instanceof ApiError ? error.userMessage : 'Não foi possível registrar o movimento.',
        ),
    };
    if (tipo === 'esperado') {
      definir.mutate({ origem: origemValor, valorEsperado: valorNum }, onResult);
    } else {
      receber.mutate({ origem: origemValor, valor: valorNum }, onResult);
    }
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title={titulo}
      size="small"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-mov-fundeb" loading={mutation.isPending}>
            {titulo}
          </Button>
        </>
      }
    >
      <form id="form-mov-fundeb" className="br-form" onSubmit={submeter} noValidate>
        <FormField label="Origem do recurso" required>
          {({ id }) => (
            <Select
              id={id}
              options={ORIGEM_OPCOES}
              value={origem}
              onChange={(e) => setOrigem(e.target.value as OrigemRecursoFundeb)}
            />
          )}
        </FormField>
        <FormField label={valorLabel} required error={erro}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              type="number"
              min="0"
              step="0.01"
              inputMode="decimal"
              aria-describedby={describedBy}
              invalid={invalid}
              value={valor}
              onChange={(e) => setValor(e.target.value)}
              placeholder="0,00"
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}

// ---------------------------------------------------------------------------
// Registrar remuneração do magistério (numerador dos 70%)
// ---------------------------------------------------------------------------

export interface RegistrarRemuneracaoModalProps {
  open: boolean;
  exercicio: number;
  onClose: () => void;
}

export function RegistrarRemuneracaoModal({
  open,
  exercicio,
  onClose,
}: RegistrarRemuneracaoModalProps) {
  const toast = useToast();
  const mutation = useRegistrarRemuneracao();
  const [valor, setValor] = useState('');
  const [erro, setErro] = useState<string | undefined>();

  function fechar(): void {
    setErro(undefined);
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const valorNum = parseValor(valor);
    if (!Number.isFinite(valorNum) || valorNum < 0) {
      setErro('Informe um valor maior ou igual a zero.');
      return;
    }
    mutation.mutate(
      { exercicio, remuneracaoProfissionais: valorNum },
      {
        onSuccess: () => {
          toast.success('Remuneração do magistério registrada.', 'Sucesso');
          setValor('');
          fechar();
        },
        onError: (error) =>
          toast.error(
            error instanceof ApiError ? error.userMessage : 'Não foi possível registrar a remuneração.',
          ),
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Registrar remuneração do magistério"
      size="small"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-remun-fundeb" loading={mutation.isPending}>
            Registrar
          </Button>
        </>
      }
    >
      <form id="form-remun-fundeb" className="br-form" onSubmit={submeter} noValidate>
        <FormField
          label={`Remuneração paga no exercício ${exercicio} (R$)`}
          required
          error={erro}
          help="Total pago aos profissionais da educação básica. // parametrizável até o cruzamento com a folha do RH."
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
              value={valor}
              onChange={(e) => setValor(e.target.value)}
              placeholder="0,00"
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}
