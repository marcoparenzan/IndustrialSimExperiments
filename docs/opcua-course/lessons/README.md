# Percorso didattico

Le lezioni 01–12 hanno laboratori sul codice disponibile; la 13 combina sicurezza attuale e progetto delle autorizzazioni. Le 14–18 sono approfondimenti con estensioni da implementare. La 09 include DataType custom, metodo strutturato, subscription e client dinamico indipendente. Ogni lezione collega concetti, codice, esperimenti e verifiche.

1. [Dal tag al modello informativo](01-fondamenti.md)
2. [Discovery, endpoint, SecureChannel e Session](02-sessioni.md)
3. [Address space, riferimenti e namespace](03-address-space.md)
4. [Read, Write, qualità, timestamp e batch](04-read-write.md)
5. [ObjectType, VariableType e regole di modellazione](05-modellazione.md)
6. [Method, argomenti e semantica dei comandi](06-metodi.md)
7. [Subscription, MonitoredItem, Publish e deadband](07-subscription.md)
8. [Variant, array, ExtensionObject e strutture standard](08-strutture.md)
9. [Tipi personalizzati e NodeSet2](09-custom-types.md)
10. [EventType, EventNotifier e filtri](10-eventi.md)
11. [Sinottici modulari e lifecycle Blazor](11-blazor.md)
12. [Riconnessione e continuità dei dati](12-recovery.md)
13. [Certificati, identità, autorizzazione e audit](13-sicurezza.md)
14. [Alarms & Conditions: stato, acknowledge e refresh](14-alarms.md)
15. [Historical Access e trend](15-history.md)
16. [State machine, FileType e contratti di processo](16-state-files.md)
17. [PubSub, DataSet e trasporti](17-pubsub.md)
18. [NodeSet, companion specification, diagnostica e progetto finale](18-interoperabilita.md)

Il comando `verify` copre tredici laboratori client/server, non le estensioni non implementate. Per i comandi completi vedere [USAGE](../USAGE.md).

## Come seguire il corso

Ogni capitolo contiene una sintesi iniziale, obiettivi e prerequisiti, spiegazione del contratto UA, percorso C# guidato, risultati attesi, errori e una soluzione ragionata degli esercizi. I frammenti dichiarati di progetto non vanno confusi con funzionalità già disponibili nel simulatore.

Percorso consigliato: lezioni 01–08 per il nucleo del protocollo; 10–13 per eventi, UI, recovery e sicurezza; poi 09 e 14–18 per progettare estensioni interoperabili. I tempi dei capitoli sono indicativi e non includono lo sviluppo completo delle estensioni.

## Mappa dei laboratori e delle soluzioni

| Capitolo | Comando o prova | Codice da studiare | Criterio |
|---|---|---|---|
| 01–02 | `client read` | [Program](../../src/UaCourse/Program.cs), [Configuration](../../src/UaCourse/Configuration.cs) | Sessione sicura e misura con qualità |
| 03 | `client browse`, `client paths` | [Browse](../../src/UaCourse/Labs/RunBrowse.cs), [Paths](../../src/UaCourse/Labs/RunPaths.cs) | Pagine consumate e percorso risolto |
| 04 | `client read`, `client write`, `client errors` | [Read](../../src/UaCourse/Labs/RunRead.cs), [Write](../../src/UaCourse/Labs/RunWrite.cs), [Errors](../../src/UaCourse/Labs/RunErrors.cs) | Risultati distinti e readback |
| 05 | `client model` | [Model](../../src/UaCourse/Labs/RunModel.cs) | Tipo, Mandatory e DataType coerenti |
| 06 | `client methods`, `client method-errors` | [Methods](../../src/UaCourse/Labs/RunMethods.cs), [MethodErrors](../../src/UaCourse/Labs/RunMethodErrors.cs) | Output, precondizioni, stato dopo rifiuto |
| 07 | `client subscribe` | [Notifications](../../src/UaCourse/Labs/ObserveNotifications.cs) | Notifica e stato dell'item |
| 08 | `client types`, `client ranges` | [Types](../../src/UaCourse/Labs/RunTypes.cs), [Ranges](../../src/UaCourse/Labs/RunRanges.cs) | Struttura, array e IndexRange |
| 09 | `client custom-types` e `UaCourse.DynamicClient` | [RecipeData](../../src/UaCourse/Models/RecipeData.cs), [RecipeNodeModel](../../src/UaCourse/Models/RecipeNodeModel.cs), [lab](../../src/UaCourse/Labs/RunCustomTypes.cs) | Read/Call/Publish e decoding senza DLL server |
| 10 | `client events` | [Notifications](../../src/UaCourse/Labs/ObserveNotifications.cs) | EventFieldList secondo SelectClauses |
| 11 | Browser e runner layout | [UaConnection](../../src/UaCourse.Web/Services/UaConnection.cs), [runner](../../scripts/check-layout.cjs) | Label, cambio schermata e comandi |
| 12 | Arresto/ripartenza server | [UaConnection](../../src/UaCourse.Web/Services/UaConnection.cs) | Cache invalidata e nuove subscription |
| 13 | Trust attuale + progetto ruoli | [Configuration](../../src/UaCourse/Configuration.cs) | Distinguere canale, utente e autorizzazione |
| 14–18 | Progetti avanzati | Contratti ed esercizi nei capitoli | Criteri expected/actual per ogni estensione |

Le implementazioni sono separate in `src/UaCourse/Labs`; `Lessons.cs` conserva dispatch e helper. Il tipo custom è disponibile con codice encodeable manuale ed export NodeSet2 parziale; la generazione automatica resta futura. A&C, storico, FileType e PubSub richiedono ancora moduli dedicati.
