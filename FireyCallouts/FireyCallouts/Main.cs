using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using LSPD_First_Response.Mod.API;
using Rage;
using FireyCallouts.Callouts;
using FireyCallouts.Utilitys;

namespace FireyCallouts {
    public class Main : Plugin {

        public override void Initialize() {
            Functions.OnOnDutyStateChanged += OnOnDutyStateChangedHandler;
            Initialization.Initalize();
            Game.LogTrivial("Plugin FireyCallouts " +
                System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString() + " has been initialised.");
            Game.LogTrivial("Go on duty to fully load FireyCallouts.");
        }

        public override void Finally() {
            Game.LogTrivial("FireyCallouts has been cleaned up.");
        }

        private static void OnOnDutyStateChangedHandler(bool OnDuty) {
            if (OnDuty) {
                RegisterCallouts();

                Game.Console.Print();
                Game.Console.Print("------------ FireyCallouts ------------");
                Game.Console.Print("[LOG]: Callouts loaded succesfully.");
                Game.Console.Print("[VERSION]: " + Assembly.GetExecutingAssembly().GetName().Version.ToString());
                Game.Console.Print("------------ FireyCallouts ------------");
                Game.Console.Print();

                Game.DisplayNotification(
                        "web_lossantospolicedept",
                        "web_lossantospolicedept",
                        "FireyCallouts",
                        "~y~v" + Assembly.GetExecutingAssembly().GetName().Version.ToString() +
                        " ~o~by Timorius", "~b~loaded successfully.");
            }
        }

        private static void RegisterCallouts() {

            Game.LogTrivial("Register FireyCallouts callouts");

            if (Initialization.develop) {
                Game.LogTrivial("[FireyCallouts] Developer mode enabled. Register all callouts.");
                // Developer mode
                Functions.RegisterCallout(typeof(Callouts.DumpsterFire));
                Functions.RegisterCallout(typeof(Callouts.BurningTruck));
                Functions.RegisterCallout(typeof(Callouts.Campfire));
                Functions.RegisterCallout(typeof(Callouts.IllegalFirework));
                Functions.RegisterCallout(typeof(Callouts.HeliCrash));
                Functions.RegisterCallout(typeof(Callouts.LostFreight));
                Functions.RegisterCallout(typeof(Callouts.PlaneLanding));
                Functions.RegisterCallout(typeof(Callouts.PlaneTesting));
                Functions.RegisterCallout(typeof(Callouts.SmokeDetected));
                Functions.RegisterCallout(typeof(Callouts.StructuralFire));

            } else {

                if (Initialization.burningGarbage) { Functions.RegisterCallout(typeof(Callouts.DumpsterFire)); }
                if (Initialization.burningTruck) { Functions.RegisterCallout(typeof(Callouts.BurningTruck)); }
                if (Initialization.campfire) { Functions.RegisterCallout(typeof(Callouts.Campfire)); }
                if (Initialization.heliCrash) { Functions.RegisterCallout(typeof(Callouts.HeliCrash)); }
                if (Initialization.illegalFirework) { Functions.RegisterCallout(typeof(Callouts.IllegalFirework)); }
                if (Initialization.lostFreight) { Functions.RegisterCallout(typeof(Callouts.LostFreight)); }
                if (Initialization.planeLanding) { Functions.RegisterCallout(typeof(Callouts.PlaneLanding)); }
                if (Initialization.smokeDetected) { Functions.RegisterCallout(typeof(Callouts.SmokeDetected)); }
                if (Initialization.structuralFire) { Functions.RegisterCallout(typeof(Callouts.StructuralFire)); }
            }
            
        }

    }
}
