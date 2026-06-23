// FormRow — alinha um campo de formulário e uma AÇÃO pareada (ex.: busca + "Consultar").
//
// Conserta o "Consultar torto" na RAIZ: o problema clássico é a ação alinhar pelo
// topo do bloco (label + input) e "subir" junto com o label. Aqui o campo e a ação
// dividem uma linha flex alinhada por baixo (flex-end), e a coluna da ação carrega um
// SPACER invisível com a altura da linha do <label> — então o botão fica exatamente
// na mesma linha do input, independente de o par ter label ou não. Zero `mb-3` chutado.
//
// Uso:
//   <FormRow acao={<Button variant="primary">Consultar</Button>}>
//     <FormField label="Inscrição"> {(p) => <Input {...p} />} </FormField>
//   </FormRow>
import type { ReactNode } from 'react';

export interface FormRowProps {
  /** O campo (normalmente um <FormField>). Ocupa o espaço disponível. */
  children: ReactNode;
  /** A ação pareada ao campo (ex.: botão "Consultar"). Alinha ao input. */
  acao?: ReactNode;
  /**
   * A ação acompanha um campo SEM label visível? Então remove o spacer do label
   * (default `false`: assume que o campo do par tem label, o caso comum).
   */
  acaoSemLabel?: boolean;
  className?: string;
}

export function FormRow({ children, acao, acaoSemLabel = false, className }: FormRowProps) {
  return (
    <div className={`tg-form-row${className ? ` ${className}` : ''}`}>
      <div className="tg-form-row-campo">{children}</div>
      {acao != null && (
        <div className={`tg-form-row-acao${acaoSemLabel ? ' tg-sem-label' : ''}`}>{acao}</div>
      )}
    </div>
  );
}
