using System.Diagnostics;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent.Mechanics;

public class BlockMPMultiblockBase : Block
{
    public override float OnGettingBroken(IPlayer player, BlockSelection blockSel, ItemSlot itemslot, float remainingResistance, float dt, int counter)
    {
        IWorldAccessor world = null;
        if (player == null && player.Entity == null) world = player.Entity.World;
        if (world == null) world = api.World;
        BEMPMultiblockBase be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BEMPMultiblockBase;
        if (be == null || be.Principal == null) return 1f;  //never break
        Block centerBlock = world.BlockAccessor.GetBlock(be.Principal);
        BlockSelection bs = blockSel.Clone();
        bs.Position = be.Principal;
        return centerBlock.OnGettingBroken(player, bs, itemslot, remainingResistance, dt, counter);
    }

    public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
    {
        BEMPMultiblockBase be = world.BlockAccessor.GetBlockEntity(pos) as BEMPMultiblockBase;
        if (be == null || be.Principal == null)
        {
            // being broken by other game code (including on breaking the center large gear): standard block breaking treatment
            base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
            return;
        }
        // being broken by player: break the center block instead
        BlockPos centerPos = be.Principal;
        Block centerBlock = world.BlockAccessor.GetBlock(centerPos);
        centerBlock.OnBlockBroken(world, centerPos, byPlayer, dropQuantityMultiplier);

        // Need to trigger neighbourchange on client side only (because it's normally in the player block breaking code)
        if (api.Side == EnumAppSide.Client)
        {
            foreach (BlockFacing facing in BlockFacing.VERTICALS)
            {
                BlockPos npos = centerPos.AddCopy(facing);
                world.BlockAccessor.GetBlock(npos).OnNeighbourBlockChange(world, npos, centerPos);
            }
        }
    }

    public override Cuboidf GetParticleBreakBox(IBlockAccessor blockAccess, BlockPos pos, BlockFacing facing)
    {
        BEMPMultiblockBase be = blockAccess.GetBlockEntity(pos) as BEMPMultiblockBase;
        if (be == null || be.Principal == null)
        {
            return base.GetParticleBreakBox(blockAccess, pos, facing);
        }
        // being broken by player: break the center block instead
        Block centerBlock = blockAccess.GetBlock(be.Principal);
        return centerBlock.GetParticleBreakBox(blockAccess, be.Principal, facing);
    }

    //Need to override because this fake block has no texture of its own (no texture gives black breaking particles)
    public override int GetRandomColor(ICoreClientAPI capi, BlockPos pos, BlockFacing facing)
    {
        IBlockAccessor blockAccess = capi.World.BlockAccessor;
        BEMPMultiblockBase be = blockAccess.GetBlockEntity(pos) as BEMPMultiblockBase;
        if (be == null || be.Principal == null)
        {
            return 0;
        }
        Block centerBlock = blockAccess.GetBlock(be.Principal);
        return centerBlock.GetRandomColor(capi, be.Principal, facing);
    }
}

public class BEMPMultiblockBase : BlockEntity
{
    public BlockPos Principal { get; set; }

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor world)
    {
        base.FromTreeAttributes(tree, world);
        int cx = tree.GetInt("cx");
        int cy = tree.GetInt("cy");
        int cz = tree.GetInt("cz");
        // (-1, -1, -1) signifies a null center; this cannot happen spontaneously
        if (cy == -1 && cx == -1 && cz == -1)
        {
            Principal = null;
        }
        else
        {
            Principal = new BlockPos(cx, cy, cz);
        }
    }

    public override void ToTreeAttributes(ITreeAttribute tree)
    {
        base.ToTreeAttributes(tree);
        // (-1, -1, -1) signifies a null center; this cannot happen spontaneously
        tree.SetInt("cx", Principal == null ? -1 : Principal.X);
        tree.SetInt("cy", Principal == null ? -1 : Principal.Y);
        tree.SetInt("cz", Principal == null ? -1 : Principal.Z);
    }

    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder sb)
    {
        if (Principal == null) return;

        BlockEntity be = Api.World.BlockAccessor.GetBlockEntity(Principal);
        if (be == null) sb.AppendLine("null be");
        else be.GetBlockInfo(forPlayer, sb);
    }
}