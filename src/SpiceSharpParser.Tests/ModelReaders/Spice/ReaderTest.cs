using SpiceSharpParser.Common;
using SpiceSharpParser.Models.Netlist.Spice;
using Xunit;
using SpiceSharpParser.ModelReaders.Netlist.Spice;

namespace SpiceSharpParser.Tests.ModelReaders.Spice
{
    public class ReaderTest
    {
        [Fact]
        public void ReadTest()
        {
            // arrange
            var reader = new SpiceNetlistReader(new SpiceNetlistReaderSettings(new SpiceNetlistCaseSensitivitySettings()));
            var netlist = new SpiceNetlist();

            // act
            var result = reader.Read(netlist);

            // assert
            Assert.NotNull(result);
        }
    }
}
