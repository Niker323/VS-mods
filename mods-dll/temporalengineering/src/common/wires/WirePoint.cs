
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

public class BlockEntityWirePoint : BlockEntity, IWirePoint
{
    public Dictionary<IWirePoint, WireClass> wiresList = new Dictionary<IWirePoint, WireClass>(0);

    public override void Initialize(ICoreAPI api)
    {
        base.Initialize(api);
        //Debug.WriteLine("Initialize");
        //Debug.WriteLine(api.World.Side == EnumAppSide.Server);

        if (api.World.Side == EnumAppSide.Server)
        {
            //InitializeEnergyDuct();
        }
    }

    //void InitializeEnergyDuct()
    //{

    //}

    public override void OnBlockRemoved()
    {
        base.OnBlockRemoved();

        if (Api.World.Side == EnumAppSide.Server)
        {
            //core.OnDuctRemoved(this);
        }
    }

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
    {
        base.FromTreeAttributes(tree, worldForResolving);

        int id = 1;
        while (true)
        {
            BlockPos newPos = new BlockPos();
            newPos.X = tree.GetInt("wire-" + id + "-X", -1);
            newPos.Y = tree.GetInt("wire-" + id + "-Y", -1);
            newPos.Z = tree.GetInt("wire-" + id + "-Z", -1);
            if (newPos.Y == -1)
            {
                break;
            }
            else
            {
                BlockEntityWirePoint block = Api.World.BlockAccessor.GetBlockEntity(newPos) as BlockEntityWirePoint;
                if (block != null)
                {
                    WireClass wireClass;
                    if (block.wiresList.TryGetValue(this, out wireClass))
                    {
                        wiresList.Add(block, wireClass);
                    }
                    else
                    {
                        wiresList.Add(block, new WireClass());
                    }
                }
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
            //tree.SetString("wire-" + id, kv.Key.GetBlockPos().X+"_"+ kv.Key.GetBlockPos().Y + "_"+ kv.Key.GetBlockPos().Z);
            tree.SetInt("wire-" + id + "-X", kv.Key.GetBlockPos().X);
            tree.SetInt("wire-" + id + "-Y", kv.Key.GetBlockPos().Y);
            tree.SetInt("wire-" + id + "-Z", kv.Key.GetBlockPos().Z);
            id++;
        }
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

//public class BlockConnector : Block
//{
//    //public override void OnLoaded(ICoreAPI api)
//    //{
//    //    base.OnLoaded(api);
//    //}

//    public string GetOrientations(IWorldAccessor world, BlockPos pos)
//    {
//        string orientations =
//            GetFenceCode(world, pos, BlockFacing.NORTH) +
//            GetFenceCode(world, pos, BlockFacing.EAST) +
//            GetFenceCode(world, pos, BlockFacing.SOUTH) +
//            GetFenceCode(world, pos, BlockFacing.WEST) +
//            GetFenceCode(world, pos, BlockFacing.UP) +
//            GetFenceCode(world, pos, BlockFacing.DOWN)
//        ;

//        if (orientations.Length == 0) orientations = "empty";
//        return orientations;
//    }

//    private string GetFenceCode(IWorldAccessor world, BlockPos pos, BlockFacing facing)
//    {
//        if (ShouldConnectAt(world, pos, facing)) return "" + facing.Code[0];
//        return "";
//    }


//    public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
//    {
//        string orientations = GetOrientations(world, blockSel.Position);
//        //Debug.WriteLine(orientations);
//        Block block = world.BlockAccessor.GetBlock(CodeWithVariant("side", orientations));

//        if (block == null) block = this;

//        if (block.CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
//        {
//            world.BlockAccessor.SetBlock(block.BlockId, blockSel.Position);
//            return true;
//        }

//        return false;
//    }

//    public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
//    {
//        string orientations = GetOrientations(world, pos);
//        //Debug.WriteLine(orientations);

//        AssetLocation newBlockCode = CodeWithVariant("side", orientations);

//        if (!Code.Equals(newBlockCode))
//        {
//            Block block = world.BlockAccessor.GetBlock(newBlockCode);
//            if (block == null) return;

//            world.BlockAccessor.ExchangeBlock(block.BlockId, pos);
//            world.BlockAccessor.TriggerNeighbourBlockUpdate(pos);
//        }
//        else
//        {
//            base.OnNeighbourBlockChange(world, pos, neibpos);
//        }
//    }

//    public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
//    {
//        Block block = world.BlockAccessor.GetBlock(CodeWithVariants(new string[] { "side" }, new string[] { "ew" }));
//        return new ItemStack(block);
//    }



//    public bool ShouldConnectAt(IWorldAccessor world, BlockPos ownPos, BlockFacing side)
//    {
//        BlockEntity block = world.BlockAccessor.GetBlockEntity(ownPos.AddCopy(side));
//        if (block is IFluxStorage)
//        {
//            if (block is IIOEnergySideConfig)
//            {
//                if (((IIOEnergySideConfig)block).getEnergySideConfig(side.Opposite) == IOEnergySideConfig.NONE) return false;
//            }
//            return true;
//        }
//        else return false;
//    }
//}