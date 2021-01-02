using Vintagestory.API.MathTools;

public interface IEnergyHandler : IEnergyConnection
{

	/**
	 * Returns the amount of energy currently stored.
	 */
	int getEnergyStored(BlockFacing from);

	/**
	 * Returns the maximum amount of energy that can be stored.
	 */
	int getMaxEnergyStored(BlockFacing from);

}