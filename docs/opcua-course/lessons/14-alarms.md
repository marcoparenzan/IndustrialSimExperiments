# Alarms & Conditions: stato, acknowledge e refresh

## Concetti

Approfondimento progettuale. ConditionType rappresenta uno stato significativo; AlarmConditionType aggiunge ActiveState e semantica d'allarme. AckedState e ConfirmedState sono azioni distinte. Retain indica che la condizione deve essere presentata al client. Le branch preservano stati precedenti che richiedono ancora attenzione.

## Esperimento

Progetta un allarme di livello basso per Well1 con soglia e isteresi. Il server attuale emette solo BaseEventType: non ci sono Acknowledge, Confirm né ConditionRefresh.

## Nel codice C#

Estendi il NodeManager con un tipo d'allarme standard e metodi standard. Il client seleziona ConditionId, EventId, Retain, ActiveState/Id e AckedState/Id. La vista allarmi deve mantenere un elenco per condizione/branch, diverso dal log cronologico.

## Esercizio e verifica

Attiva l'allarme, acknowledge, rientro, reconnect e ConditionRefresh. Verifica stato retained dopo reconnect e acknowledge con EventId scaduto. Distingui rientro fisico da presa visione.

## Errore da evitare

Non implementare acknowledge scrivendo un Boolean a caso. I servizi e metodi delle condizioni hanno regole di stato e identificatori specifici.

## Obiettivi e confine del laboratorio

Tempo indicativo: 2–3 ore. Questa lezione non usa il log BaseEventType come se fosse già un sistema allarmi. Il server corrente **non implementa Alarms & Conditions**. L'obiettivo è definire e verificare il contratto prima di aggiungere i relativi NodeState e metodi standard.

Parti da Well1: genera un allarme quando il livello scende sotto una soglia e definisci un'isteresi per evitare oscillazioni continue. Il valore misurato e lo stato dell'allarme sono informazioni differenti.

## Stato fisico, presa visione e permanenza

| Campo | Domanda |
|---|---|
| ActiveState | La situazione d'allarme è attualmente attiva? |
| AckedState | È stata presa in carico/riconosciuta? |
| ConfirmedState | È stata confermata, se il modello lo richiede? |
| EnabledState | La condizione è abilitata? |
| Retain | Deve restare nell'elenco corrente del client? |
| BranchId | A quale stato/branch appartiene la notifica? |

Acknowledge non deve spegnere una pompa né risolvere il guasto per definizione. Il rientro fisico non equivale automaticamente ad acknowledge. La policy Retain dipende dal tipo e dagli stati che il server mantiene: non fissarla sempre uguale ad ActiveState.

Un ConditionId identifica la condizione; EventId identifica un'occorrenza/stato notificato. Un server può mantenere branch precedenti quando occorre ancora riconoscere uno stato passato. Usare un solo Boolean nel browser perde questa distinzione.

## Progettare il rilevamento con isteresi

Questo codice è logica di dominio, **non un'implementazione del protocollo A&C**:

```csharp
static bool NextActive(bool wasActive, double level,
    double lowLimit, double hysteresis)
{
    if (!double.IsFinite(level))
        throw new ArgumentOutOfRangeException(nameof(level));
    return wasActive
        ? level < lowLimit + hysteresis
        : level < lowLimit;
}
```

Con lowLimit=5 e hysteresis=0.3, una misura 4.9 attiva l'allarme; 5.1 non lo disattiva ancora; 5.3 permette il rientro. La scelta di soglie strettamente minori è esplicita: scrivi i test sui valori esattamente uguali ai confini.

Il livello di qualità Bad richiede una decisione separata: non far rientrare l'allarme usando zero o un valore mancante. Puoi mantenere l'ultimo stato e segnalare qualità incerta oppure esporre una condizione di sensore guasto.

## Implementazione guidata

1. Scegli un tipo standard appropriato, per esempio un limite non esclusivo per il livello.
2. Crea l'istanza di condizione e collega SourceNode e la sorgente notificabile.
3. Aggiorna campi di stato, qualità, severity e timestamp coerentemente.
4. Implementa i metodi standard e i controlli su EventId/branch.
5. Nel client seleziona ConditionId, EventId, BranchId, Retain e gli Id degli stati.
6. Mantieni un dizionario di condizioni/branch, distinto dal log cronologico.
7. Dopo reconnect esegui ConditionRefresh e gestisci correttamente gli eventi di inizio/fine refresh.

Gli aggiornamenti durante il refresh non devono essere scambiati per nuove occorrenze tutte da aggiungere al log. La tabella corrente si riconcilia con lo stato server.

## Esercizio risolto: outage e rientro

Sequenza: l'allarme si attiva, il client si scollega, il livello rientra, l'allarme non è ancora acknowledged, il client torna.

**Soluzione.** Il client deve recuperare gli stati retained dal server, non ricostruirli soltanto dalle misure correnti. Se la policy mantiene un ramo non riconosciuto, il refresh deve mostrarlo anche se ActiveState è false. Eventi persi durante l'outage e stato corrente della condizione sono due problemi distinti.

**Criteri di accettazione.** Acknowledge con EventId non valido viene rifiutato; le branch non si sovrascrivono; il refresh riconcilia l'elenco; la UI distingue allarme attivo e allarme rientrato da riconoscere.

Riferimento: [Alarms & Conditions, Part 9](https://reference.opcfoundation.org/Core/Part9/).

[Indice](README.md) · [Uso dei sample](../USAGE.md) · [Architettura](../ARCHITECTURE.md)
