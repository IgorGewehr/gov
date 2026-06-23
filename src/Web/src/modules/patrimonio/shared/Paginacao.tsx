// Paginacao — barra de navegação de páginas para as listas navegáveis do Patrimonio.
// Componente LOCAL do módulo (não é base de UI): rodapé "X–Y de N" + Anterior/Próxima.
// Acessível: <nav aria-label>, botões desabilitados nos limites, página atual com aria-current.
import { Button } from '../../../components/ui';
import {
  primeiroDaPagina,
  totalPaginas as calcularTotalPaginas,
  ultimoDaPagina,
} from './paginacaoTipos';

export interface PaginacaoProps {
  /** Página atual (1-based). */
  pagina: number;
  /** Tamanho da página. */
  tamanho: number;
  /** Total de registros do resultado. */
  total: number;
  /** Troca de página (recebe a nova página 1-based). */
  onPaginaChange: (pagina: number) => void;
  /** Desabilita os controles enquanto a consulta está em andamento. */
  carregando?: boolean;
}

export function Paginacao({ pagina, tamanho, total, onPaginaChange, carregando = false }: PaginacaoProps) {
  const paginas = calcularTotalPaginas(total, tamanho);
  const inicio = primeiroDaPagina(pagina, tamanho, total);
  const fim = ultimoDaPagina(pagina, tamanho, total);
  const temAnterior = pagina > 1;
  const temProxima = pagina < paginas;

  return (
    <nav className="tg-toolbar tg-toolbar-between" aria-label="Paginação de resultados">
      <div className="tg-toolbar-group" aria-live="polite">
        {total === 0 ? (
          'Nenhum registro'
        ) : (
          <>
            <strong>
              {inicio}–{fim}
            </strong>{' '}
            de <strong>{total}</strong> registro{total === 1 ? '' : 's'}
          </>
        )}
      </div>
      <div className="tg-toolbar-group">
        <Button
          variant="secondary"
          onClick={() => onPaginaChange(pagina - 1)}
          disabled={!temAnterior || carregando}
          aria-label="Página anterior"
        >
          <i className="fas fa-angle-left" aria-hidden="true" /> Anterior
        </Button>
        <span aria-current="page">
          Página {pagina} de {paginas}
        </span>
        <Button
          variant="secondary"
          onClick={() => onPaginaChange(pagina + 1)}
          disabled={!temProxima || carregando}
          aria-label="Próxima página"
        >
          Próxima <i className="fas fa-angle-right" aria-hidden="true" />
        </Button>
      </div>
    </nav>
  );
}
