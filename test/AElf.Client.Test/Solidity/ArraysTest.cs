using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using AElf.Client.Core;
using AElf.Client.Genesis;
using AElf.Client.Solidity;
using AElf.Client.Test.contract;
using AElf.Types;
using Scale;
using Shouldly;
using Solang;
using Xunit.Abstractions;
using BytesType = Scale.BytesType;

namespace AElf.Client.Test.Solidity;

public class ArraysTest : AElfClientAbpContractServiceTestBase
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly IGenesisService _genesisService;
    private ISolidityContractService _solidityContractService;
    private SolangABI _solangAbi;
    private IAElfAccountProvider _accountProvider;

    private IarraysStub _arraysStub;
    private string testContract = "2nyC8hqq3pGnRu8gJzCsTaxXB6snfGxmL2viimKXgEfYWGtjEh";

    public ArraysTest(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _accountProvider = GetRequiredService<IAElfAccountProvider>();
        _genesisService = GetRequiredService<IGenesisService>();
        _arraysStub = GetRequiredService<IarraysStub>();
    }
    
    [Fact]
    public async Task DeployContract()
    {
        var contractAddress = await _arraysStub.DeployAsync();
        _testOutputHelper.WriteLine($"Test contract address: {contractAddress.ToBase58()}");
        
        var contractInfo = await _genesisService.GetContractInfo(contractAddress); 
        contractInfo.Category.ShouldBe(1);
        contractInfo.IsSystemContract.ShouldBeFalse();
        contractInfo.Version.ShouldBe(1);
        _testOutputHelper.WriteLine(contractInfo.ContractVersion);
    }
    
    [Fact]
    public async Task AddGetUserTest()
    {
        _arraysStub.SetContractAddressToStub(Address.FromBase58(testContract));

        var users = new List<User>();
        
        for (ulong i = 0; i < 3; i++)
        {
            var addr = Address.FromPublicKey(_accountProvider.GenerateNewKeyPair().PublicKey).ToByteArray();
            var name = $"name{i}";
            var id = GenerateRandomNumber();
            var perms = new List<Permission>();

            for (var j = 0; j < GenerateRandomNumber(1,8); j++)
            {
                var p = GenerateRandomNumber(0, 7);

                perms.Add((Permission)p);
            }

            var result = await _arraysStub.AddUserAsync();
            _testOutputHelper.WriteLine($"AddUser tx: {result.TransactionResult.TransactionId}");
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            users.Add(new User
            {
                addr = addr,
                name = name,
                id = id,
                perms = perms
            });
        }
        
        var userInfo = await _arraysStub.GetUserByIdAsync(UInt64Type.From(users.First().id));
        var decodeUser = new User().Decode(userInfo);
        var userInfoByAddress = await _arraysStub.GetUserByAddressAsync(BytesType.GetByteStringFrom(users.First().addr));
    }
    

    private ulong GenerateRandomNumber()
    {
        using var rng = new RNGCryptoServiceProvider();
        // Create a byte array to receive the random bytes.
        byte[] randomBytes = new byte[4];
            
        // Fill the array with cryptographically secure random bytes.
        rng.GetBytes(randomBytes);
            
        // Convert the bytes to a 32-bit unsigned integer in big-endian order.
        uint randomInt = (uint)(randomBytes[0] << 24) | 
                         (uint)(randomBytes[1] << 16) | 
                         (uint)(randomBytes[2] << 8)  | 
                         (uint)(randomBytes[3]);

        // Perform the modulo operation to limit the range from 0 to 1023.
        return randomInt % 1024;
    }
    
    private int GenerateRandomNumber(int min, int max)
    {
        var rd = new Random(Guid.NewGuid().GetHashCode());
        var random = rd.Next(min, max);
        return random;
    }
}

public enum Permission {
    Perm1, Perm2, Perm3, Perm4, Perm5, Perm6, Perm7, Perm8
}

public class User {
    public string name;
    public byte[] addr;
    public ulong id;
    public List<Permission> perms;
    public Permission p { get; set; }

    public byte[] Encode()
    {
        // Encoding name length and content to byte array
        var nameBytes = StringType.GetBytesFrom(name);
        var idBytes = UInt64Type.GetBytesFrom(id);

        // Encoding permissions
        var permsBytes = perms.SelectMany(perm => EnumType<Permission>.GetBytesFrom(p));
        
        // Concatenating all byte arrays
        return nameBytes.Concat(idBytes)
            .Concat(addr)
            .Concat(permsBytes)
            .ToArray();
    }

    public User Decode(byte[] data)
    {
        int offset = 0;

        // Decoding name
        ushort nameLength = BitConverter.ToUInt16(data, offset);
        offset += 2;
        string name = System.Text.Encoding.UTF8.GetString(data, offset, nameLength);
        offset += nameLength;

        // Decoding address
        ushort addrLength = BitConverter.ToUInt16(data, offset);
        offset += 2;
        byte[] addr = new byte[addrLength];
        Array.Copy(data, offset, addr, 0, addrLength);
        offset += addrLength;

        // Decoding id
        ulong id = BitConverter.ToUInt64(data, offset);
        offset += 8;

        // Decoding list of permissions
        ushort permsCount = BitConverter.ToUInt16(data, offset);
        offset += 2;
        List<Permission> perms = new List<Permission>();
        for (int i = 0; i < permsCount; i++)
        {
            var perm = new EnumType<Permission>();
            perm.Create(data.Skip(offset).Take(4).ToArray());
            perms.Add(perm);
        }
        
        return new User { name = name, addr = addr, id = id, perms = perms};
    }
}


