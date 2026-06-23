// Definições de colunas das tabelas do detalhe do Inventário (snapshot/contagem e
// divergências). Extraídas da página para mantê-la enxuta (< 300 linhas).
import { Button, Tag } from '../../../components/ui';
import type { Column } from '../../../components/ui';
import { Can } from '../../../auth/Can';
import { formatarMoeda } from '../../../i18n/format';
import type { DivergenciaInventarioDto, ItemInventarioDto } from './inventario.api';
import {
  recomendacaoLabel,
  situacaoEncontradaLabel,
  situacaoEncontradaTagVariant,
  tipoDivergenciaLabel,
  tipoDivergenciaTagVariant,
} from './inventario.helpers';

/** Colunas do snapshot + contagem física. `onContar` só é exposto quando `podeContar`. */
export function colunasItens(
  podeContar: boolean,
  onContar: (item: ItemInventarioDto) => void,
): Column<ItemInventarioDto>[] {
  return [
    { key: 'tombo', header: 'Tombo', render: (i) => i.numeroTombamento ?? '—' },
    {
      key: 'descricao',
      header: 'Descrição',
      sortAccessor: (i) => i.descricaoSnapshot,
      render: (i) => i.descricaoSnapshot,
    },
    { key: 'esperada', header: 'Localização esperada', render: (i) => i.localizacaoEsperada ?? '—' },
    {
      key: 'valor',
      header: 'Valor contábil',
      align: 'end',
      sortAccessor: (i) => i.valorContabilSnapshot,
      render: (i) => formatarMoeda(i.valorContabilSnapshot),
    },
    {
      key: 'contagem',
      header: 'Contagem',
      render: (i) =>
        i.contado && i.situacaoEncontrada ? (
          <Tag variant={situacaoEncontradaTagVariant(i.situacaoEncontrada)}>
            {situacaoEncontradaLabel(i.situacaoEncontrada)}
          </Tag>
        ) : (
          <Tag variant="default">Pendente</Tag>
        ),
    },
    {
      key: 'acoes',
      header: 'Ações',
      sticky: true,
      render: (i) =>
        podeContar ? (
          <Can permission="patrimonio.gerenciar">
            <Button variant="secondary" onClick={() => onContar(i)}>
              {i.contado ? 'Recontar' : 'Contar'}
            </Button>
          </Can>
        ) : (
          '—'
        ),
    },
  ];
}

/** Colunas das divergências apuradas na conciliação. */
export const COLUNAS_DIVERGENCIAS: Column<DivergenciaInventarioDto>[] = [
  {
    key: 'tipo',
    header: 'Tipo',
    render: (d) => <Tag variant={tipoDivergenciaTagVariant(d.tipo)}>{tipoDivergenciaLabel(d.tipo)}</Tag>,
  },
  { key: 'descricao', header: 'Descrição', render: (d) => d.descricao },
  { key: 'recomendacao', header: 'Recomendação', render: (d) => recomendacaoLabel(d.recomendacao) },
];
