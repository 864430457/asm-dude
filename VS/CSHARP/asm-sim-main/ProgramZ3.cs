﻿// The MIT License (MIT)
//
// Copyright (c) 2017 Henk-Jan Lebbink
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.Collections.Generic;
using AsmTools;
using System.Reflection;
using System.Linq;
using Microsoft.Z3;
using QuickGraph;

namespace AsmSim
{
    class AsmSimMain
    {
        [STAThread]
        static void Main(string[] args)
        {
            DateTime startTime = DateTime.Now;
            Assembly thisAssem = typeof(AsmSimMain).Assembly;
            AssemblyName thisAssemName = thisAssem.GetName();
            System.Version ver = thisAssemName.Version;
            Console.WriteLine(string.Format("Loaded AsmSim version {0}.", ver));

            //TestGraph();
            //TestMnemonic();
            //TestMemorySpeed();
            TestDynamicFlow();
            //EmptyMemoryTest();
            //ProgramSynthesis1();
            
            //TestFunctions();
            //TacticTest();
                        
            double elapsedSec = (double)(DateTime.Now.Ticks - startTime.Ticks) / 10000000;
            Console.WriteLine(string.Format("Elapsed time " + elapsedSec + " sec"));
            Console.WriteLine(string.Format("Press any key to continue."));
            Console.ReadKey();
        }

        static void TestMemorySpeed()
        {
            Dictionary<string, string> settings = new Dictionary<string, string>
            {
                { "unsat-core", "false" },    // enable generation of unsat cores
                { "model", "false" },         // enable model generation
                { "proof", "false" },         // enable proof generation
                { "timeout", "60000" }        // 60000=1min
            };
            Context ctx = new Context(settings);

            /*
             * 
            string line1 = "mov qword ptr [rax], rbx";
            string line2 = "mov rcx, qword ptr [rax]";
             * 
            (declare-const RAX!0 (_ BitVec 64))
            (declare-const RAX!1 (_ BitVec 64))
            (declare-const RAX!2 (_ BitVec 64))

            (declare-const RBX!0 (_ BitVec 64))
            (declare-const RBX!1 (_ BitVec 64))
            (declare-const RBX!2 (_ BitVec 64))

            (declare-const RCX!0 (_ BitVec 64))
            (declare-const RCX!1 (_ BitVec 64))
            (declare-const RCX!2 (_ BitVec 64))

            (declare-fun MEM!0 () (Array (_ BitVec 64) (_ BitVec 8)))
            (declare-fun MEM!1 () (Array (_ BitVec 64) (_ BitVec 8)))
            (declare-fun MEM!2 () (Array (_ BitVec 64) (_ BitVec 8)))

            (assert (= RAX!1 RAX!0))
            (assert (= RBX!1 RBX!0))
            (assert (= RCX!1 RCX!0))
            (assert (let ((a!1 (store (store (store MEM!0 RAX!0 ((_ extract 7 0) RBX!0)) (bvadd #x0000000000000001 RAX!0) ((_ extract 15 8) RBX!0)) (bvadd #x0000000000000002 RAX!0) ((_ extract 23 16) RBX!0)))) (let ((a!2 (store (store (store a!1 (bvadd #x0000000000000003 RAX!0) ((_ extract 31 24) RBX!0)) (bvadd #x0000000000000004 RAX!0) ((_ extract 39 32) RBX!0)) (bvadd #x0000000000000005 RAX!0) ((_ extract 47 40) RBX!0)))) (= MEM!1 (store (store a!2 (bvadd #x0000000000000006 RAX!0) ((_ extract 55 48) RBX!0)) (bvadd #x0000000000000007 RAX!0) ((_ extract 63 56) RBX!0))))))

            (assert (= RAX!2 RAX!1))
            (assert (= RBX!2 RBX!1))
            (assert (let ((a!1 (concat (select MEM!1 (bvadd RAX!1 #x0000000000000002)) (concat (select MEM!1 (bvadd RAX!1 #x0000000000000001)) (select MEM!1 RAX!1))))) (let ((a!2 (concat (select MEM!1 (bvadd RAX!1 #x0000000000000004)) (concat (select MEM!1 (bvadd RAX!1 #x0000000000000003)) a!1)))) (let ((a!3 (concat (select MEM!1 (bvadd RAX!1 #x0000000000000006)) (concat (select MEM!1 (bvadd RAX!1 #x0000000000000005)) a!2)))) (= RCX!2 (concat (select MEM!1 (bvadd RAX!1 #x0000000000000007)) a!3))))))
            (assert (= MEM!2 MEM!1))
             */
            
            //Solver solver = ctx.MkSolver("QF_ABV");
            //Solver solver = ctx.MkSolver("QF_BV");
            Solver solver = ctx.MkSolver(ctx.MkTactic("qfbv"));
            //Solver solver = ctx.MkSolver();

            BitVecExpr rax0 = ctx.MkBVConst("RAX!0", 64);
            BitVecExpr rax1 = ctx.MkBVConst("RAX!1", 64);
            BitVecExpr rax2 = ctx.MkBVConst("RAX!2", 64);

            BitVecExpr rbx0 = ctx.MkBVConst("RBX!0", 64);
            BitVecExpr rbx1 = ctx.MkBVConst("RBX!1", 64);
            BitVecExpr rbx2 = ctx.MkBVConst("RBX!2", 64);

            BitVecExpr rcx0 = ctx.MkBVConst("RCX!0", 64);
            BitVecExpr rcx1 = ctx.MkBVConst("RCX!1", 64);
            BitVecExpr rcx2 = ctx.MkBVConst("RCX!2", 64);

            ArrayExpr mem0 = ctx.MkArrayConst("MEM!0", ctx.MkBitVecSort(64), ctx.MkBitVecSort(8));
            ArrayExpr mem1 = ctx.MkArrayConst("MEM!1", ctx.MkBitVecSort(64), ctx.MkBitVecSort(8));
            ArrayExpr mem2 = ctx.MkArrayConst("MEM!2", ctx.MkBitVecSort(64), ctx.MkBitVecSort(8));

            if (true)
            {
                #region mov qword ptr [rax], rbx

                solver.Assert(ctx.MkEq(rax1, rax0));
                solver.Assert(ctx.MkEq(rbx1, rbx0));
                solver.Assert(ctx.MkEq(rcx1, rcx0));
                ArrayExpr memX0 = ctx.MkStore(mem0, ctx.MkBVAdd(ctx.MkBV(0, 64), rax0), ctx.MkExtract((1 * 8) - 1, 0 * 8, rbx0));
                ArrayExpr memX1 = ctx.MkStore(memX0, ctx.MkBVAdd(ctx.MkBV(1, 64), rax0), ctx.MkExtract((2 * 8) - 1, 1 * 8, rbx0));
                ArrayExpr memX2 = ctx.MkStore(memX1, ctx.MkBVAdd(ctx.MkBV(2, 64), rax0), ctx.MkExtract((3 * 8) - 1, 2 * 8, rbx0));
                ArrayExpr memX3 = ctx.MkStore(memX2, ctx.MkBVAdd(ctx.MkBV(3, 64), rax0), ctx.MkExtract((4 * 8) - 1, 3 * 8, rbx0));
                ArrayExpr memX4 = ctx.MkStore(memX3, ctx.MkBVAdd(ctx.MkBV(4, 64), rax0), ctx.MkExtract((5 * 8) - 1, 4 * 8, rbx0));
                ArrayExpr memX5 = ctx.MkStore(memX4, ctx.MkBVAdd(ctx.MkBV(5, 64), rax0), ctx.MkExtract((6 * 8) - 1, 5 * 8, rbx0));
                ArrayExpr memX6 = ctx.MkStore(memX5, ctx.MkBVAdd(ctx.MkBV(6, 64), rax0), ctx.MkExtract((7 * 8) - 1, 6 * 8, rbx0));
                ArrayExpr memX7 = ctx.MkStore(memX6, ctx.MkBVAdd(ctx.MkBV(7, 64), rax0), ctx.MkExtract((8 * 8) - 1, 7 * 8, rbx0));
                solver.Assert(ctx.MkEq(mem1, memX7).Simplify() as BoolExpr);

                #endregion
                #region mov rcx, qword ptr [rax]
                solver.Assert(ctx.MkEq(rax2, rax1));
                solver.Assert(ctx.MkEq(rbx2, rbx1));
                BitVecExpr y0 = ctx.MkSelect(mem1, ctx.MkBVAdd(ctx.MkBV(0, 64), rax1)) as BitVecExpr;
                BitVecExpr y1 = ctx.MkSelect(mem1, ctx.MkBVAdd(ctx.MkBV(1, 64), rax1)) as BitVecExpr;
                BitVecExpr y2 = ctx.MkSelect(mem1, ctx.MkBVAdd(ctx.MkBV(2, 64), rax1)) as BitVecExpr;
                BitVecExpr y3 = ctx.MkSelect(mem1, ctx.MkBVAdd(ctx.MkBV(3, 64), rax1)) as BitVecExpr;
                BitVecExpr y4 = ctx.MkSelect(mem1, ctx.MkBVAdd(ctx.MkBV(4, 64), rax1)) as BitVecExpr;
                BitVecExpr y5 = ctx.MkSelect(mem1, ctx.MkBVAdd(ctx.MkBV(5, 64), rax1)) as BitVecExpr;
                BitVecExpr y6 = ctx.MkSelect(mem1, ctx.MkBVAdd(ctx.MkBV(6, 64), rax1)) as BitVecExpr;
                BitVecExpr y7 = ctx.MkSelect(mem1, ctx.MkBVAdd(ctx.MkBV(7, 64), rax1)) as BitVecExpr;
                BitVecExpr y = ctx.MkConcat(y7, ctx.MkConcat(y6, ctx.MkConcat(y5, ctx.MkConcat(y4, ctx.MkConcat(y3, ctx.MkConcat(y2, ctx.MkConcat(y1, y0)))))));
                solver.Assert(ctx.MkEq(rcx2, y).Simplify() as BoolExpr);
                solver.Assert(ctx.MkEq(mem2, mem1));
                #endregion
                //Console.WriteLine(solver);
            }
            else
            {
                solver.Assert(ctx.MkEq(rax1, rax0));
                solver.Assert(ctx.MkEq(rbx1, rbx0));
                solver.Assert(ctx.MkEq(rcx1, rcx0));

                ArrayExpr mem1_0 = ctx.MkArrayConst("MEM!1-0", ctx.MkBitVecSort(64), ctx.MkBitVecSort(8));
                ArrayExpr mem1_1 = ctx.MkArrayConst("MEM!1-1", ctx.MkBitVecSort(64), ctx.MkBitVecSort(8));
                ArrayExpr mem1_2 = ctx.MkArrayConst("MEM!1-2", ctx.MkBitVecSort(64), ctx.MkBitVecSort(8));
                ArrayExpr mem1_3 = ctx.MkArrayConst("MEM!1-3", ctx.MkBitVecSort(64), ctx.MkBitVecSort(8));
                ArrayExpr mem1_4 = ctx.MkArrayConst("MEM!1-4", ctx.MkBitVecSort(64), ctx.MkBitVecSort(8));
                ArrayExpr mem1_5 = ctx.MkArrayConst("MEM!1-5", ctx.MkBitVecSort(64), ctx.MkBitVecSort(8));
                ArrayExpr mem1_6 = ctx.MkArrayConst("MEM!1-6", ctx.MkBitVecSort(64), ctx.MkBitVecSort(8));

                solver.Assert(ctx.MkEq(mem1_0, ctx.MkStore(mem0,   ctx.MkBVAdd(ctx.MkBV(0, 64), rax0), ctx.MkExtract((1 * 8) - 1, 0 * 8, rbx0))));
                solver.Assert(ctx.MkEq(mem1_1, ctx.MkStore(mem1_0, ctx.MkBVAdd(ctx.MkBV(1, 64), rax0), ctx.MkExtract((2 * 8) - 1, 1 * 8, rbx0))));
                solver.Assert(ctx.MkEq(mem1_2, ctx.MkStore(mem1_1, ctx.MkBVAdd(ctx.MkBV(2, 64), rax0), ctx.MkExtract((3 * 8) - 1, 2 * 8, rbx0))));
                solver.Assert(ctx.MkEq(mem1_3, ctx.MkStore(mem1_2, ctx.MkBVAdd(ctx.MkBV(3, 64), rax0), ctx.MkExtract((4 * 8) - 1, 3 * 8, rbx0))));
                solver.Assert(ctx.MkEq(mem1_4, ctx.MkStore(mem1_3, ctx.MkBVAdd(ctx.MkBV(4, 64), rax0), ctx.MkExtract((5 * 8) - 1, 4 * 8, rbx0))));
                solver.Assert(ctx.MkEq(mem1_5, ctx.MkStore(mem1_4, ctx.MkBVAdd(ctx.MkBV(5, 64), rax0), ctx.MkExtract((6 * 8) - 1, 5 * 8, rbx0))));
                solver.Assert(ctx.MkEq(mem1_6, ctx.MkStore(mem1_5, ctx.MkBVAdd(ctx.MkBV(6, 64), rax0), ctx.MkExtract((7 * 8) - 1, 6 * 8, rbx0))));
                solver.Assert(ctx.MkEq(mem1, ctx.MkStore(mem1_6,   ctx.MkBVAdd(ctx.MkBV(7, 64), rax0), ctx.MkExtract((8 * 8) - 1, 7 * 8, rbx0))));

                solver.Assert(ctx.MkEq(rax2, rax1));
                solver.Assert(ctx.MkEq(rbx2, rbx1));
                BitVecExpr y0 = ctx.MkSelect(mem1, ctx.MkBVAdd(ctx.MkBV(0, 64), rax1)) as BitVecExpr;
                BitVecExpr y1 = ctx.MkSelect(mem1, ctx.MkBVAdd(ctx.MkBV(1, 64), rax1)) as BitVecExpr;
                BitVecExpr y2 = ctx.MkSelect(mem1, ctx.MkBVAdd(ctx.MkBV(2, 64), rax1)) as BitVecExpr;
                BitVecExpr y3 = ctx.MkSelect(mem1, ctx.MkBVAdd(ctx.MkBV(3, 64), rax1)) as BitVecExpr;
                BitVecExpr y4 = ctx.MkSelect(mem1, ctx.MkBVAdd(ctx.MkBV(4, 64), rax1)) as BitVecExpr;
                BitVecExpr y5 = ctx.MkSelect(mem1, ctx.MkBVAdd(ctx.MkBV(5, 64), rax1)) as BitVecExpr;
                BitVecExpr y6 = ctx.MkSelect(mem1, ctx.MkBVAdd(ctx.MkBV(6, 64), rax1)) as BitVecExpr;
                BitVecExpr y7 = ctx.MkSelect(mem1, ctx.MkBVAdd(ctx.MkBV(7, 64), rax1)) as BitVecExpr;
                BitVecExpr y = ctx.MkConcat(y7, ctx.MkConcat(y6, ctx.MkConcat(y5, ctx.MkConcat(y4, ctx.MkConcat(y3, ctx.MkConcat(y2, ctx.MkConcat(y1, y0)))))));
                solver.Assert(ctx.MkEq(rcx2, y).Simplify() as BoolExpr);
                solver.Assert(ctx.MkEq(mem2, mem1));

                //Console.WriteLine(solver);
            }

            Status status_Pos = solver.Check(ctx.MkEq(rbx2, rcx2));
            Console.WriteLine("Status Pos = " + status_Pos);

            Status status_Neg = solver.Check(ctx.MkNot(ctx.MkEq(rbx2, rcx2)));
            Console.WriteLine("Status Neg = "+status_Neg);
        }

        static void TestGraph()
        {

            var graph = new BidirectionalGraph<long, TaggedEdge<long, bool>>(false);
            int rootVertex = 1;

            graph.AddVertex(1);
            graph.AddVertex(2);
            graph.AddVertex(3);
            graph.AddVertex(4);

            graph.AddEdge(new TaggedEdge<long, bool>(1L, 2L, true));
            graph.AddEdge(new TaggedEdge<long, bool>(1L, 4L, false));
            graph.AddEdge(new TaggedEdge<long, bool>(2L, 3L, false));

            string ToString(long vertex, int depth)
            {
                string result = "";
                for (int i = 0; i < depth; ++i) result += "  ";

                result += vertex.ToString() + "\n";

                foreach (var v in graph.OutEdges(vertex))
                {
                    result += v.Tag + "\n";
                    result += ToString(v.Target, depth + 2);
                }
                return result;
            }

            Console.WriteLine(ToString(rootVertex, 0));

//            graph.

        }

        static void TestMnemonic()
        {
            Dictionary<string, string> settings = new Dictionary<string, string>
            {
                { "unsat-core", "false" },    // enable generation of unsat cores
                { "model", "false" },         // enable model generation
                { "proof", "false" },         // enable proof generation
                { "timeout", "1000" }
            };
            Tools tools = new Tools(settings);
            Context ctx = new Context(settings);
            tools.StateConfig.Set_All_Off();

            if (false)
            {
                tools.StateConfig.Set_All_Off();
                tools.StateConfig.Set_All_Flags_On();
                tools.StateConfig.RAX = true;

                string line1 = "mov rax, 1";
                string line2 = "shl rax, 65"; // special behaviour: shift left too large; 65 mod 64 = 1

                {   // forward
                    string rootKey = "!INIT";
                    State state = new State(tools, rootKey, rootKey);
                    state = Runner.SimpleStep_Forward(line1, state);
                    state = Runner.SimpleStep_Forward(line2, state);

                    Console.WriteLine("IsConsistent=" + state.IsConsistent);
                    Console.WriteLine("After \"" + line2 + "\", we know:\n" + state);
                }
            }
            if (false)
            {
                tools.StateConfig.RAX = true;
                tools.StateConfig.RBX = true;
                tools.StateConfig.RCX = true;
                tools.StateConfig.CF = true;

                string line1 = "mov rax, rbx";
                string line2 = "xor rax, rbx";
                string line3 = "rcl rax, cl";

                string rootKey = "!0";
                State state = new State(tools, rootKey, rootKey);

                state = Runner.SimpleStep_Forward(line1, state);
                state = Runner.SimpleStep_Forward(line2, state);
                state = Runner.SimpleStep_Forward(line3, state);
                Console.WriteLine("After \"" + line3 + "\", we know:\n" + state);

            }
            if (false)
            {
                tools.StateConfig.RAX = true;
                tools.StateConfig.RBX = true;

                string line1 = "mov rax, 10";
                string line2 = "mov rbx, 20";
                string line3 = "add rax, rbx";

                string rootKey = "!0";
                State state = new State(tools, rootKey, rootKey);

                Console.WriteLine("Before \"" + line3 + "\", we know:\n" + state);
                state = Runner.SimpleStep_Backward(line3, state);
                Console.WriteLine("After \"" + line3 + "\", we know:\n" + state);
                state = Runner.SimpleStep_Backward(line2, state);
                Console.WriteLine("After \"" + line2 + "\", we know:\n" + state);
                state = Runner.SimpleStep_Backward(line1, state);
                Console.WriteLine("After \"" + line1 + "\", we know:\n" + state);

            }
            if (false)
            {
                ulong a = 0b0000_1000;
                ulong b = 0b0000_0100;

                string tailKey = Tools.CreateKey(tools.Rand);
                string headKey = Tools.CreateKey(tools.Rand);
                State state = new State(tools, tailKey, headKey);

                string line1 = "add al, bl";

                string nextKey = Tools.CreateKey(state.Tools.Rand);
                StateUpdate updateState = new StateUpdate(state.TailKey, nextKey, state.Tools, ctx);
                updateState.Set(Rn.AL, a);
                updateState.Set(Rn.BL, b);

                state.Update_Forward(updateState);
                //if (logToDisplay) Console.WriteLine("Before \"" + line1 + "\", we know:\n" + state);

                state = Runner.SimpleStep_Forward(line1, state);
                Console.WriteLine("After \"" + line1 + "\", we know:\n" + state);

                Console.WriteLine(ToolsZ3.ToStringBin(state.GetTvArray(Rn.AL)));
            }
            if (false)
            {
                tools.StateConfig.Set_All_Off();
                tools.StateConfig.RAX = true;
                tools.StateConfig.RBX = true;
                tools.StateConfig.ZF = true;

                string programStr =
                  //"           xor     rax,        rax             " + Environment.NewLine +
                    "           jz      label1                      " + Environment.NewLine +
                    "           mov     rax,        1               " + Environment.NewLine +
                    "           jmp     label2                      " + Environment.NewLine +
                    "label1:                                        " + Environment.NewLine +
                    "           mov     rax,        2               " + Environment.NewLine +
                    "label2:                                        " + Environment.NewLine +
                    "           mov     rbx,        0               ";

                StaticFlow sFlow1 = new StaticFlow(programStr, tools);
                Console.WriteLine(sFlow1);

                tools.Quiet = false;
                var tree1 = new DynamicFlow(tools);
                tree1.Reset(sFlow1, false);
                Console.WriteLine(tree1.EndState);
            }
            if (false)
            {
                tools.StateConfig.Set_All_Off();
                tools.StateConfig.RAX = true;
                tools.StateConfig.RBX = true;
                tools.StateConfig.ZF = true;
                tools.StateConfig.mem = true;

                string programStr0 =
                "           jz      label1                      " + Environment.NewLine +
                "           mov     byte ptr[rax],     10      " + Environment.NewLine +
                "           jmp     label2                      " + Environment.NewLine +
                "label1:                                        " + Environment.NewLine +
                "           mov     byte ptr[rax],     20      " + Environment.NewLine +
                "label2:                                        " + Environment.NewLine +
                "           mov     bl, byte ptr[rax]         ";

                StaticFlow flow1 = new StaticFlow(programStr0, tools);
                Console.WriteLine(flow1);

                if (false)
                {
                    tools.Quiet = false;
                    var tree0 = Runner.Construct_DynamicFlow_Forward(flow1, tools);

                    int lineNumber_JZ = 0;
                    State state_FirstLine = tree0.States_Before(lineNumber_JZ, 0);
                    var branchInfo = new BranchInfo(state_FirstLine.Get(Flags.ZF), true);

                    State state0 = tree0.EndState;
                    state0.BranchInfoStore.Add(branchInfo);
                    Console.WriteLine("State0:" + state0);
                }
                if (true)
                {
                    tools.Quiet = false;
                    var tree1 = Runner.Construct_DynamicFlow_Backward(flow1, tools);

                    int lineNumber_JZ = 0;
                    State state_FirstLine = tree1.States_Before(lineNumber_JZ, 0);
                    var branchInfo = new BranchInfo(state_FirstLine.Get(Flags.ZF), false);

                    State state1 = tree1.EndState;
                    state1.BranchInfoStore.Add(branchInfo);
                    Console.WriteLine("State1:" + state1);
                }
            }
            if (false)
            {
                string programStr1 =
                    "mov rax, 0" + Environment.NewLine +
                    "mov ptr qword[rax], 2";

                string programStr2 =
                    "mov rbx, 1" + Environment.NewLine +
                    "mov ptr qword[rbx], 3"; 
                
                StaticFlow flow1 = new StaticFlow(programStr1, tools);
                StaticFlow flow2 = new StaticFlow(programStr2, tools);

                tools.Quiet = true;
                tools.StateConfig.Set_All_Off();
                tools.StateConfig.RAX = true;
                tools.StateConfig.RBX = true;
                tools.StateConfig.mem = true;

                var tree1 = Runner.Construct_DynamicFlow_Forward(flow1, tools);
                var tree2 = Runner.Construct_DynamicFlow_Forward(flow2, tools);

                //Console.WriteLine(tree1.ToString(flow1));
                State state1 = tree1.EndState;
                State state2 = tree2.EndState;

                Console.WriteLine("state1:" + state1);
                Console.WriteLine("state2:" + state2);
                State mergedState = new State(state1, state2, true);
                Console.WriteLine("mergedState:" + mergedState);
            }
            if (false)
            {
                string programStr1 =
                   "mov rax, 0" + Environment.NewLine +
                   "mov rbx, 0";

                string programStr2 =
                    "mov rax, 1" + Environment.NewLine +
                    "mov rbx, 1";

                StaticFlow flow1 = new StaticFlow(programStr1, tools);
                StaticFlow flow2 = new StaticFlow(programStr2, tools);

                tools.Quiet = true;

                var tree1 = Runner.Construct_DynamicFlow_Forward(flow1, tools);
                var tree2 = Runner.Construct_DynamicFlow_Forward(flow2, tools);

                //Console.WriteLine(tree1.ToString(flow1));


                State state1 = tree1.Leafs.ElementAt(0);
                State state2 = tree2.Leafs.ElementAt(0);

                Console.WriteLine("state1:" + state1);
                Console.WriteLine("state2:" + state2);
                State mergedState = new State(state1, state2, true);
                Console.WriteLine("mergedState:" + mergedState);
            }
        }

        static void TestDynamicFlow()
        {
            Dictionary<string, string> settings = new Dictionary<string, string>
            {
                { "unsat-core", "false" },    // enable generation of unsat cores
                { "model", "false" },         // enable model generation
                { "proof", "false" },         // enable proof generation
                { "timeout", "1000" }
            };
            Tools tools = new Tools(settings);
            tools.StateConfig.Set_All_Off();
            tools.StateConfig.RAX = true;
            tools.StateConfig.RBX = true;
            tools.StateConfig.ZF = true;
            tools.ShowUndefConstraints = true;

            string programStr1 =
                "           cmp     rax,        0               " + Environment.NewLine +
                "           jz      label1                      " + Environment.NewLine +
                "           mov     rax,        0               " + Environment.NewLine +
                "label1:                                        ";

            string programStr2 =
                "           mov     rax,        0               " + Environment.NewLine +
                "           aaa                                 " + Environment.NewLine +
                "           mov     rbx,        10              " + Environment.NewLine +
                "           mov     rbx,        rax             ";

            string programStr3 =
                "           cmp     rax,        0               " + Environment.NewLine +
                "           jz      label1                      " + Environment.NewLine +
                "           mov     rax,        0               " + Environment.NewLine +
                "           jmp     label2                      " + Environment.NewLine +
                "label1:                                        " + Environment.NewLine +
                "           mov     rax,        1               " + Environment.NewLine +
                "label2:";

            string programStr4 =
                "           mov        rbx,     0               " + Environment.NewLine +
                "           mov        rax,     0x3             " + Environment.NewLine +
                "label1:                                        " + Environment.NewLine +
                "           inc        rbx                      " + Environment.NewLine +
                "           dec        rax                      " + Environment.NewLine +
                "           jnz        label1                   ";


            StaticFlow sFlow = new StaticFlow(programStr2, tools);
            Console.WriteLine(sFlow);

            if (true)
            {
                tools.Quiet = false;
                DynamicFlow dFlow = Runner.Construct_DynamicFlow_Forward(sFlow, tools);
                //DynamicFlow dFlow = Runner.Construct_DynamicFlow_Backward(sFlow, tools);

                Console.WriteLine(dFlow.ToString(sFlow));
                //DotVisualizer.SaveToDot(sFlow, dFlow, "test1.dot");

                int lineNumber = 1;
                if (false)
                {
                    IList<State> states_Before = new List<State>(dFlow.States_Before(lineNumber));
                    State state_Before = states_Before[0];
                    Console.WriteLine("Before lineNumber " + lineNumber + " \"" + sFlow.Get_Line_Str(lineNumber) + "\", we know:\n" + state_Before);
                }
                if (true)
                {
                    IList<State> states_After = new List<State>(dFlow.States_After(lineNumber));
                    State state_After = states_After[0];
                    Console.WriteLine("After lineNumber " + lineNumber + " \"" + sFlow.Get_Line_Str(lineNumber) + "\", we know:\n" + state_After);
                }
                if (false)
                {
                    State endState = dFlow.EndState;
                    Console.WriteLine("in endState we know:\n" + endState);
                }
            }
        }
        static void EmptyMemoryTest()
        {
            Dictionary<string, string> settings = new Dictionary<string, string>
            {
                { "unsat-core", "false" },    // enable generation of unsat cores
                { "model", "false" },         // enable model generation
                { "proof", "false" }         // enable proof generation
            };

            Context ctx = new Context(settings);
            Tactic tactic = ctx.MkTactic("qfbv");
            Solver solver = ctx.MkSolver(tactic);

            if (false) {
                ArrayExpr mem0 = ctx.MkArrayConst("memory", ctx.MkBitVecSort(64), ctx.MkBitVecSort(8));
                Expr address1 = ctx.MkBV(10, 64);
                Expr address2 = ctx.MkBV(12, 64);
                Expr value = ctx.MkBV(0xFF, 8);

                ArrayExpr mem1 = ctx.MkStore(mem0, address1, value);
                Console.WriteLine("Stored value " + value);

                Expr retrievedValue1 = ctx.MkSelect(mem1, address1);
                Console.WriteLine("Retrieved value 1 " + retrievedValue1);
                Console.WriteLine("Retrieved value 1 Simplified " + retrievedValue1.Simplify());

                Expr retrievedValue2 = ctx.MkSelect(mem1, address2);
                Console.WriteLine("Retrieved value 2 " + retrievedValue2);
                Console.WriteLine("Retrieved value 2 Simplified " + retrievedValue2.Simplify());

            }
            if (true) {
                ArrayExpr mem0 = ctx.MkConstArray(ctx.MkBitVecSort(64), ctx.MkBV(0, 8));
                Expr address1 = ctx.MkBV(10, 64);
                Expr address2 = ctx.MkBV(12, 64);
                Expr value = ctx.MkBV(0xFF, 8);

                ArrayExpr mem1 = ctx.MkStore(mem0, address1, value);
                Console.WriteLine("Stored value " + value);

                Expr retrievedValue1 = ctx.MkSelect(mem1, address1);
               // Console.WriteLine("Retrieved value 1 " + retrievedValue1);
                Console.WriteLine("Retrieved value 1 Simplified " + retrievedValue1.Simplify());

                Expr retrievedValue2 = ctx.MkSelect(mem1, address2);
                //Console.WriteLine("Retrieved value 2 " + retrievedValue2);
                Console.WriteLine("Retrieved value 2 Simplified " + retrievedValue2.Simplify());
            }
        }

        static void ProgramSynthesis1()
        {
            if (false)
            {
                Dictionary<string, string> settings = new Dictionary<string, string>
                {
                    { "unsat-core", "false" },    // enable generation of unsat cores
                    { "model", "true" },         // enable model generation
                    { "proof", "false" }         // enable proof generation
                };
                Context ctx = new Context(settings);
                Solver solver = ctx.MkSolver();

                BoolExpr b1 = ctx.MkBoolConst("b1");
                BoolExpr b2 = ctx.MkBoolConst("b2");
                IntExpr i1 = ctx.MkIntConst("i1");
                IntExpr i2 = ctx.MkIntConst("i2");

                if (false)
                {
                    solver.Assert(ctx.MkOr(ctx.MkNot(b1), ctx.MkLt(ctx.MkInt(0), i1), ctx.MkAnd(b2, ctx.MkEq(i1, i2))));
                    Console.WriteLine(solver);
                } else
                {
                    FuncDecl myFunc = ctx.MkFuncDecl("MyFunc", ctx.MkIntSort(), ctx.MkBoolSort());

                    BoolExpr newState = ctx.MkOr(ctx.MkNot(b1), ctx.MkLt(ctx.MkInt(0), i1), ctx.MkAnd(b2, ctx.MkEq(i1, i2)));
                    solver.Assert(ctx.MkQuantifier(true, new Expr[] { i1 }, ctx.MkEq(myFunc.Apply(i1), ctx.MkOr(ctx.MkNot(b1), ctx.MkLt(ctx.MkInt(0), i1), ctx.MkAnd(b2, ctx.MkEq(i1, i2))))));

                    solver.Assert(b1);

                    Console.WriteLine(solver);
                    Status status = solver.Check();
                    Console.WriteLine("Status = " + status);
                    if (status == Status.SATISFIABLE) Console.WriteLine(solver.Model);

                }
            }
            else if (true)
            {
                Dictionary<string, string> settings = new Dictionary<string, string>
                {
                    { "unsat-core", "true" },    // enable generation of unsat cores
                    { "model", "true" },         // enable model generation
                    { "proof", "false" }         // enable proof generation
                };
                Context ctx = new Context(settings);
                Solver solver = ctx.MkSolver();

                IList<BoolExpr> switchList = new List<BoolExpr>();

                BitVecExpr rax_0 = ctx.MkBVConst("RAX!0", 8); // register values
                BitVecExpr rax_1 = ctx.MkBVConst("RAX!1", 8);

                BoolExpr switch_XOR_RAX_RAX = ctx.MkBoolConst("switch_XOR_RAX_RAX"); // switch on/off instruction 1
                BoolExpr switch_INC_RAX = ctx.MkBoolConst("switch_INC_RAX"); // switch on/off instruction 2

                //solver.Assert(switch_XOR_RAX_RAX); // this instruction should not be allowed
                solver.Assert(switch_INC_RAX); // this instruction has to be allowed

                solver.Assert(ctx.MkImplies(switch_XOR_RAX_RAX, ctx.MkEq(rax_1, ctx.MkBV(0, 8))));
                solver.Assert(ctx.MkImplies(switch_INC_RAX, ctx.MkEq(rax_1, ctx.MkBVAdd(rax_0, ctx.MkBV(1, 8)))));

                // atleast and atmost one instruction must be executed
                solver.Assert(ctx.MkAtMost(new BoolExpr[] { switch_XOR_RAX_RAX, switch_INC_RAX }, 1));
                solver.Assert(ctx.MkOr(new BoolExpr[] { switch_XOR_RAX_RAX, switch_INC_RAX }));

                // after executing we want rax to be 0
                if (false)
                {
                    solver.Assert(ctx.MkEq(rax_1, ctx.MkBV(0b0000_0000, 8)));
                }
                else
                {
                    BitVecExpr reg_0 = ctx.MkBVConst("reg0", 8);
                    BitVecExpr reg_1 = ctx.MkBVConst("reg1", 8);

                    solver.Assert(ctx.MkNot(ctx.MkQuantifier(true, new Expr[] { reg_0, reg_1 }, 
                        ctx.MkIff(
                            ctx.MkAnd(
                                ctx.MkEq(ctx.MkBVAdd(rax_0, ctx.MkBV(1, 8)), reg_0), 
                                ctx.MkEq(rax_1, reg_1)
                            ),
                            ctx.MkEq(reg_0, reg_1))
                        )
                    ));
                    //solver.Assert(ctx.MkNot(ctx.MkEq(rax_0, ctx.MkBV(0, 64))));
                    /*
                    solver.Assert(ctx.MkNot(ctx.MkQuantifier(false, new Expr[] { reg1 },
                        ctx.MkAnd(ctx.MkEq(reg1, rax_1), ctx.MkNot(ctx.MkEq(reg1, ctx.MkBVAdd(rax_0, ctx.MkBV(1, 8)))))
                    )));
                    */
            /*
            solver.Assert(ctx.MkQuantifier(true, new Expr[] { reg1 }, ctx.MkAnd(
                ctx.MkIff(ctx.MkEq(reg1, rax_1), ctx.MkEq(reg1, ctx.MkBVAdd(rax_0, ctx.MkBV(1, 8)))),
                ctx.MkIff(ctx.MkEq(reg2, rax_1), ctx.MkEq(reg2, ctx.MkBVAdd(rax_0, ctx.MkBV(1, 8)))),
                ctx.MkNot(ctx.MkEq(reg1, reg2))
            )));
            */
        }


                foreach (BoolExpr b in solver.Assertions) Console.WriteLine("Solver A: " + b);
                Console.WriteLine("-------------");


                Status status = solver.Check();
                Console.WriteLine("Status = " + status);
                if (status == Status.SATISFIABLE)
                {
                    foreach (FuncDecl funcDecl in solver.Model.ConstDecls)
                    {
                        Console.WriteLine("Model A: " + funcDecl.Name + "=" + solver.Model.ConstInterp(funcDecl));
                    }
                } else
                {
                    foreach (BoolExpr b in solver.UnsatCore)
                    {
                        Console.WriteLine("Uncore: " + b);
                    }
                }
            }
            else if (false)
            {
                Dictionary<string, string> settings = new Dictionary<string, string>
                {
                    { "unsat-core", "true" },    // enable generation of unsat cores
                    { "model", "true" },         // enable model generation
                    { "proof", "false" }         // enable proof generation
                };
                Context ctx = new Context(settings);
                Solver solver = ctx.MkSolver();

                BitVecExpr rax0 = ctx.MkBVConst("RAX!0", 64);
                BitVecExpr rax1 = ctx.MkBVConst("RAX!1", 64);
                BitVecExpr rbx0 = ctx.MkBVConst("RBX!0", 64);
                BitVecExpr rbx1 = ctx.MkBVConst("RBX!1", 64);

                BoolExpr rax0_input = ctx.MkBoolConst("RAX!0!input");
                BoolExpr rax1_input = ctx.MkBoolConst("RAX!1!input");
                BoolExpr rbx0_input = ctx.MkBoolConst("RBX!0!input");
                BoolExpr rbx1_input = ctx.MkBoolConst("RBX!1!input");

                BoolExpr rax0_goal = ctx.MkBoolConst("RAX!0!goal");
                BoolExpr rax1_goal = ctx.MkBoolConst("RAX!1!goal");
                BoolExpr rbx0_goal = ctx.MkBoolConst("RBX!0!goal");
                BoolExpr rbx1_goal = ctx.MkBoolConst("RBX!1!goal");

                // switches
                BoolExpr switch_L1_INC_RAX = ctx.MkBoolConst("switch_L1_INC_RAX");
                BoolExpr switch_L1_INC_RBX = ctx.MkBoolConst("switch_L1_INC_RBX");

                BoolExpr switch_L1_XOR_RAX_RAX = ctx.MkBoolConst("switch_L1_XOR_RAX_RAX");
                BoolExpr switch_L1_XOR_RBX_RBX = ctx.MkBoolConst("switch_L1_XOR_RBX_RBX");

                solver.Assert(ctx.MkAtMost(new BoolExpr[] { switch_L1_INC_RAX, switch_L1_INC_RBX, switch_L1_XOR_RAX_RAX, switch_L1_XOR_RBX_RBX }, 1));
                solver.Assert(ctx.MkOr(new BoolExpr[] { switch_L1_INC_RAX, switch_L1_INC_RBX, switch_L1_XOR_RAX_RAX, switch_L1_XOR_RBX_RBX }));


                BitVecExpr ZERO = ctx.MkBV(0, 64);
                BitVecExpr ONE = ctx.MkBV(1, 64);


                // INC RAX
                solver.Assert(ctx.MkImplies(
                    ctx.MkAnd(switch_L1_INC_RAX),
                    ctx.MkAnd(ctx.MkEq(rax1, ctx.MkBVAdd(rax0, ONE)), 
                    rax0_goal, // make the prerequisite a goal
                    rax1_goal, // make application of this rule goal directed
                    rax0_input, //rax0 is  based on (variable) input but is not a constant
                    rax1_input)) //rax1 is  based on (variable) input but is not a constant
                );

                // INC RBX
                solver.Assert(ctx.MkImplies(
                    ctx.MkAnd(switch_L1_INC_RBX),
                    ctx.MkAnd(ctx.MkEq(rax1, ctx.MkBVAdd(rbx0, ONE)),
                    rbx0_goal, // make the prerequisite a goal
                    rbx1_goal, // make application of this rule goal directed
                    rbx0_input, //rax0 is  based on (variable) input but is not a constant
                    rbx1_input)) //rax1 is  based on (variable) input but is not a constant
                );

                // XOR RAX, RAX
                solver.Assert(ctx.MkImplies(
                    ctx.MkAnd(switch_L1_XOR_RAX_RAX),
                    ctx.MkAnd(ctx.MkEq(rax1, ZERO),
                    // rax0_goal is irelevant 
                    rax1_goal, // make application of this rule goal directed
                    ctx.MkNot(rax0_input), // TODO: could this create inconsistencies with other instructions that updated rax!0
                    ctx.MkNot(rax1_input))) // rax1 is not based on (variable) input but is a constant
                );

                // XOR RBX, RBX
                solver.Assert(ctx.MkImplies(
                    ctx.MkAnd(switch_L1_XOR_RBX_RBX),
                    ctx.MkAnd(ctx.MkEq(rbx1, ZERO),
                    // rbx0_goal is irelevant 
                    rbx1_goal, // make application of this rule goal directed
                    ctx.MkNot(rbx0_input), // TODO: could this create inconsistencies with other instructions that updated rax!0
                    ctx.MkNot(rbx1_input))) // rax1 is not based on (variable) input but is a constant
                );

                {   // check INC RAX
                    solver.Push();
                    solver.Assert(ctx.MkEq(rax1, ctx.MkBVAdd(rax0, ctx.MkBV(1, 64))));
                    solver.Assert(rax0_input, rax1_goal);

                    if (solver.Check(switch_L1_INC_RAX) == Status.UNSATISFIABLE)
                        Console.WriteLine("A: INC RAX: switch_INC SHOULD HAVE BEEN ALLOWED");

                    if (solver.Check(switch_L1_XOR_RAX_RAX) == Status.SATISFIABLE)
                        Console.WriteLine("A: XOR RAX, RAX: switch_XOR SHOULD NOT HAVE BEEN ALLOWED");

                    solver.Pop();
                }
                {   // check XOR RAX, RAX
                    solver.Push();
                    solver.Assert(ctx.MkEq(rax1, ctx.MkBV(0, 64)));
                    solver.Assert(ctx.MkNot(rax0_input));

                    if (solver.Check(switch_L1_INC_RAX) == Status.SATISFIABLE)
                        Console.WriteLine("B: INC RAX: switch_INC SHOULD NOT HAVE BEEN ALLOWED");

                    if (solver.Check(switch_L1_XOR_RAX_RAX) == Status.UNSATISFIABLE)
                        Console.WriteLine("B: XOR RAX, RAX: switch_XOR SHOULD HAVE BEEN ALLOWED");

                    solver.Pop();
                }

                Console.WriteLine("");
                foreach (BoolExpr b in solver.Assertions)
                    Console.WriteLine("Solver = " + b);
                Console.WriteLine("");

                Status status = solver.Check();
                Console.WriteLine("Status = " + status+"\n");
                if (status == Status.SATISFIABLE)
                {
                    foreach (FuncDecl f in solver.Model.ConstDecls)
                    {
                        Console.WriteLine("Model: " + f.Name + " = "+ solver.Model.ConstInterp(f));
                    }

                } else
                {
                    foreach (BoolExpr b in solver.UnsatCore)
                    {
                        Console.WriteLine("Unsat: " + b);
                    }
                }
                Console.WriteLine("");

                /*

(declare-const RAX!0 (_ BitVec 8))
(declare-const RAX!1 (_ BitVec 8))

(declare-const switch_inc bool)
(declare-const switch_xor_xor bool)

(assert ((_ at-most 1) switch_inc switch_xor_xor))
(assert (or switch_inc switch_xor_xor))

(assert (=> switch_inc (= RAX!1 (bvadd RAX!0 #x01))))
(assert (=> switch_xor_xor (= RAX!1 #x00)))

;(assert (= RAX!1 (bvadd RAX!0 #x01)))
(assert (forall ((RAX1 (_ BitVec 8))) (= RAX!1 (bvadd RAX!0 #x01))))
(assert (not switch_inc))

(check-sat)
(get-model)
                 */
            }
            else 
            {
                //ProgramSyntesizer ps = new ProgramSyntesizer(3, new Rn[] { Rn.RAX, Rn.RBX, Rn.RCX, Rn.RDX });
                ProgramSyntesizer ps = new ProgramSyntesizer(2, new Rn[] { Rn.RAX });
                ps.Run();
            }
        }

        private static BoolExpr IsKnownTest(BitVecExpr reg, Context ctx)
        {
            return ctx.MkImplies(ctx.MkNot(ctx.MkEq(reg, ctx.MkBV(0xFFFF_FFFF_FFFF_FFFF, 64))), ctx.MkEq(ctx.MkTrue(), ctx.MkFalse()));
        }

        static void TestFunctions()
        {
            Context ctx = new Context();
            {
                Solver solver1 = ctx.MkSolver();
                BitVecExpr rax0 = ctx.MkBVConst("RAX!0", 64);
                BitVecExpr rax1 = ctx.MkBVConst("RAX!1", 64);

                BitVecExpr var = ctx.MkBVConst("var", 64);
                FuncDecl incFunc64 = ctx.MkFuncDecl("INC_64", ctx.MkBitVecSort(64), ctx.MkBitVecSort(64));
                solver1.Assert(ctx.MkQuantifier(true, new Expr[] { var }, ctx.MkEq(incFunc64.Apply(var), ctx.MkBVAdd(var, ctx.MkBV(1, 64)))));

                solver1.Assert(ctx.MkEq(rax0, ctx.MkBV(0, 64)));
                solver1.Assert(ctx.MkEq(rax1, incFunc64.Apply(rax0)));

                Console.WriteLine(solver1);
                Console.WriteLine(ToolsZ3.ToStringBin(ToolsZ3.GetTvArray(rax1, 64, solver1, ctx)));
            }
            {
                Solver solver2 = ctx.MkSolver();
                BitVecExpr rax0 = ctx.MkBVConst("RAX!0", 64);
                BitVecExpr rax1 = ctx.MkBVConst("RAX!1", 64);

                solver2.Assert(ctx.MkEq(rax0, ctx.MkBV(0, 64)));
                solver2.Assert(ctx.MkEq(rax1, ctx.MkBVAdd(rax0, ctx.MkBV(1, 64))));

                Console.WriteLine(solver2);
                Console.WriteLine(ToolsZ3.ToStringBin(ToolsZ3.GetTvArray(rax1, 64, solver2, ctx)));
            }
        }

        static void TacticTest()
        {
            Context ctx = new Context();

            if (false)
            {
                #region Doc
                /*
                    tacticName ackermannize_bv: A tactic for performing full Ackermannization on bv instances.
                    tacticName subpaving: tactic for testing subpaving module.
                    tacticName horn: apply tactic for horn clauses.
                    tacticName horn-simplify: simplify horn clauses.
                    tacticName nlsat: (try to) solve goal using a nonlinear arithmetic solver.
                    tacticName qfnra-nlsat: builtin strategy for solving QF_NRA problems using only nlsat.
                    tacticName nlqsat: apply a NL-QSAT solver.
                    tacticName qe-light: apply light-weight quantifier elimination.
                    tacticName qe-sat: check satisfiability of quantified formulas using quantifier elimination.
                    tacticName qe: apply quantifier elimination.
                    tacticName qsat: apply a QSAT solver.
                    tacticName qe2: apply a QSAT based quantifier elimination.
                    tacticName qe_rec: apply a QSAT based quantifier elimination recursively.
                    tacticName vsubst: checks satsifiability of quantifier-free non-linear constraints using virtual substitution.
                    tacticName sat: (try to) solve goal using a SAT solver.
                    tacticName sat-preprocess: Apply SAT solver preprocessing procedures (bounded resolution, Boolean constant propagation, 2-SAT, subsumption, subsumption resolution).
                    tacticName ctx-solver-simplify: apply solver-based contextual simplification rules.
                    tacticName smt: apply a SAT based SMT solver.
                    tacticName unit-subsume-simplify: unit subsumption simplification.
                    tacticName aig: simplify Boolean structure using AIGs.
                    tacticName add-bounds: add bounds to unbounded variables (under approximation).
                    tacticName card2bv: convert pseudo-boolean constraints to bit-vectors.
                    tacticName degree-shift: try to reduce degree of polynomials (remark: :mul2power simplification is automatically applied).
                    tacticName diff-neq: specialized solver for integer arithmetic problems that contain only atoms of the form (<= k x) (<= x k) and (not (= (- x y) k)), where x and y are constants and k is a numeral, and all constants are bounded.
                    tacticName elim01: eliminate 0-1 integer variables, replace them by Booleans.
                    tacticName eq2bv: convert integer variables used as finite domain elements to bit-vectors.
                    tacticName factor: polynomial factorization.
                    tacticName fix-dl-var: if goal is in the difference logic fragment, then fix the variable with the most number of occurrences at 0.
                    tacticName fm: eliminate variables using fourier-motzkin elimination.
                    tacticName lia2card: introduce cardinality constraints from 0-1 integer.
                    tacticName lia2pb: convert bounded integer variables into a sequence of 0-1 variables.
                    tacticName nla2bv: convert a nonlinear arithmetic problem into a bit-vector problem, in most cases the resultant goal is an under approximation and is useul for finding models.
                    tacticName normalize-bounds: replace a variable x with lower bound k <= x with x' = x - k.
                    tacticName pb2bv: convert pseudo-boolean constraints to bit-vectors.
                    tacticName propagate-ineqs: propagate ineqs/bounds, remove subsumed inequalities.
                    tacticName purify-arith: eliminate unnecessary operators: -, /, div, mod, rem, is-int, to-int, ^, root-objects.
                    tacticName recover-01: recover 0-1 variables hidden as Boolean variables.
                    tacticName bit-blast: reduce bit-vector expressions into SAT.
                    tacticName bv1-blast: reduce bit-vector expressions into bit-vectors of size 1 (notes: only equality, extract and concat are supported).
                    tacticName bv_bound_chk: attempts to detect inconsistencies of bounds on bv expressions.
                    tacticName propagate-bv-bounds: propagate bit-vector bounds by simplifying implied or contradictory bounds.
                    tacticName reduce-bv-size: try to reduce bit-vector sizes using inequalities.
                    tacticName bvarray2uf: Rewrite bit-vector arrays into bit-vector (uninterpreted) functions.
                    tacticName dt2bv: eliminate finite domain data-types. Replace by bit-vectors.
                    tacticName elim-small-bv: eliminate small, quantified bit-vectors by expansion.
                    tacticName max-bv-sharing: use heuristics to maximize the sharing of bit-vector expressions such as adders and multipliers.
                    tacticName blast-term-ite: blast term if-then-else by hoisting them.
                    tacticName cofactor-term-ite: eliminate term if-the-else using cofactors.
                    tacticName collect-statistics: Collects various statistics.
                    tacticName ctx-simplify: apply contextual simplification rules.
                    tacticName der: destructive equality resolution.
                    tacticName distribute-forall: distribute forall over conjunctions.
                    tacticName elim-term-ite: eliminate term if-then-else by adding fresh auxiliary declarations.
                    tacticName elim-uncnstr: eliminate application containing unconstrained variables.
                    tacticName snf: put goal in skolem normal form.
                    tacticName nnf: put goal in negation normal form.
                    tacticName occf: put goal in one constraint per clause normal form (notes: fails if proof generation is enabled; only clauses are considered).
                    tacticName pb-preprocess: pre-process pseudo-Boolean constraints a la Davis Putnam.
                    tacticName propagate-values: propagate constants.
                    tacticName reduce-args: reduce the number of arguments of function applications, when for all occurrences of a function f the i-th is a value.
                    tacticName simplify: apply simplification rules.
                    tacticName elim-and: convert (and a b) into (not (or (not a) (not b))).
                    tacticName solve-eqs: eliminate variables by solving equations.
                    tacticName split-clause: split a clause in many subgoals.
                    tacticName symmetry-reduce: apply symmetry reduction.
                    tacticName tseitin-cnf: convert goal into CNF using tseitin-like encoding (note: quantifiers are ignored).
                    tacticName tseitin-cnf-core: convert goal into CNF using tseitin-like encoding (note: quantifiers are ignored). This tactic does not apply required simplifications to the input goal like the tseitin-cnf tactic.
                    tacticName fpa2bv: convert floating point numbers to bit-vectors.
                    tacticName qffp: (try to) solve goal using the tactic for QF_FP.
                    tacticName qffpbv: (try to) solve goal using the tactic for QF_FPBV (floats+bit-vectors).
                    tacticName nl-purify: Decompose goal into pure NL-sat formula and formula over other theories.
                    tacticName default: default strategy used when no logic is specified.
                    tacticName qfbv-sls: (try to) solve using stochastic local search for QF_BV.
                    tacticName nra: builtin strategy for solving NRA problems.
                    tacticName qfaufbv: builtin strategy for solving QF_AUFBV problems.
                    tacticName qfauflia: builtin strategy for solving QF_AUFLIA problems.
                    tacticName qfbv: builtin strategy for solving QF_BV problems.
                    tacticName qfidl: builtin strategy for solving QF_IDL problems.
                    tacticName qflia: builtin strategy for solving QF_LIA problems.
                    tacticName qflra: builtin strategy for solving QF_LRA problems.
                    tacticName qfnia: builtin strategy for solving QF_NIA problems.
                    tacticName qfnra: builtin strategy for solving QF_NRA problems.
                    tacticName qfuf: builtin strategy for solving QF_UF problems.
                    tacticName qfufbv: builtin strategy for solving QF_UFBV problems.
                    tacticName qfufbv_ackr: A tactic for solving QF_UFBV based on Ackermannization.
                    tacticName qfufnra: builtin strategy for solving QF_UNFRA problems.
                    tacticName ufnia: builtin strategy for solving UFNIA problems.
                    tacticName uflra: builtin strategy for solving UFLRA problems.
                    tacticName auflia: builtin strategy for solving AUFLIA problems.
                    tacticName auflira: builtin strategy for solving AUFLIRA problems.
                    tacticName aufnira: builtin strategy for solving AUFNIRA problems.
                    tacticName lra: builtin strategy for solving LRA problems.
                    tacticName lia: builtin strategy for solving LIA problems.
                    tacticName lira: builtin strategy for solving LIRA problems.
                    tacticName skip: do nothing tactic.
                    tacticName fail: always fail tactic.
                    tacticName fail-if-undecided: fail if goal is undecided.
                    tacticName macro-finder: Identifies and applies macros.
                    tacticName quasi-macros: Identifies and applies quasi-macros.
                    tacticName ufbv-rewriter: Applies UFBV-specific rewriting rules, mainly demodulation.
                    tacticName bv: builtin strategy for solving BV problems (with quantifiers).
                    tacticName ufbv: builtin strategy for solving UFBV problems (with quantifiers).
                */
#endregion
                foreach (string tacticName in ctx.TacticNames)
                {
                    Console.WriteLine("tacticName " + tacticName + ": " + ctx.TacticDescription(tacticName));
                }
            }

            Tactic tactic3 = ctx.MkTactic("propagate-values");
            Solver solver = ctx.MkSolver(tactic3);

            BitVecExpr rax = ctx.MkBVConst("RAX", 64);
            BitVecExpr rbx = ctx.MkBVConst("RBX", 64);
            BitVecExpr rcx = ctx.MkBVConst("RCX", 64);
            BitVecExpr rdx = ctx.MkBVConst("RDX", 64);

            BitVecExpr zero_64 = ctx.MkBV(0, 64);

            BoolExpr fact1 = ctx.MkEq(rax, zero_64);
            BoolExpr fact2 = ctx.MkEq(rbx, ctx.MkITE(ctx.MkEq(rax, zero_64), rcx, rdx));

            if (false)
            {
                solver.Assert(fact1, fact2);
            }
            else
            {
                Goal goal1 = ctx.MkGoal();
                goal1.Assert(fact1, fact2);
                ApplyResult ar = tactic3.Apply(goal1);
                solver.Assert(ar.Subgoals[0].Formulas);
            }
            Console.WriteLine(solver.ToString());
        }
    }
}