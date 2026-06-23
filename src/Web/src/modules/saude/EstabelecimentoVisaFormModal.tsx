// Cadastro de Estabelecimento sujeito à VISA (CadastrarEstabelecimentoFiscalizavelCommand):
// documento (CNPJ/CPF), razão social, ramo, risco e endereço. Modo CADASTRO (POST). Ação
// gated por "saude.vigilancia.gerenciar" na página que o abre. Padrão-ouro: mutation +
// validação por campo + Toast + mapeamento de ProblemDetails.
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Button, FormField, Input, Modal, Select, useToast } from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import { useCadastrarEstabelecimentoVisa } from './vigilancia.api';
import type {
  CadastrarEstabelecimentoVisaInput,
  GrauRiscoSanitario,
  RamoVisa,
} from './vigilancia.api';
import { opcoesRamo, opcoesRisco } from './vigilancia.helpers';

interface FormErrors {
  documento?: string;
  razaoSocial?: string;
  logradouro?: string;
  uf?: string;
}

const CAMPOS_VALIDOS: Record<keyof FormErrors, true> = {
  documento: true,
  razaoSocial: true,
  logradouro: true,
  uf: true,
};

export interface EstabelecimentoVisaFormModalProps {
  open: boolean;
  onClose: () => void;
}

export function EstabelecimentoVisaFormModal({ open, onClose }: EstabelecimentoVisaFormModalProps) {
  const toast = useToast();
  const cadastrar = useCadastrarEstabelecimentoVisa();

  const [documento, setDocumento] = useState('');
  const [razaoSocial, setRazaoSocial] = useState('');
  const [ramo, setRamo] = useState<RamoVisa>('Alimentacao');
  const [risco, setRisco] = useState<GrauRiscoSanitario>('Baixo');
  const [logradouro, setLogradouro] = useState('');
  const [bairro, setBairro] = useState('');
  const [municipio, setMunicipio] = useState('');
  const [uf, setUf] = useState('');
  const [cep, setCep] = useState('');
  const [errors, setErrors] = useState<FormErrors>({});

  function fechar(): void {
    setErrors({});
    onClose();
  }

  function validar(): FormErrors {
    const next: FormErrors = {};
    const doc = documento.replace(/\D/g, '');
    if (doc.length !== 11 && doc.length !== 14) next.documento = 'Informe um CPF (11) ou CNPJ (14) válido.';
    if (razaoSocial.trim() === '') next.razaoSocial = 'Informe a razão social/nome.';
    if (logradouro.trim() === '') next.logradouro = 'Informe o logradouro.';
    if (uf.trim().length !== 2) next.uf = 'A UF deve ter 2 caracteres.';
    return next;
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

    const input: CadastrarEstabelecimentoVisaInput = {
      documento: documento.replace(/\D/g, ''),
      razaoSocial: razaoSocial.trim(),
      ramo,
      risco,
      endereco: {
        logradouro: logradouro.trim(),
        bairro: bairro.trim(),
        municipio: municipio.trim(),
        uf: uf.trim().toUpperCase(),
        cep: cep.replace(/\D/g, ''),
      },
    };

    cadastrar.mutate(input, {
      onSuccess: () => {
        toast.success('Estabelecimento cadastrado com sucesso.', 'Sucesso');
        fechar();
      },
      onError: aplicarErroApi,
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Cadastrar estabelecimento fiscalizável"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={cadastrar.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-estab-visa" loading={cadastrar.isPending}>
            Cadastrar
          </Button>
        </>
      }
    >
      <form id="form-estab-visa" className="br-form" onSubmit={submeter} noValidate>
        <fieldset className="mb-3">
          <legend className="text-up-01 text-semi-bold">Identificação</legend>

          <FormField label="CPF/CNPJ do responsável" required error={errors.documento}>
            {({ id, describedBy, invalid }) => (
              <Input
                id={id}
                aria-describedby={describedBy}
                invalid={invalid}
                inputMode="numeric"
                maxLength={18}
                value={documento}
                onChange={(e) => setDocumento(e.target.value)}
                placeholder="Somente dígitos"
              />
            )}
          </FormField>

          <FormField label="Razão social / nome" required error={errors.razaoSocial}>
            {({ id, describedBy, invalid }) => (
              <Input
                id={id}
                aria-describedby={describedBy}
                invalid={invalid}
                maxLength={200}
                value={razaoSocial}
                onChange={(e) => setRazaoSocial(e.target.value)}
              />
            )}
          </FormField>

          <div className="row">
            <div className="col-sm-6">
              <FormField label="Ramo de atividade" required>
                {({ id }) => (
                  <Select
                    id={id}
                    options={opcoesRamo}
                    value={ramo}
                    onChange={(e) => setRamo(e.target.value as RamoVisa)}
                  />
                )}
              </FormField>
            </div>
            <div className="col-sm-6">
              <FormField label="Grau de risco" required help="Risco I dispensa licenciamento prévio.">
                {({ id, describedBy }) => (
                  <Select
                    id={id}
                    aria-describedby={describedBy}
                    options={opcoesRisco}
                    value={risco}
                    onChange={(e) => setRisco(e.target.value as GrauRiscoSanitario)}
                  />
                )}
              </FormField>
            </div>
          </div>
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

          <FormField label="Bairro">
            {({ id }) => <Input id={id} value={bairro} onChange={(e) => setBairro(e.target.value)} />}
          </FormField>

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
