// Input tipado. Pode ser usado solto ou dentro de FormField (recebe id/aria via props).
import { forwardRef } from 'react';
import type { InputHTMLAttributes } from 'react';

export type InputProps = InputHTMLAttributes<HTMLInputElement> & {
  invalid?: boolean;
};

export const Input = forwardRef<HTMLInputElement, InputProps>(function Input(
  { invalid, type = 'text', ...rest },
  ref,
) {
  return <input ref={ref} type={type} aria-invalid={invalid || undefined} {...rest} />;
});
