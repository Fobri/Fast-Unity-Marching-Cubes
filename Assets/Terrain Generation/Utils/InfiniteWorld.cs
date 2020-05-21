using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace WorldGeneration
{
    //Infinite world logic
    public class InfiniteWorld : WorldBase
    {
        //How far should chunks be spawned.
        public float chunkDrawDistance;
        //Because foreach, we cant remove chunks from currentChunks straight away. Need to store the values to this list and remove after the loop
        List<float3> toRemove = new List<float3>();

        private void Start()
        {
            StartGenerate();
        }
        //Create new chunk objects at startup
        void StartGenerate()
        {
            int amount = Mathf.RoundToInt(chunkDrawDistance / (chunkSize));
            float rootPosX = Mathf.RoundToInt(player.position.x / (chunkSize)) * (chunkSize);
            float rootPosY = Mathf.RoundToInt(player.position.y / (chunkSize)) * (chunkSize);
            float rootPosZ = Mathf.RoundToInt(player.position.z / (chunkSize)) * (chunkSize);
            for (int x = -amount / 2; x < amount / 2; x++)
            {
                for (int y = -amount / 2; y < amount / 2; y++)
                {
                    for (int z = -amount / 2; z < amount / 2; z++)
                    {
                        var pos = new float3(rootPosX + x * (chunkSize), rootPosY + y * (chunkSize), rootPosZ + z * (chunkSize));
                        if (Vector3.Distance(pos, player.position + Vector3.up) < chunkDrawDistance / 2)
                            GenerateChunk(pos);
                    }
                }
            }
        }
        public override void UpdateChunks()
        {
            toRemove.Clear();
            
            foreach(var pos in currentChunks.Keys)
            {
                if (Vector3.Distance(pos, player.position + Vector3.up) > chunkDrawDistance / 2)
                {
                    toRemove.Add(pos);
                }
            }
            toRemove.ForEach(x => currentChunks[x].gameObject.SetActive(false));
            int amount = Mathf.RoundToInt(chunkDrawDistance / (chunkSize));
            float rootPosX = Mathf.RoundToInt(player.position.x / (chunkSize)) * (chunkSize);
            float rootPosY = Mathf.RoundToInt(player.position.y / (chunkSize)) * (chunkSize);
            float rootPosZ = Mathf.RoundToInt(player.position.z / (chunkSize)) * (chunkSize);
            for (int x = -amount / 2; x < amount / 2; x++)
            {
                for (int y = -amount / 2; y < amount / 2; y++)
                {
                    for (int z = -amount / 2; z < amount / 2; z++)
                    {
                        //If no chunks are pooled, don't do anything and wait for next frame instead. Could also be set to spawn new chunks, but that wasn't necessary
                        if (freeChunks.Count == 0)
                            return;
                        var pos = new float3(rootPosX + x * (chunkSize), rootPosY + y * (chunkSize), rootPosZ + z * (chunkSize));
                        //If there is a chunk at this position already, don't do anything
                        if (currentChunks.ContainsKey(pos))
                            continue;
                        //Check if chunk is close enough.
                        if (Vector3.Distance(pos, player.position + Vector3.up) < chunkDrawDistance / 2)
                        {
                            //Get pooled chunk from the queue.
                            var chumk = freeChunks.Dequeue();
                            chumk.gameObject.transform.position = pos;
                            chumk.gameObject.SetActive(true);
                        }
                    }
                }
            }
        }
    }
}