// Select tipado (nativo <select> — acessível por padrão, sem armadilha de foco).
import { forwardRef } from 'react';
import type { SelectHTMLAttributes } from 'react';

export interface SelectOption {
  value: string;
  label: string;
  disabled?: boolean;
}

export type SelectProps = SelectHTMLAttributes<HTMLSelectElement> & {
  options: SelectOption[];
  /** Texto da opção placeholder (value vazio). */
  placeholder?: string;
  invalid?: boolean;
};

export const Select = forwardRef<HTMLSelectElement, SelectProps>(function Select(
  { options, placeholder, invalid, ...rest },
  ref,
) {
  return (
    <div className="br-select">
      <div className="br-input">
        <select ref={ref} aria-invalid={invalid || undefined} {...rest}>
          {placeholder && (
            <option value="" disabled>
              {placeholder}
            </option>
          )}
          {options.map((opt) => (
            <option key={opt.value} value={opt.value} disabled={opt.disabled}>
              {opt.label}
            </option>
          ))}
        </select>
      </div>
    </div>
  );
});
