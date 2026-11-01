# Open points

| Topic | Current decision | Next step |
|---|---|---|
| Template | Local UA Lab, dark sidebar and neutral home page; verified on desktop/mobile | Evaluate graphic preferences and further dashboards |
| Provided background | SVG reconstruction of the functional content | Confirm whether a faithful raster replica is needed |
| Process fidelity | Synthetic oscillations and a reactor model | Hydraulic balance, independent pumps and fault injection |
| Reference units | GPM, MGD, ft, in, PSI, gal as in the synoptic | EUInformation/EURange for every variable; optional SI view |
| Commands | Start/Stop/ApplyRecipe on the reactor | Hotspots and PumpType commands for every pump |
| Structures | Range, arrays and RecipeDataType; independent dynamic decoder verified | Enums, optional fields and ModelCompiler generation |
| Model | ReactorType with Temperature Mandatory | Full contract, NodeSet2 and model version |
| Security | Anonymous, local trust by DER | Configurable trust lists, roles, identity and audit |
| Reconnection | New session and new subscriptions | Alarm recovery and event-continuity strategies |
| UI refresh | Snapshot rendered every 500 ms | Aggregated updates driven by notifications |
| Performance | One shared subscription, finite queues | Measurements of overflow, latency and operating limits |
| Packaging | The web app references the console project | Independent protocol library |
| SDK | API version pinned at 1.5.376.244 (aligned with ForgeSimApp; it was 1.5.374.158 before the move into ForgeIndustrial) | Evaluate updates, API changes and a dependency audit |
| Advanced lessons | Design documentation | Implement A&C, HistoryRead, PubSub, GDS in dedicated modules |
| UI validation | Build and HTTP smoke test | Automated browser and multiple-circuit scenarios |

The fill levels and the icons of the backgrounds remain static; the labels are dynamic. Rendering fills, pump state and valves is an extension of the SVG renderer.
