﻿// The MIT License (MIT)
//
// Copyright (c) 2016 H.J. Lebbink
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
using System.Linq;
using System.IO;

using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using System.Windows.Media;

using AsmTools;
using AsmDude.Tools;
using AsmDude.SignatureHelp;

namespace AsmDude {

    public sealed class CompletionComparer : IComparer<Completion> {
        public int Compare(Completion x, Completion y) {
            return x.InsertionText.CompareTo(y.InsertionText);
        }
    }

    public sealed class AsmCompletionSource : ICompletionSource {

        private readonly ITextBuffer _buffer;
        private readonly ILabelGraph _labelGraph;
        private readonly IDictionary<AsmTokenType, ImageSource> _icons;
        private readonly AsmDudeTools _asmDudeTools;
        private bool _disposed = false;

        public AsmCompletionSource(ITextBuffer buffer, ILabelGraph labelGraph) {
            this._buffer = buffer;
            this._labelGraph = labelGraph;
            this._icons = new Dictionary<AsmTokenType, ImageSource>();
            this._asmDudeTools = AsmDudeTools.Instance;
            this.loadIcons();
        }

        public void AugmentCompletionSession(ICompletionSession session, IList<CompletionSet> completionSets) {
            //AsmDudeToolsStatic.Output(string.Format("INFO: {0}:AugmentCompletionSession", this.ToString()));

            if (_disposed) return;
            if (!Settings.Default.CodeCompletion_On) return;

            try {
                DateTime time1 = DateTime.Now;
                ITextSnapshot snapshot = this._buffer.CurrentSnapshot;
                SnapshotPoint triggerPoint = (SnapshotPoint)session.GetTriggerPoint(snapshot);
                if (triggerPoint == null) {
                    return;
                }
                ITextSnapshotLine line = triggerPoint.GetContainingLine();

                //1] check if current position is in a remark; if we are in a remark, no code completion
                #region
                if (triggerPoint.Position > 1) {
                    char currentTypedChar = (triggerPoint - 1).GetChar();
                    //AsmDudeToolsStatic.Output(string.Format("INFO: {0}:AugmentCompletionSession: current char = {1}", this.ToString(), currentTypedChar));
                    if (!currentTypedChar.Equals('#')) { //TODO UGLY since the user can configure this starting character
                        int pos = triggerPoint.Position - line.Start;
                        if (AsmSourceTools.isInRemark(pos, line.GetText())) {
                            return;
                        }
                    }
                }
                #endregion

                //2] find the start of the current keyword
                SnapshotPoint start = triggerPoint;
                while ((start > line.Start) && !AsmTools.AsmSourceTools.isSeparatorChar((start - 1).GetChar())) {
                    start -= 1;
                }

                //3] get the word that is currently being typed
                ITrackingSpan applicableTo = snapshot.CreateTrackingSpan(new SnapshotSpan(start, triggerPoint), SpanTrackingMode.EdgeInclusive);
                string partialKeyword = applicableTo.GetText(snapshot);
                bool useCapitals = AsmDudeToolsStatic.isAllUpper(partialKeyword);

                SortedSet<Completion> completions = null;
                if (false) {
                    #region 
                    //4] get the previous keyword to narrow down the possible suggestions
                    string previousKeyword = AsmDudeToolsStatic.getPreviousKeyword(line.Start, start);
                    //AsmDudeToolsStatic.Output(string.Format("INFO: AugmentCompletionSession: previousKeyword=\"{0}\"; partialKeyword=\"{1}\".", previousKeyword, partialKeyword));

                    if ((previousKeyword.Length == 0) || this.isLabel(previousKeyword)) {
                        // no previous keyword exists. Do not suggest a register
                        HashSet<AsmTokenType> selected = new HashSet<AsmTokenType> { AsmTokenType.Directive, AsmTokenType.Jump, AsmTokenType.Misc, AsmTokenType.Mnemonic /*, TokenType.Register*/ };
                        completions = this.selectedCompletions(useCapitals, selected);

                    } else if (this.isJump(previousKeyword)) {
                        // previous keyword is jump (or call) mnemonic. Suggest "SHORT" or a label
                        completions = this.labelCompletions();
                        completions.Add(new Completion("SHORT", (useCapitals) ? "SHORT" : "short", null, this._icons[AsmTokenType.Misc], ""));
                        completions.Add(new Completion("NEAR", (useCapitals) ? "NEAR" : "near", null, this._icons[AsmTokenType.Misc], ""));

                    } else if (previousKeyword.Equals("SHORT") || previousKeyword.Equals("NEAR")) {
                        // previous keyword is SHORT. Suggest a label
                        completions = this.labelCompletions();

                    } else if (this.isRegister(previousKeyword)) {
                        // if the previous keyword is a register, suggest registers (of equal size), no opcodes and no directives
                        HashSet<AsmTokenType> selected = new HashSet<AsmTokenType> { /*TokenType.Directive, TokenType.Jump,*/ AsmTokenType.Misc, /*TokenType.Mnemonic,*/ AsmTokenType.Register };
                        completions = this.selectedCompletions(useCapitals, selected);

                    } else if (this.isMnemonic(previousKeyword)) {
                        // previous keyword is a mnemonic (no jump).
                        HashSet<AsmTokenType> selected = new HashSet<AsmTokenType> { /*TokenType.Directive, TokenType.Jump,*/ AsmTokenType.Misc, /*TokenType.Mnemonic,*/ AsmTokenType.Register };
                        completions = this.selectedCompletions(useCapitals, selected);
                    } else {
                        HashSet<AsmTokenType> selected = new HashSet<AsmTokenType> { AsmTokenType.Directive, AsmTokenType.Jump, AsmTokenType.Misc, AsmTokenType.Mnemonic, AsmTokenType.Register };
                        completions = this.selectedCompletions(useCapitals, selected);
                    }
                    #endregion
                } else {
                    #region
                    string lineStr = line.GetText();
                    var t = AsmSourceTools.parseLine(lineStr);
                    Mnemonic mnemonic = t.Item2;

                    //AsmDudeToolsStatic.Output("INFO: AsmCompletionSource:AugmentCompletionSession; mnemonic="+ mnemonic);

                    if (mnemonic == Mnemonic.UNKNOWN) {
                        HashSet<AsmTokenType> selected = new HashSet<AsmTokenType> { AsmTokenType.Directive, AsmTokenType.Jump, AsmTokenType.Misc, AsmTokenType.Mnemonic, AsmTokenType.Register };
                        completions = this.selectedCompletions(useCapitals, selected);
                    } else {
                        string previousKeyword = AsmDudeToolsStatic.getPreviousKeyword(line.Start, start);
                        if (this.isJump(previousKeyword)) {
                            // previous keyword is jump (or call) mnemonic. Suggest "SHORT" or a label
                            completions = this.labelCompletions();
                            completions.Add(new Completion("SHORT", (useCapitals) ? "SHORT" : "short", null, this._icons[AsmTokenType.Misc], ""));
                            completions.Add(new Completion("NEAR", (useCapitals) ? "NEAR" : "near", null, this._icons[AsmTokenType.Misc], ""));
                        } else if (previousKeyword.Equals("SHORT") || previousKeyword.Equals("NEAR")) {
                            // previous keyword is SHORT. Suggest a label
                            completions = this.labelCompletions();
                        } else {
                            IList<Operand> operands = AsmSourceTools.makeOperands(t.Item3);
                            ISet<AsmSignatureEnum> allowed = new HashSet<AsmSignatureEnum>();
                            int commaCount = AsmSignature.countCommas(lineStr);
                            IList<AsmSignatureElement> allSignatures = this._asmDudeTools.signatureStore.get(mnemonic);

                            ISet<Arch> selectedArchitectures = AsmDudeToolsStatic.getArchSwithedOn();
                            foreach (AsmSignatureElement se in AsmSignatureHelpSource.constrainSignatures(allSignatures, operands, selectedArchitectures)) {
                                if (commaCount < se.operands.Count) {
                                    foreach (AsmSignatureEnum s in se.operands[commaCount]) {
                                        allowed.Add(s);
                                    }
                                }
                            }
                            completions = this.mnemonicOperandCompletions(useCapitals, allowed);
                        }
                    }
                    #endregion
                }
                completionSets.Add(new CompletionSet("Tokens", "Tokens", applicableTo, completions, Enumerable.Empty<Completion>()));

                AsmDudeToolsStatic.printSpeedWarning(time1, "Code Completion");
            } catch (Exception e) {
                AsmDudeToolsStatic.Output(string.Format("ERROR: {0}:AugmentCompletionSession; e={1}", this.ToString(), e.ToString()));
            }
        }

        public void Dispose() {
            if (!this._disposed) {
                GC.SuppressFinalize(this);
                _disposed = true;
            }
        }

        #region Private Methods

        private SortedSet<Completion> mnemonicOperandCompletions(bool useCapitals, ISet<AsmSignatureEnum> allowedOperands) {

            SortedSet<Completion> completions = new SortedSet<Completion>(new CompletionComparer());
            foreach (string keyword in this._asmDudeTools.getKeywords()) {

                Arch arch = this._asmDudeTools.getArchitecture(keyword);
                AsmTokenType type = this._asmDudeTools.getTokenType(keyword);

                bool selected = AsmDudeToolsStatic.isArchSwitchedOn(arch);

                if (selected) {
                    switch (type) {
                        case AsmTokenType.Register:
                            Rn regName = RegisterTools.parseRn(keyword);
                            if (AsmSignatureTools.isAllowedReg(regName, allowedOperands)) {
                                //AsmDudeToolsStatic.Output(string.Format("INFO: AsmCompletionSource:mnemonicOperandCompletions; rn="+ keyword + " is selected"));
                            } else {
                                selected = false;
                            }
                            break;
                        case AsmTokenType.Misc:
                            if (AsmSignatureTools.isAllowedMisc(keyword, allowedOperands)) {
                                //AsmDudeToolsStatic.Output(string.Format("INFO: AsmCompletionSource:mnemonicOperandCompletions; rn="+ keyword + " is selected"));
                            } else {
                                selected = false;
                            }
                            break;
                        default:
                            selected = false;
                            break;
                    }
                }
                if (selected) {
                    //AsmDudeToolsStatic.Output("INFO: AsmCompletionSource:AugmentCompletionSession: keyword \"" + keyword + "\" is added to the completions list");

                    // by default, the entry.Key is with capitals
                    string insertionText = (useCapitals) ? keyword : keyword.ToLower();
                    string archStr = (arch == Arch.NONE) ? "" : " [" + arch + "]";
                    string descriptionStr = this._asmDudeTools.getDescription(keyword);
                    descriptionStr = (descriptionStr.Length == 0) ? "" : " - " + descriptionStr;
                    String displayText = keyword + archStr + descriptionStr;
                    //String description = keyword.PadRight(15) + archStr.PadLeft(8) + descriptionStr;

                    ImageSource imageSource = null;
                    this._icons.TryGetValue(type, out imageSource);
                    completions.Add(new Completion(displayText, insertionText, null, imageSource, ""));
                }
            }
            return completions;
        }

        private SortedSet<Completion> labelCompletions() {
            SortedSet<Completion> completions = new SortedSet<Completion>(new CompletionComparer());
            ImageSource imageSource = this._icons[AsmTokenType.Label];

            SortedDictionary<string, string> labels = this._labelGraph.getLabelDescriptions;
            foreach (KeyValuePair<string, string> entry in labels) {
                //Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "INFO:{0}:AugmentCompletionSession; label={1}; description={2}", this.ToString(), entry.Key, entry.Value));
                string displayText = entry.Key + " - " + entry.Value;
                string insertionText = entry.Key;
                completions.Add(new Completion(displayText, insertionText, null, imageSource, ""));
            }
            return completions;
        }

        private SortedSet<Completion> selectedCompletions(bool useCapitals, HashSet<AsmTokenType> selectedTypes) {
            SortedSet<Completion> completions = new SortedSet<Completion>(new CompletionComparer());

            #region Add the completions of AsmDude directives (such as code folding directives)
            if (Settings.Default.CodeFolding_On) {
                {
                    string insertionText = Settings.Default.CodeFolding_BeginTag;     //the characters that start the outlining region
                    string description = insertionText + " - keyword to start code folding";
                    completions.Add(new Completion(description, insertionText, null, this._icons[AsmTokenType.Directive], ""));
                }
                {
                    string insertionText = Settings.Default.CodeFolding_EndTag;       //the characters that end the outlining region
                    string description = insertionText + " - keyword to end code folding";
                    completions.Add(new Completion(description, insertionText, null, this._icons[AsmTokenType.Directive], ""));
                }
            }
            #endregion
            AssemblerEnum usedAssember = AsmDudeToolsStatic.usedAssembler;


            #region Add the completions that are defined in the xml file
            foreach (string keyword in this._asmDudeTools.getKeywords()) {
                AsmTokenType type = this._asmDudeTools.getTokenType(keyword);
                if (selectedTypes.Contains(type)) {
                    Arch arch = this._asmDudeTools.getArchitecture(keyword);
                    bool selected = AsmDudeToolsStatic.isArchSwitchedOn(arch);

                    if (selected && (type == AsmTokenType.Directive)) {
                        AssemblerEnum assembler = this._asmDudeTools.getAssembler(keyword);
                        switch (assembler) {
                            case AssemblerEnum.MASM: if (usedAssember != AssemblerEnum.MASM) selected = false; break;
                            case AssemblerEnum.NASM: if (usedAssember != AssemblerEnum.NASM) selected = false; break;
                            case AssemblerEnum.UNKNOWN:
                            default:
                                break;
                        }
                    }
                    //AsmDudeToolsStatic.Output(string.Format(CultureInfo.CurrentCulture, "INFO:{0}:AugmentCompletionSession; keyword={1}; arch={2}; selected={3}", this.ToString(), keyword, arch, selected));

                    if (selected) {
                        //Debug.WriteLine("INFO: CompletionSource:AugmentCompletionSession: name keyword \"" + entry.Key + "\"");

                        // by default, the entry.Key is with capitals
                        string insertionText = (useCapitals) ? keyword : keyword.ToLower();
                        string archStr = (arch == Arch.NONE) ? "" : " [" + arch + "]";
                        string descriptionStr = this._asmDudeTools.getDescription(keyword);
                        descriptionStr = (descriptionStr.Length == 0) ? "" : " - " + descriptionStr;
                        String displayText = keyword + archStr + descriptionStr;
                        //String description = keyword.PadRight(15) + archStr.PadLeft(8) + descriptionStr;

                        ImageSource imageSource = null;
                        this._icons.TryGetValue(type, out imageSource);
                        completions.Add(new Completion(displayText, insertionText, null, imageSource, ""));
                    }
                }
            }
            #endregion

            return completions;
        }

        private bool isJump(string previousKeyword) {
            return this._asmDudeTools.isJumpMnenomic(previousKeyword);
        }

        private bool isRegister(string previousKeyword) {
            return AsmTools.RegisterTools.isRegister(previousKeyword);
        }

        private bool isMnemonic(string previousKeyword) {
            return this._asmDudeTools.isMnemonic(previousKeyword);
        }

        private bool isLabel(string previousKeyword) {
            return false;
            //TODO
        }

        private void loadIcons() {
            Uri uri = null;
            string installPath = AsmDudeToolsStatic.getInstallPath();
            try {
                uri = new Uri(installPath + "Resources/images/icon-R-blue.png");
                this._icons[AsmTokenType.Register] = AsmDudeToolsStatic.bitmapFromUri(uri);
            } catch (FileNotFoundException) {
                //MessageBox.Show("ERROR: AsmCompletionSource: could not find file \"" + uri.AbsolutePath + "\".");
            }
            try {
                uri = new Uri(installPath + "Resources/images/icon-M.png");
                this._icons[AsmTokenType.Mnemonic] = AsmDudeToolsStatic.bitmapFromUri(uri);
                this._icons[AsmTokenType.Jump] = this._icons[AsmTokenType.Mnemonic];
            } catch (FileNotFoundException) {
                //MessageBox.Show("ERROR: AsmCompletionSource: could not find file \"" + uri.AbsolutePath + "\".");
            }
            try {
                uri = new Uri(installPath + "Resources/images/icon-question.png");
                this._icons[AsmTokenType.Misc] = AsmDudeToolsStatic.bitmapFromUri(uri);
                this._icons[AsmTokenType.Directive] = this._icons[AsmTokenType.Misc];
            } catch (FileNotFoundException) {
                //MessageBox.Show("ERROR: AsmCompletionSource: could not find file \"" + uri.AbsolutePath + "\".");
            }
            try {
                uri = new Uri(installPath + "Resources/images/icon-L.png");
                this._icons[AsmTokenType.Label] = AsmDudeToolsStatic.bitmapFromUri(uri);
            } catch (FileNotFoundException) {
                //MessageBox.Show("ERROR: AsmCompletionSource: could not find file \"" + uri.AbsolutePath + "\".");
            }
        }
        #endregion
    }
}

