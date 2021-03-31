using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Rage;

namespace FireyCallouts.Utilitys {
    internal static class Initialization {

        // Development
        internal static bool develop = false;

        // Callouts
        internal static bool burningTruck = true;
        internal static bool burningGarbage = true;
        internal static bool illegalFirework = true;
        internal static bool lostFreight = true;
        internal static bool heliCrash = true;
        internal static bool planeLanding = true;
        internal static bool structuralFire = true;

        // in development
        internal static bool planeTesting = false;

        // Controls & other settings
        internal static Keys endKey = Keys.Delete;

        internal static void Initalize() {
            string pathToFile = "Plugins/LSPDFR/FireyCallouts.ini";
            var ini = new InitializationFile(pathToFile);
            ini.Create();

            // Controls
            Utils.gamemode = ini.ReadEnum<Utils.Gamemodes>("Controls", "gamemode", Utils.Gamemodes.Pol);
            endKey = ini.ReadEnum("Controls", "endKey", Keys.Delete);

            // Callouts
            // Not specific
            burningTruck = ini.ReadBoolean("Callouts", "burningTruck", true);
            burningGarbage = ini.ReadBoolean("Callouts", "burningGarbage", true);
            lostFreight = ini.ReadBoolean("Callouts", "lostFreight", true);
            heliCrash = ini.ReadBoolean("Callouts", "heliCrash", true);
            planeLanding = ini.ReadBoolean("Callouts", "planeLanding", true);
            structuralFire = ini.ReadBoolean("Callouts", "structuralFire", true);

            // Pol specific
            illegalFirework = ini.ReadBoolean("Callouts", "illegalFirework", true);

            // Fire specific
            // (None)

            // Development
            develop = ini.ReadBoolean("Development", "develop", false);

            Game.LogTrivial("[FireyCallouts][Init] successfully initialized");

        }
    }
}
