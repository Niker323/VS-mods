
using System.Diagnostics;
using Vintagestory.API.Common;

class Wrench : Item
{
    public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
    {
        handling = EnumHandHandling.Handled;
        if (byEntity != null && blockSel != null)
        {
            BlockEntity block = byEntity.World.BlockAccessor.GetBlockEntity(blockSel.Position);
            if (block is IIOEnergySideConfig)
            {
                IIOEnergySideConfig iiosc = (IIOEnergySideConfig)block;
                iiosc.toggleSide(blockSel.Face);
                api.World.BlockAccessor.TriggerNeighbourBlockUpdate(blockSel.Position);
            }
        }
    }
}