# Uso

## Modalità

1. Avviare `server` dalla root.
2. Avviare la web app dalla stessa root. Nel repository lo store PKI viene risolto rispetto alla solution; fuori dal repository è relativo alla working directory.
3. Aprire `http://localhost:5080`: la home mostra le dashboard disponibili. Selezionare Impianto idrico (`/dashboards/water`) e verificare lo stato connesso.
4. Avviare il reattore: il totalizzatore aumenta. Passare dalla navigazione al sinottico Reattore e ricette (`/dashboards/reactor`): la temperatura sale verso il target.
5. Scrivere un setpoint. Poi applicare una ricetta con Call: osservare output del metodo ed evento.
6. Leggere struttura e array: vengono decodificati un Range e un Double[].
7. Arrestare e riavviare il server: la UI indica indisponibilità e ricrea automaticamente le subscription.

I comandi Start/Stop sono disponibili anche come hotspot sullo sfondo, descritti nel catalogo. Il pannello aggiunge Write, ApplyRecipe e lettura delle strutture. Tutti appartengono a Reactor1. Le pompe del disegno sono simboli statici: non sono pulsanti di azionamento individuale.

La home è disponibile anche su `/dashboards` e non contiene valori live o comandi. Ogni dashboard può essere aperta direttamente o ricaricata. Un identificativo sconosciuto mostra “Dashboard non trovata” con ritorno alla home. Su mobile la sidebar diventa navigazione superiore; il sinottico mantiene lo scorrimento orizzontale interno per preservare la leggibilità.

## Laboratori console

Con server già attivo:

```powershell
dotnet run --project src/UaCourse -- client browse
dotnet run --project src/UaCourse -- client paths
dotnet run --project src/UaCourse -- client read
dotnet run --project src/UaCourse -- client write
dotnet run --project src/UaCourse -- client types
dotnet run --project src/UaCourse -- client custom-types
dotnet run --project src/UaCourse -- client ranges
dotnet run --project src/UaCourse -- client model
dotnet run --project src/UaCourse -- client method-errors
dotnet run --project src/UaCourse -- client methods
dotnet run --project src/UaCourse -- client errors
dotnet run --project src/UaCourse -- client subscribe
dotnet run --project src/UaCourse -- client events
```

`client all` li esegue in sequenza. `verify` avvia il proprio server: prima arrestare il server esterno. Subscribe ed events terminano quando arriva una notifica; timeout di 10 s se non arriva.

`ranges` verifica `Profile[1:2] = [40,60]` e un range oltre i dati. `model` verifica HasTypeDefinition, regola Mandatory e DataType della dichiarazione/istanza. `method-errors` verifica BadInvalidState sul secondo Start e l'integrità del setpoint dopo una Call rifiutata; arresta il reattore nel cleanup. I singoli laboratori sono file separati in `src/UaCourse/Labs` e corrispondono alle soluzioni delle lezioni 05, 06 e 08.

`custom-types` verifica metadati di RecipeDataType, Call con input strutturato, readback, Publish e rifiuto di una ricetta invalida. Ripristina la ricetta precedente. Con il server attivo, `dotnet run --project src/UaCourse.DynamicClient` decodifica i campi senza la DLL del server. La web app aggiunge nome/revisione e i pulsanti Carica RecipeDataType e Leggi ricetta custom. Il modello del tipo è esportato all'avvio in `.artifacts/models/Recipe.NodeSet2.xml`.

## Aggiungere un sinottico

```csharp
new ScreenDefinition("line2", "Linea 2", "Descrizione", "/screens/line2.svg",
    [new SignalLabel("Line2.Speed", "Velocità", "rpm", 50, 30)]);
```

Pubblicare `Line2.Speed` nel NodeManager, aggiungere la voce alla proprietà Screens e lo sfondo a `wwwroot/screens`. X e Y sono percentuali del canvas, non pixel del browser. Uno SVG con viewBox conserva il rapporto d'aspetto; evitare valori già stampati nello sfondo. Per usare altri raster, salvare uno sfondo pulito e mantenere nel catalogo il rapporto e le posizioni coerenti. Non serve modificare Synoptic o aprire nuove sessioni.

## Troubleshooting

| Sintomo | Verifica |
|---|---|
| Connessione continuamente offline | Server avviato? Porta 4841 libera? Log della web app? |
| BadCertificateUntrusted | Processi avviati dalla stessa root? Store `.artifacts/pki` di entrambi presenti? |
| BadCertificateHostNameInvalid | Usare localhost come nei comandi; il sample non gestisce host remoti |
| BadNodeIdUnknown | Nome nodo e namespace del catalogo corrispondono al server? |
| Nessun aggiornamento | Item.Status.Error, MonitoringMode, PublishingInterval e qualità del DataValue |
| BadInvalidState su Start | Il reattore è già in marcia; eseguire Stop |
| BadOutOfRange | Il target deve essere finito e tra 20 e 90 |
| Porta occupata durante verify | Fermare `server` prima di avviare la verifica |

`dotnet build OpcUaExperiments.slnx --no-restore` verifica entrambi i progetti. Le chiavi private e gli output di build sono ignorati da Git. Non eliminare il PKI mentre le applicazioni sono attive.
