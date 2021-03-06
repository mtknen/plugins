﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CodeHatch.Blocks.Networking.Events;
using CodeHatch.Blocks.Networking.Events.Local;
using CodeHatch.Engine.Networking;
using CodeHatch.Common;
using CodeHatch.ItemContainer;
using CodeHatch.Networking.Events;
using CodeHatch.Networking.Events.Entities;
using CodeHatch.Thrones.Taunts;
using CodeHatch.UserInterface.Dialogues;
using Oxide.Core;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Warp Shrine", "Scorpyon", "1.0.5")]
    public class WarpShrine : ReignOfKingsPlugin
    {
        #region SERVER VARIABLES (MODIFIABLE)

        private const int _warpCoolDownTime = 300; // Time taken until warp can be used again (in seconds)
        //private const int _warpCoolDownTime = 5; // Time taken until warp can be used again (in seconds)

        #endregion

        #region Save and Load Data Methods

        // SAVE DATA ===============================================================================================
        private void LoadWarpData()
        {
            _warpList = Interface.GetMod().DataFileSystem.ReadObject<Dictionary<string, float[]>>("SavedWarpLocations");
            _warpCoolDown = Interface.GetMod().DataFileSystem.ReadObject<Dictionary<ulong, int>>("SavedWarpCoolDown");
        }

        private void SaveWarpData()
        {
            Interface.GetMod().DataFileSystem.WriteObject("SavedWarpLocations", _warpList);
            Interface.GetMod().DataFileSystem.WriteObject("SavedWarpCoolDown", _warpCoolDown);
        }



        void Loaded()
        {
            LoadWarpData();
            SaveWarpData();
            timer.Repeat(1, 0, WarpShrineTimer);
        }

        private void WarpShrineTimer()
        {
            //Cooldown timer
            var updateList = new Collection<ulong>();
            foreach (var person in _warpCoolDown)
            {
                updateList.Add(person.Key);
            }

            foreach (var playerId in updateList)
            {
                var currentTime = _warpCoolDown[playerId];
                currentTime--;
                _warpCoolDown[playerId] = currentTime;
                if(currentTime <= 0) _warpCoolDown.Remove(playerId);
            }
            
        }

        private bool WarpCoolDownExistsFor(Player player)
        {
            if (_warpCoolDown.ContainsKey(player.Id)) return true;
            return false;
        }

        // ===========================================================================================================

        #endregion

        #region SERVER VARIABLES (DO NOT MODIFY)

        // ID = Location ID; float[] coordinates (x, z)
        private Dictionary<string, float[]> _warpList = new Dictionary<string, float[]>();
        private Dictionary<ulong, int> _warpCoolDown = new Dictionary<ulong, int>();

        #endregion

        #region PLAYER COMMANDS

        //// Add current location to the warp list
        //[ChatCommand("platform")]
        //private void AddPlatform(Player player, string cmd, string[] input)
        //{
        //    CreateNewPlatform(player, player.Entity.Position, 5, 5);
        //}

        // Add current location to the warp list
        [ChatCommand("addwarplocation")]
        private void AddMyLocation(Player player, string cmd, string[] input)
        {
            AddThisLocationToWarpList(player, cmd, input);
        }

        // Add current location to the warp list
        [ChatCommand("removewarplocation")]
        private void RemoveMyLocation(Player player, string cmd, string[] input)
        {
            RemoveThisLocationFromTheWarpList(player, cmd, input);
        }

        //// Get current location
        //[ChatCommand("location")]
        //private void GetMyLocation(Player player, string cmd)
        //{
        //    GetCurrentLocation(player, cmd);
        //}

        // Get current location
        //[ChatCommand("telereset")]
        //private void GoTobase(Player player, string cmd)
        //{
        //    var newPos = new Vector3(200, 50, 0);
        //    EventManager.CallEvent((BaseEvent)new TeleportEvent(player.Entity, newPos));
        //}

        // Get current location
        [ChatCommand("warp")]
        private void CommenceWarpSpeed(Player player, string cmd)
        {
            if (WarpCoolDownExistsFor(player))
            {
                if (_warpCoolDown[player.Id] > 0)
                {
                    PrintToChat(player, "The Warp Core System is currently cooling down. Please try again after " + _warpCoolDown[player.Id] + " seconds.");
                    return;
                }
            }
            WarpPlayerToShrine(player, cmd);
        }

        #endregion

#region PRIVATE METHODS

        void CreateNewPlatform(Player player, Vector3 startPosition, int width, int length)
        {
            byte material = 2;
            Quaternion rotation = new Quaternion(0.0f, 0.0f, 0.0f, 0.0f);
            byte prefabId = 0;

            Vector3Int playerPos = (Vector3Int)startPosition;
            var teleEvent = new TeleportEvent(player.Entity, playerPos);
            EventManager.CallEvent((BaseEvent)teleEvent);

            int setX = playerPos.x - (width / 2);
            setX = (int) (setX - (double) (setX/6));
            int setY = playerPos.y;
            setY = (int) (setY - (double) (setY/6) - 1);
            int setZ = playerPos.z - (length / 2);
            setZ = (int) (setZ - (double) (setZ/6));

            var originX = setX;
            var originY = setY;
            var originZ = setZ;

            for (var i = 0; i < width; i++)
            {
                for (var ii = 0; ii < length; ii++)
                {
                    BuildBlockOnThisSquare(setX, setY, setZ, material, rotation, prefabId);
                    setX++;
                }
                setX = originX;
                setZ++;
            }

            originX = originX + (width / 2);
            originZ = originZ + (length / 2);

            BuildBlockOnThisSquare(originX - 2, originY + 1, originZ + 1, material, rotation, prefabId);
            BuildBlockOnThisSquare(originX - 2, originY + 1, originZ + 2, material, rotation, prefabId);
            BuildBlockOnThisSquare(originX - 1, originY + 1, originZ + 2, material, rotation, prefabId);
            BuildBlockOnThisSquare(originX, originY + 1, originZ + 2, material, rotation, prefabId);
            BuildBlockOnThisSquare(originX + 1, originY + 1, originZ + 2, material, rotation, prefabId);
            BuildBlockOnThisSquare(originX + 2, originY + 1, originZ + 2, material, rotation, prefabId);
            BuildBlockOnThisSquare(originX + 2, originY + 1, originZ + 1, material, rotation, prefabId);

            BuildBlockOnThisSquare(originX - 1, originY + 2, originZ + 2, material, rotation, prefabId);
            BuildBlockOnThisSquare(originX, originY + 2, originZ + 2, material, rotation, prefabId);
            BuildBlockOnThisSquare(originX + 1, originY + 2, originZ + 2, material, rotation, prefabId);


            // Tele the player over the platform (to prevent getting stuck
            EventManager.CallEvent((BaseEvent)new TeleportEvent(player.Entity, new Vector3(startPosition.x, startPosition.y + 1, startPosition.z)));

        }

        private static void BuildBlockOnThisSquare(int setX, int setY, int setZ, byte material, Quaternion rotation, byte prefabId)
        {
            var newPosition = new Vector3Int(setX, setY,setZ);
            var cubeEvent = new CubePlaceEvent(0, newPosition, material, rotation, prefabId, 0.0f);
            var localCubeEvent = new CubePlaceLocalEvent(cubeEvent, true);
            EventManager.CallEvent((BaseEvent) cubeEvent);
            EventManager.CallEvent((BaseEvent) localCubeEvent);
        }

        #region LOCATION

        private void RemoveThisLocationFromTheWarpList(Player player, string cmd, string[] input)
        {
            if (!player.HasPermission("admin"))
            {
                PrintToChat(player, "Only admins can add warp locations.");
                return;
            }

            if (input.Length < 1)
            {
                PrintToChat(player, "Usage: /addlocation <Name>");
                return;
            }

            var locName = input.JoinToString(" ").ToLower();

            if (!_warpList.ContainsKey(locName))
            {
                PrintToChat(player, "That warp location doesn't appear to exist in the list.");
                return;
            }
            _warpList.Remove(locName);
            PrintToChat(player, "The warp location: " + locName + "  has been removed from the list of warp destinations.");

            SaveWarpData();
        }

        private void AddThisLocationToWarpList(Player player, string cmd, string[] input)
        {
            if (!player.HasPermission("admin"))
            {
                PrintToChat(player, "Only admins can add warp locations.");
                return;
            }

            if (input.Length < 1)
            {
                PrintToChat(player, "Usage: /addlocation <Name>");
                return;
            }

            var locName = input.JoinToString(" ").ToLower();

            if (_warpList.ContainsKey(locName.ToLower()))
            {
                PrintToChat(player, "A warp location with that name already exists!");
                return;
            }

            _warpList.Add(locName, new float[] { player.Entity.Position.x, player.Entity.Position.y, player.Entity.Position.z });
            PrintToChat(player, "The warp location has been added to the lisp of warp areas and a shrine area has been created.");
            CreateNewPlatform(player, player.Entity.Position, 5, 5);

            SaveWarpData();
        }

        private void GetCurrentLocation(Player player, string cmd)
        {
            if (!player.HasPermission("admin"))
            {
                PrintToChat(player, "For now, only admins can check locations.");
                return;
            }
            PrintToChat(player, string.Format("Current Location: x:{0} y:{1} z:{2}", player.Entity.Position.x.ToString(), player.Entity.Position.y.ToString(), player.Entity.Position.z.ToString()));
        }


        private void WarpPlayerToShrine(Player player, string cmd)
        {
            if (_warpList.Count < 1)
            {
                PrintToChat(player,"There are no warp destinations currently set.");
                return;
            }

            if (!InWarpShrineLocation(player))
            {
                PrintToChat(player, "You are not currently at a Warp Shrine.");
                return;
            }

            var message = "Current available warp destinations:\n";

            // Open a popup with the resource details
            foreach (var warp in _warpList)
            {
                message = message + Capitalise(warp.Key) + "\n";
            }
            message = message + "\n\n Where would you like to travel to?";

            player.ShowInputPopup("Warp Shrine", message, "", "Submit", "Cancel", (options, dialogue1, data) => WarpPlayerToShrineConfirm(player, options, dialogue1, data));
        }

        private bool InWarpShrineLocation(Player player)
        {
            if (_warpList.Count < 1) return false;

            foreach (var warp in _warpList)
            {
                var posX = warp.Value[0];
                var posY = warp.Value[1];
                var posZ = warp.Value[2];

                var playerX = player.Entity.Position.x;
                var playerY = player.Entity.Position.y;
                var playerZ = player.Entity.Position.z;

                if (playerX > posX - 2 && playerX < posX + 2)
                {
                    if (playerY > posY - 2 && playerY < posY + 2)
                    {
                        if (playerZ > posZ - 2 && playerZ < posZ + 2) return true;
                    }
                }
            }

            return false;
        }


        private void WarpPlayerToShrineConfirm(Player player, Options selection, Dialogue dialogue, object contextData)
        {

            if (selection == Options.Cancel)
            {
                //Leave
                return;
            }

            var locName = dialogue.ValueMessage.ToLower();
            if (!_warpList.ContainsKey(locName))
            {
                return;
            }

            var locCoords = _warpList[locName];
            var posX = locCoords[0];
            var posY = locCoords[1];
            var posZ = locCoords[2];

            var newPos = new Vector3(posX,posY + 2,posZ);
            EventManager.CallEvent((BaseEvent)new TeleportEvent(player.Entity, newPos));
//            EventManager.CallEvent((BaseEvent)new TeleportEvent(player.Entity, Lerp(player.Entity.Position, newPos)));

            // Set the warp cool down timer
            if (!WarpCoolDownExistsFor(player))
            {
                _warpCoolDown.Add(player.Id, _warpCoolDownTime);
            }

            SaveWarpData();
        }

        #endregion

        #region CUBE DAMAGE
        private void OnCubeTakeDamage(CubeDamageEvent cubeDamageEvent)
        {
            var player = cubeDamageEvent.Damage.DamageSource.Owner;
            var sourceName = cubeDamageEvent.Damage.Damager.name.ToLower();
            bool trebuchet = sourceName.Contains("trebuchet");
            bool ballista = sourceName.Contains("ballista");

            cubeDamageEvent.Damage.Amount = 0f;
            cubeDamageEvent.Damage.ImpactDamage = 0f;
            cubeDamageEvent.Damage.MiscDamage = 0f;

            //cubeDamageEvent.Cancel("This area is protected!");
            PrintToChat(player, "Warning! This area is protected! Such actions will incur a fine!");
            if (!trebuchet && !ballista)
            {
                var inventory = player.GetInventory().Contents;
                var woodRemoveAmount = 6;
                var stoneRemoveAmount = 6;

                // Make sure to take the harvested materials back off the player
                if (CanRemoveResource(player, "Wood", woodRemoveAmount))
                {
                    foreach (InvGameItemStack item in inventory.Where(item => item != null))
                    {
                        if (item.Name == "Wood" && item.StackAmount >= woodRemoveAmount)
                        {
                            inventory.SplitItem(item, woodRemoveAmount, true);
                            break;
                        }
                    }
                }
                PrintToChat("trying to remove stone");
                if (CanRemoveResource(player, "Stone", stoneRemoveAmount))
                {
                    PrintToChat("looking for stone");
                    foreach (InvGameItemStack item in inventory.Where(item => item != null))
                    {
                        if (item.Name == "Stone" && item.StackAmount >= stoneRemoveAmount)
                        {
                            PrintToChat("stone found");
                            inventory.SplitItem(item, stoneRemoveAmount, true);
                            break;
                        }
                    }
                }
                cubeDamageEvent.Cancel("This area is protected!");
            }
        }

        private bool CanRemoveResource(Player player, string resource, int amount)
        {
            // Check player's inventory
            var inventory = player.CurrentCharacter.Entity.GetContainerOfType(CollectionTypes.Inventory);

            // Check how much the player has
            var foundAmount = 0;
            foreach (var item in inventory.Contents.Where(item => item != null))
            {
                if (item.Name.ToLower() == resource.ToLower())
                {
                    foundAmount = foundAmount + item.StackAmount;
                }
            }

            if (foundAmount >= amount) return true;
            return false;
        }
        #endregion

        #region UTILITY METHODS
        // Capitalise the Starting letters
        private string Capitalise(string word)
        {
            var finalText = "";
            finalText = Char.ToUpper(word[0]).ToString();
            var spaceFound = 0;
            for (var i = 1; i < word.Length; i++)
            {
                if (word[i] == ' ')
                {
                    spaceFound = i + 1;
                }
                if (i == spaceFound)
                {
                    finalText = finalText + Char.ToUpper(word[i]).ToString();
                }
                else finalText = finalText + word[i].ToString();
            }
            return finalText;
        }
        #endregion
#endregion
    }
}
