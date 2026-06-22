# Raft Game Plan — Learning-First Approach

Reference document for building a Raft-like survival game in **Raft-proto** (Unity 6 URP).

---

## Current Project State

**Raft-proto** is a clean Unity 6 URP starter project:

- **Unity version:** 6000.5.0f1
- **Scene:** `Assets/Scenes/SampleScene.unity` (camera, directional light, global volume)
- **Rendering:** URP configured for PC and Mobile
- **Input:** `Assets/InputSystem_Actions.inputactions` (Move, Look, Jump, Interact, Attack, etc.) — not wired to code yet
- **Gameplay:** None — no scripts, prefabs, or game assets yet

This is a good starting point: infrastructure is ready, and every system will be built intentionally for learning.

---

## Build Progress — Resume Here

> Snapshot of what's actually built so you can continue on another machine. **Commit & push everything (including `Assets/Scripts/**` and `Assets/Materials/M_Ocean.mat`) before switching.** Unity will rebuild `Library/` on the new machine; empty asset folders (`Prefabs`, `Data`, `Art`) won't be in git until they contain assets — that's fine.

**Phases 1–3 complete.** Next action: start **Phase 4 (inventory & crafting)** — hook `ResourceCollector.Collected` into a real inventory.

**Done since last snapshot:**
- **Phase 1.4 camera:** `FollowCamera.cs` in place.
- **Phase 2.1 grid:** `RaftGrid.cs` — integer XZ grid, coord conversions, occupancy, `CanPlaceTile` (empty + adjacent), `RegisterTile`/`RemoveTile`, and a `TilesChanged` event.
- **Phase 2.2 building:** `BuildingSystem.cs` — aims by **projecting the camera ray onto the raft's deck plane** (works while the raft tilts/bobs and lets you point at empty water next to the raft). Ghost preview + horizontal-only reach gate. Place on **Interact**; **test-only removal on key `X`** (`removeKey`, later gated behind an axe). Won't remove the last remaining tile.
- **Phase 2.3 raft growth (physics):** `Buoyancy.cs` is now **grid-driven** — one float sample per occupied cell, `mass = tileCount × massPerTile`, and center of mass recomputed to the deck centroid + ballast each `TilesChanged`. Ride height is constant for any size/shape because force budget and mass both scale with tile count. Old fixed `FloatPoint` children are no longer used.

**Building/grid scene values:** `RaftGrid.tileSize = 2` (matches the 2 m `DeckTile`); tiles use `Assets/Prefabs/DeckTile.prefab` (Variant of kit `WoodFloor1`, Rigidbody removed); `Buoyancy` auto-finds `RaftGrid` on the same object; `Mass Per Tile 10`, `Float Sample Local Y -0.6`, `Ballast Local Y -0.9`.

**Known gaps / TODO:** removal currently lets you split the raft into disconnected islands (no connectivity check yet); building/removal are local-only (server-authoritative conversion deferred to Phase 6).

### Decisions locked in

- **Networking:** Netcode for GameObjects, host/client, server-authoritative. **Not installed yet** — deferred to Phase 6.
- **Assemblies:** one asmdef per system (see below). Networking sits at the top of the dependency graph; gameplay assemblies must not reference it.
- **Convention:** the ocean surface is at **world `y = 0`** for *both* the visual mesh and the buoyancy physics. Keep them in sync.
- **Experience/teaching:** step-by-step, explain the "why" at an experienced-dev level (see `.cursorrules`).

### Files created

- 8 assembly definitions: `Assets/Scripts/{Core,Items,Player,Raft,Building,Resources,UI,Networking}/RaftProto.*.asmdef`
- `Assets/Scripts/Raft/Buoyancy.cs`
- `Assets/Scripts/Player/PlayerController.cs`
- `Assets/Scripts/Core/InputSystem_Actions.cs` — **generated** from the input asset (don't hand-edit)
- `TutorialInfo` sample removed; `Assets/Scenes/OceanPrototype.unity` is the dev scene

### Scene setup (`OceanPrototype.unity`)

**Ocean** — Plane, Position `(0,0,0)`, Scale `(50,1,50)`, material `M_Ocean` (URP/Lit, Surface Type *Transparent*, blue, alpha ~180). **Mesh Collider removed** (force-based buoyancy needs the water *level*, not a collider).

**Raft** — empty root. Start `Y ≈ 0.1` for play (higher only to test the splash). Components:
- `Rigidbody`: Mass `40`, Use Gravity on, Linear Damping `0`, Angular Damping `0.05`
- `Buoyancy.cs`: Water Level `0`, Buoyancy Strength `1.6`, Max Submergence Depth `0.8`, Buoyancy Damping `3`, Override Center Of Mass ✔, Center Of Mass `(0, -0.9, 0)`, Float Points = the 4 `FloatPoint` children
- Children `Deck_0..3`: Cubes, Scale `(1, 0.25, 1)`, local positions `(±0.5, 0, ±0.5)` — each keeps its `BoxCollider` (compound collider)
- Children `FloatPoint_0..3`: empties, local positions `(±0.5, -0.6, ±0.5)`

**Player** — Capsule renamed `Player`, **Capsule Collider removed**, `CharacterController` (defaults: Height 2, Radius 0.5, Center 0), `PlayerController.cs`, Position `(0, 1.3, 0)`, Camera Transform left empty (auto-binds `Main Camera`).

**Input** — `InputSystem_Actions` asset has **Generate C# Class** enabled, output `Assets/Scripts/Core/InputSystem_Actions.cs`, class `InputSystem_Actions`, namespace blank.

### Tuning reference (buoyancy)

- Rest depth of float points ≈ `maxSubmergenceDepth / buoyancyStrength`.
- **Freeboard (deck height above water)** = `(deckTopLocalY − floatPointLocalY) − restDepth`.
- `buoyancyStrength` = ride height + rebound; `buoyancyDamping` = bob/dip (≈3 is a smooth settle); `centerOfMass` Y below float points = roll stability.

### Gotchas already hit (so you don't re-debug)

- Changing a script's **default field value does NOT change values already serialized** on a component in the scene — set them in the Inspector.
- The **Plane primitive ships with a Mesh Collider** — the raft was landing on it instead of dipping.
- Ocean was accidentally at `y = 1` while buoyancy used `0` — they must match.
- A flat raft floats with a **weakly stable / unstable roll equilibrium**; fix is a **low center of mass** (ballast), not more damping.
- Generated input class must live **inside an asmdef** (`Scripts/Core`), not `Assets/` root, or our assemblies can't reference it.

---

## What We're Building Toward

A raft-like game has four core pillars:

| Pillar | What the player feels |
|--------|------------------------|
| **Ocean world** | Endless water, horizon, drifting resources |
| **Raft as home** | Small platform that grows tile-by-tile |
| **Survival loop** | Gather → craft → expand → repeat |
| **Physical presence** | Walk on a moving raft, interact with the world |

This is a **co-op multiplayer game**: multiple players share one raft, gather together, and build the same world. Multiplayer is a fifth pillar that touches every other system:

| Pillar | What the player feels |
|--------|------------------------|
| **Shared world** | Friends on the same raft, seeing each other move, build, and gather in real time |

Build these in order. Each phase produces something playable and teaches a distinct skill.

> **Multiplayer is the hardest "bolt-on" in game dev.** Retrofitting networking onto finished single-player code usually means rewriting movement, building, inventory, and spawning. So we still build single-player first (it's the fastest way to learn each system), but **every system is designed to be multiplayer-friendly from day one** — see the "Multiplayer-aware design" notes in each phase and the dedicated networking phase below.

---

## Phase 0 — Project Setup

Before writing gameplay code, set up organization.

### Folder structure

```
Assets/
├── Scenes/           (OceanPrototype.unity — main dev scene)
├── Scripts/
│   ├── Player/
│   ├── Raft/
│   ├── Building/
│   ├── Resources/
│   ├── Inventory/
│   ├── Crafting/
│   ├── Networking/      (connection, spawning, ownership helpers)
│   └── UI/
├── Prefabs/
├── Materials/
├── Data/             (ScriptableObjects: items, recipes)
└── Art/              (models, textures — placeholder cubes are fine)
```

### Housekeeping (optional)

- Remove `Assets/TutorialInfo/` via the Readme panel's "Remove Readme Assets" button
- Duplicate `SampleScene.unity` → `OceanPrototype.unity` when starting to build

**Learn:** How Unity projects stay maintainable as they grow.

### Choose a networking stack (decide now, integrate in Phase 1)

Pick the transport/replication library before writing the player controller — it shapes how every script is structured.

| Option | Why / when | Notes |
|--------|------------|-------|
| **Netcode for GameObjects (NGO)** | Official Unity package, free, great docs, server-authoritative `NetworkBehaviour` / `NetworkVariable` / RPC model | **Recommended for learning** — first-party with URP, integrates via Package Manager |
| Mirror | Mature, free, large community, lots of tutorials | Third-party; very Raft-clone-friendly |
| Photon Fusion / PUN | Managed relay, easy hosting, good for "invite a friend" | Paid tiers; less to learn about transport internals |

**Recommendation:** **Netcode for GameObjects** with a **host/client (listen-server)** topology — one player hosts and also plays, others join. This is the standard model for small co-op survival games and the simplest to reason about for authority.

**Authority model:** server-authoritative for shared/simulated state (raft physics, resource spawns, inventory, world). Clients send *intent* (move, place tile, pick up) via RPCs; the server validates and applies, then replicates results. This prevents desync and cheating and is the mental model to internalize early.

**Learn:** Client-server vs peer-to-peer, authority/ownership, replication, the cost of trusting clients.

---

## Phase 1 — "Stand on a Raft in the Ocean"

**Goal:** Playable scene where you walk on a small raft over water.

**Target milestone:** WASD on a 2×2 raft, ocean around you, camera follows, raft bobs gently.

### 1.1 Ocean surface

- Large plane or tiled mesh at `y = 0`
- Simple URP material (blue, slight transparency) — Shader Graph can come later
- Optional: infinite ocean illusion via repeating texture or a follow script

**Learn:** Scenes, transforms, materials, URP rendering basics.

### 1.2 Starter raft

- 2×2 grid of cube/plank meshes under a `Raft` root GameObject
- `Rigidbody` + colliders so the raft floats and the player can stand on it
- Simple buoyancy script: apply upward force when raft is below water level

**Learn:** Physics (`Rigidbody`, colliders), parent-child hierarchy, `FixedUpdate` vs `Update`.

### 1.3 Player controller

- `CharacterController` (simpler) or `Rigidbody` (more realistic on moving platforms)
- Wire up existing `InputSystem_Actions`: **Move**, **Look**, **Jump**
- Movement relative to camera or raft forward

**Learn:** Input System (generate C# class from `.inputactions`), character movement, camera-relative input.

### 1.4 Camera

- Third-person follow cam (Raft-style) or first-person
- Smooth follow + look with mouse/right stick

**Learn:** Camera rigs, `LateUpdate`, interpolation.

### Phase 1 architecture

```
Input System → PlayerController → Raft (Rigidbody)
Buoyancy Script → Raft
Ocean Plane → Buoyancy (water level reference)
Follow Camera → Player
```

### Suggested scripts

| Script | Responsibility |
|--------|----------------|
| `Buoyancy.cs` | Upward force when below water level |
| `PlayerController.cs` | Read input, move character |
| `FollowCamera.cs` | Smooth camera follow and look |
| `NetworkBootstrap.cs` | Start host/client, connect, basic menu (Host / Join) |

### Multiplayer-aware design

- Make the **player a networked prefab**; spawn one per connected client. The local player owns its input; remote players are replicated.
- `PlayerController` should split **input reading** (local owner only) from **movement application**. Only act on input if `IsOwner`.
- `FollowCamera` follows the **local owner's** player only — don't put a camera on remote players.
- **Raft physics runs on the server.** The buoyant `Rigidbody` is simulated server-side and its transform replicated (e.g. a `NetworkTransform`); clients don't each run their own physics.
- Standing on a moving networked platform is the classic hard problem — note it now, solve it properly in the networking phase.

---

## Phase 2 — "Build Your Raft"

**Goal:** Place new floor tiles on a grid — the signature raft mechanic.

**Target milestone:** Place floor tiles on the raft grid with Interact; raft grows visually and physically.

### 2.1 Grid system

- Integer grid coords `(x, z)` on the raft
- Dictionary/map: grid cell → placed tile
- Snap positions to grid spacing (e.g. 1m)

**Learn:** Data structures in Unity, coordinate math, raycasting.

### 2.2 Building mode

- Raycast from camera center on **Interact** (hold)
- Ghost preview (semi-transparent cube) at valid snap point
- Place tile if: adjacent to existing tile, player has materials (hardcode "free" at first)

**Learn:** `Physics.Raycast`, layer masks, preview/validation patterns.

### 2.3 Raft growth

- New tiles become children of `Raft` root
- Recalculate raft center of mass / bounds (optional early; important later)

**Learn:** Dynamic hierarchy, collider composition, scaling systems.

### Suggested scripts

| Script | Responsibility |
|--------|----------------|
| `RaftGrid.cs` | Tile placement logic, grid storage |
| `BuildingSystem.cs` | Raycast, preview, place validation |
| `RaftController.cs` | Owns Rigidbody, buoyancy, grid reference |

### Multiplayer-aware design

- **Placement is a request, not a direct action.** Client raycasts and shows its own ghost preview locally, then sends a `PlaceTileServerRpc(gridCoord, tileType)`.
- **Server validates** (adjacent to existing tile, cell empty, player has materials) and is the single source of truth for the grid, then spawns/replicates the tile to everyone.
- Ghost preview is **local-only** (don't replicate previews). Each player can preview a different spot at once.
- Keep grid state authoritative on the server; clients mirror it. Two players placing on the same cell on the same frame must resolve to one winner — the server decides.

---

## Phase 3 — "Gather Resources"

**Goal:** Things float by; you collect them.

**Target milestone:** Resources spawn and drift; you pick them up and they disappear.

### 3.1 Floating spawner

- Spawn barrels/plastic/leaves at ocean edges, drift toward raft
- Simple movement script (constant velocity or ocean current)

**Learn:** Object pooling, spawn timers, destroy/recycle patterns.

### 3.2 Collection

- **Hook** (later): throwable rope with pull mechanic — use **Attack**
- **Simple MVP:** walk into floating object or press Interact in range

**Learn:** Trigger colliders, interaction ranges, state machines.

### Suggested scripts

| Script | Responsibility |
|--------|----------------|
| `ResourceSpawner.cs` | Spawn and drift resources |
| `FloatingResource.cs` | Individual resource behavior |
| `ResourceCollector.cs` | Pickup / hook interaction |

### Multiplayer-aware design

- **Only the server spawns resources** and runs their drift movement; spawned objects are networked and replicated to all clients. If every client spawned its own, you'd get duplicate/ghost barrels.
- **Pickup is a server-validated request:** client sends `CollectServerRpc(resourceId)`; server checks range/availability, despawns the resource for everyone, and credits the correct player's inventory.
- Guard against **double-collection** — two players grabbing the same barrel must result in exactly one winner (server decides, then despawns).

---

## Phase 4 — "Inventory & Crafting"

**Goal:** Items you collect become buildable materials.

**Target milestone:** Collect plastic → craft plank → spend plank to place a floor tile.

### 4.1 Item data (ScriptableObjects)

```
ItemDefinition.cs   — name, icon, stack size
RecipeDefinition.cs — inputs[] → outputs[]
```

**Learn:** ScriptableObjects as designer-friendly data, separation of data vs logic.

### 4.2 Inventory

- `Dictionary<ItemId, int>` or slot-based list
- UI panel (uGUI): item icons + counts

**Learn:** UI Canvas, Text/Image, binding data to UI.

### 4.3 Crafting

- Craft menu: select recipe → consume inputs → add outputs
- Gate building: "place floor tile" costs 2× plank

**Learn:** Game economy loops, validation, UI events.

### Suggested scripts

| Script | Responsibility |
|--------|----------------|
| `Inventory.cs` | Item storage and queries |
| `CraftingSystem.cs` | Recipe validation and execution |
| `ItemDefinition.cs` | ScriptableObject item data |
| `RecipeDefinition.cs` | ScriptableObject recipe data |

### Multiplayer-aware design

- **Decide ownership model:** per-player inventory vs. a shared raft storage. Co-op Raft typically has **per-player inventories** plus **shared chests/storage tiles** on the raft.
- Inventory state is **server-authoritative**; replicate each player's own inventory to that player (and shared storage to everyone nearby). Crafting and "spend materials to build" go through `ServerRpc` so the server is the only thing that adds/removes items.
- `ItemDefinition` / `RecipeDefinition` ScriptableObjects must be **identical assets on host and client**; sync items over the wire by a stable **item ID** (int/enum), never by object reference.
- UI binds to the **local player's** replicated inventory only.

---

## Phase 5 — Survival & Polish (Later)

Add when the core loop feels good:

| Feature | Complexity | Teaches |
|---------|------------|---------|
| Hunger / thirst | Medium | Timers, debuffs, game over |
| Water purifier, grill | Medium | Station interactables, crafting stations |
| Shark / threats | High | AI, NavMesh, damage |
| Save/load | Medium | JSON serialization, raft layout persistence |
| Better water shader | Medium–High | Shader Graph, normals, foam |

> Multiplayer is now a **first-class goal**, not a "later" item — see the dedicated phase below. Survival features above should each be built server-authoritative once networking is in.

---

## Phase 6 — "Play Together" (Networking)

**Goal:** Turn the single-player prototype into shared co-op. Best done **incrementally alongside Phases 1–4** rather than all at once at the end.

**Target milestone:** Two builds connect (one hosts, one joins); both players walk the same raft, build, gather, and craft, staying in sync.

### 6.1 Connection & spawning

- Install **Netcode for GameObjects** via Package Manager; add a `NetworkManager` with a transport (Unity Transport).
- `NetworkBootstrap.cs`: simple UI to **Host**, **Join (IP/relay)**, and show connection status.
- Player prefab registered with `NetworkManager`; one spawns per client, owned by that client.

**Learn:** `NetworkManager`, transports, connection approval, networked prefab registration.

### 6.2 Replicating movement & the raft

- Players: `NetworkTransform` (or client-authoritative move + reconciliation) so everyone sees everyone.
- Raft: physics simulated on the **server**, transform replicated; solve **player-on-moving-platform** (parent to raft when grounded, or apply raft velocity to riders).

**Learn:** `NetworkTransform`, interpolation, ownership, moving-platform sync.

### 6.3 Server-authoritative actions

- Convert building, gathering, and crafting to the **request → validate → replicate** pattern using `ServerRpc` / `ClientRpc` and `NetworkVariable`.
- Centralize shared world state (grid, spawned resources, storage) on the server.

**Learn:** RPCs, `NetworkVariable`, authority, anti-desync/anti-cheat basics.

### 6.4 Connection lifecycle & polish

- Handle **late joiners** (sync existing raft + resources to a newly connected player).
- Handle **disconnects** (host migration is hard — for co-op, host leaving can end the session; document this choice).
- Show remote player **name tags**; optional voice/text chat later.

**Learn:** State sync on join, disconnect handling, session lifecycle.

### Suggested scripts

| Script | Responsibility |
|--------|----------------|
| `NetworkBootstrap.cs` | Host/Join UI, start connection |
| `NetworkPlayer.cs` | Owns per-player networked state, name tag |
| `ServerRaftState.cs` | Authoritative grid + tile spawning |
| `ServerResourceState.cs` | Authoritative resource spawn/collect |

---

## Architecture Principles

1. **One script, one job** — `Buoyancy.cs`, `RaftGrid.cs`, `PlayerController.cs`, not one giant `GameManager`.
2. **Data in ScriptableObjects** — items and recipes as assets, not hardcoded lists.
3. **Events over tight coupling** — e.g. `OnItemCollected` → inventory listens; building doesn't know about UI.
4. **Placeholder art is fine** — colored cubes teach systems; swap meshes later.
5. **Commit after each milestone** — small wins, easy to debug.
6. **Server is the source of truth** — for any shared/simulated state, the server decides. Clients send *intent*, never authoritative results.
7. **Separate input from action** — read input only on the owning client; apply effects where authority lives. Makes single→multiplayer conversion mechanical instead of a rewrite.
8. **Sync by ID, not reference** — items, tiles, and resources cross the wire as stable IDs; both host and client resolve them to the same ScriptableObjects/prefabs.

### Core type overview

```
PlayerController      → reads input, moves character
RaftController        → owns Rigidbody, buoyancy, grid
RaftGrid              → tile placement logic
BuildingSystem        → raycast, preview, place
ResourceSpawner       → spawn/drift/collect
Inventory             → item storage
CraftingSystem        → recipe validation
GameEvents (static)   → optional event bus for decoupling
NetworkBootstrap      → host/join, connection lifecycle
NetworkPlayer         → per-player networked state/ownership
```

---

## Existing Assets to Reuse

| Asset | Use for |
|-------|---------|
| `InputSystem_Actions.inputactions` | Move, Look, Jump, Interact, Attack — don't recreate bindings |
| URP pipeline (PC + Mobile) | Ocean, post-processing, future water shader |
| `Global Volume` + bloom | Ocean mood / sunset look |
| Empty `Water` layer in tags | Ocean vs raft vs player collision layers |

---

## Scope Paths

Choose how deep to go before expanding:

### Path A — Minimal learning prototype (recommended)

Phases 1–2 **single-player**. Walk, float, build tiles — but written multiplayer-aware (split input from action, server-friendly structure). ~1–2 weeks to something fun.

### Path B — Core Raft loop

Phases 1–4 single-player, then **Phase 6 networking** layered on. Gather → craft → build, then make it co-op. ~1–2 months for a playable shared slice.

### Path C — Feature-complete co-op clone

Everything including survival, threats, save, islands — all server-authoritative co-op. Months of work — not ideal for early learning.

**Recommendation:** Path A first (single-player but multiplayer-aware), then add networking (Phase 6) once Phase 1–2 feel solid. **Don't try to write everything networked on day one** — get one system working solo, convert it to server-authoritative, internalize the pattern, then repeat.

---

## Implementation Checklist

Use this to track progress:

### Phase 0
- [x] Create folder structure under `Assets/` (incl. `Scripts/Networking/`) — with per-system asmdefs
- [x] Duplicate scene → `OceanPrototype.unity`
- [x] Remove TutorialInfo assets
- [x] Choose networking stack (Netcode for GameObjects, host/client)
- [x] Decide authority model (server-authoritative) and inventory ownership (per-player + shared storage)

### Phase 1
- [x] Ocean plane + material
- [x] 2×2 starter raft with Rigidbody + colliders
- [x] Buoyancy script (spring-damper, multi-point, low CoM for stability)
- [x] Player controller wired to Input System
- [x] Follow camera
- [x] **Milestone:** Walk on bobbing raft in ocean

### Phase 2
- [x] Raft grid data structure
- [x] Building raycast + ghost preview (deck-plane projection)
- [x] Tile placement on Interact
- [x] Tile removal (test key `X`; axe-gated later)
- [x] Buoyancy scales with raft growth (per-cell samples, mass + CoM)
- [x] **Milestone:** Expand raft by placing tiles

### Phase 3
- [x] Resource spawner (`ResourceSpawner` — ring spawn upstream, current drift, cap + despawn)
- [x] Drifting resource movement (`FloatingResource` — current drift + bob, single-winner `TryConsume`)
- [x] Object pooling (`ResourcePool` — per-visual prefab stack, prewarm)
- [x] Kit resource prefabs (`ResourceSpawnEntry` — assign kit visuals; components added at runtime)
- [x] Collection via Interact (`ResourceCollector` — aim-weighted pickup; blocked in build mode)
- [x] Hook on Attack (`ResourceHook` — raycast attach, reel, line visual)
- [x] **Milestone:** Gather floating resources

### Phase 4
- [ ] ItemDefinition + RecipeDefinition ScriptableObjects
- [ ] Inventory system + UI
- [ ] Crafting system + UI
- [ ] Gate building behind material costs
- [ ] **Milestone:** Full gather → craft → build loop

### Phase 5 (optional)
- [ ] Survival stats
- [ ] Crafting stations
- [ ] Threats / AI
- [ ] Save/load
- [ ] Water shader polish

### Phase 6 (Multiplayer)
- [ ] Install Netcode for GameObjects + Unity Transport
- [ ] `NetworkManager` + Host/Join UI (`NetworkBootstrap`)
- [ ] Networked player prefab (one per client, owner-only input)
- [ ] Replicate player + raft movement (moving-platform sync)
- [ ] Convert building to server-authoritative RPCs
- [ ] Convert gathering to server-authoritative RPCs
- [ ] Convert inventory/crafting to server-authoritative + per-player replication
- [ ] Late-joiner state sync + disconnect handling
- [ ] **Milestone:** Two players share one raft — build, gather, craft in sync

---

## Next Steps

When ready to build:

1. Complete Phase 0 setup, including choosing the networking stack and authority model
2. Implement Phase 1 in order: ocean → raft → buoyancy → player → camera — **writing each script multiplayer-aware** (separate input from action)
3. Playtest the Phase 1 milestone before starting Phase 2
4. Once Phases 1–2 feel solid, start **Phase 6.1–6.2** to get two players moving on a shared raft, then convert each later system to server-authoritative as you build it
