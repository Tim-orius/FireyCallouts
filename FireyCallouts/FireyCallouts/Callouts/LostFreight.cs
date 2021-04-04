using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rage;
using Rage.Native;
using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Mod.Callouts;
using LSPD_First_Response.Engine.Scripting.Entities;
using FireyCallouts.Utilitys;


namespace FireyCallouts.Callouts {

    [CalloutInfo("Lost Freight", CalloutProbability.Low)]

    class LostFreight : Callout {

        private Ped suspect;
        private Vehicle suspectVehicle;
        private Vehicle lostVehicle;
        private Vector3 spawnPoint;
        private Blip suspectBlip;

        public override bool OnBeforeCalloutDisplayed() {
            Game.LogTrivial("[FireyCallouts][Log] Initialising 'Lost Freight' callout.");

            spawnPoint = World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.Around(350f));

            ShowCalloutAreaBlipBeforeAccepting(spawnPoint, 30f);
            AddMinimumDistanceCheck(40f, spawnPoint);

            CalloutMessage = "Lost Freight";
            CalloutPosition = spawnPoint;

            // Initialise vehicle(s) and ped
            suspectVehicle = new Vehicle("FLATBED", spawnPoint);
            suspectVehicle.IsPersistent = true;

            lostVehicle = new Vehicle("Tractor", spawnPoint.Around(10f));
            lostVehicle.IsPersistent = true;

            suspect = suspectVehicle.CreateRandomDriver();
            suspect.IsPersistent = true;
            suspect.BlockPermanentEvents = true;

            Functions.PlayScannerAudioUsingPosition("ASSISTANCE_REQUIRED IN_OR_ON_POSITION", spawnPoint);
            Functions.PlayScannerAudio("UNITS_RESPOND_CODE_03");

            Game.DisplayNotification("web_lossantospolicedept",
                                     "web_lossantospolicedept",
                                     "~y~FireyCallouts",
                                     "~r~Lost freight",
                                     "~w~A tow truck has lost its freight. Respond ~r~Code 3");

            return base.OnBeforeCalloutDisplayed();
        }

        public override bool OnCalloutAccepted() {
            Game.LogTrivial("[FireyCallouts][Log] Accepted 'Lost Freight' callout.");

            // Show route for player
            suspectBlip = suspect.AttachBlip();
            suspectBlip.IsFriendly = true;
            suspectBlip.Color = Color.Yellow;
            suspectBlip.EnableRoute(Color.Yellow);

            Game.DisplayHelp("Press " + Initialization.endKey.ToString() + " to end the callout at any time.");

            return base.OnCalloutAccepted();
        }

        public override void OnCalloutNotAccepted(){
            Game.LogTrivial("[FireyCallouts][Log] Not accepted 'Lost Freight' callout.");

            // Clean up if not accepted
            if (suspect.Exists()) suspect.Delete();
            if (suspectVehicle.Exists()) suspectVehicle.Delete();
            if (lostVehicle.Exists()) lostVehicle.Delete();
            if (suspectBlip.Exists()) suspectBlip.Delete();

            base.OnCalloutNotAccepted();
            Game.LogTrivial("[FireyCallouts][Log] Cleaned up 'Lost Freight' callout.");
        }

        public override void Process() {

            base.Process();

            GameFiber.StartNew(delegate {
                if (suspect.Exists() && suspect.DistanceTo(Game.LocalPlayer.Character.GetOffsetPosition(Vector3.RelativeFront)) < 40f) {
                    suspect.KeepTasks = true;
                    if (suspectBlip.Exists()) suspectBlip.Delete();
                    GameFiber.Wait(2000);
                }

                if (Game.LocalPlayer.Character.IsDead) End();
                if (Game.IsKeyDown(Initialization.endKey)) End();
                if (suspect.Exists()) { if (suspect.IsDead) End(); }
                if (suspect.Exists()) { if (Functions.IsPedArrested(suspect)) End(); }
            }, "LostFreight [FireyCallouts]");
        }

        public override void End() {

            if (suspect.Exists()) { suspect.Dismiss(); }
            if (suspectVehicle.Exists()) { suspectVehicle.Dismiss(); }
            if (lostVehicle.Exists()) { lostVehicle.Dismiss(); }
            if (suspectBlip.Exists()) { suspectBlip.Delete(); }

            Functions.PlayScannerAudio("WE_ARE_CODE_4");

            base.End();
            Game.LogTrivial("[FireyCallouts][Log] Cleaned up 'Lost Freight' callout.");
        }
    }
}
