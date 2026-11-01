# EventType, EventNotifier e filtri

## Concetti

Gli eventi non sono variazioni di una Variable. Un EventType definisce campi e gerarchia; un nodo con EventNotifier abilita l'iscrizione. EventFilter.SelectClauses sceglie i campi restituiti; WhereClause può restringere gli eventi. EventId identifica l'occorrenza e SourceNode la sorgente.

## Esperimento

Esegui client events: il client sottoscrive Reactor1 e ApplyRecipe produce un evento. Il server genera anche eventi periodici. Il browser mantiene gli ultimi 50 e ne mostra gli ultimi otto.

## Nel codice C#

Emit crea BaseEventState, imposta messaggio, severity e sorgente e chiama reactor.ReportEvent. Lessons.EventFilter richiede EventId, Time, SourceName, Message e Severity. La posizione dei campi in EventFieldList segue le SelectClauses.

## Esercizio e verifica

Aggiungi un filtro Severity >= 200 con ContentFilter e OfType. Poi definisci RecipeAppliedEventType con RecipeName e Target. Conserva anche EventId per correlazione e deduplicazione.

## Errore da evitare

Un messaggio con severity elevata non diventa automaticamente un allarme. Una coda eventi può perdere elementi; il sample non persiste il log.

## Obiettivi e laboratorio

Tempo indicativo: 90 minuti. Usa client events, [ObserveNotifications.cs](../../src/UaCourse/Labs/ObserveNotifications.cs), Lessons.EventFilter e PlantNodeManager.Emit. Il sample pubblica BaseEventType, non ancora un tipo applicativo personalizzato.

## Da evento server a campi client

La sorgente è Reactor1, che dichiara EventNotifier.SubscribeToEvents ed è registrata come root notifier. Il server crea BaseEventState, imposta i campi e chiama ReportEvent. Un evento non deve essere cercato come un nuovo nodo Variable a ogni occorrenza.

Sul client il MonitoredItem usa AttributeId=EventNotifier. Non usa Attributes.Value. La configurazione del filtro determina l'array consegnato in EventFieldList.

```csharp
var filter = Lessons.EventFilter();
for (var i = 0; i < filter.SelectClauses.Count; i++)
    Console.WriteLine($"{i}: {filter.SelectClauses[i].BrowsePath}");
```

Per il filtro corrente l'ordine è:

| Indice | Campo | Uso |
|---|---|---|
| 0 | EventId | Identità dell'occorrenza |
| 1 | Time | Tempo dell'evento |
| 2 | SourceName | Sorgente leggibile |
| 3 | Message | LocalizedText per la presentazione |
| 4 | Severity | Priorità numerica UA |

EventId è ByteString: per una visualizzazione stabile usa Base64 o esadecimale, non il ToString generico di byte[]. EventFieldList restituisce Variant nello stesso ordine delle SelectClauses; non cercare le proprietà per nome come se fosse un record JSON.

## Selezionare e filtrare sono operazioni diverse

SelectClauses decide quali campi ricevere. WhereClause decide quali eventi passano. Ridurre le colonne non equivale a ridurre il numero di eventi. Per filtrare una famiglia di tipi usa OfType; per una severity usa il confronto sul campo.

Prima di creare un filtro complesso, verifica che il server supporti gli operatori necessari e controlla il risultato della creazione dell'item, inclusa la diagnostica del filtro. Un filtro non valido non diventa automaticamente una subscription senza filtro.

Per campi annidati, BrowsePath è una sequenza di QualifiedName a partire dalla definizione del tipo. Per esempio il campo Id di uno stato a due valori richiede un percorso differente dal suo testo leggibile.

## Laboratorio guidato e aspettative

1. Avvia client events: viene creato l'item e invocato ApplyRecipe(68).
2. Attendi un EventFieldList con messaggio della ricetta e severity 300.
3. Tieni aperta la web app: ricevi anche gli eventi periodici a severity 100.
4. Arresta il reattore: il server emette il messaggio di arresto.
5. Osserva che l'elenco UI è limitato: conserva 50 stringhe e mostra gli ultimi otto eventi.

Il comando console può ricevere un evento periodico prima di quello applicativo; il suo smoke test prova il canale eventi, non la correlazione esatta con un comando. Per un test di correlazione aggiungi CommandId a un tipo evento applicativo e verifica il valore ricevuto.

## Esercizio risolto: evitare doppioni senza perdere significato

Durante recovery ricevi di nuovo un evento già registrato. Puoi deduplicarlo per Message e Time?

**Soluzione.** Non è affidabile: due eventi distinti possono avere lo stesso testo e timestamp arrotondato. Conserva EventId insieme all'identità della sorgente/server e al contesto necessario. L'attuale servizio web scarta EventId nella stringa di presentazione: per una tabella affidabile sostituisci le stringhe con record di evento tipizzati.

**Progetto.** RecipeAppliedEventType aggiunge RecipeName, Target e CommandId. Definisci il tipo, pubblica i campi e aggiorna SelectClauses. Valida con un client che seleziona sia campi BaseEventType sia quelli custom.

Riferimenti: [Event model, Part 5](https://reference.opcfoundation.org/Core/Part5/), [MonitoredItem Services](https://reference.opcfoundation.org/Core/Part4/v105/docs/5.13).

[Indice](README.md) · [Uso dei sample](../USAGE.md) · [Architettura](../ARCHITECTURE.md)
