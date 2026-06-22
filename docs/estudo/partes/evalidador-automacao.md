# Automação da validação contra o oficial — SIAPC/PAD (TCE-RS) e paralelo eSocial

Status: estudo de viabilidade. Data: 2026-06-22.
Regra deste documento: toda afirmação tem FONTE ou está marcada `[a confirmar]`.

---

## 1. Contexto do nosso problema

Hoje a Tensorroot.Gov gera a remessa SIAPC/PAD (`.TXT` posicional + ZIP) e roda o
pré-validador `EValidadorLocalSiapc`, que é uma **SIMULAÇÃO** das regras. O risco é
óbvio: nossa simulação pode divergir das regras reais do TCE-RS e só descobriríamos
no momento do envio (ou pior, numa rejeição do RDI/RVE). Queremos validar contra o
**oficial** e, idealmente, automatizar isso em CI para "falhar o build" quando houver
erro estrutural na remessa.

---

## 2. O que é, de fato, o validador oficial do TCE-RS (PAD)

O fluxo oficial do TCE-RS **não** é um "validador" isolado. A validação está embutida
no **PAD — Programa Autenticador de Dados**, que lê os arquivos `.TXT` (gerados a
partir do SIAPC), valida, gera o **RVE (Relatório de Validação e Encaminhamento)** /
**RDI (Relatório de Dados e Informações)** e transmite a remessa.

Características confirmadas:

- **É um aplicativo Java desktop (GUI), não um web service nem CLI.** O usuário aperta
  o botão **"Iniciar"** para gerar a remessa e o RVE; depois assina o RVE
  digitalmente com certificado **ICP-Brasil** particular. FONTE: TCE-RS, *Perguntas
  Frequentes SIAPC* (http://www.tce.rs.gov.br/sistemas_controle/SIAPC/pdf/perguntas_frequentes.pdf).
- **Auto-atualizável e online-dependente:** "o software, sempre que iniciado, irá
  verificar se existem novas atualizações disponibilizadas, desde que haja uma conexão
  com a Internet e o link do TCE-RS esteja disponível". Ou seja, **as regras de
  validação mudam por versão** (versão de teste 25.x.x para 2025). FONTE: TCE-RS, FAQ
  SIAPC; notícia "TCE disponibiliza versão de teste do PAD para 2025"
  (https://tcers.tc.br/noticia/tce-disponibiliza-versao-de-teste-do-programa-autenticador-de-dados-para-2025/).
- **Distribuição:** download manual no portal ("Sistemas de Controle Externo" →
  "Download do PAD"). O PAD do LicitaCon é distribuído como Java Web Start (`.jnlp`),
  o que confirma a stack Java desktop da família e-Validador do TCE-RS. FONTE:
  https://portal.tce.rs.gov.br/portal_tcers/e-validador/licitacon.jnlp ;
  https://tcers.tc.br/sistemas-de-controle-externo/.
- **Layout/regras:** definidos nos Manuais Técnicos SIAPC/PAD (Vol. II e Vol. V —
  leiaute posicional dos arquivos `.TXT`). FONTE: Manual Técnico Vol. II
  (https://tcers.tc.br/repo/SIAPC/MANUAL/MT_Vol_II_SiapcPAD_6404.pdf) e Vol. V
  (http://www.tce.rs.gov.br/sistemas_controle/SIAPC/pdf/MT_Vol_V_Arq_DispTCE_4320.pdf).

O que **não** existe (do que foi possível verificar publicamente):

- **Sem modo headless/CLI documentado** para o PAD. `[a confirmar]` — não há evidência
  de flags de linha de comando; a invocação documentada é sempre a GUI (botão Iniciar).
- **Sem XSD/schema público** nem validador programático/REST oficial do SIAPC/PAD. O
  portal de **Dados Abertos** (https://dados.tce.rs.gov.br/) publica dados, mas **não**
  oferece endpoint de validação de remessa. `[a confirmar]` quanto a API privada.

Conclusão sobre (a) e (b): **(a) o e-Validador/PAD não é oficialmente headless/CLI**;
**(b) não há XSD nem validador programático publicado** do SIAPC/PAD. `[a confirmar]`
se existe contrato/canal técnico do TCE-RS que libere isso a fornecedores.

---

## 3. Como outros fornecedores/municípios validam antes de enviar (c)

Padrão de mercado observado: o ERP do fornecedor **reimplementa as regras dos Manuais
Técnicos** internamente (exatamente o que nosso `EValidadorLocalSiapc` faz) e usa o
**PAD oficial como gate manual final** — o servidor/contador abre o PAD, gera o RVE,
confere inconsistências e só então assina e transmite. Não há, no fluxo público,
automação contra o oficial; o PAD é a "fonte da verdade" rodada manualmente por
humano. FONTE (fluxo): TCE-RS FAQ SIAPC; Manual Técnico Vol. II.
`[a confirmar]` por amostragem com fornecedores concorrentes.

Implicação: nossa simulação local é a abordagem **correta e padrão**; o ganho não está
em "chamar o oficial em CI", e sim em **manter nossa simulação fiel ao Manual Técnico
versionado** e tratar o PAD como aceitação manual.

---

## 4. Paralelo eSocial — aqui SIM dá para automação real (d)

Diferente do TCE-RS, o eSocial tem **ambiente de HOMOLOGAÇÃO oficial com web service**
("Produção Restrita"), sem efeito legal, próprio para testes funcionais ponta a ponta:

- Endpoints reais:
  - Envio de lote: `https://webservices.producaorestrita.esocial.gov.br/servicos/empregador/enviarloteeventos/WsEnviarLoteEventos.svc`
  - Consulta de lote: `https://webservices.producaorestrita.esocial.gov.br/servicos/empregador/consultarloteeventos/WsConsultarLoteEventos.svc`
  FONTE: eSocial, *Produção Restrita*
  (https://www.gov.br/esocial/pt-br/acesso-ao-sistema/ambiente-de-producao-restrita).
- **WSDL + XSD oficiais** distribuídos no "Pacote de Comunicação do eSocial"
  (ex.: `WsEnviarLoteEventos-v1_1_0.wsdl`, `WsConsultarLoteEventos-v1_1_0.wsdl`),
  versionados na Documentação Técnica. FONTE: Manual de Orientação do Desenvolvedor do
  eSocial (https://www.econeteditora.com.br/bdi/manualorientacaodesenvolvedoresocial_v1-3.pdf).
- Requisitos: certificado digital ICP-Brasil do empregador para assinar os XMLs e
  acessar o serviço; ambiente limitado (até 1.000 vínculos/empregador), sem teste de
  carga. FONTE: eSocial, Produção Restrita.

Conclusão sobre (d): para os eventos de RH (M5), **dá para validar de verdade**, em CI:
validar XML contra os **XSD oficiais** e transmitir à **Produção Restrita** via SOAP,
falhando o build em retorno de erro. Esse é o caminho de automação forte.

---

## 5. Estratégia recomendada

| Alvo | Validação oficial automatizável? | Como |
|------|----------------------------------|------|
| SIAPC/PAD (TCE-RS) | **Não** (PAD é Java desktop GUI, sem CLI/XSD/WS oficial) | Manter `EValidadorLocalSiapc` fiel ao Manual Técnico **versionado**; PAD oficial = gate manual de aceitação. Monitorar nova versão anual do PAD e fazer diff de regras. |
| eSocial (RH/M5) | **Sim** | Validar XML contra **XSD oficial** em CI + transmitir à **Produção Restrita** (web service SOAP, cert. ICP-Brasil) como teste de integração. |

`[a confirmar]`: abrir chamado ao TCE-RS perguntando se há (1) modo batch/CLI do PAD,
(2) XSD/schema dos `.TXT`, (3) ambiente de homologação/web service. Se positivo, muda o
quadro do SIAPC.

---

## RESPOSTA (4 linhas)

Dá para automatizar **parcialmente**. Para o **SIAPC/PAD (TCE-RS) NÃO** existe via oficial: o PAD é app **Java desktop com GUI**, sem CLI/headless, sem XSD nem web service público — então o oficial só roda como **gate manual**; nossa defesa é manter o `EValidadorLocalSiapc` fiel ao **Manual Técnico versionado** e diferenciar regras a cada nova versão anual do PAD.
Para o **eSocial SIM**: há **ambiente oficial de Homologação (Produção Restrita) com web service SOAP + WSDL/XSD** — validamos os XML contra o **XSD oficial** e transmitimos à Produção Restrita em CI (cert. ICP-Brasil), falhando o build em erro.
Recomendo abrir chamado ao TCE-RS para confirmar se há batch/XSD/homologação `[a confirmar]`; se houver, promovemos o SIAPC ao mesmo nível do eSocial.
Em resumo: automação real e ponta a ponta no eSocial agora; no SIAPC, fidelidade ao manual + aceitação manual no PAD até o TCE-RS abrir interface programática.
