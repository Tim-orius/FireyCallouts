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

    [CalloutInfo("Plane Landing", CalloutProbability.Low)]

    class PlaneLanding : Callout {

        private Random mrRandom = new Random();

        private Ped suspect;
        private Vehicle suspectVehicle;
        private Vector3 spawnPoint;
        private Vector3 landPoint;
        private Blip suspectBlip;
        private List<Vector3> landLocations = new List<Vector3>() { new Vector3(1772.492f, 2088.625f, 66.50783f), // Near prison
                                                                    new Vector3(-2638.077f, 2736.208f, 17.22942f), // Zancudo bridge
                                                                    new Vector3(1273.857f, 6494.724f, 20.96697f), // Paleto
                                                                    new Vector3(208.9535f, -1227.534f, 38.82745f)}; // Highway-bridge directly south of simeons

        private List<Vector3> spawnLocations = new List<Vector3>() { new Vector3(1891.882f, 3130.54f, 308.6497f), // Prison - northbound
                                                                     new Vector3(-2729.4f, 2140.3f, 337.62f), // Zancudo bridge - southbound
                                                                     new Vector3(1775.787f, 6503.196f, 304.23f), // Paleto - eastbound
                                                                     new Vector3(-301.3139f, -1236.333f, 382.55003f)}; // Simeons highway - westbound

        private readonly float flySpeed = 80f;
        private int seat = 1;
        private int landingSituation;

        private readonly string[] planeModels = new string[] { "velum", "velum2", "vestra", "dodo", "duster", "mammatus" };
        private bool willCrash = false;
        private bool distanceCheck = false;

        private List<Vehicle> emergencyVehicles = new List<Vehicle>();

        public override bool OnBeforeCalloutDisplayed() {
            Game.LogTrivial("[FireyCallouts][Log] Initialising 'Plane Landing' callout.");

            // Check distance of player to landing locations
            
            List<Vector3> possibleLandLocations = new List<Vector3>();
            List<Vector3> possibleSpawnLocations = new List<Vector3>();
            Vector3 l;
            for (int zz = 0; zz < landLocations.Count; zz++) {
                l = landLocations[zz];
                if (l.DistanceTo(Game.LocalPlayer.Character.GetOffsetPosition(Vector3.RelativeFront)) < Initialization.maxCalloutDistance) {
                    possibleLandLocations.Add(l);
                    possibleSpawnLocations.Add(spawnLocations[zz]);
                }
            }

            int landLocationsCount = possibleLandLocations.Count;

            if (landLocationsCount < 1) {
                return AbortCallout();
            }

            int locationDecision = mrRandom.Next(0, landLocationsCount);
            int decision;

            landPoint = possibleLandLocations[locationDecision];
            spawnPoint = possibleSpawnLocations[locationDecision];

            Game.LogTrivial("[FireyCallouts][Debug] Locations set.");

            ShowCalloutAreaBlipBeforeAccepting(landPoint, 30f);
            AddMinimumDistanceCheck(40f, landPoint);

            CalloutMessage = "Plane doing emergency landing";
            CalloutPosition = landPoint;

            // Initialise vehicle
            decision = mrRandom.Next(0, planeModels.Length);
            suspectVehicle = new Vehicle(planeModels[decision], spawnPoint) {
                IsPersistent = true,
                IsEngineOn = true
            };
            suspectVehicle.Face(landPoint);

            // Initialise ped
            suspect = suspectVehicle.CreateRandomDriver();
            suspect.IsPersistent = true;

            suspect.Tasks.LandPlane(suspectVehicle, spawnPoint, landPoint);
            landingSituation = 0;

            Game.LogTrivial("[FireyCallouts][Debug] Vehicle & pilot set.");

            // Decide wether the plane will crash (explode)
            decision = mrRandom.Next(0, 2);
            if (decision == 1) {
                willCrash = true;
            }

            Functions.PlayScannerAudioUsingPosition("ASSISTANCE_REQUIRED IN_OR_ON_POSITION", spawnPoint);
            Functions.PlayScannerAudio("UNITS_RESPOND_CODE_03");

            Game.DisplayNotification("web_lossantospolicedept",
                                     "web_lossantospolicedept",
                                     "~y~FireyCallouts",
                                     "~r~Plane landing",
                                     "~w~A plane has to perform an emegrency landing on a highway. Respond ~r~Code 3");

            return base.OnBeforeCalloutDisplayed();
        }

        public bool AbortCallout() {
            Game.LogTrivial("[FireyCallouts][Log] Abort 'Plane Landing' callout. Locations too far away (> " + Initialization.maxCalloutDistance.ToString() + ").");

            // Clean up if not accepted
            if (suspect.Exists()) suspect.Delete();
            if (suspectVehicle.Exists()) suspectVehicle.Delete();
            if (suspectBlip.Exists()) suspectBlip.Delete();

            // Remove emergency services
            foreach (Vehicle ev in emergencyVehicles) {
                if (ev.Exists()) { ev.Delete(); }
            }

            Game.LogTrivial("[FireyCallouts][Log] Cleaned up 'Campfire' callout.");
            return false;
        }

        public override bool OnCalloutAccepted() {
            Game.LogTrivial("[FireyCallouts][Log] Accepted 'Plane Landing' callout.");

            // Show route for player
            suspectBlip = suspectVehicle.AttachBlip();
            suspectBlip.Color = Color.Yellow;
            suspectBlip.EnableRoute(Color.Yellow);

            Game.DisplayHelp("Press " + Initialization.endKey.ToString() + " to end the callout at any time.");

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

            base.OnCalloutNotAccepted();
            Game.LogTrivial("[FireyCallouts][Log] Cleaned up 'Plane Landing' callout.");
        }

        public override void Process() {

            base.Process();

            GameFiber.StartNew(delegate {

                // Distance plane - player is < 40
                if (suspect.Exists() && suspectVehicle.Exists() && !distanceCheck && (suspect.DistanceTo(Game.LocalPlayer.Character.GetOffsetPosition(Vector3.RelativeFront)) < 40f 
                    || suspectVehicle.DistanceTo(landPoint) < 40f)) {
                    suspect.KeepTasks = true;
                    suspectVehicle.EngineHealth = 1;

                    distanceCheck = true;
                    GameFiber.Wait(2000);
                }
                if (suspectBlip.Exists() && suspect.DistanceTo(Game.LocalPlayer.Character.GetOffsetPosition(Vector3.RelativeFront)) < 30f) suspectBlip.Delete();

                // Plane touchdown
                if (suspectVehicle.Exists() && suspect.Exists() && suspectVehicle.DistanceTo(landPoint) < 5f && (landingSituation == 0)) {
                    // Explode if the plane is dedicated to crash
                    if (willCrash) {
                        suspect.Tasks.CruiseWithVehicle(10f);
                        suspectVehicle.Explode();
                        landingSituation = 2;
                    } else {
                        suspect.Tasks.PerformDrivingManeuver(VehicleManeuver.GoForwardStraightBraking);
                        landingSituation = 1;
                    }
                    // Call Backup (Ambulance, Fire and Police)
                    //CallBackup();
                    Game.LogTrivial("[FireyCallouts][Debug] Landed. Landing situation = "+landingSituation.ToString());
                    GameFiber.Wait(3000);
                }

                // Making sure the plane comes to a stop eventually if the touchdown operations fail
                if (suspectVehicle.Exists() && suspect.Exists() && suspectVehicle.DistanceTo(landPoint) > 75f && (landingSituation != 0)) {
                    suspectVehicle.Explode();
                }

                if (landingSituation == 1 && !suspect.IsDead && suspectVehicle.Velocity == Vector3.Zero) {
                    suspect.Tasks.LeaveVehicle(suspectVehicle, LeaveVehicleFlags.BailOut);
                }

                if (Game.LocalPlayer.Character.IsDead) End();
                if (Game.IsKeyDown(Initialization.endKey)) End();
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

            Functions.PlayScannerAudio("WE_ARE_CODE_4");

            base.End();
            Game.LogTrivial("[FireyCallouts][Log] Cleaned up 'Plane Landing' callout.");
        }

        public void CallBackup() {
            Game.LogTrivial("[FireyCallouts][Log] Spawning other emergency vehicles (Backup).");

            int emx;
            Vehicle emVehicle;

            Game.DisplayNotification("web_lossantospolicedept",
                                     "web_lossantospolicedept",
                                     "~y~FireyCallouts",
                                     "~b~Dispatch",
                                     "~w~We are sending Backup to your position for help. They should arrive shortly.");

            for (int em = 0; em < 4; em++) { 
                emx = em % 3;
                switch (emx) {
                    case 0: {
                            // Call Fire dept
                            emVehicle = Functions.RequestBackup(landPoint, LSPD_First_Response.EBackupResponseType.Code3, LSPD_First_Response.EBackupUnitType.LocalUnit);
                            break;
                        }
                    case 1: {
                            // Call ambulance
                            emVehicle = Functions.RequestBackup(landPoint, LSPD_First_Response.EBackupResponseType.Code3, LSPD_First_Response.EBackupUnitType.Firetruck);
                            break;
                        }
                    case 2: {
                            // Call ambulance
                            emVehicle = Functions.RequestBackup(landPoint, LSPD_First_Response.EBackupResponseType.Code3, LSPD_First_Response.EBackupUnitType.Ambulance);
                            break;
                        }
                    default: {
                            // Call Fire dept
                            emVehicle = Functions.RequestBackup(landPoint, LSPD_First_Response.EBackupResponseType.Code3, LSPD_First_Response.EBackupUnitType.Firetruck);
                            break;
                        }
                }
                emergencyVehicles.Add(emVehicle);
            }
        }
    }
}
