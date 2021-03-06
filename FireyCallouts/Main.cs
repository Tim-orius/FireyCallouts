using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using LSPD_First_Response.Mod.API;
using Rage;
using FireyCallouts.Callouts;

namespace FireyCallouts{
    public class Main : Plugin {

        public override void Initialize() {
            Functions.OnOnDutyStateChanged += OnOnDutyStateChangedHandler;
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

                Game.DisplayNotification(
                        "web_lossantospolicedept", 
                        "web_lossantospolicedept", 
                        "FireryCallouts", // Title
                        "~y~v" + Assembly.GetExecutingAssembly().GetName().Version.ToString() + 
                        " ~o~by Timorius", "~b~loaded successfully."); // Subtitle
            }
        }

        private static void RegisterCallouts() {
            Functions.RegisterCallout(typeof(Callouts.LostFreight));
        }

    }
}
