
using Vintagestory.API.MathTools;

public interface IIOEnergySideConfig
{
    IOEnergySideConfig getEnergySideConfig(BlockFacing side);
    bool toggleSide(BlockFacing side);
}
