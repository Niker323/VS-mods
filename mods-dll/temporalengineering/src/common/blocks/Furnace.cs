using Cairo;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

public class BlockFurnace : Block
{
    //public bool IsExtinct;

    //Vec3f[] basePos;

    public override void OnLoaded(ICoreAPI api)
    {
        base.OnLoaded(api);
    }


    public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
    {
        ItemStack stack = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack;

        BlockEntityFirepit bef = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityFirepit;

        if (bef != null && stack != null && stack.Block != null && stack.Block.HasBehavior<BlockBehaviorCanIgnite>())
        {
            return false;
        }

        if (bef != null && stack != null && byPlayer.Entity.Controls.Sneak)
        {
            if (stack.Collectible.CombustibleProps != null && stack.Collectible.CombustibleProps.MeltingPoint > 0)
            {
                ItemStackMoveOperation op = new ItemStackMoveOperation(world, EnumMouseButton.Button1, 0, EnumMergePriority.DirectMerge, 1);
                byPlayer.InventoryManager.ActiveHotbarSlot.TryPutInto(bef.inputSlot, ref op);
                if (op.MovedQuantity > 0)
                {
                    (byPlayer as IClientPlayer).TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
                    return true;
                }
            }

            //if (stack.Collectible.CombustibleProps != null && stack.Collectible.CombustibleProps.BurnTemperature > 0)
            //{
            //    ItemStackMoveOperation op = new ItemStackMoveOperation(world, EnumMouseButton.Button1, 0, EnumMergePriority.DirectMerge, 1);
            //    byPlayer.InventoryManager.ActiveHotbarSlot.TryPutInto(bef.fuelSlot, ref op);
            //    if (op.MovedQuantity > 0)
            //    {
            //        (byPlayer as IClientPlayer).TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
            //        return true;
            //    }
            //}
        }

        //if (stack.Collectible.Attributes.IsTrue("mealContainer") == true)
        //{
        //    ItemSlot potSlot = null;
        //    if (bef.inputStack.Collectible is BlockCookedContainer)
        //    {
        //        potSlot = bef.inputSlot;
        //    }
        //    if (bef.outputStack.Collectible is BlockCookedContainer)
        //    {
        //        potSlot = bef.outputSlot;
        //    }

        //    if (potSlot != null)
        //    {
        //        BlockCookedContainer blockPot = potSlot.Itemstack.Collectible as BlockCookedContainer;
        //        ItemSlot targetSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
        //        if (byPlayer.InventoryManager.ActiveHotbarSlot.StackSize > 1)
        //        {
        //            targetSlot = new DummySlot(targetSlot.TakeOut(1));
        //            byPlayer.InventoryManager.ActiveHotbarSlot.MarkDirty();
        //            blockPot.ServeIntoBowlStack(targetSlot, potSlot, world);
        //            if (!byPlayer.InventoryManager.TryGiveItemstack(targetSlot.Itemstack, true))
        //            {
        //                world.SpawnItemEntity(targetSlot.Itemstack, byPlayer.Entity.ServerPos.XYZ);
        //            }
        //        }
        //        else
        //        {
        //            blockPot.ServeIntoBowlStack(targetSlot, potSlot, world);
        //        }

        //    }

        //    return true;
        //}



        return base.OnBlockInteractStart(world, byPlayer, blockSel);
    }

    public override bool CanAttachBlockAt(IBlockAccessor blockAccessor, Block block, BlockPos pos, BlockFacing blockFace, Cuboidi attachmentArea = null)
    {
        return true;
    }
}

public class BlockEntityFurnace : BlockEntityOpenableContainer, IFluxStorage//, IHeatSource
{
    internal InventorySmelting inventory;


    // Temperature before the half second tick
    public float prevFurnaceTemperature = 20;

    // Current temperature of the furnace
    public float furnaceTemperature = 20;
    // Current temperature of the ore (Degree Celsius * deg
    //public float oreTemperature = 20;
    // Maximum temperature that can be reached with the currently used fuel
    public int maxTemperature = 1500;
    // For how long the ore has been cooking
    public float inputStackCookingTime;


    GuiDialogBlockEntityFurnace clientDialog;
    bool clientSidePrevBurning;

    //FirepitContentsRenderer renderer;


    public FluxStorage energyStorage;


    #region Config

    public virtual float HeatModifier
    {
        get { return 1f; }
    }

    // Resting temperature
    public virtual int enviromentTemperature()
    {
        return 20;
    }

    // seconds it requires to melt the ore once beyond melting point
    public virtual float maxCookingTime()
    {
        return inputSlot.Itemstack == null ? 30f : inputSlot.Itemstack.Collectible.GetMeltingDuration(Api.World, inventory, inputSlot);
    }

    public override string InventoryClassName
    {
        get { return "stove"; }
    }

    public virtual string DialogTitle
    {
        get { return Lang.Get("Furnace"); }
    }

    public override InventoryBase Inventory
    {
        get { return inventory; }
    }

    #endregion



    public BlockEntityFurnace()
    {
        inventory = new InventorySmelting(null, null);
        inventory.SlotModified += OnSlotModifid;
    }



    public override void Initialize(ICoreAPI api)
    {
        base.Initialize(api);

        if (energyStorage == null)
        {
            energyStorage = new FluxStorage(10000, 30, 0);
        }

        inventory.pos = Pos;
        inventory.LateInitialize("smelting-" + Pos.X + "/" + Pos.Y + "/" + Pos.Z, api);

        RegisterGameTickListener(OnBurnTick, 50);
        RegisterGameTickListener(On500msTick, 500);

        //if (api is ICoreClientAPI)
        //{
        //    renderer = new FirepitContentsRenderer(api as ICoreClientAPI, Pos);

        //    (api as ICoreClientAPI).Event.RegisterRenderer(renderer, EnumRenderStage.Opaque, "firepit");

        //    UpdateRenderer();
        //}
    }

    public int receiveEnergy(BlockFacing from, int maxReceive, bool simulate)
    {
        return energyStorage.receiveEnergy(Math.Min(30, maxReceive), simulate);
    }

    public FluxStorage GetFluxStorage()
    {
        return energyStorage;
    }

    private void OnSlotModifid(int slotid)
    {
        Block = Api.World.BlockAccessor.GetBlock(Pos);

        //UpdateRenderer();
        MarkDirty(Api.Side == EnumAppSide.Server); // Save useless triple-remesh by only letting the server decide when to redraw

        if (Api is ICoreClientAPI && clientDialog != null)
        {
            SetDialogValues(clientDialog.Attributes);
        }

        Api.World.BlockAccessor.GetChunkAtBlockPos(Pos).MarkModified();
    }



    public bool IsBurning = false;


    public int getInventoryStackLimit()
    {
        return 64;
    }


    private void OnBurnTick(float dt)
    {
        // Only tick on the server and merely sync to client
        if (Api is ICoreClientAPI)
        {
            //renderer.contentStackRenderer.OnUpdate(InputStackTemp);
            return;
        }

        // Furnace is burning: Heat furnace
        if (IsBurning)
        {
            furnaceTemperature = changeTemperature(furnaceTemperature, maxTemperature, dt);
        }

        IsBurning = false;

        // Ore follows furnace temperature
        if (canHeatInput())
        {
            heatInput(dt);
        }
        else
        {
            inputStackCookingTime = 0;
        }

        if (canHeatOutput())
        {
            heatOutput(dt);
        }


        // Finished smelting? Turn to smelted item
        if (canSmeltInput() && inputStackCookingTime > maxCookingTime())
        {
            smeltItems();
        }

        // Furnace is not burning and can burn: Ignite the fuel
        if (energyStorage.getEnergyStored() >= 15)
        {
            igniteFuel();
        }


        // Furnace is not burning: Cool down furnace and ore also turn of fire
        if (!IsBurning)
        {
            furnaceTemperature = changeTemperature(furnaceTemperature, enviromentTemperature(), dt);
        }

    }


    // Sync to client every 500ms
    private void On500msTick(float dt)
    {
        if (Api is ICoreServerAPI && (IsBurning || prevFurnaceTemperature != furnaceTemperature))
        {
            MarkDirty();
        }

        prevFurnaceTemperature = furnaceTemperature;
    }


    public float changeTemperature(float fromTemp, float toTemp, float dt)
    {
        float diff = Math.Abs(fromTemp - toTemp);

        dt = dt + dt * (diff / 28);


        if (diff < dt)
        {
            return toTemp;
        }

        if (fromTemp > toTemp)
        {
            dt = -dt;
        }

        if (Math.Abs(fromTemp - toTemp) < 1)
        {
            return toTemp;
        }

        return fromTemp + dt;
    }

    public void heatInput(float dt)
    {
        float oldTemp = InputStackTemp;
        float nowTemp = oldTemp;
        float meltingPoint = inputSlot.Itemstack.Collectible.GetMeltingPoint(Api.World, inventory, inputSlot);

        // Only Heat ore. Cooling happens already in the itemstack
        if (oldTemp < furnaceTemperature)
        {
            float f = (1 + GameMath.Clamp((furnaceTemperature - oldTemp) / 30, 0, 1.6f)) * dt;
            if (nowTemp >= meltingPoint) f /= 11;

            float newTemp = changeTemperature(oldTemp, furnaceTemperature, f);
            int maxTemp = 0;
            if (inputStack.ItemAttributes != null)
            {
                maxTemp = Math.Max(inputStack.Collectible.CombustibleProps == null ? 0 : inputStack.Collectible.CombustibleProps.MaxTemperature, inputStack.ItemAttributes["maxTemperature"] == null ? 0 : inputStack.ItemAttributes["maxTemperature"].AsInt(0));
            }
            else
            {
                maxTemp = inputStack.Collectible.CombustibleProps == null ? 0 : inputStack.Collectible.CombustibleProps.MaxTemperature;
            }
            if (maxTemp > 0)
            {
                newTemp = Math.Min(maxTemp, newTemp);
            }

            if (oldTemp != newTemp)
            {
                InputStackTemp = newTemp;
                nowTemp = newTemp;
            }
        }

        // Begin smelting when hot enough
        if (nowTemp >= meltingPoint)
        {
            float diff = nowTemp / meltingPoint;
            inputStackCookingTime += GameMath.Clamp((int)(diff), 1, 30) * dt;
        }
        else
        {
            if (inputStackCookingTime > 0) inputStackCookingTime--;
        }
    }

    public void heatOutput(float dt)
    {
        //dt *= 20;

        float oldTemp = OutputStackTemp;

        // Only Heat ore. Cooling happens already in the itemstack
        if (oldTemp < furnaceTemperature)
        {
            float newTemp = changeTemperature(oldTemp, furnaceTemperature, 2 * dt);
            int maxTemp = Math.Max(outputStack.Collectible.CombustibleProps == null ? 0 : outputStack.Collectible.CombustibleProps.MaxTemperature, outputStack.ItemAttributes["maxTemperature"] == null ? 0 : outputStack.ItemAttributes["maxTemperature"].AsInt(0));
            if (maxTemp > 0)
            {
                newTemp = Math.Min(maxTemp, newTemp);
            }

            if (oldTemp != newTemp)
            {
                OutputStackTemp = newTemp;
            }
        }
    }






    public float InputStackTemp
    {
        get
        {
            return GetTemp(inputStack);
        }
        set
        {
            SetTemp(inputStack, value);
        }
    }

    public float OutputStackTemp
    {
        get
        {
            return GetTemp(outputStack);
        }
        set
        {
            SetTemp(outputStack, value);
        }
    }


    float GetTemp(ItemStack stack)
    {
        if (stack == null) return enviromentTemperature();

        if (inventory.CookingSlots.Length > 0)
        {
            bool haveStack = false;
            float lowestTemp = 0;
            for (int i = 0; i < inventory.CookingSlots.Length; i++)
            {
                ItemStack cookingStack = inventory.CookingSlots[i].Itemstack;
                if (cookingStack != null)
                {
                    float stackTemp = cookingStack.Collectible.GetTemperature(Api.World, cookingStack);
                    lowestTemp = haveStack ? Math.Min(lowestTemp, stackTemp) : stackTemp;
                    haveStack = true;
                }

            }

            return lowestTemp;

        }
        else
        {
            return stack.Collectible.GetTemperature(Api.World, stack);
        }
    }

    void SetTemp(ItemStack stack, float value)
    {
        if (stack == null) return;
        if (inventory.CookingSlots.Length > 0)
        {
            for (int i = 0; i < inventory.CookingSlots.Length; i++)
            {
                if (inventory.CookingSlots[i].Itemstack != null) inventory.CookingSlots[i].Itemstack.Collectible.SetTemperature(Api.World, inventory.CookingSlots[i].Itemstack, value);
            }
        }
        else
        {
            stack.Collectible.SetTemperature(Api.World, stack, value);
        }
    }

    public void igniteFuel()
    {
        IsBurning = true;

        energyStorage.modifyEnergyStored(-15);
    }


    //public void setBlockState(string state)
    //{
    //    AssetLocation loc = Block.CodeWithVariant("burnstate", state);
    //    Block block = Api.World.GetBlock(loc);
    //    if (block == null) return;

    //    Api.World.BlockAccessor.ExchangeBlock(block.Id, Pos);
    //    this.Block = block;
    //}



    public bool canHeatInput()
    {
        return
            canSmeltInput() || inputStack != null && inputStack.ItemAttributes != null && (inputStack.ItemAttributes["allowHeating"] != null && inputStack.ItemAttributes["allowHeating"].AsBool())
        ;
    }

    public bool canHeatOutput()
    {
        return
            outputStack != null && outputStack.ItemAttributes["allowHeating"] != null && outputStack.ItemAttributes["allowHeating"].AsBool();
        ;
    }

    public bool canSmeltInput()
    {
        return
            inputStack != null
            && inputStack.Collectible.CanSmelt(Api.World, inventory, inputSlot.Itemstack, outputSlot.Itemstack)
            && (inputStack.Collectible.CombustibleProps == null || !inputStack.Collectible.CombustibleProps.RequiresContainer)
        ;
    }


    public void smeltItems()
    {
        inputStack.Collectible.DoSmelt(Api.World, inventory, inputSlot, outputSlot);
        InputStackTemp = enviromentTemperature();
        inputStackCookingTime = 0;
        MarkDirty(true);
        inputSlot.MarkDirty();
    }


    #region Events

    public override bool OnPlayerRightClick(IPlayer byPlayer, BlockSelection blockSel)
    {
        if (Api.World is IServerWorldAccessor)
        {
            byte[] data;

            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(ms);
                writer.Write("BlockEntityStove");
                writer.Write(DialogTitle);
                TreeAttribute tree = new TreeAttribute();
                inventory.ToTreeAttributes(tree);
                tree.ToBytes(writer);
                data = ms.ToArray();
            }

            ((ICoreServerAPI)Api).Network.SendBlockEntityPacket(
                (IServerPlayer)byPlayer,
                Pos.X, Pos.Y, Pos.Z,
                (int)EnumBlockStovePacket.OpenGUI,
                data
            );

            byPlayer.InventoryManager.OpenInventory(inventory);
        }

        return true;
    }


    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
    {
        base.FromTreeAttributes(tree, worldForResolving);
        Inventory.FromTreeAttributes(tree.GetTreeAttribute("inventory"));

        if (Api != null)
        {
            Inventory.AfterBlocksLoaded(Api.World);
        }


        furnaceTemperature = tree.GetFloat("furnaceTemperature");
        //maxTemperature = tree.GetInt("maxTemperature");
        inputStackCookingTime = tree.GetFloat("oreCookingTime");

        if (Api != null)
        {
            if (Api.Side == EnumAppSide.Client)
            {
                //UpdateRenderer();

                if (clientDialog != null) SetDialogValues(clientDialog.Attributes);
            }


            if (Api.Side == EnumAppSide.Client && clientSidePrevBurning != IsBurning)
            {
                //ToggleAmbientSounds(IsBurning);
                clientSidePrevBurning = IsBurning;
                MarkDirty(true);
            }
        }

        if (energyStorage == null)
        {
            energyStorage = new FluxStorage(10000, 30, 0);
        }
        energyStorage.setEnergy(tree.GetInt("energy", 0));
    }


    //void UpdateRenderer()
    //{
    //    if (renderer == null) return;

    //    ItemStack contentStack = inputStack == null ? outputStack : inputStack;

    //    bool useOldRenderer =
    //        renderer.ContentStack != null &&
    //        renderer.contentStackRenderer != null &&
    //        contentStack.Collectible is IInFirepitRendererSupplier &&
    //        renderer.ContentStack.Equals(Api.World, contentStack, GlobalConstants.IgnoredStackAttributes)
    //    ;

    //    if (useOldRenderer) return; // Otherwise the cooking sounds restarts all the time

    //    //renderer.contentStackRenderer.Dispose();
    //    //renderer.contentStackRenderer = null;

    //    //if (contentStack?.Collectible is IInFirepitRendererSupplier)
    //    //{
    //    //    IInFirepitRenderer childrenderer = (contentStack?.Collectible as IInFirepitRendererSupplier).GetRendererWhenInFirepit(contentStack, this, contentStack == outputStack);
    //    //    if (childrenderer != null)
    //    //    {
    //    //        renderer.SetChildRenderer(contentStack, childrenderer);
    //    //        return;
    //    //    }
    //    //}

    //    //InFirePitProps props = GetRenderProps(contentStack);
    //    //if (contentStack.Collectible != null && !(contentStack.Collectible is IInFirepitMeshSupplier) && props != null)
    //    //{
    //    //    renderer.SetContents(contentStack, props.Transform);
    //    //}
    //    //else
    //    //{
    //        renderer.SetContents(null, null);
    //    //}
    //}


    void SetDialogValues(ITreeAttribute dialogTree)
    {
        dialogTree.SetFloat("furnaceTemperature", furnaceTemperature);

        dialogTree.SetInt("maxTemperature", maxTemperature);
        dialogTree.SetFloat("oreCookingTime", inputStackCookingTime);

        if (inputSlot.Itemstack != null)
        {
            float meltingDuration = inputSlot.Itemstack.Collectible.GetMeltingDuration(Api.World, inventory, inputSlot);

            dialogTree.SetFloat("oreTemperature", InputStackTemp);
            dialogTree.SetFloat("maxOreCookingTime", meltingDuration);
        }
        else
        {
            dialogTree.RemoveAttribute("oreTemperature");
        }

        dialogTree.SetString("outputText", inventory.GetOutputText());
        dialogTree.SetInt("haveCookingContainer", inventory.HaveCookingContainer ? 1 : 0);
        dialogTree.SetInt("quantityCookingSlots", inventory.CookingSlots.Length);
    }




    public override void ToTreeAttributes(ITreeAttribute tree)
    {
        base.ToTreeAttributes(tree);
        ITreeAttribute invtree = new TreeAttribute();
        Inventory.ToTreeAttributes(invtree);
        tree["inventory"] = invtree;

        tree.SetFloat("furnaceTemperature", furnaceTemperature);
        //tree.SetInt("maxTemperature", maxTemperature);
        tree.SetFloat("oreCookingTime", inputStackCookingTime);

        if (energyStorage != null)
        {
            tree.SetInt("energy", energyStorage.getEnergyStored());
        }
    }




    public override void OnBlockRemoved()
    {
        base.OnBlockRemoved();

        //renderer.Dispose();
        //renderer = null;

        if (clientDialog != null)
        {
            clientDialog.TryClose();
            if (clientDialog != null)
            {
                clientDialog.Dispose();
                clientDialog = null;
            }
        }
    }

    public override void OnBlockBroken()
    {
        base.OnBlockBroken();
    }

    public override void OnReceivedClientPacket(IPlayer player, int packetid, byte[] data)
    {
        if (packetid < 1000)
        {
            Inventory.InvNetworkUtil.HandleClientPacket(player, packetid, data);

            // Tell server to save this chunk to disk again
            Api.World.BlockAccessor.GetChunkAtBlockPos(Pos.X, Pos.Y, Pos.Z).MarkModified();

            return;
        }

        if (packetid == (int)EnumBlockStovePacket.CloseGUI)
        {
            if (player.InventoryManager != null)
            {
                player.InventoryManager.CloseInventory(Inventory);
            }
        }
    }

    public override void OnReceivedServerPacket(int packetid, byte[] data)
    {
        if (packetid == (int)EnumBlockStovePacket.OpenGUI)
        {
            using (MemoryStream ms = new MemoryStream(data))
            {
                BinaryReader reader = new BinaryReader(ms);

                string dialogClassName = reader.ReadString();
                string dialogTitle = reader.ReadString();

                TreeAttribute tree = new TreeAttribute();
                tree.FromBytes(reader);
                Inventory.FromTreeAttributes(tree);
                Inventory.ResolveBlocksOrItems();

                IClientWorldAccessor clientWorld = (IClientWorldAccessor)Api.World;

                SyncedTreeAttribute dtree = new SyncedTreeAttribute();
                SetDialogValues(dtree);

                if (clientDialog != null)
                {
                    clientDialog.TryClose();
                    clientDialog = null;
                }
                else
                {
                    clientDialog = new GuiDialogBlockEntityFurnace(dialogTitle, Inventory, Pos, dtree, Api as ICoreClientAPI);
                    clientDialog.OnClosed += () => { clientDialog.Dispose(); clientDialog = null; };
                    clientDialog.TryOpen();

                }

            }
        }

        if (packetid == (int)EnumBlockEntityPacketId.Close)
        {
            IClientWorldAccessor clientWorld = (IClientWorldAccessor)Api.World;
            clientWorld.Player.InventoryManager.CloseInventory(Inventory);
        }
    }

    #endregion

    #region Helper getters


    public ItemSlot inputSlot
    {
        get { return inventory[1]; }
    }

    public ItemSlot outputSlot
    {
        get { return inventory[2]; }
    }

    public ItemSlot[] otherCookingSlots
    {
        get { return inventory.CookingSlots; }
    }

    public ItemStack inputStack
    {
        get { return inventory[1].Itemstack; }
        set { inventory[1].Itemstack = value; inventory[1].MarkDirty(); }
    }

    public ItemStack outputStack
    {
        get { return inventory[2].Itemstack; }
        set { inventory[2].Itemstack = value; inventory[2].MarkDirty(); }
    }


    public CombustibleProperties fuelCombustibleOpts
    {
        get { return getCombustibleOpts(0); }
    }

    public CombustibleProperties getCombustibleOpts(int slotid)
    {
        ItemSlot slot = inventory[slotid];
        if (slot.Itemstack == null) return null;
        return slot.Itemstack.Collectible.CombustibleProps;
    }

    #endregion


    public override void OnStoreCollectibleMappings(Dictionary<int, AssetLocation> blockIdMapping, Dictionary<int, AssetLocation> itemIdMapping)
    {
        foreach (var slot in Inventory)
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

            slot.Itemstack.Collectible.OnStoreCollectibleMappings(Api.World, slot, blockIdMapping, itemIdMapping);
        }

        foreach (ItemSlot slot in inventory.CookingSlots)
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

            slot.Itemstack.Collectible.OnStoreCollectibleMappings(Api.World, slot, blockIdMapping, itemIdMapping);
        }
    }

    public override void OnLoadCollectibleMappings(IWorldAccessor worldForResolve, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, int schematicSeed)
    {
        foreach (var slot in Inventory)
        {
            if (slot.Itemstack == null) continue;
            if (!slot.Itemstack.FixMapping(oldBlockIdMapping, oldItemIdMapping, worldForResolve))
            {
                slot.Itemstack = null;
            }
            else
            {
                slot.Itemstack.Collectible.OnLoadCollectibleMappings(worldForResolve, slot, oldBlockIdMapping, oldItemIdMapping);
            }
        }

        foreach (ItemSlot slot in inventory.CookingSlots)
        {
            if (slot.Itemstack == null) continue;
            if (!slot.Itemstack.FixMapping(oldBlockIdMapping, oldItemIdMapping, Api.World))
            {
                slot.Itemstack = null;
            }
            else
            {
                slot.Itemstack.Collectible.OnLoadCollectibleMappings(worldForResolve, slot, oldBlockIdMapping, oldItemIdMapping);
            }
        }
    }

    //public EnumFirepitModel CurrentModel { get; private set; }

    //public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
    //{
    //    if (Block == null || Block.Code.Path.Contains("construct")) return false;


    //    ItemStack contentStack = inputStack == null ? outputStack : inputStack;
    //    MeshData contentmesh = getContentMesh(contentStack, tesselator);
    //    if (contentmesh != null)
    //    {
    //        mesher.AddMeshData(contentmesh);
    //    }

    //    //mesher.AddMeshData(getOrCreateMesh(burnState, contentState));

    //    return true;
    //}

    //private MeshData getContentMesh(ItemStack contentStack, ITesselatorAPI tesselator)
    //{
    //    CurrentModel = EnumFirepitModel.Normal;

    //    if (contentStack == null) return null;

    //    if (contentStack.Collectible is IInFirepitMeshSupplier)
    //    {
    //        EnumFirepitModel model = EnumFirepitModel.Normal;
    //        MeshData mesh = (contentStack.Collectible as IInFirepitMeshSupplier).GetMeshWhenInFirepit(contentStack, Api.World, Pos, ref model);
    //        this.CurrentModel = model;

    //        if (mesh != null)
    //        {
    //            return mesh;
    //        }

    //    }

    //    //if (contentStack.Collectible is IInFirepitRendererSupplier)
    //    //{
    //    //    EnumFirepitModel model = (contentStack.Collectible as IInFirepitRendererSupplier).GetDesiredFirepitModel(contentStack, this, contentStack == outputStack);
    //    //    this.CurrentModel = model;
    //    //    return null;
    //    //}

    //    //InFirePitProps renderProps = GetRenderProps(contentStack);

    //    //if (renderProps != null)
    //    //{
    //    //    this.CurrentModel = renderProps.UseFirepitModel;

    //    //    if (contentStack.Class != EnumItemClass.Item)
    //    //    {
    //    //        MeshData ingredientMesh;
    //    //        tesselator.TesselateBlock(contentStack.Block, out ingredientMesh);

    //    //        ingredientMesh.ModelTransform(renderProps.Transform);

    //    //        // Lower by 1 voxel if extinct
    //    //        if (!IsBurning && renderProps.UseFirepitModel != EnumFirepitModel.Spit) ingredientMesh.Translate(0, -1 / 16f, 0);

    //    //        return ingredientMesh;
    //    //    }

    //    //    return null;
    //    //}
    //    //else
    //    //{
    //    //    if (renderer.RequireSpit)
    //    //    {
    //    //        this.CurrentModel = EnumFirepitModel.Spit;
    //    //    }
    //    return null; // Mesh drawing is handled by the FirepitContentsRenderer
    //    //}

    //}

    //public override void OnBlockUnloaded()
    //{
    //    base.OnBlockUnloaded();

    //    renderer.Dispose();
    //}

    //InFirePitProps GetRenderProps(ItemStack contentStack)
    //{
    //    if (contentStack.ItemAttributes.KeyExists("inFirePitProps") == true)
    //    {
    //        InFirePitProps props = contentStack.ItemAttributes["inFirePitProps"].AsObject<InFirePitProps>();
    //        props.Transform.EnsureDefaultValues();
    //        return props;
    //    }
    //    return null;
    //}


    //public MeshData getOrCreateMesh(string burnstate, string contentstate)
    //{
    //    Dictionary<string, MeshData> Meshes = ObjectCacheUtil.GetOrCreate(Api, "firepit-meshes", () => new Dictionary<string, MeshData>());

    //    string key = burnstate + "-" + contentstate;
    //    MeshData meshdata;
    //    if (!Meshes.TryGetValue(key, out meshdata))
    //    {
    //        Block block = Api.World.BlockAccessor.GetBlock(Pos);
    //        if (block.BlockId == 0) return null;

    //        MeshData[] meshes = new MeshData[17];
    //        ITesselatorAPI mesher = ((ICoreClientAPI)Api).Tesselator;

    //        mesher.TesselateShape(block, Api.Assets.TryGet("shapes/block/wood/firepit/" + key + ".json").ToObject<Shape>(), out meshdata);
    //    }

    //    return meshdata;
    //}

    //public float GetHeatStrength(IWorldAccessor world, BlockPos heatSourcePos, BlockPos heatReceiverPos)
    //{
    //    return IsBurning ? 10 : 0;
    //}
}

public class GuiDialogBlockEntityFurnace : GuiDialogBlockEntity
{
    bool haveCookingContainer;
    string currentOutputText;

    ElementBounds cookingSlotsSlotBounds;

    long lastRedrawMs;
    EnumPosFlag screenPos;

    protected override double FloatyDialogPosition
    {
        get
        {
            return 0.6;
        }
    }

    protected override double FloatyDialogAlign
    {
        get
        {
            return 0.8;
        }
    }

    public GuiDialogBlockEntityFurnace(string dialogTitle, InventoryBase Inventory, BlockPos BlockEntityPosition, SyncedTreeAttribute tree, ICoreClientAPI capi) : base(dialogTitle, Inventory, BlockEntityPosition, capi)
    {
        if (IsDuplicate) return;
        tree.OnModified.Add(new TreeModifiedListener() { listener = OnAttributesModified });
        Attributes = tree;
    }

    private void OnInventorySlotModified(int slotid)
    {
        SetupDialog();
    }

    void SetupDialog()
    {
        ItemSlot hoveredSlot = capi.World.Player.InventoryManager.CurrentHoveredSlot;
        if (hoveredSlot != null && hoveredSlot.Inventory.InventoryID != Inventory.InventoryID)
        {
            //capi.Input.TriggerOnMouseLeaveSlot(hoveredSlot); - wtf is this for?
            hoveredSlot = null;
        }




        string newOutputText = Attributes.GetString("outputText", "");
        bool newHaveCookingContainer = Attributes.GetInt("haveCookingContainer") > 0;

        GuiElementDynamicText outputTextElem;

        if (haveCookingContainer == newHaveCookingContainer && SingleComposer != null)
        {
            outputTextElem = SingleComposer.GetDynamicText("outputText");
            outputTextElem.SetNewText(newOutputText, true);
            var loc = SingleComposer.GetCustomDraw("symbolDrawer");
            if (loc != null) loc.Redraw();

            haveCookingContainer = newHaveCookingContainer;
            currentOutputText = newOutputText;

            outputTextElem.Bounds.fixedOffsetY = 0;

            //if (outputTextElem.QuantityTextLines > 2)
            //{
            //    outputTextElem.Bounds.fixedOffsetY = -outputTextElem.Font.GetFontExtents().Height / RuntimeEnv.GUIScale * 0.65;
            //}

            outputTextElem.Bounds.CalcWorldBounds();

            return;
        }


        haveCookingContainer = newHaveCookingContainer;
        currentOutputText = newOutputText;

        int qCookingSlots = Attributes.GetInt("quantityCookingSlots");

        ElementBounds stoveBounds = ElementBounds.Fixed(0, 0, 210, 250);

        cookingSlotsSlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 30 + 45, 4, qCookingSlots / 4);
        cookingSlotsSlotBounds.fixedHeight += 10;

        double top = cookingSlotsSlotBounds.fixedHeight + cookingSlotsSlotBounds.fixedY;

        ElementBounds inputSlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, top, 1, 1);
        ElementBounds fuelSlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 110 + top, 1, 1);
        ElementBounds outputSlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 153, top, 1, 1);

        // 2. Around all that is 10 pixel padding
        ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
        bgBounds.BothSizing = ElementSizing.FitToChildren;
        bgBounds.WithChildren(stoveBounds);

        // 3. Finally Dialog
        ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog
            .WithFixedAlignmentOffset(IsRight(screenPos) ? -GuiStyle.DialogToScreenPadding : GuiStyle.DialogToScreenPadding, 0)
            .WithAlignment(IsRight(screenPos) ? EnumDialogArea.RightMiddle : EnumDialogArea.LeftMiddle)
        ;


        if (!capi.Settings.Bool["immersiveMouseMode"])
        {
            dialogBounds.fixedOffsetY += (stoveBounds.fixedHeight + 65 + (haveCookingContainer ? 25 : 0)) * YOffsetMul(screenPos);
        }


        int[] cookingSlotIds = new int[qCookingSlots];
        for (int i = 0; i < qCookingSlots; i++) cookingSlotIds[i] = 3 + i;

        SingleComposer = capi.Gui
            .CreateCompo("blockentitystove" + BlockEntityPosition, dialogBounds)
            .AddShadedDialogBG(bgBounds)
            .AddDialogTitleBar(DialogTitle, OnTitleBarClose)
            .BeginChildElements(bgBounds)
                .AddDynamicCustomDraw(stoveBounds, OnBgDraw, "symbolDrawer")
                .AddDynamicText("", CairoFont.WhiteDetailText(), EnumTextOrientation.Left, ElementBounds.Fixed(0, 30, 210, 45), "outputText")
                .AddIf(haveCookingContainer)
                    .AddItemSlotGrid(Inventory, SendInvPacket, 4, cookingSlotIds, cookingSlotsSlotBounds, "ingredientSlots")
                .EndIf()
                .AddDynamicText("", CairoFont.WhiteDetailText(), EnumTextOrientation.Left, fuelSlotBounds.RightCopy(17, 16).WithFixedSize(60, 30), "fueltemp")
                .AddItemSlotGrid(Inventory, SendInvPacket, 1, new int[] { 1 }, inputSlotBounds, "oreslot")
                .AddDynamicText("", CairoFont.WhiteDetailText(), EnumTextOrientation.Left, inputSlotBounds.RightCopy(23, 16).WithFixedSize(60, 30), "oretemp")

                .AddItemSlotGrid(Inventory, SendInvPacket, 1, new int[] { 2 }, outputSlotBounds, "outputslot")
            .EndChildElements()
            .Compose();

        lastRedrawMs = capi.ElapsedMilliseconds;

        if (hoveredSlot != null)
        {
            SingleComposer.OnMouseMove(new MouseEvent(capi.Input.MouseX, capi.Input.MouseY));
        }

        outputTextElem = SingleComposer.GetDynamicText("outputText");
        outputTextElem.SetNewText(currentOutputText, true);
        outputTextElem.Bounds.fixedOffsetY = 0;

        //if (outputTextElem.QuantityTextLines > 2)
        //{
        //    outputTextElem.Bounds.fixedOffsetY = -outputTextElem.Font.GetFontExtents().Height / RuntimeEnv.GUIScale * 0.65;
        //}
        outputTextElem.Bounds.CalcWorldBounds();


    }

    private void OnAttributesModified()
    {
        if (!IsOpened()) return;

        float ftemp = Attributes.GetFloat("furnaceTemperature");
        float otemp = Attributes.GetFloat("oreTemperature");

        string fuelTemp = ftemp.ToString("#");
        string oreTemp = otemp.ToString("#");

        fuelTemp += fuelTemp.Length > 0 ? "°C" : "";
        oreTemp += oreTemp.Length > 0 ? "°C" : "";

        if (ftemp > 0 && ftemp <= 20) fuelTemp = Lang.Get("Cold");
        if (otemp > 0 && otemp <= 20) oreTemp = Lang.Get("Cold");

        SingleComposer.GetDynamicText("fueltemp").SetNewText(fuelTemp);
        SingleComposer.GetDynamicText("oretemp").SetNewText(oreTemp);

        if (capi.ElapsedMilliseconds - lastRedrawMs > 500)
        {
            if (SingleComposer != null)
            {
                var loc = SingleComposer.GetCustomDraw("symbolDrawer");
                if (loc != null) loc.Redraw();
            }
            
            lastRedrawMs = capi.ElapsedMilliseconds;
        }
    }

    private void OnBgDraw(Context ctx, ImageSurface surface, ElementBounds currentBounds)
    {
        double top = cookingSlotsSlotBounds.fixedHeight + cookingSlotsSlotBounds.fixedY;

        // 1. Fire
        ctx.Save();
        Matrix m = ctx.Matrix;
        m.Translate(GuiElement.scaled(5), GuiElement.scaled(53 + top));
        m.Scale(GuiElement.scaled(0.25), GuiElement.scaled(0.25));
        ctx.Matrix = m;
        capi.Gui.Icons.DrawFlame(ctx);

        double dy = 210 - 210 * (Attributes.GetFloat("furnaceTemperature", 0) / Attributes.GetInt("maxTemperature", 1));
        ctx.Rectangle(0, dy, 200, 210 - dy);
        ctx.Clip();
        LinearGradient gradient = new LinearGradient(0, GuiElement.scaled(250), 0, 0);
        gradient.AddColorStop(0, new Color(1, 1, 0, 1));
        gradient.AddColorStop(1, new Color(1, 0, 0, 1));
        ctx.SetSource(gradient);
        capi.Gui.Icons.DrawFlame(ctx, 0, false, false);
        gradient.Dispose();
        ctx.Restore();


        // 2. Arrow Right
        ctx.Save();
        m = ctx.Matrix;
        m.Translate(GuiElement.scaled(63), GuiElement.scaled(top + 2));
        m.Scale(GuiElement.scaled(0.6), GuiElement.scaled(0.6));
        ctx.Matrix = m;
        capi.Gui.Icons.DrawArrowRight(ctx, 2);

        double cookingRel = Attributes.GetFloat("oreCookingTime") / Attributes.GetFloat("maxOreCookingTime", 1);


        ctx.Rectangle(5, 0, 125 * cookingRel, 100);
        ctx.Clip();
        gradient = new LinearGradient(0, 0, 200, 0);
        gradient.AddColorStop(0, new Color(0, 0.4, 0, 1));
        gradient.AddColorStop(1, new Color(0.2, 0.6, 0.2, 1));
        ctx.SetSource(gradient);
        capi.Gui.Icons.DrawArrowRight(ctx, 0, false, false);
        gradient.Dispose();
        ctx.Restore();
    }



    private void SendInvPacket(object packet)
    {
        capi.Network.SendBlockEntityPacket(BlockEntityPosition.X, BlockEntityPosition.Y, BlockEntityPosition.Z, packet);
    }


    private void OnTitleBarClose()
    {
        TryClose();
    }


    public override void OnGuiOpened()
    {
        base.OnGuiOpened();
        Inventory.SlotModified += OnInventorySlotModified;

        screenPos = GetFreePos("smallblockgui");
        OccupyPos("smallblockgui", screenPos);
        SetupDialog();
    }

    public override void OnGuiClosed()
    {
        Inventory.SlotModified -= OnInventorySlotModified;

        SingleComposer.GetSlotGrid("oreslot").OnGuiClosed(capi);
        SingleComposer.GetSlotGrid("outputslot").OnGuiClosed(capi);
        var loc = SingleComposer.GetSlotGrid("ingredientSlots");
        if (loc != null) loc.OnGuiClosed(capi);

        base.OnGuiClosed();

        FreePos("smallblockgui", screenPos);
    }
}