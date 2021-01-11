
using System.Diagnostics;
using Vintagestory.API.Common;

class ItemWire : Item
{
    BlockEntity memory = null;

    public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
    {
        handling = EnumHandHandling.Handled;
        if (byEntity != null && blockSel != null)
        {
            BlockEntity block = byEntity.World.BlockAccessor.GetBlockEntity(blockSel.Position);
            if (block is IWirePoint)
            {
                if (memory == null)
                {
                    memory = block;
                }
                else
                {
                    if (memory != block)
                    {
                        new WireClass(api, memory, block);
                        memory = null;
                    }
                    else
                    {
                        memory = null;
                    }
                }
            }
        }
    }
}