using System.Collections.Generic;
using Vintagestory.API.MathTools;

public interface IWirePoint
{
    Dictionary<IWirePoint, WireClass> GetWiresList();
    BlockPos GetBlockPos();
    void AddWire(IWirePoint point, WireClass wire);
    void RemoveWire(IWirePoint point);
}
