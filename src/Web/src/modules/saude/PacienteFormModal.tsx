// Formulário de cadastro de Paciente em Modal (foco preso). Padrão-ouro: mutation +
// validação por campo (FormField/aria-describedby) + Toast de sucesso/erro e mapeamento
// dos erros de validação do backend (ProblemDetails.errors).
import { useState } from 'react';
import type { FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import { Button, FormField, Input, Modal, Select, useToast } from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import { useCadastrarPaciente } from './api';
import type { CadastrarPacienteInput } from './api';
import { opcoesSexo, paraSexo } from './saude.helpers';

export interface PacienteFormModalProps {
  open: boolean;
  onClose: () => void;
}

interface FormErrors {
  cns?: string;
  nome?: string;
  dataNascimento?: string;
  cpf?: string;
  logradouro?: string;
  uf?: string;
}

const CAMPOS_VALIDOS: Record<keyof FormErrors, true> = {
  cns: true,
  nome: true,
  dataNascimento: true,
  cpf: true,
  logradouro: true,
  uf: true,
};

export function PacienteFormModal({ open, onClose }: PacienteFormModalProps) {
  const toast = useToast();
  const navigate = useNavigate();
  const mutation = useCadastrarPaciente();

  const [cns, setCns] = useState('');
  const [nome, setNome] = useState('');
  const [nomeSocial, setNomeSocial] = useState('');
  const [dataNascimento, setDataNascimento] = useState('');
  const [sexo, setSexo] = useState('1');
  const [cpf, setCpf] = useState('');
  const [logradouro, setLogradouro] = useState('');
  const [numero, setNumero] = useState('');
  const [bairro, setBairro] = useState('');
  const [municipio, setMunicipio] = useState('');
  const [uf, setUf] = useState('');
  const [cep, setCep] = useState('');
  const [errors, setErrors] = useState<FormErrors>({});

  function validar(): FormErrors {
    const next: FormErrors = {};
    if (cns.trim().length !== 15) next.cns = 'O CNS deve ter 15 dígitos.';
    if (nome.trim() === '') next.nome = 'Informe o nome civil do paciente.';
    if (dataNascimento.trim() === '') next.dataNascimento = 'Informe a data de nascimento.';
    else if (dataNascimento > new Date().toISOString().slice(0, 10))
      next.dataNascimento = 'A data de nascimento não pode ser futura.';
    if (cpf.trim() !== '' && cpf.replace(/\D/g, '').length !== 11) next.cpf = 'CPF inválido.';
    if (logradouro.trim() === '') next.logradouro = 'Informe o logradouro.';
    if (uf.trim().length !== 2) next.uf = 'A UF deve ter 2 caracteres.';
    return next;
  }

  function fechar(): void {
    setErrors({});
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const validacao = validar();
    setErrors(validacao);
    if (Object.keys(validacao).length > 0) return;

    const input: CadastrarPacienteInput = {
      cns: cns.trim(),
      identificacao: {
        nome: nome.trim(),
        dataNascimento,
        sexo: paraSexo(sexo),
        nomeSocial: nomeSocial.trim() || null,
        cpf: cpf.replace(/\D/g, '') || null,
      },
      endereco: {
        logradouro: logradouro.trim(),
        numero: numero.trim(),
        bairro: bairro.trim(),
        municipio: municipio.trim(),
        uf: uf.trim().toUpperCase(),
        cep: cep.replace(/\D/g, ''),
      },
    };

    mutation.mutate(input, {
      onSuccess: (id) => {
        toast.success('Paciente cadastrado com sucesso.', 'Sucesso');
        fechar();
        navigate(`/saude/pacientes/${id}`);
      },
      onError: (error) => {
        if (error instanceof ApiError && Object.keys(error.fieldErrors).length > 0) {
          const mapped: FormErrors = {};
          for (const [field, messages] of Object.entries(error.fieldErrors)) {
            // Campos aninhados do backend (ex.: "Identificacao.Nome") -> última parte.
            const leaf = field.split('.').pop() ?? field;
            const key = (leaf.charAt(0).toLowerCase() + leaf.slice(1)) as keyof FormErrors;
            if (key in CAMPOS_VALIDOS) mapped[key] = messages[0];
          }
          setErrors(mapped);
        }
        toast.error(error instanceof ApiError ? error.userMessage : 'Não foi possível cadastrar o paciente.');
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Cadastrar paciente"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-cadastrar-paciente" loading={mutation.isPending}>
            Cadastrar
          </Button>
        </>
      }
    >
      <form id="form-cadastrar-paciente" className="br-form" onSubmit={submeter} noValidate>
        <fieldset className="mb-3">
          <legend className="text-up-01 text-semi-bold">Identificação</legend>

          <FormField label="Cartão Nacional de Saúde (CNS)" required error={errors.cns}>
            {({ id, describedBy, invalid }) => (
              <Input
                id={id}
                aria-describedby={describedBy}
                invalid={invalid}
                inputMode="numeric"
                maxLength={15}
                value={cns}
                onChange={(e) => setCns(e.target.value.replace(/\D/g, ''))}
                placeholder="000000000000000"
              />
            )}
          </FormField>

          <FormField label="Nome civil" required error={errors.nome}>
            {({ id, describedBy, invalid }) => (
              <Input
                id={id}
                aria-describedby={describedBy}
                invalid={invalid}
                maxLength={120}
                value={nome}
                onChange={(e) => setNome(e.target.value)}
              />
            )}
          </FormField>

          <FormField label="Nome social" help="Opcional.">
            {({ id, describedBy }) => (
              <Input
                id={id}
                aria-describedby={describedBy}
                maxLength={120}
                value={nomeSocial}
                onChange={(e) => setNomeSocial(e.target.value)}
              />
            )}
          </FormField>

          <div className="row">
            <div className="col-sm-6">
              <FormField label="Data de nascimento" required error={errors.dataNascimento}>
                {({ id, describedBy, invalid }) => (
                  <Input
                    id={id}
                    type="date"
                    aria-describedby={describedBy}
                    invalid={invalid}
                    value={dataNascimento}
                    onChange={(e) => setDataNascimento(e.target.value)}
                  />
                )}
              </FormField>
            </div>
            <div className="col-sm-6">
              <FormField label="Sexo" required>
                {({ id }) => (
                  <Select
                    id={id}
                    options={opcoesSexo}
                    value={sexo}
                    onChange={(e) => setSexo(e.target.value)}
                  />
                )}
              </FormField>
            </div>
          </div>

          <FormField label="CPF" help="Opcional." error={errors.cpf}>
            {({ id, describedBy, invalid }) => (
              <Input
                id={id}
                aria-describedby={describedBy}
                invalid={invalid}
                inputMode="numeric"
                maxLength={14}
                value={cpf}
                onChange={(e) => setCpf(e.target.value)}
                placeholder="000.000.000-00"
              />
            )}
          </FormField>
        </fieldset>

        <fieldset>
          <legend className="text-up-01 text-semi-bold">Endereço</legend>

          <FormField label="Logradouro" required error={errors.logradouro}>
            {({ id, describedBy, invalid }) => (
              <Input
                id={id}
                aria-describedby={describedBy}
                invalid={invalid}
                value={logradouro}
                onChange={(e) => setLogradouro(e.target.value)}
              />
            )}
          </FormField>

          <div className="row">
            <div className="col-sm-4">
              <FormField label="Número">
                {({ id }) => <Input id={id} value={numero} onChange={(e) => setNumero(e.target.value)} />}
              </FormField>
            </div>
            <div className="col-sm-8">
              <FormField label="Bairro">
                {({ id }) => <Input id={id} value={bairro} onChange={(e) => setBairro(e.target.value)} />}
              </FormField>
            </div>
          </div>

          <div className="row">
            <div className="col-sm-6">
              <FormField label="Município">
                {({ id }) => (
                  <Input id={id} value={municipio} onChange={(e) => setMunicipio(e.target.value)} />
                )}
              </FormField>
            </div>
            <div className="col-sm-3">
              <FormField label="UF" required error={errors.uf}>
                {({ id, describedBy, invalid }) => (
                  <Input
                    id={id}
                    aria-describedby={describedBy}
                    invalid={invalid}
                    maxLength={2}
                    value={uf}
                    onChange={(e) => setUf(e.target.value.toUpperCase())}
                    placeholder="RS"
                  />
                )}
              </FormField>
            </div>
            <div className="col-sm-3">
              <FormField label="CEP">
                {({ id }) => (
                  <Input
                    id={id}
                    inputMode="numeric"
                    maxLength={9}
                    value={cep}
                    onChange={(e) => setCep(e.target.value)}
                    placeholder="00000-000"
                  />
                )}
              </FormField>
            </div>
          </div>
        </fieldset>
      </form>
    </Modal>
  );
}
