# Address space, riferimenti e namespace

## Concetti

L'address space è un grafo. Organizes esprime organizzazione, HasComponent composizione, HasProperty metadata, HasTypeDefinition il tipo dell'istanza e HasSubtype l'ereditarietà. BrowseName è una QualifiedName usata nei percorsi; DisplayName è localizzabile e serve alla presentazione. NodeId identifica un nodo nel contesto del server.

## Esperimento

Esegui client browse. Il server limita il primo risultato a due riferimenti: BrowseNext percorre i continuation point fino al loro esaurimento.

## Nel codice C#

Lessons.Id consulta NamespaceUris usando urn:uacourse:plant. Non salvare un indice ns=2 come identificatore portabile: può cambiare dopo modifiche o reconnect. Memorizza URI del namespace e identificatore.

## Esercizio e verifica

Aggiungi Reactor2 con lo stesso tipo e gli stessi nomi relativi. Implementa un browse ricorsivo con visited set, limite di profondità e rilascio dei continuation point se l'utente interrompe.

## Errore da evitare

Un grafo può avere più genitori e cicli. Non assumere che Browse restituisca un albero o tutti i riferimenti in una risposta.

## Obiettivi e punto di partenza

Tempo indicativo: 90 minuti. Imparerai a leggere un grafo di nodi, gestire la paginazione e risolvere percorsi relativi. Laboratori: [RunBrowse.cs](../../src/UaCourse/Labs/RunBrowse.cs) e [RunPaths.cs](../../src/UaCourse/Labs/RunPaths.cs).

```powershell
dotnet run --project src/UaCourse -- client browse
dotnet run --project src/UaCourse -- client paths
```

## Identità, nome e presentazione

Un NodeId combina namespace index e identificatore, che può essere numerico, stringa, Guid o ByteString. Il NamespaceArray della sessione collega l'indice al suo URI. Un ExpandedNodeId può portare informazioni ulteriori: non convertirlo a NodeId senza considerare URI e server index.

BrowseName è una QualifiedName. La stessa stringa Temperature in due namespace può identificare nomi qualificati differenti. DisplayName è invece un LocalizedText destinato all'utente: tradurlo non deve rompere un percorso applicativo.

Nel corso usiamo identificatori stringa leggibili, come Reactor1.Temperature. Il punto non impone una gerarchia al protocollo: è una convenzione del nostro identificatore. La gerarchia reale dipende dai riferimenti.

## Browse passo per passo

Il laboratorio chiede riferimenti gerarchici forward e un massimo di due risultati per risposta:

```csharp
session.Browse(null, null, Lessons.Id(session, "Reactor1"), 2,
    BrowseDirection.Forward, ReferenceTypeIds.HierarchicalReferences,
    true, 0, out var continuationPoint, out var references);
```

HierarchicalReferences è una famiglia di tipi; includeSubtypes=true include i riferimenti derivati. NodeClassMask=0 non limita i risultati a una sola classe. Non significa che ogni nodo sia una Variable: troverai anche i metodi.

La risposta contiene riferimenti, non valori correnti. Per ogni ReferenceDescription puoi osservare ReferenceTypeId, IsForward, BrowseName, NodeClass e TypeDefinition. Per leggere Temperature occorre una successiva Read del nodo trovato.

Il continuation point è un token opaco del server. Quando è presente chiama BrowseNext, accumula i riferimenti e continua fino a quando non è più restituito. Se interrompi l'esplorazione prima della fine, rilascia il token con releaseContinuationPoints=true. Nel nostro percorso completo i token vengono consumati fino all'esaurimento.

## Percorsi relativi

RunPaths costruisce due RelativePathElement: Reactor1 e Temperature, entrambi qualificati nel namespace applicativo. StartingNode è Plant. Il risultato atteso è una sola destinazione e status Good:

```text
Plant → Reactor1 → Temperature
Translate: Good, targets=1
ns=<indice della sessione>;s=Reactor1.Temperature
```

Il servizio può restituire più destinazioni o un percorso non completamente risolto. In un client generale controlla anche RemainingPathIndex e rifiuta target remoti se la tua applicazione non li gestisce. La singola asserzione del laboratorio è sufficiente per questo modello conosciuto, non per qualunque address space.

## Esercizio risolto: esplorazione senza cicli

Progetta un browse ricorsivo. Non usare la profondità come unica protezione: due oggetti possono riferirsi reciprocamente.

```csharp
var visited = new HashSet<string>();
bool FirstVisit(ExpandedNodeId id)
    => visited.Add(id.ToString());
```

Questo frammento illustra il controllo; in un'applicazione multi-server conserva un'identità normalizzata con namespace URI e server. Aggiungi limite di profondità, massimo numero di nodi e CancellationToken. Ogni chiamata Browse deve gestire i propri continuation point.

**Soluzione.** Visita ciascun nodo una sola volta per il traversal richiesto, ma conserva separatamente gli archi: saltare una visita ripetuta non autorizza a cancellare un riferimento significativo dal grafo mostrato.

**Diagnostica.** BadNoMatch durante Translate suggerisce BrowseName, namespace, direzione o filtro di riferimento errati. Non passare automaticamente a un Read con un NodeId costruito per supposizione.

Riferimenti: [Address Space Model](https://reference.opcfoundation.org/Core/Part3/), [View Service Set](https://reference.opcfoundation.org/Core/Part4/v105/docs/5.9).

[Indice](README.md) · [Uso dei sample](../USAGE.md) · [Architettura](../ARCHITECTURE.md)
