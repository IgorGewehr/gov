// Formulário de RENOVAÇÃO de alvará (RenovarAlvaraCommand): novo período de vigência
// + dados da TLL de renovação (código + exercício + quantidade-base + vencimento). A
// TLL é lançada à parte a partir da tabela de taxa vigente. Acessível (Modal).
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Alert, Button, FormField, Input, Modal, useToast } from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import { formatarMoeda } from '../../i18n/format';
import { useRenovarAlvara } from './alvaras.api';
import type { RenovarAlvaraInput } from './alvaras.api';

export interface RenovarAlvaraModalProps {
  open: boolean;
  onClose: () => void;
}

export function RenovarAlvaraModal({ open, onClose }: RenovarAlvaraModalProps) {
  const toast = useToast();
  const mutation = useRenovarAlvara();

  const [alvaraId, setAlvaraId] = useState('');
  const [novoInicio, setNovoInicio] = useState('');
  const [novoFim, setNovoFim] = useState('');
  const [codigoTaxaTll, setCodigoTaxaTll] = useState('');
  const [exercicio, setExercicio] = useState(String(new Date().getFullYear()));
  const [quantidadeBaseTll, setQuantidadeBaseTll] = useState('');
  const [vencimentoTll, setVencimentoTll] = useState('');
  const [erro, setErro] = useState<string | null>(null);

  function fechar(): void {
    setAlvaraId('');
    setNovoInicio('');
    setNovoFim('');
    setCodigoTaxaTll('');
    setExercicio(String(new Date().getFullYear()));
    setQuantidadeBaseTll('');
    setVencimentoTll('');
    setErro(null);
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    if (alvaraId.trim() === '') {
      setErro('Informe o identificador do alvará a renovar.');
      return;
    }
    if (novoInicio.trim() === '' || novoFim.trim() === '') {
      setErro('Informe o novo período de vigência.');
      return;
    }
    if (novoFim < novoInicio) {
      setErro('O fim da vigência deve ser igual ou posterior ao início.');
      return;
    }
    if (codigoTaxaTll.trim() === '' || vencimentoTll.trim() === '') {
      setErro('Informe o código da TLL e o vencimento da guia.');
      return;
    }
    setErro(null);

    const input: RenovarAlvaraInput = {
      novoInicioVigencia: novoInicio.trim(),
      novoFimVigencia: novoFim.trim(),
      codigoTaxaTll: codigoTaxaTll.trim(),
      exercicio: Number(exercicio),
      quantidadeBaseTll: Number((quantidadeBaseTll || '0').replace(',', '.')),
      vencimentoTll: vencimentoTll.trim(),
    };
    mutation.mutate(
      { alvaraId: alvaraId.trim(), input },
      {
        onSuccess: (r) => {
          toast.success(`Alvará renovado. TLL: ${formatarMoeda(r.valorTll)}.`, 'Sucesso');
          fechar();
        },
        onError: (error) =>
          toast.error(
            error instanceof ApiError ? error.userMessage : 'Não foi possível renovar o alvará.',
          ),
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Renovar alvará + TLL"
      size="large"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-renovar-alvara" loading={mutation.isPending}>
            Renovar
          </Button>
        </>
      }
    >
      <form id="form-renovar-alvara" className="br-form" onSubmit={submeter} noValidate>
        {erro && (
          <Alert variant="danger" title="Verifique os dados">
            {erro}
          </Alert>
        )}

        <FormField label="Identificador do alvará" required>
          {({ id, describedBy, invalid }) => (
            <Input id={id} aria-describedby={describedBy} invalid={invalid} value={alvaraId} onChange={(e) => setAlvaraId(e.target.value)} placeholder="00000000-0000-0000-0000-000000000000" />
          )}
        </FormField>

        <div className="row">
          <div className="col-md-6">
            <FormField label="Novo início de vigência" required>
              {({ id, describedBy, invalid }) => (
                <Input id={id} type="date" aria-describedby={describedBy} invalid={invalid} value={novoInicio} onChange={(e) => setNovoInicio(e.target.value)} />
              )}
            </FormField>
          </div>
          <div className="col-md-6">
            <FormField label="Novo fim de vigência" required>
              {({ id, describedBy, invalid }) => (
                <Input id={id} type="date" aria-describedby={describedBy} invalid={invalid} value={novoFim} onChange={(e) => setNovoFim(e.target.value)} />
              )}
            </FormField>
          </div>
        </div>

        <fieldset className="mb-3">
          <legend className="text-up-01 text-semi-bold">Taxa de licença (TLL) de renovação</legend>
          <div className="row">
            <div className="col-md-4">
              <FormField label="Código da TLL (CTM)" required>
                {({ id, describedBy, invalid }) => (
                  <Input id={id} aria-describedby={describedBy} invalid={invalid} value={codigoTaxaTll} onChange={(e) => setCodigoTaxaTll(e.target.value)} placeholder="TLL-001" />
                )}
              </FormField>
            </div>
            <div className="col-md-3">
              <FormField label="Exercício" required>
                {({ id, describedBy, invalid }) => (
                  <Input id={id} type="number" min={1900} inputMode="numeric" aria-describedby={describedBy} invalid={invalid} value={exercicio} onChange={(e) => setExercicio(e.target.value)} />
                )}
              </FormField>
            </div>
            <div className="col-md-2">
              <FormField label="Qtd.-base" help="Ignorada no Valor fixo.">
                {({ id }) => (
                  <Input id={id} type="number" min="0" step="0.01" inputMode="decimal" value={quantidadeBaseTll} onChange={(e) => setQuantidadeBaseTll(e.target.value)} />
                )}
              </FormField>
            </div>
            <div className="col-md-3">
              <FormField label="Vencimento da TLL" required>
                {({ id, describedBy, invalid }) => (
                  <Input id={id} type="date" aria-describedby={describedBy} invalid={invalid} value={vencimentoTll} onChange={(e) => setVencimentoTll(e.target.value)} />
                )}
              </FormField>
            </div>
          </div>
        </fieldset>
      </form>
    </Modal>
  );
}
