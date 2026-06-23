// Configuracao de servico ofertado + equipe de referencia de uma unidade (command ConfigurarUnidade).
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Alert, Button, FormField, Input, Modal, Select, useToast } from '../../../components/ui';
import { ApiError } from '../../../api/problemDetails';
import { useConfigurarUnidade } from './censo.api';
import type { TipoServico, UnidadeSocioassistencial } from './censo.api';
import { SERVICO_OPTIONS, tipoUnidadeSigla } from './censo.helpers';

export interface ConfigurarUnidadeModalProps {
  open: boolean;
  onClose: () => void;
  /** Unidade a configurar. */
  unidade: UnidadeSocioassistencial;
}

export function ConfigurarUnidadeModal({ open, onClose, unidade }: ConfigurarUnidadeModalProps) {
  const toast = useToast();
  const mutation = useConfigurarUnidade();

  const [servico, setServico] = useState<TipoServico>('Paif');
  const [capacidade, setCapacidade] = useState('0');
  const [profissionais, setProfissionais] = useState(String(unidade.quantidadeProfissionais));
  const [erro, setErro] = useState<string | undefined>();

  function fechar(): void {
    setServico('Paif');
    setCapacidade('0');
    setProfissionais(String(unidade.quantidadeProfissionais));
    setErro(undefined);
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const capNum = Number(capacidade);
    const profNum = Number(profissionais);
    if (!Number.isInteger(capNum) || capNum < 0 || !Number.isInteger(profNum) || profNum < 0) {
      setErro('Capacidade e equipe devem ser inteiros maiores ou iguais a zero.');
      return;
    }
    setErro(undefined);

    mutation.mutate(
      {
        unidadeId: unidade.id,
        servico,
        capacidadeMensal: capNum,
        quantidadeProfissionais: profNum,
      },
      {
        onSuccess: () => {
          toast.success('Configuração da unidade atualizada.', 'Sucesso');
          fechar();
        },
        onError: (error) => {
          toast.error(
            error instanceof ApiError
              ? error.userMessage
              : 'Não foi possível configurar a unidade.',
          );
        },
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title={`Configurar ${unidade.nome}`}
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-configurar-unidade" loading={mutation.isPending}>
            Salvar configuração
          </Button>
        </>
      }
    >
      <form id="form-configurar-unidade" className="br-form" onSubmit={submeter} noValidate>
        <Alert variant="info">
          Unidade <strong>{tipoUnidadeSigla(unidade.tipo)}</strong>. Oferte/atualize a capacidade de
          um serviço tipificado e defina a equipe de referência. PAIF só em CRAS; PAEFI só em CREAS.
        </Alert>

        {erro && (
          <Alert variant="danger" className="mb-3">
            {erro}
          </Alert>
        )}

        <FormField label="Serviço tipificado" required>
          {({ id, describedBy, invalid }) => (
            <Select
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              options={SERVICO_OPTIONS}
              value={servico}
              onChange={(e) => setServico(e.target.value as TipoServico)}
            />
          )}
        </FormField>

        <div className="row">
          <div className="col-sm-6">
            <FormField label="Capacidade mensal" required help="Atendimentos/mês do serviço.">
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  type="number"
                  min="0"
                  step="1"
                  inputMode="numeric"
                  aria-describedby={describedBy}
                  invalid={invalid}
                  value={capacidade}
                  onChange={(e) => setCapacidade(e.target.value)}
                />
              )}
            </FormField>
          </div>
          <div className="col-sm-6">
            <FormField label="Equipe de referência" required help="Quantidade de profissionais.">
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  type="number"
                  min="0"
                  step="1"
                  inputMode="numeric"
                  aria-describedby={describedBy}
                  invalid={invalid}
                  value={profissionais}
                  onChange={(e) => setProfissionais(e.target.value)}
                />
              )}
            </FormField>
          </div>
        </div>
      </form>
    </Modal>
  );
}
