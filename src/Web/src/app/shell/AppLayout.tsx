// Shell autenticado: topbar fixa + sidebar fixa de módulos + área de conteúdo.
//  - skip-link (accesskey 1) salta para o <main>;
//  - foco gerenciado a cada navegação (tabIndex -1) para leitores de tela;
//  - sidebar off-canvas em telas pequenas (toggle na topbar + backdrop);
//  - Suspense para o lazy-loading dos módulos.
import { Suspense, useEffect, useRef, useState } from 'react';
import { Outlet, useLocation } from 'react-router-dom';
import { Header } from './Header';
import { Sidebar } from './Sidebar';
import { Footer } from './Footer';
import { useSidebarColapsada } from './useSidebarColapsada';
import { Spinner } from '../../components/ui';

export function AppLayout() {
  const mainRef = useRef<HTMLElement>(null);
  const { pathname } = useLocation();
  const [menuAberto, setMenuAberto] = useState(false);
  const { colapsada, alternar } = useSidebarColapsada();

  useEffect(() => {
    mainRef.current?.focus();
    setMenuAberto(false);
  }, [pathname]);

  return (
    <div className={`app-shell${colapsada ? ' sidebar-colapsada' : ''}`}>
      <a className="br-skip-link" href="#conteudo" accessKey="1">
        Ir para o conteúdo
      </a>

      <Header onToggleMenu={() => setMenuAberto((aberto) => !aberto)} />
      <Sidebar aberta={menuAberto} colapsada={colapsada} onAlternar={alternar} />
      {menuAberto && (
        <div className="tg-backdrop" aria-hidden="true" onClick={() => setMenuAberto(false)} />
      )}

      <div className="tg-content">
        <main ref={mainRef} id="conteudo" className="app-main" tabIndex={-1}>
          <Suspense
            fallback={
              <div className="app-center">
                <Spinner label="Carregando módulo…" />
              </div>
            }
          >
            <Outlet />
          </Suspense>
        </main>
        <Footer />
      </div>
    </div>
  );
}
