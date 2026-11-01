# Changelog

## 0.4.0 — 2026-10-05

- Home neutra con anteprime e link generati da ScreenCatalog.
- Dashboard su rotte dedicate, accesso diretto, reload e gestione degli identificativi sconosciuti.
- Nuovo template UA Lab con sidebar scura, pannelli chiari e navigazione superiore su mobile.
- Runner Chromium aggiornato: dodici combinazioni pagina/larghezza, navigazione e comandi UA.

## 0.3.0 — 2026-10-05

- Implementato RecipeDataType con quattro campi, encoding binario, StructureDefinition ed export NodeSet2 parziale.
- Aggiunti BatchRecipe, LoadRecipe con input strutturato, validazione e clone del valore.
- Creato laboratorio custom-types con Read/Call/Publish e rifiuto delle ricette invalide; sequenza portata a tredici laboratori.
- Aggiunto client ComplexTypes indipendente senza RecipeData e senza DLL del server; decoding da metadati verificato.
- Integrati caricamento/lettura della ricetta nel pannello Blazor e nel runner Chromium.
- Riscritta la lezione 09 sul codice eseguibile e aggiornati stato delle funzionalità e documentazione.

## 0.2.0 — 2026-10-05

- Ampliati tutti i 18 capitoli con spiegazioni, codice guidato, risultati attesi, diagnostica, esercizi e soluzioni ragionate.
- Approfonditi recovery, rischi di perdita notifiche, callback tardivi e differenze tra stato di connessione e freschezza del dato.
- Separati i laboratori console in file dedicati, mantenendo `Lessons.cs` come facade compatibile con la web app.
- Aggiunte soluzioni eseguibili per IndexRange, contratto del modello e precondizioni/errori dei metodi.
- Portata la sequenza automatica da nove a dodici laboratori; aggiornata la mappa capitoli/codice/comandi.

## 0.1.1 — 2026-10-05

- Corretto il caricamento degli script Blazor nell'avvio senza launch profile.
- Sistemati layout responsive, box sizing, scorrimento del canvas e posizioni delle label.
- Migliorati focus da tastiera e selezione; comandi attivi dopo il rendering interattivo.
- Localizzati i messaggi del template e rimossi pagine e navigazione demo inutilizzate.
- Aggiunto runner Chromium con screenshot e verifica dei comandi UI.

## 0.1.0 — 2026-10-05

- Creato laboratorio C#/.NET 10 con SDK ufficiale OPC Foundation e versione fissata.
- Aggiunti server OPC UA, reattore e impianto idrico simulati, client didattico e comando verify.
- Implementati browse, browse path, Read, Write validata, metodi, strutture standard, array, subscription ed eventi.
- Creata web app Blazor Server con catalogo modulare di due schermate e servizio UA condiviso con reconnect.
- Ricostruito il sinottico idrico di riferimento come SVG pulito; aggiunto sinottico del reattore.
- Aggiunti documentazione d'uso, architettura, percorso lezioni, roadmap e punti aperti.
