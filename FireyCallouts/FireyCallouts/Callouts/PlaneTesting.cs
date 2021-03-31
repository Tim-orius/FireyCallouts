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

    [CalloutInfo("Plane Testing", CalloutProbability.Never)]

    class PlaneTesting : Callout {

        private Random mrRandom = new Random();

        private List<Ped> suspects = new List<Ped>();
        private List<Vehicle> suspectVehicles = new List<Vehicle>();
        private Vector3[] spawnPoints = new Vector3[] { new Vector3(1750f, 3100f, 800f), new Vector3(1800f, 3100f, 800f),
                                                        new Vector3(1850f, 3100f, 800f), new Vector3(1900f, 3100f, 800f),
                                                        new Vector3(1950f, 3100f, 500f), new Vector3(2000f, 3100f, 500f),
                                                        new Vector3(2050f, 3100f, 500f), new Vector3(2100f, 3100f, 500f) };
        private Vector3 landPoint = new Vector3(1772.492f, 2088.625f, 66.50783f);
        private Vector3 lastRoutePoint;
        private Blip suspectBlip;

        // Testing route
        private Vector3[] flyRoute = new Vector3[] { new Vector3(1831.5f, 2609.00f, 87.0f), new Vector3(1831.5f, 2000.0f, 87.0f) };
        private readonly float flySpeed = 80f;

        private bool positionFrozen = false;
        private bool invincible = false;
        // Pilot seat
        private int seat = 1;

        // Velum as standard model for simplicity for these tests
        private string[] planeModels = new string[] { "velum", "velum2", "vestra", "dodo", "duster", "mammatus" };

        public override bool OnBeforeCalloutDisplayed() {
            Game.LogTrivial("[FireyCallouts][Log] Initialising 'PlaneTesting' callout.");

            //lastRoutePoint = flyRoute[flyRoute.Length-1]; // flyRoutes[locationDecision][flyRoutes[locationDecision].Length];

            ShowCalloutAreaBlipBeforeAccepting(spawnPoints[0], 30f);
            AddMinimumDistanceCheck(40f, spawnPoints[0]);

            CalloutMessage = "Plane Testing; NO REGULAR CALLOUT; FOR TESTING PURPOSES!";
            CalloutPosition = spawnPoints[0];

            /*

            // Initialise vehicle
            suspectVehicle = new Vehicle(planeModel, spawnPoint);
            suspectVehicle.IsPersistent = true;

            // Initialise ped
            suspect = suspectVehicle.CreateRandomDriver();
            suspect.IsPersistent = true;
            suspect.BlockPermanentEvents = true;


            if (!suspect.IsInVehicle(suspectVehicle, false)) {

                suspect.WarpIntoVehicle(suspectVehicle, seat);
                
                suspect.Tasks.CruiseWithVehicle(suspectVehicle, flySpeed, VehicleDrivingFlags.IgnorePathFinding);

                suspect.Tasks.LandPlane(suspectVehicle, spawnPoint, landPoint);

            }

            //suspect.Tasks.PerformDrivingManeuver(VehicleManeuver.GoForwardStraight);
            }

            */

            return base.OnBeforeCalloutDisplayed();
        }

        public override bool OnCalloutAccepted() {
            Game.LogTrivial("[FireyCallouts][Log] Accepted 'PlaneTesting' callout.");

            this.SpawnPlanes();

            // Show route for player
            suspectBlip = suspectVehicles[0].AttachBlip();
            suspectBlip.Color = Color.Yellow;
            suspectBlip.EnableRoute(Color.Yellow);

            return base.OnCalloutAccepted();
        }

        public override void OnCalloutNotAccepted() {
            Game.LogTrivial("[FireyCallouts][Log] Not accepted 'PlaneTesting' callout.");

            // Clean up if not accepted
            if (suspectBlip.Exists()) suspectBlip.Delete();
            foreach (Vehicle v in suspectVehicles) {
                if (v.Exists()) { v.Delete(); }
            }
            foreach (Ped p in suspects) {
                if (p.Exists()) { p.Delete(); }
            }

            base.OnCalloutNotAccepted();
            Game.LogTrivial("[FireyCallouts][Log] Cleaned up 'PlaneTesting' callout.");
        }

        public override void Process() {

            base.Process();

            GameFiber.StartNew(delegate {

                /*
                // Distance plane - player is < 40
                if (suspect.Exists() && suspect.DistanceTo(Game.LocalPlayer.Character.GetOffsetPosition(Vector3.RelativeFront)) < 40f) {
                    suspect.KeepTasks = true;
                    //if (suspectBlip.Exists()) suspectBlip.Delete();
                    GameFiber.Wait(2000);
                }
                */

                if (Game.IsKeyDown(System.Windows.Forms.Keys.Up)) {
                    foreach (Vehicle v in suspectVehicles) {
                        if (v.Exists()) { v.IsPositionFrozen = false; }
                    }
                }
                if (Game.IsKeyDown(System.Windows.Forms.Keys.Down)) {
                    foreach (Vehicle v in suspectVehicles) {
                        if (v.Exists()) { v.IsPositionFrozen = true; }
                    }
                }


                if (Game.LocalPlayer.Character.IsDead) End();
                if (Game.IsKeyDown(Initialization.endKey)) End();
            }, "PlaneTesting [FireyCallouts]");
        }

        public override void End() {

            if (suspectBlip.Exists()) { suspectBlip.Delete(); }
            foreach (Vehicle v in suspectVehicles) {
                if (v.Exists()) { v.Dismiss(); }
            }
            foreach (Ped p in suspects) {
                if (p.Exists()) { p.Dismiss(); }
            }

            Functions.PlayScannerAudio("WE_ARE_CODE_4");

            base.End();
            Game.LogTrivial("[FireyCallouts][Log] Cleaned up 'PlaneTesting' callout.");
        }

        public void SpawnPlanes() {

            int jj, kk, ll;
            int testGroups = 8;
            Vector3 spawnPoint;
            string planeModel;

            ll = 0;
            planeModel = planeModels[0];

            for (int ii = 0; ii < testGroups; ii++) {
                kk = ii + (ll * testGroups);

                jj = ii % (testGroups / 2);

                spawnPoint = spawnPoints[ii];

                Game.LogTrivial("[FireyCallouts][Debug]" + ii.ToString());

                // Initialise vehicle
                suspectVehicles.Add(new Vehicle(planeModel, spawnPoint));

                Game.LogTrivial("[FireyCallouts][Debug] xx " + jj.ToString());

                // Initialise ped
                suspects.Add(suspectVehicles[kk].CreateRandomDriver());

                suspectVehicles[kk].Face(landPoint);

                /* Model: velum
                 * 
                 * Height = approx. 108f
                 * Same results for 0, 1 and 3; Jumps out for 2!
                 * 
                 * Height = 300f / 600f
                 * 1 & 3 gliding, 0 falling
                 * 
                 */

                switch (jj) {
                    case 0: { // TASK.LANDPLANE

                            /*
                             * THIS <--------------------------------------------------------------------------------------------
                             */

                            Game.LogTrivial("[FireyCallouts][Debug] -- " + jj.ToString());

                            suspects[kk].Tasks.LandPlane(suspectVehicles[kk], spawnPoint, landPoint);

                            Game.LogTrivial("[FireyCallouts][Debug] == " + jj.ToString());
                            break;
                        }
                    case 1: { // TASK.CRUISEWITHVEHICLE

                            /*
                             * Engine: starts
                             * Driver: stays
                             * Flys properly: no
                             * Dropping: low variance, straight
                             */

                            Game.LogTrivial("[FireyCallouts][Debug] -- " + jj.ToString());

                            suspects[kk].Tasks.CruiseWithVehicle(suspectVehicles[kk], flySpeed, VehicleDrivingFlags.IgnorePathFinding);

                            Game.LogTrivial("[FireyCallouts][Debug] == " + jj.ToString());

                            break;
                        }
                    case 2: { // TASK.FOLLOWPOINTROUTE

                            /*
                             * Engine: starts
                             * Driver: JUMPS OUT
                             * Flys properly: no
                             * Dropping: -
                             */

                            Game.LogTrivial("[FireyCallouts][Debug] -- " + jj.ToString());

                            suspects[kk].Tasks.FollowPointRoute(flyRoute, flySpeed);

                            Game.LogTrivial("[FireyCallouts][Debug] == " + jj.ToString());

                            break;
                        }
                    case 3: { // TASK.GOFORWARDSTRAIGHT

                            /*
                             * Engine: starts
                             * Driver: stays
                             * Flys properly: no
                             * Dropping: low variance, straight
                             */

                            Game.LogTrivial("[FireyCallouts][Debug] -- " + jj.ToString());

                            suspects[kk].Tasks.PerformDrivingManeuver(VehicleManeuver.GoForwardStraight);

                            Game.LogTrivial("[FireyCallouts][Debug] == " + jj.ToString());

                            break;
                        }
                }

                Game.LogTrivial("[FireyCallouts][Debug] ## " + jj.ToString());

                if (ii == testGroups - 1) {
                    ll += 1;

                    for (int xx = 0; xx < spawnPoints.Count(); xx++) {
                        spawnPoints[xx].Y += 50;
                    }
                    planeModel = planeModels[ll];
                }

                suspects[kk].IsPersistent = true;
                //suspects[kk].BlockPermanentEvents = true;

                Game.LogTrivial("[FireyCallouts][Debug] $$ " + jj.ToString());

                suspectVehicles[kk].IsPersistent = true;
                //suspectVehicles[kk].IsPositionFrozen = true;

                Game.LogTrivial("[FireyCallouts][Debug] %% " + jj.ToString());
            }
        }
    }
}
