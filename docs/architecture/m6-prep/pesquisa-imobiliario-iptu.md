# Pesquisa — Cadastro Imobiliário, PGV e IPTU (M6 Tributos)

> Piloto: Maximiliano de Almeida/RS — administração pública direta municipal (Executivo).
> Regra de ouro (CLAUDE.md §16): toda afirmação factual abaixo tem FONTE (URL oficial) OU está marcada `[a confirmar — obter doc oficial]`.
> Regra §16/§7: alíquotas, PGV, fatores e regras de desconto/parcelamento são **lei municipal** → **parametrizáveis por tenant**, NUNCA hardcoded.
> Data da pesquisa: 2026-06.

---

## 1. Cadastro Imobiliário — três camadas de identificação

O imóvel urbano no M6 carrega **três identificadores distintos** que precisam coexistir no agregado de cadastro:

| Identificador | Origem | Natureza | Observação |
|---|---|---|---|
| **Inscrição Municipal (cadastral)** | Prefeitura (cadastro de origem) | Local, formato livre por município | É a chave histórica do IPTU; sempre existirá no cadastro local. |
| **CIB — Cadastro Imobiliário Brasileiro** | Receita Federal via SINTER | Nacional, único | Código alfanumérico **ABC1234-5** (7 caracteres + dígito verificador). O "CPF do imóvel". |
| **Matrícula do registro de imóveis (RGI)** | Cartório (CRI) | Por imóvel registrado | Vínculo dominial; compartilhada com o SINTER pelos cartórios. |

- **CIB** é "um banco de dados cadastrais que reúne dados de todos os imóveis do país – urbanos, rurais, públicos e privados"; a parte urbana "reúne dados cadastrais de imóveis enviados ao Sinter **pelas prefeituras municipais**". Para o imóvel obter CIB, a prefeitura mantém o cadastro atualizado e **envia os dados ao SINTER, que gera o código**. Formato **ABC1234-5**.
  - Fonte: https://www.gov.br/receitafederal/pt-br/acesso-a-informacao/acoes-e-programas/programas-e-atividades/sinter/cib
  - Fonte (FAQ): https://www.gov.br/receitafederal/pt-br/acesso-a-informacao/perguntas-frequentes/cadastros/cib
- **Base legal:** Decreto **11.208/2022** (regulamenta SINTER e CIB) + **IN RFB nº 2.275/2025** (18/08/2025) — institui regras estruturais de gestão das informações de imóveis e introduz estimativa de **valor de referência** de mercado para imóveis urbanos (relevante p/ Reforma Tributária/IBS imobiliário).
  - Fonte: https://www.gov.br/receitafederal/pt-br/acesso-a-informacao/acoes-e-programas/programas-e-atividades/sinter/legislacao
  - Fonte (análise IN 2.275/2025): https://www.migalhas.com.br/depeso/441583/reforma-tributaria-47-cib-e-a-instrucao-normativa-rfb-2-275-25
  - Fonte (CNM): https://cnm.org.br/comunicacao/noticias/decreto-traz-regulamentacao-do-sinter-e-cib-para-os-cadastros-de-imoveis

### 1.1 Cronograma de obrigatoriedade (CIB nos sistemas)
- **Capitais + DF + órgãos federais + cartórios:** a partir de **jan/2026** (prazo de adaptação ~ago/2026 conforme leitura da IN).
- **Demais municípios (inclui Maximiliano de Almeida) + órgãos estaduais:** a partir de **jan/2027** (~ago/2027).
  - Fonte: https://www.gov.br/receitafederal/pt-br/acesso-a-informacao/acoes-e-programas/programas-e-atividades/sinter/cib
  - Fonte (CNM SINTER/CIB): https://reformatributaria.cnm.org.br/sinter-cib-e-a-reforma-tributaria/
- **Implicação M6:** o piloto NÃO é capital → janela de obrigatoriedade jan/2027. O agregado deve **suportar CIB como atributo opcional/nullable** no go-live e o envio ao SINTER deve ser **integração passível de ativação por tenant** (igual filosofia do NfseSync §8: ACL + idempotência + Outbox).
  - `[a confirmar — obter doc oficial]` Protocolo técnico EXATO de envio prefeitura→SINTER (leiaute/serviço, XSD, REST/SOAP, formato do lote cadastral) e regras do dígito verificador do CIB — IN 2.275/2025 e artigos não detalham; obter manual técnico do SINTER/convênio ENAT.
  - Fonte ENAT (convênios/orientações): https://www.enat.receita.economia.gov.br/pt-br/area_nacional/areas_interesse/sinter/celebracao-de-convenios-orientacoes-gerais
  - Fonte CNM (NT CTAT 05/2025 orientações a municípios): https://cnm.org.br/biblioteca/exibe/15927

---

## 2. BCI — Boletim de Cadastro Imobiliário (atributos do agregado urbano)

O BCI consolida os dados cadastrais do imóvel na prefeitura. Campos recorrentes nos manuais municipais (Contagem/MG, Canoas/RS, AC):

- **Localização/zona:** logradouro, setor/quadra/lote, zona fiscal, face de quadra.
- **Terreno:** área do terreno, **testada** (frente), formato/topografia/pedologia, situação na quadra.
- **Construção:** área construída, **tipo**, **padrão** construtivo, **uso** (residencial/comercial/etc.), ano de construção (p/ depreciação), estado de conservação.
- **Condomínio:** **fração ideal** (quando aplicável a unidades autônomas).
- **Saneamento/infraestrutura:** água, esgoto, energia, pavimentação (entram como fatores de PGV).
  - Fonte (manual Contagem/MG): https://receita.contagem.mg.gov.br/downloads/manual_cadastro_imobiliario.pdf
  - Fonte (Canoas/RS — mesmo estado do piloto): https://www.canoas.rs.gov.br/servicos/bci/
- **Modelagem M6:** novo agregado **CadastroImobiliario** (schema `tributos`, `IMustHaveTenant`), com VO de identificação (InscricaoMunicipal, CibCodigo opcional, MatriculaRgi opcional) e dados do BCI como entidades/VOs. Liga-se ao `Contribuinte` existente como **sujeito passivo** (proprietário/possuidor) e é a origem do `Lancamento` de `TipoTributo.Iptu` (enum já existe em `Lancamento.cs`).

---

## 3. PGV (Planta Genérica de Valores) e base de cálculo do IPTU

- **PGV** é o instrumento técnico-jurídico que fixa, **por lei municipal**, o valor venal dos imóveis urbanos: define **valor unitário (R$/m²)** de terreno e de construção por zona/setor + fatores de correção. Base de cálculo do IPTU = **valor venal** (CTN art. 33). Serve também a ITBI e Contribuição de Melhoria.
  - Fonte: https://www.geopixel.com.br/planta-generica-de-valores/
  - Fonte (revisão periódica — recomendação MP ~4 anos): idem.
- **Fórmula-padrão do valor venal** (genérica; cada município ajusta):

  ```
  ValorVenal = (AreaTerreno × VUT × fatores_terreno)
             + (AreaConstruida × VUC × FatorPadrao × FatorDepreciacao × fatores_localizacao)
  ```

  Exemplo de fatores reais (Salvador): VUT, FVT (valorização), FCT (condições do terreno), VUC, FL (localização), FDC (depreciação da construção por idade+conservação), FAV.
  - Fonte (fatores Salvador): https://www2.sefaz.salvador.ba.gov.br/servico/calculo-iptu
  - Fonte (decomposição da fórmula): https://www.tarjab.com.br/blog/escritura-e-documentacoes/valor-venal-do-imovel-o-que-e-e-como-calcular/
  - Fonte (anexo de lei municipal — apuração do valor venal): https://www.dinamicasistemas.com.br/upload/files/Lei%206_895-2022_Anexo%20I.pdf
- **Modelagem M6 — motor de cálculo parametrizável:** PGV como **tabela versionada por tenant + exercício** (vigência anual): tabelas de VUT/VUC por zona, tabelas de fatores (padrão, depreciação por faixa de idade, localização, uso). O motor de valor venal NÃO embute números — lê a PGV vigente do exercício. Alíquotas idem (ver §4).

---

## 4. Lançamento anual do IPTU, alíquotas, descontos, parcelamento/carnê (DAM)

- **Lançamento anual de ofício:** IPTU é lançado uma vez por exercício; gera `Lancamento` (TipoTributo.Iptu) por imóvel, com vencimentos do calendário fiscal municipal. O agregado `Lancamento` já existe (`Lancar(...)`, `RegistrarPagamento()`, situações Aberto/Pago/InscritoEmDividaAtiva/Cancelado) — falta o **parcelamento (cota única × N parcelas)** e o vínculo ao imóvel/cadastro.
- **Alíquotas:** **lei municipal**; podem ser **progressivas** (por valor venal e/ou por uso/edificado×territorial) e progressividade no tempo (função social, EC 29/Estatuto da Cidade). → parametrizável por tenant.
- **Cota única × parcelamento:** prática comum **até ~10 parcelas mensais**, com **valor mínimo de parcela** (ex.: R$ 50,00 em SP) e **desconto na cota única / pagamento à vista** (varia muito: SP 3%, Recife 5%, Natal até 16% p/ adimplente). Tudo definido em **decreto/lei municipal** anual.
  - Fonte (SP — parcelas/mínimo/desconto): https://prefeitura.sp.gov.br/web/fazenda/w/servicos/iptu/25037
  - Fonte (calendário/edital SP 2026): https://prefeitura.sp.gov.br/web/fazenda/w/editaldoiptu2026
  - Fonte (Recife desconto cota única): https://recifeemdia.recife.pe.gov.br/node/1856
  - Fonte (Natal): https://blog.emobiimobiliaria.com.br/iptu-natal/
- **DAM (Documento de Arrecadação Municipal) / carnê:** documento de pagamento (boleto bancário) por parcela; emissão de 2ª via/segunda parcela tende a portal. → no M6, **geração de DAM por parcela** com nosso layout, integrável ao financeiro/arrecadação.
  - Fonte: https://www2.sefaz.salvador.ba.gov.br/servico/perguntas-frequentes-iptu

### 4.1 O que é LEI MUNICIPAL → parametrizar por tenant (resumo)
- PGV completa (VUT/VUC por zona, todos os fatores) e periodicidade de revisão.
- Alíquotas IPTU (progressividade por valor/uso, territorial × predial).
- Calendário fiscal anual (data de lançamento, vencimentos, nº máximo de parcelas, valor mínimo de parcela).
- Percentuais e condições de desconto (cota única, adimplência) e isenções/imunidades.
- Atualização monetária da base (índice) e regras de correção/multa/juros do crédito.

---

## 5. Pendências `[a confirmar — obter doc oficial]`
1. **Protocolo técnico SINTER↔Prefeitura** (leiaute/XSD/serviço de envio do cadastro urbano e geração do CIB; regra do DV) — obter manual técnico SINTER / convênio ENAT / NT CTAT 05/2025.
2. **Legislação tributária de Maximiliano de Almeida/RS** — obter Código Tributário Municipal + lei da PGV + decreto anual do IPTU (alíquotas, calendário, descontos, parcelas) para popular os parâmetros do tenant piloto. NÃO inventar valores.
3. **Estimativa de valor de referência (IN 2.275/2025)** — verificar se/como impacta a base de cálculo municipal vs. base IBS da Reforma Tributária (LC 214/2025) no horizonte do go-live.
4. **Vínculo CIB↔matrícula↔inscrição** no nosso modelo (cardinalidade: unidade autônoma, desmembramento/remembramento) — confirmar regras de unicidade do CIB por unidade imobiliária.
