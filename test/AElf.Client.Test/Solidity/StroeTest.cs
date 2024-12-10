using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AElf.Client.Core;
using AElf.Client.Core.Options;
using AElf.Client.Genesis;
using AElf.Client.Solidity;
using AElf.SolidityContract;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.Options;
using Nethereum.ABI;
using Nethereum.Hex.HexConvertors.Extensions;
using Scale.Decoders;
using Shouldly;
using Solang;
using Xunit.Abstractions;
using StringType = Scale.StringType;

namespace AElf.Client.Test.Solidity;

public class StroeTest : AElfClientAbpContractServiceTestBase
{
    private readonly IAElfClientService _aelfClientService;
    private readonly AElfClientConfigOptions _aelfClientConfigOptions;
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly IGenesisService _genesisService;
    private readonly IDeployContractService _deployService;
    private ISolidityContractService _solidityContractService;

    private string testContract = "2VTusxv6BN4SQDroitnWyLyQHWiwEhdWU76PPiGBqt5VbyF27J";
    private string TestContractPath = "contracts/store.contract";

    public StroeTest(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _aelfClientService = GetRequiredService<IAElfClientService>();
        _aelfClientConfigOptions = GetRequiredService<IOptionsSnapshot<AElfClientConfigOptions>>().Value;
        _genesisService = GetRequiredService<IGenesisService>();
        _deployService = GetRequiredService<IDeployContractService>();
    }

    [Fact]
    public async Task<Address> DeployContractTest()
    {
        var (wasmCode, solangAbi) = await LoadWasmContractCodeAsync(TestContractPath);
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
    public async Task SetValuesTest()
    {
        _solidityContractService =
            new SolidityContractService(_aelfClientService, Address.FromBase58(testContract), _aelfClientConfigOptions);
        var registration =
            await _genesisService.GetSmartContractRegistrationByAddress(Address.FromBase58(testContract));
        var txResult = await _solidityContractService.SendAsync("set_values", registration);
        txResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
    }
    
    [Fact]
    public async Task StoreFeatureTest()
    {
        _solidityContractService =
            new SolidityContractService(_aelfClientService, Address.FromBase58(testContract), _aelfClientConfigOptions);
        var registration =
            await _genesisService.GetSmartContractRegistrationByAddress(Address.FromBase58(testContract));
        //
        // var values1 = await _solidityContractService.CallAsync("get_values1", registration);
        // new IntegerTypeDecoder().Decode(values1).ShouldBe(0); //?
        //
        //
        // var value2 = await _solidityContractService.CallAsync("get_values2", registration);
        // new IntegerTypeDecoder().Decode(value2).ShouldBe(0); //
        // //14942402390684785610213190996650691026924320579097246296716362593547306426518471394197504

        await SetValuesTest();
        {
            var values1 =  await _solidityContractService.CallAsync("get_values1", registration);
            // new IntegerTypeDecoder().Decode(values1).ShouldBe(0); 
            //300613450595050653169853516389035139504087366260264943450533244356122755214667284524188271970745747983352987647
            var hexReturn = values1.ToHex();
            hexReturn.ShouldContain(ulong.MaxValue.ToBytes().ToHex());
            // u32 = 0xdad0feef;
            hexReturn.ShouldContain("dad0feef");
            // i16 = 0x7ffe;
            hexReturn.ShouldContain("7ffe");
            // i256 = type(int256).max;
            hexReturn.ShouldContain(IntType.MAX_INT256_VALUE.ToHex(false).RemoveHexPrefix());

        }
        {
            var value2 =  await _solidityContractService.CallAsync("get_values2", registration);
            // new IntegerTypeDecoder().Decode(value2).ShouldBe(0); //?
            //6354101099405002486023057383624521093453298382412148916522717104634330175961188991926493760765674176993822910529798813195197050404949252144472627248940461068478136184535954268214385512452093080080845111398
            var hexReturn = value2.ToHex();
            // u256 = 102;
            hexReturn.ShouldContain(102.ToBytes().ToHex());
            // str = "the course of true love never did run smooth";
            hexReturn.ShouldContain("the course of true love never did run smooth".GetBytes().Reverse().ToArray()
                .ToHex());
            // bytes bs = hex"b00b1e";
            hexReturn.ShouldContain("b00b1e".HexToByteArray().Reverse().ToArray().ToHex());
            // fixedbytes = "ABCD";
            hexReturn.ShouldContain("ABCD".GetBytes().Reverse().ToArray().ToHex());
            // bar = enum_bar.bar2;
            hexReturn.ShouldContain("01");
        }
        
        
        var doOps = await _solidityContractService.SendAsync("do_ops", registration);
        _testOutputHelper.WriteLine($"Do_opsAsync tx: {doOps.TransactionResult.TransactionId}");
        doOps.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

        var pushZero = await _solidityContractService.SendAsync("push_zero",registration);
        _testOutputHelper.WriteLine($"Push_zeroAsync tx: {pushZero.TransactionResult.TransactionId}");
        pushZero.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

        string bs = "0xb0ff1e00";
        Random random = new Random(); // 创建一个 Random 类的实例

        for (int i = 0; i < 20; i++)
        {
            byte[] res7 = await _solidityContractService.CallAsync("get_bs", registration);
            Encoding.UTF8.GetString(res7).ShouldBe(bs);
            res7.ToHex().ShouldBe(bs); //14b0ff1e0000

            if (bs.Length <= 4 || random.NextDouble() >= 0.5)
            {
                // 生成一个0到255之间的随机值
                int valInt = random.Next(256);
                string val = valInt.ToString("x2");
                var result =
                    await _solidityContractService.SendAsync("push", registration, StringType.GetByteStringFrom(val));
                _testOutputHelper.WriteLine($"result tx: {result.TransactionResult.TransactionId}");
                result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

                bs += val;
            }
            else
            {
                var result = await _solidityContractService.SendAsync("pop", registration);
                _testOutputHelper.WriteLine($"result tx: {result.TransactionResult.TransactionId}");
                result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

                bs = bs.Substring(0, bs.Length - 2);
            }
        }
    }
}