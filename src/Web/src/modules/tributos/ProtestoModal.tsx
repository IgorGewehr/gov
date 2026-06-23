// Modal de AÇÃO "Protesto extrajudicial" (Lei 9.492/97) de uma CDA: gera a REMESSA ao CRA estadual
// (command GerarRemessaProtesto) e, em seguida, processa o RETORNO do cartório (command
// ProcessarRetornoProtesto). Espelha os payloads reais: remessa = { dataGeracao }; retorno =
// { remessaProtestoId, ocorrencia(1..4), dataRetorno, protocoloCartorio? }. A ocorrência Pendente(0)
// é recusada pelo backend, por isso não é ofertada. WIRED a duas mutations TanStack Query.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Alert, Button, FormField, Input, Modal, Select, useToast } from '../../components/ui';
import type { SelectOption } from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import { OCORRENCIA_PROTESTO_VALOR, useGerarRemessaProtesto, useProcessarRetornoProtesto } from './api';
import type { OcorrenciaProtesto } from './api';
import { OCORRENCIA_PROTESTO_LABEL } from './dividaAtiva.helpers';

export interface ProtestoModalProps {
  open: boolean;
  onClose: () => void;
  dividaAtivaId: string;
  /** Contribuinte cuja lista de dívidas deve ser invalidada após cada etapa. */
  contribuinteIdParaInvalidar?: string;
}

const OCORRENCIAS: OcorrenciaProtesto[] = ['Lavrado', 'PagoOuRetirado', 'Sustado', 'Rejeitado'];
const OCORRENCIA_OPCOES: SelectOption[] = OCORRENCIAS.map((o) => ({
  value: o,
  label: OCORRENCIA_PROTESTO_LABEL[o],
}));

function hojeIso(): string {
  return new Date().toISOString().slice(0, 10);
}

export function ProtestoModal({
  open,
  onClose,
  dividaAtivaId,
  contribuinteIdParaInvalidar,
}: ProtestoModalProps) {
  const toast = useToast();
  const remessaMutation = useGerarRemessaProtesto(contribuinteIdParaInvalidar);
  const retornoMutation = useProcessarRetornoProtesto(contribuinteIdParaInvalidar);

  const [dataGeracao, setDataGeracao] = useState(hojeIso());
  const [ocorrencia, setOcorrencia] = useState<OcorrenciaProtesto | ''>('');
  const [dataRetorno, setDataRetorno] = useState(hojeIso());
  const [protocolo, setProtocolo] = useState('');
  const [erro, setErro] = useState<string | undefined>();

  const remessa = remessaMutation.data;

  function fechar(): void {
    setDataGeracao(hojeIso());
    setOcorrencia('');
    setDataRetorno(hojeIso());
    setProtocolo('');
    setErro(undefined);
    remessaMutation.reset();
    retornoMutation.reset();
    onClose();
  }

  function gerarRemessa(event: FormEvent): void {
    event.preventDefault();
    if (dataGeracao === '') {
      setErro('Informe a data de geração da remessa.');
      return;
    }
    setErro(undefined);
    remessaMutation.mutate(
      { dividaAtivaId, dataGeracao },
      {
        onSuccess: () => toast.success('Remessa de protesto gerada.', 'Sucesso'),
        onError: (error) =>
          toast.error(
            error instanceof ApiError ? error.userMessage : 'Não foi possível gerar a remessa.',
          ),
      },
    );
  }

  function processarRetorno(event: FormEvent): void {
    event.preventDefault();
    if (!remessa) return;
    if (ocorrencia === '') {
      setErro('Selecione a ocorrência de retorno.');
      return;
    }
    if (dataRetorno === '') {
      setErro('Informe a data do retorno.');
      return;
    }
    setErro(undefined);
    retornoMutation.mutate(
      {
        dividaAtivaId,
        input: {
          remessaProtestoId: remessa.remessaProtestoId,
          ocorrencia: OCORRENCIA_PROTESTO_VALOR[ocorrencia],
          dataRetorno,
          protocoloCartorio: protocolo.trim() || null,
        },
      },
      {
        onSuccess: () => {
          toast.success('Retorno do protesto processado.', 'Sucesso');
          fechar();
        },
        onError: (error) =>
          toast.error(
            error instanceof ApiError ? error.userMessage : 'Não foi possível processar o retorno.',
          ),
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Protesto extrajudicial da CDA"
      footer={
        remessa ? (
          <>
            <Button variant="secondary" onClick={fechar} disabled={retornoMutation.isPending}>
              Fechar
            </Button>
            <Button
              variant="primary"
              type="submit"
              form="form-protesto-retorno"
              loading={retornoMutation.isPending}
            >
              Processar retorno
            </Button>
          </>
        ) : (
          <>
            <Button variant="secondary" onClick={fechar} disabled={remessaMutation.isPending}>
              Cancelar
            </Button>
            <Button
              variant="primary"
              type="submit"
              form="form-protesto-remessa"
              loading={remessaMutation.isPending}
            >
              Gerar remessa
            </Button>
          </>
        )
      }
    >
      {remessa ? (
        <>
          <Alert variant="success" title="Remessa gerada">
            Remessa enviada ao {remessa.identificadorCra}. Ao receber o arquivo de retorno do
            cartório, registre a ocorrência abaixo.
          </Alert>
          <form id="form-protesto-retorno" className="br-form" onSubmit={processarRetorno} noValidate>
            <FormField label="Ocorrência do cartório" required error={erro}>
              {({ id, describedBy, invalid }) => (
                <Select
                  id={id}
                  aria-describedby={describedBy}
                  invalid={invalid}
                  options={OCORRENCIA_OPCOES}
                  placeholder="Selecione a ocorrência"
                  value={ocorrencia}
                  onChange={(e) => setOcorrencia(e.target.value as OcorrenciaProtesto)}
                />
              )}
            </FormField>
            <FormField label="Data do retorno" required>
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  type="date"
                  aria-describedby={describedBy}
                  invalid={invalid}
                  value={dataRetorno}
                  onChange={(e) => setDataRetorno(e.target.value)}
                />
              )}
            </FormField>
            <FormField label="Protocolo do cartório" help="Opcional.">
              {({ id, describedBy }) => (
                <Input
                  id={id}
                  aria-describedby={describedBy}
                  value={protocolo}
                  onChange={(e) => setProtocolo(e.target.value)}
                  placeholder="Protocolo informado pelo cartório"
                />
              )}
            </FormField>
          </form>
        </>
      ) : (
        <form id="form-protesto-remessa" className="br-form" onSubmit={gerarRemessa} noValidate>
          <Alert variant="info" title="Remessa de protesto (CRA estadual)">
            O protesto extrajudicial (Lei 9.492/97) exige CDA já emitida. O arquivo de remessa é
            gerado no leiaute do CRA e enviado ao cartório; depois, registre o retorno aqui.
          </Alert>
          <FormField label="Data de geração da remessa" required error={erro}>
            {({ id, describedBy, invalid }) => (
              <Input
                id={id}
                type="date"
                aria-describedby={describedBy}
                invalid={invalid}
                value={dataGeracao}
                onChange={(e) => setDataGeracao(e.target.value)}
              />
            )}
          </FormField>
        </form>
      )}
    </Modal>
  );
}
