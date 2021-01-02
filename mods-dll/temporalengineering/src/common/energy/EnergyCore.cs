using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

public class EnergyCore
{

    //static HashMap<Item, bool> reverseInsertion = new HashMap<>();

    //public static int forceExtractFlux(ItemStack stack, int energy, bool simulate)
    //{
    //    if (stack.isEmpty())
    //        return 0;
    //    bool b = reverseInsertion.get(stack.getItem());
    //    if (b == bool.TRUE)
    //    {
    //        int stored = getEnergyStored(stack);
    //        insertFlux(stack, -energy, simulate);
    //        return stored - getEnergyStored(stack);
    //    }
    //    else
    //    {
    //        int drawn = extractFlux(stack, energy, simulate);
    //        if (b == null)
    //        {
    //            int stored = getEnergyStored(stack);
    //            insertFlux(stack, -energy, simulate);
    //            drawn = stored - getEnergyStored(stack);
    //            //if reverse insertion was succesful, it'll be the default approach in future
    //            reverseInsertion.put(stack.getItem(), drawn > 0 ? bool.TRUE : bool.FALSE);
    //        }
    //        return drawn;
    //    }
    //}

    public static int getEnergyStored(IFluxStorage tileEntity)
    {
        if (tileEntity == null)
            return 0;
        return tileEntity.GetFluxStorage().getEnergyStored();
    }

    public static int getMaxEnergyStored(IFluxStorage tileEntity)
    {
        if (tileEntity == null)
            return 0;
        return tileEntity.GetFluxStorage().getMaxEnergyStored();
    }

    //    public static bool isFluxReceiver(BlockEntity tileEntity, BlockFacing facing = null)
    //    {
    //        if (tileEntity == null)
    //            return false;
    //        return tileEntity.getCapability(CapabilityEnergy.ENERGY, facing)
    //                .map(IEnergyStorage::canReceive)
    //                .orElse(false);
    //    }

    //    public static bool isFluxRelated(ICapabilityProvider tile, BlockFacing facing = null)
    //    {
    //        if (tile == null)
    //            return false;
    //        return tile.getCapability(CapabilityEnergy.ENERGY, facing).isPresent();
    //    }

    public static int insertFlux(IFluxStorage tile, int energy, bool simulate, BlockFacing facing = null)
    {
        if (tile == null)
            return 0;
        return tile.receiveEnergy(facing, energy, simulate);
    }

    //    public static int extractFlux(ICapabilityProvider tile, int energy, bool simulate, BlockFacing facing = null)
    //    {
    //        if (tile == null)
    //            return 0;
    //        return tile.getCapability(CapabilityEnergy.ENERGY, facing)
    //                .map(storage->storage.extractEnergy(energy, simulate))
    //                .orElse(0);
    //    }

    //    /**
    //	 * This method takes a list of IEnergyStorages and a total output amount. It sorts the storage by how much they
    //	 * accept, distributes the energy evenly between them, starting with the lowest acceptance.
    //	 * Overflow lands in the storage with the highest acceptance.
    //	 *
    //	 * @param storages a collection of outputs
    //	 * @param amount   the total amount to be distributed
    //	 * @param simulate true if no energy should be inserted into the outputs
    //	 * @return the amount of energy remaining after insertion
    //	 */
    //    public static int distributeFlux(Collection<IEnergyStorage> storages, int amount, bool simulate)
    //    {
    //        final int finalAmount = amount;
    //        storages = storages.stream()
    //                // Remove null storages
    //                .filter(Objects::nonNull)
    //                // Map to how much each storage can accept
    //                .map(storage->Pair.of(storage, storage.receiveEnergy(finalAmount, true)))
    //                // Sort ascending by acceptance
    //                .sorted(Comparator.comparingInt(Pair::getRight))
    //                // Unmap them
    //                .map(Pair::getLeft)
    //                // Collect
    //                .collect(Collectors.toList());

    //        int remainingOutputs = storages.size();
    //        for (IEnergyStorage storage : storages)
    //        {
    //            int possibleOutput = (int)Math.ceil(amount / (float)remainingOutputs);
    //            int inserted = storage.receiveEnergy(possibleOutput, simulate);
    //            amount -= inserted;
    //            remainingOutputs--;
    //        }
    //        return amount;
    //    }

    //    public interface IIEInternalFluxHandler : IIEInternalFluxConnector, IFluxReceiver, IFluxProvider
    //    {
    //        //@Nonnull
    //        FluxStorage getFluxStorage();

    //		default void postEnergyTransferUpdate(int energy, bool simulate)
    //        {

    //        }

    //	//@Override
    //		default int extractEnergy(BlockFacing fd, int amount, bool simulate)
    //        {
    //            if (((TileEntity)this).getWorld().isRemote || getEnergySideConfig(fd) != IOSideConfig.OUTPUT)
    //                return 0;
    //            int r = getFluxStorage().extractEnergy(amount, simulate);
    //            postEnergyTransferUpdate(-r, simulate);
    //            return r;
    //        }

    //	//@Override
    //		default int getEnergyStored(BlockFacing fd)
    //        {
    //            return getFluxStorage().getEnergyStored();
    //        }

    //	//@Override
    //		default int getMaxEnergyStored(BlockFacing fd)
    //        {
    //            return getFluxStorage().getMaxEnergyStored();
    //        }

    //	//@Override
    //		default int receiveEnergy(BlockFacing fd, int amount, bool simulate)
    //        {
    //            if (((TileEntity)this).getWorld().isRemote || getEnergySideConfig(fd) != IOSideConfig.INPUT)
    //                return 0;
    //            int r = getFluxStorage().receiveEnergy(amount, simulate);
    //            postEnergyTransferUpdate(r, simulate);
    //            return r;
    //        }
    //    }

    //    public interface IIEInternalFluxConnector : IFluxConnection
    //    {
    //        //@Nonnull
    //        IOSideConfig getEnergySideConfig(BlockFacing facing);

    //	//@Override
    //		default bool canConnectEnergy(BlockFacing fd)
    //        {
    //            return getEnergySideConfig(fd) != IOSideConfig.NONE;
    //        }


    //        IEForgeEnergyWrapper getCapabilityWrapper(BlockFacing facing);
    //    }

    //    public static class IEForgeEnergyWrapper implements IEnergyStorage
    //    {
    //        final IIEInternalFluxConnector fluxHandler;

    //        public final BlockFacing side;

    //public IEForgeEnergyWrapper(IIEInternalFluxConnector fluxHandler, BlockFacing side)
    //    {
    //        this.fluxHandler = fluxHandler;
    //        this.side = side;
    //    }

    //    //@Override
    //    public int receiveEnergy(int maxReceive, bool simulate)
    //    {
    //        if (fluxHandler instanceof IIEInternalFluxHandler)
    //				return ((IIEInternalFluxHandler)fluxHandler).receiveEnergy(side, maxReceive, simulate);
    //        return 0;
    //    }

    //    //@Override
    //    public int extractEnergy(int maxExtract, bool simulate)
    //    {
    //        if (fluxHandler instanceof IIEInternalFluxHandler)
    //				return ((IIEInternalFluxHandler)fluxHandler).extractEnergy(side, maxExtract, simulate);
    //        return 0;
    //    }

    //    //@Override
    //    public int getEnergyStored()
    //    {
    //        if (fluxHandler instanceof IIEInternalFluxHandler)
    //				return ((IIEInternalFluxHandler)fluxHandler).getEnergyStored(side);
    //        return 0;
    //    }

    //    //@Override
    //    public int getMaxEnergyStored()
    //    {
    //        if (fluxHandler instanceof IIEInternalFluxHandler)
    //				return ((IIEInternalFluxHandler)fluxHandler).getMaxEnergyStored(side);
    //        return 0;
    //    }

    //    //@Override
    //    public bool canExtract()
    //    {
    //        if (fluxHandler instanceof IIEInternalFluxHandler)
    //				return ((IIEInternalFluxHandler)fluxHandler).getFluxStorage().getLimitExtract() > 0;
    //        return false;
    //    }

    //    //@Override
    //    public bool canReceive()
    //    {
    //        if (fluxHandler instanceof IIEInternalFluxHandler)
    //				return ((IIEInternalFluxHandler)fluxHandler).getFluxStorage().getLimitReceive() > 0;
    //        return false;
    //    }

    //    public static IEForgeEnergyWrapper[] getDefaultWrapperArray(IIEInternalFluxConnector handler)
    //    {
    //        return new IEForgeEnergyWrapper[]{
    //                    new IEForgeEnergyWrapper(handler, BlockFacing.DOWN),
    //                    new IEForgeEnergyWrapper(handler, BlockFacing.UP),
    //                    new IEForgeEnergyWrapper(handler, BlockFacing.NORTH),
    //                    new IEForgeEnergyWrapper(handler, BlockFacing.SOUTH),
    //                    new IEForgeEnergyWrapper(handler, BlockFacing.WEST),
    //                    new IEForgeEnergyWrapper(handler, BlockFacing.EAST)
    //            };
    //    }
    //}

    //public interface IIEEnergyItem : IFluxContainerItem
    //{
    //	//@Override
    //		default int receiveEnergy(ItemStack container, int energy, bool simulate)
    //    {
    //        return ItemNBTHelper.insertFluxItem(container, energy, getMaxEnergyStored(container), simulate);
    //    }

    //	//@Override
    //		default int extractEnergy(ItemStack container, int energy, bool simulate)
    //    {
    //        return ItemNBTHelper.extractFluxFromItem(container, energy, simulate);
    //    }

    //	//@Override
    //		default int getEnergyStored(ItemStack container)
    //    {
    //        return ItemNBTHelper.getFluxStoredInItem(container);
    //    }
    //}

    //public static class ItemEnergyStorage implements IEnergyStorage
    //{
    //    ItemStack stack;
    //    IIEEnergyItem ieEnergyItem;


    //        public ItemEnergyStorage(ItemStack item)
    //{
    //    assert(item.getItem() instanceof IIEEnergyItem);
    //    this.stack = item;
    //    this.ieEnergyItem = (IIEEnergyItem)item.getItem();
    //}

    ////@Override
    //public int receiveEnergy(int maxReceive, bool simulate)
    //{
    //    return this.ieEnergyItem.receiveEnergy(stack, maxReceive, simulate);
    //}

    ////@Override
    //public int extractEnergy(int maxExtract, bool simulate)
    //{
    //    return this.ieEnergyItem.extractEnergy(stack, maxExtract, simulate);
    //}

    ////@Override
    //public int getEnergyStored()
    //{
    //    return this.ieEnergyItem.getEnergyStored(stack);
    //}

    ////@Override
    //public int getMaxEnergyStored()
    //{
    //    return this.ieEnergyItem.getMaxEnergyStored(stack);
    //}

    ////@Override
    //public bool canExtract()
    //{
    //    return true;
    //}

    ////@Override
    //public bool canReceive()
    //{
    //    return true;
    //}
    //	}

}