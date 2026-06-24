// Sub-navegação interna de módulo (entre os contextos/agregados de um Bounded Context).
// Mantém UMA entrada por módulo na Sidebar; a navegação entre seções acontece aqui.
//
// Por que existe: havia 6 sub-navs quase idênticas (RH, Tributos, Finanças, Saúde,
// Educação, Assistência Social) + a do Legislativo, com risco de drift. Pior: módulos
// com MUITAS abas (Legislativo tem 11; RH/Tributos/Finanças/Saúde têm 9–10) ficavam
// apertados e feios. Este componente padroniza tudo: o item ATIVO recebe destaque
// forte (pílula preenchida + indicador inferior) e a barra ROLA na horizontal quando
// as abas não cabem, com botões de seta e máscaras de esmaecimento sinalizando que há
// mais conteúdo — a aba ativa é trazida para a vista automaticamente. Com poucas abas a
// barra fica centrada e sem setas. Nenhuma aba some; nada de quebra de linha bagunçada.
//
// Acessibilidade: landmark <nav aria-label> + NavLink com aria-current="page" (gov.br
// DS/eMAG/WCAG 2.1 AA). Navegação por teclado nativa (links tabuláveis); as setas de
// rolagem têm aria-hidden e são puro auxílio visual (não substituem o Tab).
import { useCallback, useEffect, useRef, useState } from 'react';
import { NavLink } from 'react-router-dom';
import { useAuth } from '../../auth/useAuth';

export interface SubNavItem {
  /** Rota de destino. */
  to: string;
  /** Rótulo visível da aba. */
  label: string;
  /** Marca a aba ativa apenas na correspondência exata (use na rota índice do módulo). */
  end?: boolean;
  /** Classe de ícone Font Awesome (ex.: 'fas fa-folder-open'); opcional. */
  icon?: string;
  /** Quando presente, a aba só aparece se o usuário tiver esta permissão. */
  perm?: string;
}

export interface SubNavProps {
  /** Texto do aria-label do landmark (ex.: 'Seções de Recursos Humanos'). */
  ariaLabel: string;
  /** Abas da sub-navegação, na ordem de exibição. */
  itens: ReadonlyArray<SubNavItem>;
}

/** Quanto rolar por clique na seta (≈ um "página" parcial da pista visível). */
const PASSO_ROLAGEM = 0.8;
/** Tolerância para considerar a pista "no fim" (evita seta acesa por subpixel). */
const FOLGA_BORDA = 2;

export function SubNav({ ariaLabel, itens }: SubNavProps) {
  // hasPermission é estável; filtramos as abas protegidas por permissão (gating de UI,
  // espelhando o RBAC do backend — a autorização real continua server-side).
  const { hasPermission } = useAuth();
  const visiveis = itens.filter((item) => !item.perm || hasPermission(item.perm));

  const pistaRef = useRef<HTMLUListElement>(null);
  // Estado de "transbordo": há conteúdo escondido à esquerda/direita? Controla as
  // máscaras de esmaecimento e a habilitação das setas (afeta só o visual/auxílio).
  const [maisInicio, setMaisInicio] = useState(false);
  const [maisFim, setMaisFim] = useState(false);

  const recalcular = useCallback(() => {
    const el = pistaRef.current;
    if (!el) return;
    const { scrollLeft, scrollWidth, clientWidth } = el;
    setMaisInicio(scrollLeft > FOLGA_BORDA);
    setMaisFim(scrollLeft + clientWidth < scrollWidth - FOLGA_BORDA);
  }, []);

  // Recalcula no mount, ao mudar as abas e ao redimensionar (ResizeObserver pega
  // colapso/expansão da sidebar e mudanças de viewport). A própria pista emite scroll.
  useEffect(() => {
    recalcular();
    const el = pistaRef.current;
    if (!el || typeof ResizeObserver === 'undefined') return;
    const ro = new ResizeObserver(recalcular);
    ro.observe(el);
    return () => ro.disconnect();
  }, [recalcular, visiveis.length]);

  // Traz a aba ativa para a vista quando a navegação muda (descoberta + orientação).
  useEffect(() => {
    const el = pistaRef.current;
    const ativo = el?.querySelector<HTMLElement>('a[aria-current="page"]');
    // Guard: scrollIntoView nao existe no jsdom (testes) e pode faltar em ambientes nao-DOM.
    if (typeof ativo?.scrollIntoView === 'function') {
      ativo.scrollIntoView({ block: 'nearest', inline: 'nearest' });
    }
    recalcular();
  });

  const rolar = useCallback((direcao: -1 | 1) => {
    const el = pistaRef.current;
    if (!el) return;
    el.scrollBy({ left: direcao * el.clientWidth * PASSO_ROLAGEM, behavior: 'smooth' });
  }, []);

  return (
    <nav className="tg-subnav mb-4" aria-label={ariaLabel}>
      <div
        className={`tg-subnav-pista-wrap${maisInicio ? ' mais-inicio' : ''}${
          maisFim ? ' mais-fim' : ''
        }`}
      >
        <button
          type="button"
          className="tg-subnav-seta tg-subnav-seta-inicio"
          aria-hidden="true"
          tabIndex={-1}
          disabled={!maisInicio}
          onClick={() => rolar(-1)}
        >
          <i className="fas fa-chevron-left" aria-hidden="true" />
        </button>

        <ul className="tg-subnav-lista" ref={pistaRef} onScroll={recalcular}>
          {visiveis.map((item) => (
            <li key={item.to}>
              <NavLink
                to={item.to}
                end={item.end}
                className={({ isActive }) => `tg-tab${isActive ? ' active' : ''}`}
              >
                {item.icon ? <i className={item.icon} aria-hidden="true" /> : null}
                <span className="tg-tab-rotulo">{item.label}</span>
              </NavLink>
            </li>
          ))}
        </ul>

        <button
          type="button"
          className="tg-subnav-seta tg-subnav-seta-fim"
          aria-hidden="true"
          tabIndex={-1}
          disabled={!maisFim}
          onClick={() => rolar(1)}
        >
          <i className="fas fa-chevron-right" aria-hidden="true" />
        </button>
      </div>
    </nav>
  );
}
