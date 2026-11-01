# OPC UA in C#: dal segnale al modello informativo

Laboratorio progressivo in italiano, con server OPC UA simulato e applicazione Blazor Server (.NET 10, rendering Interactive Server). Il primo sinottico riprende l'impianto idrico della schermata fornita: lo schema è ridisegnato in SVG, senza menu, data, valori statici o cornice. Il secondo esplora un reattore, metodi e ricette.

Le label ricevono DataValue attraverso MonitoredItem e Publish. I pulsanti eseguono Write e Call; gli eventi arrivano con una subscription a BaseEventType. Il browser comunica con Blazor via SignalR: è il processo ASP.NET a parlare OPC UA.

## Avvio rapido

Prerequisito: SDK .NET 10. Dalla root del repository:

```powershell
dotnet restore OpcUaExperiments.slnx
dotnet build OpcUaExperiments.slnx --no-restore
dotnet run --project src/UaCourse --no-build -- server
```

In un secondo terminale, **sempre dalla stessa root**:

```powershell
dotnet run --project src/UaCourse.Web --no-launch-profile --urls http://localhost:5080
```

Aprire http://localhost:5080. Endpoint UA: `opc.tcp://localhost:4840/UaCourse`. Arrestare con Ctrl+C. Il server e la web app sono processi separati, con due sessioni indipendenti se si avvia anche il client console.

La home è un indice neutro delle dashboard, con anteprime statiche e link. Il nuovo template UA Lab usa una sidebar scura, pannelli chiari e navigazione compatta su mobile. Le dashboard hanno URL dedicati: [impianto idrico](http://localhost:5080/dashboards/water) e [reattore e ricette](http://localhost:5080/dashboards/reactor). Il catalogo genera automaticamente sia le schede della home sia i link della navigazione.

## Percorso

[Indice delle lezioni](docs/lessons/README.md), [uso e troubleshooting](docs/USAGE.md), [architettura](docs/ARCHITECTURE.md), [roadmap](docs/ROADMAP.md), [punti aperti](docs/OPEN-POINTS.md), [changelog](CHANGELOG.md).

Il percorso distingue laboratori eseguibili e approfondimenti progettuali. Sono implementati browse e continuation point, browse path, letture batch e attributi, scrittura validata, ObjectType, strutture standard e custom, array, metodi con input strutturato, data change, eventi, canale cifrato e riconnessione Blazor. RecipeDataType pubblica DataTypeDefinition e HasEncoding: un client indipendente lo decodifica dai metadati. Generazione ModelCompiler, Alarms & Conditions, storico, PubSub, ruoli e GDS restano estensioni nella roadmap.

## Verifica automatica

Con nessun server attivo sulla porta 4840:

```powershell
dotnet run --project src/UaCourse -- verify
```

Avvia un server reale TCP, apre una sessione SignAndEncrypt ed esegue tredici laboratori con asserzioni. Un errore produce exit code 1. Nessun PLC necessario. I laboratori sono separati in `src/UaCourse/Labs`; includono array parziali, verifica del modello, precondizioni dei metodi e ricette personalizzate.

Con server attivo, prova la [lezione 09 eseguibile](docs/lessons/09-custom-types.md):

```powershell
dotnet run --project src/UaCourse -- client custom-types
dotnet run --project src/UaCourse.DynamicClient
```

Il secondo progetto non referenzia la DLL del server né RecipeData: usa il componente ufficiale ComplexTypes per scoprire il tipo. Il server esporta il frammento NodeSet2 della ricetta in `.artifacts/models/Recipe.NodeSet2.xml`. Il pannello Blazor permette caricamento e lettura della ricetta custom.

## Estendere le schermate

Definire uno `ScreenDefinition` in `src/UaCourse.Web/Modules/ScreenCatalog.cs`, aggiungere uno sfondo in `wwwroot/screens`, dichiarare le label con coordinate percentuali e pubblicare i relativi nodi sul server. `Synoptic` rende tutte le schermate e `UaConnection` sottoscrive automaticamente i nodi distinti del catalogo.

## Riferimenti

SDK ufficiale [OPC Foundation UA-.NETStandard](https://github.com/OPCFoundation/UA-.NETStandard/tree/1.5.374.158), fissato a 1.5.374.158 per rendere riproducibili le API del laboratorio; non è una dichiarazione di versione più recente. [Part 3: Address Space](https://reference.opcfoundation.org/Core/Part3/), [Part 4: Services](https://reference.opcfoundation.org/Core/Part4/), [Part 5: Information Model](https://reference.opcfoundation.org/Core/Part5/), [sample ufficiali](https://github.com/OPCFoundation/UA-.NETStandard-Samples). Le API del ramo corrente possono differire da quelle del pacchetto fissato.
