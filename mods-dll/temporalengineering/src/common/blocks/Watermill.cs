using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Vintagestory.API;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent.Mechanics;

public class BlockWatermillRotor : BlockMPBase, IMPPowered
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
        if (api.Side == EnumAppSide.Client && blockSel.Face.IsVertical)
        {
            if (blockSel.Face == BlockFacing.UP) blockSel.Position.Y += 1;
            else blockSel.Position.Y -= 1;
        }

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
                    //world.BlockAccessor.SetBlock(toPlaceBlock.BlockId, blockSel.Position);

                    powerOutFacing = face;
                    if (CanPlaceThisBlock(world, byPlayer, blockSel, ref failureCode)) ok = toPlaceBlock.DoPlaceBlock(world, byPlayer, blockSel, itemstack);

                    if (ok)
                    {
                        block.DidConnectAt(world, pos, face.Opposite);
                        WasPlaced(world, blockSel.Position, face);
                    }
                    break;
                }
            }
        }

        if (!ok)
        {
            if (blockSel.Face.IsVertical)
            {
                BlockFacing bestFacing = Block.SuggestedHVOrientation(byPlayer, blockSel)[0];

                Block orientedBlock = world.BlockAccessor.GetBlock(CodeWithParts(bestFacing.Code));
                powerOutFacing = bestFacing.Opposite;
                if (CanPlaceThisBlock(world, byPlayer, blockSel, ref failureCode)) ok = orientedBlock.DoPlaceBlock(world, byPlayer, blockSel, itemstack);
            }
            else
            {
                Block orientedBlock = world.BlockAccessor.GetBlock(CodeWithParts(blockSel.Face.Opposite.Code));
                powerOutFacing = blockSel.Face;
                if (CanPlaceThisBlock(world, byPlayer, blockSel, ref failureCode)) ok = orientedBlock.DoPlaceBlock(world, byPlayer, blockSel, itemstack);
            }
        }
        if (ok)
        {
            PlaceFakeBlocks(world, blockSel.Position);
        }
        return ok;
    }

    private void PlaceFakeBlocks(IWorldAccessor world, BlockPos pos)
    {
        Block toPlaceBlock = world.GetBlock(new AssetLocation("temporalengineering:multiblockwood"));
        BlockPos tmpPos = new BlockPos();

        for (int d1 = -1; d1 <= 1; d1++)
        {
            for (int d2 = -1; d2 <= 1; d2++)
            {
                if (d1 == 0 && d2 == 0) continue;
                if (powerOutFacing == BlockFacing.EAST || powerOutFacing == BlockFacing.WEST)
                {
                    tmpPos.Set(pos.X, pos.Y + d1, pos.Z + d2);
                }
                else
                {
                    tmpPos.Set(pos.X + d2, pos.Y + d1, pos.Z);
                }
                world.BlockAccessor.SetBlock(toPlaceBlock.BlockId, tmpPos);
                BEMPMultiblockBase be = world.BlockAccessor.GetBlockEntity(tmpPos) as BEMPMultiblockBase;
                if (be != null) be.Principal = pos;
            }
        }
    }

    private bool CanPlaceThisBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref string failureCode)
    {
        if (!base.CanPlaceBlock(world, byPlayer, blockSel, ref failureCode)) return false;
        BlockPos pos = blockSel.Position;

        BlockPos tmpPos = new BlockPos();
        BlockSelection bs = blockSel.Clone();

        for (int d1 = -1; d1 <= 1; d1++)
        {
            for (int d2 = -1; d2 <= 1; d2++)
            {
                if (d1 == 0 && d2 == 0) continue;
                if (powerOutFacing == BlockFacing.EAST || powerOutFacing == BlockFacing.WEST)
                {
                    tmpPos.Set(pos.X, pos.Y + d1, pos.Z + d2);
                }
                else
                {
                    tmpPos.Set(pos.X + d2, pos.Y + d1, pos.Z);
                }
                bs.Position = tmpPos;
                if (!base.CanPlaceBlock(world, byPlayer, bs, ref failureCode)) return false;
            }
        }

        return true;
    }

    public override void OnBlockRemoved(IWorldAccessor world, BlockPos pos)
    {
        base.OnBlockRemoved(world, pos);
        BlockPos tmpPos = new BlockPos();

        for (int d1 = -1; d1 <= 1; d1++)
        {
            for (int d2 = -1; d2 <= 1; d2++)
            {
                if (d1 == 0 && d2 == 0) continue;
                if (powerOutFacing == BlockFacing.EAST || powerOutFacing == BlockFacing.WEST)
                {
                    tmpPos.Set(pos.X, pos.Y + d1, pos.Z + d2);
                }
                else
                {
                    tmpPos.Set(pos.X + d2, pos.Y + d1, pos.Z);
                }

                //Destroy any fake blocks; revert small gears to their normal peg gear type
                BEMPMultiblockBase be = world.BlockAccessor.GetBlockEntity(tmpPos) as BEMPMultiblockBase;
                if (be != null && pos.Equals(be.Principal))
                {
                    be.Principal = null;  //signal to BlockMPMultiblockWood that it can be broken normally without triggering this in a loop
                    world.BlockAccessor.SetBlock(0, tmpPos);
                }
                else
                {
                    //BlockAngledGears smallgear = world.BlockAccessor.GetBlock(tmpPos) as BlockAngledGears;
                    //if (smallgear != null) smallgear.ToPegGear(world, tmpPos);
                }
            }
        }
    }
}

public class BEBehaviorWatermillRotor : BEBehaviorMPRotor
{
    private int willSpeed = 0;
    BlockFacing face;

    protected override float Resistance
    {
        get
        {
            return 3f;
        }
    }

    protected override double AccelerationFactor
    {
        get
        {
            return 0.1d;
        }
    }

    protected override float TargetSpeed
    {
        get
        {
            return 0.15f * willSpeed;
        }
    }

    protected override float TorqueFactor
    {
        get
        {
            return 0.15f * willSpeed;
        }
    }

    public override float AngleRad
    {
        get
        {
            float angle = base.AngleRad;

            bool flip = propagationDir == OutFacingForNetworkDiscovery;

            return flip ? angle : GameMath.TWOPI - angle;
        }
    }

    public BEBehaviorWatermillRotor(BlockEntity blockentity) : base(blockentity)
    {

    }

    public override void Initialize(ICoreAPI api, JsonObject properties)
    {
        base.Initialize(api, properties);

        if (api.Side.IsServer())
        {
            face = BlockFacing.FromCode(Block.Variant["side"]);
            CheckWater(0);
            Blockentity.RegisterGameTickListener(CheckWater, 1000);
        }
    }

    public override bool isInvertedNetworkFor(BlockPos pos)
    {
        return face != propagationDir;
    }

    private void CheckWater(float dt)
    {
        willSpeed = 0;
        BlockPos block1pos = Position.AddCopy(face.GetCCW()).AddCopy(face.GetCCW());
        Block block1 = Api.World.BlockAccessor.GetBlock(block1pos);
        Block block2 = Api.World.BlockAccessor.GetBlock(block1pos.AddCopy(BlockFacing.DOWN));
        Block block3 = Api.World.BlockAccessor.GetBlock(block1pos.AddCopy(BlockFacing.UP));
        if (block1.Code.BeginsWith("game", "water-d"))
        {
            willSpeed++;
        }
        if (block2.Code.BeginsWith("game", "water-d"))
        {
            willSpeed++;
        }
        if (block3.Code.BeginsWith("game", "water-d"))
        {
            willSpeed++;
        }
        BlockPos block2pos = Position.AddCopy(face.GetCW()).AddCopy(face.GetCW());
        Block block21 = Api.World.BlockAccessor.GetBlock(block2pos);
        Block block22 = Api.World.BlockAccessor.GetBlock(block2pos.AddCopy(BlockFacing.DOWN));
        Block block23 = Api.World.BlockAccessor.GetBlock(block2pos.AddCopy(BlockFacing.UP));
        if (block21.Code.BeginsWith("game", "water-d"))
        {
            willSpeed--;
        }
        if (block22.Code.BeginsWith("game", "water-d"))
        {
            willSpeed--;
        }
        if (block23.Code.BeginsWith("game", "water-d"))
        {
            willSpeed--;
        }
        BlockPos block4pos = Position.AddCopy(BlockFacing.DOWN).AddCopy(BlockFacing.DOWN);
        Block block4 = Api.World.BlockAccessor.GetBlock(block4pos);
        Block block5 = Api.World.BlockAccessor.GetBlock(block4pos.AddCopy(face.GetCCW()));
        Block block6 = Api.World.BlockAccessor.GetBlock(block4pos.AddCopy(face.GetCW()));
        string waterpath = "water-" + face.GetCW().Code[0];
        string waterpath2 = "water-" + face.GetCCW().Code[0];
        if (block4.Code.BeginsWith("game", waterpath))
        {
            willSpeed++;
        }
        else if (block4.Code.BeginsWith("game", waterpath2))
        {
            willSpeed--;
        }
        if (block5.Code.BeginsWith("game", waterpath))
        {
            willSpeed++;
        }
        else if (block5.Code.BeginsWith("game", waterpath2))
        {
            willSpeed--;
        }
        if (block6.Code.BeginsWith("game", waterpath))
        {
            willSpeed++;
        }
        else if (block6.Code.BeginsWith("game", waterpath2))
        {
            willSpeed--;
        }
        if (willSpeed < 0)
        {
            willSpeed = Math.Abs(willSpeed);
            //SetPropagationDirection(new MechPowerPath(face.Opposite, 1 /*GearedRatio*/, null, true));
            propagationDir = OutFacingForNetworkDiscovery.Opposite;
        }
        else
        {
            //SetPropagationDirection(new MechPowerPath(face.Opposite, 1 /*GearedRatio*/, null, false));
            propagationDir = OutFacingForNetworkDiscovery;
        }
    }

    //protected override CompositeShape GetShape()
    //{
    //    CompositeShape shape = Block.Shape.Clone();
    //    shape.Base = new AssetLocation("temporalengineering:shapes/block/watermill.json");
    //    shape.rotateX = 90;
    //    switch (BlockFacing.FromCode(Block.Variant["side"]).Index)
    //    {
    //        case 0:
    //            shape.rotateZ = 0;
    //            break;
    //        case 1:
    //            shape.rotateZ = 90;
    //            break;
    //        case 2:
    //            shape.rotateZ = 180;
    //            break;
    //        case 3:
    //            shape.rotateZ = 270;
    //            break;
    //        default:
    //            break;
    //    }
    //    return shape;
    //}
}