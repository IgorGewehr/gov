// Query 6.2 — Verificar integridade do documento (SHA-256; I-12).
// Modal independente, wired ao hook useVerificarIntegridade (useQuery sob demanda).
// Extraido de DocumentoAcaoModais para manter cada arquivo < 300 linhas (CLAUDE.md §13).
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Alert, Button, FormField, Input, Modal, Spinner } from '../../../components/ui';
import { useVerificarIntegridade } from './documento.api';
import type { DocumentoResumo, VerificarIntegridadeInput } from './documento.api';

const HASH_SHA256 = /^[0-9a-fA-F]{64}$/;

interface VerificarIntegridadeModalProps {
  open: boolean;
  onClose: () => void;
  documento: DocumentoResumo;
}

export function VerificarIntegridadeModal({ open, onClose, documento }: VerificarIntegridadeModalProps) {
  const [hashRecalculado, setHashRecalculado] = useState('');
  const [consulta, setConsulta] = useState<VerificarIntegridadeInput | null>(null);
  const [erro, setErro] = useState<string | undefined>();
  const query = useVerificarIntegridade(consulta);

  function fechar(): void {
    setHashRecalculado('');
    setConsulta(null);
    setErro(undefined);
    onClose();
  }

  function verificar(event: FormEvent): void {
    event.preventDefault();
    const valor = hashRecalculado.trim();
    if (!HASH_SHA256.test(valor)) {
      setErro('Informe o hash SHA-256 recalculado (64 caracteres hexadecimais).');
      setConsulta(null);
      return;
    }
    setErro(undefined);
    setConsulta({ documentoId: documento.id, hashRecalculado: valor.toLowerCase() });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Verificar integridade do documento"
      footer={
        <Button variant="secondary" onClick={fechar}>
          Fechar
        </Button>
      }
    >
      <form className="br-form" onSubmit={verificar} noValidate>
        <Alert variant="info">
          Informe o hash SHA-256 recalculado a partir do conteudo. O sistema o compara com o hash armazenado;
          divergencia indica integridade comprometida (I-12).
        </Alert>

        <p className="text-down-01 text-gray-60">
          Hash armazenado: <code>{documento.hash}</code>
        </p>

        <FormField label="Hash SHA-256 recalculado" required error={erro}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={hashRecalculado}
              onChange={(e) => setHashRecalculado(e.target.value)}
              spellCheck={false}
              autoComplete="off"
              placeholder="64 caracteres hexadecimais"
            />
          )}
        </FormField>

        <div className="mt-3">
          <Button variant="primary" type="submit" loading={query.isFetching}>
            Verificar
          </Button>
        </div>

        <div className="mt-3" aria-live="polite">
          {query.isFetching && (
            <p>
              <Spinner /> Verificando integridade…
            </p>
          )}
          {query.isError && !query.isFetching && (
            <Alert variant="danger">Nao foi possivel verificar a integridade. Tente novamente.</Alert>
          )}
          {query.data && !query.isFetching && (
            <Alert variant={query.data.integro ? 'success' : 'danger'}>
              {query.data.integro
                ? 'Integro: o hash recalculado confere com o armazenado.'
                : 'Integridade comprometida: o hash recalculado NAO confere com o armazenado.'}
            </Alert>
          )}
        </div>
      </form>
    </Modal>
  );
}
