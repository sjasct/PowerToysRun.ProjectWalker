using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Community.PowerToys.Run.Plugin.PowerToysRun.OpenProject;

namespace Community.PowerToys.Run.Plugin.PowerToysRun.OpenProject.UnitTests
{
    [TestClass]
    public class MainTests
    {
        private Main main;

        [TestInitialize]
        public void TestInitialize()
        {
            main = new Main();
        }

        [TestMethod]
        public void Query_should_return_results()
        {
            //var results = main.Query(new("search"));

            //Assert.IsNotNull(results.First());
        }

        [TestMethod]
        public void LoadContextMenus_should_return_results()
        {
            //var results = main.LoadContextMenus(new Result { ContextData = "search" });

            //Assert.IsNotNull(results.First());
        }
    }
}