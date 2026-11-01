# Variant, array, ExtensionObject e strutture standard

## Concetti

Variant trasporta tipi built-in UA, scalari e array. ValueRank descrive se un valore è scalare o array e quante dimensioni possiede. ExtensionObject incapsula una struttura con il suo identificatore di encoding e una rappresentazione codificata o già decodificata. Non ogni ExtensionObject si può castare subito a una classe C# nota.

## Esperimento

Esegui client types e il pulsante Leggi struttura e array. Recipe contiene il Range standard Low=20, High=90; Profile è Double[3]. EngineeringUnits usa EUInformation, altra struttura standard.

## Nel codice C#

Il laboratorio RunTypes usa ExtensionObject.ToEncodeable e verifica Range. Il valore Recipe è una dimostrazione di struttura standard, non un RecipeDataType personalizzato. L'array ha DataType Double e ValueRank OneDimension, non un DataType chiamato DoubleArray.

## Esercizio e verifica

Leggi un sottoinsieme di Profile con IndexRange 1:2. Aggiungi una matrice e controlla ValueRank/ArrayDimensions. Gestisci un ExtensionObject con Body binario non riconosciuto senza perdita del payload.

## Errore da evitare

Una stringa JSON non è un tipo complesso UA interoperabile. Separare gerarchia di variabili e singolo valore strutturato: hanno semantiche e granularità delle notifiche differenti.

## Obiettivi e laboratorio

Tempo indicativo: 90 minuti. Leggi [RunTypes.cs](../../src/UaCourse/Labs/RunTypes.cs) e usa client types. Questa lezione esegue realmente array e strutture **standard**. RecipeDataType personalizzato è il passo successivo della lezione 09.

## Scalarità e tipo del valore

Un DataType Double non dice se il valore è uno scalare o un array. ValueRank descrive questa dimensione del contratto. Temperature usa Scalar; Profile usa OneDimension. ArrayDimensions può aggiungere informazioni sulle dimensioni, ma la dimensione effettiva va comunque considerata a runtime.

Variant conserva l'informazione necessaria alla codifica UA. Evita di convertire tutto in stringhe nello strato di comunicazione: perderesti differenze tra Int32, Double, Boolean, array, DateTime e strutture.

## Decodificare una struttura nota

Il server assegna a Recipe un ExtensionObject con corpo Range:

```csharp
var encoded = new ExtensionObject(
    new Opc.Ua.Range { Low = 20, High = 90 });
```

Il client del laboratorio fa questo:

```csharp
DataValue value = session.ReadValue(
    Lessons.Id(session, "Reactor1.Recipe"));
var extension = value.Value as ExtensionObject;
var decoded = ExtensionObject.ToEncodeable(extension);
if (decoded is not Opc.Ua.Range range)
    throw new InvalidOperationException("Range non riconosciuto");
Console.WriteLine($"{range.Low}..{range.High}");
```

Non è un cast arbitrario di byte a una classe. Range è un tipo standard noto allo SDK. Per un tipo sconosciuto il Body può rimanere codificato: occorre caricare/registrare il contratto o trattare il payload in modo esplicito.

Sul wire, il campo identificativo di un ExtensionObject strutturato identifica l'encoding della struttura. Non confonderlo con il NodeId del DataType: questa distinzione diventa essenziale per tipi custom.

## Lettura parziale di un array

Frammento da provare in un laboratorio con sessione aperta:

```csharp
var request = new ReadValueId
{
    NodeId = Lessons.Id(session, "Reactor1.Profile"),
    AttributeId = Attributes.Value,
    IndexRange = "1:2"
};
session.Read(null, 0, TimestampsToReturn.Both,
    new ReadValueIdCollection { request }, out var values, out _);
if (StatusCode.IsBad(values[0].StatusCode))
    throw new ServiceResultException(values[0].StatusCode);
Console.WriteLine(string.Join(", ", (double[])values[0].Value));
```

Gli indici sono zero-based e la fine del range è inclusiva. Per [20,40,60] il risultato atteso è [40,60]. Un range fuori dalle dimensioni può restituire un errore specifico; non leggere un array vuoto come conferma automatica di successo.

## Gerarchia o struttura?

Un oggetto Recipe con tre variabili figlie permette accesso e notifiche per singolo campo. Una Variable con valore strutturato rappresenta invece un valore composto; la notifica riguarda quel valore. La scelta dipende dal contratto: metadata statici, granularità di aggiornamento, snapshot e servizi richiesti.

Nessuna delle due scelte rende automaticamente atomiche operazioni diverse. Se serve aggiornare una ricetta e attivarla come unità, usa una semantica di commit esplicita.

## Esercizio risolto

Il client riceve un ExtensionObject sconosciuto. Può sostituirlo con una stringa JSON costruita dai byte?

**Soluzione.** No: i byte hanno una codifica determinata dal contratto. Conserva identificatore di encoding e corpo, segnala tipo non disponibile e carica il modello/factory corretti. Soltanto dopo la decodifica puoi proiettare il dato in JSON per una vista o un'API.

**Autoverifica.** Perché Profile ha DataType Double? Perché il tipo elementare è Double e il rango descrive l'array. Perché Recipe non è una vera ricetta applicativa? Perché pubblica solo Range, senza nome, step o versione.

Riferimenti: [ExtensionObject, Part 6](https://reference.opcfoundation.org/Core/Part6/v105/docs/5.2.2.15), [DataTypes, Part 3](https://reference.opcfoundation.org/Core/Part3/).

[Indice](README.md) · [Uso dei sample](../USAGE.md) · [Architettura](../ARCHITECTURE.md)
