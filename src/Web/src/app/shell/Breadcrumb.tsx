// Breadcrumb gov.br (br-breadcrumb) — obrigatório em telas internas (eMAG 3.4).
// Deriva a trilha do pathname: Início > <Módulo> > <segmentos>. O último item é a
// página atual (aria-current="page"), sem link.
import { Link, useLocation } from 'react-router-dom';
import { moduleNavItems } from '../../modules/registry';

interface Crumb {
  label: string;
  path?: string;
}

function rotuloSegmento(segment: string): string {
  // Substitui hífens por espaços; capitaliza. Segmentos técnicos conhecidos têm rótulo amigável.
  const known: Record<string, string> = { 'dividas-ativas': 'Dívida Ativa' };
  if (known[segment]) return known[segment];
  const decoded = decodeURIComponent(segment);
  return decoded.charAt(0).toUpperCase() + decoded.slice(1).replace(/-/g, ' ');
}

function construirTrilha(pathname: string): Crumb[] {
  const segments = pathname.split('/').filter(Boolean);
  const crumbs: Crumb[] = [{ label: 'Início', path: '/' }];
  if (segments.length === 0) return crumbs;

  const moduleNav = moduleNavItems.find((m) => m.path === `/${segments[0]}`);
  let acumulado = '';
  segments.forEach((segment, index) => {
    acumulado += `/${segment}`;
    const isLast = index === segments.length - 1;
    const label = index === 0 && moduleNav ? moduleNav.label : rotuloSegmento(segment);
    crumbs.push({ label, path: isLast ? undefined : acumulado });
  });
  return crumbs;
}

export function Breadcrumb() {
  const { pathname } = useLocation();
  const crumbs = construirTrilha(pathname);
  if (crumbs.length <= 1) return null;

  return (
    <nav className="br-breadcrumb" aria-label="Você está em:">
      <ol className="crumb-list">
        {crumbs.map((crumb, index) => {
          const isLast = index === crumbs.length - 1;
          return (
            <li key={`${crumb.label}-${index}`} className={`crumb${index === 0 ? ' home' : ''}`} aria-current={isLast ? 'page' : undefined}>
              {index > 0 && <i className="icon fas fa-chevron-right" aria-hidden="true" />}
              {crumb.path ? (
                <Link to={crumb.path}>
                  {index === 0 ? <span className="sr-only">Início</span> : crumb.label}
                  {index === 0 && <i className="icon fas fa-home" aria-hidden="true" />}
                </Link>
              ) : (
                <span>{crumb.label}</span>
              )}
            </li>
          );
        })}
      </ol>
    </nav>
  );
}
