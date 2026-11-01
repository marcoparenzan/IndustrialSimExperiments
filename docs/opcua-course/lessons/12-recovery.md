# Riconnessione e continuità dei dati

## Concetti

TCP, SecureChannel, Session e Subscription hanno tempi e recuperi diversi. Ricreare una sessione è semplice ma perde il contesto precedente. Riattivare una sessione o trasferire subscription può mantenere più continuità se server e identità lo consentono. Republish può recuperare messaggi ancora nel buffer, non creare campioni mai conservati.

## Esperimento

Con web app aperta arresta il server, attendi KeepAlive fallito, riavvia. Le label diventano indisponibili e vengono ripopolate da nuove subscription. Nessun comando viene accodato automaticamente per essere eseguito dopo la riconnessione.

## Nel codice C#

UaConnection imposta KeepAliveInterval=2000 e ricostruisce sessione e item dopo 3 s. Snapshot svuotato, log warning, comandi protetti da SemaphoreSlim. Non implementa SessionReconnectHandler, trasferimento o Republish manuale.

## Esercizio e verifica

Rendi il retry esponenziale con jitter e limite, aggiungi contatori di reconnect e timestamp ultimo dato. Simula rete interrotta senza arrestare il server e confronta con riavvio server.

## Errore da evitare

Non mostrare l'ultimo valore come Good solo perché il renderer è ancora vivo. Per gli eventi persi servono log persistente o ConditionRefresh, secondo il tipo di informazione.

## Obiettivi, prerequisiti e risultato atteso

Tempo indicativo: 120 minuti. Devi avere compreso le lezioni 02, 07 e 11. L'obiettivo non è soltanto far tornare Connected=true: è decidere cosa succede a qualità, comandi in corso, eventi e identità dei nodi durante un guasto.

Apri [UaConnection.cs](../../src/UaCourse.Web/Services/UaConnection.cs). Il sample implementa **ricreazione di sessione e subscription**. Non trasferisce subscription e non promette assenza di perdita dati.

## La strategia effettiva, passo per passo

1. Il servizio apre una Session e configura KeepAliveInterval=2000.
2. Un handler KeepAlive segnala un risultato Bad tramite TaskCompletionSource.
3. Il ciclo attende quel segnale o la cancellazione del servizio.
4. Il finally acquisisce il gate dei comandi, rimuove la sessione attiva e svuota lo snapshot.
5. Tentano la chiusura subscription/sessione e la Dispose.
6. Dopo tre secondi il ciclo risolve di nuovo namespace, nodi e item.

Il ritardo di due secondi non è una garanzia che l'interfaccia diventi offline entro due secondi: contano timeout delle richieste, scheduler e condizioni di rete. Durante teardown i servizi remoti di chiusura possono fallire; il cleanup locale deve comunque proseguire.

Un segnale KeepAlive Bad seguito da uscita normale da lost.Task non entra necessariamente nel catch: la descrizione Status può rimanere quella precedente mentre Connected è false. Il renderer deve considerare Connected, non interpretare una stringa come stato autorevole.

## Quale stato si può recuperare?

| Evento | Scelta semplice del sample | Possibile strategia più evoluta |
|---|---|---|
| Canale interrotto, server vivo | Ricreare la sessione | Riattivare quella esistente |
| Sessione disponibile, client sostituito | Nuove subscription | TransferSubscriptions se autorizzato/supportato |
| Messaggio Publish perso | Nessun recupero applicativo esplicito | Republish dal buffer disponibile |
| Server riavviato | Ricreare tutto | Nuova discovery e ricostruzione modello |
| Allarme attivo dopo reconnect | Non disponibile nel sample | ConditionRefresh |
| Evento transitorio durante outage | Può andare perso | Log persistente e recupero applicativo |

Non sovrapporre queste strategie. ConditionRefresh ricostruisce condizioni retained, non una cronologia completa degli eventi.

## Laboratorio riproducibile

1. Apri la web app e annota un source timestamp.
2. Arresta il processo server, lasciando ASP.NET attivo.
3. Attendi indisponibilità: le label mostrano — e i pulsanti sono disabilitati.
4. Riavvia il server.
5. Attendi nuove label con qualità Good.
6. Leggi una struttura e usa Stop per verificare che anche il percorso comandi sia ricostruito.

Ripeti con un client console appena aperto e confronta NamespaceArray. Il client deve risolvere l'URI dopo ogni nuova sessione, non riutilizzare NodeId con namespace index vecchio.

## Esercizio risolto: retry con backoff e jitter

Frammento C# da integrare al posto del ritardo fisso, non già presente nel servizio:

```csharp
static TimeSpan RetryDelay(int failures)
{
    int attempt = Math.Clamp(failures, 0, 10);
    double ceilingMs = Math.Min(30_000, 1_000 * Math.Pow(2, attempt));
    double delayMs = ceilingMs * (0.5 + Random.Shared.NextDouble() * 0.5);
    return TimeSpan.FromMilliseconds(delayMs);
}
```

**Soluzione.** Incrementa failures sui fallimenti, azzeralo soltanto dopo una connessione considerata stabile e usa Task.Delay(delay, stoppingToken). Se azzeri il contatore appena Session.Create riesce, un endpoint che perde subito la connessione può provocare tentativi troppo frequenti.

## Concorrenza e dati di una sessione precedente

Un callback già in corso può terminare durante il cleanup. Per un client robusto assegna a ogni connessione un generation id e accetta aggiornamenti soltanto dalla generazione attiva. Il sample non implementa ancora questo controllo: svuotare il dizionario da solo non prova che nessun callback vecchio possa scrivere dopo lo svuotamento.

Non accodare automaticamente Start quando offline. Un comando può aver già avuto effetto prima del timeout; la ripetizione deve seguire un contratto idempotente o un correlation id.

**Criterio di completamento dell'esercizio.** Nessun comando usa una sessione disposta, nessun aggiornamento vecchio viene mostrato come corrente, il retry è cancellabile e il client dichiara eventuale perdita di eventi.

Riferimenti: [Session Services](https://reference.opcfoundation.org/Core/Part4/v105/docs/5.7), [Subscription Services](https://reference.opcfoundation.org/Core/Part4/v105/docs/5.14).

[Indice](README.md) · [Uso dei sample](../USAGE.md) · [Architettura](../ARCHITECTURE.md)
