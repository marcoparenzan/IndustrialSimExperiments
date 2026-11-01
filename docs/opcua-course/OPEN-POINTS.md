# Punti aperti

| Tema | Decisione attuale | Prossimo passo |
|---|---|---|
| Template | UA Lab locale, sidebar scura e home neutra; verificato desktop/mobile | Valutare preferenze grafiche e ulteriori dashboard |
| Sfondo fornito | Ricostruzione SVG del contenuto funzionale | Confermare se serve una replica raster fedele |
| Fedeltà del processo | Oscillazioni sintetiche e modello del reattore | Bilancio idraulico, pompe indipendenti e fault injection |
| Unità del riferimento | GPM, MGD, ft, in, PSI, gal come nel sinottico | EUInformation/EURange per ogni variabile; eventuale vista SI |
| Comandi | Start/Stop/ApplyRecipe sul reattore | Hotspot e comandi PumpType per ogni pompa |
| Strutture | Range, array e RecipeDataType; decoder dinamico indipendente verificato | Enum, campi opzionali e generazione ModelCompiler |
| Modello | ReactorType con Temperature Mandatory | Contratto completo, NodeSet2 e versione modello |
| Sicurezza | Anonymous, trust locale per DER | Trust list configurabili, ruoli, identità e audit |
| Riconnessione | Nuova sessione e nuove subscription | Recupero allarmi e strategie di continuità eventi |
| UI refresh | Snapshot renderizzato ogni 500 ms | Aggiornamenti aggregati guidati dalle notifiche |
| Performance | Una subscription condivisa, code finite | Misure di overflow, latenza e limiti operativi |
| Packaging | Web referenzia il progetto console | Libreria di protocollo indipendente |
| SDK | Versione API fissata 1.5.374.158 | Valutare aggiornamento, cambi API e audit dipendenze |
| Lezioni avanzate | Documentazione progettuale | Implementare A&C, HistoryRead, PubSub, GDS in moduli dedicati |
| Validazione UI | Build e smoke HTTP | Browser automatizzato e scenari multipli di circuito |

I livelli di riempimento e le icone degli sfondi restano statici; le label sono dinamiche. Renderizzare riempimenti, stato pompe e valvole è un'estensione del renderer SVG.
