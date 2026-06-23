// Formulário de cadastro/edição de Estabelecimento (CNES) em Modal. Quando recebe um
// `estabelecimento`, opera em modo EDIÇÃO (PUT — CNES imutável); senão, CADASTRO (POST).
// Padrão-ouro: mutation + validação por campo + Toast + mapeamento de ProblemDetails.
// Ação gated por "saude.gerenciar" na página que o abre.
import { useEffect, useState } from 'react';
import type { FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import { Button, FormField, Input, Modal, Select, useToast } from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import { useAtualizarEstabelecimento, useCadastrarEstabelecimento } from './api';
import type {
  AtualizarEstabelecimentoInput,
  CadastrarEstabelecimentoInput,
  EstabelecimentoDetalhe,
} from './api';
import { opcoesTipoEstabelecimento } from './saude.helpers';

export interface EstabelecimentoFormModalProps {
  open: boolean;
  onClose: () => void;
  /** Quando presente, o modal opera em modo edição (PUT). */
  estabelecimento?: EstabelecimentoDetalhe | null;
}

interface FormErrors {
  cnes?: string;
  nome?: string;
  tipo?: string;
  logradouro?: string;
  uf?: string;
}

const CAMPOS_VALIDOS: Record<keyof FormErrors, true> = {
  cnes: true,
  nome: true,
  tipo: true,
  logradouro: true,
  uf: true,
};

export function EstabelecimentoFormModal({
  open,
  onClose,
  estabelecimento,
}: EstabelecimentoFormModalProps) {
  const toast = useToast();
  const navigate = useNavigate();
  const edicao = Boolean(estabelecimento);
  const cadastrar = useCadastrarEstabelecimento();
  const atualizar = useAtualizarEstabelecimento(estabelecimento?.id ?? '');
  const mutacaoPendente = cadastrar.isPending || atualizar.isPending;

  const [cnes, setCnes] = useState('');
  const [nome, setNome] = useState('');
  const [tipo, setTipo] = useState('Ubs');
  const [logradouro, setLogradouro] = useState('');
  const [numero, setNumero] = useState('');
  const [bairro, setBairro] = useState('');
  const [municipio, setMunicipio] = useState('');
  const [uf, setUf] = useState('');
  const [cep, setCep] = useState('');
  const [errors, setErrors] = useState<FormErrors>({});

  // Pré-carrega os campos na abertura em modo edição.
  useEffect(() => {
    if (open && estabelecimento) {
      setCnes(estabelecimento.cnes);
      setNome(estabelecimento.nome);
      setTipo(estabelecimento.tipo);
      setLogradouro(estabelecimento.logradouro);
      setNumero(estabelecimento.numero);
      setBairro(estabelecimento.bairro);
      setMunicipio(estabelecimento.municipio);
      setUf(estabelecimento.uf);
      setCep(estabelecimento.cep);
      setErrors({});
    }
  }, [open, estabelecimento]);

  function validar(): FormErrors {
    const next: FormErrors = {};
    if (!edicao && cnes.trim().length !== 7) next.cnes = 'O CNES deve ter 7 dígitos.';
    if (nome.trim() === '') next.nome = 'Informe o nome do estabelecimento.';
    if (tipo.trim() === '') next.tipo = 'Selecione o tipo.';
    if (logradouro.trim() === '') next.logradouro = 'Informe o logradouro.';
    if (uf.trim().length !== 2) next.uf = 'A UF deve ter 2 caracteres.';
    return next;
  }

  function fechar(): void {
    setErrors({});
    onClose();
  }

  function aplicarErroApi(error: unknown): void {
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
      error instanceof ApiError ? error.userMessage : 'Não foi possível salvar o estabelecimento.',
    );
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const validacao = validar();
    setErrors(validacao);
    if (Object.keys(validacao).length > 0) return;

    const endereco = {
      logradouro: logradouro.trim(),
      numero: numero.trim(),
      bairro: bairro.trim(),
      municipio: municipio.trim(),
      uf: uf.trim().toUpperCase(),
      cep: cep.replace(/\D/g, ''),
    };

    if (edicao && estabelecimento) {
      const input: AtualizarEstabelecimentoInput = { nome: nome.trim(), tipo, endereco };
      atualizar.mutate(input, {
        onSuccess: () => {
          toast.success('Estabelecimento atualizado com sucesso.', 'Sucesso');
          fechar();
        },
        onError: aplicarErroApi,
      });
      return;
    }

    const input: CadastrarEstabelecimentoInput = { cnes: cnes.trim(), nome: nome.trim(), tipo, endereco };
    cadastrar.mutate(input, {
      onSuccess: (id) => {
        toast.success('Estabelecimento cadastrado com sucesso.', 'Sucesso');
        fechar();
        navigate(`/saude/estabelecimentos/${id}`);
      },
      onError: aplicarErroApi,
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title={edicao ? 'Editar estabelecimento' : 'Cadastrar estabelecimento (CNES)'}
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutacaoPendente}>
            Cancelar
          </Button>
          <Button
            variant="primary"
            type="submit"
            form="form-estabelecimento"
            loading={mutacaoPendente}
          >
            {edicao ? 'Salvar' : 'Cadastrar'}
          </Button>
        </>
      }
    >
      <form id="form-estabelecimento" className="br-form" onSubmit={submeter} noValidate>
        <fieldset className="mb-3">
          <legend className="text-up-01 text-semi-bold">Identificação</legend>

          <FormField
            label="Código CNES"
            required
            error={errors.cnes}
            help={edicao ? 'O CNES é imutável.' : '7 dígitos.'}
          >
            {({ id, describedBy, invalid }) => (
              <Input
                id={id}
                aria-describedby={describedBy}
                invalid={invalid}
                inputMode="numeric"
                maxLength={7}
                value={cnes}
                disabled={edicao}
                onChange={(e) => setCnes(e.target.value.replace(/\D/g, ''))}
                placeholder="0000000"
              />
            )}
          </FormField>

          <FormField label="Nome do estabelecimento" required error={errors.nome}>
            {({ id, describedBy, invalid }) => (
              <Input
                id={id}
                aria-describedby={describedBy}
                invalid={invalid}
                maxLength={200}
                value={nome}
                onChange={(e) => setNome(e.target.value)}
              />
            )}
          </FormField>

          <FormField label="Tipo" required error={errors.tipo}>
            {({ id }) => (
              <Select
                id={id}
                options={opcoesTipoEstabelecimento}
                value={tipo}
                onChange={(e) => setTipo(e.target.value)}
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
