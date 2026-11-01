# Verification log

## Integration into ForgeIndustrial — 2026-11-01

Run on Windows, .NET SDK 11.0.100 (release candidate), OPC UA SDK 1.5.376.244, all outputs outside the repository.

| Check | Result |
|---|---|
| Whole-solution build (`src/ForgeIndustrial.slnx`) | 0 errors; the course projects add one warning (CS0618: `CheckApplicationInstanceCertificate` is obsolete in the new SDK) |
| `UaCourse verify` against a real TCP server on port 4841 | Exit code 0; thirteen labs, 38 assertions OK |
| `client custom-types` and `UaCourse.DynamicClient` | Exit code 0; the recipe is decoded without the server assembly |
| Web app on port 5080, `/`, `/dashboards/water`, `/dashboards/reactor` | HTTP 200 |
| Headless Chromium screenshots of the three pages | Opened and inspected: English texts, connection chip "OPC UA · SignAndEncrypt · subscription 250 ms", live values on both synoptics |

Not verified in this run: clicking the commands in the browser, the automated layout runner (`check-layout.cjs`, which needs the Playwright npm package), mobile widths, and the page for an unknown dashboard identifier.

## Home and template 0.4.0 — 2026-10-05

Web build passed without warnings or errors. Chromium verifies home, water plant and reactor at 1440, 1024, 768 and 390 px: twelve combinations with no page overflow, label overlaps or JavaScript errors. Verified: home cards, navigation, reload of the dedicated routes and an unknown identifier. The values are Good; Start/Stop, Write, ApplyRecipe, structured reads and LoadRecipe pass, including the rejection of an empty name and the preservation of the previous recipe. Screenshots in `.artifacts/layout/after-*-home.png`, `after-*-water.png` and `after-*-reactor.png` (in the original repository; now `<artifacts root>/layout`).

## Custom recipe 0.3.0 — 2026-10-05

`verify` against a real TCP server passes thirteen labs. custom-types verifies StructureDefinition, HasEncoding, the input/output of LoadRecipe, readback, the subscription of the composite value and BadOutOfRange with no changes for a NaN step. The separate dynamic client decoded Name/Revision/Target/Steps from the metadata without the server DLL. Build of the three projects without warnings or errors. Chromium verified custom loading/reading and the rejection of an empty name, in addition to the layout checks on the eight combinations already planned.

## Teaching update 0.2.0 — 2026-10-05

Client build updated without warnings or errors and `client all` run against a UA TCP server: twelve labs passed, including the new `ranges`, `model` and `method-errors`. Verified: inclusive range and range without data, TypeDefinition/Mandatory, BadInvalidState and preservation of state after a rejected call. The advanced features described as projects are not included in the test result.

Run on 2026-10-05 on Windows, .NET SDK 10.0.103.

| Check | Result |
|---|---|
| Solution build | Passed, zero warnings and zero errors |
| `UaCourse verify` with a real TCP server | Passed, exit code 0 |
| Browse / BrowseNext / browse path | Components received and path resolved |
| Read / Write / readback | Valid attributes and setpoint updated |
| Structures and arrays | Range Low/High and Profile Double[] decoded |
| Call | Stop, ApplyRecipe and Start with valid outputs |
| Expected errors | BadOutOfRange, BadTypeMismatch, BadNotWritable, BadNodeIdUnknown |
| Data change and events | Notifications received through Publish |
| HTTP Blazor page | 200, connected state, 20 Good labels and 2 hotspots |
| Static assets | water.svg, reactor.svg and course.css respond 200 |
| Server shutdown | 20 invalid labels and zero Good labels |
| Server restart | New connection, 20 Good labels and events received |

## Template and layout verification — 2026-10-05

The initial HTTP check verified the prerender. Chromium subsequently detected HTTP 500 for the Blazor scripts when starting with `--no-launch-profile`: fixed the loading of the static web assets from the manifest.

- Both synoptics verified at 1440, 1024, 768 and 390 pixels.
- No collisions between labels, no horizontal page overflow, backgrounds loaded and zero JavaScript errors.
- Minimum canvas of 1000 px: on small screens only the synoptic scrolls, with adaptive command and event panels.
- Verified: screen change, structured read, Start/Stop from the hotspots, Write and ApplyRecipe.
- Improved keyboard focus, synoptic selection and scrolling accessibility.
- Buttons enabled after the first interactive render, template messages in Italian (the course has since been translated to English) and unused demos removed.

Runner: `src/OpcUaCourse/scripts/check-layout.cjs`. It requires Playwright and its Chromium to be installed. With Playwright resolvable from Node:

```powershell
node src/OpcUaCourse/scripts/check-layout.cjs playwright http://localhost:5080 after
```

Screenshots of the eight combinations are written to `<artifacts root>/layout/after-*.png`. The desktop and mobile screenshots were also inspected visually. Other browsers, system zoom and the SignalR lifecycle under fault conditions are not covered by this check.

Not verified: interoperability with third-party clients, high load, packet loss, overflow, remote hosts and authorizations. The advanced extensions of the roadmap are not included in the results.
