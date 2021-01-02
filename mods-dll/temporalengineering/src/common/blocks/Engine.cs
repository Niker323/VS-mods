using System;
using System.Collections.Generic;
using System.Diagnostics;
using Vintagestory.API;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent.Mechanics;

public class BlockTFEngine : BlockMPBase, IMPPowered
{
    BlockFacing powerOutFacing;

    public override void OnLoaded(ICoreAPI api)
    {
        powerOutFacing = BlockFacing.FromCode(Variant["side"]).Opposite;

        base.OnLoaded(api);
    }

    public override void DidConnectAt(IWorldAccessor world, BlockPos pos, BlockFacing face)
    {

    }

    public override bool HasMechPowerConnectorAt(IWorldAccessor world, BlockPos pos, BlockFacing face)
    {
        return face == powerOutFacing;
    }

    public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
    {
        bool ok = false;
        foreach (BlockFacing face in BlockFacing.HORIZONTALS)
        {
            BlockPos pos = blockSel.Position.AddCopy(face);
            IMechanicalPowerBlock block = world.BlockAccessor.GetBlock(pos) as IMechanicalPowerBlock;
            if (block != null)
            {
                if (block.HasMechPowerConnectorAt(world, pos, face.Opposite))
                {
                    //Prevent rotor back-to-back placement
                    if (block is IMPPowered) return false;

                    Block toPlaceBlock = world.GetBlock(CodeWithParts(face.Opposite.Code));
                    world.BlockAccessor.SetBlock(toPlaceBlock.BlockId, blockSel.Position);

                    block.DidConnectAt(world, pos, face.Opposite);
                    WasPlaced(world, blockSel.Position, face);

                    powerOutFacing = face;
                    ok = true;
                    break;
                }
            }
        }

        //if (!ok)
        //{
        //    if (blockSel.Face == BlockFacing.UP || blockSel.Face == BlockFacing.DOWN)
        //    {
        //        BlockFacing bestFacing = Block.SuggestedHVOrientation(byPlayer, blockSel)[0];

        //        Block orientedBlock = world.BlockAccessor.GetBlock(CodeWithParts(bestFacing.Code));
        //        powerOutFacing = bestFacing.Opposite;
        //        ok = orientedBlock.DoPlaceBlock(world, byPlayer, blockSel, itemstack);
        //    }
        //    else
        //    {
        //        Block orientedBlock = world.BlockAccessor.GetBlock(CodeWithParts(blockSel.Face.Opposite.Code));
        //        powerOutFacing = blockSel.Face;
        //        ok = orientedBlock.DoPlaceBlock(world, byPlayer, blockSel, itemstack);
        //    }
        //}
        return ok;
    }

    //public override void OnBlockRemoved(IWorldAccessor world, BlockPos pos)
    //{
    //    base.OnBlockRemoved(world, pos);
    //    BlockPos tmpPos = new BlockPos();

    //    for (int d1 = -1; d1 <= 1; d1++)
    //    {
    //        for (int d2 = -1; d2 <= 1; d2++)
    //        {
    //            if (d1 == 0 && d2 == 0) continue;
    //            if (powerOutFacing == BlockFacing.EAST || powerOutFacing == BlockFacing.WEST)
    //            {
    //                tmpPos.Set(pos.X, pos.Y + d1, pos.Z + d2);
    //            }
    //            else
    //            {
    //                tmpPos.Set(pos.X + d2, pos.Y + d1, pos.Z);
    //            }

    //            //Destroy any fake blocks; revert small gears to their normal peg gear type
    //            BEMPMultiblockBase be = world.BlockAccessor.GetBlockEntity(tmpPos) as BEMPMultiblockBase;
    //            if (be != null && pos.Equals(be.Principal))
    //            {
    //                be.Principal = null;  //signal to BlockMPMultiblockWood that it can be broken normally without triggering this in a loop
    //                world.BlockAccessor.SetBlock(0, tmpPos);
    //            }
    //            else
    //            {
    //                //BlockAngledGears smallgear = world.BlockAccessor.GetBlock(tmpPos) as BlockAngledGears;
    //                //if (smallgear != null) smallgear.ToPegGear(world, tmpPos);
    //            }
    //        }
    //    }
    //}
}

public class BlockEntityTFEngine : BlockEntity, IFluxStorage
{
    FluxStorage energyStorage;


    public BlockEntityTFEngine()
    {

    }

    public override void Initialize(ICoreAPI api)
    {
        base.Initialize(api);

        if (energyStorage == null)
        {
            energyStorage = new FluxStorage(10000, 1000, 0);
        }

        RegisterGameTickListener(OnTick, 50);
    }

    public int receiveEnergy(BlockFacing from, int maxReceive, bool simulate)
    {
        return energyStorage.receiveEnergy(Math.Min(1000, maxReceive), simulate);
    }

    public FluxStorage GetFluxStorage()
    {
        return energyStorage;
    }

    private void OnTick(float dt)
    {
        // Only tick on the server and merely sync to client
        if (Api is ICoreClientAPI)
        {
            return;
        }

        int res = Math.Min(100, energyStorage.getEnergyStored());
        energyStorage.modifyEnergyStored(-res);
        ((BEBehaviorTFEngine)Behaviors[0]).speed = res;
    }

    #region Events

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
    {
        base.FromTreeAttributes(tree, worldForResolving);

        if (energyStorage == null)
        {
            energyStorage = new FluxStorage(10000, 1000, 0);
        }
        energyStorage.setEnergy(tree.GetInt("energy", 0));
    }

    public override void ToTreeAttributes(ITreeAttribute tree)
    {
        base.ToTreeAttributes(tree);

        if (energyStorage != null)
        {
            tree.SetInt("energy", energyStorage.getEnergyStored());
        }
    }

    public override void OnBlockBroken()
    {
        base.OnBlockBroken();
    }

    #endregion

}

public class BEBehaviorTFEngine : BEBehaviorMPRotor
{
    public int speed = 0;
    BlockFacing face;

    protected override float Resistance
    {
        get
        {
            return 0.3f;
        }
    }

    protected override double AccelerationFactor
    {
        get
        {
            return 1d;
        }
    }

    protected override float TargetSpeed
    {
        get
        {
            return 0.01f * speed;
        }
    }

    protected override float TorqueFactor
    {
        get
        {
            return 0.01f * speed;
        }
    }

    public BEBehaviorTFEngine(BlockEntity blockentity) : base(blockentity)
    {

    }

    public override void Initialize(ICoreAPI api, JsonObject properties)
    {
        base.Initialize(api, properties);
        if (api.Side.IsServer())
        {
            face = BlockFacing.FromCode(Block.Variant["side"]);
            //Blockentity.RegisterGameTickListener(CheckWater, 50);
        }
    }

    //private void CheckWater(float dt)
    //{
    //    willSpeed = 0;
    //    BlockPos block1pos = Position.AddCopy(face.GetCCW()).AddCopy(face.GetCCW());
    //    Block block1 = Api.World.BlockAccessor.GetBlock(block1pos);
    //    Block block2 = Api.World.BlockAccessor.GetBlock(block1pos.AddCopy(BlockFacing.DOWN));
    //    Block block3 = Api.World.BlockAccessor.GetBlock(block1pos.AddCopy(BlockFacing.UP));
    //    if (block1.Code.BeginsWith("game", "water-d"))
    //    {
    //        willSpeed++;
    //    }
    //    if (block2.Code.BeginsWith("game", "water-d"))
    //    {
    //        willSpeed++;
    //    }
    //    if (block3.Code.BeginsWith("game", "water-d"))
    //    {
    //        willSpeed++;
    //    }
    //    BlockPos block4pos = Position.AddCopy(BlockFacing.DOWN).AddCopy(BlockFacing.DOWN);
    //    Block block4 = Api.World.BlockAccessor.GetBlock(block4pos);
    //    Block block5 = Api.World.BlockAccessor.GetBlock(block4pos.AddCopy(face.GetCCW()));
    //    Block block6 = Api.World.BlockAccessor.GetBlock(block4pos.AddCopy(face.GetCW()));
    //    string waterpath = "water-" + face.GetCW().Code[0];
    //    if (block4.Code.BeginsWith("game", waterpath))
    //    {
    //        willSpeed++;
    //    }
    //    if (block5.Code.BeginsWith("game", waterpath))
    //    {
    //        willSpeed++;
    //    }
    //    if (block6.Code.BeginsWith("game", waterpath))
    //    {
    //        willSpeed++;
    //    }
    //}

    protected override CompositeShape GetShape()
    {
        CompositeShape shape = Block.Shape.Clone();
        shape.Base = new AssetLocation("temporalengineering:shapes/block/testmodel4.json");
        shape.rotateX = 90;
        switch (BlockFacing.FromCode(Block.Variant["side"]).Index)
        {
            case 0:
                shape.rotateZ = 0;
                break;
            case 1:
                shape.rotateZ = 90;
                break;
            case 2:
                shape.rotateZ = 180;
                break;
            case 3:
                shape.rotateZ = 270;
                break;
            default:
                break;
        }
        return shape;
    }
}