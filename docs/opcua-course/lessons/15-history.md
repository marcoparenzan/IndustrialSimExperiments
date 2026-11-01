# Historical Access e trend

## Concetti

Approfondimento progettuale. HistoryRead usa dettagli come ReadRawModifiedDetails per intervallo e limite risultati. Historizing indica raccolta storica; AccessLevel.HistoryRead indica capacità di lettura. Un archivio conserva Value, qualità e timestamp della sorgente. Un grafico che accumula campioni nel browser non dimostra Historical Access.

## Esperimento

Disegna una schermata trend con temperatura storica e live. Il server del corso non implementa HistoryRead: una richiesta deve essere trattata come non supportata, non mascherata con dati generati dalla UI.

## Nel codice C#

Separare storage della simulazione da handler HistoryRead. Implementare pagination con continuation point, rilascio esplicito e limiti di memoria. Per aggregati usa capacità dichiarate e semantiche coerenti.

## Esercizio e verifica

Archivia 1000 campioni, leggi una finestra in pagine da 100, verifica ordine, duplicate e qualità. Ripeti una lettura dopo riavvio del processo e confronta i timestamp.

## Errore da evitare

SourceTimestamp e tempo di inserimento nell'archivio non sono equivalenti. Non calcolare una media ignorando qualità, intervalli mancanti e regole dell'aggregato.

## Obiettivi, prerequisiti e disponibilità

Tempo indicativo: 2–3 ore. Il server iniziale non supporta Historical Access. Questa lezione fornisce il disegno del provider, il flusso client e i criteri per realizzare il laboratorio. Un trend ottenuto conservando notifiche nel browser non è una dimostrazione di HistoryRead.

Il dato storico minimo è un DataValue con Value, StatusCode e SourceTimestamp. Conserva anche tempi di acquisizione/inserimento quando servono, senza sostituirli alla data della misura.

## Il contratto del nodo

Historizing indica raccolta storica della Variable; il bit HistoryRead nell'accesso descrive la capacità di lettura. Dichiarare questi campi senza implementare il servizio inganna il client. Anche autorizzazione utente e limiti di operazione devono essere coerenti.

Scegli inizialmente raw history. Modified history e aggregati hanno regole ulteriori e non sono automaticamente coperti da una query SQL su media e timestamp.

## Query client guidata

Frammento per un futuro server con supporto storico; richiede Session aperta e non è eseguibile con successo sul server corrente:

```csharp
var details = new ReadRawModifiedDetails
{
    IsReadModified = false,
    StartTime = DateTime.UtcNow.AddMinutes(-10),
    EndTime = DateTime.UtcNow,
    NumValuesPerNode = 100,
    ReturnBounds = false
};
var nodes = new HistoryReadValueIdCollection
{
    new() { NodeId = Lessons.Id(session, "Reactor1.Temperature") }
};
session.HistoryRead(null, new ExtensionObject(details),
    TimestampsToReturn.Both, false, nodes,
    out var results, out var diagnostics);
```

Per ogni HistoryReadResult controlla StatusCode, decodifica HistoryData dal relativo ExtensionObject e conserva ContinuationPoint. Il continuation point è opaco: non trattarlo come offset SQL esposto dal protocollo. Per ottenere la pagina successiva ripeti la richiesta appropriata con il token; se interrompi la lettura rilascialo esplicitamente.

Non assumere che la pagina contenga esattamente 100 valori: limiti server, bounds e disponibilità dei dati possono influire sul risultato. Una risposta vuota con stato valido va distinta da una risposta in errore.

## Provider server e storage

Separa tre responsabilità: acquisizione, persistenza e servizio HistoryRead. La simulazione emette misure; l'archivio le conserva; il provider applica intervallo, ordinamento, limiti e continuazione.

Una struttura concettuale per lo storage:

```csharp
public sealed record HistoricalSample(
    string NamespaceUri, string Identifier,
    DateTime SourceTimestamp, double Value, uint StatusCode);
```

Questo record è una scelta interna, non un tipo UA da pubblicare. Per una sorgente che produce più campioni con lo stesso timestamp conserva anche un criterio stabile di ordinamento. La paginazione non deve duplicare o saltare righe a causa di timestamp uguali.

## Esercizio risolto: finestra incompleta

Hai 1000 campioni, dieci con qualità Bad e un intervallo senza dati. Il grafico può disegnare una linea continua interpolando tutto?

**Soluzione.** L'archivio conserva la qualità originale; la UI deve distinguere buchi e campioni non validi. Una media che scarta silenziosamente i Bad cambia il significato dell'indicatore. Per aggregati industriali implementa le regole previste e dichiara le capability supportate.

**Laboratorio di accettazione.** Richiedi 100 campioni per pagina fino al completamento; confronta conteggio e ordine con il dataset inserito; interrompi a metà e rilascia il token; ripeti dopo un riavvio per verificare persistenza. Aggiungi due letture contemporanee per evitare un solo cursore globale condiviso.

La parte live si unisce al trend con una regola esplicita sul confine temporale, così l'ultimo campione storico non viene duplicato dal primo live.

Riferimenti: [Historical Access, Part 11](https://reference.opcfoundation.org/Core/Part11/), [HistoryRead](https://reference.opcfoundation.org/Core/Part4/v105/docs/5.11.3).

[Indice](README.md) · [Uso dei sample](../USAGE.md) · [Architettura](../ARCHITECTURE.md)
