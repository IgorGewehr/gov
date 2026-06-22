// Ação de LANÇAR o IPTU apurado (command LancarIptu): constitui o lançamento e
// gera o DAM. Pede a quantidade de parcelas e o vencimento da 1ª. Ao concluir,
// exibe o lançamento/DAM e o imposto devido. Acessível (Modal).
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Alert, Button, FormField, Input, Modal, useToast } from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import { formatarMoeda } from '../../i18n/format';
import { useLancarIptu } from './iptu.api';
import type { LancamentoIptuResultado } from './iptu.api';

export interface LancarIptuModalProps {
  open: boolean;
  onClose: () => void;
  imovelId: string;
  exercicio: number;
  /** Parcelas sugeridas pela tabela de alíquotas (padrão do exercício). */
  parcelasSugeridas?: number;
}

interface FormErrors {
  numeroParcelas?: string;
  primeiroVencimento?: string;
}

const PARCELAS_MAX = 12;

export function LancarIptuModal({
  open,
  onClose,
  imovelId,
  exercicio,
  parcelasSugeridas = 1,
}: LancarIptuModalProps) {
  const toast = useToast();
  const lancar = useLancarIptu(imovelId);

  const [quantidadeParcelas, setQuantidade] = useState(String(parcelasSugeridas));
  const [vencimento, setVencimento] = useState('');
  const [errors, setErrors] = useState<FormErrors>({});
  const [resultado, setResultado] = useState<LancamentoIptuResultado | null>(null);

  function validar(): FormErrors {
    const next: FormErrors = {};
    const qtd = Number(quantidadeParcelas);
    if (!Number.isInteger(qtd) || qtd < 1 || qtd > PARCELAS_MAX)
      next.numeroParcelas = `Informe de 1 a ${PARCELAS_MAX} parcelas.`;
    if (vencimento.trim() === '') next.primeiroVencimento = 'Informe o vencimento da 1ª parcela.';
    return next;
  }

  function fechar(): void {
    setErrors({});
    setQuantidade(String(parcelasSugeridas));
    setVencimento('');
    setResultado(null);
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const validacao = validar();
    setErrors(validacao);
    if (Object.keys(validacao).length > 0) return;

    lancar.mutate(
      {
        exercicio,
        numeroParcelas: Number(quantidadeParcelas),
        primeiroVencimento: vencimento.trim(),
      },
      {
        onSuccess: (res) => {
          setResultado(res);
          toast.success(`IPTU ${exercicio} lançado (DAM ${res.damId}).`, 'Sucesso');
        },
        onError: (error) => {
          if (error instanceof ApiError && Object.keys(error.fieldErrors).length > 0) {
            const mapped: FormErrors = {};
            for (const [field, messages] of Object.entries(error.fieldErrors)) {
              const key = field.charAt(0).toLowerCase() + field.slice(1);
              (mapped as Record<string, string>)[key] = messages[0];
            }
            setErrors(mapped);
          }
          toast.error(error instanceof ApiError ? error.userMessage : 'Não foi possível lançar o IPTU.');
        },
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title={`Lançar IPTU ${exercicio}`}
      footer={
        resultado ? (
          <Button variant="primary" onClick={fechar}>
            Concluir
          </Button>
        ) : (
          <>
            <Button variant="secondary" onClick={fechar} disabled={lancar.isPending}>
              Cancelar
            </Button>
            <Button variant="primary" type="submit" form="form-lancar-iptu" loading={lancar.isPending}>
              Lançar e gerar DAM
            </Button>
          </>
        )
      }
    >
      {resultado ? (
        <Alert variant="success" title="Lançamento gerado">
          Lançamento <strong>{resultado.lancamentoId}</strong> e DAM <strong>{resultado.damId}</strong>{' '}
          gerados. Imposto devido: <strong>{formatarMoeda(resultado.impostoDevido)}</strong>.
        </Alert>
      ) : (
        <form id="form-lancar-iptu" className="br-form" onSubmit={submeter} noValidate>
          <Alert variant="info" title="Constituição do crédito">
            O lançamento constitui definitivamente o crédito tributário do IPTU {exercicio} e gera o DAM
            (Documento de Arrecadação Municipal) parcelado.
          </Alert>
          <div className="row">
            <div className="col-md-6">
              <FormField label="Quantidade de parcelas" required error={errors.numeroParcelas}>
                {({ id, describedBy, invalid }) => (
                  <Input
                    id={id}
                    type="number"
                    min={1}
                    max={PARCELAS_MAX}
                    inputMode="numeric"
                    aria-describedby={describedBy}
                    invalid={invalid}
                    value={quantidadeParcelas}
                    onChange={(e) => setQuantidade(e.target.value)}
                  />
                )}
              </FormField>
            </div>
            <div className="col-md-6">
              <FormField label="Vencimento da 1ª parcela" required error={errors.primeiroVencimento}>
                {({ id, describedBy, invalid }) => (
                  <Input
                    id={id}
                    type="date"
                    aria-describedby={describedBy}
                    invalid={invalid}
                    value={vencimento}
                    onChange={(e) => setVencimento(e.target.value)}
                  />
                )}
              </FormField>
            </div>
          </div>
        </form>
      )}
    </Modal>
  );
}
