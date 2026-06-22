// DesignarMotorista — vincula condutor ao veículo, rejeitando CNH vencida (I-9).
// WIRED a useDesignarMotorista.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Button, FormField, Input, Modal, useToast } from '../../../../components/ui';
import { hojeIso } from '../veiculo.helpers';
import { useDesignarMotorista } from '../veiculo.api';
import { aplicarErrosBackend } from './acaoModais.shared';
import type { AcaoModalBaseProps } from './acaoModais.shared';

export function DesignarMotoristaModal({
  veiculoId,
  open,
  onClose,
}: Pick<AcaoModalBaseProps, 'veiculoId' | 'open' | 'onClose'>) {
  const toast = useToast();
  const mutation = useDesignarMotorista(veiculoId);
  const hoje = hojeIso();

  const [nome, setNome] = useState('');
  const [cnh, setCnh] = useState('');
  const [categoriaCnh, setCategoriaCnh] = useState('');
  const [validadeCnh, setValidadeCnh] = useState('');
  const [erros, setErros] = useState<Record<string, string>>({});

  function fechar(): void {
    setErros({});
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const next: Record<string, string> = {};
    if (nome.trim() === '') next.nome = 'Nome do motorista obrigatório.';
    else if (nome.trim().length > 150) next.nome = 'Máximo de 150 caracteres.';
    if (cnh.trim() === '') next.cnh = 'CNH obrigatória.';
    if (categoriaCnh.trim() === '') next.categoriaCnh = 'Informe a categoria da CNH.';
    if (validadeCnh === '') next.validadeCnh = 'Informe a validade da CNH.';
    else if (validadeCnh < hoje) next.validadeCnh = 'CNH vencida não pode ser designada (I-9).';
    setErros(next);
    if (Object.keys(next).length > 0) return;

    mutation.mutate(
      { nome: nome.trim(), cnh: cnh.trim(), categoriaCnh: categoriaCnh.trim().toUpperCase(), validadeCnh },
      {
        onSuccess: () => {
          toast.success('Motorista designado.', 'Sucesso');
          fechar();
        },
        onError: (error) => toast.error(aplicarErrosBackend(error, ['nome', 'cnh', 'categoriaCnh', 'validadeCnh'], setErros)),
      },
    );
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Designar motorista"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-motorista" loading={mutation.isPending}>
            Designar
          </Button>
        </>
      }
    >
      <form id="form-motorista" className="br-form" onSubmit={submeter} noValidate>
        <FormField label="Nome" required error={erros.nome}>
          {({ id, describedBy, invalid }) => (
            <Input id={id} aria-describedby={describedBy} invalid={invalid} maxLength={150} value={nome} onChange={(e) => setNome(e.target.value)} />
          )}
        </FormField>
        <div className="row">
          <div className="col-sm-6">
            <FormField label="CNH" required error={erros.cnh}>
              {({ id, describedBy, invalid }) => (
                <Input id={id} inputMode="numeric" aria-describedby={describedBy} invalid={invalid} value={cnh} onChange={(e) => setCnh(e.target.value)} />
              )}
            </FormField>
          </div>
          <div className="col-sm-6">
            <FormField label="Categoria" required error={erros.categoriaCnh}>
              {({ id, describedBy, invalid }) => (
                <Input id={id} aria-describedby={describedBy} invalid={invalid} maxLength={3} value={categoriaCnh} onChange={(e) => setCategoriaCnh(e.target.value.toUpperCase())} placeholder="A, B, C, D, E" />
              )}
            </FormField>
          </div>
        </div>
        <FormField label="Validade da CNH" required error={erros.validadeCnh} help="Deve ser hoje ou data futura (I-9).">
          {({ id, describedBy, invalid }) => (
            <Input id={id} type="date" min={hoje} aria-describedby={describedBy} invalid={invalid} value={validadeCnh} onChange={(e) => setValidadeCnh(e.target.value)} />
          )}
        </FormField>
      </form>
    </Modal>
  );
}
