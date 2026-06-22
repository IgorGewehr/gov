// Formulário de ATUALIZAÇÃO do cadastro civil/endereço do paciente (PUT /pacientes/{id})
// em Modal. Pré-carrega os dados conhecidos (do resumo) e envia identificação + endereço.
// Ação gated por "saude.gerenciar" na página que o abre.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Button, FormField, Input, Modal, Select, useToast } from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import { useAtualizarCadastroPaciente } from './api';
import type { AtualizarCadastroPacienteInput, PacienteResumo } from './api';
import { opcoesSexo, paraSexo } from './saude.helpers';

export interface PacienteEditModalProps {
  open: boolean;
  onClose: () => void;
  pacienteId: string;
  /** Resumo conhecido para pré-preencher os campos de identificação. */
  paciente?: PacienteResumo | null;
}

interface FormErrors {
  nome?: string;
  dataNascimento?: string;
  cpf?: string;
  logradouro?: string;
  uf?: string;
}

const CAMPOS_VALIDOS: Record<keyof FormErrors, true> = {
  nome: true,
  dataNascimento: true,
  cpf: true,
  logradouro: true,
  uf: true,
};

function sexoInicial(sexo?: string): string {
  if (sexo === 'Feminino') return '1';
  if (sexo === 'Masculino') return '2';
  return '9';
}

export function PacienteEditModal({ open, onClose, pacienteId, paciente }: PacienteEditModalProps) {
  const toast = useToast();
  const mutation = useAtualizarCadastroPaciente(pacienteId);

  const [nome, setNome] = useState(paciente?.nome ?? '');
  const [nomeSocial, setNomeSocial] = useState(paciente?.nomeSocial ?? '');
  const [dataNascimento, setDataNascimento] = useState(paciente?.dataNascimento ?? '');
  const [sexo, setSexo] = useState(sexoInicial(paciente?.sexo));
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

    const input: AtualizarCadastroPacienteInput = {
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
      onSuccess: () => {
        toast.success('Cadastro atualizado com sucesso.', 'Sucesso');
        fechar();
      },
      onError: (error) => {
        if (error instanceof ApiError && Object.keys(error.fieldErrors).length > 0) {
          const mapped: FormErrors = {};
          for (const [field, messages] of Object.entries(error.fieldErrors)) {
            const leaf = field.split('.').pop() ?? field;
            const key = (leaf.charAt(0).toLowerCase() + leaf.slice(1)) as keyof FormErrors;
            if (key in CAMPOS_VALIDOS) mapped[key] = messages[0];
          }
          setErrors(mapped);
        }
        toast.error(
          error instanceof ApiError ? error.userMessage : 'Não foi possível atualizar o cadastro.',
        );
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Atualizar cadastro do paciente"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-atualizar-paciente" loading={mutation.isPending}>
            Salvar
          </Button>
        </>
      }
    >
      <form id="form-atualizar-paciente" className="br-form" onSubmit={submeter} noValidate>
        <fieldset className="mb-3">
          <legend className="text-up-01 text-semi-bold">Identificação</legend>

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
                  <Select id={id} options={opcoesSexo} value={sexo} onChange={(e) => setSexo(e.target.value)} />
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
