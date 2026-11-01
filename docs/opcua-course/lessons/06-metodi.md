# Method, argomenti e semantica dei comandi

## Concetti

Call identifica ObjectId e MethodId: il metodo appartiene a un oggetto. InputArguments e OutputArguments descrivono nomi, DataType, ValueRank e descrizione. Un comando può avere precondizioni di processo oltre alla validazione dei tipi. BadInvalidState indica che il comando non è valido nello stato attuale.

## Esperimento

Esegui client methods: Stop, ApplyRecipe(70), Start. Nel browser applica una ricetta e osserva risposta e evento. Invoca Start due volte: la seconda chiamata viene rifiutata.

## Nel codice C#

MethodState.OnCallMethod implementa il comportamento e riempie la lista output. Il server verifica numero e tipi degli argomenti, il callback verifica range e stato. Il metodo ApplyRecipe imposta il target; non aggiorna ancora una ricetta complessa persistente.

## Esercizio e verifica

Aggiungi ResetTotalizer() con una precondizione di impianto fermo. Per operazioni lunghe restituisci un identificatore di job e pubblica stato e completamento.

## Errore da evitare

Un timeout Call non dimostra che il comando non sia stato eseguito. Evita retry ciechi di operazioni non idempotenti; progetta correlation id e risultato consultabile.

## Obiettivi e laboratorio

Tempo indicativo: 90 minuti. Leggi [RunMethods.cs](../../src/UaCourse/Labs/RunMethods.cs) e i callback MethodState in [PlantServer.cs](../../src/UaCourse/PlantServer.cs). Esegui client methods con server attivo e osserva i nuovi eventi nella web app.

## Una chiamata remota ha un contratto pubblico

ObjectId identifica l'oggetto su cui operare. MethodId identifica il metodo. Non sono intercambiabili, anche se nel sample entrambi contengono la stringa Reactor1.

La firma non è una convenzione del nome: InputArguments e OutputArguments sono proprietà UA che contengono un array di Argument. Ogni Argument descrive Name, DataType, ValueRank e Description. Un client generico può leggere la firma prima di invocarla.

| Metodo | Input | Output | Regola del sample |
|---|---|---|---|
| Start | Nessuno | Accepted: Boolean | Rifiuta se già in marcia |
| Stop | Nessuno | Nessuno | Arresto ripetibile |
| ApplyRecipe | Target: Double | Target: Double | Target finito, 20..90 |

Stop è idempotente per il valore Running, ma emette comunque un nuovo evento a ogni chiamata: idempotenza dello stato ed equivalenza di tutti gli effetti non sono necessariamente la stessa cosa.

## Client: invocazione e risultato

```csharp
NodeId reactor = Lessons.Id(session, "Reactor1");
NodeId apply = Lessons.Id(session, "Reactor1.ApplyRecipe");
IList<object> output = session.Call(reactor, apply, 70.0);
double acceptedTarget = (double)output[0];
Console.WriteLine(acceptedTarget);
```

L'helper Call usato qui restituisce gli output e può sollevare ServiceResultException. Per più chiamate nella stessa richiesta o diagnostica fine usa il servizio Call e i suoi CallMethodResult: status della chiamata, risultati dei singoli argomenti e output sono livelli distinti.

Una UI non dovrebbe concludere che il processo ha raggiunto il target soltanto perché acceptedTarget vale 70. Il metodo conferma l'azione sul contratto del server; Temperature è l'indicatore del processo.

## Server: validazione ed effetto

ApplyRecipe legge input[0], verifica finitezza e range, aggiorna Setpoint tramite Set, riempie output[0] ed emette un evento. Il Lock coordina questo comportamento con il timer di simulazione. Lo SDK verifica la compatibilità della richiesta con la firma; il dominio verifica la legalità del comando.

L'array di output è predisposto per la firma: il callback assegna le posizioni previste. Restituire Good senza valorizzare un output richiesto produce un contratto ambiguo o invalido.

## Timeout e comandi lunghi

Il caso critico è: il server ha eseguito il comando, ma la risposta non arriva. Un timeout del client non prova che l'azione non sia avvenuta. Ripetere un comando di dosaggio può duplicare l'operazione.

Per un comando lungo progetta StartBatch(CommandId, Recipe) → JobId. Il server conserva CommandId e risultato; un retry con lo stesso identificatore recupera il job esistente. Stato, avanzamento e completamento diventano nodi o eventi. Questo pattern è un progetto applicativo, non un'API SDK già implementata nel corso.

## Esercizio risolto

Chiama Stop, Start, Start. Il secondo Start deve fallire con BadInvalidState, mentre Running resta true. Poi prova ApplyRecipe(999.0): il target precedente deve rimanere invariato.

**Soluzione.** Leggi lo stato prima e dopo, cattura ServiceResultException per il comando rifiutato e verifica il readback. Non usare l'assenza di un evento come unica conferma: una subscription può perdere notifiche.

**Diagnostica.** BadArgumentsMissing suggerisce una firma non rispettata; BadTypeMismatch indica un tipo incompatibile; BadInvalidState indica una precondizione applicativa. Un pulsante disabilitato aiuta l'utente, ma non sostituisce il controllo server.

Riferimento: [Method Service Set](https://reference.opcfoundation.org/Core/Part4/v105/docs/5.12).

[Indice](README.md) · [Uso dei sample](../USAGE.md) · [Architettura](../ARCHITECTURE.md)
