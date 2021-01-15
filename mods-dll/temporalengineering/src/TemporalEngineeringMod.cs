using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace TemporalEngineering
{
    public class TemporalEngineeringMod : ModSystem
    {

        public override void Start(ICoreAPI api)
        {
            base.Start(api);

            api.RegisterBlockClass("BlockTFRotaryGenerator", typeof(BlockTFRotaryGenerator));
            api.RegisterBlockClass("BlockWatermillRotor", typeof(BlockWatermillRotor));
            api.RegisterBlockClass("BlockEnergyDuct", typeof(BlockEnergyDuct));
            api.RegisterBlockClass("BlockFurnace", typeof(BlockFurnace));
            api.RegisterBlockClass("BlockMPMultiblockBase", typeof(BlockMPMultiblockBase));
            api.RegisterBlockClass("BlockSideconfigInteractions", typeof(BlockSideconfigInteractions));
            api.RegisterBlockClass("BlockTFRelay", typeof(BlockTFRelay));
            api.RegisterBlockClass("BlockTFEngine", typeof(BlockTFEngine));
            api.RegisterBlockClass("BlockTFForge", typeof(BlockTFForge));
            api.RegisterBlockClass("BlockCharger", typeof(BlockTFCharger));
            api.RegisterBlockClass("BlockConnector", typeof(BlockConnector));
            api.RegisterBlockClass("BlockTFCapacitor", typeof(BlockTFCapacitor));

            api.RegisterBlockEntityClass("TFCapacitor", typeof(TFCapacitor));
            api.RegisterBlockEntityClass("TFRotaryGenerator", typeof(TFRotaryGenerator));
            api.RegisterBlockEntityClass("BlockEntityEnergyDuct", typeof(BlockEntityEnergyDuct));
            api.RegisterBlockEntityClass("BlockEntityFurnace", typeof(BlockEntityFurnace));
            api.RegisterBlockEntityClass("BEMPMultiblockBase", typeof(BEMPMultiblockBase));
            api.RegisterBlockEntityClass("BETFRelay", typeof(BETFRelay));
            api.RegisterBlockEntityClass("BlockEntityTFEngine", typeof(BlockEntityTFEngine));
            api.RegisterBlockEntityClass("BlockEntityTFForge", typeof(BlockEntityTFForge));
            api.RegisterBlockEntityClass("BlockEntityCharger", typeof(BlockEntityTFCharger));
            api.RegisterBlockEntityClass("BlockEntityConnector", typeof(BlockEntityConnector));

            api.RegisterBlockEntityBehaviorClass("BEBehaviorTFRotaryGenerator", typeof(BEBehaviorTFRotaryGenerator));
            api.RegisterBlockEntityBehaviorClass("BEBehaviorWatermillRotor", typeof(BEBehaviorWatermillRotor));
            api.RegisterBlockEntityBehaviorClass("BEBehaviorTFEngine", typeof(BEBehaviorTFEngine));

            api.RegisterBlockBehaviorClass("OmniOrientable", typeof(BlockBehaviorOmniOrientable));

            api.RegisterItemClass("Wrench", typeof(Wrench));
            api.RegisterItemClass("TFChisel", typeof(TFChisel));
            api.RegisterItemClass("ItemWire", typeof(ItemWire));
        }
    }
}
