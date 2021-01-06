
using System;
using System.Collections.Generic;

public class EnergyDuctCore
{
    public FluxStorage storage;
    public List<IEnergyPoint> ducts = new List<IEnergyPoint>();

    public EnergyDuctCore(int io)
    {
        storage = new FluxStorage(io, io, io);
    }

    public void OnDuctRemoved(IEnergyPoint duct)
    {
        ducts.Remove(duct);
        if (ducts.Count > 0)
        {
            foreach (BlockEntityEnergyDuct item in ducts)
            {
                item.core = null;
            }
            foreach (BlockEntityEnergyDuct item in ducts)
            {
                if (item.core == null)
                {
                    item.InitializeEnergyPoint();
                }
            }
        }
    }

    public EnergyDuctCore CombineCores(EnergyDuctCore core)
    {
        storage.setEnergy(Math.Min(storage.getEnergyStored() + core.storage.getEnergyStored(), storage.getMaxEnergyStored()));
        foreach (BlockEntityEnergyDuct item in core.ducts)
        {
            item.core = this;
        }
        ducts.AddRange(core.ducts);
        return this;
    }
}
