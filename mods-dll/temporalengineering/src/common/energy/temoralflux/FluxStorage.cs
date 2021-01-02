using System;

public class FluxStorage
{
	protected int energy;
	protected int capacity;
	protected int limitReceive;
	protected int limitExtract;

	public FluxStorage(int capacity, int limitReceive, int limitExtract)
	{
		this.capacity = capacity;
		this.limitReceive = limitReceive;
		this.limitExtract = limitExtract;
	}

	public void setCapacity(int capacity)
	{
		this.capacity = capacity;
		if (energy > capacity)
			energy = capacity;
	}

	public void setLimitTransfer(int limitTransfer)
	{
		setLimitReceive(limitTransfer);
		setMaxExtract(limitTransfer);
	}

	public void setLimitReceive(int limitReceive)
	{
		this.limitReceive = limitReceive;
	}

	public void setMaxExtract(int limitExtract)
	{
		this.limitExtract = limitExtract;
	}

	public int getLimitReceive()
	{
		return limitReceive;
	}

	public int getLimitExtract()
	{
		return limitExtract;
	}

	public void setEnergy(int energy)
	{
		this.energy = energy;
		if (this.energy > capacity)
			this.energy = capacity;
		else if (this.energy < 0)
			this.energy = 0;
	}

	public void modifyEnergyStored(int energy)
	{
		this.energy += energy;
		if (this.energy > capacity)
			this.energy = capacity;
		else if (this.energy < 0)
			this.energy = 0;
	}

	public int receiveEnergy(int energy, bool simulate)
	{
		int received = Math.Min(capacity - this.energy, Math.Min(this.limitReceive, energy));
		if (!simulate)
			this.energy += received;
		return received;
	}

	public int extractEnergy(int energy, bool simulate)
	{
		int extracted = Math.Min(this.energy, Math.Min(this.limitExtract, energy));
		if (!simulate)
			this.energy -= extracted;
		return extracted;
	}

	public int getEnergyStored()
	{
		return energy;
	}

	public int getMaxEnergyStored()
	{
		return capacity;
	}

	public string GetFluxStorageInfo()
	{
		return energy.ToString() + "/" + capacity.ToString();
	}

	public int scaleStoredEnergyTo(int scale)
	{
		return (int)(scale * (getEnergyStored() / (float)getMaxEnergyStored()));
	}
}