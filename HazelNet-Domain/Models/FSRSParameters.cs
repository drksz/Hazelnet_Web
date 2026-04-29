namespace HazelNet_Domain.Models;

public class FSRSParameters
{
    
    public double RequestRetention { get; set; }
    public double MaximumInterval { get; set; }
    public double[] W { get; set; }
    public double Decay { get; set; }
    public double Factor { get; set; }
    public bool EnableShortTerm { get; set; }
    public bool EnableFuzz { get; set; }
    internal string seed;

    private const double MinStability = 0.01;
    private const double MinRetrievability = 0.0001;

    public FSRSParameters()
    {
        double DR = 0.95;   //desired retention
        double decay = 0.0658;
        double factor = Math.Pow(DR, 1.0 / decay) - 1;
        RequestRetention = 0.9;
        MaximumInterval = 365;
        W = Weights.Default();
        Decay = decay;
        Factor = factor;
        EnableShortTerm = true;
        EnableFuzz = false;
        seed = "";
    }
}