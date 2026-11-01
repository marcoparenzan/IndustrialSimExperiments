# State machine, FileType e contratti di processo

## Concetti

Approfondimento progettuale. StateMachineType rende espliciti stati e transizioni; CurrentState e LastTransition sono dati navigabili. FileType espone Open/Read/Write/Close con handle, Size e permessi. Questi contratti evitano metodi inventati e blob senza semantica condivisa.

## Esperimento

Estendi il ciclo del reattore in Idle, Heating, Holding, Completed e Fault. Definisci quali comandi sono legali e quali transizioni generano eventi. La versione corrente usa un Boolean Running.

## Nel codice C#

Per il file di ricetta usa un oggetto FileType e limiti server di dimensione e numero handle. Separare caricamento, validazione del contenuto e attivazione della ricetta. Il parser deve rifiutare una ricetta incompleta prima di attivarla.

## Esercizio e verifica

Progetta download a chunk e cleanup degli handle quando la sessione termina. Scrivi una tabella di transizioni con precondizioni e output; genera un evento per ogni transizione.

## Errore da evitare

Un file trasferito correttamente non è necessariamente una ricetta valida. Non confondere trasferimento e commit della configurazione di processo.

## Obiettivi e scenario integrato

Tempo indicativo: 2–3 ore. State machine e FileType sono estensioni da implementare; il server corrente usa Running e non pubblica un oggetto file. Lo scenario è: trasferire una ricetta, validarla, renderla attiva e avviare un ciclo a stati.

## Definire gli stati prima dei pulsanti

| Stato | Start | Stop | Evento o condizione di uscita |
|---|---|---|---|
| Idle | Accetta ricetta valida | Nessun effetto o esito documentato | Avvio → Heating |
| Heating | Rifiuta | Ritorna Idle | Target raggiunto → Holding |
| Holding | Rifiuta | Ritorna Idle | Tempo mantenimento → Completed |
| Completed | Nuovo ciclo secondo policy | Ritorna Idle | Reset o nuovo avvio |
| Fault | Rifiuta | Policy di arresto sicuro | Reset autorizzato dopo rimozione causa |

La tabella è un contratto applicativo proposto. Deve essere mappata a uno StateMachineType/FiniteStateMachineType appropriato con stati, transizioni e identificatori. CurrentState leggibile e il suo Id non sono sostituibili con una stringa libera che cambia in base alla lingua.

Per il processo del laboratorio, evitare transizioni dovute a una sola lettura rumorosa: definisci tolleranza, qualità richiesta ed eventuale tempo di stabilità.

## Separare modello di processo e NodeManager

Un motore di stato di dominio può esporre stato corrente e una funzione di transizione; il NodeManager traduce gli effetti in aggiornamenti di nodi ed eventi. Non spargere if su Running in ogni callback.

**Soluzione dell'esercizio di progetto.** Centralizza le transizioni, valida comando e stato, aggiorna lo stato come operazione coerente e notifica il cambiamento. Il test del dominio verifica la tabella; il test UA verifica che CurrentState, output di Call ed evento rappresentino la stessa transizione.

Per un errore non limitarti a lanciare un'eccezione dal timer: il processo deve entrare in uno stato osservabile e, se appropriato, attivare una condizione.

## FileType: protocollo di trasferimento

FileType espone metodi standard basati su un file handle. Open richiede una modalità, Read usa handle e lunghezza, Close rilascia la risorsa. Il nodo espone informazioni come Size e capacità di scrittura.

Flusso client da adattare a un futuro fileNode e ai MethodId risolti tramite browse; questi nodi non sono presenti oggi:

```csharp
uint handle = (uint)session.Call(fileNode, openMethod, (byte)1)[0];
try
{
    byte[] chunk = (byte[])session.Call(
        fileNode, readMethod, handle, 4096)[0];
    // Accumula chunk e continua fino alla fine, rispettando limiti e stato.
}
finally
{
    session.Call(fileNode, closeMethod, handle);
}
```

Il literal byte della modalità e UInt32 dell'handle devono rispettare la firma. Il blocco finally è essenziale, ma un Close remoto può fallire: il server deve avere una politica di cleanup degli handle quando scade la sessione.

## Trasferimento non significa attivazione

Per caricare una ricetta usa staging: trasferisci il contenuto, chiudi il file, valida schema, valori e versione, poi richiama un comando di attivazione. Non modificare il target appena arrivano i primi byte.

Definisci dimensione massima, checksum se richiesto, permessi e limiti degli handle. Un nome file fornito dal client non deve diventare un percorso arbitrario del filesystem host.

## Esercizio risolto: upload interrotto

Il client cade dopo metà file. La ricetta corrente deve rimanere attiva e intatta; il contenuto parziale resta nello staging o viene eliminato secondo policy.

**Verifica.** Il checksum della ricetta attiva non cambia; nessuna transizione parte dal file incompleto; l'handle non resta aperto indefinitamente; un retry può creare un nuovo staging senza corrompere quello già validato.

Riferimenti: [Information Model, Part 5](https://reference.opcfoundation.org/Core/Part5/), [File Transfer, Part 20](https://reference.opcfoundation.org/Core/Part20/).

[Indice](README.md) · [Uso dei sample](../USAGE.md) · [Architettura](../ARCHITECTURE.md)
