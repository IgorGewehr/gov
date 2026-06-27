// Barril de API do módulo RecursosHumanos. A implementação está dividida por
// entidade (servidor.api / cargo.api / folha.api) para manter cada arquivo enxuto
// (< 300 linhas) e coeso por agregado. As páginas/modais continuam importando de
// './api' — este arquivo apenas reexporta a superfície pública.
//
// Contrato real: src/Modules/RecursosHumanos/...Infrastructure/RecursosHumanosEndpoints.cs
// (grupo /api/recursoshumanos; o http client já prefixa /api → usamos /recursoshumanos).
export { rhKeys } from './rhKeys';
export type { CriacaoResponse } from './rhKeys';

export * from './servidor.api';
export * from './cargo.api';
export * from './rubrica.api';
export * from './tabelaLegal.api';
export * from './folha.api';
export * from './cicloAnual.api';
export * from './ponto.api';
export * from './esocial.api';
export * from './minhaFolha.api';
export * from './consignacao.api';
export * from './relatorio.api';
export * from './portaria.api';
export * from './pasep.api';
export * from './sst.api';
export * from './bancoDeHoras.api';
export * from './certidaoTempo.api';
