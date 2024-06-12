using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Assets.Scripts.Tools
{
    static class Raycasting
    {
        static private int size = 1000;

        static private NativeArray<RaycastHit> resultArray = new NativeArray<RaycastHit>(size, Allocator.Persistent);
        static private NativeArray<RaycastCommand> commandArray = new NativeArray<RaycastCommand>(size, Allocator.Persistent);

        public static RaycastHit[] BatchRaycast(RaycastCommand[] commands)
        {
            if (commands.Length < 100)
            {
                return RaycastLinear(commands);
            }

            if (size < commands.Length)
            {
                resultArray.Dispose();
                commandArray.Dispose();
                size = commands.Length;
                resultArray = new NativeArray<RaycastHit>(size, Allocator.TempJob);
                commandArray = new NativeArray<RaycastCommand>(size, Allocator.TempJob);
            }

            // Set up raycasts
            for (int i = 0; i < commands.Length; i++)
            {
                commandArray[i] = commands[i];
            }
            // Send them off
            JobHandle rayHandle = RaycastCommand.ScheduleBatch(commandArray, resultArray, 1);
            rayHandle.Complete();

            return resultArray.GetSubArray(0, commands.Length).ToArray();
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
