# Read, Write, qualità, timestamp e batch

## Concetti

Read può richiedere più attributi di più nodi nella stessa chiamata. Ogni risultato ha il proprio StatusCode. MaxAge è una richiesta di freschezza, non un polling rate. SourceTimestamp indica il tempo attribuito al dato dalla sorgente; ServerTimestamp il tempo associato dal server alla gestione del dato. Gli attributi non-Value possono avere timestamp diversi o vuoti.

## Esperimento

Esegui client read, write ed errors. La scrittura di Setpoint=65 usa Double; 999 produce BadOutOfRange, una stringa produce BadTypeMismatch e Temperature produce BadNotWritable.

## Nel codice C#

PlantNodeManager imposta AccessLevel e UserAccessLevel e convalida Write nel callback OnWriteValue. Lessons.Write controlla lo StatusCode del singolo risultato. La UI mostra l'esito e riceve il nuovo valore attraverso la subscription.

## Esercizio e verifica

Aggiungi una scrittura batch di due nodi con un valore valido e uno invalido. Verifica entrambi i risultati e il readback, senza presumere atomicità.

## Errore da evitare

Write batch non è una transazione. Non usare double.NaN come normale setpoint: il server del corso lo rifiuta esplicitamente.

## Obiettivi e laboratori

Tempo indicativo: 90 minuti. Imparerai a distinguere risultato del servizio e risultati delle operazioni, validare i valori e verificare il readback. Leggi [RunRead.cs](../../src/UaCourse/Labs/RunRead.cs), [RunWrite.cs](../../src/UaCourse/Labs/RunWrite.cs) e [RunErrors.cs](../../src/UaCourse/Labs/RunErrors.cs).

## La lettura batch è una lista di operazioni

ReadValueId non indica solo un nodo: specifica AttributeId, eventuale IndexRange e DataEncoding. RunRead legge quattro attributi della stessa Variable. La lista dei risultati conserva la corrispondenza con l'ordine delle richieste.

```csharp
var requests = new ReadValueIdCollection
{
    new() { NodeId = Lessons.Id(session, "Reactor1.Temperature"),
            AttributeId = Attributes.Value },
    new() { NodeId = Lessons.Id(session, "Reactor1.Setpoint"),
            AttributeId = Attributes.Value }
};
session.Read(null, 0, TimestampsToReturn.Both, requests,
    out var results, out var diagnostics);
for (var i = 0; i < results.Count; i++)
    Console.WriteLine($"{requests[i].NodeId}: {results[i].StatusCode}");
```

Un errore del servizio può impedire una risposta valida; con una risposta valida può comunque fallire una delle operazioni. Mostrare solo il primo StatusCode o intercettare soltanto eccezioni perde questa granularità.

MaxAge=0 richiede un tentativo di ottenere un valore corrente secondo il comportamento del server; non certifica un accesso sincrono a un sensore fisico. Qui il valore è mantenuto in memoria dal simulatore. TimestampsToReturn.Both chiede entrambi i timestamp, ma gli attributi non-Value non devono essere interpretati come misure fisiche.

## Scrittura e validazione lato server

La richiesta usa Double in modo esplicito:

```csharp
var status = Lessons.Write(session,
    Lessons.Id(session, "Reactor1.Setpoint"), 65.0);
if (StatusCode.IsBad(status))
    throw new ServiceResultException(status);
```

65 e 65.0 non sono equivalenti dal punto di vista del Variant: il primo literal C# è Int32, il secondo Double. Non assumere conversioni automatiche universali del server.

PlantNodeManager combina permessi del nodo e callback di validazione: rifiuta tipi errati, numeri non finiti e target fuori da 20..90. La UI ha min/max, ma questa validazione server rimane indispensabile perché esistono altri client.

Il readback conferma lo stato pubblicato; non dimostra che il processo fisico abbia raggiunto 65 °C. Setpoint è il valore desiderato, Temperature è la risposta dinamica. Separare questi due indicatori è parte della semantica applicativa.

## Esercizio risolto: scrittura parzialmente riuscita

Costruisci una WriteValueCollection con Setpoint=64.0 e Temperature=50.0. Per il primo nodo attendi Good; per il secondo BadNotWritable. Dopo la chiamata leggi entrambi.

**Soluzione.** Il setpoint deve risultare 64. La temperatura continua a dipendere dalla simulazione. Non effettuare rollback inventato sul server: se il dominio richiede atomicità, esponi un Method applicativo che valida l'intera operazione e definisce il comportamento di commit.

| Caso | Esito del sample |
|---|---|
| Setpoint=65.0 | Good |
| Setpoint=999.0 | BadOutOfRange |
| Setpoint="wrong" | BadTypeMismatch |
| Temperature=50.0 | BadNotWritable |
| Nodo inesistente | BadNodeIdUnknown |

Il laboratorio errors verifica anche l'eccezione sollevata dall'helper ReadValue per un nodo inesistente. Non attribuire questo stile di gestione a tutti gli overload Read.

**Autoverifica.** Un valore Bad va sempre sostituito da zero? No: zero è un valore di processo plausibile e confonderebbe assenza dati con misura reale. Conserva qualità separata e visualizza indisponibilità.

Riferimento: [Attribute Service Set](https://reference.opcfoundation.org/Core/Part4/v105/docs/5.11).

[Indice](README.md) · [Uso dei sample](../USAGE.md) · [Architettura](../ARCHITECTURE.md)
