﻿using BepInEx.Logging;
using Comfort.Common;
using EFT;
using SAIN.Components;
using UnityEngine;

namespace SAIN
{
    public abstract class SAINControl
    {
        public void Awake()
        {
            BotController = GameWorld.GetOrAddComponent<SAINBotController>();
        }

        public SAINControl()
        {
            Logger = BepInEx.Logging.Logger.CreateLogSource("SAIN Bot Controller");
        }

        public ManualLogSource Logger { get; private set; }
        public LineOfSightManager LineOfSightManager => BotController.LineOfSightManager;
        public SAINBotController BotController { get; private set; }
        public GameWorld GameWorld => Singleton<GameWorld>.Instance;
    }
}
