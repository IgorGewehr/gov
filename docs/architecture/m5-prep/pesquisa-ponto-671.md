# Pesquisa — Ponto Eletrônico (Portaria MTP 671/2021)

Contexto: M5 (RH completo) do Tensorroot.Gov, piloto Maximiliano de Almeida/RS.
Objetivo: levantar requisitos de ponto eletrônico (REP, AFD, AEJ, tratamento de marcações, banco de horas) e a obrigatoriedade no setor público.

REGRA DE OURO (CLAUDE.md §16): toda afirmação factual abaixo tem FONTE (URL) ou está marcada `[a confirmar — obter doc oficial]` com o nome do documento. Os blogs de fabricantes (Solides, Topdata, Ortep, TOTVS, UsePonto, Pontotel) são fontes SECUNDÁRIAS — usados para orientação, mas o leiaute oficial e os Anexos DEVEM ser obtidos do texto oficial da Portaria antes de qualquer implementação.

---

## 1. Base legal e revogações

- A Portaria MTP nº 671, de 8 de novembro de 2021, regulamenta disposições da CLT relativas a relações de trabalho, incluindo o registro eletrônico de jornada. Em matéria de ponto, unificou e **revogou as Portarias MTE nº 1.510/2009 e nº 373/2011**. Publicada/disponível em fontes secundárias; **texto oficial no DOU (in.gov.br)**.
  - Fonte (secundária): https://www.normaslegais.com.br/legislacao/portaria-mtp-671-2021.htm
  - Fonte (secundária): https://solides.com.br/blog/portaria-671/
  - `[a confirmar — obter doc oficial]` texto integral e artigos exatos: **Portaria MTP nº 671/2021, publicação no DOU (https://www.in.gov.br/web/dou)** + página oficial gov.br Trabalho e Emprego.

## 2. Tipos de REP (Registrador Eletrônico de Ponto)

A Portaria 671 define três tipos de REP:

- **REP-C (Convencional):** equipamento de hardware que recebe a marcação e **imprime comprovante** ao trabalhador. Exige certificação/homologação do fabricante (Anexo de requisitos técnicos + Portaria INMETRO).
- **REP-A (Alternativo):** software, hardware ou híbrido, **só pode ser usado mediante autorização em CCT/ACT** (acordo/convenção coletiva). Não exige homologação do Ministério; componentes de software exigem registro INPI.
- **REP-P (por Programa):** sistema de registro **por software**, permite marcação inclusive mobile; exige apenas registro de programa de computador no INPI; **não exige homologação do Ministério**. Inclui coletores de marcação, armazenamento e o programa de tratamento (PTRP).
  - Fonte (oficial — Perguntas e Respostas REP, gov.br): https://www.gov.br/trabalho-e-emprego/pt-br/assuntos/inspecao-do-trabalho/fiscalizacao-do-trabalho/Perguntas%20e%20Respostas%20REP
  - Fonte (secundária): https://www.topdata.com.br/portaria-671-do-ministerio-do-trabalho/

**Assinaturas digitais (relevante para o serviço A1 server-side em construção):**
- Comprovantes do trabalhador gerados por REP-P: padrão **PAdES** (PDF).
- AFD e AEJ gerados por REP-P e REP-A: assinatura **CAdES** (CMS), **detached (.p7s)**.
  - Fonte (oficial — P&R REP, gov.br): https://www.gov.br/trabalho-e-emprego/pt-br/assuntos/inspecao-do-trabalho/fiscalizacao-do-trabalho/Perguntas%20e%20Respostas%20REP
  - Nota de integração: alinha com o serviço de assinatura A1 (envelope encryption) do M2/M5 — a assinatura CAdES/PAdES de ponto pode reaproveitar a mesma infraestrutura de A1 server-side.

## 3. Arquivos fiscais obrigatórios

### 3.1 AFD — Arquivo Fonte de Dados
- Registro **bruto, cronológico e imutável** de todas as marcações; não pode ser editado.
- Formato **texto ASCII (ISO 8859-1)**; cada linha = um registro, terminada por CR+LF (caracteres 13 e 10), com **NSR (Número Sequencial de Registro)** obrigatório.
- Validação por **CRC-16 (CCITT)**.
- Contém: identificação do empregador, identificação do empregado (CPF/PIS), data e horário exato da marcação, tipo de registro.
- **Novo leiaute de AFD vigente para REPs a partir de 10/02/2022** (inclui CPF).
  - Fonte (oficial — P&R REP, gov.br): https://www.gov.br/trabalho-e-emprego/pt-br/assuntos/inspecao-do-trabalho/fiscalizacao-do-trabalho/Perguntas%20e%20Respostas%20REP
  - Fonte (secundária): https://solides.com.br/blog/afd-e-aej/

### 3.2 AEJ — Arquivo Eletrônico de Jornada
- Gerado pelo **PTRP (Programa de Tratamento de Registro de Ponto)** — conjunto de rotinas que trata as marcações do AFD e gera o espelho de ponto + o AEJ.
- **Substitui os antigos AFDT e ACJEF** da Portaria 1.510.
- Definido no **Anexo VI** da Portaria 671.
- Formato **texto ASCII (ISO 8859-1)**; cada linha = um registro, terminada por CR+LF. Mesmos requisitos de assinatura CAdES do AFD.
- Contém pós-processamento: jornada contratada (em minutos), presenças, ausências, horas extras, atrasos, compensações.
  - Fonte (oficial — P&R REP, gov.br): https://www.gov.br/trabalho-e-emprego/pt-br/assuntos/inspecao-do-trabalho/fiscalizacao-do-trabalho/Perguntas%20e%20Respostas%20REP
  - Fonte (secundária): https://useponto.com.br/blog/afd-aej-ponto-eletronico

### 3.3 Espelho de Ponto
- Relatório gerado pelo PTRP a partir do tratamento das marcações. Citado como terceiro artefato obrigatório junto a AFD e AEJ.
  - Fonte (secundária): https://espacolegislacao.totvs.com/portaria-671/

## 4. Tratamento de marcações
- A marcação deve **refletir com precisão a jornada real**; é **vedada a manipulação** e a **marcação automática** (registro pré-preenchido conforme horário de contrato).
- Ajustes/tratamento ocorrem no PTRP, que **não altera o AFD** (imutável) — gera o AEJ e o espelho.
  - Fonte (secundária): https://www.pontotel.com.br/portaria-671/

## 5. Banco de horas e compensação
- **Acordo individual escrito:** compensação em até **6 meses**.
- **Acordo/convenção coletiva (ACT/CCT):** compensação em até **1 ano**.
- Horas não compensadas no período devem ser **pagas como extras**.
- Banco de horas: saldo positivo (excesso) / negativo (déficit), equilibrado ao longo do período.
  - Fonte (secundária): https://segecompany.adv.br/2024/05/29/entenda-as-regras-juridicas-dos-acordos-de-compensacao-de-jornada-de-trabalho-banco-de-horas-e-banco-de-horas-negativo/
  - Fonte (secundária): https://www.totvs.com/blog/gestao-para-recursos-humanos/portaria-671/
  - Base legal subjacente: CLT art. 59 e §§ (reforma trabalhista Lei 13.467/2017). `[a confirmar — obter doc oficial]` redação vigente da **CLT art. 59** (planalto.gov.br).

## 6. Obrigatoriedade no SETOR PÚBLICO (ponto crítico para o piloto)
- A Portaria 671 regulamenta a **CLT** e aplica-se a **empregadores com empregados celetistas** (regra geral: empresas com mais de 20 empregados mantêm controle de ponto). As fontes consultadas **não tratam de servidores estatutários**.
- Implicação para Maximiliano de Almeida/RS: o município tem **regime estatutário** (servidores efetivos) e possivelmente **celetistas/temporários**. A Portaria 671 (AFD/AEJ/REP) **não é, por si, a norma de ponto do servidor estatutário** — o controle de jornada do estatutário é regido por **lei municipal / regime jurídico único** e por normas do TCE-RS.
  - Fonte (secundária — escopo CLT): https://www.mywork.com.br/blog/portaria-671-controle-de-ponto
  - Fonte (oficial — P&R REP não menciona estatutário): https://www.gov.br/trabalho-e-emprego/pt-br/assuntos/inspecao-do-trabalho/fiscalizacao-do-trabalho/Perguntas%20e%20Respostas%20REP

---

## 7. Pendências `[a confirmar — obter doc oficial]`
1. **Leiaute completo e versão dos Anexos** (record types, posições, tamanhos de campo) do AFD e AEJ — obter **Anexos da Portaria MTP nº 671/2021** (texto oficial DOU/gov.br). Anexo VI = AEJ; confirmar Anexo do AFD e Anexos de requisitos técnicos do REP-C/INMETRO.
2. **Tipos de registro do AFD** (cabeçalho, identificação empregador, marcação, ajuste de relógio, inclusão/alteração de empregado, trailer) — não detalhados em fonte confiável; obter do Anexo oficial. As menções a "tipos 1–5/04/05" em fontes secundárias são **não confirmadas**.
3. **Aplicabilidade ao servidor estatutário municipal** — confirmar no **regime jurídico único de Maximiliano de Almeida/RS** e em **normas/instruções do TCE-RS** sobre controle de jornada e frequência no serviço público (ex.: dados de pessoal enviados ao TCE-RS / SIAPC-LRF). Documento: `[a confirmar — leiaute/instrução TCE-RS pessoal e folha]`.
4. **Integração eSocial S-1200/S-2299 etc. x ponto** — confirmar se há dependência entre AEJ/banco de horas e eventos de folha do eSocial no ente público. Documento: `[a confirmar — MOS eSocial vX.Y, eventos de folha do empregador público]`.
5. **Texto integral da Portaria 671 e prazos vigentes** (atualizações pós-2021) — obter de in.gov.br / gov.br.

## Fontes (oficiais primeiro)
- (Oficial) Perguntas e Respostas REP — gov.br Trabalho e Emprego: https://www.gov.br/trabalho-e-emprego/pt-br/assuntos/inspecao-do-trabalho/fiscalizacao-do-trabalho/Perguntas%20e%20Respostas%20REP
- (Oficial) DOU / Imprensa Nacional: https://www.in.gov.br/web/dou
- (Secundária) https://www.normaslegais.com.br/legislacao/portaria-mtp-671-2021.htm
- (Secundária) https://espacolegislacao.totvs.com/portaria-671/
- (Secundária) https://solides.com.br/blog/afd-e-aej/
- (Secundária) https://useponto.com.br/blog/afd-aej-ponto-eletronico
- (Secundária) https://www.topdata.com.br/portaria-671-do-ministerio-do-trabalho/
- (Secundária) https://www.pontotel.com.br/portaria-671/
- (Secundária) https://segecompany.adv.br/2024/05/29/entenda-as-regras-juridicas-dos-acordos-de-compensacao-de-jornada-de-trabalho-banco-de-horas-e-banco-de-horas-negativo/
- (Secundária) https://www.mywork.com.br/blog/portaria-671-controle-de-ponto
