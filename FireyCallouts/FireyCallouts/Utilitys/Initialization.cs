using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Rage;

namespace FireyCallouts.Utilitys {
    internal static class Initialization {

        // Development callouts / tools
        internal static bool develop = false;
        internal static bool planeTesting = false;

        // Callouts
        internal static bool burningTruck = true;
        internal static bool burningGarbage = true;
        internal static bool illegalFirework = true;
        internal static bool lostFreight = true;
        internal static bool heliCrash = true;
        internal static bool planeLanding = true;
        internal static bool structuralFire = true;
        internal static bool campfire = true;
        internal static bool smokeDetected = false;

        // Controls & other settings
        internal static Keys endKey = Keys.Delete;
        internal static Keys dialogueKey = Keys.Y;
        internal static double maxCalloutDistance = 800f;

        internal static void Initalize() {
            string pathToFile = "Plugins/LSPDFR/FireyCallouts.ini";
            var ini = new InitializationFile(pathToFile);
            ini.Create();

            // Controls
            endKey = ini.ReadEnum("Controls", "endKey", Keys.Delete);
            dialogueKey = ini.ReadEnum("Controls", "dialogueKey", Keys.Y);

            // Settings
            maxCalloutDistance = ini.ReadDouble("Settings", "minCalloutDist", 800f);

            // Callouts
            burningTruck = ini.ReadBoolean("Callouts", "burningTruck", true);
            burningGarbage = ini.ReadBoolean("Callouts", "burningGarbage", true);
            lostFreight = ini.ReadBoolean("Callouts", "lostFreight", true);
            heliCrash = ini.ReadBoolean("Callouts", "heliCrash", true);
            planeLanding = ini.ReadBoolean("Callouts", "planeLanding", true);
            illegalFirework = ini.ReadBoolean("Callouts", "illegalFirework", true);
            structuralFire = ini.ReadBoolean("Callouts", "structuralFire", true);
            campfire = ini.ReadBoolean("Callouts", "campfire", true);
            //smokeDetected = ini.ReadBoolean("Callouts", "smokeDetected", false); 

            Game.LogTrivial("[FireyCallouts][Init] successfully initialized");

        }
    }
}
