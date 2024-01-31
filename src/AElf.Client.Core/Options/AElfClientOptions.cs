namespace AElf.Client.Core.Options;

public class AElfClientOptions
{
    public List<ClientConfig> ClientConfigList { get; set; } = new();
}

public class ClientConfig
{
    public string Alias { get; set; }
    public string Endpoint { get; set; }
    public string? UserName { get; set; } = "root";
    public string? Password { get; set; } = "abc";
    public int Timeout { get; set; } = 60;
}