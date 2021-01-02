using Vintagestory.API.MathTools;

public interface IEnergyConnection
{

	/**
	 * Returns TRUE if the TileEntity can connect on a given side.
	 */
	bool canConnectEnergy(BlockFacing from);

}