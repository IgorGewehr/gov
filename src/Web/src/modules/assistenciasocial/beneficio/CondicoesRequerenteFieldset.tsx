// Fieldset acessivel das condicoes faticas do requerente (DadosElegibilidadeDto),
// extraido do BeneficioFormModal para manter os arquivos < 300 linhas. Componente
// controlado: o estado vive no formulario pai.
export interface CondicoesRequerente {
  possuiDeficiencia: boolean;
  possuiAvaliacao: boolean;
  acumulaSeguridade: boolean;
  inscritoCadUnico: boolean;
}

export interface CondicoesRequerenteFieldsetProps {
  valores: CondicoesRequerente;
  onChange: (parcial: Partial<CondicoesRequerente>) => void;
}

export function CondicoesRequerenteFieldset({ valores, onChange }: CondicoesRequerenteFieldsetProps) {
  return (
    <fieldset className="mb-3">
      <legend className="text-down-01 text-gray-60">Condições do requerente</legend>

      <div className="br-checkbox">
        <input
          id="bnf-deficiencia"
          type="checkbox"
          checked={valores.possuiDeficiencia}
          onChange={(e) => onChange({ possuiDeficiencia: e.target.checked })}
        />
        <label htmlFor="bnf-deficiencia">Pessoa com deficiência (PCD)</label>
      </div>

      <div className="br-checkbox">
        <input
          id="bnf-avaliacao"
          type="checkbox"
          checked={valores.possuiAvaliacao}
          onChange={(e) => onChange({ possuiAvaliacao: e.target.checked })}
        />
        <label htmlFor="bnf-avaliacao">Avaliação biopsicossocial concluída (requisito BPC/PCD)</label>
      </div>

      <div className="br-checkbox">
        <input
          id="bnf-seguridade"
          type="checkbox"
          checked={valores.acumulaSeguridade}
          onChange={(e) => onChange({ acumulaSeguridade: e.target.checked })}
        />
        <label htmlFor="bnf-seguridade">Acumula outro benefício da Seguridade Social (veda BPC)</label>
      </div>

      <div className="br-checkbox">
        <input
          id="bnf-cadunico"
          type="checkbox"
          checked={valores.inscritoCadUnico}
          onChange={(e) => onChange({ inscritoCadUnico: e.target.checked })}
        />
        <label htmlFor="bnf-cadunico">Inscrito no CadÚnico (preferência em benefício eventual)</label>
      </div>
    </fieldset>
  );
}
