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


namespace FireyCallouts.Callouts {
    [CalloutInfo("Dumpster Fire", CalloutProbability.Low)]

    class DumpsterFire : Callout {

        Random mrRandom = new Random();

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
        private uint[] fireList = new uint[10];
        private bool endKeyPressed = false;

        public override bool OnBeforeCalloutDisplayed() {
            Game.LogTrivial("[FireyCallouts][Log] Initialising 'Dumpster Fire' callout.");

            int decision;
            float offsetx, offsety, offsetz;

            // Check locations around 400f to the player
            List<Vector3> possibleLocations = new List<Vector3>();
            foreach (Vector3 l in locations) {
                if (l.DistanceTo(Game.LocalPlayer.Character.GetOffsetPosition(Vector3.RelativeFront)) < 400f) {
                    possibleLocations.Add(l);
                }
            }

            if (possibleLocations.Count < 1) {
                OnCalloutNotAccepted();
                return false;
            }

            // Random location for the fire
            int chosenLocation = mrRandom.Next(0, possibleLocations.Count);
            spawnPoint = possibleLocations[chosenLocation];

            ShowCalloutAreaBlipBeforeAccepting(spawnPoint, 30f);
            AddMinimumDistanceCheck(40f, spawnPoint);

            CalloutMessage = "Illegal Firework";
            CalloutPosition = spawnPoint;

            // Create Fire
            for (int f = 1; f < 11; f++) {
                // Spawn several fires with random offset positions to generate a bigger fire
                decision = mrRandom.Next(0, 6);
                offsetx = decision * f / 100;
                decision = mrRandom.Next(0, 6);
                offsety = decision / f / 100;
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

                // !!! Via Natives returns null, via callby returns uint
                // These fires do not extinguish by themselves.
                fire = NativeFunction.CallByName<uint>("START_SCRIPT_FIRE", spawnPoint.X + offsetx, spawnPoint.Y + offsety + 0.1f, spawnPoint.Z + offsetz, 25, true);
                //fire = NativeFunction.Natives.StartScriptFire(spawnPoint.X + offsetx, spawnPoint.Y + offsety, spawnPoint.Z + offsetz, 25, true);
                
                fireList.Append(fire);
            }

            // create Suspect
            suspect = new Ped(spawnPoint.Around(1f));
            suspect.IsFireProof = true;
            suspect.IsPersistent = true;
            suspect.BlockPermanentEvents = true;
            suspect.Tasks.Wander();

            // Give suspect random weapon (from the above list)
            decision = mrRandom.Next(0,3);
            if(decision > 0){
                decision = mrRandom.Next(0, weaponList.Length);
                if (decision == 2){
                    suspect.Inventory.GiveNewWeapon(new WeaponAsset(weaponList[decision]), 1, true);
                } else {
                    suspect.Inventory.GiveNewWeapon(new WeaponAsset(weaponList[decision]), 16, true);
                }
            }

            Functions.PlayScannerAudioUsingPosition("ASSISTANCE_REQUIRED IN_OR_ON_POSITION", spawnPoint);
            Functions.PlayScannerAudio("UNITS_RESPOND_CODE_03");

            return base.OnBeforeCalloutDisplayed();
        }

        public override bool OnCalloutAccepted() {
            Game.LogTrivial("[FireyCallouts][Log] Accepted 'Dumpster Fire' callout.");

            area = spawnPoint.Around2D(1f, 2f);
            locationBlip = new Blip(area, 40f);
            locationBlip.Color = Color.Yellow;
            locationBlip.EnableRoute(Color.Yellow);

            return base.OnCalloutAccepted();
        }

        public override void OnCalloutNotAccepted(){
            Game.LogTrivial("[FireyCallouts][Log] Not accepted 'Dumpster Fire' callout.");

            // Clean up if not accepted
            if (suspect.Exists()) suspect.Delete();
            if(locationBlip.Exists()) locationBlip.Delete();

            foreach (uint f in fireList) {
                // NativeFunction.Natives.RemoveScriptFire(f);
                NativeFunction.CallByName<uint>("REMOVE_SCRIPT_FIRE", f);
            }

            base.OnCalloutNotAccepted();
            Game.LogTrivial("[FireyCallouts][Log] Cleaned up 'Dumpster Fire' callout.");
        }

        public override void Process() {
            base.Process();

            GameFiber.StartNew(delegate {

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

                if (Game.LocalPlayer.Character.IsDead) End();
                if (Game.IsKeyDown(System.Windows.Forms.Keys.Delete)) { endKeyPressed = true; End(); }
                if (suspect.IsDead) End();
            }, "DumpsterFire [FireyCallouts]");
        }

        public override void End() {

            if (suspect.Exists()) { suspect.Dismiss(); }
            if(locationBlip.Exists()) locationBlip.Delete();

            // Check if ended by pressing end and delete fires; Otherwise keep them
            // Warning: ending the callout without deleting the fires is causing the fires to burn indefinitely
            if (endKeyPressed) {
                foreach (uint f in fireList) {
                    NativeFunction.CallByName<uint>("REMOVE_SCRIPT_FIRE", f);
                    // NativeFunction.Natives.RemoveScriptFire(f);
                }
            }

            Functions.PlayScannerAudio("WE_ARE_CODE_4");

            base.End();
            Game.LogTrivial("[FireyCallouts][Log] Cleaned up 'Dumpster Fire' callout.");
        }

    }
}
