using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rage;
using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Mod.Callouts;
using LSPD_First_Response.Engine.Scripting.Entities;


namespace FireyCallouts.Callouts {

    [CalloutInfo("Helicopter Crash", CalloutProbability.VeryHigh)]

    class HeliCrash : Callout {

        Random mrRandom = new Random();

        private Ped suspect;
        private Vehicle suspectVehicle;
        private Vector3 spawnPoint;
        private Vector3 area;
        private Blip locationBlip;
        private string[] helicopterModels = new string[] {"frogger", "frogger2", "maverick", "buzzard2"};

        public override bool OnBeforeCalloutDisplayed() {
            Game.LogTrivial("[FireyCallouts][Log] Initialising 'Helicopter Crash' callout.");

            spawnPoint = World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.Around(350f));

            ShowCalloutAreaBlipBeforeAccepting(spawnPoint, 30f);
            AddMinimumDistanceCheck(40f, spawnPoint);

            CalloutMessage = "Helicopter Crash";
            CalloutPosition = spawnPoint;

            int decision = mrRandom.Next(0, helicopterModels.Length - 1);
            spawnPoint.Z = 30f;
            suspectVehicle = new Vehicle(helicopterModels[decision], spawnPoint);
            suspectVehicle.IsPersistent = true;

            suspect = suspectVehicle.CreateRandomDriver();
            suspect.IsPersistent = true;
            suspect.BlockPermanentEvents = true;

            Functions.PlayScannerAudioUsingPosition("ASSISTANCE_REQUIRED IN_OR_ON_POSITION", spawnPoint);

            return base.OnBeforeCalloutDisplayed();
        }

        public override bool OnCalloutAccepted() {
            Game.LogTrivial("[FireyCallouts][Log] Accepted 'Helicopter Crash' callout.");

            suspectVehicle.Explode(true);

            area = spawnPoint.Around2D(1f, 2f);
            locationBlip = new Blip(area, 40f);
            locationBlip.Color = Color.Yellow;
            locationBlip.EnableRoute(Color.Yellow);

            return base.OnCalloutAccepted();
        }

        public override void OnCalloutNotAccepted(){
            Game.LogTrivial("[FireyCallouts][Log] Not accepted 'Helicopter Crash' callout.");

            if(suspectVehicle.Exists()) suspectVehicle.Delete();
            if(suspect.Exists()) suspect.Delete();
            if(locationBlip.Exists()) locationBlip.Delete();

            base.OnCalloutNotAccepted();
            Game.LogTrivial("[FireyCallouts][Log] Cleaned up 'Helicopter Crash' callout.");
        }

        public override void Process() {

            base.Process();

            GameFiber.StartNew(delegate {
                /*
                if (suspect.DistanceTo(Game.LocalPlayer.Character.GetOffsetPosition(Vector3.RelativeFront)) < 40f) {
                    if (suspectBlip.Exists()) suspectBlip.Delete();
                }
                */

                if (suspect.Exists() && suspect.DistanceTo(Game.LocalPlayer.Character.GetOffsetPosition(Vector3.RelativeFront)) < 40f) {
                    suspect.KeepTasks = true;
                    GameFiber.Wait(2000);
                }
                if (Game.LocalPlayer.Character.IsDead) End();
                if (Game.IsKeyDown(System.Windows.Forms.Keys.Delete)) End();
                if (Functions.IsPedArrested(suspect)) End();
            }, "HeliCrash [FireyCallouts]");
        }

        public override void End() {

            if (suspect.Exists()) { suspect.Dismiss(); }
            if (suspectVehicle.Exists()) { suspectVehicle.Dismiss(); }
            if (locationBlip.Exists()) { locationBlip.Delete(); }

            Functions.PlayScannerAudio("WE_ARE_CODE FOUR");

            base.End();
            Game.LogTrivial("[FireyCallouts][Log] Cleaned up 'Helicopter Crash' callout.");
        }
    }
}
