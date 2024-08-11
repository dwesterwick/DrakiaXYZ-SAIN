﻿using System.Collections.Generic;
using UnityEngine;

namespace SAIN.Components
{
    public class VectorsDistancesData : AbstractBatchJob<DistanceData>
    {
        public int ScheduleCalcDistanceBetweenVectors(Vector3[] vectors)
        {
            if (!base.CanBeScheduled()) {
                return 0;
            }
            int count = vectors.Length - 1;
            if (count < 0) {
                return 0;
            }
            base.SetupJob(count);
            for (int i = 0; i < count; i++) {
                Datas[i].UpdateData(vectors[i], vectors[i + 1]);
            }
            return count;
        }

        public int ScheduleCalcDistanceToPoints(Vector3[] vectors, Vector3 origin)
        {
            if (!base.CanBeScheduled()) {
                return 0;
            }
            int count = vectors.Length;
            if (count < 0) {
                return 0;
            }
            base.SetupJob(count);
            for (int i = 0; i < count; i++) {
                Datas[i].UpdateData(origin, vectors[i]);
            }
            return count;
        }
    }
}