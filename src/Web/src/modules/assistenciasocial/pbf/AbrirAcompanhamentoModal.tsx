// Acao "Abrir acompanhamento" de condicionalidades do PBF de uma familia (command
// AbrirAcompanhamentoCondicionalidade — idempotente por (familia, competencia)).
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Alert, Button, FormField, Input, Modal, Select, useToast } from '../../../components/ui';
import { ApiError } from '../../../api/problemDetails';
import { useAbrirAcompanhamento } from './pbf.api';
import { MES_OPTIONS } from './pbf.helpers';

export interface AbrirAcompanhamentoModalProps {
  open: boolean;
  onClose: () => void;
  /** Pre-preenche o ano da competencia ativa, quando aberto a partir de uma consulta. */
  anoInicial: number;
  /** Pre-preenche o mes da competencia ativa. */
  mesInicial: number;
}

export function AbrirAcompanhamentoModal({
  open,
  onClose,
  anoInicial,
  mesInicial,
}: AbrirAcompanhamentoModalProps) {
  const toast = useToast();
  const mutation = useAbrirAcompanhamento();

  const [familiaId, setFamiliaId] = useState('');
  const [ano, setAno] = useState(String(anoInicial));
  const [mes, setMes] = useState(String(mesInicial));
  const [erroFamilia, setErroFamilia] = useState<string | undefined>();

  function fechar(): void {
    setFamiliaId('');
    setAno(String(anoInicial));
    setMes(String(mesInicial));
    setErroFamilia(undefined);
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    if (familiaId.trim() === '') {
      setErroFamilia('Informe o identificador da família.');
      return;
    }
    setErroFamilia(undefined);

    mutation.mutate(
      { familiaId: familiaId.trim(), ano: Number(ano), mes: Number(mes) },
      {
        onSuccess: () => {
          toast.success('Acompanhamento de condicionalidades aberto.', 'Sucesso');
          fechar();
        },
        onError: (error) => {
          if (error instanceof ApiError && error.fieldErrors.FamiliaId) {
            setErroFamilia(error.fieldErrors.FamiliaId[0]);
          }
          toast.error(
            error instanceof ApiError
              ? error.userMessage
              : 'Não foi possível abrir o acompanhamento.',
          );
        },
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Abrir acompanhamento de condicionalidades"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-abrir-acompanhamento" loading={mutation.isPending}>
            Abrir acompanhamento
          </Button>
        </>
      }
    >
      <form id="form-abrir-acompanhamento" className="br-form" onSubmit={submeter} noValidate>
        <Alert variant="info">
          A abertura é idempotente por família e competência: reabrir a mesma competência retorna o
          acompanhamento existente, sem duplicar.
        </Alert>

        <FormField
          label="Identificador da família"
          required
          error={erroFamilia}
          help="Família beneficiária do PBF (CadÚnico)."
        >
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={familiaId}
              onChange={(e) => setFamiliaId(e.target.value)}
              placeholder="00000000-0000-0000-0000-000000000000"
            />
          )}
        </FormField>

        <div className="row">
          <div className="col-sm-6">
            <FormField label="Ano" required>
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  type="number"
                  min="1900"
                  max="9999"
                  step="1"
                  inputMode="numeric"
                  aria-describedby={describedBy}
                  invalid={invalid}
                  value={ano}
                  onChange={(e) => setAno(e.target.value)}
                />
              )}
            </FormField>
          </div>
          <div className="col-sm-6">
            <FormField label="Mês" required>
              {({ id, describedBy, invalid }) => (
                <Select
                  id={id}
                  aria-describedby={describedBy}
                  invalid={invalid}
                  options={MES_OPTIONS}
                  value={mes}
                  onChange={(e) => setMes(e.target.value)}
                />
              )}
            </FormField>
          </div>
        </div>
      </form>
    </Modal>
  );
}
