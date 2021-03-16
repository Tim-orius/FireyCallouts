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

        private List<Vector3[]> flyRoutes = new List<Vector3[]>() { new Vector3[] { new Vector3(1831.5f, 2609.00f, 87.0f) },
                                                                    new Vector3[] { new Vector3(-277.5f, -1238.0f, 59.5f) } };
        private readonly float flySpeed = 80f;
        private bool positionFrozen = false;
        private bool invincible = false;
        private bool driverThere = false;
        private int seat = 1;

        private string[] planeModels = new string[] { "velum", "velum2", "vestra", "dodo", "duster", "mammatus" };
        private bool willCrash = false;
        private int landingSituation = 3; // 0 - flying; 1 - lannding; 2 - landed

        private List<Vehicle> emergencyVehicles = new List<Vehicle>();
        private List<Ped[]> emergencyPeds = new List<Ped[]>();
        Vector3[] route = new Vector3[] { };

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

            int locationDecision = 0; // mrRandom.Next(0, landLocations.Count);
            int decision;

            landPoint = landLocations[locationDecision];
            spawnPoint = spawnLocations[locationDecision];
            spawnPoint.Z += 100f;
            lastRoutePoint = flyRoutes[0][0]; // flyRoutes[locationDecision][flyRoutes[locationDecision].Length];
            route = flyRoutes[locationDecision];

            Game.LogTrivial("[FireyCallouts][Debug] Locations set.");

            ShowCalloutAreaBlipBeforeAccepting(spawnPoint, 30f);
            AddMinimumDistanceCheck(40f, spawnPoint);

            CalloutMessage = "Plane doing emergency landing";
            CalloutPosition = spawnPoint;

            // Initialise vehicle
            decision = mrRandom.Next(0, planeModels.Length);
            suspectVehicle = new Vehicle(planeModels[decision], spawnPoint);
            suspectVehicle.IsPositionFrozen = true;
            positionFrozen = true;
            suspectVehicle.IsPersistent = true;
            suspectVehicle.Face(landPoint);
            suspectVehicle.IsEngineOn = true;
            suspectVehicle.IsInvincible = true;

            // Initialise ped
            suspect = suspectVehicle.CreateRandomDriver();
            suspect.IsPersistent = true;
            suspect.BlockPermanentEvents = true;
            suspect.WillGetOutOfUpsideDownVehiclesAutomatically = false;
            suspect.StaysInVehiclesWhenJacked = true;
            suspect.IsInvincible = true;
            invincible = true;

            // Warp pilot back into the plane if he jumps out (he is not very cooperative)
            if (!suspect.IsInVehicle(suspectVehicle, false)) {
                suspect.WarpIntoVehicle(suspectVehicle, seat);
                // Will hopefully set the flight speed
                suspect.Tasks.CruiseWithVehicle(suspectVehicle, flySpeed, VehicleDrivingFlags.IgnorePathFinding);
                // Set to landing
                //suspect.Tasks.LandPlane(suspectVehicle, spawnPoint, landPoint);
                Game.LogTrivial("[FireyCallouts][Debug]<Init> Pilot warped into vehicle.");
            }

            suspect.Tasks.PerformDrivingManeuver(VehicleManeuver.GoForwardStraight);

            Game.LogTrivial("[FireyCallouts][Debug] Vehicle & pilot set.");

            decision = 0;// mrRandom.Next(0, 2);
            if (decision == 1) {
                willCrash = true;
            }

            Functions.PlayScannerAudioUsingPosition("ASSISTANCE_REQUIRED IN_OR_ON_POSITION", spawnPoint);
            Functions.PlayScannerAudio("UNITS_RESPOND_CODE_03");

            // Warp pilot back into the plane if he jumps out
            if (!suspect.IsInVehicle(suspectVehicle, false)) {
                suspect.WarpIntoVehicle(suspectVehicle, seat);
                suspect.Tasks.CruiseWithVehicle(suspectVehicle, flySpeed, VehicleDrivingFlags.IgnorePathFinding);
                //suspect.Tasks.LandPlane(suspectVehicle, spawnPoint, landPoint);
                Game.LogTrivial("[FireyCallouts][Debug]<Init2> Pilot warped into vehicle.");
            }

            return base.OnBeforeCalloutDisplayed();
        }

        public override bool OnCalloutAccepted() {
            Game.LogTrivial("[FireyCallouts][Log] Accepted 'Plane Landing' callout.");

            // Warp pilot back into the plane if he jumps out
            if (!suspect.IsInVehicle(suspectVehicle, false)) {
                suspect.WarpIntoVehicle(suspectVehicle, seat);
                //suspect.KeepTasks = true;
                suspect.Tasks.CruiseWithVehicle(flySpeed);
                //suspect.Tasks.LandPlane(suspectVehicle, spawnPoint, landPoint);
                Game.LogTrivial("[FireyCallouts][Debug]<OnCalloutAccepted> Pilot warped into vehicle.");
            }

            // Show route for player
            suspectBlip = suspectVehicle.AttachBlip();
            suspectBlip.Color = Color.Yellow;
            suspectBlip.EnableRoute(Color.Yellow);
            suspectVehicle.IsPositionFrozen = false;

            return base.OnCalloutAccepted();
        }

        public override void OnCalloutNotAccepted() {
            Game.LogTrivial("[FireyCallouts][Log] Not accepted 'Plane Landing' callout.");

            // Clean up if not accepted
            if (suspect.Exists()) suspect.Delete();
            if (suspectVehicle.Exists()) suspectVehicle.Delete();
            if (suspectBlip.Exists()) suspectBlip.Delete();

            // Remove emergency services
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

                // If pilot jumps out of plane warp back in
                if (suspect.Exists() && suspectVehicle.Exists() && !suspect.IsInVehicle(suspectVehicle, false) && landingSituation < 1) {
                    suspect.WarpIntoVehicle(suspectVehicle, seat);
                    suspect.Tasks.CruiseWithVehicle(suspectVehicle, flySpeed, VehicleDrivingFlags.IgnorePathFinding);
                    //suspect.Tasks.LandPlane(suspectVehicle, spawnPoint, landPoint);
                    //suspect.Tasks.PerformDrivingManeuver(VehicleManeuver.GoForwardStraight);

                    Game.LogTrivial("[FireyCallouts][Debug]<Process> Pilot warped into vehicle.");
                    GameFiber.Wait(3000);
                }

                // Unfreeze plane
                if (suspectVehicle.Exists() && positionFrozen && landingSituation < 1) {
                    suspectVehicle.IsPositionFrozen = false;
                    positionFrozen = false;
                    suspect.KeepTasks = true;
                    Game.LogTrivial("[FireyCallouts][Debug] Position unfrozen.");
                }

                // Remove invincibility for pilot
                if (suspect.Exists() && invincible && suspect.IsInVehicle(suspectVehicle, false)) {
                    suspect.IsInvincible = false;
                    invincible = false;
                    Game.LogTrivial("[FireyCallouts][Debug] Removed invincibility of pilot.");
                }

                // Check if the pilot is in the correct seat
                if (suspectVehicle.Exists() && suspectVehicle.HasDriver && !driverThere) {
                    suspect.KeepTasks = true;
                    //suspect.Tasks.FollowPointRoute(route, flySpeed);

                    Game.LogTrivial("[FireyCallouts][Debug] Vehicle has driver.");
                    driverThere = true;
                }

                // Distance plane - player is < 40
                if (suspect.Exists() && suspect.DistanceTo(Game.LocalPlayer.Character.GetOffsetPosition(Vector3.RelativeFront)) < 40f) {
                    suspect.KeepTasks = true;
                    //if (suspectBlip.Exists()) suspectBlip.Delete();
                    GameFiber.Wait(2000);
                }

                // Plane reaches las point on the route -> start landing
                if (suspectVehicle.Exists() && suspect.Exists() && suspectVehicle.DistanceTo2D(lastRoutePoint) < 5f && (landingSituation == 0)) {
                    GameFiber.Wait(4000);
                    suspectVehicle.IsInvincible = false;
                    // Start landing
                    Game.LogTrivial("[FireyCallouts][Debug] Initialise landing.");
                    suspect.KeepTasks = true;
                    //suspect.Tasks.LandPlane(lastRoutePoint, landPoint);
                    landingSituation = 1;
                    GameFiber.Wait(2000);
                }

                // Plane touchdown
                if (suspectVehicle.Exists() && suspect.Exists() && suspectVehicle.DistanceTo(landPoint) < 5f && (landingSituation == 1)) {
                    // Explode if the plane is dedicated to crash
                    if(willCrash) {
                        suspect.Tasks.CruiseWithVehicle(10f);
                        suspectVehicle.Explode();
                    }
                    landingSituation = 2;
                    Game.LogTrivial("[FireyCallouts][Debug] Landed.");
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
