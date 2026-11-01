# PubSub, DataSet e trasporti

## Concetti

Approfondimento progettuale. Le subscription client/server usano Session, MonitoredItem e Publish. OPC UA PubSub usa Publisher, DataSetWriter, WriterGroup e Subscriber con messaggi e metadata. UDP/UADP e MQTT/JSON sono possibili combinazioni di trasporto e codifica: non sono un cambio di flag della Session.

## Esperimento

Crea un laboratorio separato con broker MQTT esplicito e pubblica temperature e livelli. Il server attuale non pubblica DataSetMessage e Blazor usa client/server.

## Nel codice C#

Definisci PublishedDataSet e DataSetMetaData, versioni, identificatori publisher/writer e mapping dei campi. Gestisci security e credenziali broker separatamente dal canale UA TCP.

## Esercizio e verifica

Confronta fan-out con dieci consumer, ritardo end-to-end, perdita messaggi e aggiornamento metadata. Prova un subscriber senza modello C# condiviso.

## Errore da evitare

Un messaggio MQTT JSON con valori non diventa automaticamente OPC UA PubSub. Verifica il formato e metadata previsti dallo standard.

## Obiettivi e infrastruttura richiesta

Tempo indicativo: 2–3 ore. Serve un laboratorio separato dal client/server attuale. Per MQTT è necessario un broker esplicito; per UDP occorre una rete in cui il traffico configurato sia consentito. Il sample non genera ancora NetworkMessage PubSub.

L'obiettivo è capire configurazione del DataSet, identità del publisher, metadata e comportamento del consumer, non chiamare una generica funzione Publish con una stringa JSON.

## Confronto tra modelli di comunicazione

| Aspetto | Client/server del corso | PubSub |
|---|---|---|
| Risorsa di osservazione | MonitoredItem nella Session | DataSet pubblicato e reader |
| Scoperta del dato | Browse e attributi del nodo | Metadata/configurazione e relativo modello |
| Invio | Publish del servizio UA | NetworkMessage sul trasporto scelto |
| Comandi Read/Write/Call | Servizi disponibili | Non impliciti nel flusso DataSet |
| Consumer multipli | Sessioni/subscription | Fan-out secondo rete o broker |

PubSub può convivere con un server client/server sullo stesso dispositivo. Usare PubSub per telemetria non significa rinunciare a un address space per configurazione e comandi.

## Oggetti di configurazione

PublishedDataSet definisce quali campi pubblicare. DataSetWriter produce messaggi del dataset; WriterGroup coordina invio e impostazioni di trasporto; un reader identifica ciò che deve consumare. Identificatori come PublisherId e DataSetWriterId devono essere coerenti tra i partecipanti.

DataSetMetaData descrive i campi e i loro tipi. Versione dei metadata e ordine dei campi sono parte del contratto: il consumer non deve attribuire il nome Pressure al campo che prima rappresentava Temperature soltanto perché occupa lo stesso indice.

UDP/UADP e MQTT/JSON sono combinazioni frequenti; scegli quelle supportate dal tuo SDK e profilo. QoS MQTT è una proprietà del trasporto, non prova che il processo sorgente abbia prodotto o conservato ogni campione.

## Laboratorio guidato

1. Definisci un DataSet con Temperature, Setpoint e Running.
2. Fissa gli identificatori publisher e writer, un intervallo e il mapping dei campi.
3. Configura metadata e trasporto del publisher.
4. Configura un subscriber distinto senza riferimenti alle classi del simulatore.
5. Confronta il valore ricevuto con una Read client/server contemporanea.
6. Introduci una modifica compatibile/incompatibile del modello e osserva il controllo versione.
7. Arresta il consumer e riparti: documenta cosa viene recuperato dal broker e cosa resta perso.

Usa la libreria PubSub e i sample della release scelta dello [SDK ufficiale](https://github.com/OPCFoundation/UA-.NETStandard). Non incorporare nel progetto attuale API del ramo master senza verificare compatibilità e dipendenze.

## Esercizio risolto: metadata sconosciuti

Il subscriber riceve un messaggio con versione dei metadata che non conosce. Deve interpretarlo comunque con la tabella precedente?

**Soluzione.** No: conserva o scarta secondo una policy esplicita, segnala mismatch e recupera il contratto necessario attraverso il meccanismo previsto dal deployment. Il numero di campi uguale non prova che tipi, nomi e significato siano invariati.

Puoi rendere esplicito il controllo nel tuo adapter di dominio:

```csharp
static void RequireVersion(uint expectedMajor, uint receivedMajor)
{
    if (receivedMajor != expectedMajor)
        throw new InvalidOperationException("Metadata incompatibili");
}
```

Questo frammento illustra una policy applicativa; non implementa la codifica dei metadata OPC UA né la loro intera semantica di versione.

## Sicurezza e verifica

Credenziali broker, TLS e security dei messaggi PubSub vanno progettate con il profilo scelto. Un certificato trusted dal server UA TCP non configura automaticamente un broker MQTT.

Misura latenza con timestamp sorgente, numero messaggi, sequenze e buchi dove disponibili. Verifica key frame/delta frame se abilitati e comportamento di un subscriber che si unisce in ritardo.

**Criterio di completamento.** Due consumer indipendenti interpretano gli stessi campi, rifiutano un contratto incompatibile e dichiarano ciò che il sistema non recupera dopo un'interruzione.

Riferimento: [PubSub, Part 14](https://reference.opcfoundation.org/Core/Part14/).

[Indice](README.md) · [Uso dei sample](../USAGE.md) · [Architettura](../ARCHITECTURE.md)
