// Modal de AÇÃO "Emitir CDA" (command EmitirCda) sobre uma Dívida Ativa inscrita.
// Espelha o EmitirCdaPayload real: NumeroCda + DataBaseEncargos obrigatórios; Domicílio,
// Co-responsáveis e Processo administrativo opcionais. O domínio (LEF art. 2º §5º I–VI /
// CTN art. 202) RECUSA a emissão se faltar requisito legal — exibimos a lista de requisitos
// e, em sucesso, a CDA emitida (CdaEmitidaDto). WIRED a uma mutation TanStack Query.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Alert, Button, FormField, Input, Modal, useToast } from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import { formatarData, formatarMoeda } from '../../i18n/format';
import { useEmitirCda } from './api';

const NUMERO_CDA_MAX = 40;

export interface EmitirCdaModalProps {
  open: boolean;
  onClose: () => void;
  dividaAtivaId: string;
  /** Contribuinte cuja lista de dívidas deve ser invalidada após a emissão. */
  contribuinteIdParaInvalidar?: string;
}

/** Hoje no formato DateOnly ('YYYY-MM-DD') para o default da data-base dos encargos. */
function hojeIso(): string {
  return new Date().toISOString().slice(0, 10);
}

export function EmitirCdaModal({
  open,
  onClose,
  dividaAtivaId,
  contribuinteIdParaInvalidar,
}: EmitirCdaModalProps) {
  const toast = useToast();
  const mutation = useEmitirCda(contribuinteIdParaInvalidar);
  const [numeroCda, setNumeroCda] = useState('');
  const [dataBaseEncargos, setDataBaseEncargos] = useState(hojeIso());
  const [domicilioDevedor, setDomicilioDevedor] = useState('');
  const [coResponsaveis, setCoResponsaveis] = useState('');
  const [processoAdministrativo, setProcessoAdministrativo] = useState('');
  const [erro, setErro] = useState<string | undefined>();

  function fechar(): void {
    setNumeroCda('');
    setDataBaseEncargos(hojeIso());
    setDomicilioDevedor('');
    setCoResponsaveis('');
    setProcessoAdministrativo('');
    setErro(undefined);
    mutation.reset();
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const valor = numeroCda.trim();
    if (valor === '') {
      setErro('Informe o número da CDA.');
      return;
    }
    if (valor.length > NUMERO_CDA_MAX) {
      setErro(`O número da CDA deve ter no máximo ${NUMERO_CDA_MAX} caracteres.`);
      return;
    }
    if (dataBaseEncargos === '') {
      setErro('Informe a data-base dos encargos.');
      return;
    }
    setErro(undefined);
    mutation.mutate(
      {
        dividaAtivaId,
        input: {
          numeroCda: valor,
          dataBaseEncargos,
          domicilioDevedor: domicilioDevedor.trim() || null,
          coResponsaveis: coResponsaveis.trim() || null,
          processoAdministrativo: processoAdministrativo.trim() || null,
        },
      },
      {
        onSuccess: () => {
          toast.success('CDA emitida.', 'Sucesso');
        },
        onError: (error) => {
          if (error instanceof ApiError && error.fieldErrors.NumeroCda) {
            setErro(error.fieldErrors.NumeroCda[0]);
          }
          toast.error(error instanceof ApiError ? error.userMessage : 'Não foi possível emitir a CDA.');
        },
      },
    );
  }

  const cda = mutation.data;

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Emitir Certidão de Dívida Ativa (CDA)"
      footer={
        cda ? (
          <Button variant="primary" onClick={fechar}>
            Concluir
          </Button>
        ) : (
          <>
            <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
              Cancelar
            </Button>
            <Button variant="primary" type="submit" form="form-emitir-cda" loading={mutation.isPending}>
              Emitir CDA
            </Button>
          </>
        )
      }
    >
      {cda ? (
        <>
          <Alert variant="success" title="CDA emitida">
            Certidão de Dívida Ativa nº {cda.numero} emitida com todos os requisitos legais.
          </Alert>
          <dl className="mb-0">
            <dt>Devedor (inc. I)</dt>
            <dd>{cda.nomeDevedor}</dd>
            <dt>Valor originário (inc. II)</dt>
            <dd>{formatarMoeda(cda.valorOriginario)}</dd>
            <dt>Origem e natureza (inc. III)</dt>
            <dd>{cda.origemNatureza}</dd>
            <dt>Fundamento legal (inc. III)</dt>
            <dd>{cda.fundamentoLegal}</dd>
            <dt>Inscrição (inc. V)</dt>
            <dd>
              nº {cda.numeroInscricao} em {formatarData(cda.dataInscricao)}
            </dd>
          </dl>
        </>
      ) : (
        <form id="form-emitir-cda" className="br-form" onSubmit={submeter} noValidate>
          <Alert variant="info" title="Requisitos legais da CDA (LEF art. 2º §5º / CTN art. 202)">
            A emissão é RECUSADA se faltar qualquer requisito obrigatório: nome do devedor (I), valor
            originário (II), origem/natureza e fundamento legal (III), forma de cálculo dos encargos
            (IV), data e número da inscrição (V) e, quando houver, o processo administrativo (VI).
            Devedor, valor, origem e inscrição vêm do título já cadastrado.
          </Alert>
          <FormField
            label="Número da CDA"
            required
            error={erro}
            help={`Máx. ${NUMERO_CDA_MAX} caracteres.`}
          >
            {({ id, describedBy, invalid }) => (
              <Input
                id={id}
                aria-describedby={describedBy}
                invalid={invalid}
                maxLength={NUMERO_CDA_MAX}
                value={numeroCda}
                onChange={(e) => setNumeroCda(e.target.value)}
                placeholder="Ex.: CDA-2026-000123"
              />
            )}
          </FormField>
          <FormField
            label="Data-base dos encargos (inc. IV)"
            required
            help="Data para descrever a forma de cálculo de multa, juros e correção."
          >
            {({ id, describedBy, invalid }) => (
              <Input
                id={id}
                type="date"
                aria-describedby={describedBy}
                invalid={invalid}
                value={dataBaseEncargos}
                onChange={(e) => setDataBaseEncargos(e.target.value)}
              />
            )}
          </FormField>
          <FormField label="Domicílio do devedor (inc. I)" help="Opcional.">
            {({ id, describedBy }) => (
              <Input
                id={id}
                aria-describedby={describedBy}
                value={domicilioDevedor}
                onChange={(e) => setDomicilioDevedor(e.target.value)}
                placeholder="Endereço do devedor"
              />
            )}
          </FormField>
          <FormField label="Co-responsáveis (inc. I)" help="Opcional.">
            {({ id, describedBy }) => (
              <Input
                id={id}
                aria-describedby={describedBy}
                value={coResponsaveis}
                onChange={(e) => setCoResponsaveis(e.target.value)}
                placeholder="Sócios/co-obrigados"
              />
            )}
          </FormField>
          <FormField label="Processo administrativo (inc. VI)" help="Opcional.">
            {({ id, describedBy }) => (
              <Input
                id={id}
                aria-describedby={describedBy}
                value={processoAdministrativo}
                onChange={(e) => setProcessoAdministrativo(e.target.value)}
                placeholder="Nº do processo administrativo"
              />
            )}
          </FormField>
        </form>
      )}
    </Modal>
  );
}
