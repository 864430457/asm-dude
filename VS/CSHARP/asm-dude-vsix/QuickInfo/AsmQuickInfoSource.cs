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
using System.Linq;
using System.Collections.Generic;

using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using System.ComponentModel.Composition;
using AsmDude.SyntaxHighlighting;
using System.Text;
using AsmTools;
using AsmDude.Tools;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Drawing;
using System.Windows.Media;

namespace AsmDude.QuickInfo {

    /// <summary>
    /// Provides QuickInfo information to be displayed in a text buffer
    /// </summary>
    internal sealed class AsmQuickInfoSource : IQuickInfoSource {

        private readonly ITagAggregator<AsmTokenTag> _aggregator;
        private readonly ITextBuffer _sourceBuffer;
        private readonly ILabelGraph _labelGraph;

        [Import]
        private AsmDudeTools _asmDudeTools = null;

        public object CSharpEditorResources { get; private set; }

        public AsmQuickInfoSource(ITextBuffer buffer, ITagAggregator<AsmTokenTag> aggregator) {
            this._aggregator = aggregator;
            this._sourceBuffer = buffer;

            AsmDudeToolsStatic.getCompositionContainer().SatisfyImportsOnce(this);
            this._labelGraph = new LabelGraph(buffer, aggregator);
        }

        private static Run makeRun1(string str) {
            Run r1 = new Run(str);
            r1.FontWeight = FontWeights.Bold;
            return r1;
        }

        private static Run makeRun2(string str, System.Drawing.Color color) {
            Run r1 = new Run(str);
            r1.FontWeight = FontWeights.Bold;
            r1.Foreground = new SolidColorBrush(AsmDudeToolsStatic.convertColor(color));
            return r1;
        }


        /// <summary>
        /// Determine which pieces of Quickinfo content should be displayed
        /// </summary>
        public void AugmentQuickInfoSession(IQuickInfoSession session, IList<object> quickInfoContent, out ITrackingSpan applicableToSpan) {
            applicableToSpan = null;
            try {
                DateTime time1 = DateTime.Now;

                ITextSnapshot snapshot = _sourceBuffer.CurrentSnapshot;
                var triggerPoint = (SnapshotPoint)session.GetTriggerPoint(snapshot);
                if (triggerPoint == null) {
                    return;
                }
                string keyword = "";
                IEnumerable<IMappingTagSpan<AsmTokenTag>> enumerator = this._aggregator.GetTags(new SnapshotSpan(triggerPoint, triggerPoint));

                if (enumerator.Count() > 0) {

                    if (enumerator.Count() > 1) {
                        AsmDudeToolsStatic.Output(string.Format("WARNING: {0}:AugmentQuickInfoSession. more than one tag! \"{1}\"", this.ToString(), enumerator.ElementAt(1).ToString()));
                    }


                    IMappingTagSpan<AsmTokenTag> asmTokenTag = enumerator.First();
                    SnapshotSpan tagSpan = asmTokenTag.Span.GetSpans(_sourceBuffer).First();
                    keyword = tagSpan.GetText();

                    //AsmDudeToolsStatic.Output(string.Format("INFO: {0}:AugmentQuickInfoSession. keyword=\"{1}\"", this.ToString(), keyword));
                    string keywordUpper = keyword.ToUpper();
                    applicableToSpan = snapshot.CreateTrackingSpan(tagSpan, SpanTrackingMode.EdgeExclusive);

                    TextBlock description = null;

                    switch (asmTokenTag.Tag.type) {
                        case AsmTokenType.Misc: {
                                description = new TextBlock();
                                description.Inlines.Add(makeRun1("Keyword "));
                                description.Inlines.Add(makeRun2(keyword, Settings.Default.SyntaxHighlighting_Misc));

                                string descr = this._asmDudeTools.getDescription(keywordUpper);
                                if (descr.Length > 0) {
                                    description.Inlines.Add(new Run(": " + descr));
                                }
                                break;
                            }
                        case AsmTokenType.Directive: {
                                description = new TextBlock();
                                description.Inlines.Add(makeRun1("Directive "));
                                description.Inlines.Add(makeRun2(keyword, Settings.Default.SyntaxHighlighting_Directive));

                                string descr = this._asmDudeTools.getDescription(keywordUpper);
                                if (descr.Length > 0) {
                                    description.Inlines.Add(new Run(": " + descr));
                                }
                                break;
                            }
                        case AsmTokenType.Register: {
                                description = new TextBlock();
                                description.Inlines.Add(makeRun1("Register "));
                                description.Inlines.Add(makeRun2(keyword, Settings.Default.SyntaxHighlighting_Register));

                                string descr = this._asmDudeTools.getDescription(keywordUpper);
                                if (descr.Length > 0) {
                                    description.Inlines.Add(new Run(": " + descr));
                                }
                                break;
                            }
                        case AsmTokenType.Mnemonic: {
                                description = new TextBlock();
                                description.Inlines.Add(makeRun1("Mnemonic "));
                                description.Inlines.Add(makeRun2(keyword, Settings.Default.SyntaxHighlighting_Opcode));

                                string descr = this._asmDudeTools.getDescription(keywordUpper);
                                if (descr.Length > 0) {
                                    description.Inlines.Add(new Run(": " + descr));
                                }
                                break;
                            }
                        case AsmTokenType.Jump: {
                                description = new TextBlock();
                                description.Inlines.Add(makeRun1("Mnemonic "));
                                description.Inlines.Add(makeRun2(keyword, Settings.Default.SyntaxHighlighting_Jump));

                                string descr = this._asmDudeTools.getDescription(keywordUpper);
                                if (descr.Length > 0) {
                                    description.Inlines.Add(new Run(": " + descr));
                                }
                                break;
                            }
                        case AsmTokenType.Label: {
                                description = new TextBlock();
                                description.Inlines.Add(makeRun1("Label "));
                                description.Inlines.Add(makeRun2(keyword, Settings.Default.SyntaxHighlighting_Label));

                                string descr = this.getLabelDescription(keyword);
                                if (descr.Length > 0) {
                                    description.Inlines.Add(new Run(": " + descr));
                                }
                                break;
                            }
                        case AsmTokenType.LabelDef: {
                                description = new TextBlock();
                                description.Inlines.Add(makeRun1("Label "));
                                description.Inlines.Add(makeRun2(keyword, Settings.Default.SyntaxHighlighting_Label));

                                string descr = this.getLabelDefDescription(keyword);
                                if (descr.Length > 0) {
                                    description.Inlines.Add(new Run(": " + descr));
                                }
                                break;
                            }
                        //case AsmTokenType.Constant: {
                        //        description = "Constant " + keyword;
                        //        break;
                        //    }
                        default:
                            //description = "Unused tagType " + asmTokenTag.Tag.type;
                            break;
                    }
                    if (description != null) {
                        quickInfoContent.Add(description);
                    }
                }

                double elapsedSec = (double)(DateTime.Now.Ticks - time1.Ticks) / 10000000;
                if (elapsedSec > AsmDudePackage.slowWarningThresholdSec) {
                    AsmDudeToolsStatic.Output(string.Format("WARNING: SLOW: took QuickInfo {0:F3} seconds to retrieve info for keyword \"{1}\".", elapsedSec, keyword));
                }
            } catch (Exception e) {
                AsmDudeToolsStatic.Output(string.Format("ERROR: {0}:AugmentQuickInfoSession; e={1}", this.ToString(), e.ToString()));
            }
        }

        private FrameworkElement CreateContent(string content) {
            var textBlock = new TextBlock();

            var aRun = new Run(content);
            aRun.FontWeight = FontWeights.Normal;


            //aRun.Foreground = new SolidColorBrush(Colors.Blue);
            aRun.Foreground = new SolidColorBrush(AsmDudeToolsStatic.convertColor(Settings.Default.SyntaxHighlighting_Jump));
            textBlock.Inlines.Add(aRun);

            return textBlock;
        }


        private string getLabelDescription(string label) {
            if (this._labelGraph.isEnabled) {
                StringBuilder sb = new StringBuilder();
                foreach (int lineNumber in this._labelGraph.getLabelDefLineNumbers(label)) {
                    sb.AppendLine(AsmDudeToolsStatic.cleanup(string.Format("Label defined at LINE {0}: {1}", lineNumber + 1, this.getLineContent(lineNumber))));
                }
                string result = sb.ToString();
                return result.TrimEnd(Environment.NewLine.ToCharArray());
            } else {
                return "Label analysis is disabled";
            }
        }

        private string getLabelDefDescription(string label) {
            if (this._labelGraph.isEnabled) {
                SortedSet<int> usage = this._labelGraph.labelUsedAtInfo(label);
                if (usage.Count > 0) {
                    StringBuilder sb = new StringBuilder();
                    foreach (int lineNumber in usage) {
                        sb.AppendLine(AsmDudeToolsStatic.cleanup(string.Format("Label used at LINE {0}: {1}", lineNumber + 1, this.getLineContent(lineNumber))));
                        //AsmDudeToolsStatic.Output(string.Format("INFO: {0}:getLabelDefDescription; sb=\"{1}\"", this.ToString(), sb.ToString()));
                    }
                    string result = sb.ToString();
                    return result.TrimEnd(Environment.NewLine.ToCharArray());
                } else {
                    return AsmDudeToolsStatic.cleanup(string.Format("Unused Label {0}", label));
                }
            } else {
                return "Label analysis is disabled";
            }
        }

        private string getLineContent(int lineNumber) {
            return this._sourceBuffer.CurrentSnapshot.GetLineFromLineNumber(lineNumber).GetText();
        }

        public void Dispose() {
            //empty
        }
    }
}

