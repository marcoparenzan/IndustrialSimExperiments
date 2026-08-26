# Prompt log

1. **Obiettivo:** "Puoi fare una verifica generale sul progetto e vedere se funziona correttamente?"
   **Attività prevista:** esaminare struttura, configurazione e codice esistenti, quindi eseguire build e test e correggere gli eventuali problemi riscontrati.

2. **Obiettivo:** "Toglierei le librerie kpi e oeekpi....le ho salvate. Vorrei rendere il progetto modulare. L'applicazione si deve chiamare IndustrialSimApp. ConveyorSimApp deve diventare una lib. devo poter avviare IndustrialSimApp come Conveyor o in futuro qualcosa altro per cui creare altre specifiche lib. devo poter esporre quella macchina come OPC UA, ma in futuro anche modbus oppure S7 in modo tale da fare tutte le simulazioni del caso. Puoi aggiornare la soluzione di conseguenza? Puoi creare tutti i file di documentazione del caso?"
   **Attività prevista:** rimuovere i moduli KPI/OEE, trasformare Conveyor in un modulo macchina riutilizzabile e introdurre IndustrialSimApp come host modulare con protocolli industriali intercambiabili, test e documentazione completa.
