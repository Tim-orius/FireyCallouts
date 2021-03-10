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


namespace FireyCallouts.Callouts {

    [CalloutInfo("Burning Truck", CalloutProbability.Low)]

    class BurningTruck : Callout {

        Random mrRandom = new Random();

        private Ped suspect;
        private Vehicle suspectVehicle;
        private Vector3 spawnPoint;
        private Blip locationBlip;
        private string[] truckModels = new string[] { "mule", "pounder", "biff", "mixer", "mixer2", "rubble", "tiptruck",
                                                      "tiptruck2", "trash", "boxville", "benson", "barracks"};
        private bool willExplode = false;

        public override bool OnBeforeCalloutDisplayed() {
            Game.LogTrivial("[FireyCallouts][Log] Initialising 'Burning Truck' callout.");

            spawnPoint = World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.Around(350f));

            ShowCalloutAreaBlipBeforeAccepting(spawnPoint, 30f);
            AddMinimumDistanceCheck(40f, spawnPoint);

            CalloutMessage = "Burning Truck";
            CalloutPosition = spawnPoint;

            // Initialise ped and vehicle
            int decision = mrRandom.Next(0, truckModels.Length);
            suspectVehicle = new Vehicle(truckModels[decision], spawnPoint);
            suspectVehicle.IsPersistent = true;

            suspect = suspectVehicle.CreateRandomDriver();
            suspect.IsPersistent = true;
            suspect.BlockPermanentEvents = true;
            suspect.Tasks.CruiseWithVehicle(15f);

            decision = mrRandom.Next(0, 2);
            if (decision == 1) {
                willExplode = true;
            }

            Functions.PlayScannerAudioUsingPosition("ASSISTANCE_REQUIRED IN_OR_ON_POSITION", spawnPoint);
            Functions.PlayScannerAudio("UNITS_RESPOND_CODE_03");

            return base.OnBeforeCalloutDisplayed();
        }

        public override bool OnCalloutAccepted() {
            Game.LogTrivial("[FireyCallouts][Log] Accepted 'Burning Truck' callout.");

            locationBlip = suspectVehicle.AttachBlip();
            locationBlip.Color = Color.Yellow;
            locationBlip.EnableRoute(Color.Yellow);

            return base.OnCalloutAccepted();
        }

        public override void OnCalloutNotAccepted(){
            Game.LogTrivial("[FireyCallouts][Log] Not accepted 'Burning Truck' callout.");

            if(suspectVehicle.Exists()) suspectVehicle.Delete();
            if(suspect.Exists()) suspect.Delete();
            if(locationBlip.Exists()) locationBlip.Delete();

            base.OnCalloutNotAccepted();
            Game.LogTrivial("[FireyCallouts][Log] Cleaned up 'Burning Truck' callout.");
        }

        public override void Process() {

            base.Process();

            GameFiber.StartNew(delegate {
                if (suspect.Exists() && suspect.DistanceTo(Game.LocalPlayer.Character.GetOffsetPosition(Vector3.RelativeFront)) < 40f) {
                    suspect.KeepTasks = true;

                    // Make the truck burn
                    suspectVehicle.EngineHealth = 0;
                    GameFiber.Wait(5000);
                    suspectVehicle.EngineHealth = 1;
                    GameFiber.Wait(15000);

                    if (willExplode) {
                        suspectVehicle.Explode(true);
                    }
                }

                if (Game.LocalPlayer.Character.IsDead) End();
                if (Game.IsKeyDown(System.Windows.Forms.Keys.Delete)) End();
                if (Functions.IsPedArrested(suspect)) End();
            }, "BurningTruck [FireyCallouts]");
        }

        public override void End() {

            if (suspect.Exists()) { suspect.Dismiss(); }
            if (suspectVehicle.Exists()) { suspectVehicle.Dismiss(); }
            if (locationBlip.Exists()) { locationBlip.Delete(); }

            Functions.PlayScannerAudio("WE_ARE_CODE_FOUR");

            base.End();
            Game.LogTrivial("[FireyCallouts][Log] Cleaned up 'Burning Truck' callout.");
        }
    }
}
