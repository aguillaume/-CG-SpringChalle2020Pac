using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace Tests
{
    [TestClass()]
    public class PacTests
    {
        [TestMethod()]
        public void ToStringTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void EqualsTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void EqualsTest1()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void GetHashCodeTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void DoubleForwardTest()
        {
            Pac pac = new Pac();
            pac.Pos = new Pos(16, 1);
            pac.LastMove = new Pos(14, 1);
            pac.LastMove = new Pos(15, 1);
            pac.LastMove = new Pos(16, 1);
            pac.LastMove = new Pos(16, 1);

            var df = pac.DoubleForward;
            Assert.Fail();
        }
    }
}