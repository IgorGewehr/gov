// Catálogo (dicionário) FIXO de datasets de dados abertos — espelha exatamente o
// CatalogoDadosAbertos do backend (Application/PortalPublico/DadosAbertos.cs).
// Determinístico, sem I/O: descreve o que pode ser baixado em CSV no portal público.
// Os DADOS em si vêm tenant-scoped no momento do download (endpoint público).

/** Entrada do dicionário de um dataset de dados abertos. */
export interface DatasetCatalogo {
  /** Identificador (slug) do dataset, ex.: "despesas". */
  dataset: string;
  /** Título legível. */
  titulo: string;
  /** Descrição do conteúdo. */
  descricao: string;
  /** Colunas do CSV (dicionário de dados). */
  colunas: readonly string[];
}

/** Datasets disponíveis no portal e suas colunas (dicionário de dados). */
export const CATALOGO_DADOS_ABERTOS: readonly DatasetCatalogo[] = [
  {
    dataset: 'despesas',
    titulo: 'Despesas públicas',
    descricao: 'Empenho/liquidação/pagamento por exercício (LAI).',
    colunas: [
      'Exercicio',
      'Fase',
      'NumeroEmpenho',
      'Credor',
      'CredorDocumento',
      'FuncaoSubfuncao',
      'FonteRecurso',
      'Valor',
      'Data',
    ],
  },
  {
    dataset: 'receitas',
    titulo: 'Receitas arrecadadas',
    descricao: 'Receita por rubrica/fonte por exercício.',
    colunas: ['Exercicio', 'Rubrica', 'FonteRecurso', 'Valor', 'Data'],
  },
  {
    dataset: 'contratos',
    titulo: 'Contratos',
    descricao: 'Contratos celebrados/publicados (Lei 14.133/2021).',
    colunas: [
      'Exercicio',
      'NumeroContrato',
      'Fornecedor',
      'Objeto',
      'Valor',
      'Modalidade',
      'PNCP',
    ],
  },
  {
    dataset: 'folha',
    titulo: 'Folha nominal',
    descricao: 'Remuneração por servidor (sem CPF/matrícula — Dec. 7.724/2012).',
    colunas: [
      'Competencia',
      'Servidor',
      'Cargo',
      'Lotacao',
      'RemuneracaoBruta',
      'Descontos',
      'Liquido',
    ],
  },
  {
    dataset: 'diarias',
    titulo: 'Diárias',
    descricao: 'Despesas com diárias (elemento 339014), derivadas das despesas.',
    colunas: ['Exercicio', 'Credor', 'FuncaoSubfuncao', 'FonteRecurso', 'Valor', 'Data'],
  },
  {
    dataset: 'repasses',
    titulo: 'Repasses/transferências',
    descricao: 'Repasses a OSC/fundos (elementos 335043/444042), derivados das despesas.',
    colunas: ['Exercicio', 'Beneficiario', 'FuncaoSubfuncao', 'FonteRecurso', 'Valor', 'Data'],
  },
];
