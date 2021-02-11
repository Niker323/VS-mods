
using System;
using System.Diagnostics;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;


class TFDrill : Item, IFluxStorageItem
{
    int consume = 20;
    int storage = 20000;
    int speed = 0;
    public override void OnLoaded(ICoreAPI api)
    {
        base.OnLoaded(api);

        consume = MyMiniLib.GetAttributeInt(this, "consume", consume);
        storage = MyMiniLib.GetAttributeInt(this, "storage", storage);
        Durability = storage / consume;
    }

    public override void DamageItem(IWorldAccessor world, Entity byEntity, ItemSlot itemslot, int amount = 1)
    {
        int energy = itemslot.Itemstack.Attributes.GetInt("energy", 0);
        if (energy >= consume * amount)
        {
            energy -= consume * amount;
            itemslot.Itemstack.Attributes.SetInt("durability", Math.Max(1, energy / consume));
            itemslot.Itemstack.Attributes.SetInt("energy", energy);
        }
        else
        {
            itemslot.Itemstack.Attributes.SetInt("durability", 1);
        }
    }

    public override void OnHeldAttackStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handling)
    {
        int energy = slot.Itemstack.Attributes.GetInt("energy", 0);
        if (energy >= consume)
        {
            energy -= consume;
            slot.Itemstack.Attributes.SetInt("durability", Math.Max(1, energy / consume));
            slot.Itemstack.Attributes.SetInt("energy", energy);
            base.OnHeldAttackStart(slot, byEntity, blockSel, entitySel, ref handling);
        }
        else
        {
            slot.Itemstack.Attributes.SetInt("durability", 1);
        }
        slot.MarkDirty();
    }
    public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
    {
        base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
        dsc.AppendLine(inSlot.Itemstack.Attributes.GetInt("energy", 0) + "/" + storage + " TF");
    }

    public int receiveEnergy(ItemStack itemstack, int maxReceive)
    {
        int received = Math.Min(storage - itemstack.Attributes.GetInt("energy", 0), maxReceive);
        itemstack.Attributes.SetInt("energy", itemstack.Attributes.GetInt("energy", 0) + received);
        int durab = Math.Max(1, itemstack.Attributes.GetInt("energy", 0) / consume);
        itemstack.Attributes.SetInt("durability", durab);
        return received;
    }

    public override bool OnBlockBrokenWith(IWorldAccessor world, Entity byEntity, ItemSlot slot, BlockSelection blockSel, float dropQuantityMultiplier = 1)
    {
        int energy = slot.Itemstack.Attributes.GetInt("energy", 0);
        if (energy >= consume)
                {
                    if (base.OnBlockBrokenWith(world, byEntity, slot, blockSel, dropQuantityMultiplier))
                    {
                        if (byEntity is EntityPlayer)
                        {
                            var player = world.PlayerByUid((byEntity as EntityPlayer).PlayerUID);
                            {
                                switch (blockSel.Face.Axis)
                                {
                                    case EnumAxis.X:
                                        destroyBlocks(world, blockSel.Position.AddCopy(0, -1, -1),
                                            blockSel.Position.AddCopy(0, 1, 1), player, blockSel.Position, slot);
                                        break;
                                    case EnumAxis.Y:
                                        destroyBlocks(world, blockSel.Position.AddCopy(-1, 0, -1),
                                            blockSel.Position.AddCopy(1, 0, 1), player, blockSel.Position, slot);
                                        break;
                                    case EnumAxis.Z:
                                        destroyBlocks(world, blockSel.Position.AddCopy(-1, -1, 0),
                                            blockSel.Position.AddCopy(1, 1, 0), player, blockSel.Position, slot);
                                        break;
                                }
                            }

                        }
                        return true;
                    }
                    return false;
                }
        else
            {
            return false;
        }
    }


    //credit to stitch37 for this code
    public void destroyBlocks(IWorldAccessor world, BlockPos min, BlockPos max, IPlayer player, BlockPos centerBlockPos, ItemSlot slot)
    {
        int energy = slot.Itemstack.Attributes.GetInt("energy", 0);
        var centerBlock = world.BlockAccessor.GetBlock(centerBlockPos);
        var itemStack = new ItemStack(this);
        Block tempBlock;
        var miningTimeMainBlock = GetMiningSpeed(itemStack, centerBlock, player);
        float miningTime;
        var tempPos = new BlockPos();
        for (int x = min.X; x <= max.X; x++)
        {
            for (int y = min.Y; y <= max.Y; y++)
            {
                for (int z = min.Z; z <= max.Z; z++)
                {
                    tempPos.Set(x, y, z);
                    tempBlock = world.BlockAccessor.GetBlock(tempPos);
                    if (player.WorldData.CurrentGameMode == EnumGameMode.Creative)
                        world.BlockAccessor.SetBlock(0, tempPos);
                    else
                    {
                        if (energy >= consume)
                        {
                            miningTime = tempBlock.GetMiningSpeed(itemStack, tempBlock, player);
                            if (this.ToolTier >= tempBlock.RequiredMiningTier
                                && miningTimeMainBlock * 1.5f >= miningTime
                                && MiningSpeed.ContainsKey(tempBlock.BlockMaterial))

                            {
                                world.BlockAccessor.BreakBlock(tempPos, player);
                            }
                        }
                    }
                }
            }
        }
    }

}