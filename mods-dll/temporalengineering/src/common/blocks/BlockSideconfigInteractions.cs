using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

public class BlockSideconfigInteractions : Block
{
    WorldInteraction[] interactions;

    public override void OnLoaded(ICoreAPI api)
    {
        base.OnLoaded(api);

        interactions = ObjectCacheUtil.GetOrCreate(api, "capacitorInteractions", () =>
        {
            return new WorldInteraction[]
            {
                    new WorldInteraction()
                    {
                        ActionLangCode = "blockhelp-temporalengineering-sideconfig",
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = new ItemStack[] { new ItemStack(api.World.GetItem(new AssetLocation("temporalengineering:wranch"))) }
                    },
            };
        });
    }

    public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
    {
        return interactions.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
    }
}