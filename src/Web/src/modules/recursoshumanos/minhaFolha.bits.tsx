// Blocos compartilhados pelas seções da "Minha Folha" (campo de ano e total monetário).
// Mantidos isolados para cada seção ficar enxuta (< 300 linhas, padrão-ouro).
import { FormField, Input } from '../../components/ui';
import { formatarMoeda } from '../../i18n/format';

export const ANO_ATUAL = new Date().getFullYear();
export const MES_ATUAL = new Date().getMonth() + 1;

/** Campo numérico de ano reutilizado pelos filtros de competência. */
export function CampoAno({
  label,
  value,
  onChange,
}: {
  label: string;
  value: string;
  onChange: (v: string) => void;
}) {
  return (
    <FormField label={label}>
      {({ id, describedBy }) => (
        <Input
          id={id}
          type="number"
          min="2000"
          max="2100"
          step="1"
          inputMode="numeric"
          aria-describedby={describedBy}
          value={value}
          onChange={(e) => onChange(e.target.value)}
        />
      )}
    </FormField>
  );
}

/** Bloco de total monetário (proventos/descontos/líquido, rendimentos etc.). */
export function Total({ rotulo, valor }: { rotulo: string; valor: number }) {
  return (
    <div className="col-sm-4 mb-2">
      <dt className="text-gray-60 text-down-01">{rotulo}</dt>
      <dd className="mb-0 text-semi-bold">{formatarMoeda(valor)}</dd>
    </div>
  );
}
