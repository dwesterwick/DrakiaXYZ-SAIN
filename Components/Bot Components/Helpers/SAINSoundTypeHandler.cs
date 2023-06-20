﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SAIN.Classes;
using SAIN.Components;
using EFT;
using BepInEx.Logging;

namespace SAIN.Components.Helpers
{
    public class SAINSoundTypeHandler
    {
        protected static ManualLogSource Logger;
        protected static SAINBotController BotController => SAINPlugin.BotController;

        public static void AISoundPlayer(string sound, Player player)
        {
            if (Logger == null)
            {
                Logger = BepInEx.Logging.Logger.CreateLogSource("SAIN: Sound Manager");
            }
            if (BotController == null || BotController.SAINBots.Count == 0)
            {
                return;
            }

            SAINSoundType soundType = SAINSoundType.None;
            var Item = player.HandsController.Item;
            float soundDist = 0f;

            if (Item != null)
            {
                //Logger.LogInfo($"Item: {Item.GetType().Name} : Sound {sound}");
                if (Item is GrenadeClass)
                {
                    if (sound == "Pin")
                    {
                        //Logger.LogWarning("Grenade Pin!");
                        soundType = SAINSoundType.GrenadePin;
                        soundDist = 15f;
                    }
                    if (sound == "Draw")
                    {
                        //Logger.LogWarning("Grenade Draw!");
                        soundType = SAINSoundType.GrenadeDraw;
                        soundDist = 15f;
                    }
                }
                else if (Item is MedsClass)
                {
                    if (sound == "CapRemove" || sound == "Inject")
                    {
                        //Logger.LogWarning("Heal!");
                        soundType = SAINSoundType.Heal;
                        soundDist = 15f;
                    }
                }
                else
                {
                    if (sound == "MagOut")
                    {
                        //Logger.LogWarning("Weapon Reload!");
                        soundType = SAINSoundType.Reload;
                        soundDist = 20f;
                    }
                }
            }
            if (soundType != SAINSoundType.None)
            {
                BotController.AISoundPlayed(soundType, player, soundDist);
            }
        }
    }
}
