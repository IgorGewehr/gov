// Modal genérico das TRANSIÇÕES textuais do e-SIC interno (LAI). Cobre responder,
// prorrogar, indeferir e decidir recurso — todas pedem um texto obrigatório e
// (opcionalmente) um campo extra (resultado do recurso / referência de anexo).
// Mantido genérico para não duplicar o boilerplate de modal/validação/toast.
import { useState } from 'react';
import type { FormEvent, ReactNode } from 'react';
import { Alert, Button, FormField, Modal, Textarea, useToast } from '../../components/ui';
import { ApiError } from '../../api/problemDetails';

export interface EsicAcaoModalProps {
  open: boolean;
  onClose: () => void;
  /** Título do modal (ex.: "Responder pedido"). */
  titulo: string;
  /** Alerta/contexto legal exibido no topo (fundamento da ação). */
  contextoLegal: ReactNode;
  /** Rótulo do campo de texto principal (ex.: "Texto da resposta"). */
  rotuloTexto: string;
  /** Ajuda do campo de texto. */
  ajudaTexto?: string;
  /** Limite de caracteres do texto (espelha o validador do backend). */
  maxTexto: number;
  /** Rótulo do botão de confirmação (ex.: "Responder"). */
  rotuloConfirmar: string;
  /** Campo extra opcional renderizado ACIMA do texto (ex.: <Select> de resultado). */
  campoExtra?: ReactNode;
  /** Em andamento (mutation.isPending). */
  enviando: boolean;
  /** Submete a ação com o texto informado. Deve disparar a mutation. */
  onSubmit: (texto: string) => void;
  /** True quando o campo extra (se houver) está válido — bloqueia o submit. */
  extraValido?: boolean;
}

export function EsicAcaoModal({
  open,
  onClose,
  titulo,
  contextoLegal,
  rotuloTexto,
  ajudaTexto,
  maxTexto,
  rotuloConfirmar,
  campoExtra,
  enviando,
  onSubmit,
  extraValido = true,
}: EsicAcaoModalProps) {
  const [texto, setTexto] = useState('');
  const [erro, setErro] = useState<string | undefined>();

  function fechar(): void {
    setTexto('');
    setErro(undefined);
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const valor = texto.trim();
    if (valor === '') {
      setErro('Campo obrigatório.');
      return;
    }
    if (valor.length > maxTexto) {
      setErro(`Máximo de ${maxTexto} caracteres.`);
      return;
    }
    if (!extraValido) return;
    setErro(undefined);
    onSubmit(valor);
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title={titulo}
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={enviando}>
            Cancelar
          </Button>
          <Button
            variant="primary"
            type="submit"
            form="form-esic-acao"
            loading={enviando}
            disabled={!extraValido}
          >
            {rotuloConfirmar}
          </Button>
        </>
      }
    >
      <Alert variant="info" title={titulo}>
        {contextoLegal}
      </Alert>

      <form id="form-esic-acao" className="br-form mt-3" onSubmit={submeter} noValidate>
        {campoExtra}
        <FormField label={rotuloTexto} required help={ajudaTexto} error={erro}>
          {({ id, describedBy, invalid }) => (
            <Textarea
              id={id}
              rows={6}
              maxLength={maxTexto}
              aria-describedby={describedBy}
              invalid={invalid}
              value={texto}
              onChange={(e) => setTexto(e.target.value)}
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}

/**
 * Helper de tratamento de erro padrão das ações e-SIC: toast com a mensagem do
 * ProblemDetails (ou fallback). Reutilizado por todas as transições.
 */
export function useEsicErroToast() {
  const toast = useToast();
  return (error: unknown, fallback: string) =>
    toast.error(error instanceof ApiError ? error.userMessage : fallback);
}
