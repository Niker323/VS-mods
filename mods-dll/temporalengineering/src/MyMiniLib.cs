
using Vintagestory.API.Common;

public static class MyMiniLib
{
    public static int GetAttributeInt(Block block, string attrname, int def = 0)
    {
        if (block != null && block.Attributes != null && block.Attributes[attrname] != null)
        {
            return block.Attributes[attrname].AsInt(def);
        }
        return def;
    }
}