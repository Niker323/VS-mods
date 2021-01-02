using Vintagestory.API.MathTools;

public interface IEnergyReceiver : IEnergyHandler
{

	/**
	 * Add energy to an IEnergyReceiver, internal distribution is left entirely to the IEnergyReceiver.
	 *
	 * @param from       Orientation the energy is received from.
	 * @param maxReceive Maximum amount of energy to receive.
	 * @param simulate   If TRUE, the charge will only be simulated.
	 * @return Amount of energy that was (or would have been, if simulated) received.
	 */
	int receiveEnergy(BlockFacing from, int maxReceive, bool simulate);

}