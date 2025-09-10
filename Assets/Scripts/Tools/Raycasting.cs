using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Assets.Scripts.Tools
{
    static class Raycasting
    {
        public static RaycastHit[] BatchRaycast(RaycastCommand[] commands)
        {
            if (commands.Length < 100)
            {
                return RaycastLinear(commands);
            }

            // Create a NativeArray for the commands
            using NativeArray<RaycastCommand> commandArray = new NativeArray<RaycastCommand>(commands, Allocator.TempJob);

            // Create a NativeArray for the results
            using NativeArray<RaycastHit> resultArray = new NativeArray<RaycastHit>(commands.Length, Allocator.TempJob);

            // Schedule the job and complete it
            JobHandle rayHandle = RaycastCommand.ScheduleBatch(commandArray, resultArray, 1);
            rayHandle.Complete();

            // Convert the NativeArray results to a regular C# array
            RaycastHit[] results = new RaycastHit[commands.Length];
            resultArray.CopyTo(results);

            return results;
        }

        private static RaycastHit[] RaycastLinear(RaycastCommand[] commands)
        {
            RaycastHit[] results = new RaycastHit[commands.Length];

            for (int i = 0; i < commands.Length; i++)
            {
                Physics.Raycast(commands[i].from, commands[i].direction, out RaycastHit hit);
                results[i] = hit;
            }

            return results;
        }
    }
}