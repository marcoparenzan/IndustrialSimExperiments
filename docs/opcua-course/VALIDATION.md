# Verifiche della versione 0.1.0

## Home e template 0.4.0 — 2026-10-05

Build web passata senza warning/errori. Chromium verifica home, impianto idrico e reattore a 1440, 1024, 768 e 390 px: dodici combinazioni senza overflow della pagina, sovrapposizioni delle label o errori JavaScript. Verificati schede della home, navigazione, reload delle rotte dedicate e identificativo sconosciuto. I valori risultano Good; Start/Stop, Write, ApplyRecipe, letture strutturate e LoadRecipe passano, inclusi rifiuto del nome vuoto e conservazione della ricetta precedente. Screenshot in `.artifacts/layout/after-*-home.png`, `after-*-water.png` e `after-*-reactor.png`.

## Ricetta custom 0.3.0 — 2026-10-05

`verify` con server TCP reale passa tredici laboratori. custom-types verifica StructureDefinition, HasEncoding, input/output di LoadRecipe, readback, subscription del valore composto e BadOutOfRange senza modifiche per uno step NaN. Il client dinamico separato ha decodificato Name/Revision/Target/Steps dai metadati senza la DLL del server. Build dei tre progetti senza warning/errori. Chromium ha verificato caricamento/lettura custom e rifiuto del nome vuoto, oltre ai controlli layout sulle otto combinazioni già previste.

## Aggiornamento didattico 0.2.0 — 2026-10-05

Build del client aggiornata senza warning/errori e `client all` eseguito su server UA TCP: dodici laboratori passati, inclusi i nuovi `ranges`, `model` e `method-errors`. Verificati range inclusivo e range senza dati, TypeDefinition/Mandatory, BadInvalidState e conservazione dello stato dopo una chiamata rifiutata. Le funzionalità avanzate descritte come progetti non sono incluse nel risultato del test.

Eseguite il 2026-10-05 su Windows, SDK .NET 10.0.103.

| Controllo | Risultato |
|---|---|
| Build della soluzione | Passata, zero warning e zero errori |
| `UaCourse verify` con server TCP reale | Passata, exit code 0 |
| Browse / BrowseNext / browse path | Componenti ricevuti e percorso risolto |
| Read / Write / readback | Attributi validi e setpoint aggiornato |
| Strutture e array | Range Low/High e Profile Double[] decodificati |
| Call | Stop, ApplyRecipe e Start con output validi |
| Errori attesi | BadOutOfRange, BadTypeMismatch, BadNotWritable, BadNodeIdUnknown |
| Data change ed eventi | Notifiche ricevute attraverso Publish |
| HTTP pagina Blazor | 200, stato connesso, 20 label Good e 2 hotspot |
| Asset statici | water.svg, reactor.svg e course.css rispondono 200 |
| Arresto server | 20 label non valide e zero label Good |
| Ripartenza server | Nuova connessione, 20 label Good ed eventi ricevuti |

## Verifica template e layout — 2026-10-05

Il controllo HTTP iniziale verificava il prerender. Chromium ha successivamente rilevato HTTP 500 per gli script Blazor avviando con `--no-launch-profile`: corretto il caricamento degli static web assets dal manifest.

- Entrambi i sinottici verificati a 1440, 1024, 768 e 390 pixel.
- Nessuna collisione tra label, nessun overflow orizzontale della pagina, sfondi caricati e zero errori JavaScript.
- Canvas minimo di 1000 px: sui piccoli schermi scorre solo il sinottico, con pannelli comandi ed eventi adattivi.
- Verificati cambio schermata, lettura strutturata, Start/Stop dagli hotspot, Write e ApplyRecipe.
- Migliorati focus da tastiera, selezione del sinottico e accessibilità dello scorrimento.
- Pulsanti abilitati dopo il primo rendering interattivo, messaggi del template in italiano e demo inutilizzate rimosse.

Runner: `scripts/check-layout.cjs`. Richiede Playwright e il relativo Chromium installati. Con Playwright risolvibile da Node:

```powershell
node scripts/check-layout.cjs playwright http://localhost:5080 after
```

Screenshot delle otto combinazioni: `.artifacts/layout/after-*.png`. Gli screenshot desktop e mobile sono stati anche ispezionati visivamente. Altri browser, zoom di sistema e lifecycle SignalR in condizioni di guasto non sono coperti da questo controllo.

Non verificati: interoperabilità con client di terzi, carico elevato, perdita pacchetti, overflow, host remoti e autorizzazioni. Le estensioni advanced della roadmap non sono incluse nei risultati.
