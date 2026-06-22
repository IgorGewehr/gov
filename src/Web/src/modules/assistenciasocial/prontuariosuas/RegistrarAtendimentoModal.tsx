// Modal de acao: RegistrarAtendimento -> RegistrarAtendimentoCommand (mantem Aberto).
// Validacao por campo (FormField/aria-describedby), mapeamento de ProblemDetails.errors,
// Toast de sucesso/erro e estado de loading. gov.br DS + WCAG AA.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Alert, Button, FormField, Input, Modal, Select, Textarea, useToast } from '../../../components/ui';
import { ApiError } from '../../../api/problemDetails';
import { opcoesServico, useRegistrarAtendimento } from './prontuariosuas.api';
import type { RegistrarAtendimentoInput, TipoServico } from './prontuariosuas.api';
import { mapearErros } from './acaoModalShared';
import type { ModalAcaoBaseProps } from './acaoModalShared';

interface AtendimentoErrors {
  servico?: string;
  dataAtendimento?: string;
  descricao?: string;
  profissionalId?: string;
}

export function RegistrarAtendimentoModal({ open, onClose, prontuarioId }: ModalAcaoBaseProps) {
  const toast = useToast();
  const mutation = useRegistrarAtendimento(prontuarioId);

  const [servico, setServico] = useState<TipoServico | ''>('');
  const [dataAtendimento, setDataAtendimento] = useState('');
  const [descricao, setDescricao] = useState('');
  const [profissionalId, setProfissionalId] = useState('');
  const [errors, setErrors] = useState<AtendimentoErrors>({});

  function validar(): AtendimentoErrors {
    const next: AtendimentoErrors = {};
    if (servico === '') next.servico = 'Serviço (PAIF/PAEFI/SCFV) é obrigatório.';
    if (dataAtendimento.trim() === '') next.dataAtendimento = 'Data do atendimento é obrigatória.';
    if (descricao.trim() === '') next.descricao = 'Descrição do atendimento é obrigatória.';
    else if (descricao.length > 4000) next.descricao = 'A descrição deve ter no máximo 4000 caracteres.';
    if (profissionalId.trim() === '') next.profissionalId = 'Profissional responsável é obrigatório.';
    return next;
  }

  function fechar(): void {
    setErrors({});
    setServico('');
    setDataAtendimento('');
    setDescricao('');
    setProfissionalId('');
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const validacao = validar();
    setErrors(validacao);
    if (Object.keys(validacao).length > 0 || servico === '') return;

    const input: RegistrarAtendimentoInput = {
      servico,
      dataAtendimento,
      descricao: descricao.trim(),
      profissionalId: profissionalId.trim(),
    };

    mutation.mutate(input, {
      onSuccess: () => {
        toast.success('Atendimento registrado.', 'Sucesso');
        fechar();
      },
      onError: (error) => {
        setErrors(
          mapearErros(error, { servico: 1, dataAtendimento: 1, descricao: 1, profissionalId: 1 }),
        );
        toast.error(
          error instanceof ApiError ? error.userMessage : 'Não foi possível registrar o atendimento.',
        );
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Registrar atendimento"
      size="large"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-registrar-atendimento" loading={mutation.isPending}>
            Registrar
          </Button>
        </>
      }
    >
      <div className="mb-3">
        <Alert variant="info">
          Conteúdo sigiloso (LGPD art. 11). PAIF só em CRAS; PAEFI só em CREAS — serviço incompatível
          com a unidade será rejeitado.
        </Alert>
      </div>
      <form id="form-registrar-atendimento" className="br-form" onSubmit={submeter} noValidate>
        <FormField label="Serviço" required error={errors.servico}>
          {({ id, describedBy, invalid }) => (
            <Select
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              placeholder="Selecione o serviço…"
              options={opcoesServico}
              value={servico}
              onChange={(e) => setServico(e.target.value as TipoServico)}
            />
          )}
        </FormField>

        <FormField label="Data do atendimento" required error={errors.dataAtendimento}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              type="date"
              aria-describedby={describedBy}
              invalid={invalid}
              value={dataAtendimento}
              onChange={(e) => setDataAtendimento(e.target.value)}
            />
          )}
        </FormField>

        <FormField
          label="Descrição (sigilosa)"
          required
          error={errors.descricao}
          help="Máximo de 4000 caracteres. Conteúdo protegido por sigilo profissional."
        >
          {({ id, describedBy, invalid }) => (
            <Textarea
              id={id}
              rows={5}
              maxLength={4000}
              aria-describedby={describedBy}
              aria-invalid={invalid || undefined}
              value={descricao}
              onChange={(e) => setDescricao(e.target.value)}
            />
          )}
        </FormField>

        <FormField label="Profissional responsável" required error={errors.profissionalId}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={profissionalId}
              onChange={(e) => setProfissionalId(e.target.value)}
              placeholder="00000000-0000-0000-0000-000000000000"
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}
