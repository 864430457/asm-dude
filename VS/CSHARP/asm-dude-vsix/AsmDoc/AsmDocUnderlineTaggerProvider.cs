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

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;

namespace AsmDude.AsmDoc
{
    [Export(typeof(IViewTaggerProvider))]
    [ContentType(AsmDudePackage.AsmDudeContentType)]
    [ContentType(AsmDudePackage.DisassemblyContentType)]
    [Name("AsmDocUnderlineTaggerProvider")]
    [TagType(typeof(ClassificationTag))]
    internal sealed class AsmDocUnderlineTaggerProvider : IViewTaggerProvider
    {
        [Import]
        private IClassificationTypeRegistryService _classificationTypeRegistry = null;

        private static IClassificationType UnderlineClassification = null;

        public static AsmDocUnderlineTagger GetClassifierForView(ITextView view)
        {
            if (UnderlineClassification == null)
            {
                return null;
            }
            return view.Properties.GetOrCreateSingletonProperty(() => new AsmDocUnderlineTagger(view, UnderlineClassification));
        }

        public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
        {
            //AsmDudeToolsStatic.Output_INFO("AsmDocUnderlineTaggerProvider:CreateTagger: file=" + AsmDudeToolsStatic.GetFileName(buffer));
            if (UnderlineClassification == null)
            {
                UnderlineClassification = this._classificationTypeRegistry.GetClassificationType(AsmDocClassificationDefinition.ClassificationTypeNames.Underline);
            }
            if (textView.TextBuffer != buffer)
            {
                return null;
            }
            return GetClassifierForView(textView) as ITagger<T>;
        }
    }
}
