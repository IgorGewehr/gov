// Formulario de inclusao de uma materia em uma edicao do Diario em montagem:
// tipo, titulo e conteudo. Mutation + validacao por campo + Toast.
import { useEffect, useState } from 'react';
import type { FormEvent } from 'react';
import { Button, FormField, Input, Modal, Select, Textarea, useToast } from '../../components/ui';
import { TIPOS_MATERIA } from './legislativo.shared';
import { tratarErroCampos, mensagemErro } from './legislativoAcao.shared';
import { useAdicionarMateria, type MateriaInput } from './diario.api';

export interface MateriaFormModalProps {
  open: boolean;
  onClose: () => void;
  /** Identificador da edicao em montagem. */
  edicaoId: string;
}

interface FormErrors {
  titulo?: string;
  conteudo?: string;
}

const CAMPOS: Record<string, number> = { titulo: 1, conteudo: 1 };

export function MateriaFormModal({ open, onClose, edicaoId }: MateriaFormModalProps) {
  const toast = useToast();
  const adicionar = useAdicionarMateria(edicaoId);

  const [tipo, setTipo] = useState<string>(String(TIPOS_MATERIA[0].value));
  const [titulo, setTitulo] = useState('');
  const [conteudo, setConteudo] = useState('');
  const [errors, setErrors] = useState<FormErrors>({});

  useEffect(() => {
    if (!open) return;
    setTipo(String(TIPOS_MATERIA[0].value));
    setTitulo('');
    setConteudo('');
    setErrors({});
  }, [open]);

  function validar(): FormErrors {
    const next: FormErrors = {};
    if (titulo.trim() === '') next.titulo = 'Informe o título da matéria.';
    if (conteudo.trim() === '') next.conteudo = 'Informe o conteúdo da matéria.';
    return next;
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const validacao = validar();
    setErrors(validacao);
    if (Object.keys(validacao).length > 0) return;

    const input: MateriaInput = { tipo: Number(tipo), titulo: titulo.trim(), conteudo: conteudo.trim() };

    adicionar.mutate(input, {
      onSuccess: () => {
        toast.success('Matéria adicionada.', 'Sucesso');
        onClose();
      },
      onError: (error) => {
        setErrors(tratarErroCampos(error, CAMPOS));
        toast.error(mensagemErro(error, 'Não foi possível adicionar a matéria.'));
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={onClose}
      title="Adicionar matéria"
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={adicionar.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-materia" loading={adicionar.isPending}>
            Adicionar
          </Button>
        </>
      }
    >
      <form id="form-materia" className="br-form" onSubmit={submeter} noValidate>
        <FormField label="Tipo">
          {({ id, describedBy, invalid }) => (
            <Select
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={tipo}
              onChange={(e) => setTipo(e.target.value)}
              options={TIPOS_MATERIA.map((t) => ({ value: String(t.value), label: t.label }))}
            />
          )}
        </FormField>

        <FormField label="Título" required error={errors.titulo}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={titulo}
              onChange={(e) => setTitulo(e.target.value)}
            />
          )}
        </FormField>

        <FormField label="Conteúdo" required error={errors.conteudo}>
          {({ id, describedBy, invalid }) => (
            <Textarea
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={conteudo}
              onChange={(e) => setConteudo(e.target.value)}
              rows={6}
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}
