namespace PV_analysis
{
    internal interface IComponentDesignData
    {
        double PowerLoss { get; }
        double Volume { get; }
        double Cost { get; }
        string[] Configs { get; }
    }
}
