// Formulario de REGISTRO de marcacao (batida) de ponto em Modal. Padrao-ouro: mutation +
// validacao por campo + Toast. O backend atribui o NSR sequencial (REP) e o devolve; a
// marcacao e imutavel (Portaria 671/2021). A pagina recebe o NSR via onRegistrada para
// compor a lista de marcacoes da sessao (nao ha GET de marcacoes no contrato M5).
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Button, FormField, Input, Modal, Select, useToast } from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import { useRegistrarMarcacao } from './api';
import type { RegistrarMarcacaoInput } from './api';
import { ORIGENS_REP, SENTIDOS_MARCACAO } from './recursosHumanos.helpers';

/** Marcacao registrada com sucesso, para compor a lista da sessao. */
export interface MarcacaoSessao {
  nsr: number;
  dataHora: string;
  sentido: number;
  origem: number;
}

export interface RegistrarMarcacaoFormModalProps {
  open: boolean;
  onClose: () => void;
  servidorId: string;
  servidorNome: string;
  onRegistrada: (marcacao: MarcacaoSessao) => void;
}

interface FormErrors {
  dataHora?: string;
  sentido?: string;
  origem?: string;
}

/** Data/hora local atual no formato aceito por <input type="datetime-local">. */
function agoraLocal(): string {
  const agora = new Date();
  const off = agora.getTimezoneOffset() * 60000;
  return new Date(agora.getTime() - off).toISOString().slice(0, 16);
}

export function RegistrarMarcacaoFormModal({
  open,
  onClose,
  servidorId,
  servidorNome,
  onRegistrada,
}: RegistrarMarcacaoFormModalProps) {
  const toast = useToast();
  const mutation = useRegistrarMarcacao();

  const [dataHora, setDataHora] = useState(agoraLocal());
  const [sentido, setSentido] = useState('1');
  const [origem, setOrigem] = useState('1');
  const [errors, setErrors] = useState<FormErrors>({});

  function validar(): FormErrors {
    const next: FormErrors = {};
    if (!dataHora) next.dataHora = 'Informe a data e a hora da batida.';
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

    // datetime-local nao traz offset: anexa o offset local para preservar o instante real.
    const iso = new Date(dataHora).toISOString();
    const input: RegistrarMarcacaoInput = {
      servidorId,
      dataHora: iso,
      sentido: Number(sentido),
      origem: Number(origem),
    };

    mutation.mutate(input, {
      onSuccess: (resultado) => {
        toast.success(`Marcação registrada — NSR ${resultado.nsr}.`, 'Sucesso');
        onRegistrada({
          nsr: resultado.nsr,
          dataHora: iso,
          sentido: input.sentido,
          origem: input.origem ?? 1,
        });
        fechar();
      },
      onError: (error) => {
        if (error instanceof ApiError && Object.keys(error.fieldErrors).length > 0) {
          const CAMPOS: ReadonlyArray<keyof FormErrors> = ['dataHora', 'sentido', 'origem'];
          const mapped: FormErrors = {};
          for (const [field, messages] of Object.entries(error.fieldErrors)) {
            const key = (field.charAt(0).toLowerCase() + field.slice(1)) as keyof FormErrors;
            if (CAMPOS.includes(key)) mapped[key] = messages[0];
          }
          setErrors(mapped);
        }
        toast.error(
          error instanceof ApiError ? error.userMessage : 'Não foi possível registrar a marcação.',
        );
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title={`Registrar marcação — ${servidorNome}`}
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button
            variant="primary"
            type="submit"
            form="form-registrar-marcacao"
            loading={mutation.isPending}
          >
            Registrar
          </Button>
        </>
      }
    >
      <form id="form-registrar-marcacao" className="br-form" onSubmit={submeter} noValidate>
        <FormField label="Data e hora da batida" required error={errors.dataHora}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              type="datetime-local"
              aria-describedby={describedBy}
              invalid={invalid}
              value={dataHora}
              onChange={(e) => setDataHora(e.target.value)}
            />
          )}
        </FormField>

        <FormField label="Sentido" required error={errors.sentido}>
          {({ id, describedBy, invalid }) => (
            <Select
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={sentido}
              onChange={(e) => setSentido(e.target.value)}
              options={SENTIDOS_MARCACAO}
            />
          )}
        </FormField>

        <FormField label="Origem (REP)" required error={errors.origem}>
          {({ id, describedBy, invalid }) => (
            <Select
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={origem}
              onChange={(e) => setOrigem(e.target.value)}
              options={ORIGENS_REP}
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}
