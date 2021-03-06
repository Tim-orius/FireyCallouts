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
    [CalloutInfo("BurningGarbage", CalloutProbability.VeryHigh)]
    class BurningGarbage : Callout {

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
        private Fire fire;
        private Blip locationBlip;
        private LHandle pursuit;

        private string[] weaponList = new string[] {"weapon_flaregun", "weapon_molotov", "weapon_petrolcan"};

        public override bool OnBeforeCalloutDisplayed() {
            // Random location for the fire
            int chosen_location = mrRandom.Next(0, locations.Capacity);
            spawnPoint = locations[chosen_location];

            // Create Fire
            fire = new MyFire(spawnPoint, new Vector3(0, 0, 0), 600, 30, 2); // ----------------------------------------------

            // create Suspect
            suspect = new Ped(spawnPoint.Around(20f));
            suspect.IsPersistent = true;
            suspect.BlockPermanentEvents = true;

            // Give suspect random weapon (from the above list)
            int decision = mrRandom.Next(0,1);
            if(decision == 1){
                decision = mrRandom.Next(0, weaponList.Length - 1);
                if (decision == 2){
                    suspect.Inventory.GiveNewWeapon(weaponList[decision], 1, true);
                } else {
                    suspect.Inventory.GiveNewWeapon(weaponList[decision], 16, true);
                }
            }

            return base.OnBeforeCalloutDisplayed();
        }

        public override bool OnCalloutAccepted() {
            area = spawnPoint.Around2D(1f, 2f);
            locationBlip = new Blip(area, 40f);
            locationBlip.Color = Color.Yellow;
            locationBlip.EnableRoute(Color.Yellow);

            return base.OnCalloutAccepted();
        }

        public override void OnCalloutNotAccepted(){
            if(fire.Exists()) fire.Delete();
            if(suspect.Exists()) suspect.Delete();
            if(locationBlip.Exists()) locationBlip.Delete();

            base.OnCalloutNotAccepted();
        }

        public override void Process() {
            base.Process();

            GameFiber.StartNew(delegate {
                if (suspect.Exists() && suspect.DistanceTo(Game.LocalPlayer.Character.GetOffsetPosition(Vector3.RelativeFront)) < 40f) {
                    suspect.KeepTasks = true;
                    GameFiber.Wait(2000);
                }
                
                NativeFunction.CallByName<uint>("TASK_REACT_AND_FLEE_PED", suspect);

                if(suspect.DistanceTo(Game.LocalPlayer.Character.GetOffsetPosition(Vector3.RelativeFront)) < 30f){
                    Functions.AddPedToPursuit(pursuit, suspect);
                }

                if (Game.LocalPlayer.Character.IsDead) End();
                if (Game.IsKeyDown(System.Windows.Forms.Keys.Delete)) End();
                if (suspect.IsDead) End();
                if (Functions.IsPedArrested(suspect)) End();
            }, "DumpsterFire [FireyCallouts]");
        }

        public override void End() {
            base.End();

            if (suspect.Exists()) { suspect.Dismiss(); }
            if(locationBlip.Exists()) locationBlip.Delete();

            Functions.PlayScannerAudio("WE_ARE_CODE FOUR");
        }

    }
}
