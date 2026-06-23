// Barrel da camada de API do módulo Transparência. As implementações foram
// divididas por agregado para manter cada arquivo < 300 linhas (CLAUDE.md §13):
//   - remessa.api.ts           → Prestação de contas TCE-RS (SIAPC/PAD) — contrato M4
//   - declaracao-fiscal.api.ts → Declarações fiscais (SICONFI/MSC/RREO/RGF/DCA)
//   - keys.ts                  → query keys compartilhadas + CriacaoResponse
// Mantido como ponto de entrada único (`from './api'`) consumido pelas páginas.
export { transparenciaKeys } from './keys';

export {
  useRemessas,
  useRemessa,
  useRemessaCriticas,
  useGerarRemessa,
  useGerarRemessaFolha,
  useValidarRemessa,
  useEmpacotarRemessa,
  useRegistrarProtocolo,
  useReconciliarRemessa,
  baixarArquivoRemessa,
} from './remessa.api';
export type {
  TipoPeriodo,
  SituacaoRemessa,
  SeveridadeCritica,
  RemessaResumo,
  RemessaDetalhe,
  RemessaArquivo,
  RemessaCritica,
  GerarRemessaInput,
  GerarRemessaFolhaInput,
  RegistrarProtocoloInput,
  ListarRemessasParams,
  ReconciliacaoResultado,
} from './remessa.api';

export {
  useDeclaracoesFiscais,
  useDeclaracaoFiscal,
  useConsolidarDeclaracaoFiscal,
  useTransicaoDeclaracaoFiscal,
} from './declaracao-fiscal.api';
export type {
  TipoDeclaracaoFiscal,
  SituacaoDeclaracaoFiscal,
  NaturezaSaldo,
  DeclaracaoFiscalResumo,
  DeclaracaoFiscalDetalhe,
  LinhaContabilInput,
  ConsolidarDeclaracaoFiscalInput,
  ListarDeclaracoesParams,
} from './declaracao-fiscal.api';
