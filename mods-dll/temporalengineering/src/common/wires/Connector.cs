
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

public class BlockEntityConnector : BlockEntityWirePoint, IFluxStorage, IEnergyPoint
{
    BlockFacing powerOutFacing;
    public EnergyDuctCore core;

    public override void Initialize(ICoreAPI api)
    {
        base.Initialize(api);
        //Debug.WriteLine("Initialize");
        //Debug.WriteLine(api.World.Side == EnumAppSide.Server);
        powerOutFacing = BlockFacing.FromCode(Block.Variant["side"]).Opposite;

        if (api.World.Side == EnumAppSide.Server)
        {
            InitializeEnergyPoint();

            RegisterGameTickListener(tick, 50);
        }
    }

    public void InitializeEnergyPoint()
    {
        foreach (BlockFacing face in BlockFacing.ALLFACES)
        {
            BlockPos pos = Pos.AddCopy(face);
            BlockEntityEnergyDuct block = Api.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityEnergyDuct;
            if (block != null)
            {
                if (core == null)
                {
                    if (block.core == null)
                    {
                        core = new EnergyDuctCore(getTransferLimit());
                        core.ducts.Add(this);
                    }
                    else
                    {
                        core = block.core;
                        core.ducts.Add(this);
                    }
                }
                else
                {
                    if (core != block.core && block.core != null)
                    {
                        core = core.CombineCores(block.core);
                    }
                }
            }
        }
        if (core == null)
        {
            core = new EnergyDuctCore(getTransferLimit());
            core.ducts.Add(this);
        }

        RegisterGameTickListener(tick, 50);
    }

    private void tick(float dt)
    {
        transferEnergy(powerOutFacing);
        //MarkDirty();
    }

    protected void transferEnergy(BlockFacing side)
    {
        BlockPos outPos = Pos.Copy().Offset(side);
        BlockEntity tileEntity = Api.World.BlockAccessor.GetBlockEntity(outPos);
        if (tileEntity == null) return;
        if (!(tileEntity is IFluxStorage)) return;
        int eout = Math.Min(getTransferLimit(), core.storage.getEnergyStored());
        core.storage.modifyEnergyStored(-((IFluxStorage)tileEntity).receiveEnergy(side.Opposite, eout, false));
    }

    public override void OnBlockRemoved()
    {
        base.OnBlockRemoved();

        if (Api.World.Side == EnumAppSide.Server)
        {
            core.OnDuctRemoved(this);
        }
    }

    public int getTransferLimit()
    {
        if (Block != null && Block.Attributes != null && Block.Attributes["transfer"] != null)
        {
            return Block.Attributes["transfer"].AsInt();
        }
        return 100;
    }

    public FluxStorage GetFluxStorage()
    {
        return core.storage;
    }

    public int receiveEnergy(BlockFacing from, int maxReceive, bool simulate)
    {
        return core.storage.receiveEnergy(Math.Min(maxReceive, getTransferLimit()), simulate);
    }
}

public class BlockConnector : Block
{
    BlockFacing powerOutFacing;

    public override void OnLoaded(ICoreAPI api)
    {
        powerOutFacing = BlockFacing.FromCode(Variant["side"]).Opposite;

        base.OnLoaded(api);
    }

    public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
    {
        string orientations = BlockFacing.NORTH.Code;
        foreach (BlockFacing face in BlockFacing.ALLFACES)
        {
            if (ShouldConnectAt(world, blockSel.Position, face))
            {
                orientations = face.Code;
                break;
            }
        }

        //Debug.WriteLine(orientations);
        Block block = world.BlockAccessor.GetBlock(CodeWithVariant("side", orientations));

        if (block == null) block = this;

        if (block.CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
        {
            world.BlockAccessor.SetBlock(block.BlockId, blockSel.Position);
            return true;
        }

        return false;
    }

    public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
    {
        if (neibpos == pos.AddCopy(powerOutFacing))
        {
            Block block = world.BlockAccessor.GetBlock(neibpos);
            if (block == null)
            {
                //destroy
                return;
            }
        }
        base.OnNeighbourBlockChange(world, pos, neibpos);
    }

    //public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
    //{
    //    Block block = world.BlockAccessor.GetBlock(CodeWithVariants(new string[] { "side" }, new string[] { "ew" }));
    //    return new ItemStack(block);
    //}



    public bool ShouldConnectAt(IWorldAccessor world, BlockPos ownPos, BlockFacing side)
    {
        BlockEntity block = world.BlockAccessor.GetBlockEntity(ownPos.AddCopy(side));
        if (block is IFluxStorage)
        {
            return true;
        }
        else return false;
    }
}