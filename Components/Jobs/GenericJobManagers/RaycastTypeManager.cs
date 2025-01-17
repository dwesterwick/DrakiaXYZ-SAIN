﻿using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace SAIN.Components
{
    public class RaycastTypeManager : JobTypeManager<GlobalRaycastJob, RaycastObject>
    {
        public RaycastTypeManager() : base(new GlobalRaycastJob())
        {
        }

        private readonly List<RaycastCommand> _commands = new List<RaycastCommand>();
        //private readonly List<RaycastHit> _hits = new List<RaycastHit>();

        public override void Complete()
        {
            if (!HasJobsToCheckComplete()) {
                return;
            }

            int count = Datas.Count;
            base.Complete();
            int completeCount = 0;
            NativeArray<RaycastHit> jobHits = JobContainer.Hits;
            int jobHitsCount = jobHits.Length;
            for (int i = 0; i < count; i++) {
                RaycastObject data = Datas[i];
                if (data.Status == EJobStatus.Scheduled) {
                    if (completeCount >= jobHitsCount) {
                        Logger.LogError("complete count more than job count?");
                        break;
                    }
                    data.Complete(jobHits[completeCount]);
                    completeCount++;
                }
            }
            //Logger.LogInfo($"{completeCount} : {count}");
            JobContainer.DisposeArrays();
        }

        public override void Schedule(JobHandle dependency)
        {
            if (!HasJobsToSchedule()) {
                return;
            }

            int count = Datas.Count;
            //Logger.LogInfo(count);
            _commands.Clear();
            //_hits.Clear();
            int scheduledCount = 0;
            for (int i = 0; i < count; i++) {
                RaycastObject data = Datas[i];
                if (data.Status == EJobStatus.UnScheduled) {
                    _commands.Add(data.Command);
                    //_hits.Add(data.Hit);
                    data.Status = EJobStatus.Scheduled;
                    scheduledCount++;
                }
            }
            //Logger.LogInfo(scheduledCount);
            if (scheduledCount > 0) {
                NativeArray<RaycastCommand> commandsArray = new NativeArray<RaycastCommand>(scheduledCount, Allocator.TempJob);
                for (int i = 0; i < scheduledCount; i++) {
                    commandsArray[i] = _commands[i];
                }
                NativeArray<RaycastHit> hitsArray = new NativeArray<RaycastHit>(scheduledCount, Allocator.TempJob);
                JobHandle handle = RaycastCommand.ScheduleBatch(commandsArray, hitsArray, 8);
                JobContainer.Init(handle, commandsArray, hitsArray, scheduledCount);
            }
        }
    }
}