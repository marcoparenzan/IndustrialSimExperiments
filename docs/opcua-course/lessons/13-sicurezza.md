# Certificati, identità, autorizzazione e audit

## Concetti

Il certificato applicativo identifica il peer; UserIdentity identifica l'utente. SecurityPolicy e MessageSecurityMode proteggono il canale. AccessLevel è capacità del nodo, UserAccessLevel dipende dall'utente. Un ruolo può autorizzare Read ma vietare Write e Call. Audit e log di diagnostica hanno scopi differenti.

## Esperimento

Ispeziona gli store own/trusted/issuers/rejected sotto .artifacts/pki. Il laboratorio usa solo SignAndEncrypt e trust locale per confronto DER; non ha endpoint None. Anonymous resta un'identità con permessi didattici.

## Nel codice C#

Configuration.CertificateValidation accetta solo BadCertificateUntrusted del peer locale esatto. UaConnection è condivisa e tutti i browser usano la stessa identità. Il server non implementa ancora ruoli né login web.

## Esercizio e verifica

In un modulo separato configura trust list esplicite, utente viewer e operator. Verifica che viewer non possa scrivere né chiamare Start, anche aggirando la UI. Pubblica eventi di audit dei comandi.

## Errore da evitare

Disabilitare un bottone non è autorizzazione. Non esporre questo laboratorio in rete come servizio operativo senza autenticazione web, configurazione trust e policy server.

## Obiettivi e stato del sample

Tempo indicativo: 120 minuti. Questa lezione combina osservazione di funzionalità presenti e progettazione delle autorizzazioni non ancora implementate. Il canale cifrato è presente; ruoli, login web e audit applicativo completo non lo sono.

Leggi [Configuration.cs](../../src/UaCourse/Configuration.cs) e [UaConnection.cs](../../src/UaCourse.Web/Services/UaConnection.cs). Identifica chi possiede i certificati, chi sceglie UserIdentity e quale nodo autorizza Write.

## Le domande a cui risponde ciascun meccanismo

| Meccanismo | Domanda |
|---|---|
| Certificato applicativo | Quale applicazione è il peer? |
| Trust list e validazione | Mi fido di quel certificato in questo contesto? |
| SignAndEncrypt | Il messaggio è protetto in transito? |
| UserIdentity | Quale utente rappresenta la sessione? |
| Ruoli e permessi | Quali operazioni può eseguire l'utente? |
| Audit | Chi ha richiesto quale operazione e con quale esito? |

Un certificato valido ma non trusted e un certificato scaduto sono problemi distinti. Il callback del laboratorio accetta soltanto il DER esatto del peer locale e solo BadCertificateUntrusted. Non deve diventare un e.Accept=true indiscriminato per qualsiasi errore.

## Store e configurazione effettivi

Ogni applicazione ha own, trusted, issuers e rejected sotto .artifacts/pki. La root viene trovata risalendo dall'assembly alla solution; fuori dal repository viene usata la working directory.

Il server espone SignAndEncrypt/Basic256Sha256 e un token utente Anonymous. L'helper di selezione del client sceglie un endpoint sicuro. La convenzione del laboratorio dipende da file locali accessibili a entrambe le applicazioni: per host separati va sostituita con un provisioning della fiducia appropriato.

Le chiavi DataProtection ASP.NET sono un'altra famiglia di chiavi, non i certificati UA. Proteggono funzionalità web; lo store del laboratorio è isolato in .artifacts/aspnet-keys, senza configurazione di cifratura a riposo.

## Autorizzazione lato server

AccessLevel descrive capacità generali del nodo, UserAccessLevel quelle dell'utente nel contesto della richiesta. Per Methods sono rilevanti Executable/UserExecutable e i controlli effettivi del servizio. Non impostare permessi UI e presumere che proteggano il server.

La Session UA della web app è condivisa: tutti i browser usano l'identità anonima della stessa applicazione. Aggiungere un login Blazor non trasferisce automaticamente l'identità personale alla sessione UA. Devi decidere se usare sessioni per identità, una service identity autorizzata o un gateway che esegua policy e audit applicativi.

## Esercizio risolto: viewer e operator

Progetta due ruoli:

| Operazione | Viewer | Operator |
|---|---|---|
| Browse/Read | Consentita | Consentita |
| Subscription | Consentita | Consentita |
| Write Setpoint | Vietata | Consentita con range |
| Call Start/Stop | Vietata | Consentita |
| Gestire trust e utenti | Vietata | Vietata, ruolo amministrativo separato |

**Soluzione.** Implementa i controlli nelle richieste server usando l'identità/ruoli della sessione. La UI può disabilitare controlli per chiarezza, ma la prova decisiva è una richiesta diretta di un client viewer: deve fallire anche senza browser.

Non usare uno schema C# fittizio come presunta API di autorizzazione dello SDK: i callback e i role permission disponibili dipendono dalla release. Parti dall'implementazione e dai sample della versione fissata, poi verifica ogni servizio.

## Audit e rinnovo

Un evento di processo "Reattore avviato" non contiene necessariamente identità del richiedente, esito della validazione e correlation id. Registra sia comandi riusciti sia rifiutati con timestamp e contesto, evitando di pubblicare password o chiavi private.

Prova il rinnovo del certificato in un ambiente di laboratorio: la fingerprint cambia, quindi il trust per DER esatto deve essere aggiornato in modo controllato. GDS/CertificateManager e gestione automatizzata sono un modulo futuro con infrastruttura propria.

**Autoverifica.** HTTPS del browser rende automaticamente sicuro UA TCP? No, sono canali distinti. Anonymous significa canale non cifrato? No. Un pulsante nascosto vieta Call? No.

Riferimenti: [Security Model, Part 2](https://reference.opcfoundation.org/Core/Part2/), [Services, Part 4](https://reference.opcfoundation.org/Core/Part4/).

[Indice](README.md) · [Uso dei sample](../USAGE.md) · [Architettura](../ARCHITECTURE.md)
