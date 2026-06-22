// Campos compartilhados do EducaCenso (Endereço + Infraestrutura), reutilizados
// pelo credenciamento (EscolaFormModal) e pela atualização de dados (EscolaAcaoModais).
// Centraliza validação e montagem dos value objects Endereco/Infraestrutura.
import { FormField, Input } from '../../components/ui';
import type { Endereco, Infraestrutura } from './api';

/** Estado controlado dos campos do Censo (strings da UI). */
export interface CamposCensoState {
  logradouro: string;
  municipio: string;
  uf: string;
  cep: string;
  latitude: string;
  longitude: string;
  numeroSalas: string;
  numeroDependencias: string;
  possuiAcessibilidade: boolean;
}

/** Erros por campo do Censo. */
export interface CamposCensoErrors {
  logradouro?: string;
  municipio?: string;
  uf?: string;
  cep?: string;
  latitude?: string;
  longitude?: string;
  numeroSalas?: string;
  numeroDependencias?: string;
}

/** Conjunto de chaves de campo conhecidas (para mapear ProblemDetails). */
export const CAMPOS_CENSO_CHAVES: Record<keyof CamposCensoErrors, true> = {
  logradouro: true,
  municipio: true,
  uf: true,
  cep: true,
  latitude: true,
  longitude: true,
  numeroSalas: true,
  numeroDependencias: true,
};

/** Estado inicial vazio. */
export function censoVazio(): CamposCensoState {
  return {
    logradouro: '',
    municipio: '',
    uf: '',
    cep: '',
    latitude: '',
    longitude: '',
    numeroSalas: '',
    numeroDependencias: '',
    possuiAcessibilidade: false,
  };
}

function inteiroNaoNegativo(valor: string): number | null {
  const n = Number(valor);
  if (valor.trim() === '' || !Number.isInteger(n) || n < 0) return null;
  return n;
}

/** Valida os campos do Censo, retornando os erros encontrados. */
export function validarCenso(estado: CamposCensoState): CamposCensoErrors {
  const next: CamposCensoErrors = {};
  if (estado.logradouro.trim() === '') next.logradouro = 'Informe o logradouro.';
  if (estado.municipio.trim() === '') next.municipio = 'Informe o município.';
  if (estado.uf.trim().length !== 2) next.uf = 'Informe a UF (2 letras).';
  if (estado.cep.trim() === '') next.cep = 'Informe o CEP.';
  const lat = Number(estado.latitude);
  if (estado.latitude.trim() === '' || Number.isNaN(lat) || lat < -90 || lat > 90)
    next.latitude = 'Informe uma latitude entre -90 e 90.';
  const lng = Number(estado.longitude);
  if (estado.longitude.trim() === '' || Number.isNaN(lng) || lng < -180 || lng > 180)
    next.longitude = 'Informe uma longitude entre -180 e 180.';
  if (inteiroNaoNegativo(estado.numeroSalas) === null)
    next.numeroSalas = 'Informe um número de salas válido.';
  if (inteiroNaoNegativo(estado.numeroDependencias) === null)
    next.numeroDependencias = 'Informe um número de dependências válido.';
  return next;
}

/** Monta os value objects Endereco/Infraestrutura a partir do estado validado. */
export function montarCenso(estado: CamposCensoState): {
  endereco: Endereco;
  infraestrutura: Infraestrutura;
} {
  return {
    endereco: {
      logradouro: estado.logradouro.trim(),
      municipio: estado.municipio.trim(),
      uf: estado.uf.trim().toUpperCase(),
      cep: estado.cep.trim(),
      latitude: Number(estado.latitude),
      longitude: Number(estado.longitude),
    },
    infraestrutura: {
      numeroSalas: Number(estado.numeroSalas),
      numeroDependencias: Number(estado.numeroDependencias),
      possuiAcessibilidade: estado.possuiAcessibilidade,
    },
  };
}

export interface EscolaCamposCensoProps {
  estado: CamposCensoState;
  errors: CamposCensoErrors;
  onChange: (parcial: Partial<CamposCensoState>) => void;
  /** Sufixo para garantir ids únicos do checkbox quando há mais de um formulário. */
  idSufixo: string;
}

/** Grupo de campos Endereço + Infraestrutura (acessível, gov.br DS). */
export function EscolaCamposCenso({ estado, errors, onChange, idSufixo }: EscolaCamposCensoProps) {
  return (
    <>
      <fieldset className="mt-3">
        <legend className="text-up-01 text-semi-bold">Endereço</legend>
        <FormField label="Logradouro" required error={errors.logradouro}>
          {({ id, describedBy, invalid }) => (
            <Input
              id={id}
              aria-describedby={describedBy}
              invalid={invalid}
              value={estado.logradouro}
              onChange={(e) => onChange({ logradouro: e.target.value })}
              placeholder="Rua, avenida, número"
            />
          )}
        </FormField>
        <div className="row">
          <div className="col-sm-6">
            <FormField label="Município" required error={errors.municipio}>
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  aria-describedby={describedBy}
                  invalid={invalid}
                  value={estado.municipio}
                  onChange={(e) => onChange({ municipio: e.target.value })}
                />
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
                  value={estado.uf}
                  onChange={(e) => onChange({ uf: e.target.value })}
                  maxLength={2}
                  placeholder="RS"
                />
              )}
            </FormField>
          </div>
          <div className="col-sm-3">
            <FormField label="CEP" required error={errors.cep}>
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  aria-describedby={describedBy}
                  invalid={invalid}
                  value={estado.cep}
                  onChange={(e) => onChange({ cep: e.target.value })}
                  inputMode="numeric"
                  placeholder="99000000"
                />
              )}
            </FormField>
          </div>
        </div>
        <div className="row">
          <div className="col-sm-6">
            <FormField label="Latitude" required error={errors.latitude}>
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  type="number"
                  step="any"
                  inputMode="decimal"
                  aria-describedby={describedBy}
                  invalid={invalid}
                  value={estado.latitude}
                  onChange={(e) => onChange({ latitude: e.target.value })}
                  placeholder="-27.6308"
                />
              )}
            </FormField>
          </div>
          <div className="col-sm-6">
            <FormField label="Longitude" required error={errors.longitude}>
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  type="number"
                  step="any"
                  inputMode="decimal"
                  aria-describedby={describedBy}
                  invalid={invalid}
                  value={estado.longitude}
                  onChange={(e) => onChange({ longitude: e.target.value })}
                  placeholder="-51.8050"
                />
              )}
            </FormField>
          </div>
        </div>
      </fieldset>

      <fieldset className="mt-3">
        <legend className="text-up-01 text-semi-bold">Infraestrutura</legend>
        <div className="row align-items-end">
          <div className="col-sm-4">
            <FormField label="Nº de salas" required error={errors.numeroSalas}>
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  type="number"
                  min="0"
                  step="1"
                  inputMode="numeric"
                  aria-describedby={describedBy}
                  invalid={invalid}
                  value={estado.numeroSalas}
                  onChange={(e) => onChange({ numeroSalas: e.target.value })}
                />
              )}
            </FormField>
          </div>
          <div className="col-sm-4">
            <FormField label="Nº de dependências" required error={errors.numeroDependencias}>
              {({ id, describedBy, invalid }) => (
                <Input
                  id={id}
                  type="number"
                  min="0"
                  step="1"
                  inputMode="numeric"
                  aria-describedby={describedBy}
                  invalid={invalid}
                  value={estado.numeroDependencias}
                  onChange={(e) => onChange({ numeroDependencias: e.target.value })}
                />
              )}
            </FormField>
          </div>
          <div className="col-sm-4 mb-3">
            <div className="br-checkbox">
              <input
                id={`escola-acessibilidade-${idSufixo}`}
                type="checkbox"
                checked={estado.possuiAcessibilidade}
                onChange={(e) => onChange({ possuiAcessibilidade: e.target.checked })}
              />
              <label htmlFor={`escola-acessibilidade-${idSufixo}`}>Possui acessibilidade</label>
            </div>
          </div>
        </div>
      </fieldset>
    </>
  );
}
