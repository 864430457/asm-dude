using System;
using System.Diagnostics;
using System.Xml;
using System.Globalization;
using System.IO;
using System.Windows;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Collections.Generic;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace AsmDude {


    public static class AsmDudeToolsStatic {

        public static CompositionContainer getCompositionContainer() {
            AssemblyCatalog catalog = new AssemblyCatalog(System.Reflection.Assembly.GetExecutingAssembly());
            CompositionContainer container = new CompositionContainer(catalog);
            return container;
        }

        public static string getInstallPath() {
            try {
                string fullPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                string filenameDll = "AsmDude.dll";
                return fullPath.Substring(0, fullPath.Length - filenameDll.Length);
            } catch (Exception) {
                return "";
            }
        }

        public static System.Windows.Media.Color convertColor(System.Drawing.Color color) {
            return System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B);
        }

        public static ImageSource bitmapFromUri(Uri bitmapUri) {
            var bitmap = new BitmapImage();
            try {
                bitmap.BeginInit();
                bitmap.UriSource = bitmapUri;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
            } catch (Exception e) {
                Debug.WriteLine("WARNING: bitmapFromUri: could not read icon from uri " + bitmapUri.ToString() + "; " + e.Message);
            }
            return bitmap;
        }

        public static void Output(string msg) {
            // Get the output window
            var outputWindow = Package.GetGlobalService(typeof(SVsOutputWindow)) as IVsOutputWindow;
            // Ensure that the desired pane is visible
            var paneGuid = Microsoft.VisualStudio.VSConstants.OutputWindowPaneGuid.GeneralPane_guid;
            IVsOutputWindowPane pane;
            outputWindow.CreatePane(paneGuid, "AsmDude", 1, 0);
            outputWindow.GetPane(paneGuid, out pane);
            pane.OutputString(string.Format(CultureInfo.CurrentCulture, "{0}", msg + Environment.NewLine));
        }

        public static bool isRemarkChar(char c) {
            return c.Equals('#') || c.Equals(';');
        }
        
        public static bool isSeparatorChar(char c) {
            return char.IsWhiteSpace(c) || c.Equals(',') || c.Equals('[') || c.Equals(']') || c.Equals('+') || c.Equals('-') || c.Equals('*') || c.Equals(':');
        }

        public static Tuple<bool, int, int> getRemarkPos(string line) {
            int nChars = line.Length;
            for (int i = 0; i < nChars; ++i) {
                if (AsmDudeToolsStatic.isRemarkChar(line[i])) {
                    return new Tuple<bool, int, int>(true, i, nChars);
                }
            }
            return new Tuple<bool, int, int>(false, nChars, nChars);
        }

        public static Tuple<bool, int, int> getLabelPos(string line) {
            int nChars = line.Length;
            int i = 0;

            // find the start of the first keyword
            for (; i < nChars; ++i) {
                char c = line[i];
                if (AsmDudeToolsStatic.isRemarkChar(c)) {
                    return new Tuple<bool, int, int>(false, 0, 0);
                } else if (char.IsWhiteSpace(c)) {
                    // do nothing
                } else {
                    break;
                }
            }
            if (i >= nChars) {
                return new Tuple<bool, int, int>(false, 0, 0);
            }
            int beginPos = i;
            // position i points to the start of the current keyword
            //AsmDudeToolsStatic.Output("getLabelEndPos: found first char of first keyword "+ line[i]+".");

            for (; i < nChars; ++i) {
                char c = line[i];
                if (c.Equals(':')) {
                    return new Tuple<bool, int, int>(true, beginPos, i);
                } else if (AsmDudeToolsStatic.isRemarkChar(c)) {
                    return new Tuple<bool, int, int>(false, 0, 0);
                } else if (AsmDudeToolsStatic.isSeparatorChar(c)) {
                    // found another keyword: labels can only be the first keyword on a line
                    break;
                }
            }
            return new Tuple<bool, int, int>(false, 0, 0);
        }

        public static bool isConstant(string token) {
            string token2;
            if (token.StartsWith("0x", StringComparison.CurrentCultureIgnoreCase)) {
                token2 = token.Substring(2);
            } else if (token.EndsWith("h", StringComparison.CurrentCultureIgnoreCase)) {
                token2 = token.Substring(0,token.Length-1);
            } else {
                token2 = token;
            }
            ulong dummy;
            bool parsedSuccessfully = ulong.TryParse(token2, NumberStyles.HexNumber, CultureInfo.CurrentCulture, out dummy);
            return parsedSuccessfully;
        }

        /// <summary>
        /// Get all labels with context info containing in the provided text
        /// </summary>
        public static IList<Tuple<string, string>> getLabels(ITextBuffer text) {
            var result = new List<Tuple<string, string>>();
            //TODO: this can be slow on sources with many labels, better would be to precompute the list of labels.

            foreach (ITextSnapshotLine line in text.CurrentSnapshot.Lines) {
                string str = line.GetText();
                //AsmDudeToolsStatic.Output(string.Format("INFO: getLabels: str=\"{0}\"", str));

                Tuple<bool, int, int> labelPos = AsmDudeToolsStatic.getLabelPos(str);
                if (labelPos.Item1) {
                    int labelBeginPos = labelPos.Item2;
                    int labelEndPos = labelPos.Item3;
                    string label = str.Substring(labelBeginPos, labelEndPos-labelBeginPos);
                    //AsmDudeToolsStatic.Output(string.Format("INFO: getLabels: label=\"{0}\"", label));
                    result.Add(new Tuple<string, string>(label, "line " + line.LineNumber + ": " + str.Substring(0, Math.Min(str.Length, 100))));
                }
            }
            result.Sort((x, y) => x.Item1.CompareTo(y.Item1));
            return result;
        }

        private static string getKeyword(int pos, char[] line) {
            //Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "INFO: getKeyword; pos={0}; line=\"{1}\"", pos, new string(line)));
            var t = getKeywordPos(pos, line);
            int beginPos = t.Item1;
            int endPos = t.Item2;
            return new string(line).Substring(beginPos, endPos - beginPos);
        }

        private static Tuple<int, int> getKeywordPos(int pos, char[] line) {
            //Debug.WriteLine(string.Format("INFO: getKeyword; pos={0}; line=\"{1}\"", pos, new string(line)));
            if ((pos < 0) || (pos >= line.Length)) return null;

            // find the beginning of the keyword
            int beginPos = 0;
            for (int i1 = pos - 1; i1 > 0; --i1) {
                char c = line[i1];
                if (AsmDudeToolsStatic.isSeparatorChar(c) || Char.IsControl(c)) {
                    beginPos = i1 + 1;
                    break;
                }
            }
            // find the end of the keyword
            int endPos = line.Length;
            for (int i2 = pos; i2 < line.Length; ++i2) {
                char c = line[i2];
                if (AsmDudeToolsStatic.isSeparatorChar(c) || Char.IsControl(c) || AsmDudeToolsStatic.isRemarkChar(c)) {
                    endPos = i2;
                    break;
                }
            }
            return new Tuple<int, int>(beginPos, endPos);
        }

        public static string getKeywordStr(SnapshotPoint? bufferPosition) {
            if (bufferPosition != null) {
                int seachSpanSize = 100;
                int rawPos = bufferPosition.Value.Position;
                int bufferLength = bufferPosition.Value.Snapshot.Length;

                int beginSubString = (rawPos > seachSpanSize) ? (rawPos - seachSpanSize) : 0;
                int endSubString = (bufferLength > (seachSpanSize + rawPos)) ? (seachSpanSize + rawPos) : bufferLength;
                int posInSubString = (rawPos > seachSpanSize) ? seachSpanSize : rawPos;
                //Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "INFO: PreprocessMouseUp; rawPos={0}; bufferLength={1}; beginSubString={2}; endSubString={3}; posInSubString={4}", rawPos, bufferLength, beginSubString, endSubString, posInSubString));
                char[] subString = bufferPosition.Value.Snapshot.ToCharArray(beginSubString, endSubString - beginSubString);
                string keyword = AsmDudeToolsStatic.getKeyword(posInSubString, subString);
                return keyword;
            }
            return null;
        }

        public static TextExtent getKeyword(SnapshotPoint bufferPosition) {

            //TODO: no need to search 100 chars left to right; only the current line needs to be searched

            int seachSpanSize = 100;
            int rawPos = bufferPosition.Position;
            int bufferLength = bufferPosition.Snapshot.Length;

            int beginSubString = (rawPos > seachSpanSize) ? (rawPos - seachSpanSize) : 0;
            int endSubString = (bufferLength > (seachSpanSize + rawPos)) ? (seachSpanSize + rawPos) : bufferLength;
            int posInSubString = (rawPos > seachSpanSize) ? seachSpanSize : rawPos;
            //Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "INFO: PreprocessMouseUp; rawPos={0}; bufferLength={1}; beginSubString={2}; endSubString={3}; posInSubString={4}", rawPos, bufferLength, beginSubString, endSubString, posInSubString));
            char[] subString = bufferPosition.Snapshot.ToCharArray(beginSubString, endSubString - beginSubString);

            Tuple<int, int> t = getKeywordPos(posInSubString, subString);
            int beginPos = t.Item1 + beginSubString;
            int endPos = t.Item2 + beginSubString;

            return new TextExtent(new SnapshotSpan(bufferPosition.Snapshot, beginPos, endPos - beginPos), true);
        }

        public static string getRelatedRegister(string reg) {
            switch (reg.ToUpper()) {
                case "RAX":
                case "EAX":
                case "AX":
                case "AL":
                case "AH":
                    return "\\b(RAX|EAX|AX|AH|AL)\\b";
                case "RBX":
                case "EBX":
                case "BX":
                case "BL":
                case "BH":
                    return "\\b(RBX|EBX|BX|BH|BL)\\b";
                case "RCX":
                case "ECX":
                case "CX":
                case "CL":
                case "CH":
                    return "\\b(RCX|ECX|CX|CH|CL)\\b";
                case "RDX":
                case "EDX":
                case "DX":
                case "DL":
                case "DH":
                    return "\\b(RDX|EDX|DX|DH|DL)\\b";
                case "RSI":
                case "ESI":
                case "SI":
                case "SIL":
                    return "\\b(RSI|ESI|SI|SIL)\\b";
                case "RDI":
                case "EDI":
                case "DI":
                case "DIL":
                    return "\\b(RDI|EDI|DI|DIL)\\b";
                case "RBP":
                case "EBP":
                case "BP":
                case "BPL":
                    return "\\b(RBP|EBP|BP|BPL)\\b";
                case "RSP":
                case "ESP":
                case "SP":
                case "SPL":
                    return "\\b(RSP|ESP|SP|SPL)\\b";
                case "R8":
                case "R8D":
                case "R8W":
                case "R8B":
                    return "\\b(R8|R8D|R8W|R8B)\\b";
                case "R9":
                case "R9D":
                case "R9W":
                case "R9B":
                    return "\\b(R9|R9D|R9W|R9B)\\b";
                case "R10":
                case "R10D":
                case "R10W":
                case "R10B":
                    return "\\b(R10|R10D|R10W|R10B)\\b";
                case "R11":
                case "R11D":
                case "R11W":
                case "R11B":
                    return "\\b(R11|R11D|R11W|R11B)\\b";
                case "R12":
                case "R12D":
                case "R12W":
                case "R12B":
                    return "\\b(R12|R12D|R12W|R12B)\\b";
                case "R13":
                case "R13D":
                case "R13W":
                case "R13B":
                    return "\\b(R13|R13D|R13W|R13B)\\b";
                case "R14":
                case "R14D":
                case "R14W":
                case "R14B":
                    return "\\b(R14|R14D|R14W|R14B)\\b";
                case "R15":
                case "R15D":
                case "R15W":
                case "R15B":
                    return "\\b(R15|R15D|R15W|R15B)\\b";

                default: return reg;
            }
        }

        private static bool isRegisterMethod1(string keyword) {
            //TODO  get this info from AsmDudeData.xml
            switch (keyword.ToUpper()) {
                case "RAX"://
                case "EAX"://
                case "AX"://
                case "AL"://
                case "AH"://

                case "RBX"://
                case "EBX"://
                case "BX"://
                case "BL"://
                case "BH"://

                case "RCX"://
                case "ECX"://
                case "CX"://
                case "CL"://
                case "CH"://

                case "RDX"://
                case "EDX"://
                case "DX"://
                case "DL"://
                case "DH"://

                case "RSI"://
                case "ESI"://
                case "SI"://
                case "SIL"://

                case "RDI"://
                case "EDI"://
                case "DI"://
                case "DIL"://

                case "RBP"://
                case "EBP"://
                case "BP"://
                case "BPL"://

                case "RSP"://
                case "ESP"://
                case "SP"://
                case "SPL"://
                #region

                case "R8"://
                case "R8D"://
                case "R8W"://
                case "R8B"://

                case "R9"://
                case "R9D"://
                case "R9W"://
                case "R9B"://

                case "R10"://
                case "R10D"://
                case "R10W"://
                case "R10B"://

                case "R11"://
                case "R11D"://
                case "R11W"://
                case "R11B"://

                case "R12"://
                case "R12D"://
                case "R12W"://
                case "R12B"://

                case "R13"://
                case "R13D"://
                case "R13W"://
                case "R13B"://

                case "R14"://
                case "R14D"://
                case "R14W"://
                case "R14B"://

                case "R15"://
                case "R15D"://
                case "R15W"://
                case "R15B"://
                case "MM0"://
                case "MM1"://
                case "MM2"://
                case "MM3"://
                case "MM4"://
                case "MM5"://
                case "MM6"://
                case "MM7"://

                case "XMM0"://
                case "XMM1"://
                case "XMM2"://
                case "XMM3"://
                case "XMM4"://
                case "XMM5"://
                case "XMM6"://
                case "XMM7"://

                case "XMM8"://
                case "XMM9"://
                case "XMM10"://
                case "XMM11"://
                case "XMM12"://
                case "XMM13"://
                case "XMM14"://
                case "XMM15"://

                case "YMM0"://
                case "YMM1"://
                case "YMM2"://
                case "YMM3"://
                case "YMM4"://
                case "YMM5"://
                case "YMM6"://
                case "YMM7"://

                case "YMM8"://
                case "YMM9"://
                case "YMM10"://
                case "YMM11"://
                case "YMM12"://
                case "YMM13"://
                case "YMM14"://
                case "YMM15"://

                case "ZMM0"://
                case "ZMM1"://
                case "ZMM2"://
                case "ZMM3"://
                case "ZMM4"://
                case "ZMM5"://
                case "ZMM6"://
                case "ZMM7"://

                case "ZMM8"://
                case "ZMM9"://
                case "ZMM10":
                case "ZMM11":
                case "ZMM12":
                case "ZMM13":
                case "ZMM14":
                case "ZMM15":

                case "ZMM16":
                case "ZMM17":
                case "ZMM18":
                case "ZMM19":
                case "ZMM20":
                case "ZMM21":
                case "ZMM22":
                case "ZMM23":

                case "ZMM24":
                case "ZMM25":
                case "ZMM26":
                case "ZMM27":
                case "ZMM28":
                case "ZMM29":
                case "ZMM30":
                case "ZMM31":
                    #endregion
                    return true;
                default:
                    return false;
            }
        }

        private static bool isNumber(char c) {
            switch (c) {
                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9': return true;
                default: return false;
            }
        }

        private static bool isRegisterMethod2(string keyword) {
            int length = keyword.Length;
            string str = keyword.ToUpper();
            char c1 = str[0];
            char c2 = str[1];
            char c3 = (length > 2) ? str[2] : ' ';
            char c4 = (length > 3) ? str[3] : ' ';
            char c5 = (length > 4) ? str[4] : ' ';

            switch (length) {
                #region length2
                case 2: 
                    switch (c1) {
                        case 'A': return (c2 == 'X') || (c2 == 'H') || (c2 == 'L');
                        case 'B': return (c2 == 'X') || (c2 == 'H') || (c2 == 'L') || (c2 == 'P');
                        case 'C': return (c2 == 'X') || (c2 == 'H') || (c2 == 'L');
                        case 'D': return (c2 == 'X') || (c2 == 'H') || (c2 == 'L') || (c2 == 'I');
                        case 'S': return (c2 == 'I') || (c2 == 'P');
                        case 'R': return (c2 == '8') || (c2 == '9');
                    }
                    break;
                #endregion
                #region length3
                case 3:
                    switch (c1) {
                        case 'R':
                            switch (c2) {
                                case 'A': return (c3 == 'X');
                                case 'B': return (c3 == 'X') || (c3 == 'P');
                                case 'C': return (c3 == 'X');
                                case 'D': return (c3 == 'X') || (c3 == 'I');
                                case 'S': return (c3 == 'I') || (c3 == 'P');
                                case '8': return (c3 == 'D') || (c3 == 'W') || (c3 == 'B');
                                case '9': return (c3 == 'D') || (c3 == 'W') || (c3 == 'B');
                                case '1': return (c3 == '0') || (c3 == '1') || (c3 == '2') || (c3 == '3') || (c3 == '4');
                            }
                            break;
                        case 'E':
                            switch (c2) {
                                case 'A': return (c3 == 'X');
                                case 'B': return (c3 == 'X') || (c3 == 'P');
                                case 'C': return (c3 == 'X');
                                case 'D': return (c3 == 'X') || (c3 == 'I');
                                case 'S': return (c3 == 'I') || (c3 == 'P');
                            }
                            break;
                        case 'B': return (c2 == 'P') && (c3 == 'L');
                        case 'S':
                            switch (c2) {
                                case 'P': return (c3 == 'L');
                                case 'I': return (c3 == 'L');
                            }
                            break;
                        case 'D': return (c2 == 'I') && (c3 == 'L');
                        case 'M': 
                            if (c2 == 'M') {
                                switch (c3) {
                                    case '0':
                                    case '1':
                                    case '2':
                                    case '3':
                                    case '4':
                                    case '5':
                                    case '6':
                                    case '7': return true;
                                }
                            }
                            break;
                    }
                    break;
                #endregion
                #region length4
                case 4:
                    switch (c1) {
                        case 'R':
                            if (c2 == '1') {
                                switch (c3) {
                                    case '0': return (c4 == 'D') || (c4 == 'W') || (c4 == 'B');
                                    case '1': return (c4 == 'D') || (c4 == 'W') || (c4 == 'B');
                                    case '2': return (c4 == 'D') || (c4 == 'W') || (c4 == 'B');
                                    case '3': return (c4 == 'D') || (c4 == 'W') || (c4 == 'B');
                                    case '4': return (c4 == 'D') || (c4 == 'W') || (c4 == 'B');
                                    case '5': return (c4 == 'D') || (c4 == 'W') || (c4 == 'B');
                                }
                            }
                            break;
                        case 'X':
                            if ((c2 == 'M') && (c3 == 'M')) {
                                return isNumber(c4);
                            }
                            break;
                        case 'Y':
                            if ((c2 == 'M') && (c3 == 'M')) {
                                return isNumber(c4);
                            }
                            break;
                        case 'Z':
                            if ((c2 == 'M') && (c3 == 'M')) {
                                return isNumber(c4);
                            }
                            break;
                    }
                    break;
                #endregion
                #region length5
                case 5:
                    switch (c1) {
                        case 'X':
                            if ((c2 == 'M') && (c3 == 'M') && (c4 == '1')) {
                                switch (c5) {
                                    case '0': 
                                    case '1': 
                                    case '2': 
                                    case '3': 
                                    case '4': 
                                    case '5': return true;
                                }
                            }
                            break;
                        case 'Y':
                            if ((c2 == 'M') && (c3 == 'M') && (c4 == '1')) {
                                switch (c5) {
                                    case '0':
                                    case '1':
                                    case '2':
                                    case '3':
                                    case '4':
                                    case '5': return true;
                                }
                            }
                            break;
                        case 'Z':
                            if ((c2 == 'M') && (c3 == 'M')) {
                                switch (c4) {
                                    case '1': return isNumber(c5);
                                    case '2': return isNumber(c5);
                                    case '3': return ((c5 == '0') || (c5 == '1'));
                                }
                            }
                            break;
                    }
                    break;
                    #endregion
            }
            return false;
        }
        public static bool isRegister(string keyword) {
            int length = keyword.Length;
            if ((length > 5) || (length < 2)) {
                return false;
            }
            bool b2 = isRegisterMethod2(keyword);
#           if _DEBUG
            if (b2 != isRegisterMethod1(keyword)) Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "INFO: isRegister; unequal responses"));
#           endif
            return b2;
        }
    }


    [Export]
    public class AsmDudeTools {
        private XmlDocument _xmlData;
        private IDictionary<string, AsmTokenTypes> _asmTypes;
        private IDictionary<string, string> _arch; // todo make an arch enumeration
        private IDictionary<string, string> _description;


        public AsmDudeTools() {
            //Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "INFO: Entering constructor for: {0}", this.ToString()));
            this.initData(); // load data for speed
        }

        public ICollection<string> getKeywords() {
            if (this._asmTypes == null) initData();
            return this._asmTypes.Keys;
        }

        public AsmTokenTypes getAsmTokenType(string keyword) {
            if (this._asmTypes == null) initData();
            string k2 = keyword.ToUpper();
            if (this._asmTypes.ContainsKey(k2)) {
                return this._asmTypes[k2];
            } else {
                return AsmTokenTypes.UNKNOWN;
            }
        }

        /// <summary>
        /// get url for the provided keyword. Returns empty string if the keyword does not exist or the keyword does not have an url.
        /// </summary>
        public string getUrl(string keyword) {
            // no need to pre-process this information.
            try {
                string keywordUpper = keyword.ToUpper();
                XmlNodeList all = this.getXmlData().SelectNodes("//*[@name=\"" + keywordUpper + "\"]");
                if (all.Count > 1) {
                    Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "WARNING: {0}:getUrl: multiple elements for keyword {1}.", this.ToString(), keywordUpper));
                }
                if (all.Count == 0) {
                    //Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "INFO: {0}:getUrl: no elements for keyword {1}.", this.ToString(), keywordUpper));
                    return "";
                } else {
                    XmlNode node1 = all.Item(0);
                    XmlNode node2 = node1.SelectSingleNode("./ref");
                    if (node2 == null) return "";
                    string text = node2.InnerText.Trim();
                    //Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "INFO: {0}:getUrl: keyword {1} yields {2}", this.ToString(), keyword, text));
                    return text;
                }
            } catch (Exception) {
                return "";
            }
        }

        /// <summary>
        /// get url for the provided keyword. Returns empty string if the keyword does not exist or the keyword does not have an url.
        /// </summary>
        public string getDescription(string keyword) {
            if (this._description.ContainsKey(keyword)) {
                return this._description[keyword];
            } else {
                return "";
            }
        }

        /// <summary>
        /// Determine whether the provided keyword is a jump opcode.
        /// </summary>
        public bool isJumpKeyword(string keyword) {
            if (this._asmTypes == null) initData();
            string k2 = keyword.ToUpper();
            return (this._asmTypes.ContainsKey(k2)) ? (this._asmTypes[k2] == AsmTokenTypes.Jump) : false;
        }


        public string getLabelDescription(string label, ITextBuffer text) {
            string result = "";
            foreach (ITextSnapshotLine line in text.CurrentSnapshot.Lines) {
                string str = line.GetText();

                // find first occurrence of a colon
                int posColon = -1;

                for (int pos = 0; pos < str.Length; ++pos) {
                    char c = str[pos];
                    if (c == ':') {
                        posColon = pos;
                        break;
                    } else if (AsmDudeToolsStatic.isRemarkChar(c)) {
                        break;
                    }
                }
                if (posColon > 0) {
                    string labelLocal = str.Substring(0, posColon).Trim();
                    if (labelLocal.Equals(label)) {
                        //Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "INFO: {0}:getLabelDescription: label=\"{1}\"", this.ToString(), label));
                        if (result.Length > 0) result += System.Environment.NewLine;
                        result += "line " + line.LineNumber + ": " + str.Substring(0, Math.Min(str.Length, 100));
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Get architecture of the provided keyword
        /// </summary>
        public string getArchitecture(string keyword) {
            return this._arch[keyword.ToUpper()];
        }

        public void invalidateData() {
            this._xmlData = null;
            this._asmTypes = null;
            this._description = null;
        }

#region private stuff

        private void initData() {
            this._asmTypes = new Dictionary<string, AsmTokenTypes>();
            this._arch = new Dictionary<string, string>();
            this._description = new Dictionary<string, string>();
       
            // fill the dictionary with keywords
            AsmDudeToolsStatic.getCompositionContainer().SatisfyImportsOnce(this);
            XmlDocument xmlDoc = this.getXmlData();
            foreach (XmlNode node in xmlDoc.SelectNodes("//misc")) {
                var nameAttribute = node.Attributes["name"];
                if (nameAttribute == null) {
                    Debug.WriteLine("WARNING: AsmTokenTagger: found misc with no name");
                } else {
                    string name = nameAttribute.Value.ToUpper();
                    //Debug.WriteLine("INFO: AsmTokenTagger: found misc " + name);
                    this._asmTypes[name] = AsmTokenTypes.Misc;
                    this._arch[name] = retrieveArch(node);
                    this._description[name] = retrieveDescription(node);
                }
            }

            foreach (XmlNode node in xmlDoc.SelectNodes("//directive")) {
                var nameAttribute = node.Attributes["name"];
                if (nameAttribute == null) {
                    Debug.WriteLine("WARNING: AsmTokenTagger: found directive with no name");
                } else {
                    string name = nameAttribute.Value.ToUpper();
                    //Debug.WriteLine("INFO: AsmTokenTagger: found directive " + name);
                    this._asmTypes[name] = AsmTokenTypes.Directive;
                    this._arch[name] = retrieveArch(node);
                    this._description[name] = retrieveDescription(node);
                }
            }
            foreach (XmlNode node in xmlDoc.SelectNodes("//mnemonic")) {
                var nameAttribute = node.Attributes["name"];
                if (nameAttribute == null) {
                    Debug.WriteLine("WARNING: AsmTokenTagger: found mnemonic with no name");
                } else {
                    string name = nameAttribute.Value.ToUpper();
                    //Debug.WriteLine("INFO: AsmTokenTagger: found mnemonic " + name);

                    var typeAttribute = node.Attributes["type"];
                    if (typeAttribute == null) {
                        this._asmTypes[name] = AsmTokenTypes.Mnemonic;
                    } else {
                        if (typeAttribute.Value.ToUpper().Equals("JUMP")) {
                            this._asmTypes[name] = AsmTokenTypes.Jump;
                        } else {
                            this._asmTypes[name] = AsmTokenTypes.Mnemonic;
                        }
                    }
                    this._arch[name] = retrieveArch(node);
                    this._description[name] = retrieveDescription(node);
                }
            }
            foreach (XmlNode node in xmlDoc.SelectNodes("//register")) {
                var nameAttribute = node.Attributes["name"];
                if (nameAttribute == null) {
                    Debug.WriteLine("WARNING: AsmTokenTagger: found register with no name");
                } else {
                    string name = nameAttribute.Value.ToUpper();
                    //Debug.WriteLine("INFO: AsmTokenTagger: found register " + name);
                    this._asmTypes[name] = AsmTokenTypes.Register;
                    this._arch[name] = retrieveArch(node);
                    this._description[name] = retrieveDescription(node);
                }
            }
        }

        private string retrieveArch(XmlNode node) {
            try {
                var archAttribute = node.Attributes["arch"];
                if (archAttribute == null) {
                    return null;
                } else {
                    return archAttribute.Value.ToUpper();
                }
            } catch (Exception) {
                return null;
            }
        }

        private string retrieveDescription(XmlNode node) {
            try {
                XmlNode node2 = node.SelectSingleNode("./description");
                if (node2 == null) return "";
                string text = node2.InnerText.Trim();
                //Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "INFO: {0}:getDescription: keyword {1} yields {2}", this.ToString(), keyword, text));
                return text;
            } catch (Exception) {
                return "";
            }
        }

        private XmlDocument getXmlData() {
            //Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "INFO: {0}:getXmlData", this.ToString()));
            if (this._xmlData == null) {
                string filename = AsmDudeToolsStatic.getInstallPath() + "Resources" + Path.DirectorySeparatorChar + "AsmDudeData.xml";
                Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "INFO: {0}:getXmlData: going to load file \"{1}\"", this.ToString(), filename));
                try {
                    this._xmlData = new XmlDocument();
                    this._xmlData.Load(filename);
                } catch (FileNotFoundException) {
                    MessageBox.Show("ERROR: AsmTokenTagger: could not find file \"" + filename + "\".");
                } catch (XmlException) {
                    MessageBox.Show("ERROR: AsmTokenTagger: xml error while reading file \"" + filename + "\".");
                } catch (Exception e) {
                    MessageBox.Show("ERROR: AsmTokenTagger: error while reading file \"" + filename + "\"." + e);
                }
            }
            return this._xmlData;
        }

#endregion
    }
}