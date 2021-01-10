
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
    public Dictionary<BlockPos, WireClass> wiresList = new Dictionary<BlockPos, WireClass>(0);

    public override void Initialize(ICoreAPI api)
    {
        base.Initialize(api);
        Api = api;

        UpdateWiresList();
    }

    public override void OnBlockRemoved()
    {
        base.OnBlockRemoved();

        foreach (var kv in wiresList)
        {
            kv.Value.OnPointRemoved(this);
        }
        wiresList.Clear();
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
        UpdateWiresList();
    }

    public void UpdateWiresList()
    {
        if (Api != null)
        {
            wiresList.Clear();
            foreach (BlockPos newPos in positions)
            {
                BlockEntityWirePoint block = Api.World.BlockAccessor.GetBlockEntity(newPos) as BlockEntityWirePoint;
                if (block != null && !wiresList.ContainsKey(block.Pos))
                {
                    WireClass wireClass;
                    if (block.wiresList.TryGetValue(Pos, out wireClass))
                    {
                        AddWire(block.Pos, wireClass);
                    }
                    else
                    {
                        new WireClass(Api, this, block);
                    }
                }
            }
        }
    }

    public override void ToTreeAttributes(ITreeAttribute tree)
    {
        base.ToTreeAttributes(tree);

        int id = 1;
        foreach (var kv in wiresList)
        {
            tree.SetInt("wire-" + id + "-X", kv.Key.X);
            tree.SetInt("wire-" + id + "-Y", kv.Key.Y);
            tree.SetInt("wire-" + id + "-Z", kv.Key.Z);
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

    public void AddWire(BlockPos point, WireClass wire)
    {
        if (!wiresList.ContainsKey(point)) wiresList.Add(point, wire);
        else if (wiresList[point] != wire) wiresList[point] = wire;
        MarkDirty(true);
    }

    public void RemoveWire(BlockPos point)
    {
        List<BlockPos> deleteList = new List<BlockPos>();
        foreach (var kv in wiresList)
        {
            if (kv.Key == point)
            {
                deleteList.Add(kv.Key);
            }
        }
        foreach (var key in deleteList)
        {
            wiresList.Remove(key);
        }
        MarkDirty(true);
    }

    public Dictionary<BlockPos, WireClass> GetWiresList()
    {
        return wiresList;
    }

    public BlockPos GetBlockPos()
    {
        return Pos;
    }
}