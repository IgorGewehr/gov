# Saúde e Segurança do Trabalho (SST) — Rules-as-Code (RecursosHumanos)

> **Bounded Context:** RecursosHumanos · **Agregados:** `ComunicacaoAcidente` (CAT),
> `ExameOcupacional` (ASO/PCMSO), `ExposicaoAgenteNocivo` (PPP/condições ambientais)
> **Base legal/normativa:** CAT — Lei 8.213/1991 art. 22; ASO/PCMSO — NR-07; agentes nocivos/PPP — NR-09
> e Dec. 3.048/1999. Eventos SST do **eSocial**: **S-2210** (CAT), **S-2220** (monitoramento da saúde do
> trabalhador / ASO) e **S-2240** (condições ambientais do trabalho — agentes nocivos).
> **Regra de ouro (CLAUDE.md §16):** leiautes/regras seguem o CONCEITO oficial com `// TODO(validar-oficial)`
> nos pontos exatos; dados de saúde são **sensíveis (LGPD)** — base legal explícita e trilha de acesso.

## Linguagem ubíqua

- **CAT (Comunicação de Acidente de Trabalho):** comunica acidente/doença ocupacional; tipos inicial,
  reabertura e comunicação de óbito; prazo legal de comunicação (1º dia útil; imediato em óbito).
- **ASO (Atestado de Saúde Ocupacional):** resultado do exame ocupacional (admissional, periódico,
  retorno ao trabalho, mudança de risco, demissional), com aptidão e médico responsável (CRM).
- **PCMSO:** programa de controle médico — agenda de exames periódicos por servidor/risco.
- **Exposição a agente nocivo:** período de exposição a agente (físico/químico/biológico) com técnica de
  medição/EPC/EPI, base do PPP e da aposentadoria especial; tem início e encerramento.
- **PPP (Perfil Profissiográfico Previdenciário):** consolida as exposições do servidor ao longo do vínculo.

## Invariantes

- **SST-1:** CAT exige servidor existente no tenant; a comunicação registra data do acidente ≤ data da emissão.
- **SST-2:** ASO vincula médico responsável com CRM válido; a aptidão e o tipo de exame são obrigatórios.
- **SST-3:** exposição a agente nocivo tem início; o encerramento exige data ≥ início (período consistente).
- **SST-4:** geração de evento eSocial SST é **idempotente** pela chave natural do evento (tipo + id de origem):
  S-2210 por CAT, S-2220 por ASO, S-2240 por exposição — reemitir não duplica.
- **SST-5:** cancelamento de registro SST (motivo obrigatório) é auditável e não apaga o histórico (append-only).
- **SST-6:** dado de saúde é sensível (LGPD §6): acesso minimizado e com trilha (quem leu, quando, por quê).

## Gancho com o eSocial

- Os agregados SST são a **fonte** dos eventos S-2210/S-2220/S-2240; a geração traduz o registro em insumo
  do evento (vínculo, não recálculo) e segue o ciclo gerar → assinar → transmitir do destino eSocial.

## TODO(validar-oficial)

- Leiaute exato dos eventos **S-2210/S-2220/S-2240** (grupos/campos obrigatórios) — manual do eSocial vigente.
- Prazos e regras de retificação/exclusão da CAT e dos eventos de saúde.
- Tabelas de agentes nocivos e de procedimentos diagnósticos (anexos do eSocial).

<!-- manifest
commands: ComunicarAcidente, RegistrarExameOcupacional, RegistrarExposicaoAgenteNocivo, EncerrarExposicaoAgenteNocivo, GerarS2210, GerarS2220, GerarS2240
queries: ListarComunicacoesAcidente, ListarExamesOcupacionais, ListarExposicoes, ObterAgendaPcmso, ObterPerfilProfissiografico
domainEvents: ComunicacaoAcidenteRegistrada, ExameOcupacionalRegistrado, ExposicaoAgenteNocivoIniciada, ExposicaoAgenteNocivoEncerrada, RegistroSstCancelado
integrationEventsPublished: 
integrationEventsConsumed: 
-->
