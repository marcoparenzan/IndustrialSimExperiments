# Dal tag al modello informativo

## Concetti

Il dato OPC UA non è solo un numero. Un DataValue associa Value, StatusCode, SourceTimestamp e ServerTimestamp. Una Variable espone attributi di tipo e accesso; un Object raggruppa componenti; un Method descrive una chiamata. Il client può scoprire queste informazioni senza conoscere una mappa di registri. La nostra cella usa Reactor1 come oggetto, Temperature come variabile e Start come metodo.

## Esperimento

Apri i due sinottici, avvia Reactor1, osserva temperatura e totalizzatore. La simulazione gira nel server anche senza browser; aprire una seconda finestra non crea un secondo impianto.

## Nel codice C#

Segui Program.cs dal comando server a PlantNodeManager.CreateAddressSpace; confronta con UaConnection.ExecuteAsync. Il browser usa SignalR, non UA TCP.

## Esercizio e verifica

Disegna un motore con velocità, corrente, stato e Start. Quali sono valori, quali proprietà e quali operazioni?

## Errore da evitare

La lettura può riuscire con qualità Uncertain o Bad; non trattare una risposta di trasporto riuscita come conferma di qualità Good.

## Obiettivi e preparazione del laboratorio

Tempo indicativo: 60–90 minuti. Servono le basi di C#, ma non esperienza con OPC UA. Alla fine devi saper distinguere identità di un nodo, tipo del suo valore, qualità del dato e operazione applicativa.

Apri [PlantServer.cs](../../src/UaCourse/PlantServer.cs), [Program.cs](../../src/UaCourse/Program.cs) e [Synoptic.razor](../../src/UaCourse.Web/Components/Synoptic.razor). Avvia prima il server dalla root e poi il client:

```powershell
dotnet run --project src/UaCourse -- server
# Secondo terminale
dotnet run --project src/UaCourse -- client read
```

Il client non crea un sensore: chiede al server gli attributi di un nodo già esistente. Il browser non crea il processo industriale: rappresenta lo stato che il server pubblica. Questa separazione permette di sostituire il simulatore con una sorgente reale mantenendo, se il contratto resta compatibile, lo stesso client.

## Dalla classe C# al contratto UA

Immagina questa classe di dominio:

```csharp
public sealed class Reactor
{
    public double Temperature { get; private set; }
    public double Setpoint { get; set; }
    public bool Running { get; private set; }
    public void Start() => Running = true;
}
```

La classe non è automaticamente un modello OPC UA. Non descrive un'identità stabile dei membri, unità ingegneristiche, qualità, permessi remoti o riferimenti navigabili. Serve un mapping esplicito:

| Dominio C# | Contratto UA | Cosa scopre un client |
|---|---|---|
| Istanza Reactor | Object Reactor1 | Componenti e tipo dell'oggetto |
| Temperature | Variable di DataType Double | Valore, accesso e rango |
| Unità di temperatura | Property EngineeringUnits | Unità senza convenzioni nel nome |
| Start | Method con OutputArguments | Firma e risultato della chiamata |
| Modello di reattore | ObjectType ReactorType | Contratto condiviso dalle istanze |

ObjectType e DataType non significano la stessa cosa. Il primo descrive la struttura di un oggetto nell'address space; il secondo descrive la rappresentazione e il significato di un valore.

## Leggere un valore senza perdere la sua semantica

Frammento da inserire in un laboratorio con una Session già aperta:

```csharp
var id = Lessons.Id(session, "Reactor1.Temperature");
DataValue reading = session.ReadValue(id);
if (!StatusCode.IsGood(reading.StatusCode))
    throw new InvalidOperationException($"Qualità: {reading.StatusCode}");

Console.WriteLine($"Valore={reading.Value}");
Console.WriteLine($"Sorgente={reading.SourceTimestamp:O}");
Console.WriteLine($"Server={reading.ServerTimestamp:O}");
```

Nel sample ReadValue è un helper che può sollevare ServiceResultException per risultati Bad; nelle chiamate Read batch ricevi invece i singoli DataValue e puoi classificare ogni risultato. Non uniformare le due API assumendo che restituiscano sempre un oggetto anche in errore.

Il numero può restare identico mentre cambia la qualità: un sensore scollegato potrebbe conservare l'ultimo valore. Un'interfaccia che mostra solo Value perde questa distinzione. Il nostro renderer aggiunge qualità e timestamp nel tooltip e considera anche la disponibilità della connessione.

## Esperimento guidato e soluzione

1. Leggi Temperature prima di Start: dopo l'assestamento vale circa 20 °C.
2. Esegui client methods: il target viene impostato a 70 e Running diventa true.
3. Ripeti client read: il valore cresce gradualmente.
4. Chiudi il browser e continua a leggere dalla console: il processo continua.
5. Apri due browser: entrambi osservano lo stesso reattore.

**Soluzione ragionata.** Il timer di PlantNodeManager aggiorna il processo ogni 500 ms. La regola è `current + (target-current)*0.08`: il modello non raggiunge istantaneamente il target. Lo stato è posseduto dal server; browser e console sono osservatori e possono inviare comandi.

**Autoverifica.** Se cambi il colore di una label, cambia la temperatura? No. Se cambia il NamespaceIndex, cambia il sensore? No, se URI e identificatore restano gli stessi. Se Read torna Good, il dato è automaticamente recente? No: la qualità e la freschezza sono proprietà distinte da controllare.

Riferimento: [Address Space Model, Part 3](https://reference.opcfoundation.org/Core/Part3/).

[Indice](README.md) · [Uso dei sample](../USAGE.md) · [Architettura](../ARCHITECTURE.md)
