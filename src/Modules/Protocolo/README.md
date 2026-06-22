# Módulo Protocolo
> Processo Administrativo Eletrônico paperless: autuação, tramitação, assinatura e trilha documental para todo o ERP · Poder: Ambos · Schema EF Core: `protocolo` · Ativável por tenant.

## 1. Propósito & Marco Legal
Provê o ciclo de vida do **Processo Administrativo Eletrônico (PAE)** e a gestão arquivística documental, eliminando papel. É um Bounded Context **cross-cutting**: demais módulos (Licitações, RH/Férias, Licenças) não implementam protocolo próprio — solicitam autuação, juntada e assinatura via Integration Events.
Marco: **Lei 9.784/1999** (processo administrativo; prazos art. 66 — exclui dia inicial, inclui o final; decisão em regra 30 dias prorrogáveis); **Lei 14.063/2020** + **Decreto 10.543/2020** (assinaturas SIMPLES/AVANÇADA/QUALIFICADA por criticidade); **Lei 11.419/2006** (processo eletrônico); **Decreto 8.539/2015** (SEI/PAE federal); **MP 2.200-2/2001** (ICP-Brasil); **CONARQ** (TTD, e-ARQ Brasil/SIGAD); LGPD.

## 2. Linguagem Ubíqua
- **Protocolo**: registro formal de entrada/saída de documento ou requerimento.
- **Autuacao**: ato de formar o processo, gerando o NUP.
- **NUP**: Numero Unico de Protocolo — identificador imutável e único.
- **ProcessoAdministrativo**: conjunto ordenado de documentos com finalidade.
- **Documento**: peça processual (nato-digital ou digitalizada), PDF/A.
- **Juntada**: inserção de documento ao processo.
- **Tramitacao**: movimentação entre setores/responsáveis.
- **Despacho**: manifestação/decisão de autoridade no processo.
- **Distribuicao**: atribuição inicial do processo a um relator/setor.
- **Sobrestamento**: suspensão temporária do andamento.
- **Arquivamento**: encerramento; guarda conforme TTD.
- **CapaDoProcesso**: metadados consolidados (NUP, partes, classificação).
- **TTD**: Tabela de Temporalidade e Destinação (guarda/eliminação).
- **AssinaturaQualificada**: ICP-Brasil (certificado e-CPF/e-CNPJ).
- **AssinaturaAvancada**: provedor credenciado com vínculo do signatário.
- **AssinaturaSimples**: identificação por meio eletrônico (gov.br bronze).
- **CarimboDeTempo**: atestação temporal confiável da assinatura.
- **NivelDeAcesso**: publico / restrito / sigiloso.
- **Requerimento**: pedido do cidadão que origina o processo.
- **SIGAD**: Sistema Informatizado de Gestão Arquivística de Documentos.

## 3. Mapa de Domínio
**Agregado `Processo` (raiz, IMustHaveTenant)** — VOs: `NUP`, `Classificacao`, `NivelAcesso`, `Prazo`; entidades internas: `Despacho`, `Movimentacao`. Controla autuação, tramitação, sobrestamento e arquivamento.
**Agregado `Documento` (raiz, IMustHaveTenant)** — VOs: `Hash` (SHA-256), `CarimboTempo`, `Assinatura(Tipo)`. Imutável após juntada (apenas "sem efeito").
**Agregado `Requerimento` (raiz, IMustHaveTenant)** — origina `Processo`; guarda dados do solicitante.
**Agregado `TabelaTemporalidade` (raiz, IMustHaveTenant)** — rege destinação por classe documental.
**Eventos de Domínio**: `ProcessoAutuado`, `DocumentoJuntado`, `DocumentoAssinado`, `ProcessoTramitado`, `ProcessoSobrestado`, `ProcessoArquivado`, `RequerimentoRecebido`, `PrazoGuardaExpirado`.

## 4. Integrações & Padrões Técnicos
- **Assinatura**: ICP-Brasil (qualificada), provedores de avançada, **gov.br** (bronze→simples, prata→avançada, ouro→avançada/qualificada).
- **GED/SIGAD** aderente ao **e-ARQ Brasil**, armazenamento em **PDF/A**.
- **Motor BPMN** para fluxos cross-module (férias, licenças, licitação) com raias por setor.
- **Hash SHA-256 + carimbo de tempo** por documento; eventos publicados via **Outbox** (MediatR → Integration Events).

## 5. Regras de Negócio Críticas
- **Assinatura por criticidade** (Decreto 10.543/2020): atos do dirigente máximo / bens imóveis → **qualificada**; atos internos de média criticidade → **avançada**; requerimentos do cidadão → **simples**.
- **NUP imutável e único** após a autuação.
- **Documento juntado não é excluído** — apenas tornado *sem efeito* (mantém trilha).
- **Arquivamento/eliminação só conforme TTD/CONARQ**; eliminação é registrada (termo).
- **Prazos (art. 66)**: cálculo exclui o dia inicial e inclui o final; decisão padrão em 30 dias prorrogáveis.
- **Processo sigiloso** nega acesso não autorizado e **audita a tentativa**.

## 6. Multi-Tenancy, Segurança & Auditoria
Toda raiz implementa `IMustHaveTenant`; `TenantId` aplicado por *global query filter* no EF Core (schema `protocolo`). Cross-module **exclusivamente via Integration Events** — nenhum módulo acessa tabelas do Protocolo diretamente. `NivelDeAcesso` controla visibilidade; tentativas em processos sigilosos geram registro de auditoria imutável. Toda movimentação, juntada e assinatura compõem trilha documental append-only.

## 7. Contratos Públicos (Integration Events)
Expostos em `Protocolo.Contracts` para consumo cross-cutting:
- **Comandos (in)**: `AutuarProcessoRequested`, `JuntarDocumentoRequested`, `SolicitarAssinaturaRequested`, `TramitarProcessoRequested`, `ArquivarProcessoRequested`.
- **Eventos (out)**: `ProcessoAutuadoIntegrationEvent` (NUP, origemModulo, origemId), `DocumentoAssinadoIntegrationEvent`, `ProcessoTramitadoIntegrationEvent`, `ProcessoArquivadoIntegrationEvent`.
Ex.: Licitações publica `AutuarProcessoRequested(origem="Licitacao", edital)`; RH publica para férias. O Protocolo responde com `ProcessoAutuadoIntegrationEvent` carregando o NUP de volta ao módulo originador.

## 8. Cenários BDD
**Autuação de requerimento do cidadão**
- *Given* um `Requerimento` válido recebido pelo portal
- *When* o cidadão protocola o pedido
- *Then* um `Processo` é autuado, um NUP único é gerado e `ProcessoAutuado` é publicado.

**Assinatura por criticidade**
- *Given* um `Documento` que é ato do dirigente máximo sobre bem imóvel
- *When* a assinatura **simples** é tentada
- *Then* o sistema exige **assinatura qualificada (ICP-Brasil)** e rejeita a simples.

**Imutabilidade de documento juntado**
- *Given* um `Documento` já juntado a um `Processo`
- *When* solicita-se a exclusão
- *Then* a operação é negada e só é permitido torná-lo *sem efeito*, preservando a trilha.

**Acesso a processo sigiloso**
- *Given* um `Processo` com `NivelDeAcesso = sigiloso`
- *When* usuário sem autorização tenta visualizá-lo
- *Then* o acesso é negado e a tentativa é registrada em auditoria.

**Autuação solicitada por outro módulo**
- *Given* o módulo Licitações publica `AutuarProcessoRequested(origem="Licitacao")`
- *When* o Protocolo processa o Integration Event
- *Then* autua o `Processo` e responde com `ProcessoAutuadoIntegrationEvent` contendo o NUP.

**Eliminação conforme TTD**
- *Given* um `Processo` arquivado cujo prazo de guarda na `TabelaTemporalidade` expirou
- *When* `PrazoGuardaExpirado` é disparado e a eliminação é autorizada
- *Then* o processo é eliminado e o termo de eliminação é registrado.

## 9. Fontes
- Lei 14.063/2020 — https://www.planalto.gov.br/ccivil_03/_ato2019-2022/2020/lei/l14063.htm
- Lei 9.784/1999 — https://www.planalto.gov.br/ccivil_03/leis/l9784.htm
- CONARQ (TTD, e-ARQ Brasil/SIGAD) — https://www.gov.br/conarq/
- gov.br Assinatura Eletrônica — https://www.gov.br/governodigital/
- Complementares: Lei 11.419/2006; Decreto 8.539/2015; Decreto 10.543/2020; MP 2.200-2/2001.
