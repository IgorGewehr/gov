// Formulário de inclusão de IMÓVEL BENEFICIADO numa obra de Contribuição de Melhoria
// (AdicionarImovelBeneficiadoCommand — CTN art. 81): imóvel + proprietário +
// valorização individual apurada (limite individual da contribuição). Só durante a
// fase de edital. Acessível (Modal).
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Alert, Button, FormField, Input, Modal, useToast } from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import { useAdicionarImovelBeneficiado } from './melhoria.api';

export interface AdicionarImovelMelhoriaModalProps {
  open: boolean;
  obraId: string;
  onClose: () => void;
}

export function AdicionarImovelMelhoriaModal({ open, obraId, onClose }: AdicionarImovelMelhoriaModalProps) {
  const toast = useToast();
  const mutation = useAdicionarImovelBeneficiado();

  const [imovelId, setImovelId] = useState('');
  const [proprietarioId, setProprietarioId] = useState('');
  const [valorizacao, setValorizacao] = useState('');
  const [erro, setErro] = useState<string | null>(null);

  function fechar(): void {
    setImovelId('');
    setProprietarioId('');
    setValorizacao('');
    setErro(null);
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    if (imovelId.trim() === '' || proprietarioId.trim() === '') {
      setErro('Informe o imóvel e o proprietário.');
      return;
    }
    const valor = Number((valorizacao || '0').replace(',', '.'));
    if (!(valor > 0)) {
      setErro('A valorização individual deve ser maior que zero.');
      return;
    }
    setErro(null);

    mutation.mutate(
      {
        obraId,
        input: {
          imovelId: imovelId.trim(),
          proprietarioId: proprietarioId.trim(),
          valorizacaoIndividual: valor,
        },
      },
      {
        onSuccess: () => {
          toast.success('Imóvel beneficiado adicionado à obra.', 'Sucesso');
          // Limpa para permitir incluir o próximo imóvel sem fechar.
          setImovelId('');
          setProprietarioId('');
          setValorizacao('');
        },
        onError: (error) =>
          toast.error(
            error instanceof ApiError ? error.userMessage : 'Não foi possível adicionar o imóvel.',
          ),
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Adicionar imóvel beneficiado"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Concluir
          </Button>
          <Button variant="primary" type="submit" form="form-imovel-melhoria" loading={mutation.isPending}>
            Adicionar
          </Button>
        </>
      }
    >
      <form id="form-imovel-melhoria" className="br-form" onSubmit={submeter} noValidate>
        {erro && (
          <Alert variant="danger" title="Verifique os dados">
            {erro}
          </Alert>
        )}

        <FormField label="Identificador do imóvel" required>
          {({ id, describedBy, invalid }) => (
            <Input id={id} aria-describedby={describedBy} invalid={invalid} value={imovelId} onChange={(e) => setImovelId(e.target.value)} placeholder="00000000-0000-0000-0000-000000000000" />
          )}
        </FormField>

        <FormField label="Identificador do proprietário" required>
          {({ id, describedBy, invalid }) => (
            <Input id={id} aria-describedby={describedBy} invalid={invalid} value={proprietarioId} onChange={(e) => setProprietarioId(e.target.value)} placeholder="00000000-0000-0000-0000-000000000000" />
          )}
        </FormField>

        <FormField label="Valorização individual (R$)" required help="Limite individual da contribuição (CTN art. 81).">
          {({ id, describedBy, invalid }) => (
            <Input id={id} type="number" min="0" step="0.01" inputMode="decimal" aria-describedby={describedBy} invalid={invalid} value={valorizacao} onChange={(e) => setValorizacao(e.target.value)} placeholder="0,00" />
          )}
        </FormField>
      </form>
    </Modal>
  );
}
