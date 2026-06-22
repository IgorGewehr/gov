// Modal acessível (DS §3/§5): role=dialog, aria-modal, foco preso, ESC fecha,
// restaura foco ao elemento anterior e bloqueia scroll do body. Renderiza via portal.
import { useCallback, useEffect, useId, useRef } from 'react';
import { createPortal } from 'react-dom';
import type { ReactNode } from 'react';
import { Button } from './Button';

export interface ModalProps {
  open: boolean;
  onClose: () => void;
  title: ReactNode;
  children: ReactNode;
  /** Rodapé com ações (ex.: Cancelar/Confirmar). */
  footer?: ReactNode;
  /** Tamanho gov.br: small | medium (padrão) | large. */
  size?: 'small' | 'medium' | 'large';
}

const FOCUSABLE =
  'a[href], button:not([disabled]), textarea:not([disabled]), input:not([disabled]), select:not([disabled]), [tabindex]:not([tabindex="-1"])';

export function Modal({ open, onClose, title, children, footer, size = 'medium' }: ModalProps) {
  const dialogRef = useRef<HTMLDivElement>(null);
  const previousFocus = useRef<HTMLElement | null>(null);
  const titleId = useId();

  const handleKeyDown = useCallback(
    (event: React.KeyboardEvent<HTMLDivElement>) => {
      if (event.key === 'Escape') {
        event.preventDefault();
        onClose();
        return;
      }
      if (event.key !== 'Tab') return;

      const dialog = dialogRef.current;
      if (!dialog) return;
      const focusables = Array.from(dialog.querySelectorAll<HTMLElement>(FOCUSABLE));
      if (focusables.length === 0) {
        event.preventDefault();
        return;
      }
      const first = focusables[0];
      const last = focusables[focusables.length - 1];
      const active = document.activeElement;

      if (event.shiftKey && active === first) {
        event.preventDefault();
        last.focus();
      } else if (!event.shiftKey && active === last) {
        event.preventDefault();
        first.focus();
      }
    },
    [onClose],
  );

  useEffect(() => {
    if (!open) return;
    previousFocus.current = document.activeElement as HTMLElement | null;
    const { body } = document;
    const previousOverflow = body.style.overflow;
    body.style.overflow = 'hidden';

    // Foco inicial no primeiro elemento focável (ou no diálogo).
    const dialog = dialogRef.current;
    const firstFocusable = dialog?.querySelector<HTMLElement>(FOCUSABLE);
    (firstFocusable ?? dialog)?.focus();

    return () => {
      body.style.overflow = previousOverflow;
      previousFocus.current?.focus();
    };
  }, [open]);

  if (!open) return null;

  return createPortal(
    // eslint-disable-next-line jsx-a11y/no-static-element-interactions
    <div className="br-scrim-util foco active" onKeyDown={handleKeyDown}>
      <div
        ref={dialogRef}
        className={`br-modal ${size}`}
        role="dialog"
        aria-modal="true"
        aria-labelledby={titleId}
        tabIndex={-1}
      >
        <div className="br-modal-header">
          <div className="br-modal-title" id={titleId}>
            {title}
          </div>
          <Button iconOnly variant="tertiary" aria-label="Fechar" onClick={onClose}>
            <i className="fas fa-times" aria-hidden="true" />
          </Button>
        </div>
        <div className="br-modal-body">{children}</div>
        {footer && <div className="br-modal-footer justify-content-end">{footer}</div>}
      </div>
    </div>,
    document.body,
  );
}
