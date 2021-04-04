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
    [CalloutInfo("Structural Fire", CalloutProbability.Low)]

    class StructuralFire : Callout {

        private Random mrRandom = new Random();

        private Ped suspect;
        // (First in array is center position)
        private List<Vector3[]> locations = new List<Vector3[]>() { new Vector3[] { new Vector3(-50.30444f, -1753.468f, 29.42101f), // center
                                                                                    new Vector3(-47.34398f, -1759.442f, 29.42101f), // pos1
                                                                                    new Vector3(-43.88363f, -1755.396f, 29.42101f), // pos2
                                                                                    new Vector3(-53.06423f, -1746.857f, 29.42101f), // pos3
                                                                                    new Vector3(-56.92595f, -1752.177f, 29.42101f), // pos4
                                                                                    new Vector3(-39.99396f, -1751.581f, 29.42101f), // pos5
                                                                                    new Vector3(-47.98798f, -1761.382f, 29.42101f), // posA
                                                                                    new Vector3(-59.57947f, -1751.757f, 29.42101f)} // posB
                                                                    };
        private List<Tuple<int,int>[]> calculationMatrix = new List<Tuple<int, int>[]>() { new Tuple<int, int>[] { Tuple.Create(1, 2), Tuple.Create(1, 4),
                                                                                                                   Tuple.Create(2, 3), Tuple.Create(3, 4)}
                                                                                         };
        private Vector3 spawnPoint, firePoint;
        private Vector3 area;
        private Blip locationBlip;
        private LHandle pursuit;
        private bool pursuitCreated = false;

        private readonly string[] weaponList = new string[] { "weapon_flaregun", "weapon_molotov", "weapon_petrolcan" };
        private uint fire;
        private List<uint> fireList = new List<uint>();
        private bool endKeyPressed = false;

        private List<Tuple<float, float>> mbMatrix = new List<Tuple<float, float>>();

        private List<Vehicle> emergencyVehicles = new List<Vehicle>();

        public override bool OnBeforeCalloutDisplayed() {
            Game.LogTrivial("[FireyCallouts][Log] Initialising 'Structural Fire' callout.");

            int decision;
            float offsetx, offsety, offsetz;
            float m, b;
            int a1, a2;

            // Check locations around 800f to the player
            List<Vector3[]> possibleLocations = new List<Vector3[]>();
            /*
            foreach (Vector3[] lo in locations) {
                Vector3 l = lo[0];
                if (l.DistanceTo(Game.LocalPlayer.Character.GetOffsetPosition(Vector3.RelativeFront)) < 800f || true) {
                    possibleLocations.Add(lo);
                }
            }

            if (possibleLocations.Count < 1) {
                Game.LogTrivial("[FireyCallouts][Log] Abort 'Dumpster Fire' callout. player too far away from all locations.");
                return AbortCallout();
            }
            */
            possibleLocations = locations;

            // Random location for the fire
            int chosenLocation = mrRandom.Next(0, possibleLocations.Count);
            spawnPoint = possibleLocations[chosenLocation][0];

            /*
            // Solve for all linear equations constructing the building borders
            foreach (Tuple<int, int> tup in calculationMatrix[chosenLocation]) {
                a1 = tup.Item1;
                a2 = tup.Item2;

                m = (possibleLocations[chosenLocation][a1].Y - possibleLocations[chosenLocation][a2].Y) / 
                    (possibleLocations[chosenLocation][a1].X - possibleLocations[chosenLocation][a2].X);
                b = possibleLocations[chosenLocation][a1].Y - (m * possibleLocations[chosenLocation][a1].X);

                mbMatrix.Add(Tuple.Create(m, b));
            }
            */

            ShowCalloutAreaBlipBeforeAccepting(spawnPoint, 30f);
            AddMinimumDistanceCheck(40f, spawnPoint);

            CalloutMessage = "Structural Fire";
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
                //fire = NativeFunction.CallByName<uint>("START_SCRIPT_FIRE", spawnPoint.X + offsetx, spawnPoint.Y + offsety + 0.1f, spawnPoint.Z + offsetz, 25, true);
                fire = NativeFunction.Natives.StartScriptFire<uint>(spawnPoint.X + offsetx, spawnPoint.Y + offsety, spawnPoint.Z + offsetz, 25, true);

                fireList.Add(fire);
            }

            /*
            if (Utils.gamemode == Utils.Gamemodes.Pol) {
                // create Suspect
                suspect = new Ped(spawnPoint.Around(1f));
                suspect.IsFireProof = true;
                suspect.IsPersistent = true;
                suspect.BlockPermanentEvents = true;
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
            */

            Functions.PlayScannerAudioUsingPosition("ASSISTANCE_REQUIRED IN_OR_ON_POSITION", spawnPoint);
            Functions.PlayScannerAudio("UNITS_RESPOND_CODE_03");

            Game.DisplayNotification("web_lossantospolicedept",
                                     "web_lossantospolicedept",
                                     "~y~FireyCallouts",
                                     "~r~Structur on fire",
                                     "~w~Someone reported a strcutrual fire. Respond ~r~Code 3");

            return base.OnBeforeCalloutDisplayed();
        }

        public bool AbortCallout() {
            Game.LogTrivial("[FireyCallouts][Log] Clean up 'Structural Fire' callout.");

            // Clean up if not accepted
            if (suspect.Exists()) suspect.Delete();
            if (locationBlip.Exists()) locationBlip.Delete();

            foreach (uint f in fireList) {
                NativeFunction.Natives.RemoveScriptFire(f);
                //NativeFunction.CallByName<uint>("REMOVE_SCRIPT_FIRE", f);
            }

            // Remove emergency services
            foreach (Vehicle ev in emergencyVehicles) {
                if (ev.Exists()) { ev.Delete(); }
            }

            Game.LogTrivial("[FireyCallouts][Log] Cleaned up 'Structural Fire' callout.");
            return false;
        }

        public override bool OnCalloutAccepted() {
            Game.LogTrivial("[FireyCallouts][Log] Accepted 'Structural Fire' callout.");

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
            Game.LogTrivial("[FireyCallouts][Log] Not accepted 'Structural Fire' callout.");

            // Clean up if not accepted
            if (suspect.Exists()) suspect.Delete();
            if (locationBlip.Exists()) locationBlip.Delete();

            foreach (uint f in fireList) {
                NativeFunction.Natives.RemoveScriptFire(f);
                //NativeFunction.CallByName<uint>("REMOVE_SCRIPT_FIRE", f);
            }

            // Remove emergency services
            foreach (Vehicle ev in emergencyVehicles) {
                if (ev.Exists()) { ev.Delete(); }
            }

            base.OnCalloutNotAccepted();
            Game.LogTrivial("[FireyCallouts][Log] Cleaned up 'Structural Fire' callout.");
        }

        public override void Process() {
            base.Process();

            GameFiber.StartNew(delegate {

                /*
                if (Utils.gamemode == Utils.Gamemodes.Pol) {
                    if (suspect.Exists() && suspect.DistanceTo(Game.LocalPlayer.Character.GetOffsetPosition(Vector3.RelativeFront)) < 40f) {
                        suspect.KeepTasks = true;
                        if (locationBlip.Exists()) locationBlip.Delete();
                        GameFiber.Wait(2000);
                    }

                    NativeFunction.CallByName<uint>("TASK_REACT_AND_FLEE_PED", suspect);

                    if (!pursuitCreated && suspect.DistanceTo(Game.LocalPlayer.Character.GetOffsetPosition(Vector3.RelativeFront)) < 30f) {
                        pursuit = Functions.CreatePursuit();
                        Functions.AddPedToPursuit(pursuit, suspect);
                        Functions.SetPursuitIsActiveForPlayer(pursuit, true);
                        pursuitCreated = true;
                    }
                */
                if (false) {
                    // ...
                } else {
                    if (locationBlip.Exists() && locationBlip.DistanceTo(Game.LocalPlayer.Character.GetOffsetPosition(Vector3.RelativeFront)) < 40f) {
                        if (locationBlip.Exists()) locationBlip.Delete();
                        //CallBackup();
                        GameFiber.Wait(2000);
                    }
                }

                if (Game.LocalPlayer.Character.IsDead) End();
                if (Game.IsKeyDown(Initialization.endKey)) { endKeyPressed = true; End(); }
                if (suspect.Exists()) { if (suspect.IsDead) End(); }
                if (suspect.Exists()) { if (Functions.IsPedArrested(suspect)) End(); }
            }, "StructuralFire [FireyCallouts]");
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

            /*
            // Remove emergency services
            foreach (Vehicle ev in emergencyVehicles) {
                if (ev.Exists()) { ev.Dismiss(); }
            }
            */

            Functions.PlayScannerAudio("WE_ARE_CODE_4");

            base.End();
            Game.LogTrivial("[FireyCallouts][Log] Cleaned up 'Structural Fire' callout.");
        }

        public void CallBackup() {
            Game.LogTrivial("[FireyCallouts][Log] Spawning other emergency vehicles (Backup).");

            int emx;
            Vehicle emVehicle;

            for (int em = 0; em < 7; em++) {
                emx = em % 3;
                switch (emx) {
                    case 0: {
                            // Call Fire dept
                            emVehicle = Functions.RequestBackup(spawnPoint, LSPD_First_Response.EBackupResponseType.Code3, LSPD_First_Response.EBackupUnitType.LocalUnit);
                            break;
                        }
                    case 1: {
                            // Call ambulance
                            emVehicle = Functions.RequestBackup(spawnPoint, LSPD_First_Response.EBackupResponseType.Code3, LSPD_First_Response.EBackupUnitType.Firetruck);
                            break;
                        }
                    case 2: {
                            // Call ambulance
                            emVehicle = Functions.RequestBackup(spawnPoint, LSPD_First_Response.EBackupResponseType.Code3, LSPD_First_Response.EBackupUnitType.Ambulance);
                            break;
                        }
                    default: {
                            // Call Fire dept
                            emVehicle = Functions.RequestBackup(spawnPoint, LSPD_First_Response.EBackupResponseType.Code3, LSPD_First_Response.EBackupUnitType.Firetruck);
                            break;
                        }
                }
                emergencyVehicles.Add(emVehicle);
            }
        }

    }
}
