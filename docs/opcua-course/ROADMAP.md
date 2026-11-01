# Roadmap

## Disponibile nella prima versione

- Server e client console, sinottici modulari idrico/reattore, subscription, Write e Call.
- Address space, ObjectType, attributi, namespace URI, array e struttura standard UA.
- Eventi semplici, canale cifrato, errori di servizio, riconnessione con ricreazione della sessione.
- Percorso didattico con laboratori automatici e progetti di approfondimento.
- Home neutra e dashboard con URL dedicati; template UA Lab responsive con navigazione generata dal catalogo.

## Evoluzione del modello

1. Estrarre protocollo e contratti in libreria; separare simulazione fisica e mapping NodeManager.
2. Completare ReactorType e creare PumpType con oggetti e metodi individuali. Criterio: due istanze con gli stessi BrowseName relativi e comportamento indipendente.
3. RecipeDataType con StructureDefinition, encoding, array di step, metodo e decoding indipendente è disponibile. Estenderlo con enum/campi opzionali e generazione ModelCompiler riproducibile; il NodeSet2 corrente è un export parziale runtime.
4. Estendere i comandi dichiarativi già disponibili ai metodi delle singole pompe, con argomenti e feedback per hotspot. Criterio: nessuna logica UA nel componente grafico e binding validato all'avvio.

## Servizi avanzati

5. AlarmConditionType con Active/Acked/Confirmed, ConditionRefresh e retained branches. Criterio: recupero degli allarmi attivi dopo reconnect.
6. Storico con HistoryRead raw, continuation point e archivio persistente. Criterio: paginazione e campioni ordinati con qualità e timestamp.
7. FileType per trasferire ricette, StateMachineType per il ciclo e modellazione con companion specification appropriata.
8. PubSub MQTT/JSON e UDP/UADP, usando un laboratorio separato e un broker esplicito. Criterio: correlazione dei campi e metadata del DataSet.

## Robustezza e gestione

9. Identità utente, ruoli e audit; HTTPS/autenticazione web; policy per ogni comando.
10. Reconnect con backoff e jitter, eventuale reactivation/transfer delle subscription, loss counters e diagnostica.
11. Configurazione endpoint/PKI esterna, trust amministrativo, rinnovo certificati e laboratorio GDS.
12. Test browser automatizzati, server restart, overflow delle code, carico e interoperabilità con client di terzi.

Il corso può essere esteso per moduli: gli approfondimenti oggi descrivono contratti, esperimenti e criteri di completamento, ma non promettono un supporto già implementato da questo server.
