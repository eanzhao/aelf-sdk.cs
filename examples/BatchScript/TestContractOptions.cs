namespace BatchScript;

public class TestContractOptions
{
    public string TestAddress { get; set; }
    public string TestContractPath { get; set; }
    public List<string> FromAccountList { get; set; }
    public List<string> ToAccountList { get; set; }
    public string InitAddress { get; set; }
    public int TestDuration { get; set; }
}