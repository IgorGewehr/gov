// Helpers de apresentação dos AFASTAMENTOS/LICENÇAS tipados (Onda 1).
// O usuário escolhe o TIPO; o efeito na folha (suspende/reduz proventos, conta tempo)
// vem da regra do tipo no backend — a UI apenas rotula e exibe o efeito já calculado.
// Enums: src/Modules/RecursosHumanos/...Domain/Afastamentos/Enums.cs
import type { SelectOption, TagVariant } from '../../components/ui';

/**
 * Opções de TIPO de afastamento (rótulo PT-BR → NOME do enum `TipoAfastamento` do backend).
 * O bind do endpoint é por NOME (não índice). Ordem do enum legal preservada.
 */
export const TIPOS_AFASTAMENTO: SelectOption[] = [
  { value: 'LicencaMaternidade', label: 'Licença-maternidade' },
  { value: 'LicencaPaternidade', label: 'Licença-paternidade' },
  { value: 'DoencaAte15Dias', label: 'Doença (até 15 dias — paga pelo ente)' },
  { value: 'DoencaInss', label: 'Auxílio-doença (acima de 15 dias — INSS)' },
  { value: 'AcidenteTrabalho', label: 'Acidente de trabalho' },
  { value: 'LicencaPremio', label: 'Licença-prêmio' },
  { value: 'LicencaSemVencimento', label: 'Licença sem vencimento (interesse particular)' },
  { value: 'CessaoComOnus', label: 'Cessão com ônus para o cedente' },
  { value: 'CessaoSemOnus', label: 'Cessão sem ônus para o cedente' },
  { value: 'MandatoEletivo', label: 'Mandato eletivo (CF art. 38)' },
];

const TIPO_LABEL: Record<string, string> = Object.fromEntries(
  TIPOS_AFASTAMENTO.map((o) => [o.value, o.label]),
);

/** Rótulo PT-BR do tipo de afastamento (fallback para o próprio valor se desconhecido). */
export function formatarTipoAfastamento(tipo: string): string {
  return TIPO_LABEL[tipo] ?? tipo;
}

/** Rótulo PT-BR da situação do afastamento (ciclo de vida). */
export function formatarSituacaoAfastamento(situacao: string): string {
  switch (situacao) {
    case 'Vigente':
      return 'Vigente';
    case 'Encerrado':
      return 'Encerrado';
    case 'Cancelado':
      return 'Cancelado';
    default:
      return situacao;
  }
}

/** Mapeia a situação do afastamento para a variante semântica da Tag. */
export function situacaoAfastamentoTagVariant(situacao: string): TagVariant {
  switch (situacao) {
    case 'Vigente':
      return 'info';
    case 'Encerrado':
      return 'success';
    case 'Cancelado':
      return 'default';
    default:
      return 'default';
  }
}

/**
 * Resumo legível do EFEITO na folha de um afastamento (vem da regra do tipo, snapshot
 * congelado no registro). Ex.: "Suspende proventos", "Mantém 100%", "Ente paga 15 dia(s)".
 */
export function descreverEfeitoFolha(efeito: {
  suspendeProventos: boolean;
  percentualRemuneracao: number;
  diasPagosPeloEnte: number;
  contaTempo: boolean;
}): string {
  const partes: string[] = [];
  if (efeito.diasPagosPeloEnte > 0) {
    partes.push(`Ente paga ${efeito.diasPagosPeloEnte} dia(s) iniciais`);
  }
  if (efeito.suspendeProventos) {
    partes.push('depois suspende proventos');
  } else {
    partes.push(`mantém ${formatarPercentual(efeito.percentualRemuneracao)} da remuneração`);
  }
  partes.push(efeito.contaTempo ? 'conta tempo de serviço' : 'não conta tempo de serviço');
  return partes.join(' · ');
}

/** Formata um percentual 0..100 (do backend) como texto PT-BR (ex.: 100 → "100%"). */
export function formatarPercentual(percentual: number): string {
  return `${percentual.toLocaleString('pt-BR', { maximumFractionDigits: 2 })}%`;
}
