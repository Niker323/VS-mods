using System.Collections.Generic;
using Vintagestory.API.MathTools;

public interface IWirePoint
{
    Dictionary<BlockPos, WireClass> GetWiresList();
    BlockPos GetBlockPos();
    void AddWire(BlockPos point, WireClass wire);
    void RemoveWire(BlockPos point);
    bool IsConnectedTo(BlockPos point);
}
