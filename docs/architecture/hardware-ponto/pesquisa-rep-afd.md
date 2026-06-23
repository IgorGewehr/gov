# Hardware do Ponto — Ecossistema REP e coleta do AFD (Portaria MTP 671/2021)

> **Contexto Tensorroot.Gov / módulo RecursosHumanos.** O lado *software* (marcações, geração de
> AFD/AEJ posicional Port. 671, cálculo de jornada) já existe. Esta pesquisa cobre o lado
> **hardware**: tipos de equipamento de ponto, como cada um **armazena** registros e como nosso
> sistema **coleta** o AFD do equipamento físico — e qual o papel do nosso software como **PTRP**.
>
> Marcações `[a confirmar]` dependem de leitura direta do texto integral da Portaria/Anexos ou de
> doc/SDK proprietário de fabricante (não validável só por páginas comerciais/FAQ).

---

## 1. Os três tipos de REP (Registrador Eletrônico de Ponto)

A Portaria MTP nº 671/2021 (publicada em 08/11/2021, regula CLT art. 74) consolidou **três**
modalidades de Sistema de Registro Eletrônico de Ponto (SREP). Em todas, **somente o REP** gera o
AFD para fins fiscais/legais — o programa de tratamento (PTRP) **não** gera AFD, gera o AEJ.

| Tipo | Nome | Habilitação | Armazenamento | Comunicação típica |
|------|------|-------------|---------------|--------------------|
| **REP-C** | Convencional | **Homologação Inmetro** (Avaliação da Conformidade) + registro no MTE | **MRP** — Memória de Registro de Ponto interna, inviolável, não apagável/alterável | **USB porta fiscal** (pen drive); rede **TCP/IP** para coleta gerencial |
| **REP-A** | Alternativo | Autorizado por **convenção/acordo coletivo** (art. 77). Sem homologação Inmetro | Livre (on-premise ou nuvem), mas registros imutáveis e sem restrição de horário | Definido pelo instrumento coletivo; pode ser fracionado |
| **REP-P** | Por Programa (via software) | **Registro no INPI** (certificado de programa de computador, art. 91). Sem Inmetro | **ARP** — Armazenamento de Registro de Ponto, com redundância/alta disponibilidade | API/rede entre coletores e o REP-P; nuvem ou servidor dedicado; AFD fracionável |

### REP-C (relógio físico convencional — Inmetro)
- Equipamento físico dedicado; recebe a marcação e **imprime comprovante** em papel.
- Submetido à **Avaliação da Conformidade do Inmetro** (RAC). A MRP é o cofre fiscal: inviolável,
  não permite apagar/alterar registros nem fracionar o AFD (REP-C **não** pode ser fracionado).
- **Não suporta teletrabalho** (é presencial/fixo). Custo de hardware mais alto.

### REP-A (alternativo)
- Conjunto de equipamentos e/ou programas autorizado **somente** via norma coletiva.
- Deve registrar fielmente, **não permitir alteração** dos registros nem restringir horários de
  marcação. Gera AFD quando solicitado pelo Auditor-Fiscal do Trabalho. AFD **pode ser fracionado**.

### REP-P (por programa — o caso central do nosso produto)
Conceito novo da 671. O **SREP via programa** é composto por **3 partes**:
1. **Coletores de marcações** — equipamento, dispositivo físico **ou** software (app, totem, web)
   que recebe e transmite ao REP-P a informação da marcação.
2. **ARP (Armazenamento de Registro de Ponto)** — meio com **redundância, alta disponibilidade e
   confiabilidade**. No ARP ficam registrados: inserções, exclusões/alterações, **ajustes de
   relógio**, eventos sensíveis e as marcações.
3. **REP-P (o programa)** — emite o **AFD** e assina digitalmente. Pode rodar em servidor dedicado
   ou em **nuvem**. AFD **fracionável** (facilita auditoria).

---

## 2. Como o equipamento ARMAZENA

- **REP-C → MRP (Memória de Registro de Ponto):** memória interna inviolável do relógio. Cada
  marcação recebe um **NSR (Número Sequencial de Registro)** crescente, contínuo e sem lacunas — é o
  que garante a não-adulteração (qualquer gap denuncia supressão). `[a confirmar: limites de
  capacidade/rollover por fabricante]`
- **REP-P → ARP:** armazenamento lógico (BD/nuvem) com redundância. Registra não só marcações mas
  também a **trilha de auditoria** (inserção/exclusão/alteração, ajuste de relógio, eventos
  sensíveis). É onde o nosso sistema mantém o estado bruto antes do tratamento.
- **REP-A:** livre, desde que imutável e completo.

---

## 3. Como o sistema COLETA o AFD do equipamento

### REP-C (relógio físico) — dois caminhos
1. **Porta fiscal USB (uso exclusivo do Auditor-Fiscal):** conecta-se o **pen drive** na porta
   fiscal e a **coleta inicia automaticamente**, gerando o AFD completo (sem filtro de data).
2. **Coleta gerencial (empresa/PTRP):** via menu do equipamento (`Menu > Pendrive > Exportar
   Registros de Ponto`, com senha e **intervalo de datas**) **ou** por **rede TCP/IP** (habilitar
   no menu `Conectividade > TCP/IP`), usando o gerenciador/SDK do fabricante para puxar o AFD para o
   software de tratamento. Exemplo de fluxo verificado: Topdata *Inner Rep Plus* (menu/porta
   fiscal); Dimep, Henry, Control iD operam de forma análoga `[a confirmar por SDK de cada um]`.

> **Atenção a dois layouts no REP-C:** os equipamentos geram **AFD (Portaria 671)** e também um
> **AFD (Inmetro)** — o AFD Inmetro inclui verificação **CRC-16** (Cyclic Redundancy Check) por
> registro; o AFD da 671 segue o leiaute posicional do Anexo. São arquivos/finalidades distintos;
> nosso PTRP consome o **AFD da Portaria 671**. `[a confirmar: mapeamento exato dos tipos de
> registro entre os dois layouts]`

### REP-P (nosso software como coletor + tratador)
- Coletores transmitem marcações ao REP-P por **rede/API** (REST entre coletor e central é o padrão
  de mercado) `[a confirmar: não há protocolo único normatizado; cada fornecedor define o seu]`.
- O **REP-P gera o AFD** a partir do ARP (Anexo V da Portaria), **fracionável** por período.
- A 671 cita **"novo protocolo de comunicação"** a ser certificado por fabricantes para novos
  equipamentos — sem especificação técnica pública nas fontes consultadas `[a confirmar no texto
  integral / anexos]`.

### Periodicidade
- **AFD:** gerado/disponibilizado **sob demanda** (sempre que solicitado pelo Auditor-Fiscal) e na
  rotina interna; na prática, ciclo **mensal** acompanhando o fechamento da folha e o Espelho de
  Ponto mensal entregue ao trabalhador. `[a confirmar: a Portaria não fixa periodicidade rígida de
  geração do AFD além do "quando solicitado"]`
- **Retenção:** prazo de guarda **não especificado** no material de FAQ oficial consultado — a 671
  remete à legislação geral. `[a confirmar no texto integral + prazos prescricionais trabalhistas
  (regra prática: 5 anos)]`

---

## 4. Integridade e assinatura digital (crítico p/ nosso PTRP)

- **AFD e AEJ** gerados por **REP-P/REP-A** são assinados no padrão **CAdES** (*CMS Advanced
  Electronic Signature*), armazenados em arquivo **`.p7s` destacado (*detached*)**.
- **AFD:** assinado pelo **fabricante/desenvolvedor do REP** com certificado **ICP-Brasil** válido.
- **AEJ:** assinado pelo **desenvolvedor do PTRP** (ou seja, **nós**) — também CAdES/`.p7s`,
  ICP-Brasil.
- **Comprovante de marcação (REP-P):** padrão **PAdES** com hash **SHA-256** `[a confirmar: PAdES
  citado por fonte comercial; validar no Anexo]`.
- **REP-C / AFD Inmetro:** integridade por **CRC-16** por registro + NSR contínuo.

> **Implicação de arquitetura Tensorroot.Gov:** precisamos de (a) certificado **ICP-Brasil A1/A3**
> para assinar o **AEJ** em CAdES `.p7s` (já temos infra de Key Vault/A1 do M2 — reaproveitar); e
> (b) um **parser/validador de AFD** que verifique continuidade de NSR, layout posicional do Anexo,
> e valide a assinatura `.p7s` destacada do AFD recebido do REP físico antes de tratar.

---

## 5. Papel do nosso software como PTRP

- **PTRP = Programa de Tratamento de Registro de Ponto.** Conjunto de rotinas que **processa o AFD**
  (dados brutos) **sem alterar o registro original**, mantém histórico e **gera o AEJ (Arquivo
  Eletrônico de Jornada)** e o **Espelho de Ponto** (mensal, ao trabalhador).
- **Não requer homologação** Inmetro nem registro INPI obrigatório (é tratamento, não REP).
- **Não gera AFD** — apenas **consome** o AFD do REP (C/A/P) e produz o **AEJ assinado por nós**.
- No SREP **convencional**, o PTRP é par do REP-C; no SREP **via programa**, o REP-P já embute o
  papel de coleta/armazenamento e o PTRP faz o tratamento. Nosso produto pode atuar **só como PTRP**
  (consumindo AFD de relógios REP-C de terceiros) **e/ou** evoluir para **REP-P completo**
  (coletores + ARP + AFD assinado), o que exigiria **registro no INPI** e implementação do ARP com
  redundância. `[decisão de escopo — a confirmar com o roadmap M5]`

---

## 6. Pendências `[a confirmar]` para fechar

1. Texto **integral** da Portaria 671 + Anexos (layout posicional do AFD/AEJ, NSR, tipos de
   registro, "novo protocolo de comunicação") — validar contra nosso gerador já existente.
2. Prazo legal de **retenção** do AFD/AEJ.
3. **Periodicidade** normativa exata de geração do AFD (além de "sob demanda").
4. **SDKs/protocolos por fabricante** (Topdata, Dimep, Henry, Control iD, Madis) para coleta
   TCP/IP e USB do AFD em REP-C — cada um é proprietário.
5. Mapeamento **AFD 671 × AFD Inmetro (CRC-16)** nos equipamentos REP-C.
6. Confirmar **PAdES/SHA-256** do comprovante REP-P no Anexo (citado só por fonte comercial).
7. **Decisão de escopo:** Tensorroot.Gov atua como **PTRP puro**, **REP-P completo**, ou ambos.

---

## FONTES

- [MTE — Perguntas e Respostas REP (Portaria 671) — oficial gov.br](https://www.gov.br/trabalho-e-emprego/pt-br/assuntos/inspecao-do-trabalho/fiscalizacao-do-trabalho/Perguntas%20e%20Respostas%20REP) — definições REP-C/A/P, PTRP, AFD só pelo REP, CAdES/`.p7s`, ICP-Brasil, fracionamento.
- [MTE — Leiaute do Arquivo Fonte de Dados (AFD) — PDF oficial](https://www.gov.br/trabalho-e-emprego/pt-br/assuntos/inspecao-do-trabalho/fiscalizacao-do-trabalho/leiaute-do-arquivo-fonte-de-dados-afd.pdf) — estrutura do AFD.
- [mywork — REP-C, REP-A e REP-P: diferença](https://www.mywork.com.br/blog/rep-c-rep-a-rep-p-diferenca) — Inmetro vs INPI, MRP, porta fiscal USB, nuvem, CAdES.
- [Topdata — exportar AFD por pen drive (Inner Rep Plus)](https://suporte.topdata.com.br/suporte/como-exportar-o-arquivo-afd-pelo-pendrive-no-inner-rep-plus/) — menu, porta fiscal automática, intervalo de datas.
- [Topdata — AFD vs AFD (Inmetro) e CRC-16](https://suporte.topdata.com.br/suporte/qual-a-diferenca-entre-o-afd-e-o-afd-inmetro-no-gerenciador-de-inner-rep/) — dois layouts, CRC-16.
- [Dimep — Portaria 671: o que mudou](https://www.dimep.com.br/blog/post/portaria-671-o-que-mudou-no-controle-de-ponto-eletronico/) — Anexo V (AFD), Espelho mensal, REP-C×REP-P, API.
- [Senior — REPs e Portaria 671 (integração com dispositivos)](https://documentacao.senior.com.br/seniorxplatform/manual-do-usuario/ronda/integracoes-com-dispositivos/registradores-eletronicos-de-ponto-e-portaria-671.htm) — AFD obrigatório p/ todos os REP, "novo protocolo de comunicação", módulo Controle de Terminais.
- [Topdata — Portaria 671 (visão geral)](https://www.topdata.com.br/portaria-671-do-ministerio-do-trabalho/) — componentes do REP-P (coletores, ARP, AFD).
- INMETRO — Requisitos de Avaliação da Conformidade p/ REP `[a confirmar: localizar RAC/Portaria Inmetro vigente; antiga Port. Inmetro 595/2013 referenciada]`.

*Pesquisa: 2026-06-22. Documento de pesquisa (não normativo) — validar `[a confirmar]` antes de implementar.*
