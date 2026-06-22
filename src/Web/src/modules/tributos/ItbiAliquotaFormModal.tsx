// Formulário de CONFIGURAÇÃO das alíquotas do ITBI (command ConfigurarAliquotasItbi):
// por exercício, a alíquota GERAL e a alíquota do SFH (Sistema Financeiro de
// Habitação), ambas digitadas em % e convertidas para FRAÇÃO DECIMAL. Acessível.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Alert, Button, FormField, Input, Modal, useToast } from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import { useConfigurarAliquotasItbi } from './itbi.api';
import type { ConfigurarAliquotasItbiInput } from './itbi.api';
import { percentualParaFracao } from './iptu.helpers';

const ANO_ATUAL = new Date().getFullYear();

export interface ItbiAliquotaFormModalProps {
  open: boolean;
  onClose: () => void;
}

export function ItbiAliquotaFormModal({ open, onClose }: ItbiAliquotaFormModalProps) {
  const toast = useToast();
  const mutation = useConfigurarAliquotasItbi();

  const [exercicio, setExercicio] = useState(String(ANO_ATUAL));
  const [aliquotaGeral, setGeral] = useState('');
  const [aliquotaSfh, setSfh] = useState('');
  const [erro, setErro] = useState<string | null>(null);

  function fechar(): void {
    setExercicio(String(ANO_ATUAL));
    setGeral('');
    setSfh('');
    setErro(null);
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const ano = Number(exercicio);
    if (!Number.isInteger(ano) || ano < 1900) {
      setErro('Informe um exercício válido.');
      return;
    }
    const geral = percentualParaFracao(aliquotaGeral);
    const sfh = percentualParaFracao(aliquotaSfh);
    if (Number.isNaN(geral) || geral <= 0 || geral >= 1) {
      setErro('Informe a alíquota geral (%) maior que zero.');
      return;
    }
    if (Number.isNaN(sfh) || sfh <= 0 || sfh >= 1) {
      setErro('Informe a alíquota do SFH (%) maior que zero.');
      return;
    }
    setErro(null);

    const input: ConfigurarAliquotasItbiInput = {
      exercicio: ano,
      aliquotaGeral: geral,
      aliquotaSfh: sfh,
    };
    mutation.mutate(input, {
      onSuccess: (r) => {
        toast.success(`Alíquotas do ITBI ${ano} configuradas (id ${r.id}).`, 'Sucesso');
        fechar();
      },
      onError: (error) =>
        toast.error(
          error instanceof ApiError ? error.userMessage : 'Não foi possível configurar as alíquotas.',
        ),
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Configurar alíquotas do ITBI"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-itbi-aliquotas" loading={mutation.isPending}>
            Configurar
          </Button>
        </>
      }
    >
      <form id="form-itbi-aliquotas" className="br-form" onSubmit={submeter} noValidate>
        {erro && (
          <Alert variant="danger" title="Verifique os dados">
            {erro}
          </Alert>
        )}

        <FormField label="Exercício" required>
          {({ id, describedBy, invalid }) => (
            <Input id={id} type="number" min={1900} inputMode="numeric" aria-describedby={describedBy} invalid={invalid} value={exercicio} onChange={(e) => setExercicio(e.target.value)} />
          )}
        </FormField>

        <div className="row">
          <div className="col-md-6">
            <FormField label="Alíquota geral (%)" required help="Aplicada às transmissões em geral.">
              {({ id, describedBy, invalid }) => (
                <Input id={id} type="number" min="0" step="0.01" inputMode="decimal" aria-describedby={describedBy} invalid={invalid} value={aliquotaGeral} onChange={(e) => setGeral(e.target.value)} placeholder="2,00" />
              )}
            </FormField>
          </div>
          <div className="col-md-6">
            <FormField label="Alíquota SFH (%)" required help="Sistema Financeiro de Habitação (reduzida).">
              {({ id, describedBy, invalid }) => (
                <Input id={id} type="number" min="0" step="0.01" inputMode="decimal" aria-describedby={describedBy} invalid={invalid} value={aliquotaSfh} onChange={(e) => setSfh(e.target.value)} placeholder="0,50" />
              )}
            </FormField>
          </div>
        </div>
      </form>
    </Modal>
  );
}
