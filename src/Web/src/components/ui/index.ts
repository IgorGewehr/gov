// Biblioteca de UI do Tensorroot.Gov — wrappers React tipados sobre o gov.br DS.
// Ponto de entrada único: importe sempre de '@/components/ui' (ou caminho relativo).
export { Button } from './Button';
export type { ButtonProps, ButtonVariant, ButtonSize } from './Button';

export { Input } from './Input';
export type { InputProps } from './Input';

export { Textarea } from './Textarea';
export type { TextareaProps } from './Textarea';

export { Select } from './Select';
export type { SelectProps, SelectOption } from './Select';

export { FormField } from './FormField';
export type { FormFieldProps, FormFieldRenderProps } from './FormField';

export { FormRow } from './FormRow';
export type { FormRowProps } from './FormRow';

export { Card } from './Card';
export type { CardProps, CardAccent } from './Card';

export { Toolbar } from './Toolbar';
export type { ToolbarProps, ToolbarGroupProps } from './Toolbar';

export { DataTable } from './DataTable';
export type { DataTableProps, Column } from './DataTable';

export { Modal } from './Modal';
export type { ModalProps } from './Modal';

export { Alert } from './Alert';
export type { AlertProps, AlertVariant } from './Alert';

export { Spinner } from './Spinner';
export type { SpinnerProps } from './Spinner';

export { EmptyState } from './EmptyState';
export type { EmptyStateProps } from './EmptyState';

export { PageHeader } from './PageHeader';
export type { PageHeaderProps } from './PageHeader';

export { Metrica, MetricaGrade } from './Metrica';
export type { MetricaProps } from './Metrica';

export { CardSecao } from './CardSecao';
export type { CardSecaoProps } from './CardSecao';

export { Tag } from './Tag';
export type { TagProps, TagVariant } from './Tag';

export { QueryState, errorMessage } from './QueryState';
export type { QueryStateProps } from './QueryState';

export { SubNav } from './SubNav';
export type { SubNavProps, SubNavItem } from './SubNav';

export { ToastProvider } from './toast/ToastProvider';
export { useToast } from './toast/useToast';
export type { ToastApi, ToastOptions } from './toast/ToastContext';
