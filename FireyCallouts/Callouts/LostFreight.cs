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

    [CalloutInfo("Lost Freight", CalloutProbability.VeryHigh)]

    class LostFreight : Callout {

        private Ped suspect;
        private Vehicle suspectVehicle;
        private Vehicle lostVehicle;
        private Vector3 spawnPoint;
        private Blip suspectBlip;

        public override bool OnBeforeCalloutDisplayed() {
            spawnPoint = World.GetNextPositionOnStreet(Game.LocalPlayer.Character.Position.Around(350f));

            ShowCalloutAreaBlipBeforeAccepting(spawnPoint, 30f);
            AddMinimumDistanceCheck(40f, spawnPoint);

            CalloutMessage = "Lost Freight";
            CalloutPosition = spawnPoint;

            Functions.PlayScannerAudioUsingPosition("WE_HA#VE CRIME_GRAND_THEFT_AUTO IN_OR_ON_POSITION", spawnPoint);

            return base.OnBeforeCalloutDisplayed();
        }

        public override bool OnCalloutAccepted() {
            suspectVehicle = new Vehicle("FLATBED", spawnPoint);
            suspectVehicle.IsPersistent = true;

            lostVehicle = new Vehicle("Tractor", spawnPoint.Around(10f));
            lostVehicle.IsPersistent = true;

            suspect = suspectVehicle.CreateRandomDriver();
            suspect.IsPersistent = true;
            suspect.BlockPermanentEvents = true;

            suspectBlip = suspect.AttachBlip();
            suspectBlip.IsFriendly = true;
            suspectBlip.EnableRoute(Color.Blue);

            return base.OnCalloutAccepted();
        }

        public override void OnCalloutNotAccepted(){
            if(suspectVehicle.Exists()) suspectVehicle.Delete();
            if(lostVehicle.Exists()) lostVehicle.Delete();
            if(suspect.Exists()) suspect.Delete();
            if(suspectBlip.Exists()) suspectBlip.Delete();

            base.OnCalloutNotAccepted();
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
                if (suspect.IsDead) End();
                if (Functions.IsPedArrested(suspect)) End();
            }, "LostFreight [FireyCallouts]");
        }

        public override void End() {

            base.End();

            if (suspect.Exists()) { suspect.Dismiss(); }
            if (suspectVehicle.Exists()) { suspectVehicle.Dismiss(); }
            if (lostVehicle.Exists()) { lostVehicle.Dismiss(); }
            if (suspectBlip.Exists()) { suspectBlip.Delete(); }

            Functions.PlayScannerAudio("WE_ARE_CODE FOUR");
        }
    }
}
