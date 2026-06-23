// Helpers de apresentação das telas de eSocial. Mapeiam os enums do backend
// (EstadoEventoESocial / TipoEventoESocial / AmbienteESocial) para cor de Tag e
// rótulos legíveis PT-BR. Mantém as telas enxutas e a terminologia consistente.
import type { TagVariant, SelectOption } from '../../components/ui';

/** Cor da Tag conforme o estado do evento na máquina de estados. */
export function estadoEventoTagVariant(estado: string): TagVariant {
  switch (estado) {
    case 'Processado':
      return 'success';
    case 'Assinado':
      return 'info';
    case 'Transmitido':
      return 'warning';
    case 'Gerado':
      return 'default';
    case 'Rejeitado':
    case 'RejeitadoLocal':
      return 'danger';
    default:
      return 'default';
  }
}

/** Rótulo legível (com o código S-XXXX) do tipo de evento. */
export function rotuloTipoEvento(tipo: string): string {
  switch (tipo) {
    case 'S1000Empregador':
      return 'S-1000 Empregador';
    case 'S1005Estabelecimento':
      return 'S-1005 Estabelecimento';
    case 'S1010Rubrica':
      return 'S-1010 Rubrica';
    case 'S2200Admissao':
      return 'S-2200 Admissão';
    case 'S2299Desligamento':
      return 'S-2299 Desligamento';
    case 'S1200Remuneracao':
      return 'S-1200 Remuneração';
    case 'S1202RemuneracaoRpps':
      return 'S-1202 Remuneração RPPS';
    case 'S1210Pagamentos':
      return 'S-1210 Pagamentos';
    case 'S1299Fechamento':
      return 'S-1299 Fechamento';
    default:
      return tipo;
  }
}

/** Rótulo legível do estado (sem alterar o valor canônico do backend). */
export function rotuloEstado(estado: string): string {
  switch (estado) {
    case 'RejeitadoLocal':
      return 'Rejeitado (local)';
    default:
      return estado;
  }
}

/** Rótulo do ambiente de transmissão (tpAmb). */
export function rotuloAmbiente(ambiente: string): string {
  switch (ambiente) {
    case 'ProducaoRestrita':
      return 'Produção Restrita (homologação)';
    case 'Producao':
      return 'Produção';
    default:
      return ambiente;
  }
}

/** Estados que aceitam assinatura (apenas Gerado). */
export function podeAssinar(estado: string): boolean {
  return estado === 'Gerado';
}

/**
 * Opções de filtro por estado (rótulo PT-BR → valor canônico do backend).
 * O valor vazio representa "todos".
 */
export const FILTRO_ESTADOS: SelectOption[] = [
  { value: '', label: 'Todos os estados' },
  { value: 'Gerado', label: 'Gerado' },
  { value: 'Assinado', label: 'Assinado' },
  { value: 'Transmitido', label: 'Transmitido' },
  { value: 'Processado', label: 'Processado' },
  { value: 'Rejeitado', label: 'Rejeitado' },
  { value: 'RejeitadoLocal', label: 'Rejeitado (local)' },
];

/** Opções de filtro por tipo de evento (rótulo → valor canônico). */
export const FILTRO_TIPOS: SelectOption[] = [
  { value: '', label: 'Todos os tipos' },
  { value: 'S1000Empregador', label: 'S-1000 Empregador' },
  { value: 'S1005Estabelecimento', label: 'S-1005 Estabelecimento' },
  { value: 'S1010Rubrica', label: 'S-1010 Rubrica' },
  { value: 'S2200Admissao', label: 'S-2200 Admissão' },
  { value: 'S2299Desligamento', label: 'S-2299 Desligamento' },
  { value: 'S1200Remuneracao', label: 'S-1200 Remuneração' },
  { value: 'S1202RemuneracaoRpps', label: 'S-1202 Remuneração RPPS' },
  { value: 'S1210Pagamentos', label: 'S-1210 Pagamentos' },
  { value: 'S1299Fechamento', label: 'S-1299 Fechamento' },
];
