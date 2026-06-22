# M8 — Verificação adversarial da pesquisa do Portal do Cidadão

> Auditoria cética de `pesquisa-cidadao.md` contra FONTES OFICIAIS (Planalto, gov.br, CGU, Imprensa
> Nacional). Cada afirmação classificada **CONFIRMADO** / **PLAUSÍVEL** / **INCERTO**.
> Método: WebFetch/WebSearch restritos a domínios oficiais. Data: 2026-06-22.
>
> Nota de método: o texto integral do Planalto (l13460.htm e d9094.htm) e o mirror da Imprensa
> Nacional **falharam repetidamente por queda de conexão** durante a auditoria. Onde não houve leitura
> direta do caput, a confirmação veio de fontes oficiais secundárias (CGU/gov.br/Senado) que
> transcrevem o dispositivo — registrado caso a caso abaixo.

---

## Quadro de verificação

### Lei 13.460/2017 — Código de Defesa do Usuário

| # | Afirmação na pesquisa | Classificação | Base / ressalva |
|---|---|---|---|
| 1.0 | Lei nacional, aplica-se a União, Estados, DF e **Municípios** (art. 1º) | **CONFIRMADO** | CGU/gov.br confirmam aplicação às 3 esferas. Caput do art. 1º não lido direto (Planalto caiu), mas a aplicação municipal é reafirmada pela própria notícia da CGU sobre vigência. |
| 1.1 | Vigência plena municipal desde **17/06/2019** (escalonada por porte) | **CONFIRMADO** (com correção) | Notícia oficial CGU: vigência plena nacional em **17/06/2019**; o último escalão foi **municípios com menos de 100.000 habitantes**. A pesquisa diz "escalonada por porte" — correto, mas o escalonamento documentado é **binário (acima/abaixo de 100 mil hab.)**, não multi-faixa. |
| 1.2 | Direitos básicos do art. 5º (ordem de chegada, boa-fé, **não reexigir documento já em poder do órgão**) | **PLAUSÍVEL** | Coerente com o art. 5º e com o princípio de simplificação do Decreto 9.094 (presunção de boa-fé / compartilhamento de informações), confirmados via CGU. Texto literal do art. 5º não lido direto. |
| 1.3 | Manifestações (art. 10): reclamação, denúncia, sugestão, elogio e **solicitação de providência** | **CONFIRMADO** | gov.br/CGU (Fala.BR e serviço oficial) listam exatamente: solicitação, reclamação, elogio, sugestão, denúncia (+ Simplifique). As 5 modalidades batem. |
| 1.4 | **Prazo de resposta 30 dias, prorrogável uma vez por +30** → teto 60 (art. 16) | **CONFIRMADO** | gov.br confirma: decisão conclusiva em 30 dias, prorrogável de forma justificada **uma única vez por igual período**. Parágrafo único: pedidos internos da ouvidoria têm prazo de 20 dias (+20). |
| 1.5 | Avaliação continuada (art. 23) sobre 5 aspectos; **pesquisa de satisfação ≥ anual**; **resultado publicado integralmente** + ranking de reclamações | **CONFIRMADO** | gov.br/CGU confirmam: 5 aspectos (satisfação, qualidade, cumprimento de prazos, quantidade de manifestações, melhorias); §1º pesquisa **no mínimo anual** ou outro meio com significância estatística; §2º **publicação integral no site** com ranking das entidades com maior incidência de reclamação. |
| 1.6 | Conselho de Usuários (arts. 18–21): órgão **consultivo**; acompanha, avalia, propõe melhorias, contribui em diretrizes | **CONFIRMADO** | CGU/gov.br confirmam: art. 18 institui os conselhos como **órgãos consultivos** com essas atribuições (inclui acompanhar e avaliar a atuação da ouvidoria). Texto literal dos arts. 19–21 não lido direto, mas a natureza e atribuições estão confirmadas. |

### Decreto 9.094/2017 — Carta de Serviços

| # | Afirmação na pesquisa | Classificação | Base / ressalva |
|---|---|---|---|
| 2.0 | Decreto vincula **Executivo federal**; para municípios a obrigação da Carta decorre da **Lei 13.460 art. 7º** (nacional); 9.094 é **modelo de conteúdo** | **CONFIRMADO** | CGU confirma: Decreto institui a Carta para "órgão ou entidade do **Poder Executivo federal**". A obrigação municipal de manter Carta vem da Lei 13.460 (nacional). A pesquisa marca corretamente como `[a confirmar]` a adoção local do 9.094. **Distinção juridicamente correta e bem feita.** |
| 2.1 | Conteúdo obrigatório por serviço: serviços, requisitos/**documentos**, **prazos**, forma de prestação, locais, **tempo de espera**, **canais de reclamação**, **acessibilidade**, limpeza/conforto, prioridades | **CONFIRMADO** | CGU lista exatamente: serviços oferecidos, requisitos e documentos, prazos, forma de prestação, locais, tempo de espera, canais de reclamação, condições de acessibilidade, limpeza e conforto, entre outras. |
| 2.2 | Formulário **"Simplifique!"** + divulgação permanente | **CONFIRMADO** | CGU confirma o Simplifique! (no e-Ouv/Fala.BR) e a divulgação. Ressalva: a divulgação no **servicos.gov.br** é obrigação **federal**; para município, o canal de publicação é o portal do ente. |

### Login gov.br — autenticação

| # | Afirmação na pesquisa | Classificação | Base / ressalva |
|---|---|---|---|
| 3.0 | Três níveis (bronze/prata/ouro) sobre **OIDC 1.0 / OAuth 2.0** | **CONFIRMADO** | gov.br (Governo Digital) confirma os 3 selos; acesso.gov.br confirma OIDC sobre OAuth 2.0. |
| 3.1 | **Bronze**: validação Receita Federal **ou** INSS, ou presencial (INSS/Balcão). Básico. | **CONFIRMADO** | gov.br: formulário online validado na Receita ou INSS, ou presencial em Agências INSS / Balcão gov.br. Segurança "Básico". |
| 3.2 | **Prata**: facial CNH/Senatran **ou** internet banking credenciado **ou** SIGEPE; libera documentos digitais + assinatura gratuita + 2FA | **CONFIRMADO** | gov.br confirma as 3 vias e os recursos (visualizar/compartilhar documentos, assinatura eletrônica gratuita, 2FA). Segurança "Alto". |
| 3.3 | **Ouro**: facial Justiça Eleitoral/TSE **ou** QR da CIN **ou** Certificado ICP-Brasil; nível máximo | **CONFIRMADO** | gov.br confirma as 3 vias e "maior grau de confiabilidade". Segurança "Máximo". |
| 3.4 | Integração municipal: **adesão à Rede Nacional de Governo Digital**, RP por OIDC, solicitação formal por produto, credenciais de homologação→produção | **CONFIRMADO** | acesso.gov.br (roteiro técnico, atualizado 30/10/2025): disponível a entes federais/estaduais/**municipais**; exige adesão prévia à Rede gov.br; solicitação por agente público; homologação→produção. **Achado extra:** o próprio agente solicitante precisa logar com conta **prata ou ouro** para abrir o pedido de integração. |
| 3.5 | Precedente fazendário exigindo prata/ouro: **portal Regularize (2026)** | **CONFIRMADO** | gov.br/Fazenda: a partir de **15/06/2026** PF precisa de conta **prata ou ouro** no Regularize, justificativa = maior segurança na validação dos dados. |
| 3.6 | Gating de nível por serviço (consulta=bronze/anônimo; protocolo/ouvidoria identificada=prata; ato jurídico forte=ouro) | **PLAUSÍVEL** (recomendação de produto) | Não é norma; é decisão de arquitetura coerente com o que o gov.br exige por analogia. A pesquisa já marca `[a confirmar política do ente]`. Correto manter como recomendação, não como obrigação legal. |

### Acessibilidade — eMAG / WCAG

| # | Afirmação na pesquisa | Classificação | Base / ressalva |
|---|---|---|---|
| 4.0 | eMAG versão atual **3.1**, baseado/compatível com WCAG (não cobre 100% do WCAG 2.0) | **CONFIRMADO** | governoeletronico.gov.br: eMAG 3.1 lançado em abril/2014; não abrange todos os critérios do WCAG 2.0, mas não exclui aplicar boas práticas WCAG. |
| 4.1 | Obrigatoriedade do eMAG via **Portaria SLTI nº 3, de 07/05/2007** (institui no SISP) | **CONFIRMADO** (com escopo) | governoeletronico.gov.br confirma a Portaria 3/2007 tornando o eMAG obrigatório. **Ressalva crítica de escopo:** essa obrigatoriedade vincula os sítios do **governo federal / SISP**, **não** vincula diretamente municípios. Para o município, a obrigação de acessibilidade vem da LBI (abaixo), não da Portaria SLTI. |
| 4.2 | Reforço legal: **LBI (Lei 13.146/2015) art. 63** — acessibilidade obrigatória em sítios de órgãos de governo | **CONFIRMADO** (corrige a fonte e a leitura) | Texto literal do art. 63 obtido em domínio oficial: *"É obrigatória a acessibilidade nos sítios da internet mantidos por empresas... ou por órgãos de governo... conforme as melhores práticas e diretrizes de acessibilidade adotadas **internacionalmente**."* **Dois pontos:** (a) a pesquisa citava o art. 63 por fonte secundária (jcmgroup) — agora confirmado em fonte oficial, pode remover o `[a confirmar]`; (b) o art. 63 referencia **diretrizes internacionais (WCAG)**, não o eMAG — logo o vínculo legal direto do município é com **WCAG**, e o eMAG é o padrão federal/recomendado, não imposição legal ao ente municipal. §1º exige símbolo de acessibilidade em destaque. |
| 4.3 | Front-end nascer eMAG 3.1 + WCAG 2.1 AA (recomendação) | **PLAUSÍVEL** | Decisão de produto sólida; AA é o patamar de mercado/CRT. A pesquisa já marca `[a confirmar AA vs AAA]`. Não há na LBI/art. 63 fixação literal de "AA" — vem das diretrizes internacionais referenciadas. |

---

## Placar

- **CONFIRMADO:** 17 afirmações (1.0, 1.1, 1.3, 1.4, 1.5, 1.6, 2.0, 2.1, 2.2, 3.0, 3.1, 3.2, 3.3, 3.4, 3.5, 4.0, 4.1, 4.2 — nota: 4.1 e 4.2 confirmados com ressalva de escopo).
- **PLAUSÍVEL (não verificável como norma / decisão de produto):** 3 (1.2, 3.6, 4.3).
- **INCERTO / não confirmado nesta auditoria:** 0 afirmações foram refutadas. Permanecem **não lidos diretamente no caput oficial** (mas confirmados por fonte oficial secundária): texto literal dos arts. 1º, 5º, 7º, 19–21 da Lei 13.460 e arts. 11–12 do Decreto 9.094 — a leitura direta do Planalto/Imprensa Nacional caiu por conexão. Recomenda-se nova tentativa de WebFetch ao Planalto antes do build para fixar o texto literal.

Nenhuma afirmação da pesquisa foi **falsificada**. Três correções de precisão (não de fato):
1. Escalonamento de vigência é **binário (100k hab.)**, não multi-faixa.
2. Obrigatoriedade do eMAG via Portaria SLTI 3/2007 é **federal/SISP**; município se prende à **LBI art. 63 → WCAG**.
3. O `[a confirmar]` do art. 63 (item 4.2 / pendência §6.6 da pesquisa) pode ser **removido**: confirmado em fonte oficial.

---

## 3 riscos principais para o M8

1. **Risco regulatório de habilitação do Login Único (bloqueante de cronograma).** Ser RP gov.br
   **não é só técnico**: exige adesão formal do ente à Rede Nacional de Governo Digital + pedido por
   produto + ciclo homologação→produção, e o próprio agente solicitante precisa de conta prata/ouro.
   Se o município-piloto (Maximiliano de Almeida/RS) **não tiver aderido**, o portal não autentica via
   gov.br no go-live. **Mitigação:** confirmar status de adesão (pendência §6.2) **agora**, e desenhar
   fallback de login local por CPF como contingência — mas tratá-lo como exceção auditada, nunca como
   primário (CLAUDE.md §6: AuthN forte, negar por padrão).

2. **Risco de exposição legal por acessibilidade subdimensionada.** A LBI art. 63 é **lei nacional**
   que obriga o município, com referência a "melhores práticas internacionais" (WCAG) — descumprir é
   passível de questionamento (MP/judicial) e fere o CLAUDE.md §13 (WCAG 2.1 AA obrigatório, sem tela
   fora do padrão). Tratar acessibilidade como item de QA bloqueante, não cosmético; incluir VLibras e
   símbolo de acessibilidade em destaque (§1º do art. 63). O app móvel "Minha Cidade" entra no mesmo
   escopo e é frequentemente esquecido.

3. **Risco de não-conformidade com o ciclo obrigatório do art. 23 (avaliação + publicação).** A Lei
   13.460 exige pesquisa de satisfação **≥ anual** e **publicação integral** dos resultados com ranking
   de reclamações — isso é **funcionalidade obrigatória**, não opcional de BI. Se o M8 entregar portal
   de serviços sem o motor de avaliação/satisfação vinculado a cada serviço da Carta e sem a publicação
   automática, o ente fica em descumprimento legal já no dia 1. **Mitigação:** modelar a Carta de
   Serviços como **catálogo (dados estruturados)** com pesquisa de satisfação acoplada por serviço
   (alinhado à "Implicação para o produto" da própria pesquisa) e publicação automatizada na
   Transparência — multi-tenant e auditável (CLAUDE.md §4/§5/§6).
