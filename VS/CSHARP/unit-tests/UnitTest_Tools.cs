﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using AsmTools;

namespace unit_tests {

    [TestClass]
    public class UnitTest_Tools {
        [TestMethod]
        public void Test_toConstant() {
            {
                ulong i = 0ul;
                string s = i + "";
                Tuple<bool, ulong, int> t = AsmTools.Tools.toConstant(s);
                Assert.IsTrue(t.Item1);
                Assert.AreEqual(i, t.Item2, s);
                Assert.AreEqual(8, t.Item3, s);
            }
            {
                ulong i = 0ul;
                string s = "0x"+i.ToString("X");
                Tuple<bool, ulong, int> t = AsmTools.Tools.toConstant(s);
                Assert.IsTrue(t.Item1);
                Assert.AreEqual(i, t.Item2, s);
                Assert.AreEqual(8, t.Item3, s);
            }
            {
                ulong i = 0ul;
                string s = i.ToString("X")+"h";
                Tuple<bool, ulong, int> t = AsmTools.Tools.toConstant(s);
                Assert.IsTrue(t.Item1);
                Assert.AreEqual(i, t.Item2, s);
                Assert.AreEqual(8, t.Item3, s);
            }
            {
                ulong i = 1ul;
                string s = i.ToString("X") + "h";
                Tuple<bool, ulong, int> t = AsmTools.Tools.toConstant(s);
                Assert.IsTrue(t.Item1);
                Assert.AreEqual(i, t.Item2, s);
                Assert.AreEqual(8, t.Item3, s);
            }
            {
                ulong i = 1ul;
                string s = "0x"+i.ToString("X"); ;
                Tuple<bool, ulong, int> t = AsmTools.Tools.toConstant(s);
                Assert.IsTrue(t.Item1);
                Assert.AreEqual(i, t.Item2, s);
                Assert.AreEqual(8, t.Item3, s);
            }
            {
                ulong i = 1ul;
                string s = i.ToString("X") +"h";
                Tuple<bool, ulong, int> t = AsmTools.Tools.toConstant(s);
                Assert.IsTrue(t.Item1);
                Assert.AreEqual(i, t.Item2, s);
                Assert.AreEqual(8, t.Item3, s);
            }

            {
                ulong i = 0xFFul;
                string s = "0x"+i.ToString("X"); ;
                Tuple<bool, ulong, int> t = AsmTools.Tools.toConstant(s);
                Assert.IsTrue(t.Item1);
                Assert.AreEqual(i, t.Item2, s);
                Assert.AreEqual(8, t.Item3, s);
            }
            {
                ulong i = 0xFFul;
                string s = i.ToString("X") +"h";
                Tuple<bool, ulong, int> t = AsmTools.Tools.toConstant(s);
                Assert.IsTrue(t.Item1);
                Assert.AreEqual(i, t.Item2, s);
                Assert.AreEqual(8, t.Item3, s);
            }
            {
                ulong i = 0x100ul;
                string s = "0x" + i.ToString("X");
                Tuple<bool, ulong, int> t = AsmTools.Tools.toConstant(s);
                Assert.IsTrue(t.Item1);
                Assert.AreEqual(i, t.Item2, s);
                Assert.AreEqual(16, t.Item3, s);
            }
            {
                ulong i = 0xFFFFul;
                string s = "0x" + i.ToString("X");
                Tuple<bool, ulong, int> t = AsmTools.Tools.toConstant(s);
                Assert.IsTrue(t.Item1);
                Assert.AreEqual(i, t.Item2, s);
                Assert.AreEqual(16, t.Item3, s);
            }
            {
                ulong i = 0x10000ul;
                string s = "0x" + i.ToString("X");
                Tuple<bool, ulong, int> t = AsmTools.Tools.toConstant(s);
                Assert.IsTrue(t.Item1);
                Assert.AreEqual(i, t.Item2, s);
                Assert.AreEqual(32, t.Item3, s);
            }
            {
                ulong i = 0xFFFFFFFFul;
                string s = "0x" + i.ToString("X");
                Tuple<bool, ulong, int> t = AsmTools.Tools.toConstant(s);
                Assert.IsTrue(t.Item1);
                Assert.AreEqual(i, t.Item2, s);
                Assert.AreEqual(32, t.Item3, s);
            }
            {
                ulong i = 0x100000000ul;
                string s = "0x" + i.ToString("X");
                Tuple<bool, ulong, int> t = AsmTools.Tools.toConstant(s);
                Assert.IsTrue(t.Item1);
                Assert.AreEqual(i, t.Item2, s);
                Assert.AreEqual(64, t.Item3, s);
            }
            {
                ulong i = 0xFFFFFFFFFFFFFFFFul;
                string s = "0x" + i.ToString("X");
                Tuple<bool, ulong, int> t = AsmTools.Tools.toConstant(s);
                Assert.IsTrue(t.Item1);
                Assert.AreEqual(i, t.Item2, s);
                Assert.AreEqual(64, t.Item3, s);
            }
        }
        [TestMethod]
        public void Test_parseMnemonic() {
            foreach (Mnemonic x in Enum.GetValues(typeof(Mnemonic))) {
                Assert.AreEqual(AsmTools.Tools.parseMnemonic(x.ToString()), x, 
                    "Parsing string "+x.ToString() + " does not yield the same enumeration.");
            }
        }
        [TestMethod]
        public void Test_parseArch() {
            foreach (Arch x in Enum.GetValues(typeof(Arch))) {
                Assert.AreEqual(AsmTools.Tools.parseArch(x.ToString()), x,
                    "Parsing string " + x.ToString() + " does not yield the same enumeration.");
            }
        }
        [TestMethod]
        public void Test_OperandType() {
            foreach (Ot x1 in Enum.GetValues(typeof(Ot))) {
                foreach (Ot x2 in Enum.GetValues(typeof(Ot))) {
                    Tuple<Ot, Ot> t = Tools.splitOt(Tools.mergeOt(x1, x2));
                    Assert.AreEqual(t.Item1, x1, "");
                    Assert.AreEqual(t.Item2, x2, "");
                }
            }
        }
        [TestMethod]
        public void Test_and() {
            {
                Bt[] a = new Bt[] { Bt.ONE, Bt.ZERO, Bt.UNDEFINED, Bt.KNOWN };
                Bt[] b = new Bt[] { Bt.ONE, Bt.ONE, Bt.KNOWN, Bt.ONE };
                Bt[] expected = new Bt[] { Bt.ONE, Bt.ZERO, Bt.UNDEFINED, Bt.KNOWN };
                Bt[] actual = AsmTools.BitTools.and(a, b);
                for (int i = 0; i<a.Length; ++i) {
                    Assert.AreEqual(expected[i], actual[i], "i="+i);
                }
            }
        }
    }
}
