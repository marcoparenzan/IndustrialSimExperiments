# ObjectType, VariableType e regole di modellazione

## Concetti

Un ObjectType è un contratto navigabile, diverso da una classe C#. ReactorType deriva da BaseObjectType, dichiara Temperature e la marca Mandatory. Una proprietà dell'istanza che non è dichiarata nel tipo può essere un'estensione; un'istanza conforme deve materializzare i figli obbligatori. ModellingRule non è un semplice attributo applicativo: è un riferimento a un oggetto standard.

## Esperimento

Usa client browse per l'istanza e un client generico per ReactorType. Confronta HasTypeDefinition, HasSubtype e HasModellingRule. EngineeringUnits è una Property distinta dal Value della temperatura.

## Nel codice C#

CreateAddressSpace crea il tipo e poi l'istanza. La costruzione manuale rende visibili i dettagli; il tipo iniziale dichiara solo Temperature. Il passo successivo è completare tutte le dichiarazioni del contratto.

## Esercizio e verifica

Dichiara Setpoint, Running e metodi nel tipo. Crea due istanze indipendenti. Definisci cosa è Mandatory e cosa Optional; documenta la compatibilità quando aggiungi figli.

## Errore da evitare

Un folder con nomi ordinati non sostituisce un modello tipizzato. La gerarchia visuale non esprime da sola unità, significato e comportamento.

## Obiettivi e prerequisiti

Tempo indicativo: 90–120 minuti. Dopo browse e Read, impari a progettare un contratto riutilizzabile. Il laboratorio consiste nel confrontare tipo e istanza in [PlantServer.cs](../../src/UaCourse/PlantServer.cs); non esiste ancora un comando dedicato che certifichi la conformità completa del modello.

## Due sistemi di tipi, due ruoli

Una classe CLR determina layout e comportamento nel tuo processo. Un ObjectType UA è un nodo che un client indipendente può navigare. Avere una classe Reactor con dieci proprietà non pubblica quelle proprietà nel contratto UA; viceversa un'istanza UA può essere costruita da NodeState senza una classe CLR equivalente.

Un ObjectType può dichiarare Variables, Objects e Methods. Un VariableType definisce caratteristiche di una variabile; il DataType del suo valore resta un concetto distinto. EngineeringUnits è una Property di tipo EUInformation, non il tipo della Temperature.

## Costruire e collegare il tipo

Estratto della costruzione del nostro tipo:

```csharp
var reactorType = new BaseObjectTypeState
{
    NodeId = new NodeId("ReactorType", NamespaceIndex),
    BrowseName = new QualifiedName("ReactorType", NamespaceIndex),
    DisplayName = "ReactorType",
    IsAbstract = false,
    SuperTypeId = ObjectTypeIds.BaseObjectType
};
reactorType.AddReference(
    ReferenceTypeIds.HasSubtype, true, ObjectTypeIds.BaseObjectType);
```

Questo frammento va nel contesto del NodeManager: NamespaceIndex e AddPredefinedNode sono servizi del manager, non variabili globali. Il server deve rendere navigabile anche il collegamento dal supertipo al sottotipo. Per riferimenti verso nodi gestiti da un altro manager, il sample usa externalReferences.

La sola assegnazione SuperTypeId non sostituisce ogni operazione di registrazione del modello. Analogamente, assegnare TypeDefinitionId all'istanza non crea automaticamente tutti i figli dichiarati.

## Mandatory e Optional

La dichiarazione Temperature usa ModellingRule_Mandatory. Significa che le istanze conformi devono avere il componente corrispondente. Non significa che il sensore non possa guastarsi, che il valore sia sempre Good o che il componente sia scrivibile.

La distinzione riguarda la struttura dell'istanza. Optional permette una capability che alcune istanze hanno e altre no. Per un client adattivo è meglio scoprire l'esistenza della capability che assumere che ogni dispositivo la esponga.

Nel tipo attuale soltanto Temperature è dichiarata. Setpoint, Running e i metodi esistono nell'istanza come estensioni: non fingere che il tipo iniziale sia già il contratto industriale completo.

## Laboratorio guidato

1. Naviga da BaseObjectType al nodo ReactorType.
2. Naviga i componenti di ReactorType: osserva Temperature.
3. Leggi DataType e ValueRank della dichiarazione.
4. Naviga HasModellingRule e verifica Mandatory.
5. Naviga Reactor1 e confronta la sua Temperature.
6. Verifica HasTypeDefinition di Reactor1.

La conformità non si limita all'uguaglianza del nome: controlla nome qualificato, NodeClass, tipo, rango e regola richiesta dal contratto.

## Esercizio risolto: due reattori

Progetta Reactor2. Temperature delle due istanze deve avere lo stesso BrowseName relativo, ma un NodeId distinto. Il metodo Start deve agire sull'oggetto passato nella Call e sullo stato dell'istanza corretta.

**Soluzione architetturale.** Estrai un builder di istanza che riceva identificatore e stato di dominio. Crea nodi con prefisso Reactor1 o Reactor2; conserva le variabili per istanza in un dizionario. Evita un solo campo running condiviso, che farebbe avviare entrambi i reattori insieme. Registra entrambe le istanze con lo stesso TypeDefinitionId.

**Criterio di completamento.** Avvia Reactor1 e verifica Reactor2.Running=false; poi ripeti invertendo gli oggetti. Un client deve risolvere Temperature con lo stesso percorso relativo su entrambe le istanze.

Riferimenti: [Part 3](https://reference.opcfoundation.org/Core/Part3/), [Information Model, Part 5](https://reference.opcfoundation.org/Core/Part5/).

[Indice](README.md) · [Uso dei sample](../USAGE.md) · [Architettura](../ARCHITECTURE.md)
