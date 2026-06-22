# Estudo — Mapa Estadual & Controle (TCE-RS, controle externo/interno, MP, SEFAZ-RS)

> Parte do estudo de auditoria do **Tensorroot.Gov**. Foco: nível **ESTADUAL** (RS) e de **CONTROLE**.
> Escopo: TCE-RS e seus sistemas (SIAPC/PAD, e-Validador, LicitaCon, SICOE); arquitetura constitucional
> de controle (CF arts. 31/70/74); relação Executivo ↔ Legislativo ↔ TCE ↔ Ministério Público ↔ Controladoria
> Interna; SEFAZ-RS / Tesouro-RS (transferências e convênios estaduais); encaixe controle externo ↔ interno.
> Convenção `[OFICIAL]` / `[a confirmar]` conforme CLAUDE.md §16. Piloto: Maximiliano de Almeida/RS.

---

## 1. Arquitetura constitucional do controle (federal → ente)

### 1.1 CF art. 70 — fiscalização e seu objeto
A fiscalização **contábil, financeira, orçamentária, operacional e patrimonial** da União e das entidades
da administração direta e indireta, quanto a **legalidade, legitimidade, economicidade**, aplicação de
subvenções e renúncia de receitas, é exercida pelo **Congresso Nacional, mediante controle externo, e pelo
sistema de controle interno de cada Poder**. Parágrafo único: presta contas qualquer pessoa física/jurídica,
pública/privada, que **utilize, arrecade, guarde, gerencie ou administre** dinheiros, bens e valores públicos.
Por simetria, o modelo se reproduz em Estados e Municípios. `[OFICIAL]` CF/1988 art. 70.

### 1.2 CF art. 31 — controle no Município (espinha dorsal do piloto)
- A fiscalização do Município é exercida pelo **Poder Legislativo Municipal (controle externo)** e pelos
  **sistemas de controle interno do Poder Executivo Municipal**, na forma da lei.
- O **controle externo da Câmara é exercido COM O AUXÍLIO do Tribunal de Contas** do Estado (ou do Município,
  onde houver). No RS o auxílio é do **TCE-RS** (não há TCM no RS). `[OFICIAL]` CF art. 31, §1º.
- **Parecer prévio** do TCE-RS sobre as contas anuais do Prefeito **só deixa de prevalecer por decisão de
  2/3 dos membros da Câmara**. Ou seja, o TCE emite o parecer técnico; **quem JULGA as contas de governo do
  Prefeito é a Câmara** (juízo político-administrativo). `[OFICIAL]` CF art. 31, §2º.
- Contas dos Municípios ficam **60 dias à disposição de qualquer contribuinte** para exame/apreciação
  (art. 31, §3º). É **vedada a criação de Tribunais/Conselhos/órgãos de contas municipais** (§4º) — só RJ e SP
  mantêm TCMs preexistentes. `[OFICIAL]` CF art. 31, §§3º-4º.

> **Nuance que o sistema deve respeitar:** há DUAS espécies de contas — (a) **contas de governo** (anuais,
> globais do Prefeito → parecer prévio do TCE + julgamento pela Câmara); e (b) **contas de gestão**
> (ordenador de despesa → julgadas diretamente pelo TCE, com decisão de mérito própria). O Tensorroot.Gov
> alimenta a base probatória de ambas (remessas SIAPC + contas anuais/DCASP).

### 1.3 CF art. 74 — sistema de controle INTERNO (Controladoria)
- Cada Poder mantém **sistema de controle interno integrado** para: avaliar cumprimento de metas do PPA/LDO/LOA;
  comprovar legalidade e avaliar resultados (eficácia/eficiência) da gestão; controlar operações de crédito,
  avais e garantias; e **apoiar o controle externo**. `[OFICIAL]` CF art. 74, I-IV.
- **§1º (responsabilidade SOLIDÁRIA):** os responsáveis pelo controle interno, **ao tomarem conhecimento de
  irregularidade ou ilegalidade, dela darão ciência ao Tribunal de Contas, SOB PENA DE RESPONSABILIDADE
  SOLIDÁRIA**. Logo, a Controladoria Interna do município **NÃO é mera auditoria interna — é canal obrigatório
  de comunicação ao TCE-RS**. `[OFICIAL]` CF art. 74, §1º.
- **§2º:** qualquer cidadão/partido/associação/sindicato é parte legítima para **denunciar irregularidades
  ao Tribunal de Contas**. `[OFICIAL]` CF art. 74, §2º.

> **Implicação de produto:** o módulo de controle interno (controladoria) deve produzir o **RCI / parecer do
> controle interno** anexado às contas e registrar formalmente a "ciência ao TCE" (trilha imutável — alinhado
> ao §1º). Isso conecta com a auditoria imutável já existente.

---

## 2. Atores e relações (Executivo ↔ Legislativo ↔ TCE ↔ MP/MPC ↔ Controladoria)

| Ator | Papel no controle | Relação com o ente fiscalizado |
|---|---|---|
| **Poder Executivo (Prefeitura)** | Fiscalizado (contas de governo + gestão); mantém **controle interno** próprio (art. 74) | Tenant produtor das remessas; presta contas ao TCE-RS e ao SICONFI |
| **Poder Legislativo (Câmara)** | **Titular do controle externo** (art. 31); **julga** contas de governo do Prefeito (parecer prévio só cai por 2/3) | É **tenant/CNPJ próprio** → presta SUAS contas ao TCE-RS (folha, duodécimo art. 29-A CF, limites) |
| **TCE-RS (Tribunal de Contas)** | Órgão **auxiliar** do controle externo; recebe remessas, **emite parecer prévio** (contas de governo) e **julga** contas de gestão; fiscaliza, instaura processos, aplica sanções/multas | Recebe SIAPC/PAD, LicitaCon, SICOE, contas anuais via e-Validador/e-TCERS |
| **Ministério Público de Contas (MPC-RS)** | **MP ESPECIAL** que atua **perante o TCE-RS** (custos legis): parecer em sessões, recursos, **representações** ao Tribunal; pode atuar **diretamente** junto a jurisdicionados. **Não** integra MPU nem MPE | Atua dentro dos processos do TCE-RS; órgão autônomo |
| **Ministério Público do Estado (MP-RS / MPE)** | MP **comum**: ação civil pública, improbidade (Lei 8.429/92 c/ LGL 14.230/21), ação penal; usa os achados/contas do TCE como prova | Externo ao TCE; recebe representações; pode requisitar dados ao ente |
| **Controladoria/Controle Interno municipal** | Sistema de controle interno (art. 74); **apoia** o controle externo e **comunica irregularidades ao TCE sob pena de solidariedade** | Interno ao Executivo; emite RCI/parecer anexo às contas |
| **SEFAZ-RS / Tesouro-RS** | Repassa **transferências constitucionais/legais** (cota-parte ICMS, IPVA, etc.) e firma **convênios** estaduais | Fonte de receita do ente; exige prestação de contas em convênios |

> **Resumo do circuito:** o **Executivo** executa e registra (PCASP) → **Controle interno** valida e comunica →
> remessas vão ao **TCE-RS** → **MPC** atua dentro do processo → **TCE** emite parecer prévio / julga →
> **Câmara** julga as contas de governo → achados de irregularidade podem alimentar **MPE/MPF** (improbidade,
> penal) e o **cidadão** (art. 74 §2º) pode denunciar diretamente ao TCE.

---

## 3. TCE-RS — sistemas de controle externo (o que o Tensorroot.Gov deve alimentar)

> Todos os jurisdicionados (administração direta e indireta, consórcios públicos) são obrigados a remeter
> informações nos **leiautes**, **periodicidade** e **formato** definidos pelo TCE-RS. `[OFICIAL]`

### 3.1 SIAPC — Sistema de Informações para Auditoria e Prestação de Contas + PAD
- **Objeto:** execução **orçamentária, financeira, contábil e patrimonial** (saldos PCASP, empenho → liquidação →
  pagamento, restos a pagar, receita prevista→arrecadada, dívida ativa). Base da Lei 4.320/64.
- **PAD (Programa Autenticador de Dados):** programa do TCE que **valida e autentica** os arquivos da remessa,
  gera o **RVE — Relatório de Validação e Encaminhamento** (inclui informações da RCL e dos RGF/RREO).
- **Formato:** arquivo-texto de **largura fixa**, 1 registro por linha, CR/LF, **sem campos packed/binário**;
  **Código de Remessa único**; validação local no PAD antes do envio.
- **Periodicidade:** **mensal** (execução), prazo usual **30 dias corridos** após o mês de referência.
- **Leiautes:** **MT-ASCE** (Manual Técnico), Volumes IV/V (a versão do PAD evolui por exercício — confirmar
  versão vigente). Fundamento normativo da publicação eletrônica/RREO/RGF: **IN TCE-RS 6/2019** e atualizações
  (ex.: IN 4/2021), em apoio à LRF (LC 101/2000). `[OFICIAL]` MT-ASCE Vol. IV/V; IN 6/2019.

### 3.2 Contas anuais / ordinárias (DCASP)
- **Objeto:** Balanço Geral + **DCASP (7 demonstrações)** + documentos de encerramento.
- **Periodicidade:** **anual**. Disciplina pela **Res. TCE-RS 1134/2020** `[datas a confirmar por exercício]`.
- É a base do **parecer prévio** (contas de governo) e do **julgamento** (contas de gestão). `[OFICIAL]`

### 3.3 LicitaCon — Sistema de Licitações e Contratos
- **Objeto:** licitações, dispensas, inexigibilidades e respectivos **contratos/aditivos**.
- **Dois módulos:** **LicitaCon Web** (entidade sem sistema próprio, digita online) e **LicitaCon e-Validador**
  (entidade com ERP próprio/terceiro → **exporta arquivos** no leiaute e valida/transmite).
- **Disciplina:** Resoluções **1050/2015** e **1073/2017**; prazos/regras técnicas na **IN TCE-RS 13/2017**.
- **Fluxo e-Validador:** sistema-origem gera arquivos (informações + documentos) → módulo e-Validador valida →
  gera e transmite remessa → rotina do TCE carrega no LicitaCon → gera **RVE por remessa** vinculado a um único
  **e-Protocolo** → administrador **assina digitalmente** no **e-TCERS** (prazo ~30 dias após o 5º dia útil do
  mês subsequente). `[OFICIAL]` Res. 1050/2015, 1073/2017; IN 13/2017.

### 3.4 SICOE — Sistema de Informações de Custos e Obras (acompanhamento de obras)
- **Objeto:** acompanhamento de **obras públicas** (medições, contratos de obra). `[a confirmar escopo exato]`
- **Fluxo:** instalar o sistema, gerar arquivos no **leiaute padronizado** do TCE, configurar **certificado
  digital** e diretórios, validar e **transmitir** com geração de **RVE**.
- **Periodicidade:** **trimestral** (até o último dia útil do mês seguinte) `[a confirmar]`. `[OFICIAL]` Manual SICOE.

### 3.5 e-Validador (transversal) e e-TCERS
- O **e-Validador** é a camada comum de **validação local + geração de remessa + RVE + transmissão segura**
  usada por LicitaCon e SICOE (e equivalente conceitual ao PAD para a remessa contábil do SIAPC).
- O **e-TCERS** é o ambiente de **protocolo e assinatura digital** (certificado ICP-Brasil) das remessas.
- **Atenção operacional:** o TCE-RS alterou o **envio de remessas ao LicitaCon e ao SICOE via e-Validador** —
  acompanhar comunicados (mudança de fluxo/versão pode quebrar integração). `[OFICIAL]` portalnovo.tce.rs.gov.br.

### 3.6 BLM e legislação municipal
- O ente alimenta o **BLM (Banco de Legislação Municipal)** no site do TCE-RS (apoio à transparência). `[OFICIAL]`

> **Encaixe no Tensorroot.Gov:** o módulo **Transparência** é o **TRANSMISSOR** — gera as remessas a partir do
> **Balancete/MSC** dos demais módulos, **assina (cofre A1 / envelope encryption)** e transmite (SICOE/e-Validador).
> Isso já está mapeado no MAPA-PRESTACAO-CONTAS (linha 61). O que o ERP **emula/produz** internamente é o
> conteúdo da remessa; a **validação canônica** continua no **PAD/e-Validador** do TCE-RS (não substituível).

---

## 4. SEFAZ-RS / Tesouro-RS — receita estadual, transferências e convênios

### 4.1 Transferências constitucionais/legais (não-discricionárias)
- O **Tesouro-RS / SEFAZ-RS** repassa aos municípios: **cota-parte do ICMS (25%)**, **IPVA**, **IPI-Exportação**,
  além de operar a retenção de **FUNDEB (20% do ICMS)** e Salário-Educação. Repasses divulgados mês a mês e
  com **projeção de ingressos** publicada. `[OFICIAL]` Tesouro-RS.
- **Coeficientes de participação** (índice de retorno do ICMS — "Valor Adicionado") são apurados anualmente;
  para 2026 já há coeficientes definidos. STF (tema do ICMS) firmou que o Estado **deve repassar** a parcela
  mesmo em entrada indireta de receita. `[OFICIAL]` STF; SEFAZ-RS.
- **Para o ERP:** essas transferências são **RECEITA** do ente (classificação por natureza, alimenta MSC/SICONFI
  e o RREO). Não há "prestação de contas" da transferência constitucional em si (é obrigatória/automática),
  mas há **conciliação** entre o repassado pelo Estado e o arrecadado/registrado pelo município.

### 4.2 Convênios e transferências voluntárias estaduais (discricionárias)
- Convênios/transferências voluntárias **dependem de instrumento legal** (plano de trabalho, chamamento público
  quando cabível) e **EXIGEM prestação de contas** pela entidade recebedora dos recursos.
- O **Portal da Transparência RS** publica os convênios firmados (concedente, beneficiário, vigência, município,
  tipo de repasse). `[OFICIAL]` transparencia.rs.gov.br.
- **Para o ERP:** convênio estadual → controle de **execução vinculada** (fonte/destinação de recurso), guarda
  de comprovantes e **prestação de contas ao concedente estadual** (paralela à prestação ao TCE-RS). Geralmente
  via sistema próprio do concedente/SEFAZ `[a confirmar sistema/portal específico]`.

---

## 5. Como controle EXTERNO e INTERNO se encaixam (fluxo end-to-end)

```
                 (executa e registra — PCASP é a FONTE)
 EXECUTIVO ──────────────────────────────────────────────► Lançamentos → Balancete → MSC
   │  ▲                                                          │
   │  │ apoia / comunica irregularidade (art.74 §1º — SOLIDÁRIO) │ deriva
   ▼  │                                                          ▼
 CONTROLE INTERNO (Controladoria) ── RCI/parecer ────►  Remessas: SIAPC/PAD, LicitaCon,
   │                                                       SICOE, Contas Anuais (DCASP)
   │ ciência obrigatória                                         │ e-Validador + assinatura A1
   ▼                                                             ▼
 TCE-RS  ◄──── MPC-RS (parecer/representação, custos legis) ──── recebe/valida (PAD/e-Validador)
   │   │
   │   └─► CONTAS DE GESTÃO → TCE JULGA (mérito) ──► multa/débito/recursos
   │
   └─► CONTAS DE GOVERNO → PARECER PRÉVIO ──► CÂMARA (Legislativo) JULGA (2/3 p/ rejeitar parecer)
                                                  │
   achados ──► MPE/MPF (improbidade LGL 14.230/21, ação penal) ; cidadão denuncia (art.74 §2º)
```

**Pontos de atenção para a auditoria do produto:**
1. **Dupla titularidade do controle externo:** quem julga **contas de governo** é a **Câmara** (parecer prévio
   do TCE); quem julga **contas de gestão** é o **próprio TCE**. O sistema deve produzir insumos para ambos.
2. **Controle interno = obrigação legal, não opcional** (art. 74 §1º, responsabilidade solidária). O módulo de
   controladoria precisa de **trilha imutável** da ciência ao TCE — alinhado à auditoria imutável (CLAUDE.md §4).
3. **MPC ≠ MPE:** o **MPC-RS** atua **dentro** do TCE; o **MPE/MPF** atua fora (improbidade/penal). O ERP não
   integra com MPC diretamente, mas seus dados podem virar prova em ambos.
4. **A validação canônica é do TCE** (PAD/e-Validador): o Tensorroot.Gov **emula/produz** a remessa e deve
   espelhar os leiautes, mas **não substitui** a validação oficial; risco de quebra a cada nova versão do PAD/
   e-Validador → tratar **versionamento de leiaute** como dependência externa monitorada (Polly/Outbox p/ envio).
5. **Câmara é tenant próprio** (CNPJ próprio): presta contas separadamente (folha, duodécimo art. 29-A CF,
   limites de despesa) — já refletido no MAPA (linhas 8, 57-58).
6. **SEFAZ-RS:** transferências constitucionais entram como **receita** (conciliação), convênios estaduais como
   **execução vinculada + prestação de contas ao concedente** (fluxo distinto do TCE-RS).

---

## 6. Lacunas / a confirmar `[a confirmar]`
- Versão vigente do **PAD/MT-ASCE** para o exercício corrente (numeração e novos registros).
- **Leiaute exato e periodicidade do SICOE** (obras) e dos registros patrimoniais no SIAPC.
- **Datas precisas** das contas anuais (Res. 1134/2020) por exercício.
- **Sistema/portal específico** de prestação de contas de **convênios estaduais** RS (SEFAZ/concedente).
- Detalhes da **mudança recente** no envio LicitaCon/SICOE via e-Validador (impacto de integração).
- Leiaute de remessa do **Legislativo** (Câmara) no SIAPC.

---

## 7. Fontes (oficiais primeiro)
- **CF/1988 — art. 31** (controle municipal / parecer prévio): https://portal.stf.jus.br/constituicao-supremo/artigo.asp?abrirBase=CF&abrirArtigo=31 ; https://normas.leg.br/?urn=urn%3Alex%3Abr%3Afederal%3Aconstituicao%3A1988-10-05%3B1988%21art31
- **CF/1988 — art. 74** (controle interno / responsabilidade solidária): https://www.tce.se.gov.br/Legislacao/Legisla%C3%A7%C3%A3o%20Nacional/DISPOSITIVOS%20DA%20CONSTITUI%C3%87%C3%83O%20FEDERAL%20ALUSIVOS%20AO%20TCU.pdf
- **TCE-RS — Sistemas de controle externo (portal):** https://tcers.tc.br/sistemas-de-controle-externo/ ; https://portalnovo.tce.rs.gov.br/sistemas-de-controle-externo/
- **TCE-RS — SIAPC (portal jurisdicionados):** http://portal.tce.rs.gov.br/portal/page/portal/tcers/jurisdicionados/sistemas_controle_externo/siapc
- **TCE-RS — MT-ASCE Vol. V (leiaute SIAPC):** http://www.tce.rs.gov.br/sistemas_controle/SIAPC/pdf/ResumoLeiauteDadosADisposicao_Siapc_MT_Vol_V_V2.0.pdf ; (Vol. V via MPC) https://mpc.rs.gov.br/repo/SIAPC/MANUAL/MT-ASCE-0105-06-MT-Volume-V.pdf
- **TCE-RS — SIAPC/PAD Perguntas Frequentes:** http://www.tce.rs.gov.br/sistemas_controle/SIAPC/pdf/perguntas_frequentes.pdf
- **TCE-RS — IN 6/2019 (RREO/RGF eletrônicos SIAPC/PAD):** https://atosoficiais.com.br/tcers/instrucao-normativa-n-6-2019 ; **IN 4/2021:** https://atosoficiais.com.br/tcers/instrucao-normativa-n-4-2021
- **TCE-RS — SICOE (portal + Manual):** https://portal.tce.rs.gov.br/portal/page/portal/tcers/jurisdicionados/sistemas_controle_externo/sicoe ; https://tcers.tc.br/repo/cex/sicoe/manual-sicoe.pdf
- **TCE-RS — LicitaCon (IN 13/2017):** https://atosoficiais.com.br/tcers/instrucao-normativa-n-13-2017 ; (portal) http://www1.tce.rs.gov.br/portal/page/portal/tcers/jurisdicionados/sistemas_controle_externo/licitacon/
- **TCE-RS — mudança envio LicitaCon/SICOE via e-Validador:** https://portalnovo.tce.rs.gov.br/noticia/atencao-as-mudancas-no-envio-de-remessas-ao-licitacon-e-sicoe-via-e-validador/
- **Compras Eletrônicas RS — exportação LicitaCon e-Validador:** https://www.compras.rs.gov.br/noticias/1620 ; https://www.compras.rs.gov.br/noticias/1673
- **MPC-RS — perguntas frequentes / atuação:** https://mpc.rs.gov.br/noticia-do-mpc/perguntas-frequentes-mpc/ ; https://mpc.rs.gov.br/
- **MPF — "Me explica: o que é o Ministério Público de Contas":** https://www.mpf.mp.br/pgr/noticias-pgr2/2025/me-explica-mpf-o-que-e-e-como-atua-o-ministerio-publico-de-contas
- **SEFAZ-RS / Tesouro-RS — Transferências aos Municípios:** https://tesouro.fazenda.rs.gov.br/projecao-de-ingressos-de-tributos-estaduais-e-repasses-federais-para-os-municipios ; https://tesouro.fazenda.rs.gov.br/conteudo/8349/detalhamento-da-receita
- **Transparência RS — Transferências e Convênios:** https://www.transparencia.rs.gov.br/municipios/transferencias-aos-municipios/dados/ ; https://www.transparencia.rs.gov.br/faq/
- **STF — repasse de ICMS aos municípios (entrada indireta):** https://noticias.stf.jus.br/postsnoticias/estados-devem-repassar-parcela-do-icms-aos-municipios-mesmo-quando-houver-entrada-indireta-de-receita-decide-stf/
- **Atricon — relações controle interno/externo (visão STF):** https://atricon.org.br/relacoes-entre-os-controles-interno-e-externo-visao-do-stf/

> Documentos internos correlatos: `docs/planejamento/MAPA-PRESTACAO-CONTAS.md` (linhas 17-19, 61), `docs/architecture/contabilidade-pcasp-tce.md`, `CLAUDE.md` §0/§4/§16.
