using System;
using System.Text;
using System.Threading.Tasks;
using AElf.Client.Core;
using AElf.Client.Genesis;
using AElf.Client.Solidity;
using AElf.Types;
using Scale;
using Scale.Decoders;
using Shouldly;
using Solang;
using Xunit.Abstractions;
using AElf.Client.Test.contract;

namespace AElf.Client.Test.Solidity;

public class StroeTest : AElfClientAbpContractServiceTestBase
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly IDeployContractService _deployService;
    private readonly IGenesisService _genesisService;
    private ISolidityContractService _solidityContractService;
    private SolangABI _solangAbi;
    private IAElfAccountProvider _accountProvider;

    private IstoreStub _storeStub;

    public StroeTest(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _accountProvider = GetRequiredService<IAElfAccountProvider>();
        _deployService = GetRequiredService<IDeployContractService>();
        _genesisService = GetRequiredService<IGenesisService>();
        _storeStub = GetRequiredService<IstoreStub>();
    }

    [Fact]
    public async Task StoreFeatureTest()
    {
        var contractAddress = await _storeStub.DeployAsync();
        _testOutputHelper.WriteLine($"Test contract address: {contractAddress.ToBase58()}");

        var values1 = await _storeStub.Get_values1Async();
        new IntegerTypeDecoder().Decode(values1).ShouldBe(0); //?

        var value2 = await _storeStub.Get_values2Async();
        // new IntegerTypeDecoder().Decode(value2).ShouldBe(0); //
        //14942402390684785610213190996650691026924320579097246296716362593547306426518471394197504
        
        var setValue = await _storeStub.Set_valuesAsync();
        _testOutputHelper.WriteLine($"Set tx: {setValue.TransactionResult.TransactionId}");
        setValue.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

        values1 = await _storeStub.Get_values1Async();
        // new IntegerTypeDecoder().Decode(values1).ShouldBe(0); 
        //300613450595050653169853516389035139504087366260264943450533244356122755214667284524188271970745747983352987647

        value2 = await _storeStub.Get_values2Async();
        // new IntegerTypeDecoder().Decode(value2).ShouldBe(0); //?
        //6354101099405002486023057383624521093453298382412148916522717104634330175961188991926493760765674176993822910529798813195197050404949252144472627248940461068478136184535954268214385512452093080080845111398
        
        var doOps = await _storeStub.Do_opsAsync();
        _testOutputHelper.WriteLine($"Do_opsAsync tx: {doOps.TransactionResult.TransactionId}");
        doOps.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

        var pushZero = await _storeStub.Push_zeroAsync();
        _testOutputHelper.WriteLine($"Push_zeroAsync tx: {pushZero.TransactionResult.TransactionId}");
        pushZero.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

        string bs = "0xb0ff1e00";
        Random random = new Random(); // 创建一个 Random 类的实例

        for (int i = 0; i < 20; i++)
        {
            byte[] res7 = await _storeStub.Get_bsAsync();
            // Encoding.UTF8.GetString(res7).ShouldBe(bs);
            // res7.ToHex().ShouldBe(bs); //14b0ff1e0000

            if (bs.Length <= 4 || random.NextDouble() >= 0.5)
            {
                // 生成一个0到255之间的随机值
                int valInt = random.Next(256);
                string val = valInt.ToString("x2"); 
                var result = await _storeStub.PushAsync(StringType.GetByteStringFrom(val));
                _testOutputHelper.WriteLine($"result tx: {result.TransactionResult.TransactionId}");
                result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

                bs += val;
            }
            else
            {
                var result = await _storeStub.PopAsync();
                _testOutputHelper.WriteLine($"result tx: {result.TransactionResult.TransactionId}");
                result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

                bs = bs.Substring(0, bs.Length - 2);
            }
        }
    }

}