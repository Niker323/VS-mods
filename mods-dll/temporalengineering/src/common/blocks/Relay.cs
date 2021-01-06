using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Vintagestory.API;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

public class BETFRelay : BlockEntity, IFluxStorage, IIOEnergySideConfig
{
    public Dictionary<BlockFacing, IOEnergySideConfig> sideConfig = new Dictionary<BlockFacing, IOEnergySideConfig>(IOEnergySideConfig.VALUES.Length);
    public FluxStorage energyStorage;
    public bool state = false;

    //public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
    //{
    //    base.GetBlockInfo(forPlayer, dsc);
    //    if (energyStorage != null)
    //    {
    //        dsc.AppendLine(energyStorage.GetFluxStorageInfo());
    //    }
    //}

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
    {
        base.FromTreeAttributes(tree, worldForResolving);

        if (energyStorage == null)
        {
            energyStorage = new FluxStorage(getTransferLimit(), getTransferLimit(), getTransferLimit());
        }
        energyStorage.setEnergy(tree.GetInt("energy", 0));
        state = tree.GetBool("state", false);
    }

    public override void ToTreeAttributes(ITreeAttribute tree)
    {
        base.ToTreeAttributes(tree);

        if (energyStorage != null)
        {
            tree.SetInt("energy", energyStorage.getEnergyStored());
        }
        tree.SetBool("state", state);
    }

    public override void Initialize(ICoreAPI api)
    {
        base.Initialize(api);
        Api = api;
        //Debug.WriteLine("Initialize");
        //Debug.WriteLine(api is ICoreServerAPI);

        if (api.World.Side == EnumAppSide.Server)
        {
            RegisterGameTickListener(tick, 50);
        }

        if (energyStorage == null)
        {
            energyStorage = new FluxStorage(getTransferLimit(), getTransferLimit(), getTransferLimit());
        }
        if (sideConfig.Count == 0)
        {
            string[] splitedPath = Block.Code.ToString().Split('-');
            if (splitedPath.Length >= 6)
            {
                foreach (BlockFacing f in BlockFacing.ALLFACES)
                {
                    if (splitedPath[splitedPath.Length - 6 + f.Index] == IOEnergySideConfig.INPUT.toString())
                        sideConfig.Add(f, IOEnergySideConfig.INPUT);
                    else if (splitedPath[splitedPath.Length - 6 + f.Index] == IOEnergySideConfig.OUTPUT.toString())
                        sideConfig.Add(f, IOEnergySideConfig.OUTPUT);
                    else
                        sideConfig.Add(f, IOEnergySideConfig.NONE);
                }
            }
            else
            {
                foreach (BlockFacing f in BlockFacing.ALLFACES)
                {
                    sideConfig.Add(f, IOEnergySideConfig.NONE);
                }
            }
        }
    }

    private void tick(float dt)
    {
        if (state)
        {
            foreach (BlockFacing f in BlockFacing.ALLFACES)
                transferEnergy(f);
            MarkDirty();
        }
    }

    protected void transferEnergy(BlockFacing side)
    {
        if (sideConfig[side] != IOEnergySideConfig.OUTPUT) return;
        BlockPos outPos = Pos.Copy().Offset(side);
        BlockEntity tileEntity = Api.World.BlockAccessor.GetBlockEntity(outPos);
        if (tileEntity == null) return;
        if (!(tileEntity is IFluxStorage)) return;
        int eout = Math.Min(getTransferLimit(), energyStorage.getEnergyStored());
        energyStorage.modifyEnergyStored(-((IFluxStorage)tileEntity).receiveEnergy(side.Opposite, eout, false));
    }

    public int receiveEnergy(BlockFacing from, int maxReceive, bool simulate)
    {
        if (from == null || sideConfig[from] == IOEnergySideConfig.INPUT)
        {
            return energyStorage.receiveEnergy(Math.Min(getTransferLimit(), maxReceive), simulate);
        }
        return 0;
    }

    public IOEnergySideConfig getEnergySideConfig(BlockFacing side)
    {
        return sideConfig[side];
    }

    public bool toggleSide(BlockFacing side)//, PlayerEntity player
    {
        //Debug.WriteLine("toggleSide");
        //Debug.WriteLine(Api is ICoreServerAPI);
        if (Api is ICoreServerAPI)
        {
            sideConfig[side] = sideConfig[side].next();

            //Block block = Api.World.BlockAccessor.GetBlock(Pos);
            //string[] splitedPath = block.Code.Path.Split('-');
            //Debug.WriteLine(side.ToString());
            //Debug.WriteLine(sideConfig[side].toString());
            //Debug.WriteLine(Block.CodeWithVariant(side.ToString(), sideConfig[side].toString()).Path);


            Api.World.BlockAccessor.ExchangeBlock(Api.World.GetBlock(Block.CodeWithVariant(side.ToString(), sideConfig[side].toString())).BlockId, Pos);
        }

        //MarkDirty();
        return true;
    }

    public bool OnInteract(IPlayer byPlayer)
    {
        //BEBehaviorMPTransmission be = Api.World.BlockAccessor.GetBlockEntity(transmissionPos)?.GetBehavior<BEBehaviorMPTransmission>();
        //if (!Engaged && be != null && be.engaged) return true;
        state = !state;
        Api.World.PlaySoundAt(new AssetLocation("sounds/effect/woodswitch.ogg"), Pos.X + 0.5, Pos.Y + 0.5, Pos.Z + 0.5, byPlayer);
        //if (be != null)
        //{
        //    CheckEngaged(Api.World.BlockAccessor, true);
        //}

        MarkDirty(true);
        return true;
    }

    public int getTransferLimit()
    {
        if (Block != null && Block.Attributes != null && Block.Attributes["transfer"] != null)
        {
            return Block.Attributes["transfer"].AsInt();
        }
        return 1000;
    }

    public FluxStorage GetFluxStorage()
    {
        return energyStorage;
    }
}

public class BlockTFRelay : BlockSideconfigInteractions
{
    //public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
    //{
    //    if (!CanPlaceBlock(world, byPlayer, blockSel, ref failureCode)) return false;

    //    BlockFacing frontFacing = Block.SuggestedHVOrientation(byPlayer, blockSel)[0];
    //    BlockFacing bestFacing = frontFacing;
    //    if (!(world.BlockAccessor.GetBlock(blockSel.Position.AddCopy(frontFacing)) is BlockTransmission))
    //    {
    //        BlockFacing leftFacing = BlockFacing.HORIZONTALS_ANGLEORDER[GameMath.Mod(frontFacing.HorizontalAngleIndex - 1, 4)];
    //        if (world.BlockAccessor.GetBlock(blockSel.Position.AddCopy(leftFacing)) is BlockTransmission)
    //        {
    //            bestFacing = leftFacing;
    //        }
    //        else
    //        {
    //            BlockFacing rightFacing = leftFacing.Opposite;
    //            if (world.BlockAccessor.GetBlock(blockSel.Position.AddCopy(rightFacing)) is BlockTransmission)
    //            {
    //                bestFacing = rightFacing;
    //            }
    //            else
    //            {
    //                BlockFacing backFacing = frontFacing.Opposite;
    //                if (world.BlockAccessor.GetBlock(blockSel.Position.AddCopy(backFacing)) is BlockTransmission)
    //                {
    //                    bestFacing = backFacing;
    //                }
    //            }
    //        }
    //    }

    //    Block orientedBlock = world.BlockAccessor.GetBlock(CodeWithParts(bestFacing.Code));
    //    return orientedBlock.DoPlaceBlock(world, byPlayer, blockSel, itemstack);
    //}

    public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
    {
        BETFRelay be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BETFRelay;
        if (be != null && (byPlayer.InventoryManager.ActiveHotbarSlot == null || byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack == null || byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack.Item == null || !(byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack.Item is Wrench)))
        {
            return be.OnInteract(byPlayer);
        }

        return base.OnBlockInteractStart(world, byPlayer, blockSel);
    }
}