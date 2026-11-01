# Subscription, MonitoredItem, Publish e deadband

## Concetti

SamplingInterval riguarda la misura o il campionamento del nodo; PublishingInterval il ritmo delle risposte Publish. QueueSize limita le notifiche pendenti per item. DiscardOldest decide quale campione sacrificare quando la coda è piena. Il server può rivedere i parametri richiesti. Il client SDK mantiene le richieste Publish per ricevere notifiche.

## Esperimento

Esegui client subscribe. Il laboratorio imposta sampling 100 ms, publishing 250 ms e deadband assoluta 0.1. La prima notifica può arrivare anche senza una variazione significativa; non dimostra da sola che il filtro abbia soppresso altri campioni.

## Nel codice C#

ObserveAsync crea la subscription, crea l'item, applica i cambiamenti e controlla item.Status.Error. ClearChangeMasks nel server rende disponibili le variazioni allo SDK. UaConnection usa più item nella stessa subscription.

## Esercizio e verifica

Confronta richiesti e RevisedSamplingInterval/RevisedQueueSize. Aumenta la variazione simulata, rallenta Publish e osserva la perdita con code piccole. Misura latenza usando source timestamp.

## Errore da evitare

Una subscription non garantisce di ricevere ogni campione della sorgente. Deadband percentuale richiede EURange; il sample usa una deadband assoluta.

## Obiettivi e laboratorio

Tempo indicativo: 120 minuti. Apri [ObserveNotifications.cs](../../src/UaCourse/Labs/ObserveNotifications.cs). Esegui client subscribe e poi osserva il flusso più lungo della web app. Il comando console termina alla prima notifica valida: è uno smoke test, non una misura esaustiva del filtro.

## Le tre frequenze del sample

| Attività | Intervallo | Significato |
|---|---|---|
| Timer del simulatore | 500 ms | Nuovo valore nel server |
| Sampling richiesto console | 100 ms | Controllo del nodo da parte dell'item |
| Publishing richiesto | 250 ms | Ritmo delle risposte di notifica |

Campionare ogni 100 ms non genera cinque misure indipendenti se la sorgente cambia ogni 500 ms. La web app aggiunge un timer di rendering ogni 500 ms, che legge una cache locale: non effettua Read UA periodiche.

## Costruzione guidata

```csharp
using var subscription = new Subscription(session.DefaultSubscription)
{
    PublishingInterval = 250
};
session.AddSubscription(subscription);
subscription.Create();

var item = new MonitoredItem(subscription.DefaultItem)
{
    StartNodeId = Lessons.Id(session, "Reactor1.Temperature"),
    AttributeId = Attributes.Value,
    SamplingInterval = 100,
    QueueSize = 20,
    DiscardOldest = true
};
subscription.AddItem(item);
subscription.ApplyChanges();
```

Subscription.Create crea la risorsa server; AddItem cambia la descrizione client; ApplyChanges rende effettive le modifiche degli item. Non assumere che costruire un oggetto C# equivalga a creare automaticamente un MonitoredItem remoto.

Controlla item.Status.Error dopo la creazione. I parametri possono essere rivisti: misura quelli effettivi dallo stato SDK, non solo quelli assegnati all'oggetto prima della richiesta.

## Filtri e qualità

La console usa DataChangeTrigger.StatusValue e deadband assoluta 0.1. Il filtro considera cambi di stato e valore secondo il trigger; non usa il timestamp come evento autonomo. Una deadband assoluta ha l'unità del valore: 0.1 significa 0.1 °C per questa Temperature.

La deadband percentuale richiede una scala appropriata dichiarata con EURange. EngineeringUnits da sola non definisce quella scala. Il primo valore dell'item viene normalmente notificato all'avvio: riceverlo non dimostra che altri valori siano stati filtrati.

## Code, perdita e Publish

QueueSize appartiene al MonitoredItem. DiscardOldest=true conserva i campioni più recenti quando la coda è piena; false cambia quale campione viene scartato. La coda dell'item non è la stessa cosa del buffer di ritrasmissione dei messaggi della subscription.

Il client mantiene richieste Publish; il server le utilizza per consegnare notifiche o keep-alive. Republish riguarda messaggi ancora disponibili nel buffer della subscription e non recupera un campione già scartato prima di essere incluso in un messaggio.

Il callback del sample usa DequeueValues e sposta i dati nello snapshot. Non chiamare servizi remoti lenti dentro ogni callback: rallenterebbe il consumo e complicherebbe il controllo del carico.

## Esercizio risolto: dato live o ogni campione?

Vuoi visualizzare un livello live e archiviare ogni misura per audit. Una sola label con queue=1 può soddisfare entrambi?

**Soluzione.** Per il live interessa soprattutto il valore corrente; per l'archivio interessa la completezza. Definisci un percorso di acquisizione/archiviazione con perdita misurabile, capacità adeguata e possibilmente recupero storico. Il renderer può consumare uno snapshot ridotto senza determinare ciò che conserva l'archivio.

**Prova.** In un laboratorio modificato mantieni la subscription per 30 s, confronta valori con deadband 0 e 0.1, stampa parametri rivisti e numero notifiche. Non aspettarti conteggi esatti su wall clock: scheduler e publishing producono variazioni. Verifica invece ordine e comportamento qualitativo.

Riferimenti: [MonitoredItem Services](https://reference.opcfoundation.org/Core/Part4/v105/docs/5.13), [Subscription Services](https://reference.opcfoundation.org/Core/Part4/v105/docs/5.14).

[Indice](README.md) · [Uso dei sample](../USAGE.md) · [Architettura](../ARCHITECTURE.md)
