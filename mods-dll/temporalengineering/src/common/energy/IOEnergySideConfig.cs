public class IOEnergySideConfig
{
	public static readonly IOEnergySideConfig NONE = new IOEnergySideConfig();
	public static readonly IOEnergySideConfig INPUT = new IOEnergySideConfig();
    public static readonly IOEnergySideConfig OUTPUT = new IOEnergySideConfig();
    //public static readonly IOEnergySideConfig BOTH = new IOEnergySideConfig();

	public static readonly IOEnergySideConfig[] VALUES = { NONE, INPUT, OUTPUT };//, BOTH

    public string toString()
    {
        if (this == INPUT) return "input";
        if (this == OUTPUT) return "output";
        //if (this == BOTH) return "both";
        return "none";
    }

    public int toInt()
    {
        if (this == INPUT) return 1;
        if (this == OUTPUT) return 2;
        //if (this == BOTH) return 3;
        return 0;
    }

    public IOEnergySideConfig next()
	{
        if (this == NONE) return INPUT;
		if (this == INPUT) return OUTPUT;
        //if (this == OUTPUT) return BOTH;
		return NONE;
	}
}