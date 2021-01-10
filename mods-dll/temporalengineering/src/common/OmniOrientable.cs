using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

public class BlockBehaviorOmniOrientable : BlockBehavior
{
    string dropBlockFace = "north";
    JsonItemStack drop = null;

    public BlockBehaviorOmniOrientable(Block block) : base(block)
    {

    }

    public override void Initialize(JsonObject properties)
    {
        base.Initialize(properties);
        if (properties["dropBlockFace"].Exists)
        {
            dropBlockFace = properties["dropBlockFace"].AsString();
        }
        if (properties["drop"].Exists)
        {
            drop = properties["drop"].AsObject<JsonItemStack>(null, block.Code.Domain);

        }
    }

    public override void OnLoaded(ICoreAPI api)
    {
        base.OnLoaded(api);

        drop?.Resolve(api.World, "OmniOrientable drop for " + block.Code);
    }

    public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref EnumHandling handling, ref string failureCode)
    {
        handling = EnumHandling.PreventDefault;
        Vec3f vec = new Vec3d().Ahead(1f, byPlayer.Entity.Pos.Pitch, byPlayer.Entity.Pos.Yaw).ToVec3f();
        BlockFacing face = BlockFacing.FromNormal(vec).Opposite;
        AssetLocation blockCode = block.CodeWithVariant("side", face.Code);
        Block orientedBlock = world.BlockAccessor.GetBlock(blockCode);

        if (orientedBlock.CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
        {
            orientedBlock.DoPlaceBlock(world, byPlayer, blockSel, itemstack);
            return true;
        }
        return false;
    }


    public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, ref float dropQuantityMultiplier, ref EnumHandling handled)
    {
        handled = EnumHandling.PreventDefault;
        if (drop?.ResolvedItemstack != null)
        {
            return new ItemStack[] { drop?.ResolvedItemstack.Clone() };
        }
        return new ItemStack[] { new ItemStack(world.BlockAccessor.GetBlock(block.CodeWithVariant("side", dropBlockFace))) };
    }

    public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos, ref EnumHandling handled)
    {
        handled = EnumHandling.PreventDefault;
        if (drop != null)
        {
            return drop?.ResolvedItemstack.Clone();
        }

        return new ItemStack(world.BlockAccessor.GetBlock(block.CodeWithVariant("side", dropBlockFace)));
    }


    public override AssetLocation GetRotatedBlockCode(int angle, ref EnumHandling handled)
    {
        handled = EnumHandling.PreventDefault;

        BlockFacing beforeFacing = BlockFacing.FromCode(block.LastCodePart());
        int rotatedIndex = GameMath.Mod(beforeFacing.HorizontalAngleIndex - angle / 90, 4);
        BlockFacing nowFacing = BlockFacing.HORIZONTALS_ANGLEORDER[rotatedIndex];

        return block.CodeWithVariant("side", nowFacing.Code);
    }

    public override AssetLocation GetHorizontallyFlippedBlockCode(EnumAxis axis, ref EnumHandling handling)
    {
        handling = EnumHandling.PreventDefault;

        BlockFacing facing = BlockFacing.FromCode(block.LastCodePart());
        if (facing.Axis == axis)
        {
            return block.CodeWithVariant("side", facing.Opposite.Code);
        }
        return block.Code;
    }


}