
using System;
using System.Diagnostics;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

class TFChisel : ItemChisel, IFluxStorageItem
{
    public override void OnHeldAttackStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handling)
    {
        int energy = slot.Itemstack.Attributes.GetInt("energy", 0);
        if (energy >= 20)
        {
            energy -= 20;
            slot.Itemstack.Attributes.SetInt("durability", Math.Max(1, energy / 20));
            slot.Itemstack.Attributes.SetInt("energy", energy);
            base.OnHeldAttackStart(slot, byEntity, blockSel, entitySel, ref handling);
        }
        else
        {
            slot.Itemstack.Attributes.SetInt("durability", 1);
        }
        slot.MarkDirty();
    }

    public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
    {
        int energy = slot.Itemstack.Attributes.GetInt("energy", 0);
        if (energy >= 20)
        {
            energy -= 20;
            slot.Itemstack.Attributes.SetInt("durability", Math.Max(1, energy / 20));
            slot.Itemstack.Attributes.SetInt("energy", energy);
            base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
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
        dsc.AppendLine(inSlot.Itemstack.Attributes.GetInt("energy", 0) + "/20000");
    }

    public int receiveEnergy(ItemStack itemstack, int maxReceive)
    {
        int received = Math.Min(20000 - itemstack.Attributes.GetInt("energy", 0), maxReceive);
        itemstack.Attributes.SetInt("energy", itemstack.Attributes.GetInt("energy", 0) + received);
        int durab = Math.Max(1, itemstack.Attributes.GetInt("energy", 0) / 20);
        itemstack.Attributes.SetInt("durability", durab);
        return received;
    }
}