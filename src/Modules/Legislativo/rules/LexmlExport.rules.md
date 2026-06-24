# Export LexML-BR de Normas — Regras (Rules-as-Code)

> Bounded Context: **Legislativo** (Camara). W9.5 (item 1): representacao LexML das normas
> (Lei/Decreto/Resolucao) — URN `lex-br` + XML conforme o padrao LexML-BR; export LOCAL (sem credencial).
> Autoridade: `docs/architecture/m9-prep/M9-BREAKDOWN.md` (W9.5).
> Constituicao: isolamento de modulo, multi-tenant (Global Query Filter), dominio rico, sem numero magico.

---

## 1. Linguagem ubiqua

- **URN LexML:** identificador unico/persistente/resolvel de norma — `urn:lex:[local]:[autoridade]:[tipo]:[data];[numero]`.
- **Local:** jurisdicao na hierarquia LexML — `br;[uf];[municipio]` (ex.: `br;rs;maximiliano.de.almeida`).
- **Autoridade:** quem expede a norma (`camara.municipal`, `municipio`).
- **Tipo LexML:** vocabulario do documento (`lei`, `lei.complementar`, `decreto.legislativo`, `resolucao`,
  `emenda.lei.organica`, `lei.organica`).
- **Documento LexML:** XML no namespace `http://www.lexml.gov.br/1.0` com `Metadado` (Identificacao/URN +
  epigrafe + ementa) + `Norma` (parte inicial + articulacao).

---

## 2. Invariantes

- **LX-1** A URN e composta de forma deterministica: mesmo ente + autoridade + tipo + data + numero => mesma URN.
- **LX-2** UF normalizada deve ter 2 letras; municipio e autoridade nao podem ser vazios (grafia LexML:
  minusculo, sem acento, espacos -> ponto).
- **LX-3** O tipo da norma deve ter mapeamento LexML explicito (lista exaustiva; tipo novo => erro, nao silencio).
- **LX-4** O numero da norma e positivo; a data compoe o descritor em ISO `aaaa-mm-dd`.
- **LX-5** A jurisdicao (UF/municipio/autoridade) e PARAMETRO por tenant (`Legislativo:Lexml:*`), nunca hardcoded.
- **LX-6** O export e tenant-scoped: so exporta norma do tenant atual (Global Query Filter); norma de outro
  tenant => 404.
- **LX-7** O XML preserva integralmente o texto articulado quando houver (sem motor de parsing de artigos —
  fora do escopo W9.5).

---

## 3. RBAC

- `legislativo.ver` — exportar/baixar o LexML de uma norma.

---

## 4. Cenarios BDD (resumo)

- **URN composta:** dada Lei n. 1234 de 2025-03-10 do ente `br;rs;maximiliano.de.almeida` autoridade
  `camara.municipal`, quando exportar, entao a URN e
  `urn:lex:br;rs;maximiliano.de.almeida:camara.municipal:lei:2025-03-10;1234`.
- **XML LexML:** quando exportar, entao o XML tem raiz `LexML` no namespace LexML 1.0, com `Identificacao`
  (URN), `Epigrafe` e `Ementa`.
- **Jurisdicao ausente:** dada a configuracao LexML do tenant sem UF/municipio, quando exportar, entao falha
  com mensagem clara (jurisdicao nao configurada).
- **Isolamento por tenant:** dada norma de outro tenant, quando exportar, entao 404.

---

## 5. Fronteira M10

- // TODO(M10): transmissao/publicacao oficial do LexML a base nacional/TCE e a dados abertos assinados
  (PDF-A + assinatura A1). No M9: gerar/validar local (URN + XML).

<!-- manifest
queries: ExportarNormaLexml
domainServices: DocumentoLexml, UrnLexml, IdentificacaoEnte, LexmlSlug
-->
