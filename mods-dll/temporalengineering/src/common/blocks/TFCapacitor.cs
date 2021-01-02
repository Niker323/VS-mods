using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Vintagestory.API;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

public class TFCapacitor : BlockEntity, IFluxStorage, IIOEnergySideConfig
{
    public Dictionary<BlockFacing, IOEnergySideConfig> sideConfig = new Dictionary<BlockFacing, IOEnergySideConfig>(IOEnergySideConfig.VALUES.Length);
    public FluxStorage energyStorage;

    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
    {
        base.GetBlockInfo(forPlayer, dsc);
        if (energyStorage != null)
        {
            dsc.AppendLine(energyStorage.GetFluxStorageInfo());
        }
    }

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
    {
        base.FromTreeAttributes(tree, worldForResolving);

        if (energyStorage == null)
        {
            energyStorage = new FluxStorage(getMaxStorage(), getMaxInput(), getMaxOutput());
        }
        energyStorage.setEnergy(tree.GetInt("energy", 0));
    }

    public override void ToTreeAttributes(ITreeAttribute tree)
    {
        base.ToTreeAttributes(tree);

        if (energyStorage != null)
        {
            tree.SetInt("energy", energyStorage.getEnergyStored());
        }
    }

    public override void Initialize(ICoreAPI api)
    {
        base.Initialize(api);
        Api = api;
        //Debug.WriteLine("Initialize");
        //Debug.WriteLine(api is ICoreServerAPI);

        if (api.World.Side == EnumAppSide.Server)
        {
            RegisterGameTickListener(tick, 50);
        }

        if (energyStorage == null)
        {
            energyStorage = new FluxStorage(getMaxStorage(), getMaxInput(), getMaxOutput());
        }
        if (sideConfig.Count == 0)
        {
            string[] splitedPath = Block.Code.ToString().Split('-');
            if (splitedPath.Length >= 6)
            {
                foreach (BlockFacing f in BlockFacing.ALLFACES)
                {
                    if (splitedPath[splitedPath.Length - 6 + f.Index] == IOEnergySideConfig.INPUT.toString())
                        sideConfig.Add(f, IOEnergySideConfig.INPUT);
                    else if (splitedPath[splitedPath.Length - 6 + f.Index] == IOEnergySideConfig.OUTPUT.toString())
                        sideConfig.Add(f, IOEnergySideConfig.OUTPUT);
                    else
                        sideConfig.Add(f, IOEnergySideConfig.NONE);
                }
            }
            else
            {
                foreach (BlockFacing f in BlockFacing.ALLFACES)
                {
                    sideConfig.Add(f, IOEnergySideConfig.NONE);
                }
            }
        }
    }

    private void tick(float dt)
    {
        foreach (BlockFacing f in BlockFacing.ALLFACES)
            transferEnergy(f);
        MarkDirty();
    }

    protected void transferEnergy(BlockFacing side)
    {
        if (sideConfig[side] != IOEnergySideConfig.OUTPUT) return;
        BlockPos outPos = Pos.Copy().Offset(side);
        BlockEntity tileEntity = Api.World.BlockAccessor.GetBlockEntity(outPos);
        if (tileEntity == null) return;
        if (!(tileEntity is IFluxStorage)) return;
        int eout = Math.Min(getMaxOutput(), energyStorage.getEnergyStored());
        energyStorage.modifyEnergyStored(-EnergyCore.insertFlux((IFluxStorage)tileEntity, eout, false, side.Opposite));
    }

    public int receiveEnergy(BlockFacing from, int maxReceive, bool simulate)
    {
        if (from == null || sideConfig[from] == IOEnergySideConfig.INPUT)
        {
            return energyStorage.receiveEnergy(Math.Min(getMaxInput(), maxReceive), simulate);
        }
        return 0;
    }

    public IOEnergySideConfig getEnergySideConfig(BlockFacing side)
    {
        return sideConfig[side];
    }

    public bool toggleSide(BlockFacing side)//, PlayerEntity player
    {
        //Debug.WriteLine("toggleSide");
        //Debug.WriteLine(Api is ICoreServerAPI);
        if (Api is ICoreServerAPI)
        {
            sideConfig[side] = sideConfig[side].next();

            //Block block = Api.World.BlockAccessor.GetBlock(Pos);
            //string[] splitedPath = block.Code.Path.Split('-');
            //Debug.WriteLine(side.ToString());
            //Debug.WriteLine(sideConfig[side].toString());
            //Debug.WriteLine(Block.CodeWithVariant(side.ToString(), sideConfig[side].toString()).Path);


            Api.World.BlockAccessor.ExchangeBlock(Api.World.GetBlock(Block.CodeWithVariant(side.ToString(), sideConfig[side].toString())).BlockId, Pos);
        }

        //MarkDirty();
        return true;
    }

    public int getMaxStorage()
    {
        if (Block != null && Block.Attributes != null && Block.Attributes["storage"] != null)
        {
            return Block.Attributes["storage"].AsInt();
        }
        return 10000;
    }

    public int getMaxInput()
    {
        if (Block != null && Block.Attributes != null && Block.Attributes["input"] != null)
        {
            return Block.Attributes["input"].AsInt();
        }
        return 100;
    }

    public int getMaxOutput()
    {
        if (Block != null && Block.Attributes != null && Block.Attributes["output"] != null)
        {
            return Block.Attributes["output"].AsInt();
        }
        return 100;
    }

    public FluxStorage GetFluxStorage()
    {
        return energyStorage;
    }
}