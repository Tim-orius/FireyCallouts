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

    [CalloutInfo("Plane Landing", CalloutProbability.Low)]

    class PlaneLanding : Callout {

        private Random mrRandom = new Random();

        private Ped suspect;
        private Vehicle suspectVehicle;
        private Vector3 spawnPoint;
        private Vector3 landPoint;
        private Vector3 lastRoutePoint;
        private Blip suspectBlip;
        private List<Vector3> landLocations = new List<Vector3>() { new Vector3(1772.492f, 2088.625f, 66.50783f), // Near prison
                                                                    new Vector3(77.06483f, -1230.351f, 37.81571f)}; // Highway-bridge right south of simeons

        private List<Vector3> spawnLocations = new List<Vector3>() { new Vector3(1891.882f, 3130.54f, 108.6497f), // Straight line north of landing point
                                                                     new Vector3(-632.2282f, -1246.58f, 82.55003f)}; // Straight line west of landing point

        private List<Vector3[]> flyRoutes = new List<Vector3[]>();
        private float flyspeed = 10f;

        private string[] planeModels = new string[] { "velum", "velum2", "vestra", "dodo", "duster", "mammatus" };
        private bool willCrash = false;
        private int landing_situation = 0; // 0 - flying; 1 - lannding; 2 - landed

        private List<Vehicle> emergencyVehicles = new List<Vehicle>();
        private List<Ped[]> emergencyPeds = new List<Ped[]>();

        public override bool OnBeforeCalloutDisplayed() {
            Game.LogTrivial("[FireyCallouts][Log] Initialising 'Plane Landing' callout.");

            // Check distance of player to landing locations
            /*
            List<Vector3> possibleLocations = new List<Vector3>();
            foreach (Vector3 l in landLocations) {
                if (l.DistanceTo(Game.LocalPlayer.Character.GetOffsetPosition(Vector3.RelativeFront)) < 800f) {
                    possibleLocations.Add(l);
                }
            }

            if (possibleLocations.Count < 1) {
                OnCalloutNotAccepted();
                return false;
            }
            */

            int loc_dec = 0;// mrRandom.Next(0, landLocations.Count);
            int decision;

            landPoint = landLocations[loc_dec];
            spawnPoint = spawnLocations[loc_dec];
            lastRoutePoint = flyRoutes[loc_dec][flyRoutes[loc_dec].Length];

            ShowCalloutAreaBlipBeforeAccepting(spawnPoint, 30f);
            AddMinimumDistanceCheck(40f, spawnPoint);

            CalloutMessage = "Plane Landing";
            CalloutPosition = spawnPoint;

            // Initialise vehicle
            decision = mrRandom.Next(0, planeModels.Length);
            suspectVehicle = new Vehicle(planeModels[decision], spawnPoint);
            suspectVehicle.IsPersistent = true;

            // Initialise ped
            suspect = suspectVehicle.CreateRandomDriver();
            suspect.IsPersistent = true;
            suspect.BlockPermanentEvents = true;
            //suspect.Tasks.CruiseWithVehicle(suspectVehicle, 1, Rage.VehicleDrivingFlags.IgnorePathFinding);

            // Get the corresponding fly route
            Vector3[] route = flyRoutes[loc_dec];
            suspect.Tasks.FollowPointRoute(route, flyspeed);

            decision = mrRandom.Next(0, 2);
            if (decision == 1) {
                willCrash = true;
            }

            Functions.PlayScannerAudioUsingPosition("ASSISTANCE_REQUIRED IN_OR_ON_POSITION", spawnPoint);
            Functions.PlayScannerAudio("UNITS_RESPOND_CODE_03");

            return base.OnBeforeCalloutDisplayed();
        }

        public override bool OnCalloutAccepted() {
            Game.LogTrivial("[FireyCallouts][Log] Accepted 'Plane Landing' callout.");

            // Show route for player
            suspectBlip = suspectVehicle.AttachBlip();
            suspectBlip.Color = Color.Yellow;
            suspectBlip.EnableRoute(Color.Yellow);

            return base.OnCalloutAccepted();
        }

        public override void OnCalloutNotAccepted() {
            Game.LogTrivial("[FireyCallouts][Log] Not accepted 'Plane Landing' callout.");

            // Clean up if not accepted
            if (suspect.Exists()) suspect.Delete();
            if (suspectVehicle.Exists()) suspectVehicle.Delete();
            if (suspectBlip.Exists()) suspectBlip.Delete();

            foreach (Vehicle ev in emergencyVehicles) {
                if (ev.Exists()) { ev.Delete(); }
            }
            foreach (Ped[] epl in emergencyPeds) {
                if (epl.Length > 0) {
                    foreach (Ped ep in epl) {
                        if (ep.Exists()) { ep.Delete(); }
                    }
                }
            }

            base.OnCalloutNotAccepted();
            Game.LogTrivial("[FireyCallouts][Log] Cleaned up 'Plane Landing' callout.");
        }

        public override void Process() {

            base.Process();

            GameFiber.StartNew(delegate {
                if (suspect.Exists() && suspect.DistanceTo(Game.LocalPlayer.Character.GetOffsetPosition(Vector3.RelativeFront)) < 40f) {
                    suspect.KeepTasks = true;
                    //if (suspectBlip.Exists()) suspectBlip.Delete();
                    GameFiber.Wait(2000);
                }

                if (suspectVehicle.Exists() && suspect.Exists() && suspectVehicle.DistanceTo2D(lastRoutePoint) < 5f && (landing_situation == 0)) {
                    GameFiber.Wait(4000);
                    // Start landing
                    suspect.Tasks.LandPlane(lastRoutePoint, landPoint);
                    landing_situation = 1;
                    GameFiber.Wait(2000);
                }

                if (suspectVehicle.Exists() && suspect.Exists() && suspectVehicle.DistanceTo2D(landPoint) < 5f && (landing_situation == 1)) {
                    // Explode if the plane is dedicated to crash
                    if(willCrash) {
                        suspect.Tasks.CruiseWithVehicle(10f);
                        suspectVehicle.Explode();
                    }
                    landing_situation = 2;
                    GameFiber.Wait(3000);
                }

                if (Game.LocalPlayer.Character.IsDead) End();
                if (Game.IsKeyDown(System.Windows.Forms.Keys.Delete)) End();
                if (suspect.Exists()) { if (suspect.IsDead) End(); }
                if (suspect.Exists()) { if (Functions.IsPedArrested(suspect)) End(); }
            }, "PlaneLanding [FireyCallouts]");
        }

        public override void End() {

            if (suspect.Exists()) { suspect.Dismiss(); }
            if (suspectVehicle.Exists()) { suspectVehicle.Dismiss(); }
            if (suspectBlip.Exists()) { suspectBlip.Delete(); }

            foreach (Vehicle ev in emergencyVehicles) {
                if (ev.Exists()) { ev.Dismiss(); }
            }
            foreach (Ped[] epl in emergencyPeds) {
                if (epl.Length > 0) {
                    foreach (Ped ep in epl) {
                        if (ep.Exists()) { ep.Dismiss(); }
                    }
                }
            }

            Functions.PlayScannerAudio("WE_ARE_CODE_4");

            base.End();
            Game.LogTrivial("[FireyCallouts][Log] Cleaned up 'Plane Landing' callout.");
        }
    }
}
