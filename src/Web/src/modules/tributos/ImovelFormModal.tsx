// Formulário de CADASTRO de imóvel (command CadastrarImovel) em Modal. Vincula o
// imóvel ao contribuinte e registra inscrição imobiliária, endereço/zona, áreas
// (terreno/construída), uso e padrão construtivo e ano. Enums vão como INT;
// mapeia ProblemDetails.fieldErrors por campo. Acessível (foco preso no Modal).
import { useState } from 'react';
import type { FormEvent } from 'react';
import { Button, FormField, Input, Modal, Select, useToast } from '../../components/ui';
import type { SelectOption } from '../../components/ui';
import { ApiError } from '../../api/problemDetails';
import {
  PADRAO_CONSTRUTIVO_VALOR,
  USO_IMOVEL_VALOR,
  useCadastrarImovel,
} from './iptu.api';
import type { CadastrarImovelInput, PadraoConstrutivo, UsoImovel } from './iptu.api';
import { PADRAO_CONSTRUTIVO_LABEL, USO_IMOVEL_LABEL } from './iptu.helpers';

const USO_OPCOES: SelectOption[] = (
  ['Residencial', 'Comercial', 'Industrial', 'Servicos', 'Territorial'] as UsoImovel[]
).map((u) => ({ value: u, label: USO_IMOVEL_LABEL[u] }));

const PADRAO_OPCOES: SelectOption[] = (
  ['Baixo', 'Normal', 'Alto', 'Luxo'] as PadraoConstrutivo[]
).map((p) => ({ value: p, label: PADRAO_CONSTRUTIVO_LABEL[p] }));

export interface ImovelFormModalProps {
  open: boolean;
  onClose: () => void;
  /** Pré-preenche o contribuinte quando aberto a partir de uma consulta. */
  contribuinteIdInicial?: string;
}

interface FormErrors {
  contribuinteId?: string;
  inscricaoImobiliaria?: string;
  logradouro?: string;
  numero?: string;
  bairro?: string;
  zona?: string;
  uso?: string;
  padrao?: string;
  anoConstrucao?: string;
  areaTerreno?: string;
  areaConstruida?: string;
}

const ANO_MIN = 1800;

export function ImovelFormModal({ open, onClose, contribuinteIdInicial = '' }: ImovelFormModalProps) {
  const toast = useToast();
  const mutation = useCadastrarImovel(contribuinteIdInicial);

  const [contribuinteId, setContribuinteId] = useState(contribuinteIdInicial);
  const [inscricaoImobiliaria, setInscricao] = useState('');
  const [logradouro, setLogradouro] = useState('');
  const [numero, setNumero] = useState('');
  const [bairro, setBairro] = useState('');
  const [zona, setZona] = useState('');
  const [uso, setUso] = useState<UsoImovel | ''>('');
  const [padrao, setPadrao] = useState<PadraoConstrutivo | ''>('');
  const [anoConstrucao, setAno] = useState('');
  const [areaTerreno, setAreaTerreno] = useState('');
  const [areaConstruida, setAreaConstruida] = useState('');
  const [errors, setErrors] = useState<FormErrors>({});

  const territorial = uso === 'Territorial';

  function validar(): FormErrors {
    const next: FormErrors = {};
    if (contribuinteId.trim() === '') next.contribuinteId = 'Informe o identificador do contribuinte.';
    if (inscricaoImobiliaria.trim() === '') next.inscricaoImobiliaria = 'Informe a inscrição imobiliária.';
    if (logradouro.trim() === '') next.logradouro = 'Informe o logradouro.';
    if (numero.trim() === '') next.numero = 'Informe o número.';
    if (bairro.trim() === '') next.bairro = 'Informe o bairro.';
    if (zona.trim() === '') next.zona = 'Informe a zona (deve existir na PGV).';
    if (uso === '') next.uso = 'Selecione o uso predominante.';
    if (padrao === '') next.padrao = 'Selecione o padrão construtivo.';
    const terreno = Number(areaTerreno);
    if (areaTerreno.trim() === '' || Number.isNaN(terreno) || terreno <= 0)
      next.areaTerreno = 'Informe a área do terreno (m²) maior que zero.';
    const construida = Number(areaConstruida);
    if (areaConstruida.trim() === '' || Number.isNaN(construida) || construida < 0)
      next.areaConstruida = 'Informe a área construída (m²); use 0 se territorial.';
    if (anoConstrucao.trim() !== '') {
      const anoNum = Number(anoConstrucao);
      if (!Number.isInteger(anoNum) || anoNum < ANO_MIN)
        next.anoConstrucao = `Informe um ano válido (>= ${ANO_MIN}) ou deixe em branco.`;
    }
    return next;
  }

  function fechar(): void {
    setErrors({});
    setInscricao('');
    setLogradouro('');
    setNumero('');
    setBairro('');
    setZona('');
    setUso('');
    setPadrao('');
    setAno('');
    setAreaTerreno('');
    setAreaConstruida('');
    onClose();
  }

  function submeter(event: FormEvent): void {
    event.preventDefault();
    const validacao = validar();
    setErrors(validacao);
    if (Object.keys(validacao).length > 0 || uso === '' || padrao === '') return;

    const input: CadastrarImovelInput = {
      contribuinteId: contribuinteId.trim(),
      inscricaoImobiliaria: inscricaoImobiliaria.trim(),
      logradouro: logradouro.trim(),
      numero: numero.trim(),
      bairro: bairro.trim(),
      zona: zona.trim(),
      uso: USO_IMOVEL_VALOR[uso],
      padrao: PADRAO_CONSTRUTIVO_VALOR[padrao],
      anoConstrucao: anoConstrucao.trim() === '' ? null : Number(anoConstrucao),
      areaTerreno: Number(areaTerreno),
      areaConstruida: Number(areaConstruida),
    };

    mutation.mutate(input, {
      onSuccess: (resultado) => {
        toast.success(`Imóvel cadastrado (id ${resultado.id}).`, 'Sucesso');
        fechar();
      },
      onError: (error) => {
        if (error instanceof ApiError && Object.keys(error.fieldErrors).length > 0) {
          const mapped: FormErrors = {};
          for (const [field, messages] of Object.entries(error.fieldErrors)) {
            const key = field.charAt(0).toLowerCase() + field.slice(1);
            (mapped as Record<string, string>)[key] = messages[0];
          }
          setErrors((prev) => ({ ...prev, ...mapped }));
        }
        toast.error(error instanceof ApiError ? error.userMessage : 'Não foi possível cadastrar o imóvel.');
      },
    });
  }

  return (
    <Modal
      open={open}
      onClose={fechar}
      title="Cadastrar imóvel"
      size="large"
      footer={
        <>
          <Button variant="secondary" onClick={fechar} disabled={mutation.isPending}>
            Cancelar
          </Button>
          <Button variant="primary" type="submit" form="form-cadastrar-imovel" loading={mutation.isPending}>
            Cadastrar
          </Button>
        </>
      }
    >
      <form id="form-cadastrar-imovel" className="br-form" onSubmit={submeter} noValidate>
        <FormField label="Identificador do contribuinte" required error={errors.contribuinteId}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={contribuinteId}
              onChange={(e) => setContribuinteId(e.target.value)}
              placeholder="00000000-0000-0000-0000-000000000000"
            />
          )}
        </FormField>

        <FormField label="Inscrição imobiliária" required error={errors.inscricaoImobiliaria}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={inscricaoImobiliaria}
              onChange={(e) => setInscricao(e.target.value)}
              placeholder="00.000.000.0000"
            />
          )}
        </FormField>

        <div className="row">
          <div className="col-md-8">
            <FormField label="Logradouro" required error={errors.logradouro}>
              {({ id, describedBy, invalid }) => (
                <Input id={id} aria-describedby={describedBy} invalid={invalid} value={logradouro} onChange={(e) => setLogradouro(e.target.value)} />
              )}
            </FormField>
          </div>
          <div className="col-md-4">
            <FormField label="Número" required error={errors.numero}>
              {({ id, describedBy, invalid }) => (
                <Input id={id} aria-describedby={describedBy} invalid={invalid} value={numero} onChange={(e) => setNumero(e.target.value)} />
              )}
            </FormField>
          </div>
        </div>

        <div className="row">
          <div className="col-md-6">
            <FormField label="Bairro" required error={errors.bairro}>
              {({ id, describedBy, invalid }) => (
                <Input id={id} aria-describedby={describedBy} invalid={invalid} value={bairro} onChange={(e) => setBairro(e.target.value)} />
              )}
            </FormField>
          </div>
          <div className="col-md-6">
            <FormField label="Zona (PGV)" required error={errors.zona} help="Deve existir na Planta de Valores do exercício.">
              {({ id, describedBy, invalid }) => (
                <Input id={id} aria-describedby={describedBy} invalid={invalid} value={zona} onChange={(e) => setZona(e.target.value)} placeholder="Z01" />
              )}
            </FormField>
          </div>
        </div>

        <div className="row">
          <div className="col-md-6">
            <FormField label="Uso predominante" required error={errors.uso}>
              {({ id, describedBy, invalid }) => (
                <Select
                  id={id}
                  aria-describedby={describedBy}
                  invalid={invalid}
                  placeholder="Selecione…"
                  options={USO_OPCOES}
                  value={uso}
                  onChange={(e) => setUso(e.target.value as UsoImovel | '')}
                />
              )}
            </FormField>
          </div>
          <div className="col-md-6">
            <FormField label="Padrão construtivo" required error={errors.padrao}>
              {({ id, describedBy, invalid }) => (
                <Select
                  id={id}
                  aria-describedby={describedBy}
                  invalid={invalid}
                  placeholder="Selecione…"
                  options={PADRAO_OPCOES}
                  value={padrao}
                  onChange={(e) => setPadrao(e.target.value as PadraoConstrutivo | '')}
                />
              )}
            </FormField>
          </div>
        </div>

        <div className="row">
          <div className="col-md-4">
            <FormField label="Área do terreno (m²)" required error={errors.areaTerreno}>
              {({ id, describedBy, invalid }) => (
                <Input id={id} type="number" min="0" step="0.01" inputMode="decimal" aria-describedby={describedBy} invalid={invalid} value={areaTerreno} onChange={(e) => setAreaTerreno(e.target.value)} />
              )}
            </FormField>
          </div>
          <div className="col-md-4">
            <FormField label="Área construída (m²)" required error={errors.areaConstruida} help={territorial ? 'Use 0 para imóvel territorial.' : undefined}>
              {({ id, describedBy, invalid }) => (
                <Input id={id} type="number" min="0" step="0.01" inputMode="decimal" aria-describedby={describedBy} invalid={invalid} value={areaConstruida} onChange={(e) => setAreaConstruida(e.target.value)} />
              )}
            </FormField>
          </div>
          <div className="col-md-4">
            <FormField label="Ano de construção" error={errors.anoConstrucao} help="Opcional.">
              {({ id, describedBy, invalid }) => (
                <Input id={id} type="number" min={ANO_MIN} inputMode="numeric" aria-describedby={describedBy} invalid={invalid} value={anoConstrucao} onChange={(e) => setAno(e.target.value)} />
              )}
            </FormField>
          </div>
        </div>
      </form>
    </Modal>
  );
}
