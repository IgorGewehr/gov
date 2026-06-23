# Pesquisa — Integração com Hardware de Ponto (REPs) — Fabricantes BR

> Módulo RecursosHumanos / Tensorroot.Gov.
> Já temos o lado **software** (marcações, AFD/AEJ posicional Port. 671, jornada).
> Este documento cobre o lado **hardware**: como o nosso sistema se comunica com os equipamentos físicos de ponto.
> Data: 2026-06. Tags `[a confirmar]` = depende de doc/SDK proprietário ou contrato com o fabricante.

---

## 1. Contexto regulatório (define o que é portável vs. proprietário)

A **Portaria MTP 671/2021** (que consolidou a antiga Port. 1.510/2009) define três tipos de registrador:

- **REP-C** — equipamento físico convencional, **certificado pelo INMETRO**. É o "relógio de ponto" tradicional, com memória inviolável (MRP) e impressão de comprovante.
- **REP-A** — solução alternativa; exige acordo/convenção coletiva.
- **REP-P** — **programa** registrado no INPI; inclui *coletores de marcação*, armazenamento e tratamento. É a inovação da portaria e o ponto onde **o nosso software se encaixa**.

O que a portaria **padroniza** (e portanto é comum a todos os fabricantes):

- **AFD (Arquivo Fonte de Dados)** — layout **posicional fixo, ASCII**, definido no Anexo da portaria. É o **denominador comum universal**: todo REP-C/REP-P exporta AFD no mesmo layout (mudam só os dados). Já temos o parser/gerador.
- **AEJ (Arquivo Eletrônico de Jornada)** — substituiu AFDT/ACJEF.
- **Comprovante NSR** (Número Sequencial de Registro).
- **Assinatura digital** do AFD/AEJ no padrão **CAdES (CMS)** com certificado **ICP-Brasil** (obrigatória para REP-A/REP-P). `[a confirmar]` qual certificado o nosso REP-P usará (A1 já citado no contexto contábil do projeto).
- Marcações **online prioritárias**; quando offline, o coletor reenvia ao voltar online.

O que **NÃO** é padronizado (é por-fabricante): **o protocolo de COLETA/COMUNICAÇÃO em tempo real com o equipamento** (REST, TCP, DLL), o cadastro remoto de usuários, e o **enrollment/template biométrico**. É aqui que entra o SDK de cada fabricante.

> **Conclusão arquitetural:** existe **um padrão de DADOS (AFD posicional)**, mas **NÃO existe um padrão de PROTOCOLO**. A integração em tempo real é **por-fabricante** → exige camada de drivers (adapters).

---

## 2. Fabricantes e formas de integração

### 2.1 Control iD (linha iDClass / REP-P 373) — **a melhor documentada / mais aberta**

- **REST API nativa** sobre **TCP/IP (Ethernet)**, **porta 443 (HTTPS)**, payloads **JSON**. Independente de SO/linguagem.
- Sessão: `login` → `session_is_valid` / `logout` / `change_login`. Credenciais default `admin/admin` (trocar).
- **Coleta de marcações:** endpoint **`get_afd`** retorna todas as marcações no layout AFD.
- **Usuários:** `add_users`, `load_users`, `update_users`, `remove_users`, `import_users_csv`, `export_users_csv`.
- **Biometria digital:** `template_extract`, `template_merge` (extrai 3 templates da digital, qualidade mín. ~50%, junta no equipamento). Templates retornados em **base64**.
- **Biometria facial:** suportada (modelo iDClass Facial) — `remote_enroll`/`cancel_remote_enroll`, `user_set_image`/`user_get_image`, `get/set_facial_module_config`. Até ~10.000 faces (licença).
- **DLL `RepCid`** — encapsula as funções principais; **só C# e Delphi 10** (objeto COM via RegAsm, requer .NET Framework 4.0+). Alternativa à REST.
- **iDCloud** — web service da Control iD para **comunicação direta sem agente local** (push/cloud).
- Exemplos oficiais no **GitHub `controlid/integracao`** (pasta "Ponto Eletrônico").
- Contato integração: `integracao@controlid.com.br`.

### 2.2 Topdata (linha Inner REP Plus / Inner Ponto 4)

- **SDK Inner REP** — biblioteca de rotinas (DLLs) para comunicação. Protocolo **TCP/IP com criptografia** + chaves de comunicação contra acesso não autorizado.
- **Linguagens:** C#, Delphi, Java, VB6 (COM). Requer **Windows 7+ e .NET 3.5**.
- Funções: configuração (horários, formatação de cartão), cadastro/exclusão de funcionários (lista completa ou individual), **biometria + manutenção de templates**, sincronização de relógio, e **leitura de registros → montagem do AFD**.
- Biometria facial: `[a confirmar]` (doc menciona "biometria" genérica; há Leitor Facial T4 integrado por terceiros, ex. Secullum).
- Portal: `integrador.topdata.com.br` (downloads, manuais, exemplos).

### 2.3 Henry (linha Prisma SF Advanced / R2)

- **Web Server embarcado** no equipamento (configuração via navegador, sem CD/software).
- **Coleta:** via **TCP/IP** ou **Serial**, ou por **2ª porta USB** / **USB fiscal** (pendrive) em formato **AFD**. Download de AFD completo ou **filtrado por NSR**.
- Integração via software exige **usuário/senha** para conexão com o relógio.
- **SDK/biblioteca proprietária:** `[a confirmar]` — Henry distribui SDK/DLL p/ integradores; detalhes de endpoints/linguagens não confirmados em fonte pública. Compatível com SIMPAX e Secullum.

### 2.4 Madis (Rodbel) — linha MD REP Evo II / V3 / MD Comune

- **Coleta AFD** via **pendrive** (Evo II e V3) e por software.
- **MD Comune** — software de tratamento com sincronização em tempo real.
- Integra com **TOTVS Security Access (TSA)**.
- **SDK/API REST proprietária:** `[a confirmar]` — pouca doc pública; obter SDK direto com a Madis para integração tempo-real.

### 2.5 Dimep (linha PrintPoint III / SmartPoint)

- **REST API** para comunicação direta com os equipamentos **sem agente local** (confirmado pela integração Secullum via API REST).
- Homologação por **protocolos** Dimep: **Protocolo VIII** (REP/ponto), **Protocolo VII** (controle de acesso). `[a confirmar]` spec dos protocolos (proprietária).
- Biometria: digital, cartão, senha e **facial**. Geração de AFD pelo software Dimep.

### 2.6 Tellijack — `[a confirmar]`

- Não encontrada doc técnica pública (SDK/API) nas buscas. Provável biblioteca/SDK proprietária sob contrato. Tratar como driver dedicado a confirmar com o fabricante.

---

## 3. Há um padrão comum? — Resumo

| Camada | Padrão? | Observação |
|---|---|---|
| **AFD / AEJ (arquivo de dados)** | **SIM** — Port. 671 posicional ASCII | Universal. Coleta "lenta"/em lote sempre funciona. |
| **Assinatura AFD/AEJ** | **SIM** — CAdES/ICP-Brasil | Obrigatória REP-A/REP-P. |
| **Coleta em tempo real / config / enrollment** | **NÃO** — por-fabricante | REST (Control iD, Dimep), SDK/DLL TCP (Topdata, Henry), pendrive/SDK (Madis). |
| **Template biométrico** | **NÃO** — proprietário e não-intercambiável | Template de digital da Control iD ≠ Topdata ≠ etc. Enrollment preso ao fabricante. |

---

## 4. Padrão de mercado p/ coletar de MÚLTIPLOS REPs (referência: Secullum)

A Secullum (Ponto Web / Ponto 4) é a referência de mercado e suporta **Henry, Control iD, Topdata, Madis, Dimep, RW, Proveu, Trix, ZPM, Dixi** com a seguinte arquitetura — recomendada para o Tensorroot.Gov:

1. **Camada de drivers (adapters) por fabricante** — uma implementação por marca/protocolo (REST iDClass, SDK Inner REP, TCP/Serial Henry, REST Dimep, etc.), todas expondo a **mesma interface interna** (coletar marcações, cadastrar usuário, enrollar biometria, sincronizar relógio).
2. **Normalização para AFD** — todo driver entrega marcações **no layout AFD posicional**, que o nosso core (já existente) consome de forma uniforme. O AFD é a *lingua franca*.
3. **Agente de Comunicação (Communication Agent)** — componente que roda na rede do cliente (on-premise) e fala TCP/IP/Serial/USB com os relógios locais, repassando ao servidor central. Necessário para equipamentos sem cloud.
4. **Comunicação direta/cloud quando disponível** — Control iD **iDCloud** e Dimep **REST API** dispensam o agente local (push/online). Preferir esses caminhos quando o fabricante oferece.
5. **REP-P como coletor** — sob a Port. 671 o nosso software pode atuar como **coletor de marcações** (online prioritário, reenvio offline), assinando o AFD/AEJ em CAdES/ICP-Brasil.

---

## FONTES

- Control iD — API iDClass: https://www.controlid.com.br/suporte/api_idclass_latest.html
- Control iD — exemplos integração (GitHub): https://github.com/controlid/integracao/tree/master/Ponto%20Eletr%C3%B4nico
- Control iD — REST API / DLL RepCid (README): https://github.com/controlid/integracao/blob/master/Ponto%20Eletr%C3%B4nico/README.md
- Topdata — SDK Inner REP (funcionamento): https://integrador.topdata.com.br/suporte/funcionamento-sdk-innerrep/
- Topdata — SDK Inner REP: https://integrador.topdata.com.br/suporte/sdk-inner-rep-relogios-de-ponto/
- Topdata — Portaria 671: https://www.topdata.com.br/portaria-671-do-ministerio-do-trabalho/
- Henry Prisma (coleta TCP/USB/AFD por NSR): https://www.canalautomacao.com.br/blog/conhecendo-o-equipamento-relogio-de-ponto-henry-prisma-r2/
- Henry — comunicação Prisma (Secullum FAQ): https://www.secullum.com.br/pt/perguntas-frequentes/1074
- Madis — MD REP / coleta AFD pendrive (manual): https://www.kl-quartz.com.br/wp-content/uploads/2020/02/FMgH1x_md_rep.pdf
- Madis — MD Comune: https://api.aecweb.com.br/cls/catalogos/2990/29644/catalogo-madis-MDComune.pdf
- Dimep — comunicação via API REST (Secullum FAQ): https://www.secullum.com.br/pt/perguntas-frequentes/531
- Dimep — Portaria 671: https://www.dimep.com.br/blog/post/portaria-671-o-que-mudou-no-controle-de-ponto-eletronico/
- Secullum — REPs compatíveis / como comunicar: https://www.secullum.com.br/pt/perguntas-frequentes/867
- Secullum — equipamentos integrados: https://www.secullum.com.br/pt/perguntas-frequentes/39
- Portaria 671/2021 — REP-C/A/P, AFD/AEJ, CAdES/ICP-Brasil: https://espacolegislacao.totvs.com/portaria-671/
- Gov.br — Perguntas e Respostas REP (oficial): https://www.gov.br/trabalho-e-emprego/pt-br/assuntos/inspecao-do-trabalho/fiscalizacao-do-trabalho/Perguntas%20e%20Respostas%20REP
- mywork — REP-C/REP-A/REP-P diferença: https://www.mywork.com.br/blog/rep-c-rep-a-rep-p-diferenca

## PENDÊNCIAS [a confirmar]
- **Henry**: spec do SDK/DLL (endpoints, linguagens, protocolo exato) — obter doc do integrador Henry.
- **Madis**: existência/condições de SDK ou REST API tempo-real (além de pendrive AFD) — solicitar à Madis.
- **Dimep**: especificação dos Protocolos VII/VIII (proprietária) e doc da REST API — obter via Dimep.
- **Topdata**: confirmar suporte a **biometria facial** na SDK (T4) e portas TCP exatas.
- **Tellijack**: nenhuma doc pública encontrada — confirmar se há SDK/API e protocolo.
- **Certificado ICP-Brasil** do nosso REP-P para assinar AFD/AEJ em CAdES (A1 vs A3) — definir com Key Vault do projeto.
- **Templates biométricos** são proprietários e **não-portáveis** entre fabricantes — validar impacto operacional (re-enrollment ao trocar de marca).
