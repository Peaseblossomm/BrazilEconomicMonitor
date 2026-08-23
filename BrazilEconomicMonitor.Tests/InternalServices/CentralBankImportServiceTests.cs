using System;
using System.Collections.Generic;
using System.Text;

namespace BrazilEconomicMonitor.Tests.InternalServices
{
    public class CentralBankImportServiceTests
    {

        [Fact]
        public async Task ImportDataAsync_StoredToDb()
        {
            string fakeApiResponseBody = """
                 [{ "data":"01/01/2025","valor":"11843110.3"},{ "data":"01/02/2025","valor":"11935727.9"},
                
                { "data":"01/03/2025","valor":"12039140.8"},{ "data":"01/04/2025","valor":"12134427.2"},{ "data":"01/05/2025","valor":"12230341.5"},
                { "data":"01/06/2025","valor":"12304727.1"},{ "data":"01/07/2025","valor":"12380925.6"},{ "data":"01/08/2025","valor":"12443552.8"},
                { "data":"01/09/2025","valor":"12524498.7"},{ "data":"01/10/2025","valor":"12589491.8"},{ "data":"01/11/2025","valor":"12654854.6"},
                { "data":"01/12/2025","valor":"12738565.6"},{ "data":"01/01/2026","valor":"12808404.9"},{ "data":"01/02/2026","valor":"12863688.7"},
                { "data":"01/03/2026","valor":"12964821.6"},{ "data":"01/04/2026","valor":"13038321.0"},{ "data":"01/05/2026","valor":"13106029.2"},
                { "data":"01/06/2026","valor":"13192888.1"}]
             """;
        }
    }
}
