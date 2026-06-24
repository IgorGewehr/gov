// REGISTRY central de módulos do Tensorroot.Gov.
//
// Importa a ModuleDefinition de cada módulo. As PÁGINAS de cada módulo são
// carregadas via React.lazy DENTRO do seu index.tsx, então cada módulo vira um
// chunk separado (code-splitting) mesmo com o import estático aqui — o index.tsx
// é minúsculo (só metadados + refs lazy). O router central (routes.tsx) e a
// Sidebar consomem este registry; nenhum outro arquivo importa o interno de um
// módulo. Para adicionar um módulo:
//   1. crie src/modules/<id>/index.tsx com `export default` de ModuleDefinition;
//   2. adicione a definição ao array `modules` abaixo.
import type { ModuleDefinition, ModuleNav } from './types';
import painelgestor from './painelgestor';
import administracao from './administracao';
import tributos from './tributos';
import financas from './financas';
import recursoshumanos from './recursoshumanos';
import saude from './saude';
import educacao from './educacao';
import legislativo from './legislativo';
import transparencia from './transparencia';
import protocolo from './protocolo';
import patrimonio from './patrimonio';
import assistenciasocial from './assistenciasocial';
import convenios from './convenios';
import admin from './admin';

/** Ordem reflete a navegação na Sidebar. */
export const modules: ModuleDefinition[] = [
  // Painel do Gestor (dashboard executivo) fica no topo: é a landing do prefeito/
  // gestor, gated por "painel.ver". Não é um Bounded Context de domínio.
  painelgestor,
  administracao,
  tributos,
  financas,
  recursoshumanos,
  patrimonio,
  saude,
  educacao,
  assistenciasocial,
  convenios,
  protocolo,
  legislativo,
  transparencia,
  // Administração do Sistema fica por último: área restrita (gating por permissão
  // na Sidebar e nas rotas). Não é um Bounded Context de domínio.
  admin,
];

/** Itens de navegação para a Sidebar. */
export const moduleNavItems: ModuleNav[] = modules.map((m) => m.nav);
