﻿using System;
using System.Diagnostics;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

public class WireClass
{
    BlockEntity block1, block2;
    ICoreClientAPI api;
    MeshData wiremesh = new MeshData();

    public WireClass(ICoreAPI api, BlockEntity block1, BlockEntity block2)
    {
        if (api is ICoreClientAPI)
        {
            this.api = (ICoreClientAPI)api;
            this.block1 = block1;
            this.block2 = block2;
            loadMesh();
        }
    }

    void loadMesh()
    {
        Vec3f origin = block1.Pos.ToVec3f() - block2.Pos.ToVec3f();

        api.Tesselator.TesselateBlock(api.World.GetBlock(new AssetLocation("temporalengineering:wire")), out wiremesh);


        wiremesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 1 * origin.Length(), 1, 1);

        float radZ = (float)Math.Atan2(origin.Y, Math.Sqrt(origin.X * origin.X + origin.Z * origin.Z));
        wiremesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, -(float)Math.Atan2(origin.Z, origin.X), radZ);

        origin = origin / 2;
        wiremesh.Translate(-origin.X, -origin.Y, -origin.Z);
    }

    public void OnPointRemoved(BlockEntity block)
    {
        if (block == block1) ((IWirePoint)block2)?.RemoveWire((IWirePoint)block1);
        else ((IWirePoint)block1)?.RemoveWire((IWirePoint)block2);
        api = null;
    }

    public void OnTesselation(ITerrainMeshPool mesher, BlockEntity block)
    {
        if (block != block1) return;

        if (wiremesh != null) mesher.AddMeshData(wiremesh);
    }
}