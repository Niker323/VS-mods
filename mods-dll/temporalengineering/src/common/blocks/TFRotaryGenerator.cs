﻿
using System;
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
using Vintagestory.GameContent;
using Vintagestory.GameContent.Mechanics;

public class TFRotaryGenerator : BlockEntity, IFluxStorage
{
    public FluxStorage energyStorage;
    private float generation = 0;
    private int maxgenerate;
    private int maxspeed = 1;

    BEBehaviorTFRotaryGenerator pvBh;

    public BlockFacing Facing = BlockFacing.NORTH;

    public Vec4f lightRbs = new Vec4f();
    public virtual Vec4f LightRgba { get { return lightRbs; } }

    internal Matrixf mat = new Matrixf();

    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
    {
        base.GetBlockInfo(forPlayer, dsc);
        if (energyStorage != null)
        {
            dsc.AppendLine(energyStorage.GetFluxStorageInfo());
        }

        dsc.AppendLine(Lang.Get("Generation") + ": " + Math.Round(generation) + " TF/s");
    }

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
    {
        base.FromTreeAttributes(tree, worldForResolving);

        if (energyStorage == null)
        {
            energyStorage = new FluxStorage(MyMiniLib.GetAttributeInt(Block, "storage", 10000), 0, MyMiniLib.GetAttributeInt(Block, "output", 500));
        }
        energyStorage.setEnergy(tree.GetFloat("energy"));
        if (Api != null && Api.World.Side == EnumAppSide.Client)
        {
            generation = tree.GetFloat("gen");
        }
    }

    public override void ToTreeAttributes(ITreeAttribute tree)
    {
        base.ToTreeAttributes(tree);

        if (energyStorage != null)
        {
            tree.SetFloat("energy", energyStorage.getEnergyStored());
        }
        if (Api != null && Api.World.Side == EnumAppSide.Server)
        {
            tree.SetFloat("gen", generation);
        }
    }

    public override void Initialize(ICoreAPI api)
    {
        base.Initialize(api);
        Api = api;

        pvBh = GetBehavior<BEBehaviorTFRotaryGenerator>();

        if (api.World.Side == EnumAppSide.Server)
        {
            RegisterGameTickListener(tick, 100);
        }

        if (energyStorage == null)
        {
            energyStorage = new FluxStorage(MyMiniLib.GetAttributeInt(Block, "storage", 10000), 0, MyMiniLib.GetAttributeInt(Block, "output", 500));
        }

        maxgenerate = MyMiniLib.GetAttributeInt(Block, "maxgenerate", 200);
        maxspeed = MyMiniLib.GetAttributeInt(Block, "maxspeed", 5);

        Facing = BlockFacing.FromCode(Block.Variant["side"]);
        if (Facing == null) Facing = BlockFacing.NORTH;
    }

    private void tick(float dt)
    {
        foreach (BlockFacing f in BlockFacing.ALLFACES)
            transferEnergy(f, dt);

        if (pvBh != null && pvBh.Network != null)
        {
            generation = Math.Min(maxgenerate, Math.Abs(maxgenerate * pvBh.Network.Speed * pvBh.GearedRatio / maxspeed));
            energyStorage.modifyEnergyStored(generation * dt);
        }
        else
        {
            generation = 0;
        }
        MarkDirty();
    }

    protected void transferEnergy(BlockFacing side, float dt)
    {
        BlockPos outPos = Pos.Copy().Offset(side);
        BlockEntity tileEntity = Api.World.BlockAccessor.GetBlockEntity(outPos);
        if (tileEntity == null) return;
        if (!(tileEntity is IFluxStorage)) return;
        float eout = Math.Min(energyStorage.getLimitExtract() * dt, energyStorage.getEnergyStored() * dt);
        energyStorage.modifyEnergyStored(-((IFluxStorage)tileEntity).receiveEnergy(side.Opposite, eout, false, dt));
    }

    public float receiveEnergy(BlockFacing from, float maxReceive, bool simulate, float dt)
    {
        return 0;
    }

    public FluxStorage GetFluxStorage()
    {
        return energyStorage;
    }

    public bool CanWireConnect(BlockFacing side)
    {
        return true;
    }
}

public class BEBehaviorTFRotaryGenerator : BEBehaviorMPBase//BEBehaviorMPRotor
{
    float rotateY = 0f;
    float rotateZ = 0f;

    protected BlockFacing ownFacing;

    public BEBehaviorTFRotaryGenerator(BlockEntity blockentity) : base(blockentity)
    {
    }

    public override void Initialize(ICoreAPI api, JsonObject properties)
    {
        base.Initialize(api, properties);
    }

    protected override CompositeShape GetShape()
    {
        CompositeShape shape = Block.Shape.Clone();
        shape.Base = new AssetLocation("temporalengineering:shapes/block/generator/shaft.json");
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
        Shape shape = capi.Assets.TryGet("temporalengineering:shapes/block/generator/body.json").ToObject<Shape>();
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

    public override float GetResistance()
    {
        return 0;//.085f;
    }
}

public class BlockTFRotaryGenerator : BlockMPBase
{
    BlockFacing powerOutFacing;

    public override void OnLoaded(ICoreAPI api)
    {
        powerOutFacing = BlockFacing.FromCode(Variant["side"]).Opposite;

        base.OnLoaded(api);
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
                    if (block is BlockTFRotaryGenerator) return false;

                    Block toPlaceBlock = world.GetBlock(CodeWithParts(face.Opposite.Code));
                    world.BlockAccessor.SetBlock(toPlaceBlock.BlockId, blockSel.Position);

                    block.DidConnectAt(world, pos, face.Opposite);
                    WasPlaced(world, blockSel.Position, face);

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
            return true;
        }

        bool ok = base.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref failureCode);
        if (ok)
        {
            WasPlaced(world, blockSel.Position, null);
        }
        return ok;
    }

    public override void DidConnectAt(IWorldAccessor world, BlockPos pos, BlockFacing face)
    {

    }

    public override bool CanAttachBlockAt(IBlockAccessor blockAccessor, Block block, BlockPos pos, BlockFacing blockFace, Cuboidi attachmentArea = null)
    {
        return true;
    }
}