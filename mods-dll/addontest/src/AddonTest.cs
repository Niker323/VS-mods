using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace AddonTest
{
    public class AddonTestMod : ModSystem
    {

        public override void Start(ICoreAPI api)
        {
            base.Start(api);

            api.RegisterItemClass("TFDrill", typeof(TFDrill));
        }
    }
}
