
using Vintagestory.API.MathTools;

public interface IFluxStorage
{
    FluxStorage GetFluxStorage();
    int receiveEnergy(BlockFacing from, int maxReceive, bool simulate);
    bool CanWireConnect(BlockFacing side);
}
