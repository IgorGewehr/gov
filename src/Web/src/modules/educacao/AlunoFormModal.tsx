// Formulário de CADASTRO de aluno (+ responsáveis) em Modal. Padrão de mutation +
// validação por campo (FormField/aria-describedby) + Toast. Espelha CadastrarAlunoCommand
// (DadosCivisPayload + EnderecoAlunoPayload + Cpf? + ResponsavelPayload[]).
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Button, FormField, Input, Modal, Select, useToast } from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import { useCadastrarAluno } from './aluno.api';
import type { CadastrarAlunoInput, ResponsavelInput, Sexo } from './aluno.api';
import { opcoesSexo } from './educacao.helpers';
import { AlunoResponsaveisEditor } from './AlunoResponsaveisEditor';

export interface AlunoFormModalProps {
  open: boolean;
  onClose: () => void;
}

interface FormErrors {
  nome?: string;
  dataNascimento?: string;
  nomeMae?: string;
  logradouro?: string;
  uf?: string;
}

export function AlunoFormModal({ open, onClose }: AlunoFormModalProps) {
  const toast = useToast();
  const mutation = useCadastrarAluno();

  const [nome, setNome] = useState('');
  const [nomeSocial, setNomeSocial] = useState('');
  const [dataNascimento, setDataNascimento] = useState('');
  const [sexo, setSexo] = useState<Sexo>(1);
  const [nomeMae, setNomeMae] = useState('');
  const [nomePai, setNomePai] = useState('');
  const [cpf, setCpf] = useState('');
  const [logradouro, setLogradouro] = useState('');
  const [numero, setNumero] = useState('');
  const [bairro, setBairro] = useState('');
  const [municipio, setMunicipio] = useState('');
  const [uf, setUf] = useState('');
  const [cep, setCep] = useState('');
  const [responsaveis, setResponsaveis] = useState<ResponsavelInput[]>([]);
  const [errors, setErrors] = useState<FormErrors>({});

  function reiniciar(): void {
    setNome('');
    setNomeSocial('');
    setDataNascimento('');
    setSexo(1);
    setNomeMae('');
    setNomePai('');
    setCpf('');
    setLogradouro('');
    setNumero('');
    setBairro('');
    setMunicipio('');
    setUf('');
    setCep('');
    setResponsaveis([]);
    setErrors({});
  }

  function fechar(): void {
    reiniciar();
    onClose();
  }

  function validar(): FormErrors {
    const next: FormErrors = {};
    if (nome.trim() === '') next.nome = 'Nome do aluno obrigatório.';
    if (dataNascimento.trim() === '') next.dataNascimento = 'Informe a data de nascimento.';
    if (nomeMae.trim() === '') next.nomeMae = 'Nome da mãe obrigatório (EducaCenso).';
    if (logradouro.trim() === '') next.logradouro = 'Logradouro obrigatório.';
    if (uf.trim().length !== 2) next.uf = 'UF deve ter 2 caracteres.';
    return next;
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const validacao = validar();
    setErrors(validacao);
    if (Object.keys(validacao).length > 0) return;

    const input: CadastrarAlunoInput = {
      dadosCivis: {
        nome: nome.trim(),
        dataNascimento,
        sexo,
        nomeMae: nomeMae.trim(),
        nomePai: nomePai.trim() || null,
        nomeSocial: nomeSocial.trim() || null,
      },
      endereco: {
        logradouro: logradouro.trim(),
        numero: numero.trim(),
        bairro: bairro.trim(),
        municipio: municipio.trim(),
        uf: uf.trim().toUpperCase(),
        cep: cep.trim(),
      },
      cpf: cpf.trim() || null,
      responsaveis: responsaveis.map((r) => ({
        ...r,
        nome: r.nome.trim(),
        cpf: r.cpf?.trim() || null,
        telefone: r.telefone?.trim() || null,
      })),
    };

    mutation.mutate(input, {
      onSuccess: () => {
        toast.success('Aluno cadastrado com sucesso.', 'Sucesso');
        fechar();
      },
      onError: (error) => {
        toast.error(
          error instanceof ApiError ? error.userMessage : 'Não foi possível cadastrar o aluno.',
        );
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Cadastrar aluno"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-cadastrar-aluno" loading={mutation.isPending}>
            Cadastrar
          </Button>
        </>
      }
    >
      <form id="form-cadastrar-aluno" className="br-form" onSubmit={submeter} noValidate>
        <div className="row">
          <div className="col-12 col-md-8">
            <FormField label="Nome civil" required error={errors.nome}>
              {({ id, describedBy, invalid }) => (
                <Input id={id} aria-describedby={describedBy} invalid={invalid} value={nome} onChange={(e) => setNome(e.target.value)} />
              )}
            </FormField>
          </div>
          <div className="col-12 col-md-4">
            <FormField label="Nome social" help="Opcional">
              {({ id }) => <Input id={id} value={nomeSocial} onChange={(e) => setNomeSocial(e.target.value)} />}
            </FormField>
          </div>
        </div>

        <div className="row">
          <div className="col-12 col-md-4">
            <FormField label="Data de nascimento" required error={errors.dataNascimento}>
              {({ id, describedBy, invalid }) => (
                <Input id={id} type="date" aria-describedby={describedBy} invalid={invalid} value={dataNascimento} onChange={(e) => setDataNascimento(e.target.value)} />
              )}
            </FormField>
          </div>
          <div className="col-12 col-md-4">
            <FormField label="Sexo" required>
              {({ id }) => (
                <Select id={id} options={opcoesSexo} value={String(sexo)} onChange={(e) => setSexo(Number(e.target.value) as Sexo)} />
              )}
            </FormField>
          </div>
          <div className="col-12 col-md-4">
            <FormField label="CPF do aluno" help="Opcional">
              {({ id }) => <Input id={id} value={cpf} onChange={(e) => setCpf(e.target.value)} placeholder="000.000.000-00" />}
            </FormField>
          </div>
        </div>

        <div className="row">
          <div className="col-12 col-md-6">
            <FormField label="Nome da mãe" required error={errors.nomeMae}>
              {({ id, describedBy, invalid }) => (
                <Input id={id} aria-describedby={describedBy} invalid={invalid} value={nomeMae} onChange={(e) => setNomeMae(e.target.value)} />
              )}
            </FormField>
          </div>
          <div className="col-12 col-md-6">
            <FormField label="Nome do pai" help="Opcional">
              {({ id }) => <Input id={id} value={nomePai} onChange={(e) => setNomePai(e.target.value)} />}
            </FormField>
          </div>
        </div>

        <fieldset className="mt-3">
          <legend className="text-up-01 text-bold">Endereço residencial</legend>
          <div className="row">
            <div className="col-12 col-md-8">
              <FormField label="Logradouro" required error={errors.logradouro}>
                {({ id, describedBy, invalid }) => (
                  <Input id={id} aria-describedby={describedBy} invalid={invalid} value={logradouro} onChange={(e) => setLogradouro(e.target.value)} />
                )}
              </FormField>
            </div>
            <div className="col-12 col-md-4">
              <FormField label="Número/complemento">
                {({ id }) => <Input id={id} value={numero} onChange={(e) => setNumero(e.target.value)} />}
              </FormField>
            </div>
          </div>
          <div className="row">
            <div className="col-12 col-md-5">
              <FormField label="Bairro">
                {({ id }) => <Input id={id} value={bairro} onChange={(e) => setBairro(e.target.value)} />}
              </FormField>
            </div>
            <div className="col-12 col-md-4">
              <FormField label="Município">
                {({ id }) => <Input id={id} value={municipio} onChange={(e) => setMunicipio(e.target.value)} />}
              </FormField>
            </div>
            <div className="col-6 col-md-1">
              <FormField label="UF" required error={errors.uf}>
                {({ id, describedBy, invalid }) => (
                  <Input id={id} aria-describedby={describedBy} invalid={invalid} maxLength={2} value={uf} onChange={(e) => setUf(e.target.value)} />
                )}
              </FormField>
            </div>
            <div className="col-6 col-md-2">
              <FormField label="CEP">
                {({ id }) => <Input id={id} value={cep} onChange={(e) => setCep(e.target.value)} placeholder="00000-000" />}
              </FormField>
            </div>
          </div>
        </fieldset>

        <AlunoResponsaveisEditor responsaveis={responsaveis} onChange={setResponsaveis} />
      </form>
    </Modal>
  );
}
