# Ata de Registro de Preços (ARP) — Rules-as-Code

Bounded Context: **Administracao**. Sistema de Registro de Preços (art. 82-86, Lei 14.133/2021):
a ata registra preços, fornecedores beneficiários e quantidades para contratações futuras, com
vigência (1 ano + prorrogação, art. 84), saldo por item e adesão/carona (art. 86).

## Invariantes
- I-1: número da ata obrigatório e único por tenant; vigência final posterior ao início.
- I-2: ata nasce **Vigente**; operações de registro/contratação/adesão exigem ata Vigente.
- I-3: item registrado vincula item de catálogo **Ativo** + fornecedor beneficiário + preço + quantidade registrada.
- I-4: o mesmo par (item de catálogo, fornecedor) não pode ser registrado duas vezes na mesma ata.
- I-5: contratação direta e adesão (carona) **debitam o saldo**; quantidade não pode exceder o saldo disponível.
- I-6: adesão/contratação só dentro do período de vigência da ata.
- I-7: cancelamento (ato administrativo, motivo obrigatório) e encerramento (por decurso) tiram a ata de Vigente.
- I-8: órgão participante (art. 82 §1º — IRP) só pode ser incluído enquanto a ata está Vigente; CNPJ do órgão único por ata.
- I-9: remanejamento de quantidade registrada de um item (art. 82 §5º/§6º) não pode reduzir abaixo do já contratado/aderido (saldo não fica negativo).
- I-10: prorrogação da vigência (art. 84) exige vantajosidade comprovada e nova data final posterior à vigente; respeita o teto legal de prazo.

## Fluxo
Registrar ata → (IncluirParticipante)* → (RegistrarItem | RemanejarItem)* → (ContratarItem | RegistrarAdesão)* → (Prorrogar)? → (Cancelar | Encerrar).

<!-- manifest
commands: RegistrarAta, RegistrarItemAta, RegistrarAdesao, IncluirParticipanteAta, ContratarItemAta, RemanejarItemAta, ProrrogarAta, CancelarAta
queries: ListarAtas, ObterAtaPorId
domainEvents: 
integrationEventsPublished: 
integrationEventsConsumed: 
-->
