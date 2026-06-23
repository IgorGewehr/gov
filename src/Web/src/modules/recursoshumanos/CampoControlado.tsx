// Campo de formulário controlado (FormField + Input) reutilizável, para reduzir a
// repetição em formulários densos do módulo RecursosHumanos (ex.: rescisão, com muitos
// campos numéricos/data). Mantém o padrão-ouro de acessibilidade (id/describedBy/invalid)
// do gov.br DS, apenas encapsulando o boilerplate FormField → Input.
import type { HTMLInputTypeAttribute } from 'react';
import { FormField, Input } from '../../components/ui';

export interface CampoControladoProps {
  label: string;
  value: string;
  onChange: (valor: string) => void;
  error?: string;
  required?: boolean;
  help?: string;
  type?: HTMLInputTypeAttribute;
  min?: string;
  max?: string;
  step?: string;
  inputMode?: 'numeric' | 'decimal' | 'text';
}

/** Campo controlado (rótulo + input acessível) — encapsula o boilerplate de FormField. */
export function CampoControlado({
  label,
  value,
  onChange,
  error,
  required,
  help,
  type = 'text',
  min,
  max,
  step,
  inputMode,
}: CampoControladoProps) {
  return (
    <FormField label={label} required={required} error={error} help={help}>
      {({ id, describedBy, invalid }) => (
        <Input
          id={id}
          type={type}
          min={min}
          max={max}
          step={step}
          inputMode={inputMode}
          aria-describedby={describedBy}
          invalid={invalid}
          value={value}
          onChange={(e) => onChange(e.target.value)}
        />
      )}
    </FormField>
  );
}
