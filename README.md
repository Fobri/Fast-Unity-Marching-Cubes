# Unity Marching Cubes Implementation
This is my implementation of destructible infinite marching cubes terrain. Made in Unity utilizing latest Unity features, like collider baking on separate threads and setting raw mesh buffers.

Code is not that well documented but it gets the job done. Infinite terrain generation is currently not as fast as I would like it to be, since pooled chunks need to wait out all of their jobs before they can be used again. Terrain destruction is very fast, since all the colliders are baked on separate threads using Physics.BakeMesh. Everything is pooled so no memory (or very little) gets allocated or freed on runtime.

I'm currently researching faster collision detection. I'd also like to be able to instantly stop a job instead of having to wait for it to speed up the infinite terrain part.

Render distance above 500 is very slow and causes random crashes for me. This is because all the chunks are currently full GameObjects. I'd like to render meshes outside of the player's destruction range manually, but I didn't find a way to do it yet without having too many draw calls and abysmall performance. Next thing would be to experiment with CommandBuffer.DrawProceduralIndirect or some other alternative.

Feel free to fork the project and submit a pull request if you manage to optimize it even further :)
Parts of the code are taken from https://github.com/Eldemarkki/Marching-Cubes-Terrain (those parts are mentioned in the comments)
