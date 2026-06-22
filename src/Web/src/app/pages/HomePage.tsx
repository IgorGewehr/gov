// Página inicial (dashboard) — atalhos para os módulos licenciados.
import { Link } from 'react-router-dom';
import { Card, PageHeader } from '../../components/ui';
import { useAuth } from '../../auth/useAuth';
import { moduleNavItems } from '../../modules/registry';

export function HomePage() {
  const { user } = useAuth();
  return (
    <>
      <PageHeader
        title={`Olá, ${user?.nome ?? 'usuário'}`}
        description="Selecione um módulo para começar."
      />
      <div className="row">
        {moduleNavItems.map((item) => (
          <div key={item.path} className="col-sm-6 col-lg-4 mb-3">
            <Link to={item.path} className="d-block text-decoration-none">
              <Card>
                <span className="text-up-01 text-semi-bold">
                  {item.icon && <i className={`${item.icon} mr-2`} aria-hidden="true" />}
                  {item.label}
                </span>
              </Card>
            </Link>
          </div>
        ))}
      </div>
    </>
  );
}
