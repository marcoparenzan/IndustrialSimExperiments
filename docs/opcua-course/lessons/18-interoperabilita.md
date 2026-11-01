# NodeSet, companion specification, diagnostica e progetto finale

## Concetti

Un modello condiviso descrive significato e contratti oltre al trasporto. NodeSet2 consente di distribuire nodi e dipendenze; le companion specification modellano domini specifici. Prima di inventare PumpType controlla i modelli ufficiali adatti al dominio. Conformità e interoperabilità richiedono client indipendenti e test di comportamento.

## Esperimento

Progetto finale: due linee, pompe comandabili, ricetta strutturata, allarme retained, storico e tre sinottici. L'implementazione iniziale del repository è la base eseguibile, le estensioni sono tracciate nella roadmap.

## Nel codice C#

Versiona namespace, ModelVersion, PublicationDate, NodeId e dipendenze RequiredModel. Misura numero sessioni, item, queue overflow, latenza, reconnect e errori dei servizi. GDS e rinnovo certificati richiedono un laboratorio infrastrutturale dedicato.

## Esercizio e verifica

Verifica con client generico: scoperta tipo, browse, unità, Call, decoding custom, condizioni e storico. Ripeti con namespace index diverso, restart e due utenti. Registra expected e actual senza dipendere dalla UI.

## Errore da evitare

Un client C# che condivide tutte le classi del server non prova interoperabilità. Una compilazione riuscita non prova conformità alle specifiche né certificazione.

## Obiettivi e progetto finale

Tempo indicativo: almeno una giornata per il disegno, poi implementazione per incrementi. Riunisci le lezioni in una cella con due reattori e pompe indipendenti, ricette custom, condizioni, storico e sinottici modulari. Non dichiarare completo il progetto soltanto perché i due processi C# compilano insieme.

## Distribuire il modello

NodeSet2 descrive nodi, riferimenti, attributi e dipendenze del modello. Conserva il sorgente del modello e una generazione riproducibile. Namespace URI, ModelVersion, PublicationDate e RequiredModel aiutano a identificare contratto e dipendenze.

La pubblicazione del modello non equivale al comportamento server: una Method dichiarata nel NodeSet richiede ancora una implementazione; una Variable storicizzata richiede un provider; una condizione richiede un ciclo di stato coerente.

Evita di cambiare identificatori a ogni build. Il modello deve sopravvivere alla ricreazione del processo. Gli id casuali sono utili per risorse effimere, non per nodi di contratto che i client devono ritrovare.

## Usare un modello di dominio condiviso

Prima di inventare il tuo PumpType, valuta companion specification adatte al caso d'uso, ad esempio i modelli di integrazione dispositivi e macchinari pertinenti. La scelta dipende dal dominio: un nome simile non prova che il modello sia corretto per quell'impianto.

Importa le dipendenze richieste e conserva i namespace ufficiali senza modificarli arbitrariamente. Le tue estensioni appartengono al namespace applicativo. Non etichettare un oggetto come conforme se manca parte del contratto obbligatorio.

## Una matrice di interoperabilità

| Prova | Client statico C# | Client senza DLL del server |
|---|---|---|
| Browse e percorsi | Richiesta | Richiesta |
| Tipi e unità | Richiesta | Richiesta |
| Firma e Call | Richiesta | Richiesta |
| Decoding RecipeDataType | Può usare factory | Deve scoprire/caricare il contratto |
| Acknowledge e refresh | Richiesta | Richiesta se supportato |
| HistoryRead paginato | Richiesta | Richiesta |
| Namespace index cambiato | Richiesta | Richiesta |

Il client statico è utile per sviluppo e performance; il client indipendente scopre lacune che una DLL condivisa può mascherare. La presenza di un modello non garantisce tutte le capability del server: verifica i servizi richiesti.

## Diagnostica che aiuta a decidere

Registra contatori di sessioni, subscription e item, reconnect, risultati Bad per servizio e callback scartati. Misura latenza senza mescolare tempo della sorgente, ricezione server, ricezione client e rendering.

Un diagnostico di freshness nel dominio potrebbe essere:

```csharp
static bool IsFresh(DateTime sourceUtc, DateTime nowUtc,
    TimeSpan allowedAge)
{
    var age = nowUtc - sourceUtc;
    return age >= TimeSpan.Zero && age <= allowedAge;
}
```

Questo controllo presuppone orologi coerenti e timestamp significativi. Un timestamp futuro non deve essere trattato come un dato fresco indefinitamente. Per nodi che cambiano raramente, la policy di freschezza non coincide necessariamente con il tempo dall'ultima data-change notification.

## Certificati e GDS come laboratorio dedicato

Per gestire più dispositivi definisci enrollment, trust, rinnovo, revoca e recupero. GDS e CertificateManager richiedono ruoli, servizi e configurazione propri; il trust per DER locale del corso non è una loro implementazione.

L'esercizio di gestione deve includere rinnovo prima della scadenza, peer ancora con il certificato vecchio, sincronizzazione trust e un audit amministrativo. Verifica il failure mode prima di automatizzare la rotazione.

## Esercizio risolto: criteri di consegna

Organizza quattro incrementi:

1. Modello e due istanze: stessa struttura, stato indipendente.
2. Ricetta custom: decoding indipendente e compatibilità versionata.
3. Servizi avanzati: condizioni retained e storico persistente.
4. Robustezza: recovery, identità e UI su più circuiti.

**Soluzione di verifica.** Per ogni incremento produci una tabella expected/actual e almeno una prova di errore. Per il modello cambia namespace index; per le ricette usa payload invalido; per gli allarmi riconnetti dopo rientro; per lo storico pagina e riavvia; per autorizzazioni invia richieste direttamente senza UI.

I tredici laboratori coprono il nucleo client/server e il tipo custom della ricetta; un client dinamico indipendente verifica i metadati. Le altre estensioni del progetto finale richiedono implementazioni server dedicate. Conformità formale e certificazione sono obiettivi ulteriori rispetto ai nostri test.

Riferimenti: [Modelli OPC Foundation](https://github.com/OPCFoundation/UA-Nodeset), [NodeSet e modelli, Part 6](https://reference.opcfoundation.org/Core/Part6/), [Discovery, Part 12](https://reference.opcfoundation.org/Core/Part12/).

[Indice](README.md) · [Uso dei sample](../USAGE.md) · [Architettura](../ARCHITECTURE.md)
