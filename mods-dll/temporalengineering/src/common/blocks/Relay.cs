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
using Vintagestory.API.Server;
using Vintagestory.API.Util;

public class BETFRelay : BlockEntity, IFluxStorage, IIOEnergySideConfig
{
    public Dictionary<BlockFacing, IOEnergySideConfig> sideConfig = new Dictionary<BlockFacing, IOEnergySideConfig>(IOEnergySideConfig.VALUES.Length);
    public FluxStorage energyStorage;
    public BlockFacing face;
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
            energyStorage = new FluxStorage(MyMiniLib.GetAttributeInt(Block, "transfer", 10000), MyMiniLib.GetAttributeInt(Block, "transfer", 10000), MyMiniLib.GetAttributeInt(Block, "transfer", 10000));
        }
        energyStorage.setEnergy(tree.GetFloat("energy"));
        state = tree.GetBool("state", false);
    }

    public override void ToTreeAttributes(ITreeAttribute tree)
    {
        base.ToTreeAttributes(tree);

        if (energyStorage != null)
        {
            tree.SetFloat("energy", energyStorage.getEnergyStored());
        }
        tree.SetBool("state", state);
    }

    public override void Initialize(ICoreAPI api)
    {
        base.Initialize(api);
        Api = api;
        face = BlockFacing.FromCode(Block.Variant["side"]);

        if (api.World.Side == EnumAppSide.Server)
        {
            RegisterGameTickListener(tick, 250);
        }

        if (energyStorage == null)
        {
            energyStorage = new FluxStorage(MyMiniLib.GetAttributeInt(Block, "transfer", 10000), MyMiniLib.GetAttributeInt(Block, "transfer", 10000), MyMiniLib.GetAttributeInt(Block, "transfer", 10000));
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
        if (face != null) sideConfig[face] = IOEnergySideConfig.NONE;
    }

    private void tick(float dt)
    {
        if (state)
        {
            foreach (BlockFacing f in BlockFacing.ALLFACES)
                transferEnergy(f, dt);
            MarkDirty();
        }
    }

    protected void transferEnergy(BlockFacing side, float dt)
    {
        if (sideConfig[side] != IOEnergySideConfig.OUTPUT) return;
        BlockPos outPos = Pos.Copy().Offset(side);
        BlockEntity tileEntity = Api.World.BlockAccessor.GetBlockEntity(outPos);
        if (tileEntity == null) return;
        if (!(tileEntity is IFluxStorage)) return;
        float eout = Math.Min(MyMiniLib.GetAttributeInt(Block, "transfer", 10000) * dt, energyStorage.getEnergyStored() * dt);
        energyStorage.modifyEnergyStored(-((IFluxStorage)tileEntity).receiveEnergy(side.Opposite, eout, false, dt));
    }

    public float receiveEnergy(BlockFacing from, float maxReceive, bool simulate, float dt)
    {
        if (from == null || sideConfig[from] == IOEnergySideConfig.INPUT)
        {
            return energyStorage.receiveEnergy(Math.Min(MyMiniLib.GetAttributeInt(Block, "transfer", 10000) * dt, maxReceive), simulate, dt);
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
        if (Api is ICoreServerAPI && side != face)
        {
            sideConfig[side] = sideConfig[side].next();

            Api.World.BlockAccessor.ExchangeBlock(Api.World.GetBlock(Block.CodeWithVariant(side.ToString(), sideConfig[side].toString())).BlockId, Pos);
            return true;
        }

        //MarkDirty();
        return false;
    }

    public bool OnInteract(IPlayer byPlayer)
    {
        //BEBehaviorMPTransmission be = Api.World.BlockAccessor.GetBlockEntity(transmissionPos)?.GetBehavior<BEBehaviorMPTransmission>();
        //if (!Engaged && be != null && be.engaged) return true;
        state = !state;
        if (state) Api.World.BlockAccessor.ExchangeBlock(Api.World.GetBlock(Block.CodeWithVariant("state", "on")).BlockId, Pos);
        else Api.World.BlockAccessor.ExchangeBlock(Api.World.GetBlock(Block.CodeWithVariant("state", "off")).BlockId, Pos);
        Api.World.PlaySoundAt(new AssetLocation("sounds/effect/woodswitch.ogg"), Pos.X + 0.5, Pos.Y + 0.5, Pos.Z + 0.5, byPlayer);
        //if (be != null)
        //{
        //    CheckEngaged(Api.World.BlockAccessor, true);
        //}

        MarkDirty(true);
        return true;
    }

    public FluxStorage GetFluxStorage()
    {
        return energyStorage;
    }
}

public class BlockTFRelay : BlockSideconfigInteractions
{
    BlockFacing face;
    int transfer = 10000;

    public override void OnLoaded(ICoreAPI api)
    {
        face = BlockFacing.FromCode(Variant["side"]);
        transfer = MyMiniLib.GetAttributeInt(this, "transfer", transfer);

        base.OnLoaded(api);
    }

    public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
    {
        BETFRelay be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BETFRelay;
        if (be != null && (byPlayer.InventoryManager.ActiveHotbarSlot == null || byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack == null || byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack.Item == null || !(byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack.Item is Wrench)))
        {
            return be.OnInteract(byPlayer);
        }

        return base.OnBlockInteractStart(world, byPlayer, blockSel);
    }

    public override bool CanAttachBlockAt(IBlockAccessor blockAccessor, Block block, BlockPos pos, BlockFacing blockFace, Cuboidi attachmentArea = null)
    {
        return blockFace != face;
    }

    public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
    {
        return new ItemStack[] { new ItemStack(world.BlockAccessor.GetBlock(new AssetLocation("temporalengineering:relay-off-north-none-none-none-none-none-none"))) };
    }

    public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
    {
        return new ItemStack(world.BlockAccessor.GetBlock(new AssetLocation("temporalengineering:relay-off-north-none-none-none-none-none-none")));
    }

    public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
    {
        base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
        dsc.AppendLine(Lang.Get("Traversing") + ": " + transfer + " TF/s");
    }
}