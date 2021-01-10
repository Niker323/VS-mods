
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

public interface IEnergyPoint
{
    void InitializeEnergyPoint(ICoreAPI api);
    void SetCore(EnergyDuctCore core);
    EnergyDuctCore GetCore();
    void AddSkipSide(BlockFacing face);
}