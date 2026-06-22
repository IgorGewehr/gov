// Modais de deliberacao da Proposicao: Aprovar/Rejeitar (vinculados a uma votacao)
// e Gerar autografo. Extraidos de ProposicaoAcaoModais para manter cada arquivo
// < 300 linhas (CLAUDE.md §13); reexportados la para preservar o import unico.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Button, FormField, Input, Modal, useToast } from '../../components/ui';
import { useAprovarProposicao, useRejeitarProposicao, useGerarAutografo } from './proposicao.api';
import { mensagemErro, tratarErroCampos } from './legislativoAcao.shared';
import type { AcaoModalBaseProps } from './legislativoAcao.shared';

// ---------------------------------------------------------------------------
// DELIBERACAO (Aprovar / Rejeitar) — vinculada a uma votacao
// ---------------------------------------------------------------------------

export function DeliberacaoModal({
  open,
  onClose,
  id,
  sentido,
}: AcaoModalBaseProps & { sentido: 'aprovar' | 'rejeitar' }) {
  const toast = useToast();
  const aprovar = useAprovarProposicao(id);
  const rejeitar = useRejeitarProposicao(id);
  const mutation = sentido === 'aprovar' ? aprovar : rejeitar;
  const [votacaoId, setVotacaoId] = useState('');
  const [errors, setErrors] = useState<{ votacaoId?: string }>({});
  const titulo = sentido === 'aprovar' ? 'Aprovar proposição' : 'Rejeitar proposição';

  function fechar(): void {
    setErrors({});
    setVotacaoId('');
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    if (votacaoId.trim() === '') {
      setErrors({ votacaoId: 'Informe o identificador da votação.' });
      return;
    }
    setErrors({});
    mutation.mutate(
      { votacaoId: votacaoId.trim() },
      {
        onSuccess: () => {
          toast.success(`Proposição ${sentido === 'aprovar' ? 'aprovada' : 'rejeitada'}.`, 'Sucesso');
          fechar();
        },
        onError: (error) => {
          setErrors(tratarErroCampos(error, { votacaoId: 1 }));
          toast.error(mensagemErro(error, `Não foi possível ${sentido} a proposição.`));
        },
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title={titulo}
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button
            variant={sentido === 'aprovar' ? 'primary' : 'danger'}
            type="submit"
            form="form-deliberacao"
            loading={mutation.isPending}
          >
            {sentido === 'aprovar' ? 'Aprovar' : 'Rejeitar'}
          </Button>
        </>
      }
    >
      <form id="form-deliberacao" className="br-form" onSubmit={submeter} noValidate>
        <FormField
          label="Identificador da votação"
          required
          error={errors.votacaoId}
          help="Votação encerrada que fundamenta a deliberação."
        >
          {({ id: fid, describedBy, invalid }) => (
            <Input
              id={fid}
              aria-describedby={describedBy}
              invalid={invalid}
              value={votacaoId}
              onChange={(e) => setVotacaoId(e.target.value)}
              placeholder="00000000-0000-0000-0000-000000000000"
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}

// ---------------------------------------------------------------------------
// AUTOGRAFO
// ---------------------------------------------------------------------------

export function AutografoModal({ open, onClose, id }: AcaoModalBaseProps) {
  const toast = useToast();
  const mutation = useGerarAutografo(id);
  const [numero, setNumero] = useState('');
  const [errors, setErrors] = useState<{ numeroAutografo?: string }>({});

  function fechar(): void {
    setErrors({});
    setNumero('');
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    if (numero.trim() === '') {
      setErrors({ numeroAutografo: 'Informe o número do autógrafo.' });
      return;
    }
    setErrors({});
    mutation.mutate(
      { numeroAutografo: numero.trim() },
      {
        onSuccess: () => {
          toast.success('Autógrafo gerado.', 'Sucesso');
          fechar();
        },
        onError: (error) => {
          setErrors(tratarErroCampos(error, { numeroAutografo: 1 }));
          toast.error(mensagemErro(error, 'Não foi possível gerar o autógrafo.'));
        },
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Gerar autógrafo"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-autografo" loading={mutation.isPending}>
            Gerar
          </Button>
        </>
      }
    >
      <form id="form-autografo" className="br-form" onSubmit={submeter} noValidate>
        <FormField label="Número do autógrafo" required error={errors.numeroAutografo}>
          {({ id: fid, describedBy, invalid }) => (
            <Input
              id={fid}
              aria-describedby={describedBy}
              invalid={invalid}
              value={numero}
              onChange={(e) => setNumero(e.target.value)}
              placeholder="Ex.: 012/2026"
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}
