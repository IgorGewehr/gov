# M8 — Portal/App do Cidadão ("Minha Cidade") — Pesquisa

> Escopo: serviços online ao cidadão, Carta de Serviços (Decreto 9.094/2017), Lei 13.460/2017
> (Código de Defesa do Usuário de Serviço Público), login gov.br (bronze/prata/ouro) e
> acessibilidade (eMAG/WCAG). Consome dados dos módulos já existentes (tributos, protocolo,
> finanças, ouvidoria).
>
> Regra §16: nada inventado — cada afirmação tem FONTE ou está marcada `[a confirmar]`.
> Data da pesquisa: 2026-06-22.

---

## 1. Lei 13.460/2017 — Código de Defesa do Usuário de Serviço Público

Lei nacional (aplica-se a União, Estados, DF e **Municípios** — art. 1º). Vigência plena em todo o
território desde 2019 para municípios (entrou em vigor escalonada por porte do ente).
FONTE: https://www.planalto.gov.br/ccivil_03/_ato2015-2018/2017/lei/l13460.htm
FONTE (vigência municipal): https://www.gov.br/cgu/pt-br/assuntos/noticias/2019/04/codigo-de-defesa-do-usuario-do-servico-publico-entra-em-vigor-em-todo-territorio-nacional

### 1.1 Direitos básicos do usuário (art. 5º)
Atendimento por ordem de chegada, prioridade legal, presunção de boa-fé, igualdade no
tratamento, dispensa de exigências desnecessárias, vedação à reapresentação de documentos já em
poder do órgão, entre outros. Aplicação direta no portal: **não reexigir documento já existente**
nos módulos internos (protocolo, cadastro tributário). FONTE: art. 5º (Planalto, ref. acima).

### 1.2 Manifestações e Ouvidoria (Cap. III, arts. 9º–17)
- **Tipos de manifestação** (art. 10): reclamação, denúncia, sugestão, elogio e **solicitação de
  providência** — além de pedido de acesso à informação (LAI) tratado em canal próprio.
- **Manifestação identificada ou anônima**; sigilo da identidade quando solicitado.
- **Prazo de resposta conclusiva: 30 dias**, contados do recebimento, **prorrogável por mais 30
  dias** mediante justificativa expressa (art. 16). Logo, prazo-teto = 60 dias.
  FONTE: https://www.jusbrasil.com.br/topicos/157213372/art-13-da-lei-n-13460-de-26-de-junho-de-2017
  FONTE (prazo/prorrogação): Planalto, art. 16; reportagem confirmatória https://agenciabrasil.ebc.com.br/politica/noticia/2018-06/ouvidorias-do-governo-terao-de-responder-cidadaos-em-ate-60-dias
- A ouvidoria recebe, analisa e encaminha as manifestações, acompanha a prestação e propõe
  melhorias. FONTE: https://www.gov.br/ouvidorias/pt-br/ouvidorias/rede-de-ouvidorias/normativos/regulamentacao-modelo-da-lei-13-460.pdf

### 1.3 Avaliação continuada dos serviços (art. 23)
Avaliação obrigatória sobre 5 aspectos: (i) satisfação do usuário; (ii) qualidade do atendimento;
(iii) cumprimento de compromissos e prazos; (iv) quantidade de manifestações; (v) medidas de
melhoria adotadas.
- **Pesquisa de satisfação no mínimo anual**, ou outro meio com significância estatística.
- **Resultado publicado integralmente** no site do órgão, incluindo ranking das entidades com
  maior incidência de reclamação.
  FONTE: https://modeloinicial.com.br/lei/L-13460-2017/lei-13460/art-23 (transcreve art. 23)
  FONTE (guia metodológico CGU): https://www.gov.br/ouvidorias/pt-br/ouvidorias/avaliacao-de-servicos-publicos/conselhos-de-usuarios/GUIADEAVALIAODESERVIOS.pdf

### 1.4 Conselho de Usuários (arts. 18–21)
Órgão consultivo: acompanha a prestação, participa da avaliação, propõe melhorias e contribui na
definição de diretrizes. FONTE: Planalto (ref. acima), arts. 18–21.

> **Implicação para o produto:** o módulo Ouvidoria já existente deve expor no portal:
> protocolo de manifestação, acompanhamento por número, contador de prazo (30+30) e formulário de
> **avaliação/pesquisa de satisfação** vinculado a cada serviço da Carta.

---

## 2. Decreto 9.094/2017 — Carta de Serviços ao Usuário

Decreto federal. No texto, vincula órgãos do Executivo **federal**; para municípios a obrigação de
manter Carta de Serviços decorre diretamente da **Lei 13.460/2017, art. 7º** (que é nacional). O
Decreto 9.094 serve de **modelo de conteúdo**. `[a confirmar]` se o município-alvo adota o 9.094 como
referência ou regulamento próprio.
FONTE: https://www.planalto.gov.br/ccivil_03/_ato2015-2018/2017/decreto/d9094.htm
FONTE (orientação CGU): https://www.gov.br/ouvidorias/pt-br/ouvidorias/avaliacao-de-servicos-publicos/carta-de-servicos-ao-usuario

### 2.1 Conteúdo obrigatório por serviço
Informações claras e precisas de cada serviço, especialmente: serviços oferecidos; **requisitos e
documentos** necessários; **prazos**; forma de prestação/entrega; locais e formas de acesso; tempo
de espera; **canais de comunicação/reclamação** (ouvidoria); condições de **acessibilidade**;
limpeza e conforto; e definição dos usuários com **prioridade de atendimento**.
FONTE: Decreto 9.094, art. 11–12 (Planalto, ref. acima).

### 2.2 Divulgação
Carta e formulário "Simplifique!" devem ter divulgação permanente: nos locais de atendimento, nos
portais institucionais/de serviços e no Portal de Serviços do Governo Federal (servicos.gov.br).
FONTE: Decreto 9.094 (Planalto, ref. acima).

> **Implicação para o produto:** a Carta de Serviços deve ser **estrutura de dados** (catálogo de
> serviços) e não página estática — cada serviço modelado com requisitos/documentos/prazos/canal,
> alimentando tanto o portal quanto a pesquisa de satisfação (art. 23) e o agendamento.

---

## 3. Login gov.br — autenticação do cidadão (bronze / prata / ouro)

Conta única federal sobre **OpenID Connect 1.0 / OAuth 2.0**. Três níveis ("selos de
confiabilidade") que refletem o grau de validação cadastral e liberam classes diferentes de
serviço/transação.
FONTE: https://www.gov.br/governodigital/pt-br/identidade/conta-gov-br/niveis-da-conta-govbr

| Nível | Como se obtém (validação) | Libera |
|-------|---------------------------|--------|
| **Bronze** | Cadastro online validado na Receita Federal **ou** INSS; ou presencial (INSS/Balcão gov.br). Segurança básica (CPF + senha). | Acesso básico, autorização de uso de dados, prova de vida facial. |
| **Prata** | Reconhecimento facial (app) contra base da **CNH/Senatran**; **ou** validação via internet banking de banco credenciado; ou credencial SIGEPE (servidor federal). | Visualizar/compartilhar documentos digitais, assinatura eletrônica gratuita (assinador.iti.br), 2FA, serviços de maior confiabilidade. |
| **Ouro** | Reconhecimento facial contra base da **Justiça Eleitoral/TSE**; **ou** leitura QR da CIN (nova carteira de identidade) no app; **ou** **Certificado Digital ICP-Brasil**. | Nível máximo de segurança; acesso a qualquer serviço digital sem restrição. |

FONTE (tabela e requisitos): https://www.gov.br/governodigital/pt-br/identidade/conta-gov-br/niveis-da-conta-govbr
FONTE (comparativo selos): https://agenciagov.ebc.com.br/noticias/202402/entenda-a-diferenca-entre-os-selos-de-confiabilidade-do-gov.br

### 3.1 Integração técnica (municípios)
- Padrão **OpenID Connect Core 1.0 sobre OAuth 2.0**: fluxo *authorization code* → consentimento de
  *scopes* → troca de `code` por `access_token` + `id_token` (JWT). O município é **Relying Party (RP)**.
- Pré-requisito institucional: **adesão à Rede Nacional de Governo Digital / Plano de Integração ao
  Login Único**; cada ente com domínio de acesso exclusivo; serviço digital registrado em
  servicos.gov.br. Habilitação como RP exige solicitação formal do órgão.
  FONTE: https://acesso.gov.br/roteiro-tecnico/iniciarintegracao.html
  FONTE (catálogo de API): https://www.gov.br/conecta/catalogo/apis/brasil-cidadao-login-unico
  FONTE (caso municipal): https://www.ipm.com.br/plataforma-gov-br-tecnologia-ipm-permite-acesso-por-meio-de-login-unico-nos-municipios/

### 3.2 Definição de nível mínimo por serviço (recomendação de produto)
- **Consultas públicas / 2ª via de tributos por inscrição/CPF** → pode aceitar **bronze** ou até
  acesso anônimo. `[a confirmar]` política do ente.
- **Protocolo de processo, ouvidoria identificada, dados pessoais sensíveis, assinatura** → exigir
  **prata** (documentos digitais + assinatura) — alinhado ao que o próprio gov.br exige.
- **Atos com efeito jurídico forte / assinatura qualificada** → **ouro** (ICP-Brasil).
- Precedente recente exigindo prata/ouro para serviço fazendário: portal Regularize (2026).
  FONTE: https://www.gov.br/fazenda/pt-br/assuntos/noticias/2026/junho/para-acessar-portal-regularize-pessoas-fisicas-deverao-ter-conta-nivel-prata-ou-ouro-no-gov.br

> **Implicação para o produto:** RP gov.br como provedor de identidade primário; o sistema lê o
> **nível/selo** do `id_token` e aplica **autorização por nível** (gating) por serviço. Manter
> login local/CPF apenas como fallback `[a confirmar]` conforme política do ente.

---

## 4. Acessibilidade — eMAG / WCAG

- **eMAG (Modelo de Acessibilidade em Governo Eletrônico)**, versão atual **3.1**. Conjunto de
  recomendações padronizado para sítios/portais do governo brasileiro; baseado em e compatível com
  **WCAG/W3C-WAI** (eMAG não cobre 100% do WCAG 2.0, mas não exclui aplicar qualquer boa prática
  WCAG). FONTE: https://emag.governoeletronico.gov.br/
  FONTE: https://www.gov.br/governodigital/pt-br/acessibilidade-e-usuario/acessibilidade-digital/modelo-de-acessibilidade
- **Obrigatoriedade:** Portaria SLTI nº 3, de 07/05/2007, institucionalizou o eMAG no SISP, tornando
  sua observância **obrigatória** nos sítios do governo. Reforço legal: **Lei Brasileira de Inclusão
  (Lei 13.146/2015), art. 63** — acessibilidade obrigatória em todos os sítios mantidos por órgãos de
  governo. FONTE: https://emag.governoeletronico.gov.br/
  FONTE (LBI art. 63): https://www.jcmgroup.com.br/blog/acessibilidade-digital-setor-publico-guia-pratico-emag-wcag `[a confirmar contra texto da Lei 13.146/2015 no Planalto]`

> **Implicação para o produto:** front-end do portal deve nascer **eMAG 3.1 + WCAG 2.1 AA** —
> contraste, navegação por teclado, ARIA/landmarks, texto alternativo, foco visível, libras/VLibras
> `[a confirmar nível AA vs AAA exigido]`. Inclui o app móvel ("Minha Cidade").

---

## 5. Serviços online do Portal do Cidadão (mapa funcional → módulos consumidos)

| Serviço no portal | Base legal / norma | Módulo Tensorroot.Gov consumido |
|-------------------|--------------------|----------------------------------|
| 2ª via de tributos / **DAM** (IPTU, ISS, ITBI, taxas) | Carta de Serviços (req./prazos) | Tributos / Arrecadação |
| **Consulta de débitos / dívida ativa** | art. 5º (transparência) | Tributos / Dívida Ativa |
| **Protocolo / processo administrativo eletrônico** | Lei 13.460 art. 5º (não reexigir docs) | Protocolo |
| **Ouvidoria** (manifestação + acompanhamento) | Lei 13.460 arts. 9º–17 | Ouvidoria |
| **Avaliação / pesquisa de satisfação** | Lei 13.460 art. 23 | Ouvidoria / BI |
| **Carta de Serviços** (catálogo) | Decreto 9.094 / Lei 13.460 art. 7º | Catálogo de Serviços (novo) |
| **Agendamento** de atendimento | Carta de Serviços (tempo de espera) | Agendamento `[a confirmar se já existe]` |
| **Transparência / LAI** (separado, tarefa irmã) | LAI 12.527/2011 | Finanças / BI |
| **Login do cidadão** | gov.br OIDC | Identidade (novo RP) |

---

## 6. Pendências / a confirmar

1. `[a confirmar]` Regulamento municipal próprio da Lei 13.460 (decreto local) e se adota o
   Decreto 9.094 como modelo de Carta de Serviços.
2. `[a confirmar]` Status da adesão do município ao Plano de Integração ao Login Único gov.br
   (pré-requisito para habilitar o RP) — verificar via gov.br/Conecta.
3. `[a confirmar]` Nível gov.br mínimo por serviço (política do ente) e se haverá fallback de login
   local por CPF.
4. `[a confirmar]` Nível WCAG exigido (AA padrão; AAA em itens específicos) e inclusão de VLibras.
5. `[a confirmar]` Existência prévia dos módulos Agendamento e Catálogo de Serviços no produto.
6. `[a confirmar]` Texto exato dos arts. 16, 18–21 e 23 da Lei 13.460 e do art. 63 da Lei 13.146
   diretamente no Planalto (WebFetch ao planalto.gov.br falhou por timeout nesta pesquisa; números
   de artigo vieram de fontes secundárias oficiais/jurídicas e do guia da CGU).
7. `[a confirmar]` Vigência/aplicação do **Decreto 11.034/2022** (regulamenta a Lei 13.460 no âmbito
   federal) — não localizado texto direto; relevante para periodicidade/indicadores de avaliação.

---

## 7. Fontes principais

- Lei 13.460/2017 — Planalto: https://www.planalto.gov.br/ccivil_03/_ato2015-2018/2017/lei/l13460.htm
- Decreto 9.094/2017 — Planalto: https://www.planalto.gov.br/ccivil_03/_ato2015-2018/2017/decreto/d9094.htm
- Carta de Serviços (CGU): https://www.gov.br/ouvidorias/pt-br/ouvidorias/avaliacao-de-servicos-publicos/carta-de-servicos-ao-usuario
- Guia de avaliação de serviços (CGU): https://www.gov.br/ouvidorias/pt-br/ouvidorias/avaliacao-de-servicos-publicos/conselhos-de-usuarios/GUIADEAVALIAODESERVIOS.pdf
- Vigência municipal Lei 13.460 (CGU): https://www.gov.br/cgu/pt-br/assuntos/noticias/2019/04/codigo-de-defesa-do-usuario-do-servico-publico-entra-em-vigor-em-todo-territorio-nacional
- Níveis da conta gov.br: https://www.gov.br/governodigital/pt-br/identidade/conta-gov-br/niveis-da-conta-govbr
- Selos de confiabilidade: https://agenciagov.ebc.com.br/noticias/202402/entenda-a-diferenca-entre-os-selos-de-confiabilidade-do-gov.br
- Roteiro de integração Login Único: https://acesso.gov.br/roteiro-tecnico/iniciarintegracao.html
- Catálogo API Login Único (Conecta): https://www.gov.br/conecta/catalogo/apis/brasil-cidadao-login-unico
- eMAG: https://emag.governoeletronico.gov.br/
- Modelo de acessibilidade (Gov Digital): https://www.gov.br/governodigital/pt-br/acessibilidade-e-usuario/acessibilidade-digital/modelo-de-acessibilidade
