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

namespace AsmDude.QuickInfo {

    /// <summary>
    /// Provides QuickInfo information to be displayed in a text buffer
    /// </summary>
    internal sealed class AsmQuickInfoSource : IQuickInfoSource {

        private readonly ITagAggregator<AsmTokenTag> _aggregator;
        private readonly ITextBuffer _buffer;

        [Import]
        private AsmDudeTools _asmDudeTools = null;

        public AsmQuickInfoSource(ITextBuffer buffer, ITagAggregator<AsmTokenTag> asmTagAggregator) {
            this._aggregator = asmTagAggregator;
            this._buffer = buffer;
            AsmDudeToolsStatic.getCompositionContainer().SatisfyImportsOnce(this);
        }

        /// <summary>
        /// Determine which pieces of Quickinfo content should be displayed
        /// </summary>
        public void AugmentQuickInfoSession(IQuickInfoSession session, IList<object> quickInfoContent, out ITrackingSpan applicableToSpan) {
            applicableToSpan = null;
            try {
                DateTime time1 = DateTime.Now;

                ITextSnapshot snapshot = _buffer.CurrentSnapshot;
                var triggerPoint = (SnapshotPoint)session.GetTriggerPoint(snapshot);
                if (triggerPoint == null) {
                    return;
                }
                string tagString = "";

                foreach (IMappingTagSpan<AsmTokenTag> curTag in this._aggregator.GetTags(new SnapshotSpan(triggerPoint, triggerPoint))) {

                    SnapshotSpan tagSpan = curTag.Span.GetSpans(_buffer).First();
                    tagString = tagSpan.GetText();

                    //AsmDudeToolsStatic.Output(string.Format("INFO: {0}:AugmentQuickInfoSession. tag ", this.ToString(), tagString));
                    string tagStringUpper = tagString.ToUpper();
                    applicableToSpan = snapshot.CreateTrackingSpan(tagSpan, SpanTrackingMode.EdgeExclusive);

                    string description = null;

                    switch (curTag.Tag.type) {
                        case AsmTokenType.Misc: {
                                string descr = this._asmDudeTools.getDescription(tagStringUpper);
                                description = (descr.Length > 0) ? ("Keyword " + tagStringUpper + ": " + descr) : "Keyword " + tagStringUpper;
                                break;
                            }
                        case AsmTokenType.Directive: {
                                string descr = this._asmDudeTools.getDescription(tagStringUpper);
                                description = (descr.Length > 0) ? ("Directive " + tagStringUpper + ": " + descr) : "Directive " + tagStringUpper;
                                break;
                            }
                        case AsmTokenType.Register: {
                                string descr = this._asmDudeTools.getDescription(tagStringUpper);
                                description = (descr.Length > 0) ? (tagStringUpper + ": " + descr) : "Register " + tagStringUpper;
                                break;
                            }
                        case AsmTokenType.Mnemonic: // intentional fall through
                        case AsmTokenType.Jump: {
                                string descr = this._asmDudeTools.getDescription(tagStringUpper);
                                description = (descr.Length > 0) ? ("Mnemonic " + tagStringUpper + ": " + descr) : "Mnemonic " + tagStringUpper;
                                break;
                            }
                        case AsmTokenType.Label: {
                                string descr = AsmDudeToolsStatic.getLabelDescription(tagString, snapshot.GetText());
                                description = (descr.Length > 0) ? descr : "Label " + tagString;
                                break;
                            }
                        case AsmTokenType.LabelDef: {
                                string descr = AsmDudeToolsStatic.getLabelDefDescription(tagString, snapshot.GetText());
                                description = (descr.Length > 0) ? descr : "Label " + tagString;
                                break;
                            }
                        case AsmTokenType.Constant: {
                                description = "Constant " + tagString;
                                break;
                            }
                        default:
                            break;
                    }
                    if (description != null) {
                        quickInfoContent.Add(AsmSourceTools.linewrap(description, AsmDudePackage.maxNumberOfCharsInToolTips+1));
                    }
                }

                double elapsedSec = (double)(DateTime.Now.Ticks - time1.Ticks) / 10000000;
                if (elapsedSec > AsmDudePackage.slowWarningThresholdSec) {
                    AsmDudeToolsStatic.Output(string.Format("WARNING: SLOW: took {0:F3} seconds to retrieve quick info for tag \"{1}\".", elapsedSec, tagString));
                }
            } catch (Exception e) {
                AsmDudeToolsStatic.Output(string.Format("ERROR: {0}:AugmentQuickInfoSession; e={1}", this.ToString(), e.ToString()));
            }
        }

        public void Dispose() {
            //empty
        }
    }
}

