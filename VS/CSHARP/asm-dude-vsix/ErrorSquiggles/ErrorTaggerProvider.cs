﻿using AsmDude.SyntaxHighlighting;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;

namespace AsmDude.ErrorSquiggles {

    [Export(typeof(IViewTaggerProvider))]
    [ContentType(AsmDudePackage.AsmDudeContentType)]
    [TagType(typeof(ErrorTag))]
    internal sealed class ErrorTaggerProvider : IViewTaggerProvider {

        [Import]
        private ITextSearchService _textSearchService { get; set; }

        [Import]
        private IBufferTagAggregatorFactoryService _aggregatorFactory = null;

        public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag {
            if (textView == null) {
                return null;
            }
            //provide Error Squiggles only on the top-level buffer
            if (textView.TextBuffer != buffer) {
                return null;
            }
            ITagAggregator<AsmTokenTag> asmTagAggregator = _aggregatorFactory.CreateTagAggregator<AsmTokenTag>(buffer);
            return new ErrorTagger(textView, buffer, asmTagAggregator, _textSearchService) as ITagger<T>;
        }
    }
}
