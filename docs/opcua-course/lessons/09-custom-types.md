# Tipi personalizzati: RecipeDataType, encoding e client dinamico

Questa lezione implementa un tipo UA personalizzato, una Variable strutturata, un Method che riceve la struttura e una subscription sul valore composto. Include due client: uno conosce la classe C#; l'altro costruisce il tipo dai metadati del server, senza dipendere dalla DLL del simulatore.

## Obiettivi, prerequisiti e codice

Tempo indicativo: 2–3 ore. Servono le lezioni 03, 06 e 08. Alla fine devi saper distinguere DataTypeId ed encoding id, mantenere coerenti definizione e codifica e verificare che una ricetta rifiutata non modifichi lo stato.

| Artefatto | Responsabilità |
|---|---|
| [RecipeData.cs](../../src/UaCourse/Models/RecipeData.cs) | Classe encodeable, campi, Clone e validazione |
| [RecipeNodeModel.cs](../../src/UaCourse/Models/RecipeNodeModel.cs) | DataType, StructureDefinition, encoding e LoadRecipe |
| [RunCustomTypes.cs](../../src/UaCourse/Labs/RunCustomTypes.cs) | Asserzioni client, Read, Call e Publish |
| [DynamicClient](../../src/UaCourse.DynamicClient/Program.cs) | Decodifica da metadati senza RecipeData |
| [UaConnection](../../src/UaCourse.Web/Services/UaConnection.cs) | Comandi e letture del pannello Blazor |

Il codice encodeable è scritto manualmente per rendere visibile il contratto. La generazione con ModelCompiler resta un'evoluzione, non un passaggio necessario per avviare questo laboratorio.

## Preparazione ed esecuzione

Dalla root, aggiorna le dipendenze e compila. Ferma i processi che usano gli stessi output di build prima di ricompilare su Windows.

```powershell
dotnet restore OpcUaExperiments.slnx
dotnet build OpcUaExperiments.slnx --no-restore
dotnet run --project src/UaCourse --no-build -- server
```

In un secondo terminale:

```powershell
dotnet run --project src/UaCourse --no-build -- client custom-types
dotnet run --project src/UaCourse.DynamicClient --no-build
```

Il primo comando verifica il comportamento e ripristina la ricetta precedente. Il secondo usa solo il client SDK e ComplexTypes: il suo progetto non referenzia UaCourse e non compila RecipeData.

Con nessun server attivo, `verify` avvia il proprio server ed esegue tredici laboratori, incluso questo.

## 1. Definire un contratto preciso

| Campo | DataType UA | ValueRank | Regola |
|---|---|---|---|
| Name | String | Scalar | Non vuoto, massimo 128 caratteri |
| Revision | UInt32 | Scalar | Maggiore di zero |
| Target | Double | Scalar | Finito, compreso tra 20 e 90 |
| Steps | Double | OneDimension | Da 1 a 64 elementi, tutti finiti e tra 20 e 90 |

Steps è un profilo di target trasportato e conservato. Il simulatore attuale **non esegue una sequenza temporizzata degli step**: LoadRecipe imposta Setpoint al campo Target. Non confondere il trasporto della ricetta con l'implementazione del suo ciclo di processo.

La Variable `Reactor1.BatchRecipe` usa il nuovo DataType. La vecchia `Reactor1.Recipe` rimane il Range standard della lezione 08, così i due esempi sono confrontabili.

## 2. Pubblicare il DataType e l'encoding

Gli identificatori applicativi sono distinti:

```text
urn:uacourse:plant / s=RecipeDataType
urn:uacourse:plant / s=RecipeDataType.Binary
```

Il primo identifica il tipo del valore; il secondo il suo encoding binario. Il client risolve l'indice del namespace in ogni sessione. L'indice numerico non è parte del contratto stabile.

RecipeNodeModel crea DataTypeState con SuperTypeId=Structure e DataTypeDefinition che contiene una StructureDefinition. DefaultEncodingId punta al nodo Default Binary; i campi sono dichiarati nell'ordine Name, Revision, Target, Steps.

HasEncoding è un riferimento esplicito dal DataType all'oggetto encoding. Non basta trattare l'encoding come un normale figlio gerarchico: il laboratorio controlla che HasEncoding sia navigabile e il client dinamico lo usa per ricostruire il contratto.

## 3. Codificare e decodificare nello stesso ordine

Estratto della classe realmente usata:

```csharp
encoder.WriteString(nameof(Name), Name);
encoder.WriteUInt32(nameof(Revision), Revision);
encoder.WriteDouble(nameof(Target), Target);
encoder.WriteDoubleArray(nameof(Steps), Steps);
```

Decode legge gli stessi tipi nello stesso ordine. Non riordinare questi campi per migliorare l'aspetto della classe: l'ordine è parte della rappresentazione binaria. Le funzioni PushNamespace/PopNamespace delimitano il contesto di codifica.

TypeId e BinaryEncodingId sono ExpandedNodeId con URI stabile. Questa versione espone solo l'encoding binario personalizzato; XmlEncodingId è nullo. Non dichiara encoding XML o JSON custom che non implementa nel modello.

La classe implementa IEncodeable e Clone copia anche l'array. Un clone superficiale condividerebbe Steps, consentendo a una modifica locale di alterare una ricetta già conservata in memoria. Decode normalizza un array nullo a un array vuoto, che la validazione poi rifiuta.

## 4. Ricevere la struttura in un Method

Il metodo `LoadRecipe(Recipe: RecipeDataType) → AcceptedRevision: UInt32` espone la firma nelle proprietà InputArguments e OutputArguments.

Il client statico costruisce una ricetta:

```csharp
var recipe = new UaCourse.Models.RecipeData
{
    Name = "Lab09",
    Revision = 42,
    Target = 66,
    Steps = [20, 35, 50, 66]
};
var output = session.Call(
    Lessons.Id(session, "Reactor1"),
    Lessons.Id(session, "Reactor1.LoadRecipe"),
    new Opc.Ua.ExtensionObject(recipe));
```

Il callback accetta il corpo encodeable, verifica tutte le regole prima di modificare lo stato, conserva un clone, aggiorna Setpoint, restituisce Revision ed emette un evento. Tutto avviene nel Lock del NodeManager.

BadTypeMismatch indica che il corpo non è il tipo atteso; BadOutOfRange indica una ricetta che viola le regole applicative. Non descrivere Name vuoto come un successo parziale: viene rifiutata l'intera operazione.

## 5. Verificare anche la subscription

Il laboratorio crea un MonitoredItem su BatchRecipe prima della Call. Ignora la notifica iniziale e attende una ricetta con nome e revisione della richiesta inviata. Poi confronta tutti i campi, incluso l'array.

Questo dimostra più di un cast locale: il valore passa attraverso il server TCP e Publish. Tuttavia il client statico conosce ancora la classe e potrebbe mascherare metadata mancanti. Per questo serve il secondo client.

## 6. Decodifica indipendente

DynamicClient usa `ComplexTypeSystem(session).Load()` per caricare le definizioni disponibili. Non registra RecipeData nella factory. Il tipo CLR stampato viene generato a runtime:

```text
Tipo CLR generato: Opc.Ua.ComplexTypes.plant.<indice>.RecipeDataType
Name = Standard
Revision = 1
Target = 60
Steps = 20, 40, 60
OK: ricetta decodificata senza assembly del server e senza RecipeData
```

Il nome del tipo generato e l'indice possono cambiare; i valori riflettono la ricetta corrente. Il client usa IComplexTypeProperties per leggere i campi, non un cast alla classe del server.

La configurazione del certificato è condivisa come file sorgente neutro: non contiene né registra la classe della ricetta. Il processo dinamico e la sua factory sono indipendenti dal processo del server.

## 7. NodeSet2 e sinottico Blazor

All'avvio il server esporta `.artifacts/models/Recipe.NodeSet2.xml` dalla stessa definizione usata a runtime. È un **frammento del modello** contenente il tipo e l'encoding; non è l'intero address space e non è ancora un modello generato/versionato con tutte le dipendenze RequiredModel.

Nel file il namespace può essere ns=1 mentre nel server è ns=2: le rispettive NamespaceUris spiegano la differenza. Un importatore deve rimappare gli indici.

Il pannello Blazor aggiunge nome e revisione, `Carica RecipeDataType · Call` e `Leggi ricetta custom`. Il target del pannello viene inserito nel campo Target e nel profilo [20,40,target]. Le risposte riportano la revisione accettata o la struttura letta.

## Esercizi con soluzione e verifica

**Ricetta invalida.** Inserisci uno step NaN. La Call deve fallire con BadOutOfRange e il readback deve essere uguale alla ricetta precedente. RunCustomTypes esegue questa prova.

**Clone.** Clona una ricetta e cambia Steps[0] nella copia. L'originale non deve cambiare. La soluzione è la copia dell'array in Clone, non il solo MemberwiseClone.

**Tipo sconosciuto.** Rimuovi la registrazione statica dal client di una copia del progetto: un corpo non decodificato non va trasformato in JSON per supposizione. La soluzione disponibile è il client dinamico che carica il contratto prima della Read.

**Evoluzione.** Aggiungi un campo al modello in un ramo di esercizio. Devi aggiornare definizione, Encode, Decode e test; valuta nuovi identificatori quando il layout è incompatibile. Non mantenere gli stessi id cambiando il significato dei campi e presumere compatibilità.

## Limiti e prossimo incremento

Il laboratorio verifica decodifica statica/dinamica, metadati, Call, Read e subscription. Non include ancora campi opzionali, union, enum custom, import del NodeSet nel server o generazione ModelCompiler. La fase successiva è rendere la generazione riproducibile e verificare la compatibilità con una versione precedente del modello.

Riferimenti: [DataTypeDefinition, Part 3](https://reference.opcfoundation.org/Core/Part3/), [ExtensionObject, Part 6](https://reference.opcfoundation.org/Core/Part6/v105/docs/5.2.2.15), [SDK ufficiale](https://github.com/OPCFoundation/UA-.NETStandard/tree/1.5.374.158).

[Indice](README.md) · [Uso dei sample](../USAGE.md) · [Architettura](../ARCHITECTURE.md)
