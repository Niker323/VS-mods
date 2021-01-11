
using Vintagestory.API.MathTools;

public interface IFluxStorage
{
    FluxStorage GetFluxStorage();
    float receiveEnergy(BlockFacing from, float maxReceive, bool simulate, float dt);
    bool CanWireConnect(BlockFacing side);
}
