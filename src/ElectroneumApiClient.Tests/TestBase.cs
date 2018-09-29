using Xunit.Abstractions;

namespace ElectroneumApiClient.Tests
{
    public abstract class TestBase
    {
        protected readonly ITestOutputHelper _out;
        protected readonly EtnTestMockFactory _mock;

        protected TestBase(ITestOutputHelper output)
        {
            _out = output;
            _mock = new EtnTestMockFactory();
        }
    }
}