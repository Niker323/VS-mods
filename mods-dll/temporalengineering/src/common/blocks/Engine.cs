using System;
using System.Collections.Generic;
using System.Diagnostics;
using Vintagestory.API;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent.Mechanics;

public class BlockTFEngine : BlockMPBase, IMPPowered
{
    BlockFacing powerOutFacing;

    public override void OnLoaded(ICoreAPI api)
    {
        powerOutFacing = BlockFacing.FromCode(Variant["side"]).Opposite;

        base.OnLoaded(api);
    }

    public override void DidConnectAt(IWorldAccessor world, BlockPos pos, BlockFacing face)
    {

    }

    public override bool HasMechPowerConnectorAt(IWorldAccessor world, BlockPos pos, BlockFacing face)
    {
        return face == powerOutFacing;
    }

    public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
    {
        if (!CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
        {
            return false;
        }

        foreach (BlockFacing face in BlockFacing.ALLFACES)
        {
            BlockPos pos = blockSel.Position.AddCopy(face);
            IMechanicalPowerBlock block = world.BlockAccessor.GetBlock(pos) as IMechanicalPowerBlock;
            if (block != null)
            {
                if (block.HasMechPowerConnectorAt(world, pos, face.Opposite))
                {
                    //Prevent rotor back-to-back placement
                    if (block is IMPPowered) return false;

                    Block toPlaceBlock = world.GetBlock(CodeWithParts(face.Opposite.Code));
                    world.BlockAccessor.SetBlock(toPlaceBlock.BlockId, blockSel.Position);

                    block.DidConnectAt(world, pos, face.Opposite);
                    WasPlaced(world, blockSel.Position, face);

                    powerOutFacing = face;
                    return true;
                }
            }
        }

        if (byPlayer != null && byPlayer.Entity != null)
        {
            Vec3f vec = new Vec3d().Ahead(1f, byPlayer.Entity.Pos.Pitch, byPlayer.Entity.Pos.Yaw).ToVec3f();
            BlockFacing face = BlockFacing.FromNormal(vec);
            Block toPlaceBlock = world.GetBlock(CodeWithParts(face.Opposite.Code));
            world.BlockAccessor.SetBlock(toPlaceBlock.BlockId, blockSel.Position);
            WasPlaced(world, blockSel.Position, null);
            powerOutFacing = face;
            return true;
        }

        bool ok = base.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref failureCode);
        if (ok)
        {
            WasPlaced(world, blockSel.Position, null);
        }
        return ok;
    }

    public override bool CanAttachBlockAt(IBlockAccessor blockAccessor, Block block, BlockPos pos, BlockFacing blockFace, Cuboidi attachmentArea = null)
    {
        return true;
    }
}

public class BlockEntityTFEngine : BlockEntity, IFluxStorage
{
    FluxStorage energyStorage;


    public BlockEntityTFEngine()
    {

    }

    public override void Initialize(ICoreAPI api)
    {
        base.Initialize(api);

        if (energyStorage == null)
        {
            energyStorage = new FluxStorage(10000, 10000, 0);
        }

        RegisterGameTickListener(OnTick, 250);
    }

    public float receiveEnergy(BlockFacing from, float maxReceive, bool simulate, float dt)
    {
        return energyStorage.receiveEnergy(Math.Min(energyStorage.getLimitReceive() * dt, maxReceive), simulate, dt);
    }

    public FluxStorage GetFluxStorage()
    {
        return energyStorage;
    }

    public bool CanWireConnect(BlockFacing side)
    {
        return true;
    }

    private void OnTick(float dt)
    {
        // Only tick on the server and merely sync to client
        if (Api is ICoreClientAPI)
        {
            return;
        }

        float res = Math.Min(1000 * dt, energyStorage.getEnergyStored() * dt);
        energyStorage.modifyEnergyStored(-res);
        ((BEBehaviorTFEngine)Behaviors[0]).speed = res / (1000 * dt);
    }

    #region Events

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
    {
        base.FromTreeAttributes(tree, worldForResolving);

        if (energyStorage == null)
        {
            energyStorage = new FluxStorage(10000, 10000, 0);
        }
        energyStorage.setEnergy(tree.GetFloat("energy", 0));
    }

    public override void ToTreeAttributes(ITreeAttribute tree)
    {
        base.ToTreeAttributes(tree);

        if (energyStorage != null)
        {
            tree.SetFloat("energy", energyStorage.getEnergyStored());
        }
    }

    public override void OnBlockBroken()
    {
        base.OnBlockBroken();
    }

    #endregion

}

public class BEBehaviorTFEngine : BEBehaviorMPRotor
{
    float rotateY = 0f;
    float rotateZ = 0f;
    public float speed = 0;

    protected override float Resistance
    {
        get
        {
            return 0.3f;
        }
    }

    protected override double AccelerationFactor
    {
        get
        {
            return 1d;
        }
    }

    protected override float TargetSpeed
    {
        get
        {
            return 1 * speed;
        }
    }

    protected override float TorqueFactor
    {
        get
        {
            return 1 * speed;
        }
    }

    public BEBehaviorTFEngine(BlockEntity blockentity) : base(blockentity)
    {

    }

    protected override CompositeShape GetShape()
    {
        CompositeShape shape = Block.Shape.Clone();
        shape.Base = new AssetLocation("temporalengineering:shapes/block/testmodel3.json");
        switch (BlockFacing.FromCode(Block.Variant["side"]).Index)
        {
            case 0:
                rotateY = 90;
                break;
            case 1:
                rotateY = 0;
                break;
            case 2:
                rotateY = 270;
                break;
            case 3:
                rotateY = 180;
                break;
            case 4:
                rotateZ = 90;
                break;
            case 5:
                rotateZ = 270;
                break;
            default:
                break;
        }
        shape.rotateY = rotateY;
        shape.rotateZ = rotateZ;
        return shape;
    }

    public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
    {
        ICoreClientAPI capi = Api as ICoreClientAPI;
        Shape shape = capi.Assets.TryGet("temporalengineering:shapes/block/testmodel2.json").ToObject<Shape>();
        switch (BlockFacing.FromCode(Block.Variant["side"]).Index)
        {
            case 0:
                AxisSign = new int[] { 0, 0, -1 };
                rotateY = 180;
                break;
            case 1:
                AxisSign = new int[] { -1, 0, 0 };
                rotateY = 90;
                break;
            case 2:
                AxisSign = new int[] { 0, 0, -1 };
                rotateY = 0;
                break;
            case 3:
                AxisSign = new int[] { -1, 0, 0 };
                rotateY = 270;
                break;
            case 4:
                AxisSign = new int[] { 0, 1, 0 };
                rotateZ = 270;
                break;
            case 5:
                AxisSign = new int[] { 0, 1, 0 };
                rotateZ = 90;
                break;
            default:
                break;
        }
        MeshData mesh;
        capi.Tesselator.TesselateShape(Block, shape, out mesh, new Vec3f(rotateZ, rotateY, 0));
        mesher.AddMeshData(mesh);
        return true;
    }
}