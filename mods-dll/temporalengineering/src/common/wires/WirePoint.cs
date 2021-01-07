
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

public class BlockEntityWirePoint : BlockEntity, IWirePoint
{
    List<BlockPos> positions = new List<BlockPos>();
    public Dictionary<IWirePoint, WireClass> wiresList = new Dictionary<IWirePoint, WireClass>(0);

    public override void Initialize(ICoreAPI api)
    {
        base.Initialize(api);

        wiresList.Clear();
        foreach (BlockPos newPos in positions)
        {
            BlockEntityWirePoint block = api.World.BlockAccessor.GetBlockEntity(newPos) as BlockEntityWirePoint;
            if (block != null && !wiresList.ContainsKey(block))
            {
                WireClass wireClass;
                if (block.wiresList.TryGetValue(this, out wireClass))
                {
                    wiresList.Add(block, wireClass);
                }
                else
                {
                    wiresList.Add(block, new WireClass(api, this, block));
                }
            }
        }
    }

    public override void OnBlockRemoved()
    {
        base.OnBlockRemoved();

        foreach (var kv in wiresList)
        {
            kv.Value.OnPointRemoved(this);
        }
    }

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
    {
        base.FromTreeAttributes(tree, worldForResolving);

        positions.Clear();
        int id = 1;
        while (true)
        {
            BlockPos newPos = new BlockPos(tree.GetInt("wire-" + id + "-X", -1),tree.GetInt("wire-" + id + "-Y", -1),tree.GetInt("wire-" + id + "-Z", -1));
            if (newPos.Y == -1)
            {
                break;
            }
            else
            {
                positions.Add(newPos);
            }
            id++;
        }
    }

    public override void ToTreeAttributes(ITreeAttribute tree)
    {
        base.ToTreeAttributes(tree);

        int id = 1;
        foreach (var kv in wiresList)
        {
            tree.SetInt("wire-" + id + "-X", kv.Key.GetBlockPos().X);
            tree.SetInt("wire-" + id + "-Y", kv.Key.GetBlockPos().Y);
            tree.SetInt("wire-" + id + "-Z", kv.Key.GetBlockPos().Z);
            id++;
        }
    }

    public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
    {
        foreach (var kv in wiresList)
        {
            kv.Value.OnTesselation(mesher, this);
        }

        ICoreClientAPI clientApi = (ICoreClientAPI)Api;
        MeshData mesh = clientApi.TesselatorManager.GetDefaultBlockMesh(Block);
        if (mesh == null) return true;

        mesher.AddMeshData(mesh);

        return true;
    }

    public void AddWire(IWirePoint point, WireClass wire)
    {
        if (!wiresList.ContainsKey(point)) wiresList.Add(point, wire);
        MarkDirty(true);
    }

    public void RemoveWire(IWirePoint point)
    {
        if (wiresList.ContainsKey(point)) wiresList.Remove(point);
    }

    public Dictionary<IWirePoint, WireClass> GetWiresList()
    {
        return wiresList;
    }

    public BlockPos GetBlockPos()
    {
        return Pos;
    }
}