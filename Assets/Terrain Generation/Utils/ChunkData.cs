using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Unity.Jobs;
using UnityEngine.Rendering;
using System;
using Unity.Collections.LowLevel.Unsafe;

namespace WorldGeneration
{
    public class ChunkData : MonoBehaviour
    {
        //Native collections for mesh and density data used in jobs.
        NativeArray<Vector3> buffer;
        NativeArray<int> indexes;
        NativeArray<float> noiseMap;

        //Chunk's position
        float3 pos;

        //Marching cubes job handle
        JobHandle myHandle;
        //Is marching cubes job currently running
        bool isBeingProcessed;

        //Collider baking handle
        JobHandle colliderHandle;
        //Is a collider being currently baked
        bool meshBaking = false;
        
        //This chunk's mesh
        Mesh myMesh;
        //This chunk's mesh filter
        MeshFilter filter;

        //Thread safe counter to keep track of vertex index from https://github.com/Eldemarkki/Marching-Cubes-Terrain
        Counter _counter;

        //Chunk's size
        public int size;
        //This is needed after mesh.SetIndexBuffer data to make the mesh visible, for some reason
        SubMeshDescriptor desc = new SubMeshDescriptor();
        
        bool isNewChunk = true;
        //Has chunk been modified since last frame
        bool needsUpdate = false;

        //Variables passed chunk modify job
        int3 newExplosionSpot;
        float explosionRange;
        float explosionValue;

        //Simulate an explosion at a point for this chunk
        public void Explode(int3 worldPos, float explosionRange, float explosionValue)
        {
            newExplosionSpot = worldPos - (int3)pos;//(worldPos - (int3)pos).Mod(size + 1);
            this.explosionRange = explosionRange;
            this.explosionValue = explosionValue;
            needsUpdate = true;
        }
        //Set density at given index immediatly. Chunk will start an update at LateUpdate after possible current jobs are done.
        public void SetDensity(int3 localPos, float density)
        {
            var size = this.size + 1;
            noiseMap[localPos.z + localPos.y * size + localPos.x * size * size] = density;
            needsUpdate = true;
        }
        private void OnEnable()
        {
            if (isNewChunk)
            {
                NewChunk(size, transform.position);
                isNewChunk = false;
            }
            else
            {
                myHandle.Complete();
                myMesh.Clear();
                ChunkUpdate(transform.position);
            }
            WorldBase.currentChunks.Add(pos, this);
        }
        private void OnDisable()
        {
            //myHandle.Complete();
            isBeingProcessed = false;
            
            WorldBase.currentChunks.Remove(pos);
            WorldBase.freeChunks.Enqueue(this);
        }
        private void OnApplicationQuit()
        {
            buffer.Dispose();
            indexes.Dispose();
            noiseMap.Dispose();
        }

        private void LateUpdate()
        {
            if (isBeingProcessed)
            {
                if (myHandle.IsCompleted)
                {
                    isBeingProcessed = false;
                    myHandle.Complete();
                    UpdateChunk();
                }
            }
            else if(needsUpdate)
            {
                NoiseMapExplosion();
                needsUpdate = false;
            }
            else if(colliderHandle.IsCompleted && meshBaking)
            {
                colliderHandle.Complete();
                ApplyCollider();
                meshBaking = false;
            }
        }

        void NewChunk(int size, Vector3 pos)
        {
            this.size = size;
            transform.position = (float3)pos;
            this.pos = pos;
            var arraySize = size * size * size;
            buffer = new NativeArray<Vector3>(arraySize * 3 * 5, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            indexes = new NativeArray<int>(arraySize * 3 * 5, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            noiseMap = new NativeArray<float>((size + 1) * (size + 1) * (size + 1), Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            myMesh = new Mesh();
            filter = GetComponent<MeshFilter>();
            filter.sharedMesh = myMesh;
            
            desc.topology = MeshTopology.Triangles;
            MarchChunk();
        }

        void ChunkUpdate(float3 pos)
        {
            this.pos = pos;
            MarchChunk();
        }

        void UpdateChunk()
        {
            var layout = new[]
                {
                    new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
                };
            var vertexCount = _counter.Count * 3;
            if (vertexCount > 0)
            {
                var fuck = size / 2;
                myMesh.bounds = new Bounds(new Vector3(fuck, fuck, fuck), new Vector3(size, size, size));
                //Set vertices and indices
                myMesh.SetVertexBufferParams(vertexCount, layout);
                myMesh.SetVertexBufferData(buffer, 0, 0, vertexCount, 0, MeshUpdateFlags.DontValidateIndices);
                myMesh.SetIndexBufferParams(vertexCount, IndexFormat.UInt32);
                myMesh.SetIndexBufferData(indexes, 0, 0, vertexCount, MeshUpdateFlags.DontValidateIndices);

                desc.indexCount = vertexCount;
                myMesh.SetSubMesh(0, desc, MeshUpdateFlags.DontValidateIndices);

                myMesh.RecalculateNormals();
                filter.sharedMesh = myMesh;
                //Start collider baking
                var colliderJob = new ChunkColliderBakeJob()
                {
                    meshId = myMesh.GetInstanceID()
                };
                colliderHandle = colliderJob.Schedule();
                meshBaking = true;
            }
            else
            {
                myMesh.Clear();
            }
            transform.position = pos;
            
            _counter.Dispose();
        }
        //Collider baked, set it to the chunk
        void ApplyCollider()
        {
            GetComponent<MeshCollider>().sharedMesh = myMesh;
        }
        void MarchChunk()
        {
            _counter = new Counter(Allocator.Persistent);
            var arraySize = size * size * size;

            var noiseJob = new NoiseJob()
            {
                ampl = WorldBase.noiseData.ampl,
                freq = WorldBase.noiseData.freq,
                oct = WorldBase.noiseData.oct,
                offset = pos,
                seed = WorldBase.noiseData.offset,
                surfaceLevel = WorldBase.noiseData.surfaceLevel,
                noiseMap = noiseMap,
                size = size + 1
                //pos = WorldSetup.positions
            };
            var noiseHandle = noiseJob.Schedule((size + 1) * (size + 1) * (size + 1), 64);

            
            
            var marchingJob = new MarchingJob()
            {
                densities = noiseMap,
                isolevel = 0f,
                chunkSize = size,
                triangles = indexes,
                vertices = buffer,
                counter = _counter

            };
            var marchinJob = marchingJob.Schedule(arraySize, 32, noiseHandle);
            myHandle = marchinJob;
            isBeingProcessed = true;
        }
        void RefreshChunkMesh()
        {
            _counter = new Counter(Allocator.Persistent);
            var marchingJob = new MarchingJob()
            {
                densities = noiseMap,
                isolevel = 0f,
                chunkSize = size,
                triangles = indexes,
                vertices = buffer,
                counter = _counter

            };
            var marchinJob = marchingJob.Schedule(size * size * size, 32);
            myHandle = marchinJob;
            isBeingProcessed = true;
        }
        void NoiseMapExplosion()
        {
            _counter = new Counter(Allocator.Persistent);
            var noiseUpdateJob = new ChunkExplodeJob()
            {
                size = size + 1,
                explosionOrigin = newExplosionSpot,
                explosionRange = explosionRange,
                newDensity = explosionValue,
                noiseMap = noiseMap
            };
            var handl = noiseUpdateJob.Schedule((size + 1) * (size + 1) * (size + 1), 64);

            var marchingJob = new MarchingJob()
            {
                densities = noiseMap,
                isolevel = 0f,
                chunkSize = size,
                triangles = indexes,
                vertices = buffer,
                counter = _counter

            };
            var marchinJob = marchingJob.Schedule(size * size * size, 32, handl);
            myHandle = marchinJob;
            isBeingProcessed = true;
        }
    }
}