# Transmissão das Remessas ao TCE-RS — SIAPC/PAD (M4)

> Pesquisa para implementação do canal de transmissão (módulo Transparencia: `EnviarRemessaTce`, hoje STUB).
> REGRA DE OURO (CLAUDE.md §8/§16): nenhum leiaute/protocolo inventado. Toda afirmação factual tem FONTE; o que não foi extraído byte-a-byte está marcado `[a confirmar — obter doc oficial]`.
> Fontes primárias verificadas (baixadas via curl + pdftotext, jun/2026): FAQ SIAPC/PAD oficial do TCE-RS e Manual SICOE (TCERS WIKI). Piloto: Maximiliano de Almeida/RS (pequeno porte → semestral em RGF/MCI).

---

## ⚠️ Distinção crítica de escopo (NÃO confundir os dois canais)

Há DOIS canais distintos no TCE-RS, com aplicativos e periodicidades diferentes. O nome "SICOE" no nosso código pode estar ambíguo:

| Canal | Quem usa | Aplicativo cliente | Periodicidade | Norma |
|---|---|---|---|---|
| **SIAPC/PAD + MCI** | Administração DIRETA municipal (Prefeitura, Câmara) — Lei 4.320/64. **É o nosso piloto.** | **PAD** (Programa Autenticador de Dados) + **MCI** | **Mensal** (acumulado de 1º jan até fim do mês) | Res. 1099/2018; IN RREO/RGF |
| **SICOE** (e-Validador / LicitaCon) | Administração INDIRETA / **Companhias e Entidades** (autarquias, empresas, fundações) | **e-Validador** (app desktop Java) | **Trimestral** (acumulado de 1º jan até fim do trimestre) | Res. 1.074/2017; IN 08/2017 |

**Implicação:** o piloto (Prefeitura) usa o fluxo **PAD/MCI mensal**, NÃO o SICOE trimestral. O nome `EnviarRemessaTce`/`SICOE` no domínio deve ser revisto: ou tratamos os dois canais, ou renomeamos para refletir o PAD. `[a confirmar com o dono: o escopo M4 é só Prefeitura (PAD) ou também entidades indiretas (SICOE)?]`

---

## 1. Canal de envio e "portal manual vs web service/API"

**Conclusão central: NÃO há API/web service público REST/SOAP para o ente integrar diretamente.** A transmissão é feita por um **aplicativo cliente oficial do TCE-RS** (PAD para direta; e-Validador para indireta) que:
- roda na máquina do ente,
- lê os arquivos `.TXT` gerados pelo sistema contábil (o nosso ERP),
- valida contra o leiaute, autentica/gera a remessa,
- **transmite pela Internet aos servidores do TCE-RS** por canal proprietário do próprio aplicativo,
- e exige **assinatura digital no e-Protocolo** (portal web) para concluir.

> FONTE (Manual SICOE, seção "Transmitir Remessa"): *"Esta tela permite selecionar a forma de envio da remessa e transmitir os dados para o TCE-RS. Para gerar o arquivo de remessa e o RVE, pressionar o botão 'Iniciar'... A transmissão só será iniciada após realizar o processo de autenticação descrito em https://tcers.tc.br/repo/cex/licitacon/eValidador_NovaAutenticacao.pdf"* — https://tcers.tc.br/repo/cex/sicoe/manual-sicoe.pdf
>
> FONTE (FAQ SIAPC/PAD, item 2): *"Após enviar as informações do PAD e do MCI pela internet... É necessário realizar a assinatura digital. Para tal, acesse Portal > Jurisdicionado > Processo Eletrônico > Acesso ao Sistema (nesse momento é necessário estar com o certificado digital conectado à máquina)..."* — http://www.tce.rs.gov.br/sistemas_controle/SIAPC/pdf/perguntas_frequentes.pdf

**O que nosso ERP realmente entrega:** o Tensorroot.Gov gera os **arquivos `.TXT` no leiaute SIAPC** (papel do módulo Transparencia / `ArquivoRemessa` / `RegistroLeiaute`). O envio em si é feito pelo **PAD oficial operado pelo servidor da prefeitura**, não pela nossa aplicação. `EnviarRemessaTce` provavelmente NÃO faz POST para um web service do TCE — deve **empacotar/disponibilizar os TXT** para o PAD e registrar o protocolo manualmente. `[a confirmar — obter doc de "eValidador_NovaAutenticacao.pdf" e verificar se há endpoint de transmissão automatizável]`

**Portal manual (web):** assinatura digital do RVE e o "envio do e-protocolo" são feitos no **Processo Eletrônico do TCE-RS** (portal web, com certificado conectado). A consulta de recibo também é portal manual.

---

## 2. Formato do pacote (ZIP) e identificação do ente

- Arquivos reunidos em **um único ZIP**; nome estruturado de 60 bytes:
  `CNPJ(14).DataIni(8).DataFim(8).DataGer(8).Tipo(1).CodRemessa(12).zip`
  Tipo de Setor de Governo: `P`=Prefeitura, `C`=Câmara, `A`=Autarquia, `F`=Fundação, `E`=Empresa, `S`=Consórcio, `O`=Outros.
  Exemplo oficial: `99999999000199.01012007.31052007.15062007.P.000000000010.zip`
  > FONTE: Manual SIAPC Vol. V (já transcrito em `specs-oficiais/tce-rs-siapc-pad.md` §2.3). Confirmar leiaute do exercício 2026.
- **Identificação/autenticação do ente:** **certificado digital ICP-Brasil** (A1/A3). O certificado é configurado no aplicativo ("Configurações" → certificado + diretórios) e os signatários são resolvidos automaticamente do **SISCAD** (cadastro único).
  > FONTE (FAQ SIAPC/PAD item 1): *"Desde 2014 o SIAPC (PAD E MCI) está integrado com o Sistema de Cadastro Único – SISCAD... todas as informações cadastrais devem ser registradas no SISCAD."*
  > FONTE (Manual SICOE, FAQ item 2c): *"customizar as informações de configuração do certificado digital da entidade e os diretórios de localização dos arquivos de texto..."*
- **Certificado de assinatura final:** o Administrador Responsável assina o **RVE** com **Certificado particular ICP-Brasil** no e-Protocolo.
  > FONTE (Manual SICOE, "Assinar Remessa"): *"o Administrador Responsável deverá assinar digitalmente o RVE (Relatório de Validação e Encaminhamento), utilizando seu Certificado particular ICP-Brasil... será direcionado para o Sistema e-Protocolo do TCE-RS."*

---

## 3. Periodicidade e prazos por tipo de remessa

**Periodicidade SIAPC/PAD:** mensal, sempre **acumulado de 1º de janeiro até o encerramento do mês** (não é o mês isolado).

| Remessa | Cadência | Prazo | Fonte |
|---|---|---|---|
| **Regular** (PAD) | Mensal | **Até 30 dias corridos** após o fim do período (regra confirmada p/ Folha e Livro Diário) | Res. 1099/2018 (FAQ itens 9/10) |
| **Complementar** (PAD + RDI) | Mensal | Mesma janela mensal | FAQ (quadro de assinaturas) |
| **Folha de Pagamento** | Mensal | **até 30 dias corridos** após o fim do mês (a partir de jan/2019) | Res. 1099/2018 — FAQ: *"a partir de janeiro de 2019 a remessa é enviada mensalmente, em até 30 (trinta) dias corridos após o encerramento do período"* |
| **Livro Diário Geral** | Mensal | mesma regra (Res. 1099/2018), obrigatório desde 2017 | FAQ item "É obrigatório o envio do Livro Diário Geral?" |
| **RGF** (Gestão Fiscal) | Quadrimestral (>50k hab.) **ou Semestral (≤50k hab.)** → **Maximiliano = semestral**: 1º sem. na entrega de junho; 2º na de janeiro | Junto à entrega mensal do mês de fechamento | LRF/LC 101; IN RREO/RGF do TCE-RS |
| **MCI** (Controle Interno) | Mesma cadência do RGF | Junto à remessa do mês de fechamento | FAQ (quadros por quadrimestre/semestre) |

> ⚠️ `[a confirmar — obter norma vigente]` o **prazo padrão mensal do PAD "regular"** (orçamentário/contábil, fora Folha/Diário) não foi extraído byte-a-byte com a norma específica; a regra de "30 dias corridos" está confirmada para Folha e Diário (Res. 1099/2018). Confirmar na IN/Resolução de calendário de entrega do exercício 2026, pois o TCE-RS publica **Ofício Circular anual** com calendário e frequentemente **prorroga prazos** (ex.: prorrogações 2022/2023/abr-2024).

**Reenvio do PAD** (FAQ item "É possível fazer o reenvio do PAD?"):
- Dentro do prazo: reenvio livre, sem contato.
- Fora do prazo e NÃO mês de Gestão Fiscal: reenvio **justificando** na tela do PAD.
- Fora do prazo E mês de RGF: **contatar o TCE** (SAG/SATE) para orientação antes de reenviar.

---

## 4. Fluxo passo a passo do envio (consolidado das fontes oficiais)

Baseado no FAQ SICOE item 2 (a–h) e FAQ SIAPC/PAD itens 2–6 — adaptado ao PAD (direta):

1. **Cadastro (SISCAD):** responsáveis (Responsável, Contabilista, Controle Interno, Folha) cadastrados no SISCAD; o PAD busca os vínculos automaticamente no dia da execução.
2. **Geração dos TXT:** o sistema contábil (NOSSO ERP) gera os arquivos `.TXT` no leiaute SIAPC do exercício (cabeçalho + corpo + `FINALIZADOR`).
3. **Configuração:** no PAD/aplicativo, configurar **certificado digital** + diretórios dos arquivos.
4. **Validação:** o app valida (consistência lógica e contábil) e gera o **Relatório de Críticas** (Pendências = bloqueiam; Avisos = não bloqueiam). Corrigir até zerar pendências.
5. **Conferir RVE:** visualizar o **RVE (Relatório de Validação e Encaminhamento)** — resumo do que será transmitido + totalizadores.
6. **Gerar a remessa:** consolida todos os TXT validados em **um ZIP** com o nome estruturado.
7. **Transmitir:** envio do ZIP pela Internet aos servidores do TCE-RS (após autenticação do app).
8. **Assinar:** assinatura digital do RVE no **Processo Eletrônico / e-Protocolo** (Portal > Jurisdicionado > Processo Eletrônico > Acesso ao Sistema, com certificado conectado). Para indireta, no 4º período há anexos PDF obrigatórios (BP, DRE, DFC etc.).
9. **Enviar e-protocolo:** *"É necessário fazer o envio do e-protocolo para completar a entrega."* (FAQ item 4).
10. **Comprovante/recibo:** consultar em **Portal > Jurisdicionado > Sistema de Controle Externo > Relatórios e Recibos de envio** — status `pendente | concluída | carregada` (FAQ item 6).

---

## 5. Implicações para o módulo Transparencia (design)

- `ArquivoRemessa`/`RegistroLeiaute` → geram os **TXT no leiaute** (ASCII ISO-8859-1, CR/LF, larguras fixas). É aqui que está o valor automatizável do ERP.
- `RemessaTce` → agrega arquivos, calcula `CodRemessa`, `DataIni/DataFim/DataGer`, monta o **nome do ZIP** e empacota.
- `EnviarRemessaTce` → **provavelmente NÃO é um POST a web service**. Mais provável: (a) **exportar o ZIP/TXT** para o PAD oficial operado pelo servidor; ou (b) integração com e-Protocolo, se houver endpoint. `[a confirmar — obter eValidador_NovaAutenticacao.pdf e verificar existência de API de transmissão]`
- `MatrizSaldos` → alimenta também SICONFI/MSC (pesquisa separada).
- Persistir **recibo/protocolo** e status (pendente/concluída/carregada) como artefato de auditoria (CLAUDE.md §6).

---

## 6. Fontes (URLs oficiais — verificadas)

- **FAQ SIAPC/PAD (verificado, transcrito):** http://www.tce.rs.gov.br/sistemas_controle/SIAPC/pdf/perguntas_frequentes.pdf
- **Manual SICOE / e-Validador (verificado, transcrito):** https://tcers.tc.br/repo/cex/sicoe/manual-sicoe.pdf (espelho WIKI: http://wiki.tce.rs.gov.br/wiki/index.php?title=Manual_SICOE)
- **Autenticação do e-Validador (transmissão):** https://tcers.tc.br/repo/cex/licitacon/eValidador_NovaAutenticacao.pdf `[baixar e transcrever]`
- **Resolução 1.074/2017 (SICOE):** https://tcers.tc.br/repo/cex/sicoe/resolucao-1074-2017.pdf
- **IN 08/2017 (SICOE — leiaute/periodicidade):** https://tcers.tc.br/repo/cex/sicoe/in-08-2017.pdf
- **Portal Sistemas de Controle Externo (downloads PAD/SICOE/SISCAD):** https://tcers.tc.br/sistemas-de-controle-externo/
- **Notícia — versão de teste PAD 2025 (25.0.0.0) só valida, não transmite:** https://tcers.tc.br/noticia/tce-disponibiliza-versao-de-teste-do-programa-autenticador-de-dados-para-2025/
- **Manual SIAPC Vol. V (leiaute — já transcrito):** http://www.tce.rs.gov.br/sistemas_controle/SIAPC/pdf/MT_Vol_V_Arq_DispTCE_4320.pdf

---

## 7. Pendências `[a confirmar — obter doc oficial]`

1. **Escopo M4:** só Prefeitura (PAD mensal) ou também entidades indiretas (SICOE trimestral)? Renomear/revisar `EnviarRemessaTce`.
2. **Existe API/web service de transmissão automatizável?** Baixar `eValidador_NovaAutenticacao.pdf` e a Res. 1.074/2017 + IN 08/2017 para confirmar se a transmissão é exclusivamente via app desktop (PAD) ou se há endpoint integrável. Hipótese atual: **NÃO há API pública**; ERP só gera TXT/ZIP.
3. **Prazo padrão mensal do PAD regular (orçamentário/contábil):** confirmar norma e calendário do exercício 2026 (Ofício Circular anual + possíveis prorrogações). "30 dias corridos" confirmado só p/ Folha/Diário (Res. 1099/2018).
4. **Versão do PAD do exercício 2026** (sucessora da 25.0.0.0) e formato exato do pacote/handshake de transmissão.
5. **Conteúdo byte-a-byte da Res. 1.074/2017 e IN 08/2017** (não extraídos — só citados pelo Manual SICOE).
6. Confirmar **leiaute do nome do ZIP e cabeçalho** no MT do exercício 2026 (Revisão muda anualmente; novidades 2026: Balanço Patrimonial + DFC).
