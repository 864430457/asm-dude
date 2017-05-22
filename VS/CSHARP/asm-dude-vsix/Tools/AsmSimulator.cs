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

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.Z3;

using AsmDude.SyntaxHighlighting;
using AsmSim;
using AsmTools;

namespace AsmDude.Tools
{
    public class AsmSimulator
    {
        private readonly ITextBuffer _buffer;
        private readonly ITagAggregator<AsmTokenTag> _aggregator;
        private readonly CFlow _cflow;
        private readonly IDictionary<int, State> _cached_States_After;
        private readonly IDictionary<int, State> _cached_States_Before;
        private readonly IDictionary<int, ExecutionTree> _cached_Tree;

        public readonly AsmSim.Tools Tools;
        private object _updateLock = new object();

        private bool _busy;
        private ISet<int> _scheduled_After;
        private ISet<int> _scheduled_Before;

        public bool Is_Enabled { get; set; }

        #region Constuctors
        /// <summary>Factory return singleton</summary>
        public static AsmSimulator GetOrCreate_AsmSimulator(
            ITextBuffer buffer,
            IBufferTagAggregatorFactoryService aggregatorFactory)
        {
            Func<AsmSimulator> sc = delegate ()
            {
                return new AsmSimulator(buffer, aggregatorFactory);
            };
            return buffer.Properties.GetOrCreateSingletonProperty(sc);
        }

        private AsmSimulator(ITextBuffer buffer, IBufferTagAggregatorFactoryService aggregatorFactory)
        {
            this._buffer = buffer;
            this._aggregator = AsmDudeToolsStatic.GetOrCreate_Aggregator(buffer, aggregatorFactory);

            if (Settings.Default.AsmSim_On)
            {
                AsmDudeToolsStatic.Output_INFO("AsmSimulator:AsmSimulator: swithed on");
                this.Is_Enabled = true;

                this._cflow = new CFlow(this._buffer.CurrentSnapshot.GetText());
                this._cached_States_After = new Dictionary<int, State>();
                this._cached_States_Before = new Dictionary<int, State>();
                this._cached_Tree = new Dictionary<int, ExecutionTree>();
                this._scheduled_After = new HashSet<int>();
                this._scheduled_Before = new HashSet<int>();

                Dictionary<string, string> settings = new Dictionary<string, string> {
                    /*
                    Legal parameters are:
                        auto_config(bool)(default: true)
                        debug_ref_count(bool)(default: false)
                        dump_models(bool)(default: false)
                        model(bool)(default: true)
                        model_validate(bool)(default: false)
                        proof(bool)(default: false)
                        rlimit(unsigned int)(default: 4294967295)
                        smtlib2_compliant(bool)(default: false)
                        timeout(unsigned int)(default: 4294967295)
                        trace(bool)(default: false)
                        trace_file_name(string)(default: z3.log)
                        type_check(bool)(default: true)
                        unsat_core(bool)(default: false)
                        well_sorted_check(bool)(default: false)
                    */
                    { "unsat-core", "false" },    // enable generation of unsat cores
                    { "model", "false" },         // enable model generation
                    { "proof", "false" },         // enable proof generation
                    { "timeout", Settings.Default.AsmSim_Z3_Timeout_MS.ToString()}
                };
                this.Tools = new AsmSim.Tools(settings);
                if (Settings.Default.AsmSim_64_Bits)
                {
                    this.Tools.Parameters.mode_64bit = true;
                    this.Tools.Parameters.mode_32bit = false;
                    this.Tools.Parameters.mode_16bit = false;
                }
                else
                {
                    this.Tools.Parameters.mode_64bit = false;
                    this.Tools.Parameters.mode_32bit = true;
                    this.Tools.Parameters.mode_16bit = false;
                }
                this._buffer.Changed += this.Buffer_Changed;
            }
            else
            {
                AsmDudeToolsStatic.Output_INFO("AsmSimulator:AsmSimulator: swithed off");
                this.Is_Enabled = false;
            }
        }

        #endregion Constructors

        public (bool IsImplemented, Mnemonic Mnemonic, string Message) Get_Syntax_Errors(int lineNumber)
        {
            var dummyKeys = ("", "", "", "");
            var content = this._cflow.Get_Line(lineNumber);
            var opcodeBase = Runner.InstantiateOpcode(content.Mnemonic, content.Args, dummyKeys, this.Tools);
            if (opcodeBase == null) return (IsImplemented: false, Mnemonic: Mnemonic.NONE, Message: null);

            if (opcodeBase.GetType() == typeof(AsmSim.Mnemonics.NotImplemented))
            {
                return (IsImplemented: false, Mnemonic: content.Mnemonic, Message: null);
            }
            else
            {
                return (opcodeBase.IsHalted) 
                    ? (IsImplemented: true, Mnemonic: content.Mnemonic, Message: opcodeBase.SyntaxError) 
                    : (IsImplemented: true, Mnemonic: content.Mnemonic, Message: null);
            }
        }

        public string Get_Usage_Undefined_Warnings(int lineNumber)
        {
          State state = this.Get_State_Before(lineNumber, false, true);

            lock (this._updateLock)
            {
                var dummyKeys = ("", "", "", "");
                var content = this._cflow.Get_Line(lineNumber);
                var opcodeBase = Runner.InstantiateOpcode(content.Mnemonic, content.Args, dummyKeys, this.Tools);

                string message = "";
                if (opcodeBase != null)
                {
                    StateConfig stateConfig = this.Tools.StateConfig;
                    foreach (Flags flag in FlagTools.GetFlags(opcodeBase.FlagsReadStatic))
                    {
                        if (stateConfig.IsFlagOn(flag))
                        {
                            if (state.IsUndefined(flag))
                            {
                                message = message + flag + " is undefined; ";
                            }
                        }
                    }
                    foreach (Rn reg in opcodeBase.RegsReadStatic)
                    {
                        if (stateConfig.IsRegOn(RegisterTools.Get64BitsRegister(reg)))
                        {
                            Tv[] regContent = state.GetTv5Array(reg, true);
                            bool isUndefined = false;
                            foreach (var tv in regContent)
                            {
                                if (tv == Tv.UNDEFINED)
                                {
                                    isUndefined = true;
                                    break;
                                }
                            }
                            if (isUndefined)
                            {
                                message = message + reg + " has undefined content: "+ToolsZ3.ToStringHex(regContent) +" = " + ToolsZ3.ToStringBin(regContent)+"; ";
                            }
                        }
                    }
                }
                return message;
            }
        }

        public string Get_Redundant_Instruction_Warnings(int lineNumber)
        {
            var content = this._cflow.Get_Line(lineNumber);
            if (content.Mnemonic == Mnemonic.NONE) return "";
            if (content.Mnemonic == Mnemonic.NOP) return "";
            if (content.Mnemonic == Mnemonic.UNKNOWN) return "";

            State state_Before = this.Get_State_Before(lineNumber, false, true);
            if (state_Before == null) return "";
            State state_After = this.Get_State_After(lineNumber, false, true);
            if (state_After == null) return "";

            lock (this._updateLock)
            {
                //Context ctx = state_Before.Ctx;
                //AsmSimZ3.Mnemonics_ng.Tools tools2 = new AsmSimZ3.Mnemonics_ng.Tools(this.Tools.Settings);
                Context ctx = this.Tools.Ctx;

                State stateB = new State(state_Before);
                stateB.UpdateConstName("!B");

                State stateA = new State(state_After);
                stateA.UpdateConstName("!A");

                State diffState = new State(this.Tools, "!0", "!0");
                foreach (var v in stateB.Solver.Assertions)
                {
                    diffState.Solver.Assert(v as BoolExpr);
                }
                foreach (var v in stateA.Solver.Assertions)
                {
                    diffState.Solver.Assert(v as BoolExpr);
                }

                foreach (Flags flag in this.Tools.StateConfig.GetFlagOn())
                {
                    diffState.Solver.Assert(ctx.MkEq(stateB.GetTail(flag), stateA.GetTail(flag)));
                }
                foreach (Rn reg in this.Tools.StateConfig.GetRegOn())
                {
                    diffState.Solver.Assert(ctx.MkEq(stateB.GetTail(reg), stateA.GetTail(reg)));
                }
                diffState.Solver.Assert(ctx.MkEq(AsmSim.Tools.Mem_Key(stateB.TailKey, ctx), AsmSim.Tools.Mem_Key(stateA.TailKey, ctx)));
                //AsmDudeToolsStatic.Output_INFO(diffState.ToString());

                StateConfig written = Runner.GetUsage_StateConfig(this._cflow, lineNumber, lineNumber, this.Tools);

                foreach (Flags flag in written.GetFlagOn())
                {
                    BoolExpr value = ctx.MkEq(stateB.Get(flag), stateA.Get(flag));
                    Tv tv = ToolsZ3.GetTv(value, diffState.Solver, ctx);
                    //AsmDudeToolsStatic.Output_INFO("AsmSimulator: Get_Redundant_Instruction_Warnings: line " + lineNumber + ": tv=" + tv + "; value=" + value);
                    if (tv != Tv.ONE) return "";
                }
                foreach (Rn reg in written.GetRegOn())
                {
                    BoolExpr value = ctx.MkEq(stateB.Get(reg), stateA.Get(reg));
                    Tv tv = ToolsZ3.GetTv(value, diffState.Solver, ctx);
                    //AsmDudeToolsStatic.Output_INFO("AsmSimulator: Get_Redundant_Instruction_Warnings: line " + lineNumber + ":tv=" + tv + "; value=" + value);
                    if (tv != Tv.ONE) return "";
                }
                if (written.mem) {
                    BoolExpr value = ctx.MkEq(AsmSim.Tools.Mem_Key(stateB.HeadKey, ctx), AsmSim.Tools.Mem_Key(stateA.HeadKey, ctx));
                    Tv tv = ToolsZ3.GetTv(value, diffState.Solver, ctx);
                    //AsmDudeToolsStatic.Output_INFO("AsmSimulator: Get_Redundant_Instruction_Warnings: line " + lineNumber + ":tv=" + tv + "; value=" + value);
                    if (tv != Tv.ONE) return "";
                }
            }
            string message = "\"" + this._cflow.Get_Line_Str(lineNumber) + "\" is redundant.";
            AsmDudeToolsStatic.Output_INFO("AsmSimulator: Has_Redundant_Instruction_Warnings: lineNumber " + lineNumber + ": "+ message);
            return message;
        }

        public string Get_Register_Value(Rn name, State state)
        {
            if (!this.Is_Enabled) return "";
            if (state == null) return "";

            Tv[] reg = null;
            lock (this._updateLock)
            {
                reg = state.GetTv5Array(name);
            }

            if (false)
            {
                return string.Format("{0} = {1}\n{2}", ToolsZ3.ToStringHex(reg), ToolsZ3.ToStringBin(reg), state.ToStringConstraints("") + state.ToStringRegs("") + state.ToStringFlags(""));
            }
            else
            {
                return string.Format("{0} = {1}", ToolsZ3.ToStringHex(reg), ToolsZ3.ToStringBin(reg));
            }
        }

        public bool Has_Register_Value(Rn name, State state)
        {
            //TODO 
            if (true)
            {
                return true;
            }
            else
            {
                // this code throw a AccessViolationException, probably due because Z3 does not multithread
                Tv[] content = state.GetTv5Array(name, true);
                foreach (Tv tv in content)
                {
                    if ((tv == Tv.ONE) || (tv == Tv.ZERO) || (tv == Tv.UNDEFINED)) return true;
                }
                return false;
            }
        }

        /// <summary>If async is false, return the state of the provided lineNumber.
        /// If async is true, returns the state of the provided lineNumber when it exists in the case, 
        /// returns null otherwise and schedules its computation. 
        /// if the state is not computed yet, 
        /// return null and create one in a different thread according to the provided createState boolean.</summary>
        public State Get_State_After(int lineNumber, bool async, bool create)
        {
            if (!this.Is_Enabled) return null;

            if (this._cached_States_After.TryGetValue(lineNumber, out State result))
            {
                return result;
            }
            if (create)
            {
                if (async)
                {
                    if (this._busy)
                    {
                        if (this._scheduled_After.Contains(lineNumber))
                        {
                            AsmDudeToolsStatic.Output_INFO("AsmSimulator:Get_State_After: busy; and line " + lineNumber + " is already scheduled");
                        }
                        else
                        {
                            AsmDudeToolsStatic.Output_INFO("AsmSimulator:Get_State_After: busy; scheduling line " + lineNumber);
                            this._scheduled_Before.Add(lineNumber);
                        }
                    }
                    else
                    {
                        AsmDudeToolsStatic.Output_INFO("AsmSimulator:Get_State_After: going to execute this in a different thread.");
                        AsmDudeTools.Instance.Thread_Pool.QueueWorkItem(this.Calculate_State_After, lineNumber, true);
                    }
                }
                else
                {
                    this.Calculate_State_After(lineNumber, false);
                    return (this._cached_States_After.TryGetValue(lineNumber, out State result2)) ? result2 : null;
                }
            }
            return null;
        }

        public State Get_State_Before(int lineNumber, bool async, bool create)
        {
            if (!this.Is_Enabled) return null;
            if (this._cached_States_Before.TryGetValue(lineNumber, out State result))
            {
                if (result == null)
                {
                    AsmDudeToolsStatic.Output_WARNING("AsmSimulator:Get_State_Before: serving state from cache but it is null!");
                }
                return result;
            }
            if (create)
            {
                if (async)
                {
                    if (this._busy)
                    {
                        if (this._scheduled_Before.Contains(lineNumber))
                        {
                            AsmDudeToolsStatic.Output_INFO("AsmSimulator:Get_State_Before: busy; already scheduled line " + lineNumber);
                        }
                        else
                        {
                            AsmDudeToolsStatic.Output_INFO("AsmSimulator:Get_State_Before: busy; scheduling this line " + lineNumber);
                            this._scheduled_Before.Add(lineNumber);
                        }
                    }
                    else
                    {
                        AsmDudeToolsStatic.Output_INFO("AsmSimulator:Get_State_Before: going to execute this in a different thread.");
                        AsmDudeTools.Instance.Thread_Pool.QueueWorkItem(this.Calculate_State_Before, lineNumber, async);
                    }
                }
                else
                {
                    this.Calculate_State_Before(lineNumber, false);
                    this._cached_States_Before.TryGetValue(lineNumber, out State result2);
                    return result2;
                }
            }
            return null;
        }

        #region Private

        private void Buffer_Changed(object sender, TextContentChangedEventArgs e)
        {
            //AsmDudeToolsStatic.Output_INFO("AsmSimulation:Buffer_Changed");

            bool nonSpaceAdded = false;
            foreach (var c in e.Changes)
            {
                if (c.NewText != " ") nonSpaceAdded = true;
            }
            if (!nonSpaceAdded) return;

            string sourceCode = this._buffer.CurrentSnapshot.GetText();
            if (this._cflow.Update(sourceCode))
            {
                this._cached_States_After.Clear();
                this._cached_States_Before.Clear();
                this._scheduled_After.Clear();
                this._scheduled_Before.Clear();
            }
        }

        private void Calculate_State_Before(int lineNumber, bool async)
        {
            lock (this._updateLock)
            {
                this._busy = true;
                var tree = Get_Tree(lineNumber);

                if (tree != null)
                {
                    var state = Get_State_Before(lineNumber, tree);
                    this._cached_States_Before.Remove(lineNumber);
                    if (state != null) this._cached_States_Before.Add(lineNumber, state);
                } else
                {
                    AsmDudeToolsStatic.Output_INFO("AsmSimulator: Calculate_State_Before: tree for lineNumber " + lineNumber + " is null");
                }
                this._scheduled_Before.Remove(lineNumber);
                this._busy = false;
            }
            if (async)
            {
                if (this._scheduled_Before.Count > 0)
                {
                    int lineNumber2;
                    lock (this._updateLock)
                    {
                        lineNumber2 = this._scheduled_Before.GetEnumerator().Current;
                        this._scheduled_Before.Remove(lineNumber2);
                    }
                    this.Calculate_State_Before(lineNumber2, true);
                }
            }            
        }

        private void Calculate_State_After(int lineNumber, bool async)
        {
            lock (this._updateLock)
            {
                this._busy = true;
                var tree = Get_Tree(lineNumber);

                if (tree != null)
                {
                    var state = Get_State_After(lineNumber, tree);
                    if (state != null) this._cached_States_After.Remove(lineNumber);
                    this._cached_States_After.Add(lineNumber, state);
                }
                this._scheduled_After.Remove(lineNumber);
                this._busy = false;
            }
            if (async)
            {
                if (this._scheduled_After.Count > 0)
                {
                    int lineNumber2;
                    lock (this._updateLock)
                    {
                        lineNumber2 = this._scheduled_After.GetEnumerator().Current;
                        this._scheduled_After.Remove(lineNumber2);
                    }
                    this.Calculate_State_After(lineNumber2, true);
                }
            }            
        }

        private ExecutionTree Get_Tree(int lineNumber)
        {
            if (this._cached_Tree.TryGetValue(lineNumber, out ExecutionTree result))
            {
                return result;
            }
            else
            {
                this.Tools.StateConfig = Runner.GetUsage_StateConfig(this._cflow, 0, this._cflow.LastLineNumber, this.Tools);
                result = Runner.Construct_ExecutionTree_Backward(this._cflow, lineNumber, Settings.Default.AsmSim_Number_Of_Steps, this.Tools);
                if (result != null) this._cached_Tree.Add(lineNumber, result);
                return result;
            }
        }

        private State Get_State_After(int lineNumber, ExecutionTree tree)
        {
            State result = AsmSim.Tools.Collapse(tree.States_After(lineNumber));
            AsmDudeToolsStatic.Output_INFO("AsmSimulator:Get_State_After: lineNumber " + lineNumber + "\nTree=" + tree.ToString(this._cflow) + "\nState=" + result);
            return result;
        }

        private State Get_State_Before(int lineNumber, ExecutionTree tree)
        {           
            //AsmDudeToolsStatic.Output_INFO("AsmSimulator:Get_State_Before: retrieving state at lineNumber "+lineNumber +" from tree");
            //IList<State> before = new List<State>(tree.States_Before(lineNumber));
            // AsmSim.Tools.Collapse(before);
            return AsmSim.Tools.Collapse(tree.States_Before(lineNumber));
        }

        #endregion Private
    }
}
