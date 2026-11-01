# Discovery, endpoint, SecureChannel e Session

## Concetti

Discovery descrive endpoint, security policy, modalità e token utente. SecureChannel protegge i messaggi; Session conserva il contesto applicativo. Una Subscription appartiene a una Session ma ha un ciclo di vita proprio. Se il TCP cade, la sessione non è necessariamente immediatamente persa: il timeout e le strategie di recovery contano.

## Esperimento

Esegui client read. Osserva SessionId e Basic256Sha256. Avvia un secondo client e confronta le sessioni; entrambe vedono lo stesso server.

## Nel codice C#

Configuration.CreateAsync crea il certificato applicativo. CoreClientUtils.SelectEndpoint seleziona un endpoint sicuro. Session.Create usa AnonymousIdentityToken: l'applicazione ha un certificato, l'utente resta anonimo.

## Esercizio e verifica

Registra endpoint selezionato, security mode e token policy. Prova una porta senza server e distingue errore discovery da errore di autenticazione.

## Errore da evitare

Non scegliere un endpoint None come fallback silenzioso quando la configurazione richiede sicurezza.

## Obiettivi e prerequisiti

Tempo indicativo: 75 minuti. Dopo la lezione 01, impari a seguire il ciclo discovery → endpoint → canale → sessione → subscription e a individuare il livello di un errore. Apri [Configuration.cs](../../src/UaCourse/Configuration.cs) e il ramo client di [Program.cs](../../src/UaCourse/Program.cs).

## Quattro livelli da non confondere

| Livello | Stato che possiede | Esempio di guasto |
|---|---|---|
| Connessione TCP | Trasporto dei byte | Server non raggiungibile |
| SecureChannel | Protezione e token del canale | Certificato rifiutato |
| Session | Contesto e identità utente | Sessione scaduta |
| Subscription | Item e sequenza delle notifiche | Lifetime superato |

Un canale può essere sostituito senza che ogni stato applicativo debba essere ricreato. Tuttavia il sample adotta una strategia più semplice: in caso di KeepAlive fallito ricrea tutto. Questo è una scelta del nostro client, non una proprietà obbligatoria del protocollo.

## Aprire la sessione in C#

Questo è il percorso usato dal programma; i parametri sono API della versione fissata nel progetto:

```csharp
var app = await Configuration.CreateAsync(false);
var description = CoreClientUtils.SelectEndpoint(
    app.ApplicationConfiguration, Configuration.Endpoint, true);
var endpoint = new ConfiguredEndpoint(
    null, description,
    EndpointConfiguration.Create(app.ApplicationConfiguration));

using var session = await Session.Create(
    app.ApplicationConfiguration, endpoint, false,
    "Client didattico", 60_000,
    new UserIdentity(new AnonymousIdentityToken()), null);
```

Il Boolean true di SelectEndpoint richiede la selezione sicura; il Boolean false di questo overload di Session.Create riguarda updateBeforeConnect. Non copiarli come generici flag di sicurezza. Per comprenderne il significato, consulta la firma dell'overload nella versione SDK installata.

60_000 è una richiesta di timeout di sessione, non la periodicità delle letture. Il server può rivedere i parametri. Anche l'indirizzo discovery può restituire endpoint con host differente: in una rete industriale verifica raggiungibilità, nome host e certificato anziché sostituire il nome arbitrariamente.

## Identità applicativa e identità utente

Il certificato UA del client dimostra l'identità dell'applicazione nel canale. AnonymousIdentityToken significa che l'utente non fornisce credenziali personali. Non c'è contraddizione tra canale cifrato e utente anonimo: protezione del trasporto e autorizzazione rispondono a domande diverse.

Configuration genera store distinti per server e client. Il trust di laboratorio compara il DER esatto del peer locale soltanto quando l'errore è BadCertificateUntrusted. Non rende accettabili errori di scadenza, host o algoritmo.

## Laboratorio con risultati attesi

1. Avvia server e client read: annota SessionId e security policy.
2. Avvia un'altra console client read: SessionId deve differire, la sorgente dei valori resta la stessa.
3. Arresta solo la console: il server e il browser devono continuare.
4. Per l'esercizio, usa un endpoint con porta diversa in una copia locale della configurazione: discovery fallisce prima della lettura.
5. Ripristina la porta e ripeti: ora verifica il trust prima di investigare i NodeId.

## Esercizio risolto: classificare gli errori

Devi scegliere tra retry e correzione di configurazione:

| Errore osservato | Ipotesi iniziale | Azione ragionata |
|---|---|---|
| BadNotConnected durante discovery | Trasporto indisponibile | Verificare processo e porta, poi retry limitato |
| BadCertificateUntrusted | Trust assente | Approvare/configurare il peer atteso |
| BadIdentityTokenRejected | Identità utente rifiutata | Correggere token e policy |
| BadNodeIdUnknown durante Read | Contratto o mapping errato | Verificare namespace e browse |

**Soluzione.** Non risolvere ogni eccezione selezionando un endpoint meno sicuro. Il problema va identificato nel livello in cui avviene. Un retry può risolvere un server temporaneamente spento; non corregge una password né un namespace errato.

**Autoverifica.** Perché chiudere il canale non significa necessariamente perdere ogni subscription? Perché le risorse appartengono a livelli con tempi e servizi propri. Quali risorse va comunque rilasciate quando termina il client? Subscription, sessione e oggetti SDK posseduti.

Riferimento: [Services, Part 4](https://reference.opcfoundation.org/Core/Part4/).

[Indice](README.md) · [Uso dei sample](../USAGE.md) · [Architettura](../ARCHITECTURE.md)
