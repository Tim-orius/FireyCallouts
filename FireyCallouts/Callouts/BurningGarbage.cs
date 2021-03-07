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
    [CalloutInfo("DumpsterFire", CalloutProbability.VeryHigh)]

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
                                                                new Vector3(-256.27f, 6247.1f, 31.49f)}; // Paleto
        private Vector3 spawnPoint;
        private Vector3 area;
        private int fire;
        private Blip locationBlip;
        private LHandle pursuit;
        private bool pursuitCreated = false;

        private string[] weaponList = new string[] {"weapon_flaregun", "weapon_molotov", "weapon_petrolcan"};

        public override bool OnBeforeCalloutDisplayed() {
            Game.LogTrivial("[FireyCallouts][Log] Initialising 'Dumpster Fire' callout.");

            // Random location for the fire
            int chosenLocation = mrRandom.Next(0, locations.Capacity);
            spawnPoint = locations[chosenLocation];
            
            // Create Fire
            fire = NativeFunction.Natives.StartScriptFire(spawnPoint, 25, true);

            // create Suspect
            suspect = new Ped(spawnPoint.Around(20f));
            suspect.IsPersistent = true;
            suspect.BlockPermanentEvents = true;
            suspect.Tasks.Wander();

            // Give suspect random weapon (from the above list)
            int decision = mrRandom.Next(0,3);
            if(decision > 0){
                decision = mrRandom.Next(0, weaponList.Length);
                if (decision == 2){
                    suspect.Inventory.GiveNewWeapon(weaponList[decision], 1, true);
                } else {
                    suspect.Inventory.GiveNewWeapon(weaponList[decision], 16, true);
                }
            }

            Functions.PlayScannerAudioUsingPosition("ASSISTANCE_REQUIRED IN_OR_ON_POSITION", spawnPoint);

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

            if(suspect.Exists()) suspect.Delete();
            if(locationBlip.Exists()) locationBlip.Delete();

            base.OnCalloutNotAccepted();
            Game.LogTrivial("[FireyCallouts][Log] Cleaned up 'Dumpster Fire' callout.");
        }

        public override void Process() {
            base.Process();

            GameFiber.StartNew(delegate {

                if (suspect.Exists() && suspect.DistanceTo(Game.LocalPlayer.Character.GetOffsetPosition(Vector3.RelativeFront)) < 40f) {
                    suspect.KeepTasks = true;
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
                if (Game.IsKeyDown(System.Windows.Forms.Keys.Delete)) End();
                if (suspect.IsDead) End();
                if (Functions.IsPedArrested(suspect)) End();
            }, "DumpsterFire [FireyCallouts]");
        }

        public override void End() {

            if (suspect.Exists()) { suspect.Dismiss(); }
            if(locationBlip.Exists()) locationBlip.Delete();

            Functions.PlayScannerAudio("WE_ARE_CODE FOUR");

            base.End();
            Game.LogTrivial("[FireyCallouts][Log] Cleaned up 'Dumpster Fire' callout.");
        }

    }
}
