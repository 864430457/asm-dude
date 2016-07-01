// The MIT License (MIT)
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

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

/*
namespace AsmDude.OptionsPage {
    [Guid(Guids.GuidOptionsPageCodeCompletion)]
    public class OptionsPageCodeCompletion : DialogPage {

        #region Properties
        private const string cat = "Architectures used for Code Completion";

        [Category("General")]
        [Description("Use Code Completion")]
        [DisplayName("Use Code Completion")]
        [DefaultValue(true)]
        public bool _useCodeCompletion { get; set; }

        [Category(cat)]
        [Description("x86")]
        [DisplayName("x86")]
        [DefaultValue(true)]
        public bool _x86 { get; set; }

        [Category(cat)]
        [Description("i686 (conditional move and set)")]
        [DisplayName("i686")]
        [DefaultValue(true)]
        public bool _i686 { get; set; }

        [Category(cat)]
        [Description("MMX")]
        [DisplayName("MMX")]
        [DefaultValue(false)]
        public bool _mmx { get; set; }

        [Category(cat)]
        [Description("SSE")]
        [DisplayName("SSE")]
        [DefaultValue(true)]
        public bool _sse { get; set; }

        [Category(cat)]
        [Description("SSE2")]
        [DisplayName("SSE2")]
        [DefaultValue(true)]
        public bool _sse2 { get; set; }

        [Category(cat)]
        [Description("SSE3")]
        [DisplayName("SSE3")]
        [DefaultValue(true)]
        public bool _sse3 { get; set; }

        [Category(cat)]
        [Description("SSSE3")]
        [DisplayName("SSSE3")]
        [DefaultValue(true)]
        public bool _ssse3 { get; set; }

        [Category(cat)]
        [Description("SSE4.1")]
        [DisplayName("SSE4.1")]
        [DefaultValue(true)]
        public bool _sse41 { get; set; }

        [Category(cat)]
        [Description("SSE4.2")]
        [DisplayName("SSE4.2")]
        [DefaultValue(true)]
        public bool _sse42 { get; set; }

        [Category(cat)]
        [Description("AVX")]
        [DisplayName("AVX")]
        [DefaultValue(true)]
        public bool _avx { get; set; }

        [Category(cat)]
        [Description("AVX2")]
        [DisplayName("AVX2")]
        [DefaultValue(false)]
        public bool _avx2 { get; set; }

        [Category(cat)]
        [Description("Xeon Phi Knights Corner")]
        [DisplayName("KNC")]
        [DefaultValue(false)]
        public bool _knc { get; set; }

        #endregion Properties

        #region Event Handlers

        /// <summary>
        /// Handles "activate" messages from the Visual Studio environment.
        /// </summary>
        /// <devdoc>
        /// This method is called when Visual Studio wants to activate this page.  
        /// </devdoc>
        /// <remarks>If this handler sets e.Cancel to true, the activation will not occur.</remarks>
        protected override void OnActivate(CancelEventArgs e) {
            base.OnActivate(e);
            this._useCodeCompletion = Settings.Default.CodeCompletion_On;

            this._x86 = Settings.Default.CodeCompletion_x86;
            this._i686 = Settings.Default.CodeCompletion_i686;
            this._mmx = Settings.Default.CodeCompletion_mmx;
            this._sse = Settings.Default.CodeCompletion_sse;
            this._sse2 = Settings.Default.CodeCompletion_sse2;
            this._sse3 = Settings.Default.CodeCompletion_sse3;
            this._ssse3 = Settings.Default.CodeCompletion_ssse3;
            this._sse41 = Settings.Default.CodeCompletion_sse41;
            this._sse42 = Settings.Default.CodeCompletion_sse42;
            this._avx = Settings.Default.CodeCompletion_avx;
            this._avx2 = Settings.Default.CodeCompletion_avx2;
            this._knc = Settings.Default.CodeCompletion_knc;
        }

        /// <summary>
        /// Handles "close" messages from the Visual Studio environment.
        /// </summary>
        /// <devdoc>
        /// This event is raised when the page is closed.
        /// </devdoc>
        protected override void OnClosed(EventArgs e) {}

        /// <summary>
        /// Handles "deactivate" messages from the Visual Studio environment.
        /// </summary>
        /// <devdoc>
        /// This method is called when VS wants to deactivate this
        /// page.  If this handler sets e.Cancel, the deactivation will not occur.
        /// </devdoc>
        /// <remarks>
        /// A "deactivate" message is sent when focus changes to a different page in
        /// the dialog.
        /// </remarks>
        protected override void OnDeactivate(CancelEventArgs e) {
            bool changed = false;
            if (Settings.Default.CodeCompletion_On != this._useCodeCompletion) {
                changed = true;
            }
            if (Settings.Default.CodeCompletion_x86 != this._x86) {
                changed = true;
            }
            if (Settings.Default.CodeCompletion_i686 != this._i686) {
                changed = true;
            }
            if (Settings.Default.CodeCompletion_mmx != this._mmx) {
                changed = true;
            }
            if (Settings.Default.CodeCompletion_sse != this._sse) {
                changed = true;
            }
            if (Settings.Default.CodeCompletion_sse2 != this._sse2) {
                changed = true;
            }
            if (Settings.Default.CodeCompletion_sse3 != this._sse3) {
                changed = true;
            }
            if (Settings.Default.CodeCompletion_ssse3 != this._ssse3) {
                changed = true;
            }
            if (Settings.Default.CodeCompletion_sse41 != this._sse41) {
                changed = true;
            }
            if (Settings.Default.CodeCompletion_sse42 != this._sse42) {
                changed = true;
            }
            if (Settings.Default.CodeCompletion_avx != this._avx) {
                changed = true;
            }
            if (Settings.Default.CodeCompletion_avx2 != this._avx2) {
                changed = true;
            }
            if (Settings.Default.CodeCompletion_knc != this._knc) {
                changed = true;
            }
            if (changed) {
                string title = null;
                string message = "Unsaved changes exist. Would you like to save.";
                int result = VsShellUtilities.ShowMessageBox(Site, message, title, OLEMSGICON.OLEMSGICON_QUERY, OLEMSGBUTTON.OLEMSGBUTTON_OKCANCEL, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                if (result == (int)VSConstants.MessageBoxResult.IDOK) {
                    this.save();
                } else if (result == (int)VSConstants.MessageBoxResult.IDCANCEL) {
                    e.Cancel = true;
                }
            }
        }

        /// <summary>
        /// Handles "apply" messages from the Visual Studio environment.
        /// </summary>
        /// <devdoc>
        /// This method is called when VS wants to save the user's 
        /// changes (for example, when the user clicks OK in the dialog).
        /// </devdoc>
        protected override void OnApply(PageApplyEventArgs e) {
            this.save();
            base.OnApply(e);
        }

        private void save() {
            //Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "INFO:{0}:OnApply", this.ToString()));

            bool changed = false;
            bool restartNeeded = false;

            if (Settings.Default.CodeCompletion_On != this._useCodeCompletion) {
                Settings.Default.CodeCompletion_On = this._useCodeCompletion;
                changed = true;
            }
            if (Settings.Default.CodeCompletion_x86 != this._x86) {
                Settings.Default.CodeCompletion_x86 = this._x86;
                changed = true;
                restartNeeded = true;
            }
            if (Settings.Default.CodeCompletion_i686 != this._i686) {
                Settings.Default.CodeCompletion_i686 = this._i686;
                changed = true;
                restartNeeded = true;
            }
            if (Settings.Default.CodeCompletion_mmx != this._mmx) {
                Settings.Default.CodeCompletion_mmx = this._mmx;
                changed = true;
                restartNeeded = true;
            }
            if (Settings.Default.CodeCompletion_sse != this._sse) {
                Settings.Default.CodeCompletion_sse = this._sse;
                changed = true;
                restartNeeded = true;
            }
            if (Settings.Default.CodeCompletion_sse2 != this._sse2) {
                Settings.Default.CodeCompletion_sse2 = this._sse2;
                changed = true;
                restartNeeded = true;
            }
            if (Settings.Default.CodeCompletion_sse3 != this._sse3) {
                Settings.Default.CodeCompletion_sse3 = this._sse3;
                changed = true;
                restartNeeded = true;
            }
            if (Settings.Default.CodeCompletion_ssse3 != this._ssse3) {
                Settings.Default.CodeCompletion_ssse3 = this._ssse3;
                changed = true;
                restartNeeded = true;
            }
            if (Settings.Default.CodeCompletion_sse41 != this._sse41) {
                Settings.Default.CodeCompletion_sse41 = this._sse41;
                changed = true;
                restartNeeded = true;
            }
            if (Settings.Default.CodeCompletion_sse42 != this._sse42) {
                Settings.Default.CodeCompletion_sse42 = this._sse42;
                changed = true;
                restartNeeded = true;
            }
            if (Settings.Default.CodeCompletion_avx != this._avx) {
                Settings.Default.CodeCompletion_avx = this._avx;
                changed = true;
                restartNeeded = true;
            }
            if (Settings.Default.CodeCompletion_avx2 != this._avx2) {
                Settings.Default.CodeCompletion_avx2 = this._avx2;
                changed = true;
                restartNeeded = true;
            }
            if (Settings.Default.CodeCompletion_knc != this._knc) {
                Settings.Default.CodeCompletion_knc = this._knc;
                changed = true;
                restartNeeded = true;
            }
            if (changed) {
                Settings.Default.Save();
            }
            if (restartNeeded) {
                string title = null;
                string message = "You may need to restart visual studio for the changes to take effect.";
                int result = VsShellUtilities.ShowMessageBox(Site, message, title, OLEMSGICON.OLEMSGICON_QUERY, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
        }

        #endregion Event Handlers
    }
}
*/
