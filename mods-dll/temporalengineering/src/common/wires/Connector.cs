
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

public class BlockEntityConnector : BlockEntityWirePoint, IFluxStorage, IEnergyPoint
{
    public BlockFacing powerOutFacing;
    public EnergyDuctCore core;
    List<BlockFacing> skipList = new List<BlockFacing>();

    public override void Initialize(ICoreAPI api)
    {
        base.Initialize(api);

        powerOutFacing = BlockFacing.FromCode(Block.Variant["side"]);

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
            foreach (var kv in wiresList)
            {
                BlockEntityConnector block = api.World.BlockAccessor.GetBlockEntity(kv.Key) as BlockEntityConnector;
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
        if (!skipList.Contains(powerOutFacing)) transferEnergy(powerOutFacing, dt);
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
        if (from == powerOutFacing) return core.storage.receiveEnergy(Math.Min(maxReceive, MyMiniLib.GetAttributeInt(Block, "transfer", 500) * dt), simulate, dt);
        else return 0;
    }

    public bool CanWireConnect(BlockFacing side)
    {
        return side == powerOutFacing;
    }
}

public class BlockConnector : Block
{
    //public bool ShouldConnectAt(IWorldAccessor world, BlockPos ownPos, BlockFacing side)
    //{
    //    BlockEntity block = world.BlockAccessor.GetBlockEntity(ownPos.AddCopy(side));
    //    if (block is IFluxStorage)
    //    {
    //        return true;
    //    }
    //    else return false;
    //}




    bool handleDrops = true;
    string dropBlockFace = "down";
    string dropBlock = null;

    //public override void Initialize(JsonObject properties)
    //{
    //    base.Initialize(properties);

    //    handleDrops = properties["handleDrops"].AsBool(true);

    //    if (properties["dropBlockFace"].Exists)
    //    {
    //        dropBlockFace = properties["dropBlockFace"].AsString();
    //    }
    //    if (properties["dropBlock"].Exists)
    //    {
    //        dropBlock = properties["dropBlock"].AsString();
    //    }
    //}

    public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
    {
        if (TryAttachTo(world, byPlayer, blockSel, itemstack, ref failureCode)) return true;

        failureCode = "requirehorizontalattachable";

        return false;
    }

    public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier)
    {
        if (handleDrops)
        {
            if (dropBlock != null)
            {
                return new ItemStack[] { new ItemStack(world.BlockAccessor.GetBlock(new AssetLocation(dropBlock))) };
            }
            return new ItemStack[] { new ItemStack(world.BlockAccessor.GetBlock(CodeWithParts(dropBlockFace))) };

        }
        return null;
    }

    public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
    {

        if (dropBlock != null)
        {
            return new ItemStack(world.BlockAccessor.GetBlock(new AssetLocation(dropBlock)));
        }

        return new ItemStack(world.BlockAccessor.GetBlock(CodeWithParts(dropBlockFace)));
    }


    public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
    {
        if (!CanBlockStay(world, pos))
        {
            world.BlockAccessor.BreakBlock(pos, null);
        }
    }


    bool TryAttachTo(IWorldAccessor world, IPlayer player, BlockSelection blockSel, ItemStack itemstack, ref string failureCode)
    {
        BlockFacing oppositeFace = blockSel.Face.Opposite;

        BlockPos attachingBlockPos = blockSel.Position.AddCopy(oppositeFace);
        Block attachingBlock = world.BlockAccessor.GetBlock(world.BlockAccessor.GetBlockId(attachingBlockPos));
        Block orientedBlock = world.BlockAccessor.GetBlock(CodeWithParts(oppositeFace.Code));

        if (attachingBlock.CanAttachBlockAt(world.BlockAccessor, this, attachingBlockPos, blockSel.Face) && orientedBlock.CanPlaceBlock(world, player, blockSel, ref failureCode))
        {
            orientedBlock.DoPlaceBlock(world, player, blockSel, itemstack);
            return true;
        }

        return false;
    }

    bool CanBlockStay(IWorldAccessor world, BlockPos pos)
    {
        string[] parts = Code.Path.Split('-');
        BlockFacing facing = BlockFacing.FromCode(parts[parts.Length - 1]);
        int blockId = world.BlockAccessor.GetBlockId(pos.AddCopy(facing));

        Block attachingblock = world.BlockAccessor.GetBlock(blockId);

        return attachingblock.CanAttachBlockAt(world.BlockAccessor, this, pos.AddCopy(facing), facing.Opposite);
    }


    public override bool CanAttachBlockAt(IBlockAccessor blockAccessor, Block block, BlockPos pos, BlockFacing blockFace, Cuboidi attachmentArea = null)
    {
        return false;
    }


    //public override AssetLocation GetRotatedBlockCode(int angle, ref EnumHandling handled)
    //{
    //    handled = EnumHandling.PreventDefault;

    //    BlockFacing beforeFacing = BlockFacing.FromCode(LastCodePart());
    //    int rotatedIndex = GameMath.Mod(beforeFacing.HorizontalAngleIndex - angle / 90, 4);
    //    BlockFacing nowFacing = BlockFacing.HORIZONTALS_ANGLEORDER[rotatedIndex];

    //    return CodeWithParts(nowFacing.Code);
    //}

    //public override AssetLocation GetHorizontallyFlippedBlockCode(EnumAxis axis, ref EnumHandling handling)
    //{
    //    handling = EnumHandling.PreventDefault;

    //    BlockFacing facing = BlockFacing.FromCode(LastCodePart());
    //    if (facing.Axis == axis)
    //    {
    //        return CodeWithParts(facing.Opposite.Code);
    //    }
    //    return Code;
    //}
}