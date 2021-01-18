
using System.Diagnostics;
using Vintagestory.API.Common;

class Wrench : Item
{
    public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
    {
        if (byEntity != null && blockSel != null)
        {
            BlockEntity block = byEntity.World.BlockAccessor.GetBlockEntity(blockSel.Position);
            if (block is IIOEnergySideConfig)
            {
                handling = EnumHandHandling.Handled;
                IIOEnergySideConfig iiosc = (IIOEnergySideConfig)block;
                if (iiosc.toggleSide(blockSel.Face)) api.World.BlockAccessor.TriggerNeighbourBlockUpdate(blockSel.Position);
            }
            //if (block is BlockEntityEnergyDuct)
            //{
            //    Debug.WriteLine(blockSel.HitPosition);
            //}
        }
    }
}