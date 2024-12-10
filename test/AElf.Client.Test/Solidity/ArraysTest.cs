using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using AElf.Client.Core;
using AElf.Client.Core.Options;
using AElf.Client.Extensions;
using AElf.Client.Genesis;
using AElf.Client.Solidity;
using AElf.SolidityContract;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.Options;
using Scale;
using Shouldly;
using Solang;
using Xunit.Abstractions;
using BytesType = Scale.BytesType;

namespace AElf.Client.Test.Solidity;

public class ArraysTest : AElfClientAbpContractServiceTestBase
{
    private readonly IAElfClientService _aelfClientService;
    private readonly AElfClientConfigOptions _aelfClientConfigOptions;
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly IGenesisService _genesisService;
    private readonly IDeployContractService _deployService;
    private ISolidityContractService _solidityContractService;
    private SolangABI _solangAbi;
    private IAElfAccountProvider _accountProvider;

    private string testContract = "2LUmicHyH4RXrMjG4beDwuDsiWJESyLkgkwPdGTR8kahRzq5XS";
    private string TestContractPath = "contracts/arrays.contract";

    public ArraysTest(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _aelfClientService = GetRequiredService<IAElfClientService>();
        _aelfClientConfigOptions = GetRequiredService<IOptionsSnapshot<AElfClientConfigOptions>>().Value;
        _accountProvider = GetRequiredService<IAElfAccountProvider>();
        _genesisService = GetRequiredService<IGenesisService>();
        _deployService = GetRequiredService<IDeployContractService>();
    }


    [Fact]
    public async Task<Address> DeployContractTest()
    {
        var (wasmCode, solangAbi) = await LoadWasmContractCodeAsync(TestContractPath);
        _solangAbi = solangAbi;
        var input = new DeploySoliditySmartContractInput
        {
            Category = 1,
            Code = wasmCode.ToByteString(),
            Parameter = ByteString.Empty
        };
        var contractAddress = await _deployService.DeploySolidityContract(input);
        contractAddress.Value.ShouldNotBeEmpty();
        _testOutputHelper.WriteLine(contractAddress.ToBase58());
        var contractInfo = await _genesisService.GetContractInfo(contractAddress);
        contractInfo.Category.ShouldBe(1);
        contractInfo.IsSystemContract.ShouldBeFalse();
        contractInfo.Version.ShouldBe(1);
        _testOutputHelper.WriteLine(contractInfo.ContractVersion);

        return contractAddress;
    }

    [Fact]
    public async Task AddGetUserTest()
    {
        _solidityContractService =
            new SolidityContractService(_aelfClientService, Address.FromBase58(testContract), _aelfClientConfigOptions);
        var users = new List<User>();
        var registration =
            await _genesisService.GetSmartContractRegistrationByAddress(Address.FromBase58(testContract));

        for (ulong i = 0; i < 3; i++)
        {
            _testOutputHelper.WriteLine($"========{i}========");
            var addr = Address.FromPublicKey(_accountProvider.GenerateNewKeyPair().PublicKey).ToByteArray();
            var address = Address.FromBytes(addr).ToBase58();
            var name = $"name{i}";
            var id = GenerateRandomNumber();
            var randomPerm = 1; //new Random().Next(0, 6);

            var perms = new List<Permission>
            {
                (Permission)randomPerm,
                (Permission)(randomPerm + 1)
            };

            var permissions = perms.Select(EnumType<Permission>.From).ToArray();
            var txResult = await _solidityContractService.SendAsync("addUser", registration,
                TupleType<UInt64Type, BytesType, StringType, VecType<EnumType<Permission>>>
                    .GetByteStringFrom(
                        UInt64Type.From(id),
                        BytesType.From(addr),
                        //new AddressType(Address.FromBase58(address)),
                        StringType.From(name),
                        VecType<EnumType<Permission>>.From(permissions)
                    ));
            txResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            _testOutputHelper.WriteLine(txResult.TransactionResult.TransactionId.ToHex());

            _testOutputHelper.WriteLine("[Prints]");
            foreach (var print in txResult.TransactionResult.GetPrints())
            {
                _testOutputHelper.WriteLine(print);
            }

            _testOutputHelper.WriteLine("[Runtime logs]");
            foreach (var runtimeLog in txResult.TransactionResult.GetRuntimeLogs())
            {
                _testOutputHelper.WriteLine(runtimeLog);
            }

            _testOutputHelper.WriteLine("[Debug messages]");
            foreach (var debugMessage in txResult.TransactionResult.GetDebugMessages())
            {
                _testOutputHelper.WriteLine(debugMessage);
            }

            _testOutputHelper.WriteLine("[Error messages]");
            foreach (var errorMessage in txResult.TransactionResult.GetErrorMessages())
            {
                _testOutputHelper.WriteLine(errorMessage);
            }
        
            _testOutputHelper.WriteLine($"Charged gas fee: {txResult.TransactionResult.GetChargedGasFee()}");
            users.Add(new User(id, address, name, perms));
        }

        var firstUser = users.First();
        {
            var queriedUserByteString = await _solidityContractService.CallAsync("getUserById",
                registration, UInt64Type.GetByteStringFrom(firstUser.Id));
            var queriedUser =
                TupleType<StringType, AddressType, UInt64Type, VecType<EnumType<Permission>>>
                    .From(queriedUserByteString);
            StringType.From(queriedUser.Value[0].Encode()).ToString().ShouldBe(firstUser.Name);
            AddressType.From(queriedUser.Value[1].Encode()).Value.ToBase58().ShouldBe(firstUser.Address);
            UInt64Type.From(queriedUser.Value[2].Encode()).Value.ShouldBe(firstUser.Id);
            VecType<EnumType<Permission>>.From(queriedUser.Value[3].Encode()).Value.Select(p => p.Value)
                .ShouldBe(firstUser.Permissions);
        }

        // Test hasPermission method.
        {
            var hasPermission = await _solidityContractService.CallAsync("hasPermission",
                registration,TupleType<UInt64Type, EnumType<Permission>>.GetByteStringFrom(
                    UInt64Type.From(firstUser.Id),
                    EnumType<Permission>.From(firstUser.Permissions.First())
                ));
            BoolType.From(hasPermission).Value.ShouldBeTrue();
        }

        {
            var hasPermission = await _solidityContractService.CallAsync("hasPermission",
                registration, TupleType<UInt64Type, EnumType<Permission>>.GetByteStringFrom(
                    UInt64Type.From(firstUser.Id),
                    EnumType<Permission>.From(firstUser.Permissions.Last() + 1)
                ));
            BoolType.From(hasPermission).Value.ShouldBeFalse();
        }

        // Test getUserByAddress method.
        {
            var queriedUserByteString = await _solidityContractService.CallAsync( "getUserByAddress",
                registration, BytesType.GetByteStringFrom(Address.FromBase58(firstUser.Address).ToByteArray()));
            var queriedUser =
                TupleType<StringType, AddressType, UInt64Type, VecType<EnumType<Permission>>>
                    .From(queriedUserByteString);
            StringType.From(queriedUser.Value[0].Encode()).ToString().ShouldBe(firstUser.Name);
            AddressType.From(queriedUser.Value[1].Encode()).Value.ToBase58().ShouldBe(firstUser.Address);
            UInt64Type.From(queriedUser.Value[2].Encode()).Value.ShouldBe(firstUser.Id);
            VecType<EnumType<Permission>>.From(queriedUser.Value[3].Encode()).Value.Select(p => p.Value)
                .ShouldBe(firstUser.Permissions);
        }

        // Test removeUser method.
        {
            _testOutputHelper.WriteLine("======removeUser======");
            var txResult = await _solidityContractService.SendAsync( "removeUser", registration, UInt64Type.GetByteStringFrom(firstUser.Id));
            txResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            _testOutputHelper.WriteLine($"TxId: {txResult.TransactionResult.TransactionId}");
            _testOutputHelper.WriteLine($"Charged gas fee: {txResult.TransactionResult.GetChargedGasFee()}");

            _testOutputHelper.WriteLine("[Prints]");
            foreach (var print in txResult.TransactionResult.GetPrints())
            {
                _testOutputHelper.WriteLine(print);
            }

            _testOutputHelper.WriteLine("[Runtime logs]");
            foreach (var runtimeLog in txResult.TransactionResult.GetRuntimeLogs())
            {
                _testOutputHelper.WriteLine(runtimeLog);
            }

            _testOutputHelper.WriteLine("[Debug messages]");
            foreach (var debugMessage in txResult.TransactionResult.GetDebugMessages())
            {
                _testOutputHelper.WriteLine(debugMessage);
            }

            _testOutputHelper.WriteLine("[Error messages]");
            foreach (var errorMessage in txResult.TransactionResult.GetErrorMessages())
            {
                _testOutputHelper.WriteLine(errorMessage);
            }
            var isUserExists =
                await _solidityContractService.CallAsync( "userExists", registration, UInt64Type.GetByteStringFrom(firstUser.Id));
            BoolType.From(isUserExists).Value.ShouldBeFalse();
        }
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
                         (uint)(randomBytes[2] << 8) |
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

public enum Permission
{
    Perm1,
    Perm2,
    Perm3,
    Perm4,
    Perm5,
    Perm6,
    Perm7,
    Perm8
}

public record User(ulong Id, string Address, string Name, List<Permission> Permissions);

// public class User {
//     public string name;
//     public byte[] addr;
//     public ulong id;
//     public List<Permission> perms;
//     public Permission p { get; set; }
//
//     public byte[] Encode()
//     {
//         // Encoding name length and content to byte array
//         var nameBytes = StringType.GetBytesFrom(name);
//         var idBytes = UInt64Type.GetBytesFrom(id);
//
//         // Encoding permissions
//         var permsBytes = perms.SelectMany(perm => EnumType<Permission>.GetBytesFrom(p));
//         
//         // Concatenating all byte arrays
//         return nameBytes.Concat(idBytes)
//             .Concat(addr)
//             .Concat(permsBytes)
//             .ToArray();
//     }
//
//     public User Decode(byte[] data)
//     {
//         int offset = 0;
//
//         // Decoding name
//         ushort nameLength = BitConverter.ToUInt16(data, offset);
//         offset += 2;
//         string name = System.Text.Encoding.UTF8.GetString(data, offset, nameLength);
//         offset += nameLength;
//
//         // Decoding address
//         ushort addrLength = BitConverter.ToUInt16(data, offset);
//         offset += 2;
//         byte[] addr = new byte[addrLength];
//         Array.Copy(data, offset, addr, 0, addrLength);
//         offset += addrLength;
//
//         // Decoding id
//         ulong id = BitConverter.ToUInt64(data, offset);
//         offset += 8;
//
//         // Decoding list of permissions
//         ushort permsCount = BitConverter.ToUInt16(data, offset);
//         offset += 2;
//         List<Permission> perms = new List<Permission>();
//         for (int i = 0; i < permsCount; i++)
//         {
//             var perm = new EnumType<Permission>();
//             perm.Create(data.Skip(offset).Take(4).ToArray());
//             perms.Add(perm);
//         }
//         
//         return new User { name = name, addr = addr, id = id, perms = perms};
//     }
// }