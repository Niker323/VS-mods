﻿using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

public class BlockEntityTFCharger : BlockEntity, ITexPositionSource, IFluxStorage
{
    public InventoryGeneric inventory;
    public FluxStorage energyStorage;

    MeshData[] toolMeshes = new MeshData[1];


    public Size2i AtlasSize
    {
        get { return ((ICoreClientAPI)Api).BlockTextureAtlas.Size; }
    }

    CollectibleObject tmpItem;
    public TextureAtlasPosition this[string textureCode]
    {
        get
        {
            ToolTextures tt = null;

            if (BlockTFCharger.ToolTextureSubIds(Api).TryGetValue((Item)tmpItem, out tt))
            {
                int textureSubId = 0;
                if (tt.TextureSubIdsByCode.TryGetValue(textureCode, out textureSubId))
                {
                    return ((ICoreClientAPI)Api).BlockTextureAtlas.Positions[textureSubId];
                }

                return ((ICoreClientAPI)Api).BlockTextureAtlas.Positions[tt.TextureSubIdsByCode.First().Value];
            }

            return null;
        }
    }

    public BlockEntityTFCharger() : base()
    {
        inventory = new InventoryGeneric(1, "charger", null, null, null);
    }

    public override void Initialize(ICoreAPI api)
    {
        base.Initialize(api);

        if (energyStorage == null)
        {
            energyStorage = new FluxStorage(10000, 100, 100);
        }

        inventory.LateInitialize("charger-" + Pos.ToString(), api);
        inventory.ResolveBlocksOrItems();

        if (api is ICoreClientAPI)
        {
            loadToolMeshes();
        }

        RegisterGameTickListener(OnTick, 50);
    }

    private void OnTick(float dt)
    {
        if (inventory[0]?.Itemstack?.Item is IFluxStorageItem)
        {
            energyStorage.modifyEnergyStored(-((IFluxStorageItem)inventory[0].Itemstack.Item).receiveEnergy(inventory[0].Itemstack, energyStorage.getLimitExtract()));
        }
        MarkDirty();
    }

    void loadToolMeshes()
    {
        BlockFacing facing = getFacing().GetCCW();
        if (facing == null) return;

        Vec3f facingNormal = facing.Normalf;

        Vec3f origin = new Vec3f(0.5f, 0.5f, 0.5f);

        ICoreClientAPI clientApi = (ICoreClientAPI)Api;

        //for (int i = 0; i < 1; i++)
        //{
        toolMeshes[0] = null;
        IItemStack stack = inventory[0].Itemstack;
        if (stack == null) return;

        tmpItem = stack.Collectible;

        if (stack.Class == EnumItemClass.Item)
        {
            clientApi.Tesselator.TesselateItem(stack.Item, out toolMeshes[0], this);
        }
        else
        {
            clientApi.Tesselator.TesselateBlock(stack.Block, out toolMeshes[0]);
        }


        if (tmpItem.Attributes?["toolrackTransform"].Exists == true)
        {
            ModelTransform transform = tmpItem.Attributes["toolrackTransform"].AsObject<ModelTransform>();
            transform.EnsureDefaultValues();

            toolMeshes[0].ModelTransform(transform);
        }


        //float zOff = i > 1 ? (-1.8f / 16f) : 0;

        if (stack.Class == EnumItemClass.Item && stack.Item.Shape?.VoxelizeTexture == true)
        {
            toolMeshes[0].Scale(origin, 0.33f, 0.33f, 0.33f);
            //toolMeshes[0].Translate(
            //    (((i % 2) == 0) ? 0.23f : -0.3f),
            //    ((i > 1) ? 0.2f : -0.3f) + zOff,
            //    0.433f * ((facing.Axis == EnumAxis.X) ? -1 : 1)
            //);
            toolMeshes[0].Rotate(origin, 0, facing.HorizontalAngleIndex * 90 * GameMath.DEG2RAD, 0);
            toolMeshes[0].Rotate(origin, 180 * GameMath.DEG2RAD, 0, 0);
        }
        else
        {

            toolMeshes[0].Scale(origin, 0.3f, 0.3f, 0.3f);
            //float x = ((i > 1) ? -0.2f : 0.3f);
            //float z = ((i % 2 == 0) ? 0.23f : -0.2f) * (facing.Axis == EnumAxis.X ? 1f : -1f);

            //toolMeshes[0].Translate(x, 0.433f + zOff, z);
            toolMeshes[0].Rotate(origin, 0, facing.HorizontalAngleIndex * 90 * GameMath.DEG2RAD, GameMath.PIHALF);
            toolMeshes[0].Rotate(origin, 0, GameMath.PIHALF, 0);
        }
        //}
    }


    internal bool OnPlayerInteract(IPlayer byPlayer, Vec3d hit)
    {
        if (inventory[0].Itemstack != null)
        {
            return TakeFromSlot(byPlayer, 0);
        }
        else
        {
            return PutInSlot(byPlayer, 0);
        }
    }

    bool PutInSlot(IPlayer player, int slot)
    {
        IItemStack stack = player.InventoryManager.ActiveHotbarSlot.Itemstack;
        if (stack == null) return false;

        player.InventoryManager.ActiveHotbarSlot.TryPutInto(Api.World, inventory[slot]);

        didInteract(player);
        return true;
    }


    bool TakeFromSlot(IPlayer player, int slot)
    {
        ItemStack stack = inventory[slot].TakeOutWhole();

        if (!player.InventoryManager.TryGiveItemstack(stack))
        {
            Api.World.SpawnItemEntity(stack, Pos.ToVec3d().Add(0.5, 0.5, 0.5));
        }

        didInteract(player);
        return true;
    }


    void didInteract(IPlayer player)
    {
        Api.World.PlaySoundAt(new AssetLocation("sounds/player/buildhigh"), Pos.X, Pos.Y, Pos.Z, player, false);
        if (Api is ICoreClientAPI) loadToolMeshes();
        MarkDirty(true);
    }



    public override void OnBlockRemoved()
    {

    }

    public override void OnBlockBroken()
    {
        ItemStack stack = inventory[0].Itemstack;
        if (stack != null) Api.World.SpawnItemEntity(stack, Pos.ToVec3d().Add(0.5, 0.5, 0.5));
    }

    BlockFacing getFacing()
    {
        Block block = Api.World.BlockAccessor.GetBlock(Pos);
        BlockFacing facing = BlockFacing.FromCode(block.LastCodePart());
        return facing == null ? BlockFacing.NORTH : facing;
    }

    public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
    {
        ICoreClientAPI clientApi = (ICoreClientAPI)Api;
        Block block = Api.World.BlockAccessor.GetBlock(Pos);
        MeshData mesh = clientApi.TesselatorManager.GetDefaultBlockMesh(block);
        if (mesh == null) return true;

        mesher.AddMeshData(mesh);

        if (toolMeshes[0] != null) mesher.AddMeshData(toolMeshes[0]);


        return true;
    }

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
    {
        base.FromTreeAttributes(tree, worldForResolving);
        inventory.FromTreeAttributes(tree.GetTreeAttribute("inventory"));
        if (Api != null)
        {
            inventory.Api = Api;
            inventory.ResolveBlocksOrItems();
        }

        if (Api is ICoreClientAPI)
        {
            loadToolMeshes();
            Api.World.BlockAccessor.MarkBlockDirty(Pos);
        }

        if (energyStorage == null)
        {
            energyStorage = new FluxStorage(10000, 100, 100);
        }
        energyStorage.setEnergy(tree.GetInt("energy", 0));
    }

    public override void ToTreeAttributes(ITreeAttribute tree)
    {
        base.ToTreeAttributes(tree);
        ITreeAttribute invtree = new TreeAttribute();
        inventory.ToTreeAttributes(invtree);
        tree["inventory"] = invtree;

        if (energyStorage != null)
        {
            tree.SetInt("energy", energyStorage.getEnergyStored());
        }
    }

    public override void OnStoreCollectibleMappings(Dictionary<int, AssetLocation> blockIdMapping, Dictionary<int, AssetLocation> itemIdMapping)
    {
        foreach (var slot in inventory)
        {
            if (slot.Itemstack == null) continue;

            if (slot.Itemstack.Class == EnumItemClass.Item)
            {
                itemIdMapping[slot.Itemstack.Item.Id] = slot.Itemstack.Item.Code;
            }
            else
            {
                blockIdMapping[slot.Itemstack.Block.BlockId] = slot.Itemstack.Block.Code;
            }
        }
    }

    public override void OnLoadCollectibleMappings(IWorldAccessor worldForResolve, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, int schematicSeed)
    {
        foreach (var slot in inventory)
        {
            if (slot.Itemstack == null) continue;
            if (!slot.Itemstack.FixMapping(oldBlockIdMapping, oldItemIdMapping, worldForResolve))
            {
                slot.Itemstack = null;
            }
        }
    }

    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder sb)
    {
        if (energyStorage != null)
        {
            sb.AppendLine(energyStorage.GetFluxStorageInfo());
        }
        int i = 0;
        foreach (var slot in inventory)
        {
            if (sb.Length > 0 && i == 2)
            {
                sb.Append("\n");
            }
            i++;
            if (slot.Itemstack == null) continue;

            if (sb.Length > 0 && sb[sb.Length - 1] != '\n')
            {
                sb.Append(", ");
            }


            sb.Append(slot.Itemstack.GetName());
        }

        sb.AppendLineOnce();
        sb.ToString();
    }

    public FluxStorage GetFluxStorage()
    {
        return energyStorage;
    }

    public int receiveEnergy(BlockFacing from, int maxReceive, bool simulate)
    {
        return energyStorage.receiveEnergy(maxReceive, simulate);
    }

}

public class ToolTextures
{
    public Dictionary<string, int> TextureSubIdsByCode = new Dictionary<string, int>();
}

public class BlockTFCharger : Block
{
    public static Dictionary<Item, ToolTextures> ToolTextureSubIds(ICoreAPI api)
    {
        Dictionary<Item, ToolTextures> toolTextureSubIds;
        object obj;

        if (api.ObjectCache.TryGetValue("toolTextureSubIdsTest", out obj))
        {

            toolTextureSubIds = obj as Dictionary<Item, ToolTextures>;
        }
        else
        {
            api.ObjectCache["toolTextureSubIdsTest"] = toolTextureSubIds = new Dictionary<Item, ToolTextures>();
        }

        return toolTextureSubIds;
    }


    WorldInteraction[] interactions;

    public override void OnLoaded(ICoreAPI api)
    {
        if (api.Side != EnumAppSide.Client) return;
        ICoreClientAPI capi = api as ICoreClientAPI;


        interactions = ObjectCacheUtil.GetOrCreate(api, "toolrackBlockInteractions", () =>
        {
            List<ItemStack> rackableStacklist = new List<ItemStack>();

            foreach (CollectibleObject obj in api.World.Collectibles)
            {
                //if (obj.Tool == null && obj.Attributes?["rackable"].AsBool() != true) continue;

                List<ItemStack> stacks = obj.GetHandBookStacks(capi);
                if (stacks != null) rackableStacklist.AddRange(stacks);
            }

            return new WorldInteraction[] {
                    new WorldInteraction()
                    {
                        ActionLangCode = "blockhelp-toolrack-place",
                        HotKeyCode = null,
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = rackableStacklist.ToArray()
                    },
                    new WorldInteraction()
                    {
                        ActionLangCode = "blockhelp-toolrack-take",
                        HotKeyCode = null,
                        MouseButton = EnumMouseButton.Right,
                    }
                };
        });
    }



    public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
    {
        BlockEntity be = world.BlockAccessor.GetBlockEntity(blockSel.Position);
        if (be is BlockEntityTFCharger)
        {
            BlockEntityTFCharger rack = (BlockEntityTFCharger)be;
            return rack.OnPlayerInteract(byPlayer, blockSel.HitPosition);
        }

        return false;
    }

    // We need the tool item textures also in the block atlas
    public override void OnCollectTextures(ICoreAPI api, ITextureLocationDictionary textureDict)
    {
        base.OnCollectTextures(api, textureDict);

        for (int i = 0; i < api.World.Items.Count; i++)
        {
            Item item = api.World.Items[i];
            //if (item.Tool == null && item.Attributes?["rackable"].AsBool() != true) continue;

            ToolTextures tt = new ToolTextures();


            if (item.Shape != null)
            {
                IAsset asset = api.Assets.TryGet(item.Shape.Base.Clone().WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json"));
                if (asset != null)
                {
                    Shape shape = asset.ToObject<Shape>();
                    foreach (var val in shape.Textures)
                    {
                        CompositeTexture ctex = new CompositeTexture(val.Value.Clone());
                        ctex.Bake(api.Assets);

                        textureDict.AddTextureLocation(new AssetLocationAndSource(ctex.Baked.BakedName, "Shape code " + item.Shape.Base));
                        tt.TextureSubIdsByCode[val.Key] = textureDict[new AssetLocationAndSource(ctex.Baked.BakedName)];
                    }
                }
            }

            foreach (var val in item.Textures)
            {
                val.Value.Bake(api.Assets);
                textureDict.AddTextureLocation(new AssetLocationAndSource(val.Value.Baked.BakedName, "Item code " + item.Code));
                tt.TextureSubIdsByCode[val.Key] = textureDict[new AssetLocationAndSource(val.Value.Baked.BakedName)];
            }



            ToolTextureSubIds(api)[item] = tt;
        }
    }

    public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
    {
        return interactions.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
    }
}