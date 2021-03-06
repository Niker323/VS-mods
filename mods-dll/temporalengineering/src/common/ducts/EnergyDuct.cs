﻿
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

public class BlockEntityEnergyDuct : BlockEntity, IFluxStorage, IEnergyPoint
{
    public EnergyDuctCore core;
    List<BlockFacing> skipList = new List<BlockFacing>();
    //Dictionary<BlockFacing, bool> sideConfig = new Dictionary<BlockFacing, bool>(IOEnergySideConfig.VALUES.Length);

    public override void Initialize(ICoreAPI api)
    {
        base.Initialize(api);

        if (api.World.Side == EnumAppSide.Server)
        {
            InitializeEnergyPoint(api);

            RegisterGameTickListener(tick, 250);
        }
    }

    public void InitializeEnergyPoint(ICoreAPI api)
    {
        if (api.World.Side == EnumAppSide.Server)
        {
            foreach (BlockFacing face in BlockFacing.ALLFACES)
            {
                BlockPos pos = Pos.AddCopy(face);
                BlockEntityEnergyDuct block = api.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityEnergyDuct;
                if (block != null)
                {
                    if (core == null)
                    {
                        if (block.core == null)
                        {
                            core = new EnergyDuctCore(MyMiniLib.GetAttributeInt(Block, "transfer", 500));
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
                core = new EnergyDuctCore(MyMiniLib.GetAttributeInt(Block, "transfer", 500));
                core.ducts.Add(this);
            }
        }
    }

    private void tick(float dt)
    {
        foreach (BlockFacing f in BlockFacing.ALLFACES)
            if (!skipList.Contains(f)) transferEnergy(f, dt);
        skipList.Clear();
        //MarkDirty();
    }

    protected void transferEnergy(BlockFacing side, float dt)
    {
        BlockPos outPos = Pos.Copy().Offset(side);
        BlockEntity tileEntity = Api.World.BlockAccessor.GetBlockEntity(outPos);
        if (tileEntity == null) return;
        if (!(tileEntity is IFluxStorage)) return;
        if (tileEntity is IEnergyPoint && ((IEnergyPoint)tileEntity).GetCore() == GetCore()) return;
        float eout = Math.Min(MyMiniLib.GetAttributeInt(Block, "transfer", 500) * dt, core.storage.getEnergyStored() * dt);
        eout = ((IFluxStorage)tileEntity).receiveEnergy(side.Opposite, eout, false, dt);
        if (tileEntity is IEnergyPoint && eout > 0)
        {
            ((IEnergyPoint)tileEntity).AddSkipSide(side.Opposite);
        }
        core.storage.modifyEnergyStored(-eout);
    }

    public override void OnBlockRemoved()
    {
        base.OnBlockRemoved();

        if (Api.World.Side == EnumAppSide.Server)
        {
            core.OnDuctRemoved(this, Api);
        }
    }

    public void AddSkipSide(BlockFacing face)
    {
        skipList.Add(face);
    }

    public EnergyDuctCore GetCore()
    {
        return core;
    }

    public void SetCore(EnergyDuctCore core)
    {
        this.core = core;
    }

    public FluxStorage GetFluxStorage()
    {
        return core.storage;
    }

    public float receiveEnergy(BlockFacing from, float maxReceive, bool simulate, float dt)
    {
        return core.storage.receiveEnergy(Math.Min(maxReceive, MyMiniLib.GetAttributeInt(Block, "transfer", 500) * dt), simulate, dt);
    }

    public bool CanWireConnect(BlockFacing side)
    {
        return true;
    }
}

public class BlockEnergyDuct : Block
{
    int transfer = 2000;

    public override void OnLoaded(ICoreAPI api)
    {
        transfer = MyMiniLib.GetAttributeInt(this, "transfer", transfer);

        base.OnLoaded(api);
    }

    public string GetOrientations(IWorldAccessor world, BlockPos pos)
    {
        string orientations =
            GetFenceCode(world, pos, BlockFacing.NORTH) +
            GetFenceCode(world, pos, BlockFacing.EAST) +
            GetFenceCode(world, pos, BlockFacing.SOUTH) +
            GetFenceCode(world, pos, BlockFacing.WEST) +
            GetFenceCode(world, pos, BlockFacing.UP) +
            GetFenceCode(world, pos, BlockFacing.DOWN)
        ;

        if (orientations.Length == 0) orientations = "empty";
        return orientations;
    }

    private string GetFenceCode(IWorldAccessor world, BlockPos pos, BlockFacing facing)
    {
        if (ShouldConnectAt(world, pos, facing)) return "" + facing.Code[0];
        return "";
    }


    public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
    {
        string orientations = GetOrientations(world, blockSel.Position);
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
        string orientations = GetOrientations(world, pos);
        //Debug.WriteLine(orientations);

        AssetLocation newBlockCode = CodeWithVariant("side", orientations);

        if (!Code.Equals(newBlockCode))
        {
            Block block = world.BlockAccessor.GetBlock(newBlockCode);
            if (block == null) return;

            world.BlockAccessor.ExchangeBlock(block.BlockId, pos);
            world.BlockAccessor.TriggerNeighbourBlockUpdate(pos);
        }
        else
        {
            base.OnNeighbourBlockChange(world, pos, neibpos);
        }
    }

    //public override BlockDropItemStack[] GetDropsForHandbook(ItemStack handbookStack, IPlayer forPlayer)
    //{
    //    return new BlockDropItemStack[] { new BlockDropItemStack(handbookStack) };
    //}

    public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
    {
        Block block = world.BlockAccessor.GetBlock(CodeWithVariants(new string[] { "side" }, new string[] { "ew" }));
        return new ItemStack[] { new ItemStack(block) };
    }

    public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
    {
        Block block = world.BlockAccessor.GetBlock(CodeWithVariants(new string[] { "side" }, new string[] { "ew" }));
        return new ItemStack(block);
    }



    public bool ShouldConnectAt(IWorldAccessor world, BlockPos ownPos, BlockFacing side)
    {
        BlockEntity block = world.BlockAccessor.GetBlockEntity(ownPos.AddCopy(side));
        if (block is IFluxStorage)
        {
            return ((IFluxStorage)block).CanWireConnect(side.Opposite);
        }
        else return false;
    }

    public override bool CanAttachBlockAt(IBlockAccessor blockAccessor, Block block, BlockPos pos, BlockFacing blockFace, Cuboidi attachmentArea = null)
    {
        return block is BlockConnector;
    }

    public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
    {
        base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
        dsc.AppendLine(Lang.Get("Traversing") + ": " + transfer + " TF/s");
    }
}