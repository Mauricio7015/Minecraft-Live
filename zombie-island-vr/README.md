# 🧟 Zombie Island VR

**Jogo de sobrevivência zombie em VR para Meta Quest 3S**

Motor: Unity 6 (URP) + Meta XR SDK + OpenXR

---

## 🚀 Demo Executável (Agora)

Abra `demo/index.html` no navegador para jogar a demo 2D de teste:

- **WASD / Setas** — Mover
- **Mouse** — Olhar (clique para capturar)
- **Click esquerdo** — Atirar
- **R** — Recarregar
- **F** — Pegar item
- **Shift** — Correr (consome stamina)

> A demo testa as mecânicas de: HP, Fome, Sede, Stamina, IA de zumbis (chase/wander), sistema de munição, recolha de itens e ciclo dia/noite.

---

## 🗂️ Estrutura do Projeto Unity

```
zombie-island-vr/
├── Assets/
│   ├── _Project/
│   │   ├── Scripts/
│   │   │   ├── Player/         PlayerStats, PlayerController, PlayerInventory, PlayerHands
│   │   │   ├── Weapons/        WeaponBase, FirearmWeapon, MeleeWeapon, WeaponPickup
│   │   │   ├── Zombie/         ZombieAI, ZombieHealth, ZombieAnimator, ZombieSpawner
│   │   │   ├── World/          DayNightCycle, RadioactiveWater, InteractableObject, WorldGenerator
│   │   │   ├── Systems/        SaveSystem, FoodSystem
│   │   │   └── UI/             HUDController
│   │   └── Scenes/
│   │       ├── Bootstrap.unity
│   │       └── ZombieIsland.unity
│   ├── FreeAssets/             Assets gratuitos (ver seção Assets)
│   └── Prefabs/
├── Packages/
│   └── manifest.json           Todos os pacotes Unity configurados
├── ProjectSettings/
│   ├── ProjectVersion.txt      Unity 6000.0.23f1
│   └── ProjectSettings.asset   Android ARM64, OpenXR, URP
├── demo/
│   └── index.html              Demo jogável no browser
└── README.md
```

---

## ⚙️ Setup no Unity 6

### Pré-requisitos

- [Unity Hub](https://unity.com/download)
- Unity 6000.0.x LTS instalado com módulo **Android Build Support** (NDK/SDK incluídos)
- [Meta Quest Developer Hub (MQDH)](https://developer.oculus.com/meta-quest-developer-hub/)
- Meta Quest 3S em **Developer Mode** (ativar no app Meta Horizon no celular)

### Passo 1 — Abrir o Projeto

```bash
# Clone o repositório
git clone <url-do-repo>
cd zombie-island-vr

# Abrir pelo Unity Hub:
# Add → selecionar pasta zombie-island-vr → Unity 6000.0.x
```

### Passo 2 — Importar Meta XR SDK

No Unity Editor:

1. **Window → Package Manager → + → Add package by name:**
   ```
   com.meta.xr.sdk.all
   ```
   Versão: `65.0.0` ou mais recente

2. Aceitar todas as dependências

### Passo 3 — Configurar XR Management

1. **Edit → Project Settings → XR Plug-in Management**
2. Aba **Android** → habilitar **OpenXR**
3. Clicar em **OpenXR** → **Feature Groups** → habilitar **Meta Quest Support**
4. Habilitar: **Render Mode: Multi-View**, **Symmetric Projection**

### Passo 4 — Importar Assets Gratuitos

#### Mixamo (personagens e animações de zumbi)
1. Acessar [mixamo.com](https://mixamo.com) (conta Adobe gratuita)
2. Procurar personagem "Zombie" → Download FBX for Unity
3. Para animações: buscar `zombie walk`, `zombie attack`, `zombie death`
4. Importar em `Assets/FreeAssets/Characters/`
5. Inspector → Rig: **Humanoid** → Avatar: **Create from Model**

#### Kenney.nl (itens e ambiente — CC0)
```
kenney.nl/assets/survival-kit         → itens de sobrevivência
kenney.nl/assets/city-kit-suburban    → casas e cercas
```
Extrair em `Assets/FreeAssets/Kenney/`

#### Quaternius (modelos zumbi low-poly — CC0)
```
quaternius.com → "Zombie Pack" e "Tropical Environment"
```
Extrair em `Assets/FreeAssets/Quaternius/`

#### Sons (Freesound.org — CC0)
Buscar e baixar:
- `zombie groan` → gemidos
- `gunshot 9mm` → tiro de pistola
- `heartbeat fast` → HP crítico
- `footstep sand` / `footstep grass`
- `door creak`, `glass break`

Colocar em `Assets/FreeAssets/Audio/`

---

## 🏗️ Build para Meta Quest 3S

### Configurações de Build

```
File → Build Settings
Platform:         Android
Architecture:     ARM64
Texture Compress: ASTC
```

### PlayerSettings (já configurado no ProjectSettings.asset)

```
Company:          ZombieIslandStudios
Product:          ZombieIslandVR
Bundle ID:        com.zombieislandstudios.zombieislandvr
Min SDK:          API 29 (Android 10)
Target SDK:       API 33 (Android 13)
Color Space:      Linear
Graphics API:     OpenGLES3 / Vulkan
Stereo Mode:      Multi-View
```

### Gerar APK

```bash
# No Unity: File → Build → ZombieIsland.apk

# Instalar via ADB (Quest em Developer Mode, conectado por USB):
adb install -r ZombieIsland.apk
```

Ou use o **Meta Quest Developer Hub** (interface gráfica, mais simples).

---

## 🎮 Mecânicas Implementadas

### Player (`PlayerStats.cs`)
- **HP** máx 100 — regenera quando bem alimentado
- **Stamina** — drena ao correr/atacar, regenera em repouso
- **Fome** — drena 1 unit/min; abaixo de 10 → dano contínuo
- **Sede** — drena 1.5 unit/min; abaixo de 10 → dano contínuo
- HP só regenera com fome > 40 e sede > 40

### Armas
| Arma | Dano | Mecânica VR |
|------|------|-------------|
| Pistola 9mm | 25 | Recarga física: tirar/inserir carregador |
| Espingarda | 80 | Pump-action física |
| Rifle AR | 35 | Automático |
| Faca | 30 | Velocidade de swing determina dano |
| Machado | 60 | Dano escalado por velocidade |
| Barra de ferro | 50 | Indestrutível |

### Zumbis
| Tipo | Velocidade | HP | Especial |
|------|----------|-----|---------|
| Comum | 1.2 m/s | 100 | — |
| Corredor | 5.5 m/s | 50 | Rápido |
| Bruto | 2 m/s | 300 | Alto dano |
| Noturno | 4 m/s | 100 | Invisível de dia > 3m |

### Ciclo Dia/Noite
- 20 minutos por ciclo completo
- Horda de 20–25 zumbis a cada 8 min (noite aumenta spawn)
- Caixa de suprimentos a cada 2h de jogo

### Água Radioativa
- Borda da ilha emite som Geiger ao se aproximar
- 15 HP/s de dano ao entrar
- Zumbis se dissolvem na água

---

## 📦 Scripts — Referência Rápida

| Script | Função |
|--------|--------|
| `PlayerStats.cs` | Hub de todos os stats vitais |
| `PlayerController.cs` | Locomoção VR (smooth + snap turn) |
| `PlayerInventory.cs` | Mochila 20 slots + cinto 4 slots |
| `PlayerHands.cs` | Pegar/soltar objetos, tracking velocidade |
| `WeaponBase.cs` | Base abstrata para todas as armas |
| `FirearmWeapon.cs` | Armas de fogo com recarga física |
| `MeleeWeapon.cs` | Corpo-a-corpo com física de velocidade |
| `ZombieAI.cs` | State machine: Idle/Wander/Chase/Attack |
| `ZombieHealth.cs` | HP, ragdoll, desmembramento |
| `ZombieSpawner.cs` | Pool de zumbis, horde events |
| `DayNightCycle.cs` | Ciclo solar, eventos de dawn/dusk/night |
| `RadioactiveWater.cs` | Dano e efeitos da borda radioativa |
| `InteractableObject.cs` | Base para portas, gavetas, fogueiras |
| `WorldGenerator.cs` | Geração de terreno com Perlin noise |
| `SaveSystem.cs` | Save/load JSON + auto-save 5min |
| `FoodSystem.cs` | Sistema de consumo de alimentos |
| `HUDController.cs` | HUD VR no pulso (world space canvas) |

---

## 🔧 Otimização Quest 3S

- Max 30 zumbis ativos simultâneos (object pool)
- LOD: 3 níveis por zumbi (10m / 25m / 50m)
- Texturas: ASTC 6x6, máximo 1024×1024
- Shadow distance: 30m
- Fixed Foveated Rendering: High
- Target: 72fps (mínimo) / 90fps (ideal)
- Draw calls: < 100 por frame

---

## 📋 Próximos Passos

- [ ] Importar assets do Mixamo e configurar Animator Controller
- [ ] Criar prefabs de zumbi com NavMeshAgent e ragdoll
- [ ] Configurar XR Rig com Meta XR SDK (mãos + câmera)
- [ ] Criar NavMesh no terreno da ilha
- [ ] Implementar sistema de crafting
- [ ] Adicionar áudio espacial 3D
- [ ] Configurar Post-Processing (bloom, vignette, color grading)
- [ ] Build e teste no Quest 3S

---

*Projeto criado com Claude Code · Stack: Unity 6 · URP · Meta XR SDK · OpenXR*
