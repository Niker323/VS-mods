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

public class TFCapacitor : BlockEntity, IFluxStorage, IIOEnergySideConfig
{
    public Dictionary<BlockFacing, IOEnergySideConfig> sideConfig = new Dictionary<BlockFacing, IOEnergySideConfig>(IOEnergySideConfig.VALUES.Length);
    public FluxStorage energyStorage;

    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
    {
        base.GetBlockInfo(forPlayer, dsc);
        if (energyStorage != null)
        {
            dsc.AppendLine(energyStorage.GetFluxStorageInfo());
        }
    }

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
    {
        base.FromTreeAttributes(tree, worldForResolving);

        if (energyStorage == null)
        {
            energyStorage = new FluxStorage(MyMiniLib.GetAttributeInt(Block, "storage", 10000), MyMiniLib.GetAttributeInt(Block, "input", 1000), MyMiniLib.GetAttributeInt(Block, "output", 1000));
        }
        energyStorage.setEnergy(tree.GetFloat("energy"));
    }

    public override void ToTreeAttributes(ITreeAttribute tree)
    {
        base.ToTreeAttributes(tree);

        if (energyStorage != null)
        {
            tree.SetFloat("energy", energyStorage.getEnergyStored());
        }
    }

    public override void Initialize(ICoreAPI api)
    {
        base.Initialize(api);
        Api = api;

        if (api.World.Side == EnumAppSide.Server)
        {
            RegisterGameTickListener(tick, 100);
        }

        if (energyStorage == null)
        {
            energyStorage = new FluxStorage(MyMiniLib.GetAttributeInt(Block, "storage", 10000), MyMiniLib.GetAttributeInt(Block, "input", 1000), MyMiniLib.GetAttributeInt(Block, "output", 1000));
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
        foreach (BlockFacing f in BlockFacing.ALLFACES)
            transferEnergy(f, dt);
        MarkDirty();
    }

    protected void transferEnergy(BlockFacing side, float dt)
    {
        if (sideConfig[side] != IOEnergySideConfig.OUTPUT) return;
        BlockPos outPos = Pos.Copy().Offset(side);
        BlockEntity tileEntity = Api.World.BlockAccessor.GetBlockEntity(outPos);
        if (tileEntity == null) return;
        if (!(tileEntity is IFluxStorage)) return;
        float eout = Math.Min(energyStorage.getLimitExtract() * dt, energyStorage.getEnergyStored() * dt);
        energyStorage.modifyEnergyStored(-((IFluxStorage)tileEntity).receiveEnergy(side.Opposite, eout, false, dt));
    }

    public float receiveEnergy(BlockFacing from, float maxReceive, bool simulate, float dt)
    {
        if (from == null || sideConfig[from] == IOEnergySideConfig.INPUT)
        {
            return energyStorage.receiveEnergy(Math.Min(energyStorage.getLimitReceive() * dt, maxReceive), simulate, dt);
        }
        return 0;
    }

    public bool CanWireConnect(BlockFacing side)
    {
        return sideConfig[side] != IOEnergySideConfig.NONE;
    }

    public IOEnergySideConfig getEnergySideConfig(BlockFacing side)
    {
        return sideConfig[side];
    }

    public bool toggleSide(BlockFacing side)//, PlayerEntity player
    {
        if (Api is ICoreServerAPI)
        {
            sideConfig[side] = sideConfig[side].next();

            Api.World.BlockAccessor.ExchangeBlock(Api.World.GetBlock(Block.CodeWithVariant(side.ToString(), sideConfig[side].toString())).BlockId, Pos);
        }

        //MarkDirty();
        return true;
    }

    public FluxStorage GetFluxStorage()
    {
        return energyStorage;
    }
}

public class BlockTFCapacitor : BlockSideconfigInteractions, IFluxStorageItem
{
    int maxCapacity = 1;

    public override void OnLoaded(ICoreAPI api)
    {
        maxCapacity = MyMiniLib.GetAttributeInt(this, "storage", 1);
        Durability = 100;
    }

    public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
    {
        base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
        dsc.AppendLine(inSlot.Itemstack.Attributes.GetInt("energy", 0) + "/" + maxCapacity);
    }

    public int receiveEnergy(ItemStack itemstack, int maxReceive)
    {
        int energy = itemstack.Attributes.GetInt("energy", 0);
        int received = Math.Min(maxCapacity - energy, maxReceive);
        itemstack.Attributes.SetInt("energy", energy + received);
        int durab = (energy + received) / (maxCapacity / GetDurability(itemstack));
        itemstack.Attributes.SetInt("durability", durab);
        return received;
    }

    public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
    {
        TFCapacitor be = world.BlockAccessor.GetBlockEntity(pos) as TFCapacitor;
        ItemStack item = new ItemStack(world.BlockAccessor.GetBlock(new AssetLocation("temporalengineering:capacitor-"+ FirstCodePart(1) +"-input-input-input-input-input-input")));
        if (be != null) item.Attributes.SetInt("energy", (int)be.energyStorage?.getEnergyStored());
        return new ItemStack[] {item};
    }

    public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
    {
        ItemStack item = new ItemStack(world.BlockAccessor.GetBlock(new AssetLocation("temporalengineering:capacitor-" + FirstCodePart(1) + "-input-input-input-input-input-input")));
        TFCapacitor be = world.BlockAccessor.GetBlockEntity(pos) as TFCapacitor;
        if (be != null) item.Attributes.SetInt("energy", (int)be.energyStorage?.getEnergyStored());
        return item;
    }

    public override void OnBlockPlaced(IWorldAccessor world, BlockPos blockPos, ItemStack byItemStack = null)
    {
        base.OnBlockPlaced(world, blockPos, byItemStack);
        if (byItemStack != null)
        {
            TFCapacitor be = world.BlockAccessor.GetBlockEntity(blockPos) as TFCapacitor;
            be.energyStorage.setEnergy(byItemStack.Attributes.GetInt("energy", 0));
        }
    }
}