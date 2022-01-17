using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using Rage;
using Rage.Native;
using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Mod.Callouts;
using LSPD_First_Response.Engine.Scripting.Entities;
using FireyCallouts.Utilitys;


namespace FireyCallouts.Callouts {
    [CalloutInfo("Smoke Detected", CalloutProbability.Low)]

    class SmokeDetected : Callout {

        private Random mrRandom = new Random();

        private Ped suspect;
        // (First in array is center position)
        private List<Vector3> locations = new List<Vector3>() { new Vector3(0f, 0f, 0f),
                                                                new Vector3(1f, 1f, 1f)
                                                              };
        private Vector3 spawnPoint;
        private Vector3 area;
        private Blip locationBlip;

        private uint fire;
        private List<uint> fireList = new List<uint>();
        private bool endKeyPressed = false;

        private List<string[]> dialoguesWitness = new List<string[]>() { new string[] { "~y~Witness: ~w~Hello Officer. I have noticed the smell of smoke.", "~y~You: ~w~I can't smell it. When did you smell it?",
                                                                                        "~y~Witness: ~w~xx.",
                                                                                        "~y~You: ~w~yy", "~y~Witness: ~w~xx",
                                                                                        "~y~You: ~w~yy"},
                                                                         new string[] { "~y~Witness: ~w~Hello Officer.", "~y~You: ~w~yy",
                                                                                        "~y~Witness: ~w~xx",
                                                                                        "~y~You: ~w~yy", "~y~Witness: ~w~xx",
                                                                                        "~y~You: ~w~yy"},
                                                                         new string[] { "~y~Witness: ~w~xx", "~y~You: ~w~yy",
                                                                                        "~y~Witness: ~w~xx",
                                                                                        "~y~You: ~w~yy", "~y~Witness: ~w~xx",
                                                                                        "~y~You: ~w~yy"},
                                                                         new string[] { "~y~Witness: ~w~xx", "~y~You: ~w~yy",
                                                                                        "~y~Witness: ~w~xx",
                                                                                        "~y~You: ~w~yy", "~y~You: ~w~yy",
                                                                                        "~y~You: ~w~xx"} };


        public override bool OnBeforeCalloutDisplayed() {
            Game.LogTrivial("[FireyCallouts][Log] Initialising 'Smoke Detected' callout.");

            int decision;
            float offsetx, offsety, offsetz;

            // Check locations around 800f to the player
            List<Vector3> possibleLocations = new List<Vector3>();
            /*
            foreach (Vector3 l in locations) {
                if (l.DistanceTo(Game.LocalPlayer.Character.GetOffsetPosition(Vector3.RelativeFront)) < Initialization.maxCalloutDistance || true) {
                    possibleLocations.Add(lo);
                }
            }

            if (possibleLocations.Count < 1) {
                Game.LogTrivial("[FireyCallouts][Log] Abort 'Dumpster Fire' callout. player too far away from all locations.");
                return AbortCallout();
            }
            */
            // DELETE WHEN UNCOMMENTED THE ABOVE STATEMENTS!!! ---------------------------------------------------------------------------
            possibleLocations = locations;

            // Random location for the fire
            int chosenLocation = mrRandom.Next(0, possibleLocations.Count);
            spawnPoint = possibleLocations[chosenLocation];

            ShowCalloutAreaBlipBeforeAccepting(spawnPoint, 30f);
            AddMinimumDistanceCheck(40f, spawnPoint);

            CalloutMessage = "Smoke Detected";
            CalloutPosition = spawnPoint;

            // Create Fire
            for (int f = 1; f < 100; f++) {
                // Spawn several fires with random offset positions to generate a bigger fire
                decision = mrRandom.Next(0, 4);
                offsetx = decision * 2 * f / (100f * 2);
                decision = mrRandom.Next(0, 3);
                offsety = decision * f / 50f;
                decision = mrRandom.Next(0, 2);
                offsetz = decision * f / 80f;

                decision = mrRandom.Next(0, 2);
                if (decision == 0) {
                    offsetx = -offsetx;
                }
                decision = mrRandom.Next(0, 2);
                if (decision == 0) {
                    offsety = -offsety;
                }

                // These fires do not extinguish by themselves.
                fire = NativeFunction.Natives.StartScriptFire<uint>(spawnPoint.X + offsetx, spawnPoint.Y + offsety, spawnPoint.Z + offsetz, 25, true);

                fireList.Add(fire);
            }

            Functions.PlayScannerAudioUsingPosition("ASSISTANCE_REQUIRED IN_OR_ON_POSITION", spawnPoint);
            Functions.PlayScannerAudio("UNITS_RESPOND_CODE_03");

            Game.DisplayNotification("web_lossantospolicedept",
                                     "web_lossantospolicedept",
                                     "~y~FireyCallouts",
                                     "~r~Smoke detected",
                                     "~w~A caller reported smoke at a building. Respond ~r~Code 3");

            return base.OnBeforeCalloutDisplayed();
        }

        public bool AbortCallout() {
            Game.LogTrivial("[FireyCallouts][Log] Clean up 'Smoke Detected' callout.");

            // Clean up if not accepted
            if (suspect.Exists()) suspect.Delete();
            if (locationBlip.Exists()) locationBlip.Delete();

            foreach (uint f in fireList) {
                NativeFunction.Natives.RemoveScriptFire(f);
            }

            Game.LogTrivial("[FireyCallouts][Log] Cleaned up 'Smoke Detected' callout.");
            return false;
        }

        public override bool OnCalloutAccepted() {
            Game.LogTrivial("[FireyCallouts][Log] Accepted 'Smoke Detected' callout.");

            int emx;
            Vehicle emVehicle;

            area = spawnPoint.Around2D(1f, 2f);
            locationBlip = new Blip(area, 40f) {
                Color = Color.Yellow
            };
            locationBlip.EnableRoute(Color.Yellow);

            Game.DisplayHelp("Press " + Initialization.endKey.ToString() + " to end the callout at any time.");

            return base.OnCalloutAccepted();
        }

        public override void OnCalloutNotAccepted() {
            Game.LogTrivial("[FireyCallouts][Log] Not accepted 'Smoke Detected' callout.");

            // Clean up if not accepted
            if (suspect.Exists()) suspect.Delete();
            if (locationBlip.Exists()) locationBlip.Delete();

            foreach (uint f in fireList) {
                NativeFunction.Natives.RemoveScriptFire(f);
            }

            base.OnCalloutNotAccepted();
            Game.LogTrivial("[FireyCallouts][Log] Cleaned up 'Smoke Detected' callout.");
        }

        public override void Process() {
            base.Process();

            GameFiber.StartNew(delegate {

               
                if (locationBlip.Exists() && locationBlip.DistanceTo(Game.LocalPlayer.Character.GetOffsetPosition(Vector3.RelativeFront)) < 40f) {
                        if (locationBlip.Exists()) locationBlip.Delete();
                        //CallBackup();
                        GameFiber.Wait(2000);
                }

                if (Game.LocalPlayer.Character.IsDead) End();
                if (Game.IsKeyDown(Initialization.endKey)) { endKeyPressed = true; End(); }
                if (suspect.Exists()) { if (suspect.IsDead) End(); }
                if (suspect.Exists()) { if (Functions.IsPedArrested(suspect)) End(); }
            }, "SmokeDetected [FireyCallouts]");
        }

        public override void End() {

            if (suspect.Exists()) { suspect.Dismiss(); }
            if (locationBlip.Exists()) locationBlip.Delete();

            // Check if ended by pressing end and delete fires; Otherwise keep them
            // Warning: ending the callout without deleting the fires is causing the fires to burn indefinitely
            if (endKeyPressed) {
                foreach (uint f in fireList) {
                    //NativeFunction.CallByName<uint>("REMOVE_SCRIPT_FIRE", f);
                    NativeFunction.Natives.RemoveScriptFire(f);
                }
            }

            Functions.PlayScannerAudio("WE_ARE_CODE_4");

            base.End();
            Game.LogTrivial("[FireyCallouts][Log] Cleaned up 'Smoke Detected' callout.");
        }

    }
}
