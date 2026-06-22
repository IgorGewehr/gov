# Módulo RecursosHumanos
> Gestão do ciclo de vida de servidores e empregados públicos, folha de pagamento e ponto, com transmissão ao eSocial/DCTFWeb. · Poder: Ambos · Schema EF Core: `recursoshumanos` · Ativável por tenant.

## 1. Propósito & Marco Legal
Centraliza pessoal, folha e ponto para entes públicos (Executivo e Legislativo) com conformidade fiscal e previdenciária. **Marco legal:** CF/1988 arts. 37–41 — concurso público obrigatório para cargo efetivo (art. 37, II), teto remuneratório (art. 37, XI), estabilidade após 3 anos de efetivo exercício (art. 41); **Lei 8.112/1990** (RJU federal, referência supletiva ao estatuto municipal próprio do tenant); **EC 103/2019** e leis do RPPS local (regime previdenciário do efetivo); **CLT** (empregados públicos celetistas); **eSocial** (Decreto 8.373/2014, leiautes S-1.3); **DCTFWeb** (IN RFB 2.005/2021 — obrigatória para entes públicos desde jul/2022); **ponto eletrônico** Portaria MTP 671/2021 (REP-P/REP-C, AEJ) e Decreto 10.854/2021.

## 2. Linguagem Ubíqua
1. **ServidorEstatutario** — vínculo regido por estatuto/RJU, regime RPPS.
2. **EmpregadoPublico** — vínculo celetista (CLT), regime RGPS.
3. **CargoPublico** — posição na estrutura, efetivo/comissionado/temporário.
4. **Provimento** — ato de preenchimento de cargo (nomeação).
5. **Posse** — aceitação formal das atribuições; caduca no prazo legal.
6. **Exercicio** — início efetivo do desempenho das funções.
7. **Estabilidade** — garantia adquirida após 3 anos de efetivo exercício.
8. **RPPS** — Regime Próprio de Previdência Social (servidor efetivo).
9. **RGPS** — Regime Geral de Previdência Social (celetista/comissionado/temporário).
10. **Provento** — verba de natureza creditícia na folha.
11. **Desconto** — verba de natureza debitória na folha.
12. **Consignacao** — desconto autorizado por terceiro (empréstimo, mensalidade).
13. **TetoRemuneratorio** — limite constitucional aplicado via abate-teto.
14. **Rubrica** — código que classifica provento/desconto (incidências).
15. **Matricula** — identificador único do vínculo no tenant.
16. **REP-P** — Registrador Eletrônico de Ponto via Programa.
17. **AEJ** — Arquivo Eletrônico de Jornada (tratamento de marcações).
18. **DCTFWeb** — declaração de débitos previdenciários, gerada pós-fechamento.
19. **Competencia** — mês/ano de referência da folha (`AAAA-MM`).

## 3. Mapa de Domínio
**Agregados (raiz : IMustHaveTenant):**
- **Servidor** (raiz) — VOs `CPF`, `Matricula`, `DadosPessoais`; entidade `Dependente`.
- **Cargo** (raiz) — VOs `Vencimento`, `Lotacao`; entidade `PlanoDeCargos`.
- **Vinculo** (raiz) — associa `Servidor` ↔ `Cargo` ↔ `Regime` (RPPS/RGPS).
- **FolhaDePagamento** (raiz, por `Competencia`) — entidade `EventoFolha` (provento/desconto); VOs `Rubrica`, `BaseCalculo`, `LiquidoAPagar`.
- **CartaoPonto** (raiz) — entidade `Marcacao`; VO `Jornada`; entidade `BancoDeHoras`.

**Value Objects:** `CPF`, `Matricula`, `DadosPessoais`, `Vencimento`, `Lotacao`, `Rubrica`, `BaseCalculo`, `LiquidoAPagar`, `Jornada`, `Competencia`.

**Eventos de Domínio:** `ServidorAdmitido`, `PosseRegistrada`, `ExercicioIniciado`, `EstabilidadeConcedida`, `AfastamentoRegistrado`, `FolhaCalculada`, `FolhaFechada`, `PagamentoEfetuado`, `ServidorDesligado`, `PontoApurado`.

## 4. Integrações & Padrões Técnicos
**eSocial** — web service SOAP, XML assinado com certificado A1; ambientes **Produção Restrita** e **Produção**; cada lote retorna protocolo e cada evento gera recibo persistido.
- Tabelas: **S-1000** (empregador), **S-1005** (estabelecimentos), **S-1010** (rubricas), **S-1020** (lotação tributária).
- Não periódicos: **S-2200** (admissão), **S-2206** (alteração contratual), **S-2230** (afastamento), **S-2299** (desligamento).
- Periódicos: **S-1200** (remuneração RGPS), **S-1202** (remuneração RPPS — entes públicos), **S-1210** (pagamentos), **S-1299** (fechamento); totalizadores **S-5001/5002/5003**.

**DCTFWeb** — gerada automaticamente após fechamento da folha (S-1299) a partir dos totalizadores.
**REP / Portaria 671** — marcações de REP-P/REP-C consolidadas em **AEJ** para apuração.
**Padrões:** integração externa via **ACL** (Anti-Corruption Layer); resiliência com **Polly** (retry/circuit-breaker) sobre o SOAP; envio assíncrono via **Outbox**.

## 5. Regras de Negócio Críticas
- Concurso público obrigatório para cargo efetivo; sequência **Nomeação → Posse → Exercício**; a posse **caduca** se não ocorrer no prazo legal.
- Estabilidade concedida apenas após **3 anos** de efetivo exercício (efetivos).
- Efetivo → **RPPS** (S-1202); temporário/comissionado/celetista → **RGPS** (S-1200).
- **Abate-teto** aplicado quando a soma de proventos excede o teto remuneratório.
- **Não excluir** S-1200/S-1202/S-2299 enquanto houver **S-1210 vinculado**.
- Rubricas devem existir em **S-1010** e estabelecimentos em **S-1005**, ambos vigentes na competência.
- Prazos: **S-2200** até a véspera do início do exercício; eventos periódicos até o **dia 15** do mês seguinte.
- Marcações de ponto são **imutáveis**; correções só via tratamento no **AEJ**.

## 6. Multi-Tenancy, Segurança & Auditoria
Toda raiz de agregado implementa **IMustHaveTenant**; `TenantId` no global query filter do EF Core (schema `recursoshumanos`). Certificados A1 e credenciais eSocial isolados por tenant. **LGPD:** dados pessoais de servidores e dependentes tratados como sensíveis (minimização, controle de acesso por perfil, mascaramento de CPF em logs). Auditoria via Outbox + trilha imutável de eventos de domínio; recibos/protocolos eSocial preservados como prova fiscal.

## 7. Contratos Públicos (Integration Events)
Cross-module **somente** via Integration Events (publicados pelo Outbox; consumo via ACL):
- `ServidorAdmitidoIntegrationEvent` — para Cadastro/Patrimônio (alocação).
- `FolhaFechadaIntegrationEvent` — para Contabilidade/Empenho (despesa de pessoal).
- `PagamentoEfetuadoIntegrationEvent` — para Tesouraria (liquidação financeira).
- `ServidorDesligadoIntegrationEvent` — para Acesso/Patrimônio (revogação).

## 8. Cenários BDD
**Cenário 1 — Admissão de servidor efetivo**
Dado um candidato aprovado em concurso e nomeado para cargo efetivo
Quando registro a admissão antes da véspera do exercício
Então o evento `ServidorAdmitido` é emitido e o **S-2200** é enfileirado para o eSocial.

**Cenário 2 — Posse caducada**
Dado um servidor nomeado sem posse dentro do prazo legal
Quando o prazo de posse expira
Então o provimento é tornado sem efeito e nenhum `PosseRegistrada` é emitido.

**Cenário 3 — Fechamento de folha com RPPS**
Dada uma `FolhaDePagamento` calculada na competência `2026-06` com servidores efetivos
Quando executo o fechamento
Então `FolhaFechada` é emitido, **S-1202** é gerado para os efetivos e a **DCTFWeb** é produzida.

**Cenário 4 — Abate-teto**
Dado um servidor cujos proventos somados ultrapassam o teto remuneratório
Quando a folha é calculada
Então é lançada rubrica de abate-teto reduzindo o `LiquidoAPagar` ao limite constitucional.

**Cenário 5 — Exclusão bloqueada de evento periódico**
Dado um **S-1200** com **S-1210** vinculado já transmitido
Quando solicito a exclusão do S-1200
Então a operação é rejeitada exigindo a exclusão prévia do S-1210.

**Cenário 6 — Apuração de ponto imutável**
Dadas marcações de REP-P importadas para o `CartaoPonto`
Quando o gestor ajusta a jornada
Então as marcações originais permanecem intactas, o tratamento ocorre no **AEJ** e `PontoApurado` é emitido.

## 9. Fontes
- eSocial — Documentação Técnica: https://www.gov.br/esocial/pt-br/documentacao-tecnica
- Lei 8.112/1990 (RJU): https://www.planalto.gov.br/ccivil_03/leis/l8112cons.htm
- Portaria MTP 671/2021 (ponto/AEJ): https://www.gov.br/trabalho-e-emprego/
- DCTFWeb (IN RFB 2.005/2021): https://www.gov.br/receitafederal/
