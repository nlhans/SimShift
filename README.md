SimShift
========

Driver utilities for various open roaming simulators like Euro Truck Simulator 2 and Test Drive Unlimited.

## Windows Installation

### Place SimShift folder in ETS2 Main Folder
- `steam\steamapps\common\Euro Truck Simulator 2\`

### Revert to ETS2 version 1.19
- **[Guide](https://forum.truckersmp.com/index.php?/topic/17-how-to-downgrade-ets2ats-to-supported-version/)**
- Revert to version 1.19x **FIRST** before doing any of the following instructions

### Extract base.scs and def.scs 
**Highly reccomended:** Use [SCS EXTRACTOR GUI](https://github.com/Bluscream/SCS-Extractor-GUI/releases)
1. Open SCS Extractor GUI
2. Navigate to ETS2 folder `steam\steamapps\common\Euro Truck Simulator 2\`
3. Extract base.scs (GUI will place in `\Euro Truck Simulator 2\base`)
    - THIS WILL TAKE A WHILE, THE COMMAND PROMPT WILL CLOSE BY ITSELF, DO NOT CLOSE EARLY
4. Extract def.scs (GUI will place in `\Euro Truck Simulator 2\def`)

### Install SDK Plugin for ETS2
1. Get [SDK Plugin](https://github.com/nlhans/ets2-sdk-plugin/releases)
2. Place the acquired DLL inside bin/win_x86/plugins/ of your ETS2 installation. 

### Install VJoy
- http://vjoystick.sourceforge.net/site/index.php/download-a-install/download

### Add Reference Paths
1. Open Visual Studio
2. Open the project
3. Click `Project` then `SimShift Properties...` inside of Visual Studio
4. Go to the `References Paths` tab and add these paths
    - `steam\steamapps\common\Euro Truck Simulator 2\Simshift\Binaries\`
    - `steam\steamapps\common\Euro Truck Simulator 2\Simshift\Resources\`

### Add settings path
1. Run project
2. Get an error
3. Create folder `Settings\Drivetrain\` under the folder `steam\steamapps\common\Euro Truck Simulator 2\Simshift\Simshift\Simshift\bin\Debug`
