// Formulario de cadastro de Norma em Modal: tipo, numero, ano, ementa, texto
// integral e data de publicacao. Mutation + validacao por campo + Toast.
import { useEffect, useState } from 'react';
import type { FormEvent } from 'react';
import { Button, FormField, Input, Modal, Select, Textarea, useToast } from '../../components/ui';
import { TIPOS_NORMA } from './legislativo.shared';
import { tratarErroCampos, mensagemErro } from './legislativoAcao.shared';
import { useCriarNorma, type NormaInput } from './normas.api';

export interface NormaFormModalProps {
  open: boolean;
  onClose: () => void;
}

interface FormErrors {
  numero?: string;
  ano?: string;
  ementa?: string;
  textoArticulado?: string;
  dataPromulgacao?: string;
}

const CAMPOS: Record<string, number> = {
  numero: 1,
  ano: 1,
  ementa: 1,
  textoArticulado: 1,
  dataPromulgacao: 1,
};

export function NormaFormModal({ open, onClose }: NormaFormModalProps) {
  const toast = useToast();
  const criar = useCriarNorma();

  const [tipo, setTipo] = useState<string>(String(TIPOS_NORMA[0].value));
  const [numero, setNumero] = useState('');
  const [ano, setAno] = useState('');
  const [ementa, setEmenta] = useState('');
  const [textoArticulado, setTextoArticulado] = useState('');
  const [dataPromulgacao, setDataPromulgacao] = useState('');
  const [errors, setErrors] = useState<FormErrors>({});

  useEffect(() => {
    if (!open) return;
    setTipo(String(TIPOS_NORMA[0].value));
    setNumero('');
    setAno('');
    setEmenta('');
    setTextoArticulado('');
    setDataPromulgacao('');
    setErrors({});
  }, [open]);

  function validar(): FormErrors {
    const next: FormErrors = {};
    if (numero.trim() === '' || !Number.isInteger(Number(numero)) || Number(numero) <= 0)
      next.numero = 'Informe um número válido (inteiro).';
    if (ano.trim() === '' || Number.isNaN(Number(ano))) next.ano = 'Informe um ano válido.';
    if (ementa.trim() === '') next.ementa = 'Informe a ementa.';
    if (textoArticulado.trim() === '') next.textoArticulado = 'Informe o texto articulado.';
    if (dataPromulgacao === '') next.dataPromulgacao = 'Informe a data de promulgação.';
    return next;
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const validacao = validar();
    setErrors(validacao);
    if (Object.keys(validacao).length > 0) return;

    const input: NormaInput = {
      tipo: Number(tipo),
      numero: Number(numero),
      ano: Number(ano),
      ementa: ementa.trim(),
      textoArticulado: textoArticulado.trim(),
      dataPromulgacao,
    };

    criar.mutate(input, {
      onSuccess: () => {
        toast.success('Norma cadastrada.', 'Sucesso');
        onClose();
      },
      onError: (error) => {
        setErrors(tratarErroCampos(error, CAMPOS));
        toast.error(mensagemErro(error, 'Não foi possível cadastrar a norma.'));
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={onClose}
      title="Cadastrar norma"
      footer={
        <>
          <Button variant="secondary" onClick={onClose} disabled={criar.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-norma" loading={criar.isPending}>
            Cadastrar
          </Button>
        </>
      }
    >
      <form id="form-norma" className="br-form" onSubmit={submeter} noValidate>
        <FormField label="Tipo">
          {({ id, describedBy, invalid }) => (
            <Select
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={tipo}
              onChange={(e) => setTipo(e.target.value)}
              options={TIPOS_NORMA.map((t) => ({ value: String(t.value), label: t.label }))}
            />
          )}
        </FormField>

        <div className="row">
          <div className="col-sm-6">
            <FormField label="Número" required error={errors.numero}>
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  aria-describedby={describedBy}
                  invalid={invalid}
                  type="number"
                  value={numero}
                  onChange={(e) => setNumero(e.target.value)}
                  placeholder="Ex.: 1234"
                />
              )}
            </FormField>
          </div>
          <div className="col-sm-6">
            <FormField label="Ano" required error={errors.ano}>
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  aria-describedby={describedBy}
                  invalid={invalid}
                  type="number"
                  value={ano}
                  onChange={(e) => setAno(e.target.value)}
                  placeholder="Ex.: 2026"
                />
              )}
            </FormField>
          </div>
        </div>

        <FormField label="Ementa" required error={errors.ementa}>
          {({ id, describedBy, invalid }) => (
            <Textarea
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={ementa}
              onChange={(e) => setEmenta(e.target.value)}
              rows={2}
            />
          )}
        </FormField>

        <FormField label="Texto articulado" required error={errors.textoArticulado}>
          {({ id, describedBy, invalid }) => (
            <Textarea
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={textoArticulado}
              onChange={(e) => setTextoArticulado(e.target.value)}
              rows={6}
            />
          )}
        </FormField>

        <FormField label="Data de promulgação" required error={errors.dataPromulgacao}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              type="date"
              value={dataPromulgacao}
              onChange={(e) => setDataPromulgacao(e.target.value)}
            />
          )}
        </FormField>
      </form>
    </Modal>
  );
}
