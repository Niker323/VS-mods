using System;

public class FluxStorage
{
	protected float energy;
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

	public void setEnergy(float energy)
	{
		this.energy = energy;
		if (this.energy > capacity)
			this.energy = capacity;
		else if (this.energy < 0)
			this.energy = 0;
	}

	public void modifyEnergyStored(float energy)
	{
		this.energy += energy;
		if (this.energy > capacity)
			this.energy = capacity;
		else if (this.energy < 0)
			this.energy = 0;
	}

	public float receiveEnergy(float energy, bool simulate, float dt)
	{
		float received = Math.Min(capacity - this.energy, Math.Min(limitReceive * dt, energy));
		if (!simulate)
			this.energy += received;
		return received;
	}

	public float extractEnergy(float energy, bool simulate, float dt)
	{
		float extracted = Math.Min(this.energy, Math.Min(limitExtract * dt, energy));
		if (!simulate)
			this.energy -= extracted;
		return extracted;
	}

	public float getEnergyStored()
	{
		return energy;
	}

	public int getMaxEnergyStored()
	{
		return capacity;
	}

	public string GetFluxStorageInfo()
	{
		return Math.Round(energy).ToString() + "/" + capacity.ToString();
	}

	public int scaleStoredEnergyTo(int scale)
	{
		return (int)(scale * (getEnergyStored() / getMaxEnergyStored()));
	}
}