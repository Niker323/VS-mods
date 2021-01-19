using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

public class BlockTFForge : Block
{
    WorldInteraction[] interactions;
    int consume = 300;

    public override void OnLoaded(ICoreAPI api)
    {
        base.OnLoaded(api);

        consume = MyMiniLib.GetAttributeInt(this, "consume", consume);

        if (api.Side != EnumAppSide.Client) return;
        ICoreClientAPI capi = api as ICoreClientAPI;

        interactions = ObjectCacheUtil.GetOrCreate(api, "forgeBlockInteractions", () =>
        {
            List<ItemStack> heatableStacklist = new List<ItemStack>();
            List<ItemStack> fuelStacklist = new List<ItemStack>();
            List<ItemStack> canIgniteStacks = new List<ItemStack>();

            foreach (CollectibleObject obj in api.World.Collectibles)
            {
                string firstCodePart = obj.FirstCodePart();

                if (firstCodePart == "ingot" || firstCodePart == "metalplate" || firstCodePart == "workitem")
                {
                    List<ItemStack> stacks = obj.GetHandBookStacks(capi);
                    if (stacks != null) heatableStacklist.AddRange(stacks);
                }
                else
                {
                    if (obj.CombustibleProps != null)
                    {
                        if (obj.CombustibleProps.BurnTemperature > 1000)
                        {
                            List<ItemStack> stacks = obj.GetHandBookStacks(capi);
                            if (stacks != null) fuelStacklist.AddRange(stacks);
                        }
                    }
                }

                if (obj is Block && (obj as Block).HasBehavior<BlockBehaviorCanIgnite>())
                {
                    List<ItemStack> stacks = obj.GetHandBookStacks(capi);
                    if (stacks != null) canIgniteStacks.AddRange(stacks);
                }
            }

            return new WorldInteraction[] {
                    new WorldInteraction()
                    {
                        ActionLangCode = "blockhelp-forge-addworkitem",
                        HotKeyCode = "sneak",
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = heatableStacklist.ToArray(),
                        GetMatchingStacks = (wi, bs, es) =>
                        {
                            BlockEntityForge bef = api.World.BlockAccessor.GetBlockEntity(bs.Position) as BlockEntityForge;
                            if (bef!= null && bef.Contents != null)
                            {
                                return wi.Itemstacks.Where(stack => stack.Equals(api.World, bef.Contents, GlobalConstants.IgnoredStackAttributes)).ToArray();
                            }
                            return wi.Itemstacks;
                        }
                    },
                    new WorldInteraction()
                    {
                        ActionLangCode = "blockhelp-forge-takeworkitem",
                        HotKeyCode = null,
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = heatableStacklist.ToArray(),
                        GetMatchingStacks = (wi, bs, es) =>
                        {
                            BlockEntityForge bef = api.World.BlockAccessor.GetBlockEntity(bs.Position) as BlockEntityForge;
                            if (bef!= null && bef.Contents != null)
                            {
                                return new ItemStack[] { bef.Contents };
                            }
                            return null;
                        }
                    }
                };
        });
    }


    public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
    {
        BlockEntityTFForge bea = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityTFForge;
        if (bea != null)
        {
            return bea.OnPlayerInteract(world, byPlayer, blockSel);
        }

        return base.OnBlockInteractStart(world, byPlayer, blockSel);
    }

    public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
    {
        return interactions.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
    }

    public override bool CanAttachBlockAt(IBlockAccessor blockAccessor, Block block, BlockPos pos, BlockFacing blockFace, Cuboidi attachmentArea = null)
    {
        return blockFace != BlockFacing.UP;
    }

    public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
    {
        base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
        dsc.AppendLine(Lang.Get("Consumes") + ": " + consume + " TF/s");
    }
}

public class BlockEntityTFForge : BlockEntity, IHeatSource, IFluxStorage
{
    TFForgeContentsRenderer renderer;
    ItemStack contents;
    bool burning;
    int consume = 0;

    double lastTickTotalHours;
    ILoadedSound ambientSound;

    FluxStorage energyStorage;

    public ItemStack Contents
    {
        get
        {
            return contents;
        }
    }

    static SimpleParticleProperties smokeParticles;

    static BlockEntityTFForge()
    {
        smokeParticles = new SimpleParticleProperties(
               1, 1,
               ColorUtil.ToRgba(150, 80, 80, 80),
               new Vec3d(),
               new Vec3d(0.75, 0, 0.75),
               new Vec3f(-1 / 32f, 0.1f, -1 / 32f),
               new Vec3f(1 / 32f, 0.1f, 1 / 32f),
               2f,
               -0.025f / 4,
               0.2f,
               0.4f,
               EnumParticleModel.Quad
           );

        smokeParticles.SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -0.25f);
        smokeParticles.SelfPropelled = true;
        smokeParticles.AddPos.Set(8 / 16.0, 0, 8 / 16.0);
    }



    public override void Initialize(ICoreAPI api)
    {
        base.Initialize(api);
        if (contents != null) contents.ResolveBlockOrItem(api.World);

        if (energyStorage == null)
        {
            energyStorage = new FluxStorage(MyMiniLib.GetAttributeInt(Block, "storage", 10000), MyMiniLib.GetAttributeInt(Block, "input", 1000), 0);
        }

        if (api is ICoreClientAPI)
        {
            ICoreClientAPI capi = (ICoreClientAPI)api;
            capi.Event.RegisterRenderer(renderer = new TFForgeContentsRenderer(Pos, capi), EnumRenderStage.Opaque, "tfforge");
            renderer.SetContents(contents, burning, true);

            RegisterGameTickListener(OnClientTick, 500);
        }

        consume = MyMiniLib.GetAttributeInt(Block, "consume", 500);

        wsys = api.ModLoader.GetModSystem<WeatherSystemBase>();

        if (api.Side.IsServer())
        {
            RegisterGameTickListener(OnCommonTick, 250);
        }
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
        return side != BlockFacing.UP;
    }

    public void ToggleAmbientSounds(bool on)
    {
        if (Api.Side != EnumAppSide.Client) return;

        if (on)
        {
            if (ambientSound == null || !ambientSound.IsPlaying)
            {
                ambientSound = ((IClientWorldAccessor)Api.World).LoadSound(new SoundParams()
                {
                    Location = new AssetLocation("sounds/effect/embers.ogg"),
                    ShouldLoop = true,
                    Position = Pos.ToVec3f().Add(0.5f, 0.25f, 0.5f),
                    DisposeOnFinish = false,
                    Volume = 1
                });

                ambientSound.Start();
            }
        }
        else
        {
            ambientSound.Stop();
            ambientSound.Dispose();
            ambientSound = null;
        }

    }


    bool clientSidePrevBurning;
    private void OnClientTick(float dt)
    {
        if (Api != null && Api.Side == EnumAppSide.Client && clientSidePrevBurning != burning)
        {
            ToggleAmbientSounds(IsBurning);
            clientSidePrevBurning = IsBurning;
        }

        if (burning && Api.World.Rand.NextDouble() < 0.13)
        {
            smokeParticles.MinPos.Set(Pos.X + 4 / 16f, Pos.Y + 14 / 16f, Pos.Z + 4 / 16f);
            int g = 50 + Api.World.Rand.Next(50);
            smokeParticles.Color = ColorUtil.ToRgba(150, g, g, g);
            Api.World.SpawnParticles(smokeParticles);
        }
        if (renderer != null)
        {
            renderer.SetContents(contents, burning, false);
        }
    }


    WeatherSystemBase wsys;
    Vec3d tmpPos = new Vec3d();
    private void OnCommonTick(float dt)
    {
        if (energyStorage.getEnergyStored() >= consume * dt)
        {
            energyStorage.modifyEnergyStored(-consume * dt);
            if (burning == false)
            {
                burning = true;
                MarkDirty(true);
            }
        }
        else
        {
            if (burning == true)
            {
                burning = false;
                MarkDirty(true);
            }
        }

        if (burning)
        {
            double hoursPassed = Api.World.Calendar.TotalHours - lastTickTotalHours;

            if (contents != null)
            {
                float temp = contents.Collectible.GetTemperature(Api.World, contents);
                if (temp < 1100)
                {
                    float tempGain = (float)(hoursPassed * 1100);

                    //float diff = Math.Abs(temp - 1100);
                    //float tempGain = dt + dt * (diff / 28);

                    contents.Collectible.SetTemperature(Api.World, contents, Math.Min(1100, temp + tempGain));
                    MarkDirty(true);
                }
            }
        }


        tmpPos.Set(Pos.X + 0.5, Pos.Y + 0.5, Pos.Z + 0.5);
        double rainLevel = 0;
        bool rainCheck =
            Api.Side == EnumAppSide.Server
            && Api.World.Rand.NextDouble() < 0.15
            && Api.World.BlockAccessor.GetRainMapHeightAt(Pos.X, Pos.Z) <= Pos.Y
            && (rainLevel = wsys.GetPrecipitation(tmpPos)) > 0.1
        ;

        if (rainCheck && Api.World.Rand.NextDouble() < rainLevel * 5)
        {
            bool playsound = false;
            if (burning)
            {
                playsound = true;
                //fuelLevel -= (float)rainLevel / 250f;
                //if (Api.World.Rand.NextDouble() < rainLevel / 30f || fuelLevel <= 0)
                //{
                //    burning = false;
                //}
                MarkDirty(true);
            }


            float temp = contents == null ? 0 : contents.Collectible.GetTemperature(Api.World, contents);
            if (temp > 20)
            {
                playsound = temp > 100;
                contents.Collectible.SetTemperature(Api.World, contents, Math.Min(1100, temp - 8), false);
                MarkDirty(true);
            }

            if (playsound)
            {
                Api.World.PlaySoundAt(new AssetLocation("sounds/effect/extinguish"), Pos.X + 0.5, Pos.Y + 0.75, Pos.Z + 0.5, null, false, 16);
            }
        }

        lastTickTotalHours = Api.World.Calendar.TotalHours;
    }

    public bool IsBurning
    {
        get { return burning; }
    }

    internal bool OnPlayerInteract(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
    {
        ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;

        if (!byPlayer.Entity.Controls.Sneak)
        {
            if (contents == null) return false;
            ItemStack split = contents.Clone();
            split.StackSize = 1;
            contents.StackSize--;

            if (contents.StackSize == 0) contents = null;

            if (!byPlayer.InventoryManager.TryGiveItemstack(split))
            {
                world.SpawnItemEntity(split, Pos.ToVec3d().Add(0.5, 0.5, 0.5));
            }

            if (renderer != null)
            {
                renderer.SetContents(contents, burning, true);
            }
            MarkDirty();
            Api.World.PlaySoundAt(new AssetLocation("sounds/block/ingot"), Pos.X, Pos.Y, Pos.Z, byPlayer, false);

            return true;

        }
        else
        {
            if (slot.Itemstack == null) return false;

            string firstCodePart = slot.Itemstack.Collectible.FirstCodePart();
            bool forgableGeneric = false;
            if (slot.Itemstack.Collectible.Attributes != null)
            {
                forgableGeneric = slot.Itemstack.Collectible.Attributes.IsTrue("forgable") == true;
            }

            // Add heatable item
            if (contents == null && (firstCodePart == "ingot" || firstCodePart == "metalplate" || firstCodePart == "workitem" || forgableGeneric))
            {
                contents = slot.Itemstack.Clone();
                contents.StackSize = 1;

                slot.TakeOut(1);
                slot.MarkDirty();

                if (renderer != null)
                {
                    renderer.SetContents(contents, burning, true);
                }
                MarkDirty();
                Api.World.PlaySoundAt(new AssetLocation("sounds/block/ingot"), Pos.X, Pos.Y, Pos.Z, byPlayer, false);

                return true;
            }

            // Merge heatable item
            if (!forgableGeneric && contents != null && contents.Equals(Api.World, slot.Itemstack, GlobalConstants.IgnoredStackAttributes) && contents.StackSize < 4 && contents.StackSize < contents.Collectible.MaxStackSize)
            {
                float myTemp = contents.Collectible.GetTemperature(Api.World, contents);
                float histemp = slot.Itemstack.Collectible.GetTemperature(Api.World, slot.Itemstack);

                contents.Collectible.SetTemperature(world, contents, (myTemp * contents.StackSize + histemp * 1) / (contents.StackSize + 1));
                contents.StackSize++;

                slot.TakeOut(1);
                slot.MarkDirty();

                if (renderer != null)
                {
                    renderer.SetContents(contents, burning, true);
                }
                Api.World.PlaySoundAt(new AssetLocation("sounds/block/ingot"), Pos.X, Pos.Y, Pos.Z, byPlayer, false);

                MarkDirty();
                return true;
            }

            return false;
        }
    }


    public override void OnBlockRemoved()
    {
        base.OnBlockRemoved();
        if (renderer != null)
        {
            renderer.Dispose();
            renderer = null;
        }

        if (ambientSound != null) ambientSound.Dispose();
    }

    public override void OnBlockBroken()
    {
        base.OnBlockBroken();

        if (contents != null)
        {
            Api.World.SpawnItemEntity(contents, Pos.ToVec3d().Add(0.5, 0.5, 0.5));
        }

        if (ambientSound != null) ambientSound.Dispose();
    }

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
    {
        base.FromTreeAttributes(tree, worldForResolving);

        contents = tree.GetItemstack("contents");
        burning = tree.GetInt("burning") > 0;
        lastTickTotalHours = tree.GetDouble("lastTickTotalHours");

        if (Api != null)
        {
            if (contents != null)
            {
                contents.ResolveBlockOrItem(Api.World);
            }
        }
        if (renderer != null)
        {
            renderer.SetContents(contents, burning, true);
        }

        if (energyStorage == null)
        {
            energyStorage = new FluxStorage(MyMiniLib.GetAttributeInt(Block, "storage", 10000), MyMiniLib.GetAttributeInt(Block, "input", 1000), 0);
        }
        energyStorage.setEnergy(tree.GetFloat("energy"));
    }

    public override void ToTreeAttributes(ITreeAttribute tree)
    {
        base.ToTreeAttributes(tree);

        tree.SetItemstack("contents", contents);
        tree.SetInt("burning", burning ? 1 : 0);
        tree.SetDouble("lastTickTotalHours", lastTickTotalHours);

        if (energyStorage != null)
        {
            tree.SetFloat("energy", energyStorage.getEnergyStored());
        }
    }

    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
    {
        if (contents != null)
        {
            int temp = (int)contents.Collectible.GetTemperature(Api.World, contents);
            if (temp <= 25)
            {
                dsc.AppendLine(string.Format("Contents: {0}x {1}\nTemperature: {2}", contents.StackSize, contents.GetName(), Lang.Get("Cold")));
            }
            else
            {
                dsc.AppendLine(string.Format("Contents: {0}x {1}\nTemperature: {2}°C", contents.StackSize, contents.GetName(), temp));
            }

        }
    }


    public override void OnLoadCollectibleMappings(IWorldAccessor worldForResolve, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, int schematicSeed)
    {
        if (contents != null && contents.FixMapping(oldBlockIdMapping, oldItemIdMapping, worldForResolve) == false)
        {
            contents = null;
        }
    }

    public override void OnStoreCollectibleMappings(Dictionary<int, AssetLocation> blockIdMapping, Dictionary<int, AssetLocation> itemIdMapping)
    {
        if (contents != null)
        {
            if (contents.Class == EnumItemClass.Item)
            {
                blockIdMapping[contents.Id] = contents.Item.Code;
            }
            else
            {
                itemIdMapping[contents.Id] = contents.Block.Code;
            }
        }

    }

    public override void OnBlockUnloaded()
    {
        base.OnBlockUnloaded();

        if (renderer != null)
        {
            renderer.Dispose();
        }
    }

    public float GetHeatStrength(IWorldAccessor world, BlockPos heatSourcePos, BlockPos heatReceiverPos)
    {
        return IsBurning ? 5 : 0;
    }
}

public class TFForgeContentsRenderer : IRenderer, ITexPositionSource
{
    private ICoreClientAPI capi;
    private BlockPos pos;

    MeshRef workItemMeshRef;

    MeshRef heQuadRef;


    ItemStack stack;
    bool burning;

    TextureAtlasPosition hetexpos;

    int textureId;


    string tmpMetal;
    ITexPositionSource tmpTextureSource;

    Matrixf ModelMat = new Matrixf();



    public double RenderOrder
    {
        get { return 0.5; }
    }

    public int RenderRange
    {
        get { return 24; }
    }

    public Size2i AtlasSize
    {
        get { return capi.BlockTextureAtlas.Size; }
    }

    public TextureAtlasPosition this[string textureCode]
    {
        get { return tmpTextureSource[tmpMetal]; }
    }




    public TFForgeContentsRenderer(BlockPos pos, ICoreClientAPI capi)
    {
        this.pos = pos;
        this.capi = capi;

        Block block = capi.World.GetBlock(new AssetLocation("temporalengineering:tfforge"));

        hetexpos = capi.BlockTextureAtlas.GetPosition(block, "he");

        MeshData heMesh;
        Shape ovshape = capi.Assets.TryGet(new AssetLocation("temporalengineering:shapes/block/tfforge/heating_element.json")).ToObject<Shape>();
        capi.Tesselator.TesselateShape(block, ovshape, out heMesh);

        for (int i = 0; i < heMesh.Uv.Length; i += 2)
        {
            heMesh.Uv[i + 0] = hetexpos.x1 + heMesh.Uv[i + 0] * 32f / AtlasSize.Width;
            heMesh.Uv[i + 1] = hetexpos.y1 + heMesh.Uv[i + 1] * 32f / AtlasSize.Height;
        }

        heQuadRef = capi.Render.UploadMesh(heMesh);
    }

    public void SetContents(ItemStack stack, bool burning, bool regen)
    {
        this.stack = stack;
        this.burning = burning;

        if (regen) RegenMesh();
    }


    void RegenMesh()
    {
        if (workItemMeshRef != null) workItemMeshRef.Dispose();
        workItemMeshRef = null;
        if (stack == null) return;

        Shape shape;

        tmpMetal = stack.Collectible.LastCodePart();
        MeshData mesh = null;

        string firstCodePart = stack.Collectible.FirstCodePart();
        if (firstCodePart == "metalplate")
        {
            tmpTextureSource = capi.Tesselator.GetTexSource(capi.World.GetBlock(new AssetLocation("platepile")));
            shape = capi.Assets.TryGet("shapes/block/stone/forge/platepile.json").ToObject<Shape>();
            textureId = tmpTextureSource[tmpMetal].atlasTextureId;
            capi.Tesselator.TesselateShape("block-fcr", shape, out mesh, this, null, 0, 0, 0, stack.StackSize);

        }
        else if (firstCodePart == "workitem")
        {
            MeshData workItemMesh = ItemWorkItem.GenMesh(capi, stack, ItemWorkItem.GetVoxels(stack), out textureId);
            workItemMesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.75f, 0.75f, 0.75f);
            workItemMesh.Translate(0, -9f / 16f, 0);
            workItemMeshRef = capi.Render.UploadMesh(workItemMesh);
        }
        else if (firstCodePart == "ingot")
        {
            tmpTextureSource = capi.Tesselator.GetTexSource(capi.World.GetBlock(new AssetLocation("ingotpile")));
            shape = capi.Assets.TryGet("shapes/block/stone/forge/ingotpile.json").ToObject<Shape>();
            textureId = tmpTextureSource[tmpMetal].atlasTextureId;
            capi.Tesselator.TesselateShape("block-fcr", shape, out mesh, this, null, 0, 0, 0, stack.StackSize);
        }
        else if (stack.Collectible.Attributes != null && stack.Collectible.Attributes.IsTrue("forgable") == true)
        {
            if (stack.Class == EnumItemClass.Block)
            {
                mesh = capi.TesselatorManager.GetDefaultBlockMesh(stack.Block).Clone();
                textureId = capi.BlockTextureAtlas.AtlasTextureIds[0];
            }
            else
            {
                capi.Tesselator.TesselateItem(stack.Item, out mesh);
                textureId = capi.ItemTextureAtlas.AtlasTextureIds[0];
            }

            ModelTransform tf = stack.Collectible.Attributes["inForgeTransform"].AsObject<ModelTransform>();
            if (tf != null)
            {
                tf.EnsureDefaultValues();
                mesh.ModelTransform(tf);
            }
        }

        if (mesh != null)
        {
            workItemMeshRef = capi.Render.UploadMesh(mesh);
        }
    }



    public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
    {
        IRenderAPI rpi = capi.Render;
        IClientWorldAccessor worldAccess = capi.World;
        Vec3d camPos = worldAccess.Player.Entity.CameraPos;

        rpi.GlDisableCullFace();
        IStandardShaderProgram prog = rpi.StandardShader;
        prog.Use();
        prog.RgbaAmbientIn = rpi.AmbientColor;
        prog.RgbaFogIn = rpi.FogColor;
        prog.FogMinIn = rpi.FogMin;
        prog.FogDensityIn = rpi.FogDensity;
        prog.RgbaTint = ColorUtil.WhiteArgbVec;
        prog.DontWarpVertices = 0;
        prog.AddRenderFlags = 0;
        prog.ExtraGodray = 0;
        prog.OverlayOpacity = 0;

        Vec4f lightrgbs = capi.World.BlockAccessor.GetLightRGBs(pos.X, pos.Y, pos.Z);

        if (stack != null && workItemMeshRef != null)
        {
            int temp = (int)stack.Collectible.GetTemperature(capi.World, stack);

            //Vec4f lightrgbs = capi.World.BlockAccessor.GetLightRGBs(pos.X, pos.Y, pos.Z);
            float[] glowColor = ColorUtil.GetIncandescenceColorAsColor4f(temp);
            int extraGlow = GameMath.Clamp((temp - 550) / 2, 0, 255);

            prog.NormalShaded = 1;
            prog.RgbaLightIn = lightrgbs;
            prog.RgbaGlowIn = new Vec4f(glowColor[0], glowColor[1], glowColor[2], extraGlow / 255f);

            prog.ExtraGlow = extraGlow;
            prog.Tex2D = textureId;
            prog.ModelMatrix = ModelMat.Identity().Translate(pos.X - camPos.X, pos.Y - camPos.Y + 10 / 16f, pos.Z - camPos.Z).Values;
            prog.ViewMatrix = rpi.CameraMatrixOriginf;
            prog.ProjectionMatrix = rpi.CurrentProjectionMatrix;

            rpi.RenderMesh(workItemMeshRef);
        }

        if (burning)
        {
            float[] glowColor = ColorUtil.GetIncandescenceColorAsColor4f(1200);
            prog.RgbaGlowIn = new Vec4f(glowColor[0], glowColor[1], glowColor[2], 1);
        }
        else
        {
            prog.RgbaGlowIn = new Vec4f(0, 0, 0, 0);
        }

        prog.NormalShaded = 1;
        prog.RgbaLightIn = lightrgbs;

        prog.ExtraGlow = burning ? 255 : 0;


        rpi.BindTexture2d(hetexpos.atlasTextureId);

        prog.ModelMatrix = ModelMat.Identity().Translate(pos.X - camPos.X, pos.Y - camPos.Y, pos.Z - camPos.Z).Values;
        prog.ViewMatrix = rpi.CameraMatrixOriginf;
        prog.ProjectionMatrix = rpi.CurrentProjectionMatrix;

        rpi.RenderMesh(heQuadRef);

        prog.Stop();
    }



    public void Dispose()
    {
        capi.Event.UnregisterRenderer(this, EnumRenderStage.Opaque);
        if (heQuadRef != null) heQuadRef.Dispose();
        if (workItemMeshRef != null) workItemMeshRef.Dispose();
    }
}