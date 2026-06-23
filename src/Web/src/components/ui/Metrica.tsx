// Métrica de KPI reutilizável (dashboards executivos): rótulo pequeno em cima,
// valor grande em destaque e uma linha secundária CLARA (ex.: percentual) abaixo
// — nada de "(0,00%)" minúsculo grudado no valor. Semântica de lista de descrição
// (<dt>/<dd>) preservada via wrapper <div role-less>; o conjunto deve viver dentro
// de uma <MetricaGrade> (grid responsivo auto-fit, alinhado e com ritmo único).
import type { ReactNode } from 'react';

export interface MetricaProps {
  /** Rótulo curto (pequeno, muted). */
  label: ReactNode;
  /** Valor principal (grande). */
  valor: ReactNode;
  /** Linha secundária clara (ex.: percentual, contexto). Opcional. */
  secundario?: ReactNode;
  /** Tom semântico do valor/secundário (status, não decoração). */
  tom?: 'neutro' | 'sucesso' | 'alerta' | 'perigo';
}

const TOM_CLASSE: Record<NonNullable<MetricaProps['tom']>, string> = {
  neutro: '',
  sucesso: 'text-success',
  alerta: 'text-warning',
  perigo: 'text-danger',
};

export function Metrica({ label, valor, secundario, tom = 'neutro' }: MetricaProps) {
  const classeTom = TOM_CLASSE[tom];
  return (
    <div className="tg-metrica">
      <dt className="tg-metrica-label">{label}</dt>
      <dd className="tg-metrica-valor mb-0">
        <span className={classeTom || undefined}>{valor}</span>
        {secundario != null && (
          <span className={`tg-metrica-secundario${classeTom ? ` ${classeTom}` : ''}`}>
            {secundario}
          </span>
        )}
      </dd>
    </div>
  );
}

/** Grade responsiva e alinhada de métricas (auto-fit minmax — ritmo único). */
export function MetricaGrade({ children }: { children: ReactNode }) {
  return <dl className="tg-metrica-grade">{children}</dl>;
}
