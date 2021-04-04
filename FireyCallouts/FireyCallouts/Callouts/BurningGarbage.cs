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
    [CalloutInfo("Dumpster Fire", CalloutProbability.Low)]

    class DumpsterFire : Callout {

        private Random mrRandom = new Random();

        private Ped suspect;
        private List<Vector3> locations = new List<Vector3>() { new Vector3(-857.6f, -240.9f, 39.5f), // Rockford Hills
                                                                new Vector3(-1165.1f, -1396.4f, 4.9f), // Vespucci Canals
                                                                new Vector3(1057.8f, -787.16f, 58.26f), // Mirror Park
                                                                new Vector3(129.1f, -1486.8f, 29.14f), // Davis - Strawberry
                                                                new Vector3(2543.4f, 341.0f, 108.5f), // Truck stop
                                                                new Vector3(-2954.0f, 445.75f, 15.28f), // Fleeca bank (Heist)
                                                                new Vector3(1534.0f, 3610.7f, 35.35f), // Sandy Shores Motel
                                                                new Vector3(1639.2f, 4820.9f, 41.9f), // Grapeseed
                                                                new Vector3(-256.417f, 6246.083f, 32.57662f)}; // Paleto
        private Vector3 spawnPoint;
        private Vector3 area;
        private Blip locationBlip;
        private LHandle pursuit;
        private bool pursuitCreated = false;

        private string[] weaponList = new string[] {"weapon_flaregun", "weapon_molotov", "weapon_petrolcan"};
        private uint fire;
        private List<uint> fireList = new List<uint>();
        private bool endKeyPressed = false;

        public override bool OnBeforeCalloutDisplayed() {
            Game.LogTrivial("[FireyCallouts][Log] Initialising 'Dumpster Fire' callout.");

            int decision;
            float offsetx, offsety, offsetz;

            // Check locations around 800f to the player
            List<Vector3> possibleLocations = new List<Vector3>();
            foreach (Vector3 l in locations) {
                if (l.DistanceTo(Game.LocalPlayer.Character.GetOffsetPosition(Vector3.RelativeFront)) < 800f) {
                    possibleLocations.Add(l);
                }
            }

            if (possibleLocations.Count < 1) {
                Game.LogTrivial("[FireyCallouts][Log] Abort 'Dumpster Fire' callout. player too far away from all locations.");
                return AbortCallout();
            }

            // Random location for the fire
            int chosenLocation = mrRandom.Next(0, possibleLocations.Count);
            spawnPoint = possibleLocations[chosenLocation];

            ShowCalloutAreaBlipBeforeAccepting(spawnPoint, 30f);
            AddMinimumDistanceCheck(40f, spawnPoint);

            CalloutMessage = "Dumpster Fire";
            CalloutPosition = spawnPoint;

            // Create Fire
            for (int f = 1; f < 11; f++) {
                // Spawn several fires with random offset positions to generate a bigger fire
                decision = mrRandom.Next(0, 6);
                offsetx = decision * f / 100;
                decision = mrRandom.Next(0, 6);
                offsety = decision * f / 100;
                decision = mrRandom.Next(0, 6);
                offsetz = decision * f / 100;

                decision = mrRandom.Next(0, 2);
                if (decision == 0) {
                    offsetx = -offsetx;
                }
                decision = mrRandom.Next(0, 2);
                if (decision == 0) {
                    offsetz = -offsetz;
                }

                // These fires do not extinguish by themselves.
                //fire = NativeFunction.CallByName<uint>("START_SCRIPT_FIRE", spawnPoint.X + offsetx, spawnPoint.Y + offsety + 0.1f, spawnPoint.Z + offsetz, 25, true);
                fire = NativeFunction.Natives.StartScriptFire<uint>(spawnPoint.X + offsetx, spawnPoint.Y + offsety, spawnPoint.Z + offsetz, 25, true);
                
                fireList.Add(fire);
            }

            if (Utils.gamemode == Utils.Gamemodes.Pol) {
                // create Suspect
                suspect = new Ped(spawnPoint.Around(1f)) {
                    IsFireProof = true,
                    IsPersistent = true,
                    BlockPermanentEvents = true
                };
                suspect.Tasks.Wander();

                // Give suspect random weapon (from the above list)
                decision = mrRandom.Next(0, 3);
                if (decision > 0) {
                    decision = mrRandom.Next(0, weaponList.Length);
                    if (decision == 2) {
                        suspect.Inventory.GiveNewWeapon(new WeaponAsset(weaponList[decision]), 1, true);
                    } else {
                        suspect.Inventory.GiveNewWeapon(new WeaponAsset(weaponList[decision]), 16, true);
                    }
                }
            }

            Functions.PlayScannerAudioUsingPosition("ASSISTANCE_REQUIRED IN_OR_ON_POSITION", spawnPoint);
            Functions.PlayScannerAudio("UNITS_RESPOND_CODE_03");

            Game.DisplayNotification("web_lossantospolicedept",
                                     "web_lossantospolicedept",
                                     "~y~FireyCallouts",
                                     "~r~Dumpster on fire",
                                     "~w~Someone called on a burning dumpster. They also witnessed a suspicious person walking away from the location. Respond ~r~Code 3");

            return base.OnBeforeCalloutDisplayed();
        }

        public bool AbortCallout() {
            Game.LogTrivial("[FireyCallouts][Log] Clean up 'Dumpster Fire' callout.");

            // Clean up if not accepted
            if (suspect.Exists()) suspect.Delete();
            if (locationBlip.Exists()) locationBlip.Delete();

            foreach (uint f in fireList) {
                NativeFunction.Natives.RemoveScriptFire(f);
                //NativeFunction.CallByName<uint>("REMOVE_SCRIPT_FIRE", f);
            }

            Game.LogTrivial("[FireyCallouts][Log] Cleaned up 'Dumpster Fire' callout.");
            return false;
        }

        public override bool OnCalloutAccepted() {
            Game.LogTrivial("[FireyCallouts][Log] Accepted 'Dumpster Fire' callout.");

            area = spawnPoint.Around2D(1f, 2f);
            locationBlip = new Blip(area, 40f) {
                Color = Color.Yellow
            };
            locationBlip.EnableRoute(Color.Yellow);

            Game.DisplayHelp("Press " + Initialization.endKey.ToString() + " to end the callout at any time.");

            return base.OnCalloutAccepted();
        }

        public override void OnCalloutNotAccepted(){
            Game.LogTrivial("[FireyCallouts][Log] Not accepted 'Dumpster Fire' callout.");

            // Clean up if not accepted
            if (suspect.Exists()) suspect.Delete();
            if(locationBlip.Exists()) locationBlip.Delete();

            foreach (uint f in fireList) {
                NativeFunction.Natives.RemoveScriptFire(f);
                //NativeFunction.CallByName<uint>("REMOVE_SCRIPT_FIRE", f);
            }

            base.OnCalloutNotAccepted();
            Game.LogTrivial("[FireyCallouts][Log] Cleaned up 'Dumpster Fire' callout.");
        }

        public override void Process() {
            base.Process();

            GameFiber.StartNew(delegate {

                if (Utils.gamemode == Utils.Gamemodes.Pol) {
                    if (suspect.Exists() && suspect.DistanceTo(Game.LocalPlayer.Character.GetOffsetPosition(Vector3.RelativeFront)) < 40f) {
                        suspect.KeepTasks = true;
                        if (locationBlip.Exists()) locationBlip.Delete();
                        GameFiber.Wait(2000);
                    }
                
                    NativeFunction.CallByName<uint>("TASK_REACT_AND_FLEE_PED", suspect);

                    if(!pursuitCreated && suspect.DistanceTo(Game.LocalPlayer.Character.GetOffsetPosition(Vector3.RelativeFront)) < 30f){
                        pursuit = Functions.CreatePursuit();
                        Functions.AddPedToPursuit(pursuit, suspect);
                        Functions.SetPursuitIsActiveForPlayer(pursuit, true);
                        pursuitCreated = true;
                    }
                } else {
                    if (locationBlip.Exists() && locationBlip.DistanceTo(Game.LocalPlayer.Character.GetOffsetPosition(Vector3.RelativeFront)) < 40f) {
                        if (locationBlip.Exists()) locationBlip.Delete();
                        GameFiber.Wait(2000);
                    }
                }

                if (Game.LocalPlayer.Character.IsDead) End();
                if (Game.IsKeyDown(Initialization.endKey)) { endKeyPressed = true; End(); }
                if (suspect.Exists()) { if (suspect.IsDead) End(); }
                if (suspect.Exists()) { if (Functions.IsPedArrested(suspect)) End(); }
            }, "DumpsterFire [FireyCallouts]");
        }

        public override void End() {

            if (suspect.Exists()) { suspect.Dismiss(); }
            if(locationBlip.Exists()) locationBlip.Delete();

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
            Game.LogTrivial("[FireyCallouts][Log] Cleaned up 'Dumpster Fire' callout.");
        }

    }
}
