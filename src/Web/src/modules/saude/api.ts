// Barrel da camada de API do módulo Saúde. A implementação foi dividida por agregado
// (paciente/atendimento/regulacao) para manter cada arquivo < 300 linhas; este barrel
// preserva o ponto único de import `./api` usado pelas páginas/modais e testes.
// Endpoints reais sob /api/saude (ver ...Saude.Infrastructure/SaudeEndpoints.cs).
export { saudeKeys } from './saude.keys';

export * from './paciente.api';
export * from './estabelecimento.api';
export * from './profissional.api';
export * from './atendimento.api';
export * from './regulacao.api';
export * from './fiscal.api';
